// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: ICalculoPriceService.cs
// Descrição: Contrato (interface) do serviço de cálculo da tabela PRICE
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.ValueObjects;

namespace CreditoPrice.Domain.Interfaces
{
    /// <summary>
    /// Contrato do serviço de cálculo de amortização pelo método PRICE.
    /// Responsável por gerar a lista completa de registros diários
    /// de evolução do contrato de financiamento.
    /// </summary>
    public interface ICalculoPriceService
    {
        /// <summary>
        /// Executa o cálculo PRICE e retorna a lista de registros diários.
        /// Cada registro representa um dia do contrato com prestação,
        /// juros, amortização e saldo devedor.
        /// </summary>
        /// <param name="request">Dados do contrato (valor, taxa, prazo).</param>
        /// <returns>Lista de registros diários ordenados por dia (1 a N).</returns>
        List<EvolucaoContrato> Calcular(ContratoRequest request);
    }
}
