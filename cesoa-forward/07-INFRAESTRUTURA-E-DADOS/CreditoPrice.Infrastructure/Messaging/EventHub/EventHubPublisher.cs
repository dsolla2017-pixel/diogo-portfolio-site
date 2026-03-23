// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: EventHubPublisher.cs
// Descrição: Publicador de eventos no Azure Event Hub para processamento assíncrono
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Liberação Assíncrona de Recursos — garante que conexões e memória sejam liberadas corretamente
//   • Dispara e Esquece — envia a informação sem esperar confirmação de recebimento
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Central de Eventos Azure — serviço de nuvem para receber milhões de eventos por segundo
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Infrastructure.Messaging.EventHub
{
    /// <summary>
    /// Publica eventos de telemetria no Azure Event Hub.
    /// Cada evento contém informações de status, métricas e rastreabilidade
    /// do processamento de contratos de crédito.
    /// </summary>
    public class EventHubPublisher : IEventHubPublisher, IAsyncDisposable
    {
        private readonly EventHubProducerClient _producerClient;
        private readonly ILogger<EventHubPublisher> _logger;
        private readonly string _eventHubName;

        /// <summary>
        /// Inicializa o publisher com as configurações do Event Hub.
        /// A connection string e o nome do hub são lidos do appsettings.json.
        /// </summary>
        public EventHubPublisher(
            IConfiguration configuration,
            ILogger<EventHubPublisher> logger)
        {
            _logger = logger;

            // Lê configurações do Event Hub
            string connectionString = configuration["EventHub:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Connection string do Event Hub não configurada.");

            _eventHubName = configuration["EventHub:EventHubName"]
                ?? "eh-credito-log";

            _producerClient = new EventHubProducerClient(
                connectionString, _eventHubName);

            _logger.LogInformation(
                "EventHubPublisher inicializado. Hub: {HubName}", _eventHubName);
        }

        /// <summary>
        /// Publica um evento de processamento no Event Hub.
        /// O evento é serializado em JSON e enviado como um EventData.
        /// Em caso de falha na publicação, o erro é logado mas não
        /// interrompe o fluxo principal (fire-and-forget com log).
        /// </summary>
        public async Task PublicarEventoAsync(
            EventoProcessamento evento,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Publicando evento no Event Hub. Ação: {Acao}, Status: {Status}, CorrelationId: {CorrelationId}",
                    evento.Acao, evento.Status, evento.CorrelationId);

                // Serializa o evento em JSON
                string jsonEvento = JsonSerializer.Serialize(evento, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                // Cria o lote de eventos (um evento por publicação)
                using EventDataBatch eventBatch = await _producerClient
                    .CreateBatchAsync(cancellationToken);

                var eventData = new EventData(Encoding.UTF8.GetBytes(jsonEvento));

                // Adiciona propriedades de roteamento para consumidores
                eventData.Properties["acao"] = evento.Acao;
                eventData.Properties["status"] = evento.Status.ToString();
                eventData.Properties["correlationId"] = evento.CorrelationId;
                eventData.Properties["timestamp"] = evento.Timestamp.ToString("o");

                if (!eventBatch.TryAdd(eventData))
                {
                    _logger.LogError(
                        "Evento excede o tamanho máximo do lote. CorrelationId: {CorrelationId}",
                        evento.CorrelationId);
                    return;
                }

                // Publica o lote no Event Hub
                await _producerClient.SendAsync(eventBatch, cancellationToken);

                _logger.LogInformation(
                    "Evento publicado com sucesso no Event Hub '{Hub}'. CorrelationId: {CorrelationId}",
                    _eventHubName, evento.CorrelationId);
            }
            catch (Exception ex)
            {
                // Log do erro sem interromper o fluxo principal
                // A telemetria é importante mas não deve bloquear o processamento
                _logger.LogError(ex,
                    "Falha ao publicar evento no Event Hub. CorrelationId: {CorrelationId}",
                    evento.CorrelationId);
            }
        }

        /// <summary>
        /// Libera os recursos do producer client de forma assíncrona.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _producerClient.DisposeAsync();
            _logger.LogInformation("EventHubPublisher encerrado.");
        }
    }
}
