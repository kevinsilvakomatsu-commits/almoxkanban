using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Services;

namespace AlmoxKanban.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class LogoutModel : PageModel
    {
        private readonly IAuthService _authService;

        public LogoutModel(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _authService.SignOutAsync(HttpContext);
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _authService.SignOutAsync(HttpContext);
            return RedirectToPage("/Login");
        }
    }
}
