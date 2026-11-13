namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Payload de requisição para avaliação de severidade de obstrução a partir de uma imagem.
/// </summary>
/// <param name="Foto">URL, caminho ou identificador da imagem capturada pelo aplicativo.</param>
public record AvaliarSeveridadeRequest(string? Foto);

/// <summary>
/// Resposta estruturada da análise de severidade realizada pelo serviço de visão computacional.
/// </summary>
/// <param name="Severidade">Grau de severidade normalizado entre 0.0 e 1.0.</param>
/// <param name="Confianca">Nível de confiança da inferência do modelo entre 0.0 e 1.0.</param>
/// <param name="Justificativa">Descrição técnica explicando a classificação atribuída.</param>
public record AvaliarSeveridadeResponse(
    double Severidade,
    double Confianca,
    string Justificativa
);

/// <summary>
/// Resposta com os dados de previsão pluviométrica e vulnerabilidade topográfica de um bairro.
/// </summary>
/// <param name="Bairro">Nome do bairro consultado.</param>
/// <param name="ChuvaMm12h">Volume de chuva previsto em milímetros para as próximas 12 horas.</param>
/// <param name="ChuvaMm24h">Volume de chuva previsto em milímetros para as próximas 24 horas.</param>
/// <param name="RiscoAlagamento">Índice composto de risco de alagamento entre 0.0 e 1.0.</param>
/// <param name="VulnerabilidadeVia">Índice de vulnerabilidade histórica e topográfica da via entre 0.0 e 1.0.</param>
public record PrevisaoRiscoResponse(
    string Bairro,
    double ChuvaMm12h,
    double ChuvaMm24h,
    double RiscoAlagamento,
    double VulnerabilidadeVia
);
