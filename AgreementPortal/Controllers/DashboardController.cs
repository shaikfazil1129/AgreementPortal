using AgreementPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace AgreementPortal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _config;

        public DashboardController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            var roleId = User.FindFirst("RoleId")?.Value;
            var userName = User.Identity?.Name;
            ViewBag.Username = userName;
            ViewBag.RoleId = roleId;

            var agreements = GetAgreementsByRole(Convert.ToInt32(roleId));
            return View(agreements);
        }

        private List<Agreement> GetAgreementsByRole(int roleId)
        {
            var agreements = new List<Agreement>();
            string connStr = _config.GetConnectionString("AgreementDB");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string query = "";

            switch (roleId)
            {
                case 1: // Analyst
                    query = "SELECT * FROM [Agreement] WHERE [Status] IN ('SetUp', 'ReadyToProcess') AND [Assigned_To] = @UserId";
                    break;
                case 2: // Manager
                    query = "SELECT * FROM [Agreement] WHERE [Status] = 'Review' AND [Assigned_To] = @UserId";
                    break;
                case 3: // COE
                    query = "SELECT * FROM [Agreement] WHERE [Status] = 'SignOff' AND [Assigned_To] = @UserId";
                    break;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                int.TryParse(userId, out int userIdInt);
                cmd.Parameters.AddWithValue("@UserId", userIdInt);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    agreements.Add(new Agreement
                    {
                        Id = (int)reader["Id"],
                        Agreement_Num = reader["Agreement_Num"].ToString(),
                        Status = reader["Status"].ToString(),
                        Assigned_To = (int)reader["Assigned_To"],
                        Created_Date = Convert.ToDateTime(reader["Created_Date"]),
                        End_Date = reader["End_Date"] == DBNull.Value ? null : Convert.ToDateTime(reader["End_Date"]),
                        Comments = reader["Comments"].ToString()
                    });
                }
            }

            return agreements;
        }

        [HttpGet]
        public IActionResult GetFilteredAgreements(string status, string sort, string roleId)
        {
            var agreements = GetAgreementsByRole(Convert.ToInt32(roleId));

            // Filter by status if selected
            if (!string.IsNullOrEmpty(status))
            {
                agreements = agreements.Where(a => a.Status == status).ToList();
            }

            // Sort by selected option
            if (sort == "endDate")
            {
                agreements = agreements.OrderBy(a => a.End_Date ?? DateTime.MaxValue).ToList();
            }
            else if (sort == "createdDate")
            {
                agreements = agreements.OrderByDescending(a => a.Created_Date).ToList();
            }

            // Return JSON data for AJAX
            return Json(agreements);
        }


        // 🚀 Handle status transitions and reassignments
        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateRequest request)
        {
            string connStr = _config.GetConnectionString("AgreementDB");

            foreach (var agreementId in request.AgreementIds)
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    string newStatus = "";
                    int? newAssignedUser = null;
                    string newComment = "";

                    switch (request.RoleId)
                    {
                        case 1: // Analyst
                            if (request.ActionType == "submit")
                            {
                                newStatus = "Review";
                                newComment = "Review in progress.";
                                newAssignedUser = GetRandomUserByRole(con, 2); // Manager
                            }
                            else if (request.ActionType == "process")
                            {
                                newStatus = "ReadyToProcess";
                                newComment = "Ready to process completed.";
                                newAssignedUser = null; // No assignment as task ends here
                            }

                            break;

                        case 2: // Manager
                            if (request.ActionType == "review")
                            {
                                newStatus = "SignOff";
                                newComment = "Waiting for sign-off.";
                                newAssignedUser = GetRandomUserByRole(con, 3); // COE
                            }
                            else if (request.ActionType == "revoke")
                            {
                                newStatus = "SetUp";
                                newComment = "Reassigned to Analyst.";
                                newAssignedUser = GetRandomUserByRole(con, 1); // Analyst
                            }
                            break;

                        case 3: // COE
                            if (request.ActionType == "signoff")
                            {
                                newStatus = "ReadyToProcess";
                                newComment = "Agreement signed off.";
                                newAssignedUser = GetRandomUserByRole(con, 1); // Analyst
                            }
                            else if (request.ActionType == "revoke")
                            {
                                newStatus = "Review";
                                newComment = "Reassigned to Manager.";
                                newAssignedUser = GetRandomUserByRole(con, 2); // Manager
                            }
                            break;
                    }

                    string updateQuery = "UPDATE [Agreement] SET [Status]=@Status, [Assigned_To]=@Assigned_To, [Comments]=@Comments WHERE [Id]=@Id";
                    SqlCommand cmd = new SqlCommand(updateQuery, con);
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@Comments", newComment);
                    cmd.Parameters.AddWithValue("@Id", agreementId);
                    cmd.Parameters.AddWithValue("@Assigned_To", (object?)newAssignedUser ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { success = true });
        }

        private int GetRandomUserByRole(SqlConnection con, int roleId)
        {
            string q = "SELECT TOP 1 Id FROM [User] WHERE Role_Id = @RoleId ORDER BY NEWID()";
            SqlCommand cmd = new SqlCommand(q, con);
            cmd.Parameters.AddWithValue("@RoleId", roleId);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }

    public class UpdateRequest
    {
        public List<int> AgreementIds { get; set; }
        public int RoleId { get; set; }
        public string ActionType { get; set; } = string.Empty;
    }
}
