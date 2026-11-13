using System.Text;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Contexts.Usuarios;
using DrenaAi.Api.Data;
using DrenaAi.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<IpcSettings>(builder.Configuration.GetSection("IpcSettings"));
builder.Services.Configure<IaServicesSettings>(builder.Configuration.GetSection("IaServices"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
