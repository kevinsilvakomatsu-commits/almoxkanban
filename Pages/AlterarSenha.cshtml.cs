using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlmoxKanban.Pages
{
    public class AlterarSenhaModel : PageModel
    {
        private readonly IAuthService _authService;

        public AlterarSenhaModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string? SenhaAtual { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve conter no mínimo 6 caracteres.")]
        [DataType(DataType.Password)]
        public string NovaSenha { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem.")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha { get; set; } = string.Empty;

        public bool PrimeiroAcesso { get; set; }
        public string? MensagemErro { get; set; }

        public void OnGet()
        {
            PrimeiroAcesso = User.FindFirst("DeveTrocarSenha")?.Value == "True";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            PrimeiroAcesso = User.FindFirst("DeveTrocarSenha")?.Value == "True";

            if (!ModelState.IsValid) return Page();

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId))
            {
                return RedirectToPage("/Login");
            }

            var (sucesso, erro) = await _authService.AlterarSenhaAsync(usuarioId, SenhaAtual ?? "", NovaSenha);
            if (!sucesso)
            {
                MensagemErro = erro;
                return Page();
            }

            // Realizar logout para forçar login com a nova senha
            await _authService.SignOutAsync(HttpContext);
            TempData["MensagemSucesso"] = "Senha alterada com sucesso! Faça login com a nova senha.";
            return RedirectToPage("/Login");
        }
    }
}
