// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: StatusProcessamento.cs
// Descrição: Enumeração dos estados possíveis de processamento de um contrato
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

namespace CreditoPrice.Domain.Enums
{
    /// <summary>
    /// Define os estados possíveis do processamento de um contrato.
    /// Utilizado tanto na persistência quanto na publicação de eventos no Event Hub.
    /// </summary>
    public enum StatusProcessamento
    {
        /// <summary>
        /// Processamento concluído com sucesso. Todos os registros diários
        /// foram calculados, persistidos e o evento de telemetria foi publicado.
        /// </summary>
        SUCESSO = 1,

        /// <summary>
        /// Ocorreu uma falha durante o processamento. O evento de erro
        /// contém detalhes para diagnóstico e ação corretiva.
        /// </summary>
        ERRO = 2,

        /// <summary>
        /// Processamento em andamento. Estado intermediário utilizado
        /// para rastreabilidade em cenários de longa duração.
        /// </summary>
        PROCESSANDO = 3
    }
}
