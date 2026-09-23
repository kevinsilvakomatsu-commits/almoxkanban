using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Data;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Pages.Equipe
{
    public class PerfilModel : PageModel
    {
        private readonly AppDbContext _context;

        public PerfilModel(AppDbContext context)
        {
            _context = context;
        }

        public Usuario? Usuario { get; set; }
        public List<Tarefa> TarefasAtuais { get; set; } = new();
        public List<Tarefa> TarefasConcluidas { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Usuario = await _context.Usuarios
                .Include(u => u.TarefasResponsavel).ThenInclude(tr => tr.Tarefa)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (Usuario == null) return NotFound();

            var todas = Usuario.TarefasResponsavel.Select(tr => tr.Tarefa).ToList();

            TarefasAtuais = todas.Where(t => t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada)
                .OrderBy(t => t.DataPrazo ?? DateTime.MaxValue)
                .ToList();

            TarefasConcluidas = todas.Where(t => t.Status == StatusTarefa.Concluida)
                .OrderByDescending(t => t.DataConclusao ?? t.DataAtualizacao)
                .ToList();

            return Page();
        }
    }
}
