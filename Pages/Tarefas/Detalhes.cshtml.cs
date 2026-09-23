using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Models;
using AlmoxKanban.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AlmoxKanban.Pages.Tarefas
{
    public class DetalhesModel : PageModel
    {
        private readonly ITarefaService _tarefaService;

        public DetalhesModel(ITarefaService tarefaService)
        {
            _tarefaService = tarefaService;
        }

        public Tarefa? Tarefa { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "O comentário não pode ser vazio.")]
        public string NovoComentario { get; set; } = string.Empty;

        [BindProperty]
        public string? JustificativaReabertura { get; set; }

        [BindProperty]
        public string? MotivoCancelamento { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tarefa = await _tarefaService.ObterPorIdAsync(id);
            if (Tarefa == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostComentarAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(NovoComentario))
            {
                TempData["MensagemErro"] = "O texto do comentário é obrigatório.";
                return RedirectToPage(new { id });
            }

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId))
            {
                return RedirectToPage("/Login");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucesso, erro) = await _tarefaService.AdicionarComentarioAsync(id, NovoComentario, usuarioId, ip);

            if (!sucesso)
            {
                TempData["MensagemErro"] = erro;
            }
            else
            {
                TempData["MensagemSucesso"] = "Comentário adicionado com sucesso!";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostReabrirAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["MensagemErro"] = "Apenas administradores podem reabrir tarefas.";
                return RedirectToPage(new { id });
            }

            if (string.IsNullOrWhiteSpace(JustificativaReabertura))
            {
                TempData["MensagemErro"] = "A justificativa para reabertura é obrigatória.";
                return RedirectToPage(new { id });
            }

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucesso, erro) = await _tarefaService.ReabrirAsync(id, JustificativaReabertura, usuarioId, ip);

            if (!sucesso) TempData["MensagemErro"] = erro;
            else TempData["MensagemSucesso"] = "Tarefa reaberta com sucesso!";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCancelarAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["MensagemErro"] = "Apenas administradores podem cancelar tarefas.";
                return RedirectToPage(new { id });
            }

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucesso, erro) = await _tarefaService.CancelarAsync(id, MotivoCancelamento ?? "Cancelada pelo Administrador", usuarioId, ip);

            if (!sucesso) TempData["MensagemErro"] = erro;
            else TempData["MensagemSucesso"] = "Tarefa cancelada com sucesso.";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDefinirCapaAsync(int id, int fotoId)
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucesso, erro) = await _tarefaService.DefinirFotoCapaAsync(id, fotoId, usuarioId, ip);

            if (!sucesso) TempData["MensagemErro"] = erro;
            else TempData["MensagemSucesso"] = "Foto de capa do card atualizada com sucesso!";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostExcluirAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["MensagemErro"] = "Apenas administradores podem excluir tarefas.";
                return RedirectToPage(new { id });
            }

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out var usuarioId)) return RedirectToPage("/Login");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (sucesso, erro) = await _tarefaService.ExcluirAsync(id, usuarioId, ip);

            if (!sucesso)
            {
                TempData["MensagemErro"] = erro;
                return RedirectToPage(new { id });
            }

            TempData["MensagemSucesso"] = "Tarefa excluída definitivamente.";
            return RedirectToPage("/Index");
        }
    }
}
