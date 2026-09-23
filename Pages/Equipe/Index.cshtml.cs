using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Data;
using AlmoxKanban.Services;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Pages.Equipe
{
    public class UsuarioEquipeDto
    {
        public Usuario Usuario { get; set; } = null!;
        public int TarefasPendentes { get; set; }
        public int TarefasEmAndamento { get; set; }
        public int TarefasConcluidas { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IImportService _importService;

        public IndexModel(AppDbContext context, IImportService importService)
        {
            _context = context;
            _importService = importService;
        }

        public List<UsuarioEquipeDto> Equipe { get; set; } = new();

        [BindProperty]
        public IFormFile? PlanilhaExcel { get; set; }

        public async Task OnGetAsync()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.TarefasResponsavel).ThenInclude(tr => tr.Tarefa)
                .OrderBy(u => u.NomeCompleto)
                .ToListAsync();

            Equipe = usuarios.Select(u => new UsuarioEquipeDto
            {
                Usuario = u,
                TarefasPendentes = u.TarefasResponsavel.Count(tr => tr.Tarefa.Status == StatusTarefa.Pendente),
                TarefasEmAndamento = u.TarefasResponsavel.Count(tr => tr.Tarefa.Status == StatusTarefa.EmAndamento),
                TarefasConcluidas = u.TarefasResponsavel.Count(tr => tr.Tarefa.Status == StatusTarefa.Concluida)
            }).ToList();
        }

        public async Task<IActionResult> OnPostImportarAsync()
        {
            if (PlanilhaExcel == null || PlanilhaExcel.Length == 0)
            {
                TempData["MensagemErro"] = "Selecione uma planilha Excel (.xlsx) válida para importar.";
                return RedirectToPage();
            }

            using var stream = PlanilhaExcel.OpenReadStream();
            var (importados, erros) = await _importService.ImportarUsuariosExcelAsync(stream);

            if (importados > 0)
            {
                TempData["MensagemSucesso"] = $"{importados} usuário(s) importado(s) com sucesso!";
            }

            if (erros.Any())
            {
                TempData["MensagemErro"] = string.Join("<br/>", erros);
            }

            return RedirectToPage();
        }

        public IActionResult OnGetBaixarModelo()
        {
            var bytes = _importService.GerarPlanilhaModeloUsuarios();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo_importacao_usuarios.xlsx");
        }
    }
}
