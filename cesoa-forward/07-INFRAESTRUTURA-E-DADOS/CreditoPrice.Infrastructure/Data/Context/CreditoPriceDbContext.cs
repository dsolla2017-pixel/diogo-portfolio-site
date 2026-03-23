// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: CreditoPriceDbContext.cs
// Descrição: Contexto do banco de dados — mapeia entidades para tabelas SQL Server
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Ferramenta que traduz código C# em comandos SQL automaticamente
//   • Contexto do Banco — é a 'porta de entrada' para acessar o banco de dados
//   • Idempotência — garante que processar a mesma mensagem duas vezes não cause duplicidade
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using CreditoPrice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditoPrice.Infrastructure.Data.Context
{
    /// <summary>
    /// Contexto principal do Entity Framework Core.
    /// Gerencia a conexão com o banco de dados e o mapeamento ORM.
    /// </summary>
    public class CreditoPriceDbContext : DbContext
    {
        /// <summary>
        /// Inicializa o contexto com as opções de configuração injetadas.
        /// </summary>
        public CreditoPriceDbContext(DbContextOptions<CreditoPriceDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// DbSet para a tabela EvolucaoContrato.
        /// Representa a coleção de registros diários de evolução dos contratos.
        /// </summary>
        public DbSet<EvolucaoContrato> EvolucaoContratos { get; set; } = null!;

        /// <summary>
        /// Configuração do modelo via Fluent API.
        /// Define índices, constraints e otimizações de performance.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EvolucaoContrato>(entity =>
            {
                // Nome da tabela no banco de dados
                entity.ToTable("EvolucaoContrato");

                // Chave primária
                entity.HasKey(e => e.Id);

                // Índice composto para consultas por contrato e dia
                // Otimiza queries de busca e validação de idempotência
                entity.HasIndex(e => new { e.ContratoId, e.Dia })
                      .HasDatabaseName("IX_EvolucaoContrato_ContratoId_Dia")
                      .IsUnique();

                // Índice para consultas por ContratoId (idempotência)
                entity.HasIndex(e => e.ContratoId)
                      .HasDatabaseName("IX_EvolucaoContrato_ContratoId");

                // Índice para consultas por data de processamento (auditoria)
                entity.HasIndex(e => e.DataProcessamento)
                      .HasDatabaseName("IX_EvolucaoContrato_DataProcessamento");

                // Configuração de precisão para campos monetários
                entity.Property(e => e.Prestacao)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.JurosPeriodo)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.Amortizacao)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.SaldoAposPagar)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.ContratoId)
                      .HasMaxLength(100)
                      .IsRequired();
            });
        }
    }
}
