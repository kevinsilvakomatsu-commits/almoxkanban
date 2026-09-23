using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class Tarefa
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        public StatusTarefa Status { get; set; } = StatusTarefa.Pendente;

        public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Normal;

        public DateTime? DataPrazo { get; set; }

        public string? FotoPrincipal { get; set; }

        [StringLength(2000)]
        public string? Observacoes { get; set; }

        public int CriadorId { get; set; }

        [ForeignKey("CriadorId")]
        public Usuario? Criador { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public DateTime DataAtualizacao { get; set; } = DateTime.Now;

        public DateTime? DataConclusao { get; set; }

        public int? UsuarioConclusaoId { get; set; }

        [ForeignKey("UsuarioConclusaoId")]
        public Usuario? UsuarioConclusao { get; set; }

        public ICollection<TarefaResponsavel> Responsaveis { get; set; } = new List<TarefaResponsavel>();
        public ICollection<FotoTarefa> Fotos { get; set; } = new List<FotoTarefa>();
        public ResolucaoTarefa? Resolucao { get; set; }
        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public ICollection<HistoricoTarefa> Historicos { get; set; } = new List<HistoricoTarefa>();

        [NotMapped]
        public bool EstaAtrasada => DataPrazo.HasValue && DataPrazo.Value < DateTime.Now && Status != StatusTarefa.Concluida && Status != StatusTarefa.Cancelada;

        [NotMapped]
        public bool PrazoHoje => DataPrazo.HasValue && DataPrazo.Value.Date == DateTime.Today && Status != StatusTarefa.Concluida && Status != StatusTarefa.Cancelada;

        [NotMapped]
        public bool SemPrazo => !DataPrazo.HasValue;

        /// <summary>
        /// Foto exibida como capa do card no Kanban. Enquanto a tarefa está em aberto
        /// (Pendente, Em Andamento ou Aguardando), mostra a foto principal registrada na
        /// criação. Assim que a tarefa é concluída, passa a mostrar a foto marcada como
        /// capa entre as evidências de resolução (sem apagar a foto original de criação,
        /// que continua disponível na tela de detalhes).
        /// </summary>
        [NotMapped]
        public string? FotoCapaAtual
        {
            get
            {
                if (Status == StatusTarefa.Concluida && Resolucao != null && Resolucao.Fotos.Any())
                {
                    var capa = Resolucao.Fotos.FirstOrDefault(f => f.IsCapa) ?? Resolucao.Fotos.First();
                    return capa.CaminhoArquivo;
                }

                return FotoPrincipal;
            }
        }
    }
}
