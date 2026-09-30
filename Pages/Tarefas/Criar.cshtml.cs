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
    public class CriarModel : PageModel
    {
        private readonly ITarefaService _tarefaService;
        private readonly IUploadService _uploadService;
        private readonly AppDbContext _context;

        public CriarModel(ITarefaService tarefaService, IUploadService uploadService, AppDbContext context)
        {
            _tarefaService = tarefaService;
            _uploadService = uploadService;
            _context = context;
        }

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
        public StatusTarefa StatusInicial { get; set; } = StatusTarefa.Pendente;

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
        public IFormFile? FotoPrincipal { get; set; }

        [BindProperty]
        public List<IFormFile>? FotosAdicionais { get; set; }

        public List<Usuario> UsuariosEquipe { get; set; } = new();
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync()
        {
            UsuariosEquipe = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.NomeCompleto).ToListAsync();

            var agora = DateTime.Now;
            DataPrazo = agora.Date.AddDays(1);
            HoraPrazo = agora.AddHours(1).ToString("HH:mm");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            UsuariosEquipe = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.NomeCompleto).ToListAsync();

            if (ResponsaveisIds == null || !ResponsaveisIds.Any())
            {
                ModelState.AddModelError("ResponsaveisIds", "É obrigatório atribuir a tarefa a pelo menos um responsável.");
            }

            if (!ModelState.IsValid) return Page();

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId))
            {
                return RedirectToPage("/Login");
            }

            // Montar data e hora do prazo
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
                    prazoFinal = prazoFinal.Value.AddHours(18); // Fim do expediente por padrão
                }
            }

            string? caminhoFotoPrincipal = null;
            if (FotoPrincipal != null)
            {
                var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(FotoPrincipal, "tarefas");
                if (!sucesso)
                {
                    MensagemErro = erro;
                    return Page();
                }
                caminhoFotoPrincipal = caminho;
            }

            var caminhosAdicionais = new List<string>();
            if (FotosAdicionais != null)
            {
                foreach (var f in FotosAdicionais)
                {
                    var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(f, "tarefas");
                    if (sucesso && caminho != null)
                    {
                        caminhosAdicionais.Add(caminho);
                    }
                }
            }

            var tarefa = new Tarefa
            {
                Titulo = Titulo.Trim(),
                Descricao = Descricao.Trim(),
                Prioridade = Prioridade,
                Status = StatusInicial,
                DataPrazo = prazoFinal,
                Observacoes = Observacoes?.Trim(),
                FotoPrincipal = caminhoFotoPrincipal
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _tarefaService.CriarAsync(tarefa, ResponsaveisIds, caminhosAdicionais, usuarioId, ip);

            TempData["MensagemSucesso"] = $"Tarefa #{tarefa.Id} '{tarefa.Titulo}' criada com sucesso!";
            return RedirectToPage("/Index");
        }
    }
}
