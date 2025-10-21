using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgreementPortal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            var roleId = User.FindFirst("RoleId")?.Value;

            ViewBag.Username = username;
            ViewBag.RoleId = roleId;

            return View();
        }
    }
}
