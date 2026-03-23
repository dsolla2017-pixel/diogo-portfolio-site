# Proposta CESOA Forward — Grawingholt, Diogo | Março 2026

# Proposta Estratégica: Desafio Técnico PSI-CTI
## Processamento Assíncrono de Contratos de Crédito (Método PRICE)
### CAIXA Econômica Federal | CESOA - CN Soluções de TI

---

## 1. Contexto e Posicionamento Estratégico

O desafio técnico PSI-CTI representa uma oportunidade concreta de demonstrar como a tecnologia se conecta diretamente à Estratégia CAIXA 2030. A solução proposta não se limita a resolver um problema técnico isolado. Ela estabelece um modelo arquitetural de referência para o processamento assíncrono de contratos de crédito, com impacto direto na experiência do cliente, na eficiência operacional e na governança de dados.

A CAIXA atua como o maior banco público da América Latina, com responsabilidade institucional que exige excelência em cada processo. O processamento de contratos de financiamento pelo método PRICE afeta milhões de cidadãos brasileiros. A precisão do cálculo, a resiliência do processamento e a rastreabilidade de cada operação são requisitos inegociáveis.

Esta proposta foi construída com a visão de um líder transformador que integra estratégia, organização, cultura, tecnologia e cliente em um modelo coerente e sustentável.

## 2. Arquitetura de Referência

A solução adota a **Clean Architecture** (Robert C. Martin) combinada com **Event-Driven Architecture (EDA)**, padrões reconhecidos como boas práticas em times ágeis e em organizações de alta criticidade no setor financeiro.

### 2.1 Estrutura de Camadas

| Camada | Projeto | Responsabilidade |
| :--- | :--- | :--- |
| **Domínio** | `CreditoPrice.Domain` | Entidades, Value Objects, Interfaces e Enums. Regras de negócio puras, sem dependência de infraestrutura. |
| **Aplicação (API)** | `CreditoPrice.Api` | Motor de cálculo PRICE. Endpoint REST `POST /api/price/calcular`. Swagger para documentação. |
| **Aplicação (Worker)** | `CreditoPrice.Worker` | BackgroundService que consome a fila, orquestra o fluxo e garante idempotência. |
| **Infraestrutura** | `CreditoPrice.Infrastructure` | Entity Framework Core (SQL Server), Azure Event Hub Publisher, Azure Service Bus Consumer. |
| **Testes** | `CreditoPrice.Tests` | Testes unitários (xUnit) para validação do cálculo financeiro e coerência dos resultados. |

### 2.2 Fluxo Ponta a Ponta

O fluxo completo segue sete etapas sequenciais, cada uma com tratamento de exceções e logging estruturado:

1. **Recepção da mensagem** na fila Azure Service Bus (`queue-contrato-price`).
2. **Validação do payload** (valores positivos, prazo maior que zero).
3. **Verificação de idempotência** (consulta ao banco se o contrato já foi processado).
4. **Chamada à API de Cálculo PRICE** via HTTP (`POST /api/price/calcular`).
5. **Persistência no banco de dados** (uma linha por dia, 900 registros para 30 meses).
6. **Publicação de evento no Event Hub** (telemetria com status, métricas e rastreabilidade).
7. **Confirmação da mensagem** na fila (complete) ou abandono (abandon) em caso de erro.

### 2.3 Cálculo Financeiro (Método PRICE)

O motor de cálculo implementa o Sistema Francês de Amortização com capitalização diária equivalente à taxa mensal, conforme padrão bancário brasileiro:

| Parâmetro | Fórmula | Descrição |
| :--- | :--- | :--- |
| Taxa diária equivalente | `i_dia = (1 + i_mes)^(1/30) - 1` | Converte a taxa mensal para taxa diária equivalente (mês comercial de 30 dias). |
| Prestação fixa mensal | `PMT = PV * [i*(1+i)^n] / [(1+i)^n - 1]` | Fórmula clássica PRICE. Prestação constante ao longo de todo o contrato. |
| Juros do período | `J = Saldo * i_acumulado_30dias` | Juros acumulados nos 30 dias do período, aplicados sobre o saldo devedor. |
| Amortização | `A = PMT - J` | Diferença entre prestação e juros. Cresce ao longo do contrato. |
| Saldo devedor | `S = S_anterior - A` | Decresce até atingir zero no último pagamento. |

O cálculo gera um registro por dia (total de 900 para 30 meses), com pagamento efetivo a cada 30 dias e capitalização diária nos dias intermediários.

## 3. Inovação e Diferencial Competitivo

### 3.1 Idempotência e Resiliência

O Worker verifica no banco de dados se o `ContratoId` (derivado do `MessageId` da fila) já possui registros antes de processar. Isso previne duplicidade em cenários de reprocessamento (retry automático do Service Bus). A mensagem só é confirmada (complete) após a persistência bem-sucedida no banco e a publicação do evento no Event Hub.

### 3.2 Observabilidade de Alto Volume

O Azure Event Hub foi escolhido (conforme requisito do desafio) como canal de telemetria por sua capacidade de processar milhões de eventos por segundo. Cada evento publicado contém:

