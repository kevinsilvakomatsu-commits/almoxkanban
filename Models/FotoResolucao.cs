using System.ComponentModel.DataAnnotations.Schema;

namespace AlmoxKanban.Models
{
    public class FotoResolucao
    {
        public int Id { get; set; }

        public int ResolucaoId { get; set; }

        [ForeignKey("ResolucaoId")]
        public ResolucaoTarefa Resolucao { get; set; } = null!;

        public string CaminhoArquivo { get; set; } = string.Empty;

        public string NomeOriginal { get; set; } = string.Empty;

        public DateTime DataUpload { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica se esta é a foto escolhida para ser exibida como capa do card no Kanban.
        /// Apenas uma foto por resolução pode estar marcada como capa.
        /// </summary>
        public bool IsCapa { get; set; } = false;
    }
}
