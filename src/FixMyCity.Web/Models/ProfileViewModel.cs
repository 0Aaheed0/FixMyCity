using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FixMyCity.Web.Models
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Role")]
        public string? Role { get; set; }

        [Display(Name = "Profile picture")]
        public IFormFile? ProfileImage { get; set; }

        // Current image file name (for display)
        public string? CurrentProfileImageFileName { get; set; }
    }
}
