using AlmoxKanban.Data;
using AlmoxKanban.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Services
{
    public interface IUploadService
    {
        Task<(bool Sucesso, string? CaminhoRelativo, string? Erro)> SalvarFotoAsync(IFormFile arquivo, string subpasta);
        void ExcluirArquivo(string? caminhoRelativo);
    }

    public class UploadService : IUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly string[] _extensoesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long TamanhoMaximoBytes = 10 * 1024 * 1024; // 10MB

        public UploadService(IWebHostEnvironment env, AppDbContext context)
        {
            _env = env;
            _context = context;
        }

        public async Task<(bool Sucesso, string? CaminhoRelativo, string? Erro)> SalvarFotoAsync(IFormFile arquivo, string subpasta)
        {
            if (arquivo == null || arquivo.Length == 0)
                return (false, null, "Arquivo não selecionado ou vazio.");

            if (arquivo.Length > TamanhoMaximoBytes)
                return (false, null, "O tamanho do arquivo excede o limite máximo permitido de 10MB.");

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (!_extensoesPermitidas.Contains(extensao))
                return (false, null, $"Formato de imagem não permitido ({extensao}). Envie JPG, PNG ou WEBP.");

            var pastaDestino = Path.Combine(_env.WebRootPath, "uploads", subpasta);
            if (!Directory.Exists(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino);
            }

            var nomeUnico = $"{Guid.NewGuid():N}{extensao}";
            var caminhoFisico = Path.Combine(pastaDestino, nomeUnico);

            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                await arquivo.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            // Salvar no disco local
            await File.WriteAllBytesAsync(caminhoFisico, bytes);

            var caminhoRelativo = $"/uploads/{subpasta}/{nomeUnico}";

            // Persistir também no banco de dados para nunca perder em reinicializações da nuvem (Render)
            try
            {
                var contentType = extensao switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                var registro = new ArquivoUpload
                {
                    CaminhoRelativo = caminhoRelativo,
                    Conteudo = bytes,
                    ContentType = contentType,
                    DataUpload = DateTime.Now
                };

                _context.ArquivosUpload.Add(registro);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UploadService] Aviso ao persistir imagem no banco: {ex.Message}");
            }

            return (true, caminhoRelativo, null);
        }

        public void ExcluirArquivo(string? caminhoRelativo)
        {
            if (string.IsNullOrWhiteSpace(caminhoRelativo)) return;

            var caminhoLimpo = caminhoRelativo.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var caminhoFisico = Path.Combine(_env.WebRootPath, caminhoLimpo);

            if (File.Exists(caminhoFisico))
            {
                try
                {
                    File.Delete(caminhoFisico);
                }
                catch
                {
                    // Evita falha crítica ao deletar arquivo em uso
                }
            }

            try
            {
                var registro = _context.ArquivosUpload.FirstOrDefault(a => a.CaminhoRelativo == caminhoRelativo);
                if (registro != null)
                {
                    _context.ArquivosUpload.Remove(registro);
                    _context.SaveChanges();
                }
            }
            catch { }
        }
    }
}