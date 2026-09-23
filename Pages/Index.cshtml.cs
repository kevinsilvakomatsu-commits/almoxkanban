using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Services;
using AlmoxKanban.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlmoxKanban.Pages
{
    public class MoverStatusRequest
    {
        public int TarefaId { get; set; }
        public string NovoStatus { get; set; } = string.Empty;
    }

    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ITarefaService _tarefaService;
        private readonly AppDbContext _context;

        public IndexModel(ITarefaService tarefaService, AppDbContext context)
        {
            _tarefaService = tarefaService;
            _context = context;
        }

        public List<Tarefa> Pendentes { get; set; } = new();
        public List<Tarefa> EmAndamento { get; set; } = new();
        public List<Tarefa> Aguardando { get; set; } = new();
        public List<Tarefa> Concluidas { get; set; } = new();

        public List<Usuario> UsuariosEquipe { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Termo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ResponsavelId { get; set; }

        [BindProperty(SupportsGet = true)]
        public StatusTarefa? StatusFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public PrioridadeTarefa? PrioridadeFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SomenteAtrasadas { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool PrazoHoje { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SemPrazo { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool MinhasTarefas { get; set; }

        public async Task OnGetAsync()
        {
            UsuariosEquipe = await _context.Usuarios
                .Where(u => u.Ativo)
                .OrderBy(u => u.NomeCompleto)
                .ToListAsync();

            var filtro = new FiltroTarefaDto
            {
                Termo = Termo,
                ResponsavelId = ResponsavelId,
                Status = StatusFiltro,
                Prioridade = PrioridadeFiltro,
                SomenteAtrasadas = SomenteAtrasadas,
                PrazoHoje = PrazoHoje,
                SemPrazo = SemPrazo
            };

            if (MinhasTarefas)
            {
                var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(usuarioIdStr, out var uId))
                {
                    filtro.SomenteDoUsuarioId = uId;
                }
            }

            var todas = await _tarefaService.ObterTarefasAsync(filtro);

            Pendentes = todas.Where(t => t.Status == StatusTarefa.Pendente).ToList();
            EmAndamento = todas.Where(t => t.Status == StatusTarefa.EmAndamento).ToList();
            Aguardando = todas.Where(t => t.Status == StatusTarefa.Aguardando).ToList();
            Concluidas = todas.Where(t => t.Status == StatusTarefa.Concluida).ToList();
        }

        public async Task<IActionResult> OnPostMoverStatusAsync([FromBody] MoverStatusRequest? request)
        {
            if (request == null)
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(body))
                    {
                        request = System.Text.Json.JsonSerializer.Deserialize<MoverStatusRequest>(body, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                }
                catch { }
            }

            if (request == null || !Enum.TryParse<StatusTarefa>(request.NovoStatus, out var statusEnum))
            {
                return new JsonResult(new
                {
                    sucesso = false,
                    erro = "Status inválido."
                });
            }

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(usuarioIdStr, out var usuarioId))
            {
                return new JsonResult(new
                {
                    sucesso = false,
                    erro = "Sessão expirada."
                });
            }

            var isAdmin = User.IsInRole("Admin");
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (sucesso, erro) = await _tarefaService.MoverStatusAsync(
                request.TarefaId,
                statusEnum,
                usuarioId,
                isAdmin,
                ip);

            if (!sucesso)
            {
                return new JsonResult(new
                {
                    sucesso = false,
                    erro
                });
            }

            return new JsonResult(new
            {
                sucesso = true,
                mensagem = "Status atualizado com sucesso!"
            });
        }
    }
}
