using AlmoxKanban.Data;
using AlmoxKanban.Models;

namespace AlmoxKanban.Services
{
    public interface IAuditoriaService
    {
        Task RegistrarAsync(int? usuarioId, string acao, string entidade, int? entidadeId, string? detalhes, string? ip);
    }

    public class AuditoriaService : IAuditoriaService
    {
        private readonly AppDbContext _context;

        public AuditoriaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(int? usuarioId, string acao, string entidade, int? entidadeId, string? detalhes, string? ip)
        {
            var registro = new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Detalhes = detalhes,
                EnderecoIP = ip,
                DataRegistro = DateTime.Now
            };

            _context.RegistrosAuditoria.Add(registro);
            await _context.SaveChangesAsync();
        }
    }
}
