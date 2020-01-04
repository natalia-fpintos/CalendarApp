using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BucksCalendar.Areas.Identity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BucksCalendar.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<CalendarUser> _userManager;
        private readonly SignInManager<CalendarUser> _signInManager;

        public IndexModel(
            UserManager<CalendarUser> userManager,
            SignInManager<CalendarUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }
        
        public string ProfileImage { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "This field is required.")]
            [DataType(DataType.Text)]
            [Display(Name = "Full name")]
            public string Name { get; set; }

            [Phone]
            [Display(Name = "Mobile phone")]
            [RegularExpression(@"^07\d{9}$", ErrorMessage = "UK mobile with no spaces.")]
            [DataType(DataType.Text)]
            public string PhoneNumber { get; set; }
            
            [Display(Name = "Profile Picture")]
            public IFormFile Image { get; set; }
        }

        private async Task LoadAsync(CalendarUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            if (user.Image != null)
            {
                var base64Img = Convert.ToBase64String(user.Image);
                ProfileImage = $"data:image/jpg;base64,{base64Img}";
            }
            else
            {
                ProfileImage = "/assets/img/user-default.png";
            }

            Input = new InputModel
            {
                Name = user.Name,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting phone number for user with ID '{userId}'.");
                }
            }
            
            if (Input.Name != user.Name)
            {
                user.Name = Input.Name;
            }

            if (Input.Image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await Input.Image.CopyToAsync(memoryStream);

                    // Upload the file if less than 2 MB
                    if (memoryStream.Length < 2097152)
                    {
                        user.Image = memoryStream.ToArray();
                    }
                    else
                    {
                        StatusMessage = "Please upload a smaller image (max 2MB)";
                        return RedirectToPage();
                    }
                }
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
