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
//   • Contexto do Banco — é a 'porta de entrada' para acessar o banco de dados
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Tentativa Automática — se algo falhar, o sistema tenta novamente automaticamente
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Infrastructure.Data.Context;
using CreditoPrice.Infrastructure.Data.Repositories;
using CreditoPrice.Infrastructure.Messaging.EventHub;
using CreditoPrice.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// -----------------------------------------------------------------------
// CONFIGURAÇÃO DO BANCO DE DADOS
// Utiliza InMemory para desenvolvimento local e SQL Server para produção.
// A troca é feita via configuração, sem alteração de código (DIP/SOLID).
// -----------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Modo desenvolvimento: banco em memória para testes rápidos
    builder.Services.AddDbContext<CreditoPriceDbContext>(options =>
        options.UseInMemoryDatabase("CreditoPriceDb"));

    Console.WriteLine("[CONFIG] Banco de dados: InMemory (desenvolvimento)");
}
else
{
    // Modo produção: SQL Server
    builder.Services.AddDbContext<CreditoPriceDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

    Console.WriteLine("[CONFIG] Banco de dados: SQL Server");
}

// -----------------------------------------------------------------------
// REGISTRO DE SERVIÇOS (Dependency Injection)
// -----------------------------------------------------------------------

// Repositório de persistência
builder.Services.AddScoped<IEvolucaoContratoRepository, EvolucaoContratoRepository>();

// Publisher do Event Hub
builder.Services.AddSingleton<IEventHubPublisher, EventHubPublisher>();

// HttpClient para chamadas à API de Cálculo PRICE
builder.Services.AddHttpClient("PriceApi", client =>
{
    string baseUrl = builder.Configuration["PriceApi:BaseUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Registra o Worker Service como Hosted Service
builder.Services.AddHostedService<ContratoWorker>();

// -----------------------------------------------------------------------
// CONFIGURAÇÃO DE LOGGING
// -----------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// -----------------------------------------------------------------------
// BUILD E EXECUÇÃO
// -----------------------------------------------------------------------
var host = builder.Build();

Console.WriteLine("==========================================================");
Console.WriteLine("  CAIXA - Worker Service de Processamento de Crédito");
Console.WriteLine("  Método PRICE | Desafio Técnico PSI-CTI");
Console.WriteLine("==========================================================");

host.Run();
