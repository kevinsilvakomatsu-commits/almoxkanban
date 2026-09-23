using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class ResolucaoTarefa
    {
        public int Id { get; set; }

        public int TarefaId { get; set; }

        [ForeignKey("TarefaId")]
        public Tarefa Tarefa { get; set; } = null!;

        [Required(ErrorMessage = "A descrição da resolução é obrigatória.")]
        [StringLength(2000)]
        public string Descricao { get; set; } = string.Empty;

        public int UsuarioConclusaoId { get; set; }

        [ForeignKey("UsuarioConclusaoId")]
        public Usuario UsuarioConclusao { get; set; } = null!;

        public DateTime DataConclusao { get; set; } = DateTime.Now;

        public ICollection<FotoResolucao> Fotos { get; set; } = new List<FotoResolucao>();
    }
}
