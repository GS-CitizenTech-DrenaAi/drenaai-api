using System.Net.Http.Json;
using DrenaAi.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Implementação de produção para integração HTTP com os microsserviços externos de Visão Computacional e ML.
/// </summary>
public class IntegracaoIaHttpService : IIntegracaoIaService
{
    private readonly HttpClient _httpClient;
    private readonly IaServicesSettings _settings;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="IntegracaoIaHttpService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado para comunicação com os microsserviços externos.</param>
    /// <param name="options">Configurações de URLs dos serviços de IA injetadas via <see cref="IOptions{IaServicesSettings}"/>.</param>
    public IntegracaoIaHttpService(HttpClient httpClient, IOptions<IaServicesSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando nenhuma foto válida é informada ou a URL do serviço está ausente.</exception>
    public async Task<AvaliarSeveridadeResponse> AvaliarSeveridadeAsync(string? foto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(foto))
            throw new DomainException("A referência da foto é obrigatória para avaliação de severidade.");

        if (string.IsNullOrWhiteSpace(_settings.VisionBaseUrl))
            throw new DomainException("A URL base do serviço de visão computacional não está configurada.");

        var url = $"{_settings.VisionBaseUrl.TrimEnd('/')}/api/severidade";
        var response = await _httpClient.PostAsJsonAsync(url, new AvaliarSeveridadeRequest(foto), cancellationToken);
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<AvaliarSeveridadeResponse>(cancellationToken: cancellationToken);
        return resultado ?? throw new DomainException("Resposta inválida do serviço de visão computacional.");
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando o parâmetro bairro é ausente ou a URL do serviço está ausente.</exception>
    public async Task<PrevisaoRiscoResponse> ObterPrevisaoERiscoAsync(string bairro, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("O parâmetro 'bairro' é obrigatório.");

        if (string.IsNullOrWhiteSpace(_settings.MlFloodRiskBaseUrl))
            throw new DomainException("A URL base do serviço de ML de risco de alagamento não está configurada.");

        var url = $"{_settings.MlFloodRiskBaseUrl.TrimEnd('/')}/api/previsao?bairro={Uri.EscapeDataString(bairro.Trim())}";
        var resultado = await _httpClient.GetFromJsonAsync<PrevisaoRiscoResponse>(url, cancellationToken);
        return resultado ?? throw new DomainException("Resposta inválida do serviço de ML de risco de alagamento.");
    }
}
