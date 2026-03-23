// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: IEventHubPublisher.cs
// Descrição: Contrato (interface) para publicação de eventos no barramento de mensagens
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Central de Eventos Azure — serviço de nuvem para receber milhões de eventos por segundo
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.ValueObjects;

namespace CreditoPrice.Domain.Interfaces
{
    /// <summary>
    /// Contrato para publicação de eventos de telemetria e logs
    /// no Azure Event Hub. Implementado pela camada de infraestrutura.
    /// </summary>
    public interface IEventHubPublisher
    {
        /// <summary>
        /// Publica um evento de processamento no Event Hub.
        /// O evento contém informações de status, métricas e rastreabilidade.
        /// </summary>
        /// <param name="evento">Dados do evento a ser publicado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        Task PublicarEventoAsync(EventoProcessamento evento, CancellationToken cancellationToken = default);
    }
}
