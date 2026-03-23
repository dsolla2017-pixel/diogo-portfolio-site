// ============================================================================
// Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
// Autor:   Grawingholt, Diogo
// Data:    Março 2026
// Arquivo: FabricLakehousePublisher.cs
// Descrição: Publicador de dados no Microsoft Fabric Lakehouse (camadas Bronze/Silver/Gold)
// ============================================================================
//
// GLOSSÁRIO PARA LEIGO — Termos técnicos usados neste arquivo:
//   • Injeção de Dependências
//   • Token de Cancelamento — permite parar uma operação de forma segura e controlada
//   • Lago de Dados — armazém inteligente que combina dados estruturados e não estruturados
//   • Arquitetura Medallion — organiza dados em 3 camadas: Bronze (bruto), Silver (limpo), Gold (pronto para uso)
//   • Tabela PRICE — método de cálculo onde a prestação é fixa do início ao fim do financiamento
//

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CreditoPrice.Domain.Entities;
using CreditoPrice.Domain.Interfaces;
using CreditoPrice.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditoPrice.Infrastructure.Fabric
{
    /// <summary>
    /// Publica dados no Microsoft Fabric Lakehouse via OneLake REST API.
    /// Implementa o padrão Medallion Architecture (Bronze/Silver/Gold)
    /// para organização dos dados no Data Lakehouse.
    /// </summary>
    public class FabricLakehousePublisher : IFabricLakehousePublisher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FabricLakehousePublisher> _logger;
        private readonly string _workspaceId;
        private readonly string _lakehouseId;
        private readonly string _oneLakeEndpoint;

        // ---------------------------------------------------------------
        // CONFIGURAÇÃO DO MICROSOFT FABRIC
        // Os parâmetros são lidos do appsettings.json e podem ser
        // sobrescritos por variáveis de ambiente (Azure App Configuration).
        // ---------------------------------------------------------------
        public FabricLakehousePublisher(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<FabricLakehousePublisher> logger)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("FabricApi");

            _workspaceId = configuration["Fabric:WorkspaceId"]
                ?? "ws-credito-psi-cti";
            _lakehouseId = configuration["Fabric:LakehouseId"]
                ?? "lh-credito-price";
            _oneLakeEndpoint = configuration["Fabric:OneLakeEndpoint"]
                ?? "https://onelake.dfs.fabric.microsoft.com";

            // Configuração de autenticação via Azure AD / Entra ID
            string? bearerToken = configuration["Fabric:BearerToken"];
            if (!string.IsNullOrEmpty(bearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            _logger.LogInformation(
                "FabricLakehousePublisher inicializado. Workspace: {Workspace}, Lakehouse: {Lakehouse}",
                _workspaceId, _lakehouseId);
        }

        /// <summary>
        /// Publica registros de evolução de contrato na camada Gold do Lakehouse.
        /// Os dados são particionados por ContratoId e data de processamento
        /// para otimizar consultas analíticas no Power BI.
        ///
        /// Tabela destino: Tables/gold_evolucao_contrato/
        /// Formato: Delta Lake (JSON para ingestão, convertido por Spark)
        /// Particionamento: ano_mes={yyyy-MM}/contrato_id={id}
        /// </summary>
        public async Task PublicarEvolucaoContratoAsync(
            IEnumerable<EvolucaoContrato> registros,
            string contratoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var listaRegistros = registros.ToList();
                string anoMes = DateTime.UtcNow.ToString("yyyy-MM");

                _logger.LogInformation(
                    "[FABRIC] Publicando {Qtd} registros de evolução. ContratoId: {ContratoId}, Partição: {Particao}",
                    listaRegistros.Count, contratoId, anoMes);

                // Monta o payload no formato esperado pelo Lakehouse
                var payload = listaRegistros.Select(r => new
                {
                    contrato_id = r.ContratoId,
                    dia = r.Dia,
                    prestacao = r.Prestacao,
                    juros_periodo = r.JurosPeriodo,
                    amortizacao = r.Amortizacao,
                    saldo_apos_pagar = r.SaldoAposPagar,
                    data_processamento = r.DataProcessamento.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ano_mes = anoMes,
                    // Campos adicionais para enriquecimento analítico
                    is_dia_pagamento = r.Dia % 30 == 0,
                    numero_parcela = r.Dia % 30 == 0 ? r.Dia / 30 : 0,
                    camada = "gold"
                });

                string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = false
                });

                // Caminho no OneLake (Medallion Architecture - Gold Layer)
                string filePath = $"{_workspaceId}/{_lakehouseId}/Tables/" +
                                  $"gold_evolucao_contrato/ano_mes={anoMes}/" +
                                  $"contrato_{contratoId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                string requestUrl = $"{_oneLakeEndpoint}/{filePath}?resource=file";

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(requestUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "[FABRIC] Evolução do contrato publicada com sucesso. Path: {Path}",
                        filePath);
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "[FABRIC] Falha na publicação (HTTP {Status}). Path: {Path}, Erro: {Erro}",
                        response.StatusCode, filePath, errorBody);
                }
            }
            catch (Exception ex)
            {
                // A falha no Fabric não deve interromper o fluxo principal
                _logger.LogError(ex,
                    "[FABRIC] Erro ao publicar evolução do contrato. ContratoId: {ContratoId}",
                    contratoId);
            }
        }

        /// <summary>
        /// Publica métricas de processamento na camada Gold do Lakehouse.
        /// Alimenta o dashboard de produtividade e SLA.
        ///
        /// Tabela destino: Tables/gold_metricas_processamento/
        /// Campos: correlation_id, status, tempo_ms, qtd_registros, timestamp
        /// </summary>
        public async Task PublicarMetricaProcessamentoAsync(
            EventoProcessamento metrica,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[FABRIC] Publicando métrica de processamento. CorrelationId: {Id}, Status: {Status}",
                    metrica.CorrelationId, metrica.Status);

                var payload = new
                {
                    correlation_id = metrica.CorrelationId,
                    acao = metrica.Acao,
                    status = metrica.Status.ToString(),
                    mensagem_processada = metrica.MensagemProcessada,
                    timestamp = metrica.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    quantidade_registros = metrica.QuantidadeRegistros,
                    tempo_processamento_ms = metrica.TempoProcessamentoMs,
                    detalhes_erro = metrica.DetalhesErro ?? "",
                    // Campos calculados para o dashboard
                    tempo_processamento_seg = metrica.TempoProcessamentoMs / 1000.0,
                    hora_processamento = metrica.Timestamp.Hour,
                    dia_semana = metrica.Timestamp.DayOfWeek.ToString(),
                    camada = "gold"
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                string filePath = $"{_workspaceId}/{_lakehouseId}/Tables/" +
                                  $"gold_metricas_processamento/" +
                                  $"metrica_{metrica.CorrelationId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                string requestUrl = $"{_oneLakeEndpoint}/{filePath}?resource=file";
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                await _httpClient.PutAsync(requestUrl, content, cancellationToken);

                _logger.LogInformation("[FABRIC] Métrica publicada com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FABRIC] Erro ao publicar métrica de processamento.");
            }
        }

        /// <summary>
        /// Publica métricas do Agente de IA na camada Gold do Lakehouse.
        /// Alimenta o dashboard de ROI e eliminação de rotinas repetitivas.
        ///
        /// Tabela destino: Tables/gold_agente_ia_metricas/
        /// Campos: tarefa, tempo_economizado, custo_evitado, data, agente_versao
        /// </summary>
        public async Task PublicarMetricasAgenteIAAsync(
            Dictionary<string, object> metricas,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[FABRIC] Publicando {Qtd} métricas do Agente de IA.",
                    metricas.Count);

                // Enriquece com metadados de rastreabilidade
                metricas["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                metricas["camada"] = "gold";
                metricas["fonte"] = "CreditoPrice.AiAgent";

                string jsonPayload = JsonSerializer.Serialize(metricas);
                string filePath = $"{_workspaceId}/{_lakehouseId}/Tables/" +
                                  $"gold_agente_ia_metricas/" +
                                  $"agente_ia_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                string requestUrl = $"{_oneLakeEndpoint}/{filePath}?resource=file";
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                await _httpClient.PutAsync(requestUrl, content, cancellationToken);

                _logger.LogInformation("[FABRIC] Métricas do Agente de IA publicadas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FABRIC] Erro ao publicar métricas do Agente de IA.");
            }
        }
    }
}
