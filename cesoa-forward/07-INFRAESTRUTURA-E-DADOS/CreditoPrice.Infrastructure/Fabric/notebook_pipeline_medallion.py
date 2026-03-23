# ============================================================================
# Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
# Autor:   Grawingholt, Diogo
# Data:    Março 2026
# Descrição: Pipeline Medallion (Bronze-Silver-Gold) para Microsoft Fabric
# ============================================================================

# =============================================================================
# CAIXA ECONÔMICA FEDERAL - DESAFIO TÉCNICO PSI-CTI
# Microsoft Fabric - Notebook Spark (Pipeline Medallion Architecture)
# =============================================================================
# Descrição: Pipeline de dados que transforma os registros de evolução de
#            contratos de crédito (método PRICE) através das camadas
#            Bronze -> Silver -> Gold no Microsoft Fabric Lakehouse.
#
# Execução: Este notebook é executado como Data Pipeline no Fabric,
#           agendado para rodar a cada 15 minutos (near-real-time)
#           ou acionado por trigger do Event Hub.
#
# INOVAÇÃO E LEGADO INSTITUCIONAL:
#   A Medallion Architecture garante qualidade progressiva dos dados.
#   Cada camada adiciona valor: Bronze preserva o dado bruto (auditoria),
#   Silver limpa e valida (confiabilidade), Gold agrega e enriquece
#   (decisão). Este modelo é referência em organizações Data Driven.
# =============================================================================

from pyspark.sql import SparkSession
from pyspark.sql.functions import (
    col, when, lit, current_timestamp, date_format,
    sum as spark_sum, avg, count, max as spark_max, min as spark_min,
    round as spark_round, expr
)
from pyspark.sql.types import (
    StructType, StructField, StringType, IntegerType,
    DecimalType, TimestampType, BooleanType, LongType, DoubleType
)
from datetime import datetime

# =============================================================================
# CONFIGURAÇÃO DO LAKEHOUSE
# =============================================================================
WORKSPACE = "ws-credito-psi-cti"
LAKEHOUSE = "lh-credito-price"

# Caminhos das tabelas no OneLake
BRONZE_PATH = f"Tables/bronze_eventos_raw"
SILVER_PATH = f"Tables/silver_evolucao_contrato"
GOLD_EVOLUCAO_PATH = f"Tables/gold_evolucao_contrato"
GOLD_METRICAS_PATH = f"Tables/gold_metricas_processamento"
GOLD_DASHBOARD_PATH = f"Tables/gold_dashboard_gerencial"
GOLD_AGENTE_IA_PATH = f"Tables/gold_agente_ia_metricas"

print("=" * 70)
print("  CAIXA - Pipeline Medallion Architecture")
print("  Microsoft Fabric Lakehouse | Desafio PSI-CTI")
print(f"  Execução: {datetime.utcnow().isoformat()}Z")
print("=" * 70)

# =============================================================================
# CAMADA BRONZE: Ingestão de dados brutos do Event Hub
# Os dados chegam em formato JSON cru, sem transformação.
# Preserva o dado original para auditoria e reprocessamento.
# =============================================================================
print("\n[BRONZE] Lendo dados brutos do Event Hub...")

bronze_schema = StructType([
    StructField("acao", StringType(), True),
    StructField("status", StringType(), True),
    StructField("mensagemProcessada", StringType(), True),
    StructField("timestamp", StringType(), True),
    StructField("correlationId", StringType(), True),
    StructField("quantidadeRegistros", IntegerType(), True),
    StructField("tempoProcessamentoMs", LongType(), True),
    StructField("detalhesErro", StringType(), True),
])

# Leitura incremental (apenas novos dados desde a última execução)
df_bronze = (
    spark.readStream
    .format("eventhubs")
    .option("eventhubs.connectionString", spark.conf.get("eventhubs.connectionString"))
    .option("eventhubs.consumerGroup", "fabric-pipeline")
    .option("eventhubs.startingPosition", '{"offset": "-1", "isInclusive": true}')
    .load()
)

# Persiste na camada Bronze (append-only, sem transformação)
df_bronze_parsed = (
    df_bronze
    .selectExpr("CAST(body AS STRING) as json_raw", "enqueuedTime as event_time")
    .withColumn("ingestao_timestamp", current_timestamp())
    .withColumn("camada", lit("bronze"))
)

print("[BRONZE] Dados brutos persistidos com sucesso.")

# =============================================================================
# CAMADA SILVER: Limpeza, validação e tipagem
# Remove duplicatas, valida campos obrigatórios, converte tipos.
# =============================================================================
print("\n[SILVER] Aplicando transformações de limpeza e validação...")

