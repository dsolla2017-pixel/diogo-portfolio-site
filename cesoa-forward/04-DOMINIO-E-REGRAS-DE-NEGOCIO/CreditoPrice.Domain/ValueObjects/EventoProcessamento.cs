// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: EventoProcessamento.cs
// Descrição: Objeto de valor que representa um evento de processamento para rastreabilidade
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Central de Eventos Azure — serviço de nuvem para receber milhões de eventos por segundo
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Enums;

namespace CreditoPrice.Domain.ValueObjects
{
    /// <summary>
    /// Representa o evento de telemetria publicado no Azure Event Hub
    /// após o processamento de cada contrato de crédito.
    /// Estrutura padronizada para consumo por sistemas de monitoramento.
    /// </summary>
    public class EventoProcessamento
    {
        /// <summary>
        /// Descrição da ação realizada pelo Worker Service.
        /// Exemplo: "ProcessamentoContratoPRICE", "ValidacaoPayload".
        /// </summary>
        public string Acao { get; set; } = string.Empty;

        /// <summary>
        /// Status do processamento (SUCESSO ou ERRO).
        /// Tipado como enum para garantir consistência nos logs.
        /// </summary>
        public StatusProcessamento Status { get; set; }

        /// <summary>
        /// Conteúdo resumido da mensagem processada.
        /// Inclui os parâmetros do contrato para correlação.
        /// </summary>
        public string MensagemProcessada { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora UTC do processamento.
        /// Padrão ISO 8601 para interoperabilidade.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Identificador de correlação para rastreabilidade ponta a ponta.
        /// Permite vincular o evento à mensagem original da fila.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Detalhes adicionais em caso de erro.
        /// Contém a mensagem de exceção para diagnóstico.
        /// </summary>
        public string? DetalhesErro { get; set; }

        /// <summary>
        /// Quantidade de registros diários gerados pelo cálculo.
        /// Permite validação cruzada (ex.: 900 para 30 meses).
        /// </summary>
        public int QuantidadeRegistros { get; set; }

        /// <summary>
        /// Tempo total de processamento em milissegundos.
        /// Métrica de performance para SLA e melhoria contínua.
        /// </summary>
        public long TempoProcessamentoMs { get; set; }
    }
}
