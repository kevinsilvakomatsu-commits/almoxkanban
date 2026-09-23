using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Services;
using AlmoxKanban.Data;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ITarefaService _tarefaService;
        private readonly AppDbContext _context;

        public IndexModel(ITarefaService tarefaService, AppDbContext context)
        {
            _tarefaService = tarefaService;
            _context = context;
        }

        public DashboardMetricasDto Metricas { get; set; } = new();
        public List<Usuario> UsuariosEquipe { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ResponsavelId { get; set; }

        [BindProperty(SupportsGet = true)]
        public PrioridadeTarefa? Prioridade { get; set; }

        [BindProperty(SupportsGet = true)]
        public StatusTarefa? Status { get; set; }

        public async Task OnGetAsync()
        {
            UsuariosEquipe = await _context.Usuarios.Where(u => u.Ativo).OrderBy(u => u.NomeCompleto).ToListAsync();

            var filtro = new FiltroTarefaDto
            {
                DataInicio = DataInicio,
                DataFim = DataFim,
                ResponsavelId = ResponsavelId,
                Prioridade = Prioridade,
                Status = Status
            };

            Metricas = await _tarefaService.ObterMetricasDashboardAsync(filtro);
        }
    }
}
