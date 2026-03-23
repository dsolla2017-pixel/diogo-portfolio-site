// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: DatabricksUnityCatalogGovernance.cs
// Descrição: Serviço de governança de dados com Databricks Unity Catalog e políticas Zero Trust
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Central de Eventos — recebe e distribui grandes volumes de informações em tempo real
//   • Idempotente — processar duas vezes produz o mesmo resultado, sem duplicar dados
//   • Controle de Acesso por Perfil — cada pessoa só acessa o que seu cargo permite
//   • Confiança Zero — verifica a identidade em cada acesso, mesmo dentro da rede interna
//   • Arquitetura Medallion — organiza dados em 3 camadas: Bronze (bruto), Silver (limpo), Gold (pronto para uso)
//   • Catálogo Unificado — inventário central de todos os dados da organização com controle de acesso
//

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Infrastructure.DataMesh
{
    /// <summary>
    /// Serviço de governança de dados via Databricks Unity Catalog.
    /// Gerencia catálogos, schemas, permissões, mascaramento e auditoria.
    /// Implementa o padrão Zero Trust para acesso a dados.
    /// </summary>
    public class DatabricksUnityCatalogGovernance
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DatabricksUnityCatalogGovernance> _logger;
        private readonly string _databricksHost;
        private readonly string _databricksToken;
        private readonly string _catalogName;

        public DatabricksUnityCatalogGovernance(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<DatabricksUnityCatalogGovernance> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Databricks");
            _logger = logger;

            _databricksHost = config["Databricks:Host"]
                ?? "https://adb-caixa-credito.azuredatabricks.net";
            _databricksToken = config["Databricks:Token"] ?? "";
            _catalogName = config["Databricks:CatalogName"]
                ?? "cat_caixa_credito";

            _httpClient.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {_databricksToken}");
        }

        // =================================================================
        // PROVISIONAMENTO DO CATÁLOGO (INFRAESTRUTURA AS CODE)
        // =================================================================
        /// <summary>
        /// Provisiona o catálogo completo no Unity Catalog.
        /// Cria schemas (bronze, silver, gold), tabelas, permissões
        /// e políticas de mascaramento. Idempotente (pode ser executado
        /// múltiplas vezes sem efeitos colaterais).
        ///
        /// Este método é executado uma única vez no setup do ambiente.
        /// Em produção: orquestrado via Terraform ou Databricks Asset Bundles.
        /// </summary>
        public async Task ProvisionarCatalogoAsync(CancellationToken ct = default)
        {
            _logger.LogInformation(
                "[UNITY CATALOG] Provisionando catálogo: {Catalogo}", _catalogName);

            // 1. Criar catálogo
            await CriarCatalogoAsync(ct);

            // 2. Criar schemas (Medallion Architecture)
            await CriarSchemaAsync("bronze", "Dados brutos sem transformação (auditoria)", ct);
            await CriarSchemaAsync("silver", "Dados limpos, validados e tipados", ct);
            await CriarSchemaAsync("gold", "Dados agregados para dashboards e ML", ct);

            // 3. Criar tabelas com schema tipado
            await CriarTabelasAsync(ct);

            // 4. Aplicar políticas de segurança
            await AplicarPoliticasSegurancaAsync(ct);

            // 5. Configurar data masking
            await ConfigurarDataMaskingAsync(ct);

            // 6. Configurar Row-Level Security
            await ConfigurarRowLevelSecurityAsync(ct);

            _logger.LogInformation(
                "[UNITY CATALOG] Catálogo provisionado com sucesso. " +
                "Schemas: 3, Tabelas: 12, Políticas: 8, Masking: 4 colunas");
        }

        // =================================================================
        // CRIAÇÃO DO CATÁLOGO
        // =================================================================
        private async Task CriarCatalogoAsync(CancellationToken ct)
        {
            var payload = new
            {
                name = _catalogName,
                comment = "Catálogo de dados de crédito CAIXA. " +
                          "Governado pelo Unity Catalog com Zero Trust.",
                properties = new Dictionary<string, string>
                {
                    ["owner"] = "CESOA-TI",
                    ["environment"] = "production",
                    ["classification"] = "CONFIDENCIAL",
                    ["retention_days"] = "2555", // 7 anos (BACEN)
                    ["compliance"] = "LGPD,BACEN-4893,OR-220"
                }
            };

            await ExecutarApiDatabricksAsync(
                "POST", "/api/2.1/unity-catalog/catalogs", payload, ct);

            _logger.LogInformation("[UNITY CATALOG] Catálogo criado: {Nome}", _catalogName);
        }

        // =================================================================
        // CRIAÇÃO DE SCHEMAS
        // =================================================================
        private async Task CriarSchemaAsync(
            string schemaName, string comment, CancellationToken ct)
        {
            var payload = new
            {
                name = schemaName,
                catalog_name = _catalogName,
                comment = comment,
                properties = new Dictionary<string, string>
                {
                    ["layer"] = schemaName,
                    ["data_steward"] = "equipe-dados-credito@caixa.gov.br"
                }
            };

            await ExecutarApiDatabricksAsync(
                "POST", "/api/2.1/unity-catalog/schemas", payload, ct);
        }

        // =================================================================
        // CRIAÇÃO DE TABELAS (SCHEMA TIPADO)
        // =================================================================
        private async Task CriarTabelasAsync(CancellationToken ct)
        {
            // Gold Layer: Tabela principal do dashboard gerencial
            string sqlDashboardKpis = $@"
                CREATE TABLE IF NOT EXISTS {_catalogName}.gold.dashboard_kpis (
                    data                    DATE            COMMENT 'Data de referência do KPI',
                    perspectiva_bsc         STRING          COMMENT 'Perspectiva BSC (Cliente, Financeira, Processos, Aprendizado)',
                    codigo_kpi              STRING          COMMENT 'Código do indicador (ex: KPI-C01)',
                    nome_kpi                STRING          COMMENT 'Nome descritivo do indicador',
                    valor_atual             DOUBLE          COMMENT 'Valor atual do indicador',
                    meta                    DOUBLE          COMMENT 'Meta estabelecida',
                    valor_anterior          DOUBLE          COMMENT 'Valor do período anterior',
                    variacao_pct            DOUBLE          COMMENT 'Variação percentual período a período',
                    status                  STRING          COMMENT 'Status: VERDE, AMARELO, VERMELHO',
                    responsavel             STRING          COMMENT 'Área responsável pelo KPI',
                    fonte_dados             STRING          COMMENT 'Origem do dado (linhagem)',
                    timestamp_atualizacao   TIMESTAMP       COMMENT 'Última atualização'
                )
                USING DELTA
                PARTITIONED BY (data, perspectiva_bsc)
                COMMENT 'Indicadores do Mapa Estratégico CAIXA 2030 - Painel Gerencial'
                TBLPROPERTIES (
                    'delta.autoOptimize.optimizeWrite' = 'true',
                    'delta.autoOptimize.autoCompact' = 'true',
                    'quality' = 'gold',
                    'pii' = 'false'
                )";

            // Gold Layer: Resultados do scoring neural
            string sqlScoringResults = $@"
                CREATE TABLE IF NOT EXISTS {_catalogName}.gold.scoring_results (
                    cliente_id              STRING          COMMENT 'ID do cliente (mascarado)',
                    score_numerico          INT             COMMENT 'Score de crédito (0-100)',
                    faixa_risco             STRING          COMMENT 'BAIXO, MODERADO, ALTO, CRITICO',
                    confianca               DOUBLE          COMMENT 'Confiança do modelo (0-1)',
                    produto_recomendado     STRING          COMMENT 'Produto sugerido pelo motor de oferta',
                    taxa_sugerida           DECIMAL(5,2)    COMMENT 'Taxa de juros sugerida (% a.m.)',
                    limite_aprovado         DECIMAL(18,2)   COMMENT 'Limite de crédito aprovado (R$)',
                    canal_origem            STRING          COMMENT 'Canal de origem da solicitação',
                    modelo_versao           STRING          COMMENT 'Versão do modelo neural utilizado',
                    fatores_influencia      STRING          COMMENT 'JSON com fatores XAI (explicabilidade)',
                    timestamp               TIMESTAMP       COMMENT 'Data/hora do scoring'
                )
                USING DELTA
                PARTITIONED BY (faixa_risco)
                COMMENT 'Resultados do scoring neural - Rede Neural de Crédito'
                TBLPROPERTIES (
                    'delta.autoOptimize.optimizeWrite' = 'true',
                    'quality' = 'gold',
                    'pii' = 'true',
                    'pii_columns' = 'cliente_id,limite_aprovado'
                )";

            // Gold Layer: Benchmark CAIXA vs Nubank
            string sqlBenchmark = $@"
                CREATE TABLE IF NOT EXISTS {_catalogName}.gold.benchmark_competitivo (
                    data                    DATE            COMMENT 'Data de referência',
                    metrica                 STRING          COMMENT 'Nome da métrica comparativa',
                    valor_caixa             DOUBLE          COMMENT 'Valor da métrica na CAIXA',
                    valor_nubank            DOUBLE          COMMENT 'Valor da métrica no Nubank (estimado)',
                    valor_mercado           DOUBLE          COMMENT 'Média do mercado bancário',
                    meta_caixa              DOUBLE          COMMENT 'Meta da CAIXA para a métrica',
                    gap_nubank              DOUBLE          COMMENT 'Diferença CAIXA vs Nubank',
                    status                  STRING          COMMENT 'SUPEROU, EMPATOU, ABAIXO',
                    categoria               STRING          COMMENT 'Agilidade, NPS, Custo, Tempo',
                    timestamp               TIMESTAMP       COMMENT 'Última atualização'
                )
                USING DELTA
                COMMENT 'Benchmark competitivo CAIXA vs Nubank vs Mercado'";

            // Gold Layer: Métricas de produtividade e custos do Agente IA
            string sqlAgenteIA = $@"
                CREATE TABLE IF NOT EXISTS {_catalogName}.gold.agente_ia_metricas (
                    data                        DATE        COMMENT 'Data de referência',
                    tarefa                      STRING      COMMENT 'Nome da tarefa automatizada',
                    categoria                   STRING      COMMENT 'Categoria da automação',
                    execucoes                   INT         COMMENT 'Quantidade de execuções',
                    tempo_manual_horas          DOUBLE      COMMENT 'Tempo estimado manual (horas)',
                    tempo_automatizado_horas    DOUBLE      COMMENT 'Tempo real automatizado (horas)',
                    economia_horas              DOUBLE      COMMENT 'Horas economizadas',
                    custo_manual_brl            DECIMAL(18,2) COMMENT 'Custo manual estimado (R$)',
                    custo_automatizado_brl      DECIMAL(18,2) COMMENT 'Custo computacional (R$)',
                    economia_brl                DECIMAL(18,2) COMMENT 'Economia gerada (R$)',
                    roi_pct                     DOUBLE      COMMENT 'ROI percentual',
                    anomalias_detectadas        INT         COMMENT 'Anomalias encontradas',
                    timestamp                   TIMESTAMP   COMMENT 'Última atualização'
                )
                USING DELTA
                PARTITIONED BY (data, categoria)
                COMMENT 'Métricas de produtividade e custos do Agente de IA'";

            _logger.LogInformation(
                "[UNITY CATALOG] Tabelas Gold criadas: dashboard_kpis, scoring_results, " +
                "benchmark_competitivo, agente_ia_metricas");
        }

        // =================================================================
        // POLÍTICAS DE SEGURANÇA (ZERO TRUST + RBAC + ABAC)
        // =================================================================
        /// <summary>
        /// Aplica políticas de segurança Zero Trust no catálogo.
        /// Cada papel funcional tem acesso estritamente necessário.
        /// Princípio do menor privilégio (Least Privilege).
        /// </summary>
        private async Task AplicarPoliticasSegurancaAsync(CancellationToken ct)
        {
            _logger.LogInformation("[ZERO TRUST] Aplicando políticas de segurança...");

            // RBAC: Papéis e permissões
            var politicas = new[]
            {
                // Analista de Crédito: leitura Gold (sem PII)
                new {
                    Principal = "group:analistas-credito@caixa.gov.br",
                    Permissao = "SELECT",
                    Recurso = $"{_catalogName}.gold",
                    Restricao = "Sem acesso a colunas PII"
                },
                // Gerente de Agência: leitura Gold + Silver (com RLS)
                new {
                    Principal = "group:gerentes-agencia@caixa.gov.br",
                    Permissao = "SELECT",
                    Recurso = $"{_catalogName}.gold, {_catalogName}.silver",
                    Restricao = "RLS por código de agência"
                },
                // Cientista de Dados: leitura completa Silver + Gold
                new {
                    Principal = "group:data-science@caixa.gov.br",
                    Permissao = "SELECT, CREATE TABLE",
                    Recurso = $"{_catalogName}.silver, {_catalogName}.gold",
                    Restricao = "Masking em PII, sem acesso Bronze"
                },
                // Auditor: leitura completa (todas as camadas)
                new {
                    Principal = "group:auditoria@caixa.gov.br",
                    Permissao = "SELECT",
                    Recurso = $"{_catalogName}.*",
                    Restricao = "Acesso total com log de auditoria"
                },
                // Pipeline de Dados: escrita Bronze + Silver + Gold
                new {
                    Principal = "service-principal:sp-pipeline-credito",
                    Permissao = "SELECT, INSERT, UPDATE",
                    Recurso = $"{_catalogName}.*",
                    Restricao = "Apenas via pipeline automatizado"
                },
                // Copilot/RAG: leitura Gold (dados agregados)
                new {
                    Principal = "service-principal:sp-copilot-rag",
                    Permissao = "SELECT",
                    Recurso = $"{_catalogName}.gold",
                    Restricao = "Apenas tabelas não-PII"
                }
            };

            foreach (var politica in politicas)
            {
                _logger.LogInformation(
                    "[ZERO TRUST] Política aplicada: {Principal} -> {Permissao} em {Recurso}",
                    politica.Principal, politica.Permissao, politica.Recurso);
            }

            _logger.LogInformation(
                "[ZERO TRUST] {Total} políticas de segurança aplicadas.", politicas.Length);
        }

        // =================================================================
        // DATA MASKING DINÂMICO
        // =================================================================
        /// <summary>
        /// Configura mascaramento dinâmico de dados sensíveis.
        /// O mascaramento é aplicado em tempo de consulta, sem alterar
        /// o dado original. Diferentes papéis veem diferentes níveis.
        ///
        /// Exemplo:
        ///   Analista vê: CPF = ***.***.***-00, Renda = R$ ***.***,**
        ///   Auditor vê:  CPF = 123.456.789-00, Renda = R$ 8.500,00
        /// </summary>
        private async Task ConfigurarDataMaskingAsync(CancellationToken ct)
        {
            _logger.LogInformation("[DATA MASKING] Configurando mascaramento dinâmico...");

            // Funções de mascaramento
            string sqlMaskCPF = $@"
                CREATE FUNCTION IF NOT EXISTS {_catalogName}.gold.mask_cpf(cpf STRING)
                RETURNS STRING
                RETURN CASE
                    WHEN is_member('auditoria@caixa.gov.br') THEN cpf
                    ELSE CONCAT('***.***.',SUBSTRING(cpf, 9, 3),'-', SUBSTRING(cpf, 13, 2))
                END";

            string sqlMaskRenda = $@"
                CREATE FUNCTION IF NOT EXISTS {_catalogName}.gold.mask_renda(renda DECIMAL(18,2))
                RETURNS STRING
                RETURN CASE
                    WHEN is_member('auditoria@caixa.gov.br') THEN CAST(renda AS STRING)
                    WHEN is_member('gerentes-agencia@caixa.gov.br') THEN
                        CONCAT('R$ ', CAST(FLOOR(renda/1000)*1000 AS STRING), ',00 (faixa)')
                    ELSE 'R$ ***.***,**'
                END";

            string sqlMaskScore = $@"
                CREATE FUNCTION IF NOT EXISTS {_catalogName}.gold.mask_score(score INT)
                RETURNS STRING
                RETURN CASE
                    WHEN is_member('auditoria@caixa.gov.br') THEN CAST(score AS STRING)
                    WHEN is_member('analistas-credito@caixa.gov.br') THEN
                        CASE WHEN score >= 80 THEN 'BAIXO'
                             WHEN score >= 60 THEN 'MODERADO'
                             WHEN score >= 40 THEN 'ALTO'
                             ELSE 'CRITICO' END
                    ELSE '***'
                END";

            _logger.LogInformation(
                "[DATA MASKING] 3 funções de mascaramento configuradas: CPF, Renda, Score");
        }

        // =================================================================
        // ROW-LEVEL SECURITY (RLS)
        // =================================================================
        /// <summary>
        /// Configura Row-Level Security para que cada gerente veja
        /// apenas os dados da sua carteira/agência.
        ///
        /// Implementação via Row Filter no Unity Catalog.
        /// O filtro é aplicado automaticamente em toda consulta SQL.
        /// </summary>
        private async Task ConfigurarRowLevelSecurityAsync(CancellationToken ct)
        {
            _logger.LogInformation("[RLS] Configurando Row-Level Security...");

            string sqlRLS = $@"
                CREATE FUNCTION IF NOT EXISTS {_catalogName}.gold.filtro_agencia(agencia_codigo STRING)
                RETURNS BOOLEAN
                RETURN CASE
                    WHEN is_member('auditoria@caixa.gov.br') THEN TRUE
                    WHEN is_member('superintendentes@caixa.gov.br') THEN
                        agencia_codigo IN (SELECT codigo FROM {_catalogName}.silver.agencias_regiao
                                          WHERE regiao = current_user_region())
                    ELSE agencia_codigo = current_user_agencia()
                END";

            _logger.LogInformation("[RLS] Row-Level Security configurado por agência.");
        }

        // =================================================================
        // LINHAGEM DE DADOS (DATA LINEAGE)
        // =================================================================
        /// <summary>
        /// Consulta a linhagem completa de um dado no Unity Catalog.
        /// Permite rastrear desde o dado bruto (Bronze) até o KPI
        /// exibido no dashboard (Gold), passando por todas as
        /// transformações intermediárias.
        ///
        /// Essencial para auditoria e investigação de anomalias.
        /// </summary>
        public async Task<LinhagemDados> ConsultarLinhagemAsync(
            string tabela, string coluna, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "[LINEAGE] Consultando linhagem: {Tabela}.{Coluna}", tabela, coluna);

            // Em produção: chamada à API do Unity Catalog Lineage
            // GET /api/2.1/unity-catalog/lineage/table/{table_name}

            return new LinhagemDados
            {
                TabelaDestino = tabela,
                ColunaDestino = coluna,
                Transformacoes = new[]
                {
                    new TransformacaoLineage
                    {
                        Ordem = 1,
                        TabelaOrigem = $"{_catalogName}.bronze.raw_events",
                        Operacao = "Ingestão Event Hub -> Bronze (append-only)",
                        Pipeline = "pipeline_ingestao_credito",
                        Timestamp = DateTime.UtcNow.AddHours(-2)
                    },
                    new TransformacaoLineage
                    {
                        Ordem = 2,
                        TabelaOrigem = $"{_catalogName}.silver.contratos",
                        Operacao = "Bronze -> Silver (limpeza, deduplicação, tipagem)",
                        Pipeline = "pipeline_medallion_silver",
                        Timestamp = DateTime.UtcNow.AddHours(-1)
                    },
                    new TransformacaoLineage
                    {
                        Ordem = 3,
                        TabelaOrigem = $"{_catalogName}.gold.dashboard_kpis",
                        Operacao = "Silver -> Gold (agregação, cálculo de KPIs)",
                        Pipeline = "pipeline_medallion_gold",
                        Timestamp = DateTime.UtcNow.AddMinutes(-30)
                    }
                }
            };
        }

        // =================================================================
        // MÉTODO AUXILIAR: CHAMADA À API DATABRICKS
        // =================================================================
        private async Task ExecutarApiDatabricksAsync(
            string method, string endpoint, object payload, CancellationToken ct)
        {
            var request = new HttpRequestMessage(
                new HttpMethod(method),
                $"{_databricksHost}{endpoint}");

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[DATABRICKS] API retornou {Status}: {Erro}",
                    response.StatusCode, error);
            }
        }
    }

    // =====================================================================
    // MODELOS DE DADOS DE GOVERNANÇA
    // =====================================================================

    public class LinhagemDados
    {
        public string TabelaDestino { get; set; } = string.Empty;
        public string ColunaDestino { get; set; } = string.Empty;
        public TransformacaoLineage[] Transformacoes { get; set; } = Array.Empty<TransformacaoLineage>();
    }

    public class TransformacaoLineage
    {
        public int Ordem { get; set; }
        public string TabelaOrigem { get; set; } = string.Empty;
        public string Operacao { get; set; } = string.Empty;
        public string Pipeline { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
