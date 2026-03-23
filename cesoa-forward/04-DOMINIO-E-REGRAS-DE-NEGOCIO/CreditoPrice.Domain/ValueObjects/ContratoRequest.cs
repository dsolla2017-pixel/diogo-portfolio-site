// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: ContratoRequest.cs
// Descrição: Objeto de valor que encapsula os dados de entrada para simulação de crédito
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Idempotência — garante que processar a mesma mensagem duas vezes não cause duplicidade
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using System.ComponentModel.DataAnnotations;

namespace CreditoPrice.Domain.ValueObjects
{
    /// <summary>
    /// Representa a solicitação de cálculo de um contrato de financiamento.
    /// Contém os três parâmetros essenciais para o cálculo PRICE:
    /// valor do empréstimo, taxa de juros mensal e prazo em meses.
    /// </summary>
    public class ContratoRequest
    {
        /// <summary>
        /// Valor total do empréstimo solicitado pelo cliente.
        /// Deve ser um valor positivo, representado em reais (BRL).
        /// Exemplo: 10000.00 para um empréstimo de R$ 10.000,00.
        /// </summary>
        [Required(ErrorMessage = "O valor do empréstimo é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor do empréstimo deve ser positivo.")]
        public decimal ValorEmprestimo { get; set; }

        /// <summary>
        /// Taxa de juros mensal expressa em percentual (%).
        /// Exemplo: 1.8 representa 1,8% ao mês.
        /// A conversão para taxa diária equivalente será realizada pelo serviço de cálculo.
        /// </summary>
        [Required(ErrorMessage = "A taxa de juros mensal é obrigatória.")]
        [Range(0.001, 100.0, ErrorMessage = "A taxa de juros mensal deve estar entre 0,001% e 100%.")]
        public decimal TaxaJurosMensal { get; set; }

        /// <summary>
        /// Prazo do financiamento expresso em meses.
        /// Será convertido para dias considerando 30 dias por mês (padrão bancário).
        /// Exemplo: 30 meses geram 900 registros diários de evolução.
        /// </summary>
        [Required(ErrorMessage = "O prazo em meses é obrigatório.")]
        [Range(1, 600, ErrorMessage = "O prazo deve ser entre 1 e 600 meses.")]
        public int PrazoMeses { get; set; }

        /// <summary>
        /// Identificador único da mensagem para controle de idempotência.
        /// Previne o reprocessamento de contratos já calculados.
        /// Gerado automaticamente pelo produtor da mensagem na fila.
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Calcula o prazo total em dias, considerando o padrão bancário
        /// de 30 dias por mês (ano comercial de 360 dias).
        /// </summary>
        public int PrazoDias => PrazoMeses * 30;

        /// <summary>
        /// Converte a taxa mensal percentual para formato decimal.
        /// Exemplo: 1.8% retorna 0.018.
        /// </summary>
        public decimal TaxaMensalDecimal => TaxaJurosMensal / 100m;

        /// <summary>
        /// Valida as regras de negócio do contrato.
        /// Retorna true se todos os campos atendem aos critérios mínimos.
        /// </summary>
        public bool EhValido()
        {
            return ValorEmprestimo > 0
                && TaxaJurosMensal > 0
                && PrazoMeses > 0;
        }

        /// <summary>
        /// Retorna uma representação textual resumida do contrato para logs.
        /// </summary>
        public override string ToString()
        {
            return $"Contrato [Valor={ValorEmprestimo:C2}, Taxa={TaxaJurosMensal}%/mês, Prazo={PrazoMeses}m ({PrazoDias}d)]";
        }
    }
}
