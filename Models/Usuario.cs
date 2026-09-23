using System.ComponentModel.DataAnnotations;

namespace AlmoxKanban.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(50, ErrorMessage = "O usuário deve ter no máximo 50 caracteres.")]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string Funcao { get; set; } = string.Empty;

        public string? FotoPerfil { get; set; }

        public bool Ativo { get; set; } = true;

        public bool IsAdmin { get; set; } = false;

        public bool DeveTrocarSenha { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public DateTime DataAtualizacao { get; set; } = DateTime.Now;

        public ICollection<TarefaResponsavel> TarefasResponsavel { get; set; } = new List<TarefaResponsavel>();
        public ICollection<Tarefa> TarefasCriadas { get; set; } = new List<Tarefa>();
        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public ICollection<HistoricoTarefa> Historicos { get; set; } = new List<HistoricoTarefa>();
    }
}
