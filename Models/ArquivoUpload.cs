using System.ComponentModel.DataAnnotations;

namespace AlmoxKanban.Models
{
    public class ArquivoUpload
    {
        [Key]
        [StringLength(300)]
        public string CaminhoRelativo { get; set; } = string.Empty;

        public byte[] Conteudo { get; set; } = Array.Empty<byte>();

        [StringLength(100)]
        public string ContentType { get; set; } = "image/png";

        public DateTime DataUpload { get; set; } = DateTime.Now;
    }
}