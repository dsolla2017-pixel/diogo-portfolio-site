-- ============================================================================
-- Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
-- Autor:   Grawingholt, Diogo
-- Data:    Março 2026
-- Descrição: Script de criação da tabela de evolução de contratos de crédito
-- ============================================================================

-- =============================================================================
-- CAIXA ECONÔMICA FEDERAL - DESAFIO TÉCNICO PSI-CTI
-- Script: 001_CriarTabelaEvolucaoContrato.sql
-- Descrição: Criação da tabela EvolucaoContrato no SQL Server.
--            Armazena a evolução diária de contratos de financiamento
--            calculados pelo método PRICE (uma linha por dia).
-- =============================================================================
-- Referência: Requisito do desafio (seção 6 - Persistência no Banco de Dados)
-- Campos: Id, ContratoId, Dia, Prestacao, JurosPeriodo, Amortizacao,
--          SaldoAposPagar, DataProcessamento
-- Tipos monetários: decimal(18,2) conforme padrão bancário
-- =============================================================================

-- Verifica se a tabela já existe antes de criar (idempotência do script)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EvolucaoContrato]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EvolucaoContrato]
    (
        -- Chave primária auto-incremento (bigint para alto volume)
        [Id]                    BIGINT          IDENTITY(1,1)   NOT NULL,

        -- Identificador do contrato (vincula todos os registros diários)
        [ContratoId]            NVARCHAR(100)                   NOT NULL,

        -- Dia do contrato (1 a N, onde N = prazoMeses * 30)
        [Dia]                   INT                             NOT NULL,

        -- Valor da prestação fixa (método PRICE)
        [Prestacao]             DECIMAL(18,2)                   NOT NULL,

        -- Juros acumulados no período
        [JurosPeriodo]          DECIMAL(18,2)                   NOT NULL,

        -- Valor da amortização
        [Amortizacao]           DECIMAL(18,2)                   NOT NULL,

        -- Saldo devedor após pagamento
        [SaldoAposPagar]        DECIMAL(18,2)                   NOT NULL,

        -- Timestamp do processamento (auditoria)
        [DataProcessamento]     DATETIME                        NOT NULL,

        -- Constraint de chave primária
        CONSTRAINT [PK_EvolucaoContrato] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Índice composto único: garante que não haja duplicação de dia por contrato
    CREATE UNIQUE NONCLUSTERED INDEX [IX_EvolucaoContrato_ContratoId_Dia]
        ON [dbo].[EvolucaoContrato] ([ContratoId] ASC, [Dia] ASC);

    -- Índice para consultas por ContratoId (verificação de idempotência)
    CREATE NONCLUSTERED INDEX [IX_EvolucaoContrato_ContratoId]
        ON [dbo].[EvolucaoContrato] ([ContratoId] ASC);

    -- Índice para consultas por data de processamento (auditoria)
    CREATE NONCLUSTERED INDEX [IX_EvolucaoContrato_DataProcessamento]
        ON [dbo].[EvolucaoContrato] ([DataProcessamento] ASC);

    PRINT 'Tabela EvolucaoContrato criada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Tabela EvolucaoContrato já existe. Nenhuma ação necessária.';
END
GO
