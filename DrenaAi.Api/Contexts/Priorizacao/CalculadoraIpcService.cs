using DrenaAi.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Serviço de domínio responsável pelo cálculo matemático do Índice de Prioridade de Coleta (IPC).
/// </summary>
public class CalculadoraIpcService : ICalculadoraIpcService
{
    private readonly IpcSettings _settings;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CalculadoraIpcService"/> validando a soma dos pesos.
    /// </summary>
    /// <param name="options">Opções de configuração injetadas a partir do appsettings.</param>
    /// <exception cref="DomainException">Lançada quando a soma dos pesos configurados difere de 1.0.</exception>
    public CalculadoraIpcService(IOptions<IpcSettings> options)
    {
        _settings = options.Value;
        var somaPesos = Math.Round(_settings.PesoSeveridade + _settings.PesoChuva + _settings.PesoVulnerabilidade, 2);
        if (somaPesos != 1.0)
            throw new DomainException("A soma dos pesos do IPC deve ser exatamente 1.0.");
    }

    /// <inheritdoc />
    public double CalcularIpc(double severidade, double chuvaMm12h, double vulnerabilidadeVia)
    {
        var s = Math.Clamp(severidade, 0.0, 1.0);
        var c = _settings.TetoChuvaMm > 0
            ? Math.Clamp(chuvaMm12h / _settings.TetoChuvaMm, 0.0, 1.0)
            : 0.0;
        var v = Math.Clamp(vulnerabilidadeVia, 0.0, 1.0);

        var ipc = (_settings.PesoSeveridade * s) +
                  (_settings.PesoChuva * c) +
                  (_settings.PesoVulnerabilidade * v);

        return Math.Round(Math.Clamp(ipc, 0.0, 1.0), 4);
    }
}
