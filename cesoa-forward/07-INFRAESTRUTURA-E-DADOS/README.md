# 07 — Infraestrutura e Dados

**Autor:** Grawingholt, Diogo

**O que encontrar aqui:** Event Hub, Microsoft Fabric, Pipeline Medallion, Data Mesh e SQL.

| Pasta/Arquivo | Descricao |
|---------------|-----------|
| `Data/Context/CreditoPriceDbContext.cs` | Entity Framework Core com SQL Server |
| `Data/Repositories/EvolucaoContratoRepository.cs` | Repositorio com bulk insert |
| `Messaging/EventHub/EventHubPublisher.cs` | Publicador de eventos para Azure Event Hub |
| `Fabric/FabricLakehousePublisher.cs` | Integracao com Microsoft Fabric Lakehouse |
| `Fabric/notebook_pipeline_medallion.py` | Pipeline Bronze/Silver/Gold em Python |
| `DataMesh/DatabricksUnityCatalogGovernance.cs` | Governanca de dados com Unity Catalog |
| `001_CriarTabelaEvolucaoContrato.sql` | Script DDL para criacao da tabela |

Dados projetados desde a construcao para evitar retrabalho. Conformidade com LGPD.

**Proximo passo:** Siga para `08-INTELIGENCIA-ARTIFICIAL`