| Campo | Tipo | Finalidade |
| :--- | :--- | :--- |
| `acao` | string | Identifica a operação realizada (ex.: `ProcessamentoContratoPRICE`). |
| `status` | enum | SUCESSO ou ERRO (tipagem forte, sem "magic strings"). |
| `mensagemProcessada` | string | Resumo do contrato para correlação. |
| `timestamp` | datetime | Data/hora UTC no padrão ISO 8601. |
| `correlationId` | string | Vincula o evento à mensagem original da fila. |
| `quantidadeRegistros` | int | Permite validação cruzada (ex.: 900 para 30 meses). |
| `tempoProcessamentoMs` | long | Métrica de performance para SLA e melhoria contínua. |

### 3.3 Preparação para IA e Data Driven

Os dados estruturados gerados pelo processamento (armazenados no SQL Server e replicados via Event Hub) podem alimentar a Plataforma de Dados CAIXA (PDC/Databricks) para:

* Modelos preditivos de inadimplência (Machine Learning).
* Análise de perfil de risco em tempo real.
* Dashboards de acompanhamento para a gestão (Power BI).
* Auditoria contínua automatizada (conforme PAINT 2026).

## 4. Alinhamento com Frameworks Ágeis

### 4.1 Scrum

A solução foi construída com a mentalidade de entrega incremental. Cada camada (Domain, API, Worker, Infrastructure, Tests) representa um incremento potencialmente liberável. O Product Owner pode priorizar a entrega da API de cálculo primeiro (valor imediato para validação do motor financeiro) e depois evoluir para o Worker e a integração com Event Hub.

### 4.2 SAFe (Scaled Agile Framework)

A arquitetura respeita os princípios do SAFe:

| Princípio SAFe | Aplicação na Solução |
| :--- | :--- |
| **Built-in Quality** | Testes automatizados (xUnit), validação de domínio (Value Objects), idempotência. |
| **Assume Variability** | Interfaces desacopladas permitem troca de implementação (SQL Server para PostgreSQL, Event Hub para Kafka). |
| **Build Incrementally** | Cada projeto da solution é um incremento independente e testável. |
| **Apply Systems Thinking** | A solução considera o ecossistema completo (fila, API, banco, telemetria, auditoria). |

## 5. Governança e Conformidade

A solução atende aos normativos internos da CAIXA:

* **PO-007 (Segurança da Informação):** Dados sensíveis não são expostos em logs. Connection strings são gerenciadas via configuração (appsettings.json / Azure Key Vault em produção).
* **OR-220 (Diretrizes para Analytics e IA):** Rastreabilidade completa de cada decisão automatizada. Dados estruturados e auditáveis.
* **OR-213 (Governança de Soluções):** Arquitetura padronizada, documentada e integrada ao ambiente corporativo.

## 6. Estrutura de Arquivos

```
MATRICULA_NUMERO_PSI/
├── CreditoPrice.sln
├── README.md
├── docker-compose.yml
├── .gitignore
├── docs/
│   └── PROPOSTA_DESAFIO_PSI_CTI.md
├── scripts/
│   └── 001_CriarTabelaEvolucaoContrato.sql
└── src/
    ├── CreditoPrice.Domain/
    │   ├── Entities/EvolucaoContrato.cs
    │   ├── Enums/StatusProcessamento.cs
    │   ├── Interfaces/ICalculoPriceService.cs
    │   ├── Interfaces/IEvolucaoContratoRepository.cs
    │   ├── Interfaces/IEventHubPublisher.cs
    │   └── ValueObjects/ContratoRequest.cs, EventoProcessamento.cs
    ├── CreditoPrice.Api/
    │   ├── Controllers/PriceController.cs
    │   ├── Services/CalculoPriceService.cs
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    ├── CreditoPrice.Worker/
    │   ├── Services/ContratoWorker.cs
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    ├── CreditoPrice.Infrastructure/
    │   ├── Data/Context/CreditoPriceDbContext.cs
    │   ├── Data/Repositories/EvolucaoContratoRepository.cs
    │   └── Messaging/EventHub/EventHubPublisher.cs
    └── CreditoPrice.Tests/
        └── Unit/CalculoPriceServiceTests.cs
```

## 7. Conclusão e Mensagem Estratégica

Esta entrega demonstra a capacidade de conectar estratégia, organização, cultura, tecnologia e cliente em um modelo coerente, sustentável e orientado a valor. A solução vai além do código: ela estabelece um padrão arquitetural replicável, com qualidade embutida, observabilidade de ponta a ponta e preparação para a era da Inteligência Artificial no setor bancário.

O legado institucional desta proposta reside na combinação de excelência técnica (cálculo financeiro preciso, arquitetura limpa, testes automatizados) com visão estratégica (alinhamento à Estratégia 2030, governança de dados, cultura ágil). Este é o tipo de entrega que fortalece a CAIXA como referência em transformação digital no setor público brasileiro.

---

**Referências Técnicas:**
1. Robert C. Martin, "Clean Architecture" (2017)
2. Eric Evans, "Domain-Driven Design" (2003)
3. Microsoft, "Azure Well-Architected Framework" (2024)
4. Ken Schwaber e Jeff Sutherland, "Guia do Scrum" (2020)
5. Scaled Agile Inc., "SAFe 6.0 Framework" (2024)
6. CAIXA, "Estratégia CAIXA 2030"
7. CAIXA, "OR-220 - Diretrizes para Analytics, Ciência de Dados e IA" (2025)
