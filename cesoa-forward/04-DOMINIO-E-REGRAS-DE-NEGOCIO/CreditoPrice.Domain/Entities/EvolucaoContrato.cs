// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: EvolucaoContrato.cs
// Descrição: Entidade principal que representa a evolução de um contrato de crédito com tabela PRICE
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Idempotência — garante que processar a mesma mensagem duas vezes não cause duplicidade
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditoPrice.Domain.Entities
{
    /// <summary>
    /// Representa um registro diário da evolução de um contrato de financiamento.
    /// Cada registro contém os valores de prestação, juros, amortização e saldo
    /// devedor para um dia específico do contrato.
    /// Tabela de destino: EvolucaoContrato (SQL Server).
    /// </summary>
    [Table("EvolucaoContrato")]
    public class EvolucaoContrato
    {
        /// <summary>
        /// Identificador único do registro (chave primária auto-incremento).
        /// Tipo bigint para suportar alto volume de registros em produção.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Identificador único do contrato para agrupamento e rastreabilidade.
        /// Permite vincular todos os registros diários a um mesmo contrato
        /// e implementar idempotência no processamento.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ContratoId { get; set; } = string.Empty;

        /// <summary>
        /// Dia do contrato (1 a N, onde N = prazoMeses * 30).
        /// Exemplo: para um contrato de 30 meses, varia de 1 a 900.
        /// </summary>
        [Required]
        public int Dia { get; set; }

        /// <summary>
        /// Valor da prestação fixa calculada pelo método PRICE.
        /// No método PRICE, a prestação permanece constante ao longo
        /// de todo o período do financiamento.
        /// Precisão: decimal(18,2) conforme padrão monetário bancário.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Prestacao { get; set; }

        /// <summary>
        /// Valor dos juros acumulados no período (dia) corrente.
        /// Calculado com base na taxa diária equivalente aplicada
        /// sobre o saldo devedor do dia anterior.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal JurosPeriodo { get; set; }

        /// <summary>
        /// Valor da amortização do dia corrente.
        /// Corresponde à diferença entre a prestação e os juros do período.
        /// No método PRICE, a amortização cresce ao longo do tempo.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amortizacao { get; set; }

        /// <summary>
        /// Saldo devedor remanescente após o pagamento do dia corrente.
        /// Decresce ao longo do contrato até atingir zero no último dia.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoAposPagar { get; set; }

        /// <summary>
        /// Data e hora em que o registro foi processado e persistido.
        /// Utilizado para auditoria e rastreabilidade temporal.
        /// </summary>
        [Required]
        public DateTime DataProcessamento { get; set; }
    }
}
