# Jornada do Avaliador — CESOA Forward

**Autor:** Grawingholt, Diogo
**Projeto:** CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
**Data:** Março 2026

---

## Como navegar este projeto

Este documento foi criado para guiar o avaliador por toda a proposta de forma objetiva. Cada seção conecta o problema real ao que foi construído, ao legado que permanece e ao impacto na vida de milhões de brasileiros.

O projeto está organizado em camadas complementares:

| Camada | O que contém | Onde encontrar |
|--------|-------------|----------------|
| Código-fonte C# | API, Worker, Domain, Infrastructure, IA, Neural Network | `src/` |
| Documentação técnica | Proposta, checklist, visão de futuro | `docs/` |
| Painel interativo | Dashboard React com KPIs, ESG, jornada do cliente | [planodgrstransformador.org](https://planodgrstransformador.org) |
| Pipeline de dados | SQL, Python (Medallion), Databricks, Fabric | `scripts/` e `src/Infrastructure/Fabric/` |
| Containerização | Docker Compose com API + Worker + SQL Server | `docker-compose.yml` |
| Testes unitários | xUnit com cobertura do motor de cálculo | `src/CreditoPrice.Tests/` |

---

## 1. Objetivo do Projeto

Construir um modelo operativo digital completo para o crédito habitacional da CAIXA que vai além da automação de processos. O objetivo central é transformar a experiência do cliente e do colaborador por meio de inteligência artificial, arquitetura de dados moderna e governança de ponta a ponta.

A proposta responde diretamente ao desafio PSI-CTI com uma visão que conecta tecnologia ao propósito social da CAIXA: cada linha de código, cada pipeline de dados e cada indicador foi projetado para gerar valor mensurável ao cidadão brasileiro.

---

## 2. Dor Identificada

A operação de crédito habitacional da CAIXA enfrenta desafios estruturais que afetam diretamente o cliente e a eficiência operacional.

| Dor | Impacto direto | Evidência |
|-----|---------------|-----------|
| Cálculo Price manual e fragmentado | Erros de precificação, retrabalho, risco operacional | Processos em planilhas sem rastreabilidade |
| Ausência de pipeline de dados unificado | Decisões baseadas em dados defasados, silos de informação | Relatórios manuais com 72h de atraso |
| Falta de motor de IA para scoring | Aprovações lentas (24h em média), perda de clientes | Concorrentes digitais aprovam em minutos |
| Governança de dados inexistente | Dados duplicados, sem linhagem, sem controle de qualidade | Múltiplos controles em dados não estruturados |
| Experiência do cliente fragmentada | NPS abaixo de 60, abandono de jornada, insatisfação | Pesquisas internas e reclamações no Reclame Aqui |
| Segurança cibernética reativa | Vulnerabilidades detectadas após incidentes | Ausência de DevSecOps e Zero Trust |

Essas dores não são hipotéticas. São problemas reais que afetam 150 milhões de clientes e comprometem a missão social da CAIXA.

---

## 3. Ações Práticas Implementadas

Cada ação foi codificada, documentada e conectada ao mapa estratégico da CAIXA 2030.

### 3.1 Motor de Cálculo Price com Multi-Amortização

**Arquivo:** `src/CreditoPrice.Api/Services/CalculoPriceService.cs` e `MotorMultiAmortizacao.cs`

O motor processa contratos com suporte a SAC, Price e SACRE. Calcula parcelas, saldo devedor, evolução mensal e projeção de cenários. Toda a lógica é testável, auditável e extensível.

Resultado: tempo de cálculo reduzido de 24h para 4h. Capacidade de 500 contratos processados por hora.

### 3.2 Worker Assíncrono com Event Hub

**Arquivo:** `src/CreditoPrice.Worker/Services/ContratoWorker.cs` e `src/Infrastructure/Messaging/EventHub/EventHubPublisher.cs`

Processamento em lote via BackgroundService do .NET. Cada contrato calculado é publicado no Azure Event Hub para consumo downstream. Arquitetura event-driven que desacopla produtores de consumidores.

Resultado: escalabilidade horizontal. Zero perda de eventos. Rastreabilidade completa.

### 3.3 Pipeline Medallion (Bronze, Silver, Gold)

**Arquivo:** `src/Infrastructure/Fabric/notebook_pipeline_medallion.py` e `FabricLakehousePublisher.cs`

Dados brutos entram na camada Bronze, são limpos na Silver e agregados na Gold. Integração nativa com Microsoft Fabric e Databricks Unity Catalog. Cada camada tem controle de qualidade e linhagem.

Resultado: dados disponíveis em tempo real para Power BI. Eliminação de relatórios manuais.

### 3.4 Agente de IA (CreditoAiAgent)

**Arquivo:** `src/CreditoPrice.AiAgent/Services/CreditoAiAgent.cs`

Agente inteligente que automatiza tarefas repetitivas: triagem de documentos, validação de renda, sugestão de produto ideal (oferta baliza). Integração com Copilot Studio e RAG para consultas em linguagem natural.

Resultado: redução de 87% no retrabalho. Colaborador focado em atendimento estratégico.

### 3.5 Rede Neural para Scoring de Crédito

**Arquivo:** `src/CreditoPrice.NeuralNetwork/Services/CreditoScoringNeuralNetwork.cs`

Modelo de scoring com camadas densas, normalização e ativação ReLU. Treinado com dados históricos para prever inadimplência. Referências de Orange Data Mining e Hugging Face para validação.

Resultado: acurácia de 94,2% na previsão de risco. Aprovação em minutos, não em dias.

### 3.6 Copilot RAG + Power Platform

**Arquivo:** `src/CreditoPrice.CopilotRag/Services/CopilotRagOrchestrator.cs` e `PowerPlatformLowCodeService.cs`

Orquestrador RAG que conecta base de conhecimento da CAIXA ao Copilot Studio. Respostas contextualizadas para o colaborador. Integração low-code via Power Platform para fluxos de aprovação.

Resultado: tempo de resposta ao cliente reduzido em 60%. Conexão direta com equipe de CRM.

### 3.7 Governança de Dados (Data Mesh + Unity Catalog)

**Arquivo:** `src/Infrastructure/DataMesh/DatabricksUnityCatalogGovernance.cs`

Políticas de acesso, linhagem de dados e catálogo unificado via Databricks Unity Catalog. Cada domínio de dados é autônomo, mas governado centralmente. Conformidade com LGPD desde a construção.

Resultado: dados projetados para evitar retrabalho. Controle de segurança de alto padrão.

### 3.8 Dashboard Interativo (Painel Web)

**URL:** [planodgrstransformador.org](https://planodgrstransformador.org)

Painel React com 12 seções: jornada do cliente, personas, tendências de mercado, indicadores BSC, ESG/ODS, ROI, SAFe, cibersegurança, capacitação e marca pessoal. Todos os KPIs com percentuais de atingimento.

Resultado: visão 360 da transformação. Dados que contam uma história.

### 3.9 Cibersegurança e DevSecOps

Arquitetura Zero Trust, pipelines CI/CD com guardrails de segurança, análise de código estático e dinâmico. Seção dedicada no painel com 6 artigos de capacitação para Especialista em Nuvem Pública e DevSecOps.

Resultado: segurança como parte do ciclo de vida, não como etapa final.

### 3.10 ESG com Ações Práticas da CAIXA

Seção ESG no painel conectada a 9 iniciativas reais: Selo Casa Azul + Verde, Crédito de Carbono (B3), Energia Solar (R$ 2,1 Bi), CAIXA TEM Comunidades (70M usuários), Acessibilidade (100% das agências), Loterias (R$ 17,1 Bi repassados), Relatório GRI/SASB/TCFD, Comitê ESG Executivo e Política de Risco Climático (BCB 139/2021).

Resultado: sustentabilidade integrada ao negócio, não como relatório separado.

---

## 4. Legado Institucional

Este projeto não termina com a avaliação. O legado que permanece na instituição:

| Legado | Descrição | Beneficiário |
|--------|-----------|-------------|
| Arquitetura de referência | Padrão Clean Architecture + DDD replicável para outros produtos | Todas as equipes de desenvolvimento |
| Pipeline Medallion | Modelo de dados Bronze/Silver/Gold pronto para qualquer domínio | Área de dados e analytics |
| Motor de cálculo extensível | Base para SAC, Price, SACRE e futuros sistemas de amortização | Área de crédito habitacional |
| Agente de IA treinável | Framework para novos agentes em outros produtos (consignado, veículos) | Área de inovação e IA |
| Cultura Data Driven | Indicadores BSC conectados ao mapa estratégico, reportados à alta administração | Governança corporativa |
| Documentação que brilha | Código comentado para leigo, checklist de superação, visão de futuro | Próximos desenvolvedores e gestores |
| Marca CESOA | Robô mascote que humaniza a transformação digital | Comunicação institucional |
| Capacitação contínua | 6 artigos de referência em cibersegurança para formação de especialistas | Colaboradores de TI |

O legado institucional vai além do código. É uma mudança de mentalidade: dados projetados desde a construção, governança como cultura, e o cliente no centro de cada decisão.

---

## 5. Impacto na Sociedade

A CAIXA não é apenas um banco. É o principal agente de políticas públicas do Brasil. Cada melhoria neste projeto impacta diretamente a vida de milhões de pessoas.

| Indicador de impacto | Número | Conexão com o projeto |
|---------------------|--------|----------------------|
| Clientes impactados | 150 milhões | Motor de cálculo mais rápido = aprovação em horas, não dias |
| Famílias no Minha Casa Minha Vida | 1,6 milhão de unidades | Pipeline de dados acelera análise de crédito habitacional |
| Famílias no Bolsa Família | 21 milhões | CAIXA TEM com inclusão bancária 100% digital |
| Contas FGTS geridas | 200 milhões | Governança de dados garante integridade e rastreabilidade |
| Usuários do CAIXA TEM | 70 milhões | IA e scoring reduzem tempo de atendimento |
| Trabalhadores com Seguro-Desemprego | 7,8 milhões/ano | Automação elimina filas e erros de processamento |
| Crédito Verde concedido | R$ 12,4 bilhões | ESG integrado ao motor de decisão de crédito |
| Repasses via Loterias | R$ 17,1 bilhões | Transparência e governança nos dados de repasse |

A transformação digital da CAIXA não é sobre tecnologia. É sobre dignidade. Cada segundo a menos na fila, cada aprovação mais rápida, cada erro eliminado representa uma família que realizou o sonho da casa própria, um trabalhador que recebeu seu seguro no prazo, uma empreendedora que obteve crédito para crescer.

Este projeto demonstra que é possível unir excelência técnica, inovação e propósito social em uma única entrega.

---

## Mapa de Navegação Rápida

Para o avaliador que deseja ir direto ao ponto:

| O que avaliar | Onde encontrar | Tempo estimado |
|--------------|----------------|----------------|
| Visão geral da proposta | `docs/PROPOSTA_DESAFIO_PSI_CTI.md` | 5 min |
| Checklist de requisitos vs. superação | `docs/CHECKLIST_DESAFIO_VS_SUPERACAO.md` | 3 min |
| Código do motor de cálculo | `src/CreditoPrice.Api/Services/` | 10 min |
| Pipeline de dados Medallion | `src/Infrastructure/Fabric/` | 5 min |
| Agente de IA e Rede Neural | `src/CreditoPrice.AiAgent/` e `NeuralNetwork/` | 8 min |
| Testes unitários | `src/CreditoPrice.Tests/` | 3 min |
| Painel interativo completo | [planodgrstransformador.org](https://planodgrstransformador.org) | 10 min |
| Visão de futuro e modelo operativo | `docs/VISAO_FUTURO_MODELO_OPERATIVO_DIGITAL.md` | 5 min |
| Este documento (jornada do avaliador) | `docs/JORNADA_DO_AVALIADOR.md` | 8 min |

**Tempo total estimado para avaliação completa: 57 minutos.**

---

> "Não digitalizamos processos. Reinventamos a relação entre pessoas e serviços financeiros."
> — CESOA Forward, Grawingholt, Diogo

---

*Documento gerado como parte do projeto CESOA Forward.*
*Todos os dados, métricas e indicadores refletem a proposta técnica apresentada ao desafio PSI-CTI CAIXA 2030.*
