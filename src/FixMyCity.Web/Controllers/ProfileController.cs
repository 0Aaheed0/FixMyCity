using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new Models.ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role,
                CurrentProfileImageFileName = user.ProfileImageFileName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Models.ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                model.CurrentProfileImageFileName = user.ProfileImageFileName;
                return View(model);
            }

            // Update simple fields
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            // Handle uploaded profile image
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("ProfileImage", "Only JPG, PNG and GIF images are allowed.");
                    model.CurrentProfileImageFileName = user.ProfileImageFileName;
                    return View(model);
                }

                const long maxBytes = 2 * 1024 * 1024; // 2 MB
                if (model.ProfileImage.Length > maxBytes)
                {
                    ModelState.AddModelError("ProfileImage", "Profile image must be 2 MB or smaller.");
                    model.CurrentProfileImageFileName = user.ProfileImageFileName;
                    return View(model);
                }

                var uploads = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");
                Directory.CreateDirectory(uploads);

                var fileName = user.Id + ext;
                var filePath = Path.Combine(uploads, fileName);

                // Save new file
                using (var stream = System.IO.File.Create(filePath))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                // Update user record with relative path/file name
                user.ProfileImageFileName = fileName;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.CurrentProfileImageFileName = user.ProfileImageFileName;
                return View(model);
            }

            TempData["ProfileSuccess"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(Models.ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                TempData["ChangePasswordErrors"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                TempData["ChangePasswordErrors"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["ChangePasswordSuccess"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }
    }
}