# 06 — Worker Processamento Assincrono

**Autor:** Grawingholt, Diogo

**O que encontrar aqui:** BackgroundService que consome fila e processa contratos em lote.

| Pasta/Arquivo | Descricao |
|---------------|-----------|
| `Services/ContratoWorker.cs` | Worker com idempotencia e retry |
| `Program.cs` | Configuracao do host e DI |
| `appsettings.json` | Configuracoes (connection strings sanitizadas) |

Arquitetura event-driven: cada contrato calculado e publicado no Azure Event Hub para consumo downstream. Escalabilidade horizontal com zero perda de eventos.

**Proximo passo:** Siga para `07-INFRAESTRUTURA-E-DADOS`
