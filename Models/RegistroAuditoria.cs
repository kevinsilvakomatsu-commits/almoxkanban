using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class RegistroAuditoria
    {
        public int Id { get; set; }

        public int? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public string Acao { get; set; } = string.Empty;

        public string Entidade { get; set; } = string.Empty;

        public int? EntidadeId { get; set; }

        public string? Detalhes { get; set; }

        public DateTime DataRegistro { get; set; } = DateTime.Now;

        public string? EnderecoIP { get; set; }
    }
}
