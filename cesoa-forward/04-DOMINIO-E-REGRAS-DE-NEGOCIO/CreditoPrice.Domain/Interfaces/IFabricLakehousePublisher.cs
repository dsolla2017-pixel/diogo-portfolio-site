// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: IFabricLakehousePublisher.cs
// Descrição: Contrato (interface) para publicação de dados no Microsoft Fabric Lakehouse
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Lago de Dados — armazém inteligente que combina dados estruturados e não estruturados
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.ValueObjects;

namespace CreditoPrice.Domain.Interfaces
{
    /// <summary>
    /// Contrato para publicação de dados no Microsoft Fabric Lakehouse.
    /// Suporta envio de registros de evolução de contrato e métricas
    /// de processamento para o Data Lakehouse (formato Delta/Parquet).
    /// </summary>
    public interface IFabricLakehousePublisher
    {
        /// <summary>
        /// Publica registros de evolução de contrato no Lakehouse.
        /// Os dados são gravados em formato Delta Lake na tabela
        /// "gold_evolucao_contrato" para consumo analítico.
        /// </summary>
        /// <param name="registros">Lista de registros diários do contrato.</param>
        /// <param name="contratoId">Identificador do contrato para particionamento.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        Task PublicarEvolucaoContratoAsync(
            IEnumerable<EvolucaoContrato> registros,
            string contratoId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publica métricas de processamento no Lakehouse.
        /// Alimenta a tabela "gold_metricas_processamento" para
        /// dashboards de produtividade e custos operacionais.
        /// </summary>
        /// <param name="metrica">Dados de telemetria do processamento.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        Task PublicarMetricaProcessamentoAsync(
            EventoProcessamento metrica,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publica dados de produtividade do Agente de IA no Lakehouse.
        /// Alimenta a tabela "gold_agente_ia_metricas" para análise
        /// de ROI e eliminação de rotinas repetitivas.
        /// </summary>
        /// <param name="metricas">Dicionário com métricas chave-valor.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        Task PublicarMetricasAgenteIAAsync(
            Dictionary<string, object> metricas,
            CancellationToken cancellationToken = default);
    }
}
