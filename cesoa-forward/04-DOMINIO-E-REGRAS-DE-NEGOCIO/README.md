# 04 — Dominio e Regras de Negocio

**Autor:** Grawingholt, Diogo

**O que encontrar aqui:** Camada de dominio (Clean Architecture). Entidades, Value Objects, Enums e Interfaces.

| Pasta/Arquivo | Descricao |
|---------------|-----------|
| `Entities/EvolucaoContrato.cs` | Entidade principal com evolucao diaria do contrato |
| `Enums/StatusProcessamento.cs` | Estados do ciclo de vida do processamento |
| `Interfaces/` | Contratos (ICalculoPriceService, IEventHubPublisher, etc.) |
| `ValueObjects/` | ContratoRequest e EventoProcessamento (imutaveis) |

O dominio nao depende de nenhuma camada externa. Todas as regras de negocio estao aqui.

**Proximo passo:** Siga para `05-API-MOTOR-DE-CALCULO`
