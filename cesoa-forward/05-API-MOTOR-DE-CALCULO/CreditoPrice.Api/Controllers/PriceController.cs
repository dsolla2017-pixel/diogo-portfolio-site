// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: PriceController.cs
// Descrição: Controlador REST que expõe os endpoints de cálculo PRICE e simulação de crédito
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//   • Amortização — parte da prestação que efetivamente reduz a dívida
//

using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CreditoPrice.Api.Controllers
{
    /// <summary>
    /// Controller responsável pelo cálculo de amortização pelo método PRICE.
    /// Expõe o endpoint principal consumido pelo Worker Service.
    /// </summary>
    [ApiController]
    [Route("api/price")]
    [Produces("application/json")]
    public class PriceController : ControllerBase
    {
        private readonly ICalculoPriceService _calculoService;
        private readonly ILogger<PriceController> _logger;

        /// <summary>
        /// Inicializa o controller com injeção de dependência.
        /// </summary>
        /// <param name="calculoService">Serviço de cálculo PRICE.</param>
        /// <param name="logger">Logger para rastreabilidade.</param>
        public PriceController(
            ICalculoPriceService calculoService,
            ILogger<PriceController> logger)
        {
            _calculoService = calculoService;
            _logger = logger;
        }

        /// <summary>
        /// Executa o cálculo de amortização pelo método PRICE.
        /// 
        /// Recebe os parâmetros do contrato (valor, taxa, prazo) e retorna
        /// a lista completa de registros diários de evolução.
        /// 
        /// Exemplo de entrada:
        /// {
        ///   "valorEmprestimo": 10000,
        ///   "taxaJurosMensal": 1.8,
        ///   "prazoMeses": 30
        /// }
        /// 
        /// Retorna 900 registros (30 meses * 30 dias) com:
        /// dia, prestacao, jurosPeriodo, amortizacao, saldoAposPagar.
        /// </summary>
        /// <param name="request">Dados do contrato de financiamento.</param>
        /// <returns>Lista de registros diários de evolução do contrato.</returns>
        /// <response code="200">Cálculo realizado com sucesso.</response>
        /// <response code="400">Dados de entrada inválidos.</response>
        /// <response code="500">Erro interno no processamento.</response>
        [HttpPost("calcular")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Calcular([FromBody] ContratoRequest request)
        {
            // -----------------------------------------------------------------
            // VALIDAÇÃO 1: Model Binding (DataAnnotations)
            // O ASP.NET Core valida automaticamente os atributos [Required],
            // [Range] etc. definidos no ContratoRequest.
            // -----------------------------------------------------------------
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Requisição inválida recebida. Erros: {Erros}",
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));

                return BadRequest(ModelState);
            }

            // -----------------------------------------------------------------
            // VALIDAÇÃO 2: Regras de negócio adicionais
            // Complementa a validação de DataAnnotations com regras de domínio.
            // -----------------------------------------------------------------
            if (!request.EhValido())
            {
                _logger.LogWarning(
                    "Contrato com valores inválidos: {Contrato}", request.ToString());

                return BadRequest(new
                {
                    erro = "Dados do contrato inválidos.",
                    detalhes = "Todos os valores devem ser positivos e o prazo maior que zero."
                });
            }

            try
            {
                _logger.LogInformation(
                    "Processando cálculo PRICE: {Contrato}", request.ToString());

                // -------------------------------------------------------------
                // EXECUÇÃO DO CÁLCULO
                // Delega ao serviço de domínio a lógica financeira.
                // -------------------------------------------------------------
                var registros = _calculoService.Calcular(request);

                _logger.LogInformation(
                    "Cálculo PRICE concluído com sucesso. Registros: {Total}",
                    registros.Count);

                // Retorna a lista de registros diários com os campos esperados
                var resultado = registros.Select(r => new
                {
                    dia = r.Dia,
                    prestacao = r.Prestacao,
                    jurosPeriodo = r.JurosPeriodo,
                    amortizacao = r.Amortizacao,
                    saldoAposPagar = r.SaldoAposPagar
                });

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao processar cálculo PRICE para contrato: {Contrato}",
                    request.ToString());

                return StatusCode(500, new
                {
                    erro = "Erro interno ao processar o cálculo.",
                    correlationId = Guid.NewGuid().ToString()
                });
            }
        }

        /// <summary>
        /// Health check do serviço de cálculo.
        /// Permite verificação de disponibilidade pelo Worker e por
        /// ferramentas de monitoramento (Azure Monitor, Application Insights).
        /// </summary>
        /// <returns>Status de saúde do serviço.</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                servico = "CreditoPrice.Api",
                timestamp = DateTime.UtcNow,
                versao = "1.0.0"
            });
        }
    }
}
