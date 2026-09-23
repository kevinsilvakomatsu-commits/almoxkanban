using ClosedXML.Excel;
using AlmoxKanban.Data;
using AlmoxKanban.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Services
{
    public interface IImportService
    {
        Task<(int Importados, List<string> Erros)> ImportarUsuariosExcelAsync(Stream stream);
        byte[] GerarPlanilhaModeloUsuarios();
    }

    public class ImportService : IImportService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public ImportService(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<(int Importados, List<string> Erros)> ImportarUsuariosExcelAsync(Stream stream)
        {
            var erros = new List<string>();
            int importados = 0;

            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // pular cabeçalho

                var usuariosExistentes = await _context.Usuarios
                    .Select(u => u.NomeUsuario.ToLower())
                    .ToListAsync();

                int linha = 1;
                foreach (var row in rows)
                {
                    linha++;
                    var nome = row.Cell(1).GetString().Trim();
                    var usuario = row.Cell(2).GetString().Trim();
                    var funcao = row.Cell(3).GetString().Trim();
                    var senhaInicial = row.Cell(4).GetString().Trim();
                    var ativoStr = row.Cell(5).GetString().Trim().ToLower();

                    if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(usuario))
                    {
                        erros.Add($"Linha {linha}: Nome e Usuário são obrigatórios.");
                        continue;
                    }

                    if (usuariosExistentes.Contains(usuario.ToLower()))
                    {
                        erros.Add($"Linha {linha}: Usuário '{usuario}' já existe no sistema.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(senhaInicial) || senhaInicial.Length < 6)
                    {
                        senhaInicial = "Mudar@123"; // Senha padrão se não informada
                    }

                    bool ativo = true;
                    if (ativoStr == "nao" || ativoStr == "não" || ativoStr == "false" || ativoStr == "0" || ativoStr == "inativo")
                    {
                        ativo = false;
                    }

                    var novoUsuario = new Usuario
                    {
                        NomeCompleto = nome,
                        NomeUsuario = usuario,
                        Funcao = string.IsNullOrWhiteSpace(funcao) ? "Operador de Almoxarifado" : funcao,
                        SenhaHash = _authService.GerarHash(senhaInicial),
                        Ativo = ativo,
                        IsAdmin = false,
                        DeveTrocarSenha = true,
                        DataCriacao = DateTime.Now,
                        DataAtualizacao = DateTime.Now
                    };

                    _context.Usuarios.Add(novoUsuario);
                    usuariosExistentes.Add(usuario.ToLower());
                    importados++;
                }

                if (importados > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                erros.Add($"Erro ao processar o arquivo Excel: {ex.Message}");
            }

            return (importados, erros);
        }

        public byte[] GerarPlanilhaModeloUsuarios()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Modelo_Usuarios");

            ws.Cell(1, 1).Value = "Nome";
            ws.Cell(1, 2).Value = "Usuário";
            ws.Cell(1, 3).Value = "Função";
            ws.Cell(1, 4).Value = "SenhaInicial";
            ws.Cell(1, 5).Value = "Ativo";

            var header = ws.Range(1, 1, 1, 5);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0284C7");
            header.Style.Font.FontColor = XLColor.White;

            // Exemplos
            ws.Cell(2, 1).Value = "Carlos Oliveira";
            ws.Cell(2, 2).Value = "carlos.oliveira";
            ws.Cell(2, 3).Value = "Conferente de Carga";
            ws.Cell(2, 4).Value = "Almox@2026";
            ws.Cell(2, 5).Value = "Sim";

            ws.Cell(3, 1).Value = "Mariana Souza";
            ws.Cell(3, 2).Value = "mariana.souza";
            ws.Cell(3, 3).Value = "Operadora de Empilhadeira";
            ws.Cell(3, 4).Value = "Almox@2026";
            ws.Cell(3, 5).Value = "Sim";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
