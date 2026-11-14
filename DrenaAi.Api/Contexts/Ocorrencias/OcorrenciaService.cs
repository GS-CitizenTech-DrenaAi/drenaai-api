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
        var descricaoSanitizada = _piiSanitizer.Sanitizar(request.Descricao ?? string.Empty);

        var localizacao = request.Endereco is not null
            ? new Endereco(
                request.Endereco.Logradouro,
                request.Endereco.Bairro,
                request.Endereco.Latitude,
                request.Endereco.Longitude)
            : new Endereco(
                logradouro: null,
                bairro: request.Bairro,
                latitude: request.Latitude,
                longitude: request.Longitude);

        var referenciaFotoOuTipo = string.IsNullOrWhiteSpace(request.FotoUrl)
            ? request.Tipo
            : $"{request.FotoUrl}|{request.Tipo}";

        var avaliacaoFoto = await _integracaoIa.AvaliarSeveridadeAsync(referenciaFotoOuTipo, cancellationToken);
        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(localizacao.Bairro, cancellationToken);

        var ipc = _calculadoraIpc.CalcularIpc(
            avaliacaoFoto.Severidade,
            previsaoRisco.ChuvaMm12h,
            previsaoRisco.VulnerabilidadeVia);

        var ocorrencia = new Ocorrencia(
            OrigemOcorrencia.APP,
            request.Tipo.Trim(),
            descricaoSanitizada,
            request.FotoUrl,
            localizacao,
            avaliacaoFoto.Severidade,
            ipc,
            usuarioId);

        _context.Ocorrencias.Add(ocorrencia);
        await _context.SaveChangesAsync(cancellationToken);

        var posicaoFila = await CalcularPosicaoNaFilaAsync(ocorrencia.Id, localizacao.Bairro, cancellationToken);
        var parcelas = CalcularParcelasIpc(avaliacaoFoto.Severidade, previsaoRisco.ChuvaMm12h, previsaoRisco.VulnerabilidadeVia);

        _logger.LogInformation(
            "Ocorrência {OcorrenciaId} registrada em {Logradouro} ({Bairro}) com IPC {Ipc}.",
            ocorrencia.Id,
            ocorrencia.Logradouro,
            ocorrencia.Bairro,
            ocorrencia.Ipc);

        return new CriarOcorrenciaResponse(
            ocorrencia.Id,
            ocorrencia.Severidade,
            ocorrencia.Ipc,
            ocorrencia.Status.ToString(),
            ocorrencia.Descricao,
            ocorrencia.FotoUrl,
            MapearEnderecoVo(ocorrencia.Localizacao),
            avaliacaoFoto.Confianca,
            avaliacaoFoto.Justificativa,
            parcelas,
            posicaoFila,
            NormalizarUtc(ocorrencia.CriadaEm));
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
            query = query.Where(o => o.Localizacao.Bairro.ToLower() == bairroNorm);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var ocorrencias = await query
            .OrderByDescending(o => o.Ipc)
            .ThenBy(o => o.CriadaEm)
            .ToListAsync(cancellationToken);

        var posicaoAtiva = 0;
        return ocorrencias
            .Select(o =>
            {
                int? posicao = o.Status != StatusOcorrencia.CONCLUIDA ? ++posicaoAtiva : null;
                return MapearResumo(o, posicao);
            })
            .ToList();
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
            .Select(h => new HistoricoStatusItemResponse(h.Status.ToString(), NormalizarUtc(h.DataHora)))
            .ToList();

        var referenciaFotoOuTipo = string.IsNullOrWhiteSpace(ocorrencia.FotoUrl)
            ? ocorrencia.Tipo
            : $"{ocorrencia.FotoUrl}|{ocorrencia.Tipo}";

        var avaliacaoFoto = await _integracaoIa.AvaliarSeveridadeAsync(referenciaFotoOuTipo, cancellationToken);
        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(ocorrencia.Bairro, cancellationToken);
        var parcelas = CalcularParcelasIpc(ocorrencia.Severidade, previsaoRisco.ChuvaMm12h, previsaoRisco.VulnerabilidadeVia);
        var posicaoFila = ocorrencia.Status != StatusOcorrencia.CONCLUIDA
            ? await CalcularPosicaoNaFilaAsync(ocorrencia.Id, ocorrencia.Bairro, cancellationToken)
            : null;

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
            NormalizarUtc(ocorrencia.CriadaEm),
            historicoOrdenado,
            MapearEnderecoVo(ocorrencia.Localizacao),
            avaliacaoFoto.Confianca,
            avaliacaoFoto.Justificativa,
            parcelas,
            posicaoFila,
            Confirmacoes: 0,
            ocorrencia.UsuarioId);
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
            query = query.Where(o => o.Localizacao.Bairro.ToLower() == bairroNorm);
        }

        var fila = await query
            .OrderByDescending(o => o.Ipc)
            .ThenBy(o => o.CriadaEm)
            .ToListAsync(cancellationToken);

        return fila.Select((o, index) => MapearResumo(o, index + 1)).ToList();
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

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Lançada quando a ocorrência informada não existe.</exception>
    public async Task<OcorrenciaDetalheResponse> AtualizarStatusAsync(
        Guid id,
        StatusOcorrencia novoStatus,
        CancellationToken cancellationToken = default)
    {
        var ocorrencia = await _context.Ocorrencias
            .Include(o => o.HistoricoStatus)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (ocorrencia is null)
            throw new NotFoundException($"Ocorrência com ID '{id}' não encontrada.");

        var novoHistorico = ocorrencia.AtualizarStatus(novoStatus);
        _context.HistoricosStatus.Add(novoHistorico);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Status da ocorrência {OcorrenciaId} atualizado para {NovoStatus}.",
            ocorrencia.Id,
            novoStatus);

        return await ObterPorIdAsync(ocorrencia.Id, cancellationToken);
    }

    private async Task<int?> CalcularPosicaoNaFilaAsync(
        Guid ocorrenciaId,
        string bairro,
        CancellationToken cancellationToken)
    {
        var bairroNorm = bairro.Trim().ToLowerInvariant();
        var idsOrdenados = await _context.Ocorrencias
            .AsNoTracking()
            .Where(o => o.Status != StatusOcorrencia.CONCLUIDA && o.Localizacao.Bairro.ToLower() == bairroNorm)
            .OrderByDescending(o => o.Ipc)
            .ThenBy(o => o.CriadaEm)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var indice = idsOrdenados.IndexOf(ocorrenciaId);
        return indice >= 0 ? indice + 1 : null;
    }

    private static ParcelasIpcVo CalcularParcelasIpc(double severidade, double chuvaMm12h, double vulnerabilidadeVia)
    {
        var s = Math.Round(0.4 * Math.Clamp(severidade, 0.0, 1.0), 2);
        var c = Math.Round(0.3 * Math.Clamp(chuvaMm12h / 50.0, 0.0, 1.0), 2);
        var v = Math.Round(0.3 * Math.Clamp(vulnerabilidadeVia, 0.0, 1.0), 2);
        return new ParcelasIpcVo(s, c, v);
    }

    private static EnderecoVo MapearEnderecoVo(Endereco localizacao) => new(
        localizacao.Logradouro,
        localizacao.Bairro,
        localizacao.Latitude,
        localizacao.Longitude);

    private static OcorrenciaResumoResponse MapearResumo(Ocorrencia o, int? posicaoFila = null) => new(
        o.Id,
        o.Origem.ToString(),
        o.Tipo,
        o.Descricao,
        o.Bairro,
        o.Severidade,
        o.Ipc,
        o.Status.ToString(),
        NormalizarUtc(o.CriadaEm),
        MapearEnderecoVo(o.Localizacao),
        o.FotoUrl,
        posicaoFila);

    private static DateTime NormalizarUtc(DateTime data) =>
        data.Kind == DateTimeKind.Utc
            ? data
            : DateTime.SpecifyKind(data, DateTimeKind.Utc);
}
