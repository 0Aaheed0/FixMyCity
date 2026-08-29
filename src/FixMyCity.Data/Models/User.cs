using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Citizen"; // Citizen, DepartmentStaff, DepartmentManager, Administrator
    }
}