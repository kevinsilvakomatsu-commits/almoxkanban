using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlmoxKanban.Pages.Tarefas
{
    public class ConcluirModel : PageModel
    {
        private readonly ITarefaService _tarefaService;
        private readonly IUploadService _uploadService;

        public ConcluirModel(ITarefaService tarefaService, IUploadService uploadService)
        {
            _tarefaService = tarefaService;
            _uploadService = uploadService;
        }

        public Tarefa? Tarefa { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A descrição da solução é obrigatória.")]
        [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
        public string DescricaoSolucao { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "É obrigatório anexar pelo menos uma foto comprovando a tarefa resolvida.")]
        public List<IFormFile> FotosResolucao { get; set; } = new();

        public string? MensagemErro { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tarefa = await _tarefaService.ObterPorIdAsync(id);
            if (Tarefa == null) return NotFound();

            if (Tarefa.Status == StatusTarefa.Concluida)
            {
                TempData["MensagemErro"] = "Esta tarefa já se encontra concluída.";
                return RedirectToPage("/Tarefas/Detalhes", new { id });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Tarefa = await _tarefaService.ObterPorIdAsync(id);
            if (Tarefa == null) return NotFound();

            if (FotosResolucao == null || !FotosResolucao.Any() || FotosResolucao.All(f => f.Length == 0))
            {
                ModelState.AddModelError("FotosResolucao", "É obrigatório anexar pelo menos uma foto como evidência da conclusão.");
            }

            if (!ModelState.IsValid) return Page();

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId))
            {
                return RedirectToPage("/Login");
            }

            // Salvar fotos de resolução
            var caminhosFotos = new List<string>();
            foreach (var foto in FotosResolucao)
            {
                if (foto.Length > 0)
                {
                    var (sucesso, caminho, erro) = await _uploadService.SalvarFotoAsync(foto, "resolucoes");
                    if (!sucesso)
                    {
                        MensagemErro = erro;
                        return Page();
                    }
                    if (caminho != null)
                    {
                        caminhosFotos.Add(caminho);
                    }
                }
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (concluido, erroConclusao) = await _tarefaService.ConcluirAsync(id, DescricaoSolucao, caminhosFotos, usuarioId, ip);

            if (!concluido)
            {
                MensagemErro = erroConclusao;
                return Page();
            }

            TempData["MensagemSucesso"] = "Parabéns! Tarefa concluída com sucesso e evidência registrada!";
            return RedirectToPage("/Tarefas/Detalhes", new { id });
        }
    }
}
