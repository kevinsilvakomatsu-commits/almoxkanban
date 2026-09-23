using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AlmoxKanban.Services;
using AlmoxKanban.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlmoxKanban.Pages.Backup
{
    public class IndexModel : PageModel
    {
        private readonly IBackupService _backupService;
        private readonly IExportService _exportService;
        private readonly ITarefaService _tarefaService;
        private readonly IAuditoriaService _auditoria;
        private readonly AppDbContext _context;

        public IndexModel(IBackupService backupService, IExportService exportService, ITarefaService tarefaService, IAuditoriaService auditoria, AppDbContext context)
        {
            _backupService = backupService;
            _exportService = exportService;
            _tarefaService = tarefaService;
            _auditoria = auditoria;
            _context = context;
        }

        public int TotalTarefas { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalFotos { get; set; }
        public string? MensagemErro { get; set; }

        public async Task OnGetAsync()
        {
            TotalTarefas = await _context.Tarefas.CountAsync();
            TotalUsuarios = await _context.Usuarios.CountAsync();
            TotalFotos = await _context.FotosTarefa.CountAsync() + await _context.FotosResolucao.CountAsync();
        }

        public async Task<IActionResult> OnGetGerarBackupAsync()
        {
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminIdStr, out var adminId);

            var (sucesso, arquivoZip, nomeArquivo, erro) = await _backupService.GerarBackupAsync();

            if (!sucesso || arquivoZip == null)
            {
                TempData["MensagemErro"] = erro ?? "Erro desconhecido ao gerar o arquivo de backup.";
                return RedirectToPage();
            }

            await _auditoria.RegistrarAsync(adminId, "Backup", "Sistema", null, $"Backup completo gerado: {nomeArquivo}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return File(arquivoZip, "application/zip", nomeArquivo ?? "Backup_AlmoxKanban.zip");
        }

        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var tarefas = await _tarefaService.ObterTarefasAsync();
            var bytes = _exportService.ExportarTarefasExcel(tarefas);

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(adminIdStr, out var adminId);
            await _auditoria.RegistrarAsync(adminId, "ExportarExcel", "Tarefa", null, "Exportação de tarefas para Excel", HttpContext.Connection.RemoteIpAddress?.ToString());

            var nomeArquivo = $"Tarefas_Almoxarifado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
        }
    }
}
