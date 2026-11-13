namespace DrenaAi.Api.Security;

/// <summary>
/// Configurações de autenticação por chave de API para dispositivos IoT (ESP32).
/// </summary>
public class DeviceAuthSettings
{
    /// <summary>
    /// Chave secreta esperada no cabeçalho HTTP <c>X-Device-Api-Key</c>.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
