using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class Comentario
    {
        public int Id { get; set; }

        public int TarefaId { get; set; }

        [ForeignKey("TarefaId")]
        public Tarefa Tarefa { get; set; } = null!;

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;

        [Required(ErrorMessage = "O texto do comentário é obrigatório.")]
        [StringLength(2000)]
        public string Texto { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public DateTime? DataEdicao { get; set; }

        public bool Editado { get; set; } = false;
    }
}
