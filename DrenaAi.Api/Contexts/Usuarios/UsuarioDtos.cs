using System.ComponentModel.DataAnnotations;

namespace DrenaAi.Api.Contexts.Usuarios;

/// <summary>
/// Payload de requisição para cadastro de um novo morador com coleta mínima de dados.
/// </summary>
/// <param name="Nome">Nome completo do morador.</param>
/// <param name="Email">Endereço de e-mail válido e único.</param>
/// <param name="Senha">Senha de acesso com no mínimo 6 caracteres.</param>
public record CadastroRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(120)]
    string Nome,

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    [MaxLength(180)]
    string Email,

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve possuir no mínimo 6 caracteres.")]
    string Senha
);

/// <summary>
/// Resposta retornada após o cadastro bem-sucedido de um morador.
/// </summary>
/// <param name="Id">Identificador único gerado para o usuário.</param>
/// <param name="Nome">Nome cadastrado do morador.</param>
/// <param name="Email">E-mail cadastrado do morador.</param>
/// <param name="CriadoEm">Data e hora (UTC) de criação do registro.</param>
public record CadastroResponse(
    Guid Id,
    string Nome,
    string Email,
    DateTime CriadoEm
);

/// <summary>
/// Payload de requisição para autenticação de um morador cadastrado.
/// </summary>
/// <param name="Email">E-mail cadastrado do morador.</param>
/// <param name="Senha">Senha de acesso em texto plano para verificação contra o hash.</param>
public record LoginRequest(
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    string Email,

    [Required(ErrorMessage = "A senha é obrigatória.")]
    string Senha
);

/// <summary>
/// Resposta retornada após autenticação bem-sucedida contendo o token JWT e dados básicos do usuário.
/// </summary>
/// <param name="Token">Token JWT Bearer para acesso às rotas protegidas.</param>
/// <param name="Id">Identificador único do usuário autenticado.</param>
/// <param name="Nome">Nome do usuário autenticado.</param>
/// <param name="Email">E-mail do usuário autenticado.</param>
public record LoginResponse(
    string Token,
    Guid Id,
    string Nome,
    string Email
);
