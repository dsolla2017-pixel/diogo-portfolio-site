// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: EvolucaoContratoRepository.cs
// Descrição: Repositório que persiste e consulta dados de evolução de contratos no banco
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Entity Framework Core — ferramenta que traduz código C# em comandos SQL automaticamente
//   • Ferramenta que traduz código C# em comandos SQL automaticamente
//   • Contexto do Banco — é a 'porta de entrada' para acessar o banco de dados
//   • Idempotência — garante que processar a mesma mensagem duas vezes não cause duplicidade
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repositório para persistência de registros de evolução de contrato.
    /// Utiliza Entity Framework Core para acesso ao SQL Server.
    /// </summary>
    public class EvolucaoContratoRepository : IEvolucaoContratoRepository
    {
        private readonly CreditoPriceDbContext _context;
        private readonly ILogger<EvolucaoContratoRepository> _logger;

        /// <summary>
        /// Inicializa o repositório com injeção de dependência.
        /// </summary>
        public EvolucaoContratoRepository(
            CreditoPriceDbContext context,
            ILogger<EvolucaoContratoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Persiste uma lista de registros diários em lote.
        /// Utiliza transação implícita do EF Core para garantir atomicidade.
        /// Em caso de falha, nenhum registro é persistido (rollback automático).
        /// </summary>
        public async Task InserirLoteAsync(
            IEnumerable<EvolucaoContrato> registros,
            CancellationToken cancellationToken = default)
        {
            var listaRegistros = registros.ToList();

            _logger.LogInformation(
                "Persistindo {Quantidade} registros de evolução. ContratoId: {ContratoId}",
                listaRegistros.Count,
                listaRegistros.FirstOrDefault()?.ContratoId ?? "N/A");

            // Inserção em lote com transação implícita
            await _context.EvolucaoContratos.AddRangeAsync(listaRegistros, cancellationToken);
            int registrosSalvos = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Persistência concluída. Registros salvos: {Total}", registrosSalvos);
        }

        /// <summary>
        /// Verifica se um contrato já foi processado (controle de idempotência).
        /// Consulta o índice IX_EvolucaoContrato_ContratoId para performance.
        /// </summary>
        public async Task<bool> ContratoJaProcessadoAsync(
            string contratoId,
            CancellationToken cancellationToken = default)
        {
            bool existe = await _context.EvolucaoContratos
                .AnyAsync(e => e.ContratoId == contratoId, cancellationToken);

            if (existe)
            {
                _logger.LogWarning(
                    "Contrato já processado (idempotência). ContratoId: {ContratoId}",
                    contratoId);
            }

            return existe;
        }

        /// <summary>
        /// Recupera todos os registros de evolução de um contrato,
        /// ordenados por dia, para consulta e validação.
        /// </summary>
        public async Task<IEnumerable<EvolucaoContrato>> ObterPorContratoAsync(
            string contratoId,
            CancellationToken cancellationToken = default)
        {
            return await _context.EvolucaoContratos
                .Where(e => e.ContratoId == contratoId)
                .OrderBy(e => e.Dia)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
