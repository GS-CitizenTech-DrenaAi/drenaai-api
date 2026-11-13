using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
using DrenaAi.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Serviço de domínio responsável pelo registro, consulta e ordenação dinâmica de ocorrências na fila de coleta.
/// </summary>
public class OcorrenciaService : IOcorrenciaService
{
    private readonly AppDbContext _context;
    private readonly IPiiSanitizerService _piiSanitizer;
    private readonly IIntegracaoIaService _integracaoIa;
    private readonly ICalculadoraIpcService _calculadoraIpc;
    private readonly ILogger<OcorrenciaService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OcorrenciaService"/>.
    /// </summary>
    /// <param name="context">Contexto de banco de dados da aplicação.</param>
    /// <param name="piiSanitizer">Serviço de sanitização de dados pessoais (PII).</param>
    /// <param name="integracaoIa">Serviço de integração com os modelos de visão e clima.</param>
    /// <param name="calculadoraIpc">Calculadora do Índice de Prioridade de Coleta.</param>
    /// <param name="logger">Logger estruturado da aplicação.</param>
    public OcorrenciaService(
        AppDbContext context,
        IPiiSanitizerService piiSanitizer,
        IIntegracaoIaService integracaoIa,
        ICalculadoraIpcService calculadoraIpc,
        ILogger<OcorrenciaService> logger)
    {
        _context = context;
        _piiSanitizer = piiSanitizer;
        _integracaoIa = integracaoIa;
        _calculadoraIpc = calculadoraIpc;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CriarOcorrenciaResponse> CriarAsync(
        CriarOcorrenciaRequest request,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var descricaoSanitizada = _piiSanitizer.Sanitizar(request.Descricao);

        var avaliacaoFoto = await _integracaoIa.AvaliarSeveridadeAsync(request.FotoUrl, cancellationToken);
        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(request.Bairro, cancellationToken);

        var ipc = _calculadoraIpc.CalcularIpc(
            avaliacaoFoto.Severidade,
            previsaoRisco.ChuvaMm12h,
            previsaoRisco.VulnerabilidadeVia);

        var ocorrencia = new Ocorrencia(
            OrigemOcorrencia.APP,
            request.Tipo.Trim(),
            descricaoSanitizada,
            request.FotoUrl,
            request.Latitude,
            request.Longitude,
            request.Bairro.Trim(),
            avaliacaoFoto.Severidade,
            ipc,
            usuarioId);

        _context.Ocorrencias.Add(ocorrencia);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ocorrência {OcorrenciaId} registrada no bairro {Bairro} com IPC {Ipc}.",
            ocorrencia.Id,
            ocorrencia.Bairro,
            ocorrencia.Ipc);

        return new CriarOcorrenciaResponse(
            ocorrencia.Id,
            ocorrencia.Severidade,
            ocorrencia.Ipc,
            ocorrencia.Status.ToString(),
            ocorrencia.Descricao);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OcorrenciaResumoResponse>> ListarAsync(
        string? bairro,
        StatusOcorrencia? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Ocorrencias.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var bairroNorm = bairro.Trim().ToLowerInvariant();
            query = query.Where(o => o.Bairro.ToLower() == bairroNorm);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var ocorrencias = await query
            .OrderByDescending(o => o.Ipc)
            .ThenBy(o => o.CriadaEm)
            .ToListAsync(cancellationToken);

        return ocorrencias.Select(MapearResumo).ToList();
    }

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Lançada quando a ocorrência solicitada não existe.</exception>
    public async Task<OcorrenciaDetalheResponse> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ocorrencia = await _context.Ocorrencias
            .AsNoTracking()
            .Include(o => o.HistoricoStatus)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (ocorrencia is null)
            throw new NotFoundException($"Ocorrência com ID '{id}' não encontrada.");

        var historicoOrdenado = ocorrencia.HistoricoStatus
            .OrderBy(h => h.DataHora)
            .Select(h => new HistoricoStatusItemResponse(h.Status.ToString(), h.DataHora))
            .ToList();

        return new OcorrenciaDetalheResponse(
            ocorrencia.Id,
            ocorrencia.Origem.ToString(),
            ocorrencia.Tipo,
            ocorrencia.Descricao,
            ocorrencia.FotoUrl,
            ocorrencia.Latitude,
            ocorrencia.Longitude,
            ocorrencia.Bairro,
            ocorrencia.Severidade,
            ocorrencia.Ipc,
            ocorrencia.Status.ToString(),
            ocorrencia.CriadaEm,
            historicoOrdenado);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OcorrenciaResumoResponse>> ObterFilaAsync(
        string? bairro = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Ocorrencias
            .AsNoTracking()
            .Where(o => o.Status != StatusOcorrencia.CONCLUIDA);

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var bairroNorm = bairro.Trim().ToLowerInvariant();
            query = query.Where(o => o.Bairro.ToLower() == bairroNorm);
        }

        var fila = await query
            .OrderByDescending(o => o.Ipc)
            .ThenBy(o => o.CriadaEm)
            .ToListAsync(cancellationToken);

        return fila.Select(MapearResumo).ToList();
    }

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Lançada quando a ocorrência informada para priorização não existe.</exception>
    public async Task<PriorizarFilaResponse> PriorizarFilaAsync(
        Guid ocorrenciaId,
        CancellationToken cancellationToken = default)
    {
        var ocorrencia = await _context.Ocorrencias
            .FirstOrDefaultAsync(o => o.Id == ocorrenciaId, cancellationToken);

        if (ocorrencia is null)
            throw new NotFoundException($"Ocorrência com ID '{ocorrenciaId}' não encontrada.");

        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(ocorrencia.Bairro, cancellationToken);
        var novoIpc = _calculadoraIpc.CalcularIpc(
            ocorrencia.Severidade,
            previsaoRisco.ChuvaMm12h,
            previsaoRisco.VulnerabilidadeVia);

        ocorrencia.RecalcularIpc(novoIpc);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Webhook de priorização executado para ocorrência {OcorrenciaId}. Novo IPC: {NovoIpc}.",
            ocorrencia.Id,
            novoIpc);

        var filaAtualizada = await ObterFilaAsync(null, cancellationToken);

        return new PriorizarFilaResponse(
            ocorrencia.Id,
            novoIpc,
            NotificacaoEnviada: true,
            Mensagem: "Fila reordenada e notificação de priorização registrada.",
            Fila: filaAtualizada);
    }

    private static OcorrenciaResumoResponse MapearResumo(Ocorrencia o) => new(
        o.Id,
        o.Origem.ToString(),
        o.Tipo,
        o.Descricao,
        o.Bairro,
        o.Severidade,
        o.Ipc,
        o.Status.ToString(),
        o.CriadaEm);
}
