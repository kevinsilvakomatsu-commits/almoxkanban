using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class HistoricoTarefa
    {
        public int Id { get; set; }

        public int TarefaId { get; set; }

        [ForeignKey("TarefaId")]
        public Tarefa Tarefa { get; set; } = null!;

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;

        public string TipoAlteracao { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string? StatusAnterior { get; set; }

        public string? StatusNovo { get; set; }

        public DateTime DataRegistro { get; set; } = DateTime.Now;
    }
}
