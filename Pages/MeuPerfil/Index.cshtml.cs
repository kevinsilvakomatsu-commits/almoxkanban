using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Data;
using AlmoxKanban.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AlmoxKanban.Pages.MeuPerfil
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IUploadService _uploadService;
        private readonly IAuthService _authService;

        public IndexModel(AppDbContext context, IUploadService uploadService, IAuthService authService)
        {
            _context = context;
            _uploadService = uploadService;
            _authService = authService;
        }

        public Usuario Usuario { get; set; } = null!;
        public List<Tarefa> MinhasTarefasAbertas { get; set; } = new();
        public List<Tarefa> MinhasTarefasConcluidas { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(150)]
        public string NomeCompleto { get; set; } = string.Empty;

        [BindProperty]
        public IFormFile? NovaFotoPerfil { get; set; }

        [BindProperty]
        public string? SenhaAtual { get; set; }

        [BindProperty]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A nova senha deve ter no mínimo 6 caracteres.")]
        public string? NovaSenha { get; set; }

        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var u = await _context.Usuarios
                .Include(u => u.TarefasResponsavel).ThenInclude(tr => tr.Tarefa)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (u == null) return RedirectToPage("/Login");

            Usuario = u;
            NomeCompleto = u.NomeCompleto;

            var todas = u.TarefasResponsavel.Select(tr => tr.Tarefa).ToList();
            MinhasTarefasAbertas = todas.Where(t => t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada)
                .OrderBy(t => t.DataPrazo ?? DateTime.MaxValue).ToList();

            MinhasTarefasConcluidas = todas.Where(t => t.Status == StatusTarefa.Concluida)
                .OrderByDescending(t => t.DataConclusao ?? t.DataAtualizacao).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostSalvarPerfilAsync()
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var u = await _context.Usuarios.FindAsync(usuarioId);
            if (u == null) return RedirectToPage("/Login");

            u.NomeCompleto = NomeCompleto.Trim();
            u.DataAtualizacao = DateTime.Now;

            if (NovaFotoPerfil != null)
            {
                var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(NovaFotoPerfil, "perfis");
                if (!sucesso)
                {
                    TempData["MensagemErro"] = erro;
                    return RedirectToPage();
                }
                u.FotoPerfil = caminho;
            }

            await _context.SaveChangesAsync();
            await _authService.SignInAsync(HttpContext, u, true);

            TempData["MensagemSucesso"] = "Perfil atualizado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTrocarSenhaAsync()
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(SenhaAtual) || string.IsNullOrWhiteSpace(NovaSenha))
            {
                TempData["MensagemErro"] = "Informe a senha atual e a nova senha.";
                return RedirectToPage();
            }

            var (sucesso, erro) = await _authService.AlterarSenhaAsync(usuarioId, SenhaAtual, NovaSenha);
            if (!sucesso)
            {
                TempData["MensagemErro"] = erro;
            }
            else
            {
                TempData["MensagemSucesso"] = "Sua senha foi alterada com sucesso!";
            }

            return RedirectToPage();
        }
    }
}
