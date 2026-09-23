using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Data;
using AlmoxKanban.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlmoxKanban.Pages.Equipe
{
    public class CriarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly IUploadService _uploadService;
        private readonly IAuditoriaService _auditoria;

        public CriarModel(AppDbContext context, IAuthService authService, IUploadService uploadService, IAuditoriaService auditoria)
        {
            _context = context;
            _authService = authService;
            _uploadService = uploadService;
            _auditoria = auditoria;
        }

        [BindProperty]
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(150)]
        public string NomeCompleto { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(50)]
        public string NomeUsuario { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A senha inicial é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string SenhaInicial { get; set; } = "Almox@123";

        [BindProperty]
        [Required(ErrorMessage = "A função/cargo é obrigatória.")]
        [StringLength(100)]
        public string Funcao { get; set; } = "Operador de Almoxarifado";

        [BindProperty]
        public bool IsAdmin { get; set; } = false;

        [BindProperty]
        public IFormFile? FotoPerfil { get; set; }

        public string? MensagemErro { get; set; }

        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var jaExiste = await _context.Usuarios.AnyAsync(u => u.NomeUsuario.ToLower() == NomeUsuario.Trim().ToLower());
            if (jaExiste)
            {
                MensagemErro = $"O nome de usuário '{NomeUsuario}' já está em uso por outro integrante.";
                return Page();
            }

            string? caminhoFoto = null;
            if (FotoPerfil != null)
            {
                var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(FotoPerfil, "perfis");
                if (!sucesso)
                {
                    MensagemErro = erro;
                    return Page();
                }
                caminhoFoto = caminho;
            }

            var novoUsuario = new Usuario
            {
                NomeCompleto = NomeCompleto.Trim(),
                NomeUsuario = NomeUsuario.Trim().ToLower(),
                SenhaHash = _authService.GerarHash(SenhaInicial),
                Funcao = Funcao.Trim(),
                FotoPerfil = caminhoFoto,
                IsAdmin = IsAdmin,
                Ativo = true,
                DeveTrocarSenha = true,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            _context.Usuarios.Add(novoUsuario);
            await _context.SaveChangesAsync();

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminIdStr, out var adminId);
            await _auditoria.RegistrarAsync(adminId, "CriarUsuario", "Usuario", novoUsuario.Id, $"Usuário {novoUsuario.NomeUsuario} criado", HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["MensagemSucesso"] = $"Integrante {novoUsuario.NomeCompleto} cadastrado com sucesso!";
            return RedirectToPage("/Equipe/Index");
        }
    }
}
