using DrenaAi.Api.Exceptions;

namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Implementação simulada determinística dos serviços de IA para desenvolvimento local e validação dos cenários de referência.
/// </summary>
public class IntegracaoIaMockService : IIntegracaoIaService
{
    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando nenhuma foto ou referência válida é informada.</exception>
    public Task<AvaliarSeveridadeResponse> AvaliarSeveridadeAsync(string? foto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(foto))
            throw new DomainException("A referência da foto é obrigatória para avaliação de severidade.");

        var fotoLower = foto.Trim().ToLowerInvariant();

        AvaliarSeveridadeResponse resposta;
        if (fotoLower.Contains("bueiro_obstruido_critico") ||
            fotoLower.Contains("foto_bueiro") ||
            fotoLower.Contains("cenario_a") ||
            fotoLower.Contains("critico") ||
            fotoLower.Contains("cheio"))
        {
            resposta = new AvaliarSeveridadeResponse(
                0.75,
                0.92,
                "Acúmulo severo de resíduos obstruindo a grade de drenagem."
            );
        }
        else if (fotoLower.Contains("cenario_b") ||
                 fotoLower.Contains("medio") ||
                 fotoLower.Contains("leve"))
        {
            resposta = new AvaliarSeveridadeResponse(
                0.50,
                0.88,
                "Acúmulo moderado de resíduos próximo à sarjeta."
            );
        }
        else
        {
            resposta = new AvaliarSeveridadeResponse(
                0.60,
                0.85,
                "Presença de resíduos sólidos identificada na via pública."
            );
        }

        return Task.FromResult(resposta);
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando o parâmetro bairro é ausente ou vazio.</exception>
    public Task<PrevisaoRiscoResponse> ObterPrevisaoERiscoAsync(string bairro, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("O parâmetro 'bairro' é obrigatório.");

        var bairroNormalizado = bairro.Trim();
        var bairroLower = bairroNormalizado.ToLowerInvariant();

        PrevisaoRiscoResponse resposta;
        if (bairroLower.Contains("vila mariana") || bairroLower.Contains("cenario a") || bairroLower.Contains("cenário a"))
        {
            resposta = new PrevisaoRiscoResponse(
                bairroNormalizado,
                ChuvaMm12h: 35.0,
                ChuvaMm24h: 55.0,
                RiscoAlagamento: 0.85,
                VulnerabilidadeVia: 0.90
            );
        }
        else if (bairroLower.Contains("pinheiros") || bairroLower.Contains("cenario b") || bairroLower.Contains("cenário b"))
        {
            resposta = new PrevisaoRiscoResponse(
                bairroNormalizado,
                ChuvaMm12h: 0.0,
                ChuvaMm24h: 2.0,
                RiscoAlagamento: 0.15,
                VulnerabilidadeVia: 0.20
            );
        }
        else
        {
            resposta = new PrevisaoRiscoResponse(
                bairroNormalizado,
                ChuvaMm12h: 15.0,
                ChuvaMm24h: 28.0,
                RiscoAlagamento: 0.50,
                VulnerabilidadeVia: 0.60
            );
        }

        return Task.FromResult(resposta);
    }
}
