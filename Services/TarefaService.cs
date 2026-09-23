using AlmoxKanban.Data;
using AlmoxKanban.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmoxKanban.Services
{
    public class FiltroTarefaDto
    {
        public string? Termo { get; set; }
        public int? ResponsavelId { get; set; }
        public StatusTarefa? Status { get; set; }
        public PrioridadeTarefa? Prioridade { get; set; }
        public bool? SomenteAtrasadas { get; set; }
        public bool? PrazoHoje { get; set; }
        public bool? SemPrazo { get; set; }
        public int? SomenteDoUsuarioId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class DashboardMetricasDto
    {
        public int TotalCriadas { get; set; }
        public int TotalPendentes { get; set; }
        public int TotalEmAndamento { get; set; }
        public int TotalAguardando { get; set; }
        public int TotalConcluidas { get; set; }
        public int TotalCanceladas { get; set; }
        public int TotalAtrasadas { get; set; }
        public int TotalConcluidasNoPrazo { get; set; }
        public int TotalConcluidasForaPrazo { get; set; }
        public int TotalSemPrazo { get; set; }
        public double PercentualConclusao { get; set; }
        public double MediaHorasConclusao { get; set; }
        public int CriadasNoPeriodo { get; set; }
        public int ConcluidasNoPeriodo { get; set; }
        public Dictionary<string, int> PorStatus { get; set; } = new();
        public Dictionary<string, int> PorPrioridade { get; set; } = new();
        public Dictionary<string, int> PorResponsavel { get; set; } = new();
        public Dictionary<string, int> ConclusaoPrazo { get; set; } = new();
    }

    public interface ITarefaService
    {
        Task<List<Tarefa>> ObterTarefasAsync(FiltroTarefaDto? filtro = null);
        Task<Tarefa?> ObterPorIdAsync(int id);
        Task<Tarefa> CriarAsync(Tarefa tarefa, List<int> responsaveisIds, List<string> fotosAdicionais, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> AtualizarAsync(Tarefa tarefa, List<int> responsaveisIds, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> MoverStatusAsync(int tarefaId, StatusTarefa novoStatus, int usuarioId, bool isAdmin, string? ip);
        Task<(bool Sucesso, string? Erro)> ConcluirAsync(int tarefaId, string descricaoResolucao, List<string> caminhosFotos, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> ReabrirAsync(int tarefaId, string justificativa, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> CancelarAsync(int tarefaId, string motivo, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> ExcluirAsync(int tarefaId, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> AdicionarComentarioAsync(int tarefaId, string texto, int usuarioId, string? ip);
        Task<(bool Sucesso, string? Erro)> DefinirFotoCapaAsync(int tarefaId, int fotoResolucaoId, int usuarioId, string? ip);
        Task<DashboardMetricasDto> ObterMetricasDashboardAsync(FiltroTarefaDto? filtro = null);
    }

    public class TarefaService : ITarefaService
    {
        private readonly AppDbContext _context;
        private readonly IUploadService _uploadService;
        private readonly IAuditoriaService _auditoria;

        public TarefaService(AppDbContext context, IUploadService uploadService, IAuditoriaService auditoria)
        {
            _context = context;
            _uploadService = uploadService;
            _auditoria = auditoria;
        }

        public async Task<List<Tarefa>> ObterTarefasAsync(FiltroTarefaDto? filtro = null)
        {
            var query = _context.Tarefas
                .Include(t => t.Criador)
                .Include(t => t.UsuarioConclusao)
                .Include(t => t.Responsaveis).ThenInclude(r => r.Usuario)
                .Include(t => t.Fotos)
                .Include(t => t.Resolucao).ThenInclude(r => r.Fotos)
                .Include(t => t.Comentarios)
                .AsQueryable();

            if (filtro != null)
            {
                if (!string.IsNullOrWhiteSpace(filtro.Termo))
                {
                    var termo = filtro.Termo.ToLower().Trim();
                    query = query.Where(t => t.Titulo.ToLower().Contains(termo) || t.Descricao.ToLower().Contains(termo));
                }

                if (filtro.ResponsavelId.HasValue)
                {
                    query = query.Where(t => t.Responsaveis.Any(r => r.UsuarioId == filtro.ResponsavelId.Value));
                }

                if (filtro.Status.HasValue)
                {
                    query = query.Where(t => t.Status == filtro.Status.Value);
                }

                if (filtro.Prioridade.HasValue)
                {
                    query = query.Where(t => t.Prioridade == filtro.Prioridade.Value);
                }

                if (filtro.SomenteAtrasadas == true)
                {
                    var agora = DateTime.Now;
                    query = query.Where(t => t.DataPrazo.HasValue && t.DataPrazo.Value < agora && t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada);
                }

                if (filtro.PrazoHoje == true)
                {
                    var hoje = DateTime.Today;
                    var amanha = hoje.AddDays(1);
                    query = query.Where(t => t.DataPrazo.HasValue && t.DataPrazo.Value >= hoje && t.DataPrazo.Value < amanha && t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada);
                }

                if (filtro.SemPrazo == true)
                {
                    query = query.Where(t => !t.DataPrazo.HasValue);
                }

                if (filtro.SomenteDoUsuarioId.HasValue)
                {
                    query = query.Where(t => t.Responsaveis.Any(r => r.UsuarioId == filtro.SomenteDoUsuarioId.Value));
                }

                if (filtro.DataInicio.HasValue)
                {
                    query = query.Where(t => t.DataCriacao >= filtro.DataInicio.Value);
                }

                if (filtro.DataFim.HasValue)
                {
                    var fim = filtro.DataFim.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.DataCriacao <= fim);
                }
            }

            return await query.OrderByDescending(t => t.Prioridade).ThenBy(t => t.DataPrazo ?? DateTime.MaxValue).ToListAsync();
        }

        public async Task<Tarefa?> ObterPorIdAsync(int id)
        {
            return await _context.Tarefas
                .Include(t => t.Criador)
                .Include(t => t.UsuarioConclusao)
                .Include(t => t.Responsaveis).ThenInclude(r => r.Usuario)
                .Include(t => t.Fotos)
                .Include(t => t.Resolucao).ThenInclude(r => r!.UsuarioConclusao)
                .Include(t => t.Resolucao).ThenInclude(r => r!.Fotos)
                .Include(t => t.Comentarios).ThenInclude(c => c.Usuario)
                .Include(t => t.Historicos).ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tarefa> CriarAsync(Tarefa tarefa, List<int> responsaveisIds, List<string> fotosAdicionais, int usuarioId, string? ip)
        {
            tarefa.CriadorId = usuarioId;
            tarefa.DataCriacao = DateTime.Now;
            tarefa.DataAtualizacao = DateTime.Now;

            foreach (var respId in responsaveisIds.Distinct())
            {
                tarefa.Responsaveis.Add(new TarefaResponsavel
                {
                    UsuarioId = respId,
                    DataAtribuicao = DateTime.Now
                });
            }

            foreach (var foto in fotosAdicionais)
            {
                tarefa.Fotos.Add(new FotoTarefa
                {
                    CaminhoArquivo = foto,
                    NomeOriginal = Path.GetFileName(foto),
                    DataUpload = DateTime.Now
                });
            }

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                UsuarioId = usuarioId,
                TipoAlteracao = "Criação",
                Descricao = $"Tarefa criada com status inicial '{tarefa.Status}'.",
                StatusNovo = tarefa.Status.ToString(),
                DataRegistro = DateTime.Now
            });

            _context.Tarefas.Add(tarefa);
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(usuarioId, "Criar", "Tarefa", tarefa.Id, $"Tarefa '{tarefa.Titulo}' criada.", ip);

            return tarefa;
        }

        public async Task<(bool Sucesso, string? Erro)> AtualizarAsync(Tarefa tarefa, List<int> responsaveisIds, int usuarioId, string? ip)
        {
            var existente = await _context.Tarefas
                .Include(t => t.Responsaveis)
                .FirstOrDefaultAsync(t => t.Id == tarefa.Id);

            if (existente == null) return (false, "Tarefa não encontrada.");

            existente.Titulo = tarefa.Titulo;
            existente.Descricao = tarefa.Descricao;
            existente.Prioridade = tarefa.Prioridade;
            existente.DataPrazo = tarefa.DataPrazo;
            existente.Observacoes = tarefa.Observacoes;
            existente.DataAtualizacao = DateTime.Now;

            if (!string.IsNullOrEmpty(tarefa.FotoPrincipal))
            {
                existente.FotoPrincipal = tarefa.FotoPrincipal;
            }

            // Atualizar responsáveis
            _context.TarefaResponsaveis.RemoveRange(existente.Responsaveis);
            foreach (var respId in responsaveisIds.Distinct())
            {
                existente.Responsaveis.Add(new TarefaResponsavel
                {
                    TarefaId = existente.Id,
                    UsuarioId = respId,
                    DataAtribuicao = DateTime.Now
                });
            }

            existente.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = existente.Id,
                UsuarioId = usuarioId,
                TipoAlteracao = "Edição",
                Descricao = "Dados da tarefa atualizados.",
                DataRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Editar", "Tarefa", existente.Id, $"Tarefa '{existente.Titulo}' editada.", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> MoverStatusAsync(int tarefaId, StatusTarefa novoStatus, int usuarioId, bool isAdmin, string? ip)
        {
            var tarefa = await _context.Tarefas
                .Include(t => t.Responsaveis)
                .FirstOrDefaultAsync(t => t.Id == tarefaId);

            if (tarefa == null) return (false, "Tarefa não encontrada.");

            // Validação de permissão: admin pode tudo; usuário normal deve ser responsável
            if (!isAdmin && !tarefa.Responsaveis.Any(r => r.UsuarioId == usuarioId))
            {
                return (false, "Você não tem permissão para movimentar esta tarefa porque não está atribuído a ela.");
            }

            // Para concluir pelo Kanban, se exigir evidência fotográfica, deve orientar a usar o fluxo de conclusão
            if (novoStatus == StatusTarefa.Concluida && tarefa.Status != StatusTarefa.Concluida)
            {
                var temResolucao = await _context.Resolucoes.AnyAsync(r => r.TarefaId == tarefaId);
                if (!temResolucao)
                {
                    return (false, "Para concluir esta tarefa é obrigatório registrar a foto da resolução e uma breve descrição. Abra a tarefa e clique em 'Concluir Tarefa'.");
                }
            }

            var statusAnterior = tarefa.Status;
            tarefa.Status = novoStatus;
            tarefa.DataAtualizacao = DateTime.Now;

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = tarefa.Id,
                UsuarioId = usuarioId,
                TipoAlteracao = "Movimentação",
                Descricao = $"Status alterado de '{statusAnterior}' para '{novoStatus}'.",
                StatusAnterior = statusAnterior.ToString(),
                StatusNovo = novoStatus.ToString(),
                DataRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Movimentar", "Tarefa", tarefa.Id, $"Status alterado de {statusAnterior} para {novoStatus}", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> ConcluirAsync(int tarefaId, string descricaoResolucao, List<string> caminhosFotos, int usuarioId, string? ip)
        {
            if (string.IsNullOrWhiteSpace(descricaoResolucao))
                return (false, "A descrição da solução/resolução é obrigatória.");

            if (caminhosFotos == null || !caminhosFotos.Any())
                return (false, "É obrigatório anexar pelo menos uma foto comprovando a tarefa resolvida.");

            var tarefa = await _context.Tarefas
                .Include(t => t.Resolucao).ThenInclude(r => r!.Fotos)
                .FirstOrDefaultAsync(t => t.Id == tarefaId);

            if (tarefa == null) return (false, "Tarefa não encontrada.");

            var statusAnterior = tarefa.Status;
            var agora = DateTime.Now;

            var resolucao = new ResolucaoTarefa
            {
                TarefaId = tarefa.Id,
                Descricao = descricaoResolucao.Trim(),
                UsuarioConclusaoId = usuarioId,
                DataConclusao = agora
            };

            for (int i = 0; i < caminhosFotos.Count; i++)
            {
                resolucao.Fotos.Add(new FotoResolucao
                {
                    CaminhoArquivo = caminhosFotos[i],
                    NomeOriginal = Path.GetFileName(caminhosFotos[i]),
                    DataUpload = agora,
                    IsCapa = i == 0
                });
            }

            tarefa.Resolucao = resolucao;
            tarefa.Status = StatusTarefa.Concluida;
            tarefa.DataConclusao = agora;
            tarefa.UsuarioConclusaoId = usuarioId;
            tarefa.DataAtualizacao = agora;

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = tarefa.Id,
                UsuarioId = usuarioId,
                TipoAlteracao = "Conclusão",
                Descricao = $"Tarefa concluída com foto e evidência. Solução: {descricaoResolucao.Trim()}",
                StatusAnterior = statusAnterior.ToString(),
                StatusNovo = StatusTarefa.Concluida.ToString(),
                DataRegistro = agora
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Concluir", "Tarefa", tarefa.Id, "Tarefa concluída com evidência fotográfica.", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> ReabrirAsync(int tarefaId, string justificativa, int usuarioId, string? ip)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
                return (false, "A justificativa para reabertura da tarefa é obrigatória.");

            var tarefa = await _context.Tarefas.FirstOrDefaultAsync(t => t.Id == tarefaId);
            if (tarefa == null) return (false, "Tarefa não encontrada.");

            var statusAnterior = tarefa.Status;
            tarefa.Status = StatusTarefa.EmAndamento;
            tarefa.DataConclusao = null;
            tarefa.UsuarioConclusaoId = null;
            tarefa.DataAtualizacao = DateTime.Now;

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = tarefa.Id,
                UsuarioId = usuarioId,
                TipoAlteracao = "Reabertura",
                Descricao = $"Tarefa reaberta. Justificativa: {justificativa.Trim()}",
                StatusAnterior = statusAnterior.ToString(),
                StatusNovo = StatusTarefa.EmAndamento.ToString(),
                DataRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Reabrir", "Tarefa", tarefa.Id, $"Justificativa: {justificativa}", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> CancelarAsync(int tarefaId, string motivo, int usuarioId, string? ip)
        {
            var tarefa = await _context.Tarefas.FirstOrDefaultAsync(t => t.Id == tarefaId);
            if (tarefa == null) return (false, "Tarefa não encontrada.");

            var statusAnterior = tarefa.Status;
            tarefa.Status = StatusTarefa.Cancelada;
            tarefa.DataAtualizacao = DateTime.Now;

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = tarefa.Id,
                UsuarioId = usuarioId,
                TipoAlteracao = "Cancelamento",
                Descricao = $"Tarefa cancelada. Motivo: {motivo?.Trim() ?? "Não informado"}",
                StatusAnterior = statusAnterior.ToString(),
                StatusNovo = StatusTarefa.Cancelada.ToString(),
                DataRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Cancelar", "Tarefa", tarefa.Id, $"Motivo: {motivo}", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> ExcluirAsync(int tarefaId, int usuarioId, string? ip)
        {
            var tarefa = await _context.Tarefas
                .Include(t => t.Fotos)
                .Include(t => t.Resolucao).ThenInclude(r => r!.Fotos)
                .FirstOrDefaultAsync(t => t.Id == tarefaId);

            if (tarefa == null) return (false, "Tarefa não encontrada.");

            // Excluir arquivos físicos com segurança
            if (!string.IsNullOrEmpty(tarefa.FotoPrincipal))
                _uploadService.ExcluirArquivo(tarefa.FotoPrincipal);

            foreach (var f in tarefa.Fotos)
                _uploadService.ExcluirArquivo(f.CaminhoArquivo);

            if (tarefa.Resolucao != null)
            {
                foreach (var f in tarefa.Resolucao.Fotos)
                    _uploadService.ExcluirArquivo(f.CaminhoArquivo);
            }

            var titulo = tarefa.Titulo;
            _context.Tarefas.Remove(tarefa);
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync(usuarioId, "Excluir", "Tarefa", tarefaId, $"Tarefa '{titulo}' excluída definitivamente.", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> AdicionarComentarioAsync(int tarefaId, string texto, int usuarioId, string? ip)
        {
            if (string.IsNullOrWhiteSpace(texto)) return (false, "O comentário não pode ser vazio.");

            var tarefa = await _context.Tarefas.FindAsync(tarefaId);
            if (tarefa == null) return (false, "Tarefa não encontrada.");

            var comentario = new Comentario
            {
                TarefaId = tarefaId,
                UsuarioId = usuarioId,
                Texto = texto.Trim(),
                DataCriacao = DateTime.Now
            };

            _context.Comentarios.Add(comentario);

            tarefa.Historicos.Add(new HistoricoTarefa
            {
                TarefaId = tarefaId,
                UsuarioId = usuarioId,
                TipoAlteracao = "Comentário",
                Descricao = "Novo comentário inserido.",
                DataRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "Comentar", "Tarefa", tarefaId, "Novo comentário adicionado", ip);

            return (true, null);
        }

        public async Task<(bool Sucesso, string? Erro)> DefinirFotoCapaAsync(int tarefaId, int fotoResolucaoId, int usuarioId, string? ip)
        {
            var tarefa = await _context.Tarefas
                .Include(t => t.Resolucao).ThenInclude(r => r!.Fotos)
                .FirstOrDefaultAsync(t => t.Id == tarefaId);

            if (tarefa == null) return (false, "Tarefa não encontrada.");
            if (tarefa.Resolucao == null) return (false, "Esta tarefa ainda não possui evidência de resolução.");

            var fotoEscolhida = tarefa.Resolucao.Fotos.FirstOrDefault(f => f.Id == fotoResolucaoId);
            if (fotoEscolhida == null) return (false, "Foto não encontrada nesta resolução.");

            foreach (var foto in tarefa.Resolucao.Fotos)
            {
                foto.IsCapa = foto.Id == fotoEscolhida.Id;
            }

            tarefa.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAsync(usuarioId, "DefinirCapa", "Tarefa", tarefaId, "Foto de capa do card alterada.", ip);

            return (true, null);
        }

        public async Task<DashboardMetricasDto> ObterMetricasDashboardAsync(FiltroTarefaDto? filtro = null)
        {
            var tarefas = await ObterTarefasAsync(filtro);
            var metricas = new DashboardMetricasDto();

            metricas.TotalCriadas = tarefas.Count;
            metricas.TotalPendentes = tarefas.Count(t => t.Status == StatusTarefa.Pendente);
            metricas.TotalEmAndamento = tarefas.Count(t => t.Status == StatusTarefa.EmAndamento);
            metricas.TotalAguardando = tarefas.Count(t => t.Status == StatusTarefa.Aguardando);
            metricas.TotalConcluidas = tarefas.Count(t => t.Status == StatusTarefa.Concluida);
            metricas.TotalCanceladas = tarefas.Count(t => t.Status == StatusTarefa.Cancelada);
            metricas.TotalSemPrazo = tarefas.Count(t => !t.DataPrazo.HasValue);

            var agora = DateTime.Now;
            metricas.TotalAtrasadas = tarefas.Count(t => t.DataPrazo.HasValue && t.DataPrazo.Value < agora && t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada);

            metricas.TotalConcluidasNoPrazo = tarefas.Count(t => t.Status == StatusTarefa.Concluida && t.DataPrazo.HasValue && t.DataConclusao <= t.DataPrazo.Value);
            metricas.TotalConcluidasForaPrazo = tarefas.Count(t => t.Status == StatusTarefa.Concluida && t.DataPrazo.HasValue && t.DataConclusao > t.DataPrazo.Value);

            if (metricas.TotalCriadas > 0)
            {
                metricas.PercentualConclusao = Math.Round((double)metricas.TotalConcluidas / metricas.TotalCriadas * 100, 1);
            }

            var concluidasComTempo = tarefas.Where(t => t.Status == StatusTarefa.Concluida && t.DataConclusao.HasValue).ToList();
            if (concluidasComTempo.Any())
            {
                metricas.MediaHorasConclusao = Math.Round(concluidasComTempo.Average(t => (t.DataConclusao!.Value - t.DataCriacao).TotalHours), 1);
            }

            // Agrupamentos para gráficos
            metricas.PorStatus = new Dictionary<string, int>
            {
                { "Pendente", metricas.TotalPendentes },
                { "Em Andamento", metricas.TotalEmAndamento },
                { "Aguardando", metricas.TotalAguardando },
                { "Concluída", metricas.TotalConcluidas },
                { "Cancelada", metricas.TotalCanceladas }
            };

            metricas.PorPrioridade = new Dictionary<string, int>
            {
                { "Baixa", tarefas.Count(t => t.Prioridade == PrioridadeTarefa.Baixa) },
                { "Normal", tarefas.Count(t => t.Prioridade == PrioridadeTarefa.Normal) },
                { "Alta", tarefas.Count(t => t.Prioridade == PrioridadeTarefa.Alta) },
                { "Urgente", tarefas.Count(t => t.Prioridade == PrioridadeTarefa.Urgente) }
            };

            var responsavelGroups = tarefas.SelectMany(t => t.Responsaveis.Select(r => new { r.Usuario.NomeCompleto, t.Status }))
                .GroupBy(x => x.NomeCompleto);

            foreach (var g in responsavelGroups.Take(10))
            {
                metricas.PorResponsavel[g.Key] = g.Count();
            }

            metricas.ConclusaoPrazo = new Dictionary<string, int>
            {
                { "No Prazo", metricas.TotalConcluidasNoPrazo },
                { "Fora do Prazo", metricas.TotalConcluidasForaPrazo },
                { "Sem Prazo", tarefas.Count(t => t.Status == StatusTarefa.Concluida && !t.DataPrazo.HasValue) }
            };

            return metricas;
        }
    }
}
