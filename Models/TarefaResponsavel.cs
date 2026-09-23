namespace AlmoxKanban.Models
{
    public class TarefaResponsavel
    {
        public int TarefaId { get; set; }
        public Tarefa Tarefa { get; set; } = null!;

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public DateTime DataAtribuicao { get; set; } = DateTime.Now;
    }
}
