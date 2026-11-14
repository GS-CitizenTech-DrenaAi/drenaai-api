using System.Reflection;
using System.Text;
using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Contexts.Usuarios;
using DrenaAi.Api.Data;
using DrenaAi.Api.Middlewares;
using DrenaAi.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DrenaAi API",
        Version = "v1",
        Description = "API de zeladoria urbana e prevenção de enchentes com motor de priorização IPC."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {seu_token}"
    });

    options.AddSecurityDefinition("DeviceApiKey", new OpenApiSecurityScheme
    {
        Name = ApiKeyAuthFilter.ApiKeyHeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Chave de autenticação do dispositivo ESP32 (X-Device-Api-Key)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        [new OpenApiSecuritySchemeReference("DeviceApiKey", document)] = []
    });
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<IpcSettings>(builder.Configuration.GetSection("IpcSettings"));
builder.Services.Configure<IaServicesSettings>(builder.Configuration.GetSection("IaServices"));
builder.Services.Configure<DeviceAuthSettings>(builder.Configuration.GetSection("DeviceAuth"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(
        builder.Configuration.GetConnectionString("OracleConnection"),
        oracleOptions => oracleOptions.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();
builder.Services.AddScoped<ITelemetriaService, TelemetriaService>();
builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddSingleton<IPiiSanitizerService, PiiSanitizerService>();
builder.Services.AddSingleton<ICalculadoraIpcService, CalculadoraIpcService>();

var iaServicesSettings = builder.Configuration.GetSection("IaServices").Get<IaServicesSettings>() ?? new IaServicesSettings();
if (iaServicesSettings.UseMocks)
{
    builder.Services.AddSingleton<IIntegracaoIaService, IntegracaoIaMockService>();
}
else
{
    builder.Services.AddHttpClient<IIntegracaoIaService, IntegracaoIaHttpService>();
}

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
if (!string.IsNullOrWhiteSpace(jwtSettings.SecretKey) && !jwtSettings.SecretKey.StartsWith('<'))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        });
}

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DrenaAi API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Classe parcial de entrada exposta para testes de integração via WebApplicationFactory.
/// </summary>
public partial class Program { }
