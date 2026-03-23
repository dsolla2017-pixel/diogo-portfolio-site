// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: ContratoWorker.cs
// Descrição: Worker Service que consome contratos da fila e processa em segundo plano
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Serviço em Segundo Plano — roda continuamente sem interação do usuário, como um robô de processamento
//   • Serviço Hospedado — componente que roda automaticamente quando a aplicação inicia
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Objeto de Transferência de Dados — estrutura simples para transportar informações entre componentes
//   • Barramento de Mensagens — fila inteligente que garante entrega confiável de mensagens entre sistemas
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Fila de Mensagens — organiza pedidos para serem processados um por vez, na ordem correta
//

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.Enums;
using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Worker.Services
{
    /// <summary>
    /// Worker Service que consome mensagens da fila Azure Service Bus,
    /// processa contratos de financiamento pelo método PRICE e orquestra
    /// todo o fluxo de cálculo, persistência e telemetria.
    /// 
    /// Implementa IHostedService via BackgroundService, padrão recomendado
    /// pelo .NET para processamento assíncrono em segundo plano.
    /// </summary>
    public class ContratoWorker : BackgroundService
    {
        private readonly ILogger<ContratoWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEvolucaoContratoRepository _repository;
        private readonly IEventHubPublisher _eventHubPublisher;
        private readonly HttpClient _httpClient;

        private ServiceBusClient? _serviceBusClient;
        private ServiceBusProcessor? _processor;

        /// <summary>
        /// Inicializa o Worker com todas as dependências injetadas.
        /// </summary>
        public ContratoWorker(
            ILogger<ContratoWorker> logger,
            IConfiguration configuration,
            IEvolucaoContratoRepository repository,
            IEventHubPublisher eventHubPublisher,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
            _eventHubPublisher = eventHubPublisher;
            _httpClient = httpClientFactory.CreateClient("PriceApi");
        }

        /// <summary>
        /// Método principal do BackgroundService.
        /// Configura o processador do Service Bus e inicia o consumo de mensagens.
        /// Executa continuamente até que o CancellationToken seja acionado.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "=== WORKER SERVICE INICIADO === " +
                "Aguardando mensagens na fila de contratos de crédito...");

            try
            {
                // ---------------------------------------------------------
                // CONFIGURAÇÃO DO SERVICE BUS
                // Lê connection string e nome da fila do appsettings.json
                // ---------------------------------------------------------
                string connectionString = _configuration["ServiceBus:ConnectionString"]
                    ?? throw new InvalidOperationException(
                        "Connection string do Service Bus não configurada.");

                string queueName = _configuration["ServiceBus:QueueName"]
                    ?? "queue-contrato-price";

                _serviceBusClient = new ServiceBusClient(connectionString);

                // Configura o processador com opções de resiliência
                _processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
                {
                    // Processa uma mensagem por vez (garante ordem e simplicidade)
                    MaxConcurrentCalls = 1,
                    // Auto-complete desabilitado para controle manual
                    AutoCompleteMessages = false,
                    // Tempo máximo de lock da mensagem
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10)
                });

                // Registra os handlers de mensagem e erro
                _processor.ProcessMessageAsync += ProcessarMensagemAsync;
                _processor.ProcessErrorAsync += TratarErroAsync;

                // Inicia o processamento
                await _processor.StartProcessingAsync(stoppingToken);

                _logger.LogInformation(
                    "Processador do Service Bus iniciado. Fila: {Fila}", queueName);

                // Mantém o worker ativo até o cancelamento
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker Service encerrado por solicitação.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro fatal no Worker Service.");
                throw;
            }
        }

        /// <summary>
        /// Handler principal de processamento de mensagens.
        /// Implementa o fluxo completo: validação, cálculo, persistência,
        /// telemetria e confirmação da mensagem.
        /// </summary>
        private async Task ProcessarMensagemAsync(ProcessMessageEventArgs args)
        {
            var stopwatch = Stopwatch.StartNew();
            string correlationId = args.Message.MessageId ?? Guid.NewGuid().ToString();

            _logger.LogInformation(
                ">>> Mensagem recebida. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                args.Message.MessageId, correlationId);

            try
            {
                // ---------------------------------------------------------
                // PASSO 1: Deserialização do payload JSON
                // ---------------------------------------------------------
                string body = args.Message.Body.ToString();
                _logger.LogDebug("Payload recebido: {Payload}", body);

                var request = JsonSerializer.Deserialize<ContratoRequest>(body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null)
                {
                    throw new InvalidOperationException("Payload da mensagem é nulo ou inválido.");
                }

                // Atribui o MessageId para controle de idempotência
                request.MessageId = correlationId;

                // ---------------------------------------------------------
                // PASSO 2: Validação do payload
                // ---------------------------------------------------------
                if (!request.EhValido())
                {
                    _logger.LogWarning(
                        "Payload inválido. Valores devem ser positivos. {Contrato}",
                        request.ToString());

                    // Mensagem com dados inválidos é completada (dead letter seria alternativa)
                    await args.CompleteMessageAsync(args.Message);

                    await PublicarEventoErro(
                        correlationId, request.ToString(),
                        "Payload inválido: valores devem ser positivos e prazo > 0.",
                        stopwatch.ElapsedMilliseconds);

                    return;
                }

                _logger.LogInformation("Payload validado: {Contrato}", request.ToString());

                // ---------------------------------------------------------
                // PASSO 3: Verificação de idempotência
                // Previne reprocessamento de contratos já calculados
                // ---------------------------------------------------------
                bool jaProcessado = await _repository.ContratoJaProcessadoAsync(correlationId);
                if (jaProcessado)
                {
                    _logger.LogWarning(
                        "Contrato já processado (idempotência ativa). MessageId: {MessageId}",
                        correlationId);

                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                // ---------------------------------------------------------
                // PASSO 4: Chamada à API de Cálculo PRICE via HTTP
                // ---------------------------------------------------------
                _logger.LogInformation("Chamando API de Cálculo PRICE...");

                string apiUrl = _configuration["PriceApi:BaseUrl"]
                    ?? "http://localhost:5100";

                var response = await _httpClient.PostAsJsonAsync(
                    $"{apiUrl}/api/price/calcular", request);

                response.EnsureSuccessStatusCode();

                var registrosApi = await response.Content
                    .ReadFromJsonAsync<List<RegistroApiResponse>>();

                if (registrosApi == null || registrosApi.Count == 0)
                {
                    throw new InvalidOperationException("API retornou resultado vazio.");
                }

                _logger.LogInformation(
                    "API retornou {Total} registros.", registrosApi.Count);

                // ---------------------------------------------------------
                // PASSO 5: Conversão e persistência no banco de dados
                // ---------------------------------------------------------
                var registrosEntidade = registrosApi.Select(r => new EvolucaoContrato
                {
                    ContratoId = correlationId,
                    Dia = r.Dia,
                    Prestacao = r.Prestacao,
                    JurosPeriodo = r.JurosPeriodo,
                    Amortizacao = r.Amortizacao,
                    SaldoAposPagar = r.SaldoAposPagar,
                    DataProcessamento = DateTime.UtcNow
                }).ToList();

                _logger.LogInformation("Persistindo registros no banco de dados...");
                await _repository.InserirLoteAsync(registrosEntidade);

                // ---------------------------------------------------------
                // PASSO 6: Publicação de evento de sucesso no Event Hub
                // ---------------------------------------------------------
                stopwatch.Stop();

                await _eventHubPublisher.PublicarEventoAsync(new EventoProcessamento
                {
                    Acao = "ProcessamentoContratoPRICE",
                    Status = StatusProcessamento.SUCESSO,
                    MensagemProcessada = request.ToString(),
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    QuantidadeRegistros = registrosEntidade.Count,
                    TempoProcessamentoMs = stopwatch.ElapsedMilliseconds
                });

                // ---------------------------------------------------------
                // PASSO 7: Confirmação da mensagem na fila
                // ---------------------------------------------------------
                await args.CompleteMessageAsync(args.Message);

                _logger.LogInformation(
                    "<<< Processamento concluído com SUCESSO. " +
                    "MessageId: {MessageId}, Registros: {Total}, Tempo: {Tempo}ms",
                    correlationId, registrosEntidade.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "ERRO ao processar mensagem. MessageId: {MessageId}", correlationId);

                // Publica evento de erro no Event Hub
                await PublicarEventoErro(
                    correlationId,
                    args.Message.Body.ToString(),
                    ex.Message,
                    stopwatch.ElapsedMilliseconds);

                // Abandona a mensagem para retry automático do Service Bus
                await args.AbandonMessageAsync(args.Message);
            }
        }

        /// <summary>
        /// Handler de erros do processador do Service Bus.
        /// Trata erros de infraestrutura (conexão, timeout, etc.).
        /// </summary>
        private Task TratarErroAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception,
                "Erro no processador do Service Bus. Fonte: {Fonte}, Namespace: {Namespace}",
                args.ErrorSource, args.FullyQualifiedNamespace);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Publica um evento de erro no Event Hub para rastreabilidade.
        /// </summary>
        private async Task PublicarEventoErro(
            string correlationId, string mensagem, string detalhesErro, long tempoMs)
        {
            try
            {
                await _eventHubPublisher.PublicarEventoAsync(new EventoProcessamento
                {
                    Acao = "ProcessamentoContratoPRICE",
                    Status = StatusProcessamento.ERRO,
                    MensagemProcessada = mensagem,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    DetalhesErro = detalhesErro,
                    QuantidadeRegistros = 0,
                    TempoProcessamentoMs = tempoMs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao publicar evento de erro no Event Hub.");
            }
        }

        /// <summary>
        /// Encerra o processador e libera recursos ao parar o Worker.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Encerrando Worker Service...");

            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }

            if (_serviceBusClient != null)
            {
                await _serviceBusClient.DisposeAsync();
            }

            _logger.LogInformation("Worker Service encerrado com sucesso.");
            await base.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// DTO interno para deserialização da resposta da API de Cálculo PRICE.
    /// Mapeia os campos retornados pelo endpoint POST /api/price/calcular.
    /// </summary>
    internal class RegistroApiResponse
    {
        public int Dia { get; set; }
        public decimal Prestacao { get; set; }
        public decimal JurosPeriodo { get; set; }
        public decimal Amortizacao { get; set; }
        public decimal SaldoAposPagar { get; set; }
    }
}
