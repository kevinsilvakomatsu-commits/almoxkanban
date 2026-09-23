using System.IO.Compression;

namespace AlmoxKanban.Services
{
    public interface IBackupService
    {
        Task<(bool Sucesso, byte[]? ArquivoZip, string? NomeArquivo, string? Erro)> GerarBackupAsync();
    }

    public class BackupService : IBackupService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public BackupService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        public async Task<(bool Sucesso, byte[]? ArquivoZip, string? NomeArquivo, string? Erro)> GerarBackupAsync()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "AlmoxKanban_Backup_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // 1. Copiar dados locais se existirem
                var dbPath = Path.Combine(_env.ContentRootPath, "Data", "tarefas.db");
                if (File.Exists(dbPath))
                {
                    var destDbDir = Path.Combine(tempDir, "Data");
                    Directory.CreateDirectory(destDbDir);
                    File.Copy(dbPath, Path.Combine(destDbDir, "tarefas.db"), true);
                }

                // 2. Copiar uploads
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                if (Directory.Exists(uploadsDir))
                {
                    var destUploadsDir = Path.Combine(tempDir, "uploads");
                    CopiarDiretorioRecursivo(uploadsDir, destUploadsDir);
                }

                // 3. Compactar em ZIP na memória
                var nomeZip = $"Backup_AlmoxKanban_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                    {
                        var relPath = Path.GetRelativePath(tempDir, file);
                        archive.CreateEntryFromFile(file, relPath, CompressionLevel.Optimal);
                    }
                }

                // Limpeza temp
                try { Directory.Delete(tempDir, true); } catch { }

                return (true, memoryStream.ToArray(), nomeZip, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Falha ao gerar backup: {ex.Message}");
            }
        }

        private static void CopiarDiretorioRecursivo(string origem, string destino)
        {
            Directory.CreateDirectory(destino);
            foreach (var file in Directory.GetFiles(origem))
            {
                var destFile = Path.Combine(destino, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var dir in Directory.GetDirectories(origem))
            {
                var destSubDir = Path.Combine(destino, Path.GetFileName(dir));
                CopiarDiretorioRecursivo(dir, destSubDir);
            }
        }
    }
}
