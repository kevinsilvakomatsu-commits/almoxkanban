using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Data;
using AlmoxKanban.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlmoxKanban.Pages.Equipe
{
    public class EditarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly IUploadService _uploadService;
        private readonly IAuditoriaService _auditoria;

        public EditarModel(AppDbContext context, IAuthService authService, IUploadService uploadService, IAuditoriaService auditoria)
        {
            _context = context;
            _authService = authService;
            _uploadService = uploadService;
            _auditoria = auditoria;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(150)]
        public string NomeCompleto { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A função é obrigatória.")]
        [StringLength(100)]
        public string Funcao { get; set; } = string.Empty;

        [BindProperty]
        public bool Ativo { get; set; }

        [BindProperty]
        public bool IsAdmin { get; set; }

        [BindProperty]
        public string? NovaSenhaReset { get; set; }

        [BindProperty]
        public IFormFile? NovaFotoPerfil { get; set; }

        public string NomeUsuario { get; set; } = string.Empty;
        public string? FotoAtual { get; set; }
        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var u = await _context.Usuarios.FindAsync(id);
            if (u == null) return NotFound();

            Id = u.Id;
            NomeCompleto = u.NomeCompleto;
            NomeUsuario = u.NomeUsuario;
            Funcao = u.Funcao;
            Ativo = u.Ativo;
            IsAdmin = u.IsAdmin;
            FotoAtual = u.FotoPerfil;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var u = await _context.Usuarios.FindAsync(Id);
            if (u == null) return NotFound();

            NomeUsuario = u.NomeUsuario;
            FotoAtual = u.FotoPerfil;

            if (!ModelState.IsValid) return Page();

            u.NomeCompleto = NomeCompleto.Trim();
            u.Funcao = Funcao.Trim();
            u.Ativo = Ativo;
            u.IsAdmin = IsAdmin;
            u.DataAtualizacao = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(NovaSenhaReset))
            {
                if (NovaSenhaReset.Length < 6)
                {
                    MensagemErro = "A nova senha deve ter no mínimo 6 caracteres.";
                    return Page();
                }
                u.SenhaHash = _authService.GerarHash(NovaSenhaReset);
                u.DeveTrocarSenha = true;
            }

            if (NovaFotoPerfil != null)
            {
                var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(NovaFotoPerfil, "perfis");
                if (!sucesso)
                {
                    MensagemErro = erro;
                    return Page();
                }
                u.FotoPerfil = caminho;
            }

            await _context.SaveChangesAsync();

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminIdStr, out var adminId);
            await _auditoria.RegistrarAsync(adminId, "EditarUsuario", "Usuario", u.Id, $"Dados do usuário {u.NomeUsuario} alterados", HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["MensagemSucesso"] = "Dados do usuário atualizados com sucesso!";
            return RedirectToPage("/Equipe/Index");
        }
    }
}
