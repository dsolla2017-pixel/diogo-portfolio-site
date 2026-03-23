// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: IEvolucaoContratoRepository.cs
// Descrição: Contrato (interface) do repositório de dados de evolução de contratos
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Ferramenta que traduz código C# em comandos SQL automaticamente
//   • Inserção em Massa — grava muitos registros de uma vez, muito mais rápido que um por um
//   • Idempotência — garante que processar a mesma mensagem duas vezes não cause duplicidade
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Entities;

namespace CreditoPrice.Domain.Interfaces
{
    /// <summary>
    /// Contrato de persistência para registros de evolução de contrato.
    /// Implementado pela camada de infraestrutura (Entity Framework Core).
    /// </summary>
    public interface IEvolucaoContratoRepository
    {
        /// <summary>
        /// Persiste uma lista de registros diários de evolução do contrato.
        /// Utiliza inserção em lote (bulk insert) para otimização de performance.
        /// </summary>
        /// <param name="registros">Lista de registros diários calculados.</param>
        /// <param name="cancellationToken">Token de cancelamento para operações assíncronas.</param>
        Task InserirLoteAsync(IEnumerable<EvolucaoContrato> registros, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se um contrato já foi processado (idempotência).
        /// Previne duplicação de registros em caso de reprocessamento.
        /// </summary>
        /// <param name="contratoId">Identificador único do contrato.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>True se o contrato já possui registros persistidos.</returns>
        Task<bool> ContratoJaProcessadoAsync(string contratoId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recupera todos os registros de evolução de um contrato específico.
        /// Utilizado para consultas e validações pós-processamento.
        /// </summary>
        /// <param name="contratoId">Identificador único do contrato.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Lista ordenada por dia de todos os registros do contrato.</returns>
        Task<IEnumerable<EvolucaoContrato>> ObterPorContratoAsync(string contratoId, CancellationToken cancellationToken = default);
    }
}