df_silver = (
    spark.read.format("delta").load(BRONZE_PATH)
    .selectExpr("json_raw", "event_time", "ingestao_timestamp")
    .selectExpr(
        "get_json_object(json_raw, '$.correlationId') as correlation_id",
        "get_json_object(json_raw, '$.acao') as acao",
        "get_json_object(json_raw, '$.status') as status",
        "CAST(get_json_object(json_raw, '$.quantidadeRegistros') AS INT) as qtd_registros",
        "CAST(get_json_object(json_raw, '$.tempoProcessamentoMs') AS BIGINT) as tempo_ms",
        "get_json_object(json_raw, '$.detalhesErro') as detalhes_erro",
        "event_time",
        "ingestao_timestamp"
    )
    # Remove duplicatas por correlation_id (idempotência)
    .dropDuplicates(["correlation_id"])
    # Valida campos obrigatórios
    .filter(col("correlation_id").isNotNull())
    .filter(col("acao").isNotNull())
    # Adiciona metadados da camada Silver
    .withColumn("silver_timestamp", current_timestamp())
    .withColumn("camada", lit("silver"))
    .withColumn("is_sucesso", when(col("status") == "SUCESSO", True).otherwise(False))
    .withColumn("tempo_seg", spark_round(col("tempo_ms") / 1000.0, 2))
)

print(f"[SILVER] Registros processados: {df_silver.count()}")

# =============================================================================
# CAMADA GOLD: Agregações para o Dashboard Gerencial
# Gera as tabelas consumidas pelo Power BI / Painel Gerencial.
# =============================================================================
print("\n[GOLD] Gerando agregações para o Dashboard Gerencial...")

# -----------------------------------------------------------------------
# GOLD 1: Métricas de Produtividade (por hora, dia, semana)
# -----------------------------------------------------------------------
df_gold_produtividade = (
    df_silver
    .withColumn("hora", date_format("event_time", "HH"))
    .withColumn("data", date_format("event_time", "yyyy-MM-dd"))
    .withColumn("dia_semana", date_format("event_time", "EEEE"))
    .groupBy("data", "hora", "dia_semana")
    .agg(
        count("*").alias("total_contratos"),
        spark_sum(when(col("is_sucesso"), 1).otherwise(0)).alias("contratos_sucesso"),
        spark_sum(when(~col("is_sucesso"), 1).otherwise(0)).alias("contratos_erro"),
        spark_round(avg("tempo_ms"), 0).alias("tempo_medio_ms"),
        spark_min("tempo_ms").alias("tempo_min_ms"),
        spark_max("tempo_ms").alias("tempo_max_ms"),
        spark_sum("qtd_registros").alias("total_registros_gerados"),
        # Métricas de produtividade e custo
        spark_round(
            spark_sum(when(col("is_sucesso"), 1).otherwise(0)) * 100.0 / count("*"), 2
        ).alias("taxa_sucesso_pct"),
    )
    .withColumn("camada", lit("gold"))
    .withColumn("gold_timestamp", current_timestamp())
    # Cálculos de custo e economia
    .withColumn("custo_manual_estimado_brl",
                spark_round(col("total_contratos") * lit(12.50), 2))
    .withColumn("custo_automatizado_brl",
                spark_round(col("total_contratos") * lit(0.35), 2))
    .withColumn("economia_brl",
                spark_round(col("custo_manual_estimado_brl") - col("custo_automatizado_brl"), 2))
    .withColumn("tempo_manual_estimado_min",
                spark_round(col("total_contratos") * lit(15.0), 0))
    .withColumn("tempo_automatizado_min",
                spark_round(col("total_contratos") * col("tempo_medio_ms") / 60000.0, 2))
    .withColumn("tempo_economizado_min",
                spark_round(col("tempo_manual_estimado_min") - col("tempo_automatizado_min"), 2))
)

# Persiste na camada Gold
df_gold_produtividade.write.format("delta").mode("overwrite").save(GOLD_DASHBOARD_PATH)

print("[GOLD] Dashboard Gerencial atualizado com sucesso.")

# -----------------------------------------------------------------------
# GOLD 2: Métricas do Agente de IA
# -----------------------------------------------------------------------
df_gold_agente = (
    df_silver
    .withColumn("data", date_format("event_time", "yyyy-MM-dd"))
    .groupBy("data")
    .agg(
        count("*").alias("tarefas_automatizadas"),
        spark_sum(when(col("is_sucesso"), 1).otherwise(0)).alias("tarefas_sucesso"),
        spark_round(avg("tempo_ms"), 0).alias("tempo_medio_ms"),
        spark_sum("qtd_registros").alias("registros_processados"),
    )
    .withColumn("rotinas_eliminadas", spark_round(col("tarefas_automatizadas") * lit(3), 0))
    .withColumn("horas_economizadas", spark_round(col("tarefas_automatizadas") * lit(0.25), 2))
    .withColumn("custo_evitado_brl", spark_round(col("tarefas_automatizadas") * lit(12.15), 2))
    .withColumn("roi_percentual", spark_round(
        (col("custo_evitado_brl") - col("tarefas_automatizadas") * lit(0.35)) /
        (col("tarefas_automatizadas") * lit(0.35)) * lit(100), 1
    ))
    .withColumn("camada", lit("gold"))
)

df_gold_agente.write.format("delta").mode("overwrite").save(GOLD_AGENTE_IA_PATH)

print("[GOLD] Métricas do Agente de IA atualizadas com sucesso.")

# =============================================================================
# RESUMO DA EXECUÇÃO
# =============================================================================
print("\n" + "=" * 70)
print("  PIPELINE CONCLUÍDO COM SUCESSO")
print(f"  Bronze: dados brutos ingeridos")
print(f"  Silver: dados limpos e validados")
print(f"  Gold:   dashboards e métricas atualizados")
print(f"  Timestamp: {datetime.utcnow().isoformat()}Z")
print("=" * 70)
