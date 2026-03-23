// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: CalculoPriceServiceTests.cs
// Descrição: Testes unitários do serviço de cálculo PRICE — valida cenários reais de crédito
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Framework de Testes — ferramenta para validar automaticamente se o código funciona corretamente
//   • Verificação — confirma se o resultado obtido é igual ao resultado esperado
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using CreditoPrice.Api.Services;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreditoPrice.Tests.Unit
{
    /// <summary>
    /// Testes unitários para o serviço de cálculo PRICE.
    /// Cada teste valida um aspecto específico do cálculo financeiro.
    /// </summary>
    public class CalculoPriceServiceTests
    {
        private readonly CalculoPriceService _service;

        public CalculoPriceServiceTests()
        {
            // Utiliza NullLogger para testes (sem output de log)
            var logger = NullLogger<CalculoPriceService>.Instance;
            _service = new CalculoPriceService(logger);
        }

        /// <summary>
        /// Verifica se o cálculo gera exatamente 900 registros para 30 meses.
        /// Requisito: 30 meses * 30 dias = 900 registros diários.
        /// </summary>
        [Fact]
        public void Calcular_DeveGerar900Registros_Para30Meses()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-001"
            };

            // Act
            var registros = _service.Calcular(request);

            // Assert
            Assert.Equal(900, registros.Count);
        }

        /// <summary>
        /// Verifica se a prestação é fixa nos dias de pagamento (múltiplos de 30).
        /// No método PRICE, a prestação deve ser constante.
        /// </summary>
        [Fact]
        public void Calcular_PrestacaoDeveSerFixa_NosDiasDePagamento()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-002"
            };

            // Act
            var registros = _service.Calcular(request);

            // Filtra apenas os dias de pagamento (múltiplos de 30)
            var diasPagamento = registros.Where(r => r.Dia % 30 == 0).ToList();

            // Assert - Todas as prestações devem ser iguais
            var prestacaoReferencia = diasPagamento.First().Prestacao;
            Assert.All(diasPagamento, r =>
                Assert.Equal(prestacaoReferencia, r.Prestacao));
        }

        /// <summary>
        /// Verifica se o saldo devedor é zero (ou próximo) no último dia.
        /// O contrato deve ser totalmente quitado ao final do prazo.
        /// </summary>
        [Fact]
        public void Calcular_SaldoDevedorDeveSerZero_NoUltimoDia()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-003"
            };

            // Act
            var registros = _service.Calcular(request);
            var ultimoDiaPagamento = registros.Last(r => r.Dia % 30 == 0);

            // Assert - Saldo deve ser zero ou muito próximo
            Assert.True(ultimoDiaPagamento.SaldoAposPagar <= 0.01m,
                $"Saldo devedor no último dia: {ultimoDiaPagamento.SaldoAposPagar}");
        }

        /// <summary>
        /// Verifica a coerência: Prestação = Juros + Amortização nos dias de pagamento.
        /// </summary>
        [Fact]
        public void Calcular_PrestacaoDeveSerIgual_JurosMaisAmortizacao()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-004"
            };

            // Act
            var registros = _service.Calcular(request);
            var diasPagamento = registros.Where(r => r.Dia % 30 == 0).ToList();

            // Assert - Para cada dia de pagamento: Prestação = Juros + Amortização
            foreach (var registro in diasPagamento)
            {
                decimal soma = registro.JurosPeriodo + registro.Amortizacao;
                Assert.True(
                    Math.Abs(registro.Prestacao - soma) <= 0.02m,
                    $"Dia {registro.Dia}: Prestação={registro.Prestacao}, " +
                    $"Juros+Amortização={soma}");
            }
        }

        /// <summary>
        /// Verifica se os juros decrescem ao longo do contrato (característica PRICE).
        /// No método PRICE, os juros diminuem a cada período.
        /// </summary>
        [Fact]
        public void Calcular_JurosDevemDecrescer_AoLongoDoContrato()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-005"
            };

            // Act
            var registros = _service.Calcular(request);
            var diasPagamento = registros.Where(r => r.Dia % 30 == 0).ToList();

            // Assert - Juros do primeiro período > juros do último período
            Assert.True(
                diasPagamento.First().JurosPeriodo > diasPagamento.Last().JurosPeriodo,
                "Juros devem decrescer ao longo do contrato no método PRICE.");
        }

        /// <summary>
        /// Verifica se a amortização cresce ao longo do contrato (característica PRICE).
        /// No método PRICE, a amortização aumenta a cada período.
        /// </summary>
        [Fact]
        public void Calcular_AmortizacaoDeveCrescer_AoLongoDoContrato()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-006"
            };

            // Act
            var registros = _service.Calcular(request);
            var diasPagamento = registros.Where(r => r.Dia % 30 == 0).ToList();

            // Assert - Amortização do primeiro período < amortização do último
            Assert.True(
                diasPagamento.First().Amortizacao < diasPagamento.Last().Amortizacao,
                "Amortização deve crescer ao longo do contrato no método PRICE.");
        }

        /// <summary>
        /// Verifica se dias sem pagamento têm prestação e amortização zeradas.
        /// </summary>
        [Fact]
        public void Calcular_DiasSemPagamento_DevemTerPrestacaoZero()
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = 10000m,
                TaxaJurosMensal = 1.8m,
                PrazoMeses = 30,
                MessageId = "test-007"
            };

            // Act
            var registros = _service.Calcular(request);
            var diasSemPagamento = registros.Where(r => r.Dia % 30 != 0).ToList();

            // Assert
            Assert.All(diasSemPagamento, r =>
            {
                Assert.Equal(0m, r.Prestacao);
                Assert.Equal(0m, r.Amortizacao);
            });
        }

        /// <summary>
        /// Verifica o cálculo com diferentes parâmetros de entrada.
        /// Testa com prazo de 12 meses (360 registros).
        /// </summary>
        [Theory]
        [InlineData(10000, 1.8, 30, 900)]
        [InlineData(50000, 2.0, 12, 360)]
        [InlineData(100000, 1.5, 60, 1800)]
        public void Calcular_DeveGerarQuantidadeCorretaDeRegistros(
            decimal valor, decimal taxa, int prazo, int esperado)
        {
            // Arrange
            var request = new ContratoRequest
            {
                ValorEmprestimo = valor,
                TaxaJurosMensal = taxa,
                PrazoMeses = prazo,
                MessageId = $"test-param-{prazo}"
            };

            // Act
            var registros = _service.Calcular(request);

            // Assert
            Assert.Equal(esperado, registros.Count);
        }
    }
}
