// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: Program.cs
// Descrição: Ponto de entrada da aplicação — configura serviços, middleware e pipeline HTTP
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências — permite trocar componentes facilmente, como peças de um quebra-cabeça
//   • Injeção de Dependências
//   • Design Orientado ao Domínio — organiza o código espelhando as regras de negócio do mundo real
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using CreditoPrice.Api.Services;
using CreditoPrice.Domain.Interfaces;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// CONFIGURAÇÃO DE SERVIÇOS (Dependency Injection Container)
// Segue o princípio de Inversão de Dependência (SOLID - DIP).
// -----------------------------------------------------------------------

// Registra o serviço de cálculo PRICE como Singleton (stateless, thread-safe)
builder.Services.AddSingleton<ICalculoPriceService, CalculoPriceService>();

// Configura os controllers MVC com serialização JSON padronizada
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Mantém nomes de propriedades em camelCase (padrão JSON)
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        // Permite trailing commas para maior tolerância na deserialização
        options.JsonSerializerOptions.AllowTrailingCommas = true;
    });

// Configura Swagger/OpenAPI para documentação interativa da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CAIXA - API de Cálculo PRICE",
        Version = "v1",
        Description = "API para cálculo de amortização pelo método PRICE. " +
                      "Gera a evolução diária de contratos de financiamento " +
                      "com capitalização diária equivalente à taxa mensal. " +
                      "Parte do ecossistema de processamento assíncrono de crédito.",
        Contact = new OpenApiContact
        {
            Name = "CESOA - CN Soluções de TI",
            Email = "cesoa@caixa.gov.br"
        }
    });
});

// Configura Health Checks para monitoramento
builder.Services.AddHealthChecks();

// Configura CORS para permitir chamadas do Worker Service
builder.Services.AddCors(options =>
{
    options.AddPolicy("WorkerPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configura logging estruturado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// -----------------------------------------------------------------------
// CONFIGURAÇÃO DO PIPELINE HTTP
// Ordem dos middlewares é crítica para o funcionamento correto.
// -----------------------------------------------------------------------

// Swagger habilitado em todos os ambientes para facilitar testes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CAIXA - API PRICE v1");
    c.RoutePrefix = "swagger";
});

// CORS deve vir antes de Authorization e Controllers
app.UseCors("WorkerPolicy");

// Health check endpoint
app.MapHealthChecks("/health");

// Mapeia os controllers
app.MapControllers();

// -----------------------------------------------------------------------
// INICIALIZAÇÃO
// -----------------------------------------------------------------------
app.Logger.LogInformation(
    "API de Cálculo PRICE iniciada. Ambiente: {Env}",
    app.Environment.EnvironmentName);

app.Run();
