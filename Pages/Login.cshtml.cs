using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Services;
using System.ComponentModel.DataAnnotations;

namespace AlmoxKanban.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IAuditoriaService _auditoria;

        public LoginModel(IAuthService authService, IAuditoriaService auditoria)
        {
            _authService = authService;
            _auditoria = auditoria;
        }

        [BindProperty]
        [Required(ErrorMessage = "Informe o usuário.")]
        public string Usuario { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [BindProperty]
        public bool LembrarMe { get; set; }

        public string? MensagemErro { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var (sucesso, usuario, mensagem) = await _authService.ValidarLoginAsync(Usuario, Senha);
            if (!sucesso || usuario == null)
            {
                MensagemErro = mensagem ?? "Usuário ou senha inválidos.";
                await _auditoria.RegistrarAsync(null, "FalhaLogin", "Usuario", null, $"Tentativa falha para '{Usuario}'", HttpContext.Connection.RemoteIpAddress?.ToString());
                return Page();
            }

            await _authService.SignInAsync(HttpContext, usuario, LembrarMe);
            await _auditoria.RegistrarAsync(usuario.Id, "Login", "Usuario", usuario.Id, "Login realizado com sucesso", HttpContext.Connection.RemoteIpAddress?.ToString());

            if (usuario.DeveTrocarSenha)
            {
                return RedirectToPage("/AlterarSenha");
            }

            return RedirectToPage("/Index");
        }
    }
}
