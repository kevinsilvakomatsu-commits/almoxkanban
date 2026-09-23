using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Services;
using AlmoxKanban.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlmoxKanban.Pages.Tarefas
{
    public class EditarModel : PageModel
    {
        private readonly ITarefaService _tarefaService;
        private readonly IUploadService _uploadService;
        private readonly AppDbContext _context;

        public EditarModel(ITarefaService tarefaService, IUploadService uploadService, AppDbContext context)
        {
            _tarefaService = tarefaService;
            _uploadService = uploadService;
            _context = context;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        [BindProperty]
        public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Normal;

        [BindProperty]
        public DateTime? DataPrazo { get; set; }

        [BindProperty]
        public string? HoraPrazo { get; set; }

        [BindProperty]
        [StringLength(2000)]
        public string? Observacoes { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Selecione pelo menos um responsável.")]
        public List<int> ResponsaveisIds { get; set; } = new();

        [BindProperty]
        public IFormFile? NovaFotoPrincipal { get; set; }

        public string? FotoPrincipalAtual { get; set; }
        public List<Usuario> UsuariosEquipe { get; set; } = new();
        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["MensagemErro"] = "Apenas administradores podem editar tarefas diretamente.";
                return RedirectToPage("/Tarefas/Detalhes", new { id });
            }

            var tarefa = await _tarefaService.ObterPorIdAsync(id);
            if (tarefa == null) return NotFound();

            Id = tarefa.Id;
            Titulo = tarefa.Titulo;
            Descricao = tarefa.Descricao;
            Prioridade = tarefa.Prioridade;
            Observacoes = tarefa.Observacoes;
            FotoPrincipalAtual = tarefa.FotoPrincipal;

            if (tarefa.DataPrazo.HasValue)
            {
                DataPrazo = tarefa.DataPrazo.Value.Date;
                HoraPrazo = tarefa.DataPrazo.Value.ToString("HH:mm");
            }

            ResponsaveisIds = tarefa.Responsaveis.Select(r => r.UsuarioId).ToList();
            UsuariosEquipe = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.NomeCompleto).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            UsuariosEquipe = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.NomeCompleto).ToListAsync();

            if (ResponsaveisIds == null || !ResponsaveisIds.Any())
            {
                ModelState.AddModelError("ResponsaveisIds", "É obrigatório atribuir pelo menos um responsável.");
            }

            if (!ModelState.IsValid) return Page();

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            DateTime? prazoFinal = null;
            if (DataPrazo.HasValue)
            {
                prazoFinal = DataPrazo.Value.Date;
                if (!string.IsNullOrWhiteSpace(HoraPrazo) && TimeSpan.TryParse(HoraPrazo, out var hora))
                {
                    prazoFinal = prazoFinal.Value.Add(hora);
                }
                else
                {
                    prazoFinal = prazoFinal.Value.AddHours(18);
                }
            }

            string? caminhoNovaFoto = null;
            if (NovaFotoPrincipal != null)
            {
                var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(NovaFotoPrincipal, "tarefas");
                if (!sucesso)
                {
                    MensagemErro = erro;
                    return Page();
                }
                caminhoNovaFoto = caminho;
            }

            var tarefa = new Tarefa
            {
                Id = Id,
                Titulo = Titulo.Trim(),
                Descricao = Descricao.Trim(),
                Prioridade = Prioridade,
                DataPrazo = prazoFinal,
                Observacoes = Observacoes?.Trim(),
                FotoPrincipal = caminhoNovaFoto
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucessoEdit, erroEdit) = await _tarefaService.AtualizarAsync(tarefa, ResponsaveisIds, usuarioId, ip);

            if (!sucessoEdit)
            {
                MensagemErro = erroEdit;
                return Page();
            }

            TempData["MensagemSucesso"] = "Tarefa atualizada com sucesso!";
            return RedirectToPage("/Tarefas/Detalhes", new { id = Id });
        }
    }
}
