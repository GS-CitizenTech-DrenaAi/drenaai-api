using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DrenaAi.Api.Contexts.Usuarios;

/// <summary>
/// Serviço responsável pelo cadastro seguro de moradores, hashing de senhas e emissão de tokens JWT.
/// </summary>
public class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuthService"/>.
    /// </summary>
    /// <param name="context">Contexto de banco de dados da aplicação.</param>
    /// <param name="jwtOptions">Configurações JWT injetadas via <see cref="IOptions{JwtSettings}"/>.</param>
    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtOptions)
    {
        _context = context;
        _jwtSettings = jwtOptions.Value;
    }

    /// <inheritdoc />
    public async Task<CadastroResponse> CadastrarAsync(CadastroRequest request)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var emailJaExiste = await _context.Usuarios
            .AnyAsync(u => u.Email == emailNormalizado);

        if (emailJaExiste)
            throw new DomainException("O e-mail informado já está cadastrado.");

        var senhaHash = GerarHashSenha(request.Senha);
        var usuario = new Usuario(request.Nome.Trim(), emailNormalizado, senhaHash);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return new CadastroResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.CriadoEm
        );
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        if (usuario is null || !VerificarSenha(request.Senha, usuario.SenhaHash))
            throw new UnauthorizedException("Credenciais inválidas.");

        var token = GerarTokenJwt(usuario);

        return new LoginResponse(
            token,
            usuario.Id,
            usuario.Nome,
            usuario.Email
        );
    }

    private static string GerarHashSenha(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerificarSenha(string senha, string senhaHashArmazenada)
    {
        var partes = senhaHashArmazenada.Split('.');
        if (partes.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(partes[0]);
            var hashEsperado = Convert.FromBase64String(partes[1]);
            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(senha),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string GerarTokenJwt(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey) || _jwtSettings.SecretKey.Length < 32)
            throw new DomainException("A chave secreta JWT não está configurada corretamente.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpiracaoHoras),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
