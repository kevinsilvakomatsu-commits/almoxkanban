using ClosedXML.Excel;
using AlmoxKanban.Models;

namespace AlmoxKanban.Services
{
    public interface IExportService
    {
        byte[] ExportarTarefasExcel(IEnumerable<Tarefa> tarefas);
    }

    public class ExportService : IExportService
    {
        public byte[] ExportarTarefasExcel(IEnumerable<Tarefa> tarefas)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tarefas");

            // Cabeçalhos
            var headers = new string[]
            {
                "ID", "Título", "Status", "Prioridade", "Responsáveis", "Criador",
                "Data Criação", "Prazo", "Data Conclusão", "Concluído Por",
                "Cumprimento do Prazo", "Descrição"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var t in tarefas)
            {
                worksheet.Cell(row, 1).Value = t.Id;
                worksheet.Cell(row, 2).Value = t.Titulo;
                worksheet.Cell(row, 3).Value = t.Status.ToString();
                worksheet.Cell(row, 4).Value = t.Prioridade.ToString();
                worksheet.Cell(row, 5).Value = string.Join(", ", t.Responsaveis.Select(r => r.Usuario.NomeCompleto));
                worksheet.Cell(row, 6).Value = t.Criador?.NomeCompleto ?? "N/A";
                worksheet.Cell(row, 7).Value = t.DataCriacao.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 8).Value = t.DataPrazo.HasValue ? t.DataPrazo.Value.ToString("dd/MM/yyyy HH:mm") : "Sem Prazo";
                worksheet.Cell(row, 9).Value = t.DataConclusao.HasValue ? t.DataConclusao.Value.ToString("dd/MM/yyyy HH:mm") : "-";
                worksheet.Cell(row, 10).Value = t.UsuarioConclusao?.NomeCompleto ?? "-";

                string cumprimento;
                if (t.Status == StatusTarefa.Concluida)
                {
                    if (!t.DataPrazo.HasValue) cumprimento = "Concluído (Sem Prazo)";
                    else if (t.DataConclusao <= t.DataPrazo) cumprimento = "Dentro do Prazo";
                    else cumprimento = "Fora do Prazo";
                }
                else
                {
                    if (!t.DataPrazo.HasValue) cumprimento = "Em Aberto (Sem Prazo)";
                    else if (DateTime.Now > t.DataPrazo) cumprimento = "Atrasado";
                    else cumprimento = "No Prazo";
                }

                worksheet.Cell(row, 11).Value = cumprimento;
                worksheet.Cell(row, 12).Value = t.Descricao;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
