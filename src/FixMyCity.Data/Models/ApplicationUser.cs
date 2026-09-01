using Microsoft.AspNetCore.Identity;

namespace FixMyCity.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Citizen"; // Citizen, DepartmentStaff, DepartmentManager, Administrator
        // Optional profile fields
        public string? Address { get; set; }

        // Relative URL or file name for the profile picture stored under wwwroot/uploads/profile-pictures
        public string? ProfileImageFileName { get; set; }
    }
}