using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class FotoTarefa
    {
        public int Id { get; set; }

        public int TarefaId { get; set; }

        [ForeignKey("TarefaId")]
        public Tarefa Tarefa { get; set; } = null!;

        public string CaminhoArquivo { get; set; } = string.Empty;

        public string NomeOriginal { get; set; } = string.Empty;

        public DateTime DataUpload { get; set; } = DateTime.Now;
    }
}
