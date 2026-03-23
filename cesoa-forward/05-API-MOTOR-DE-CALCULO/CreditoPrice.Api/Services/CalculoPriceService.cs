// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: CalculoPriceService.cs
// Descrição: Serviço que implementa o cálculo da tabela PRICE com juros compostos
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • SAC — método onde a amortização é fixa e as prestações diminuem ao longo do tempo
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Api.Services
{
    /// <summary>
    /// Serviço responsável pelo cálculo de amortização pelo método PRICE.
    /// Implementa a geração diária da evolução do contrato de financiamento.
    /// </summary>
    public class CalculoPriceService : ICalculoPriceService
    {
        private readonly ILogger<CalculoPriceService> _logger;

        /// <summary>
        /// Inicializa o serviço com injeção de dependência do logger.
        /// </summary>
        public CalculoPriceService(ILogger<CalculoPriceService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Executa o cálculo PRICE completo, gerando um registro por dia.
        /// 
        /// Fluxo do cálculo:
        /// 1. Converte a taxa mensal para taxa diária equivalente.
        /// 2. Calcula a prestação fixa mensal pelo método PRICE.
        /// 3. Para cada dia do contrato:
        ///    a. Calcula os juros diários sobre o saldo devedor.
        ///    b. Se é dia de pagamento (múltiplo de 30):
        ///       - Aplica a prestação fixa.
        ///       - Calcula amortização = prestação - juros acumulados.
        ///       - Atualiza o saldo devedor.
        ///    c. Se não é dia de pagamento:
        ///       - Acumula os juros ao saldo (capitalização diária).
        /// </summary>
        /// <param name="request">Dados do contrato de financiamento.</param>
        /// <returns>Lista de registros diários (1 registro por dia).</returns>
        public List<EvolucaoContrato> Calcular(ContratoRequest request)
        {
            _logger.LogInformation(
                "Iniciando cálculo PRICE para contrato: {Contrato}",
                request.ToString());

            // ---------------------------------------------------------------
            // PASSO 1: Conversão de taxas
            // Taxa mensal (percentual) -> decimal -> taxa diária equivalente
            // Fórmula: i_dia = (1 + i_mes)^(1/30) - 1
            // ---------------------------------------------------------------
            decimal taxaMensal = request.TaxaMensalDecimal;
            double taxaDiariaEquivalente = Math.Pow(1.0 + (double)taxaMensal, 1.0 / 30.0) - 1.0;

            _logger.LogDebug(
                "Taxa mensal: {TaxaMensal:P4}, Taxa diária equivalente: {TaxaDiaria:P6}",
                taxaMensal, taxaDiariaEquivalente);

            // ---------------------------------------------------------------
            // PASSO 2: Cálculo da prestação fixa mensal (fórmula PRICE)
            // PMT = PV * [i * (1+i)^n] / [(1+i)^n - 1]
            // Onde: PV = valor empréstimo, i = taxa mensal, n = prazo meses
            // ---------------------------------------------------------------
            double pv = (double)request.ValorEmprestimo;
            double i = (double)taxaMensal;
            int n = request.PrazoMeses;

            double fator = Math.Pow(1.0 + i, n);
            double prestacaoMensal = pv * (i * fator) / (fator - 1.0);

            _logger.LogInformation(
                "Prestação mensal fixa calculada: {Prestacao:F2}",
                prestacaoMensal);

            // ---------------------------------------------------------------
            // PASSO 3: Geração dos registros diários
            // Total de dias = prazoMeses * 30
            // ---------------------------------------------------------------
            int totalDias = request.PrazoDias;
            var registros = new List<EvolucaoContrato>(totalDias);
            double saldoDevedor = pv;
            double jurosAcumuladosPeriodo = 0.0;
            string contratoId = request.MessageId ?? Guid.NewGuid().ToString();
            DateTime dataProcessamento = DateTime.UtcNow;

            for (int dia = 1; dia <= totalDias; dia++)
            {
                // Calcula juros diários sobre o saldo devedor atual
                double jurosDiario = saldoDevedor * taxaDiariaEquivalente;
                jurosAcumuladosPeriodo += jurosDiario;

                bool ehDiaPagamento = (dia % 30 == 0);

                if (ehDiaPagamento)
                {
                    // ---------------------------------------------------------
                    // DIA DE PAGAMENTO (a cada 30 dias)
                    // Prestação fixa, juros acumulados no período, amortização
                    // ---------------------------------------------------------
                    double amortizacao = prestacaoMensal - jurosAcumuladosPeriodo;
                    saldoDevedor -= amortizacao;

                    // Ajuste para evitar saldo negativo no último pagamento
                    if (dia == totalDias && Math.Abs(saldoDevedor) < 0.01)
                    {
                        saldoDevedor = 0.0;
                    }

                    registros.Add(new EvolucaoContrato
                    {
                        ContratoId = contratoId,
                        Dia = dia,
                        Prestacao = Math.Round((decimal)prestacaoMensal, 2),
                        JurosPeriodo = Math.Round((decimal)jurosAcumuladosPeriodo, 2),
                        Amortizacao = Math.Round((decimal)amortizacao, 2),
                        SaldoAposPagar = Math.Round((decimal)Math.Max(saldoDevedor, 0), 2),
                        DataProcessamento = dataProcessamento
                    });

                    // Reseta acumulador de juros para o próximo período
                    jurosAcumuladosPeriodo = 0.0;
                }
                else
                {
                    // ---------------------------------------------------------
                    // DIA SEM PAGAMENTO
                    // Registra a capitalização diária dos juros
                    // Prestação e amortização são zero
                    // ---------------------------------------------------------
                    registros.Add(new EvolucaoContrato
                    {
                        ContratoId = contratoId,
                        Dia = dia,
                        Prestacao = 0m,
                        JurosPeriodo = Math.Round((decimal)jurosDiario, 2),
                        Amortizacao = 0m,
                        SaldoAposPagar = Math.Round((decimal)(saldoDevedor + jurosAcumuladosPeriodo), 2),
                        DataProcessamento = dataProcessamento
                    });
                }
            }

            _logger.LogInformation(
                "Cálculo PRICE concluído. Total de registros gerados: {Total}. ContratoId: {ContratoId}",
                registros.Count, contratoId);

            return registros;
        }
    }
}
