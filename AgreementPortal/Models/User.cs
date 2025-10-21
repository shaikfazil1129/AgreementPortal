using System.ComponentModel.DataAnnotations;

namespace AgreementPortal.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public int Role_Id { get; set; }
    }
}
