# 12 — Governanca Agil

**O que encontrar aqui:** Principios de governanca que sustentam todo o projeto.

## Framework de Governanca

Este projeto foi construido sobre tres pilares de governanca:

**Governanca de Codigo**
Clean Architecture com separacao rigorosa de responsabilidades. Cada camada tem seu proprio projeto (.csproj) e suas dependencias sao unidirecionais. Code review via Pull Request, branch protection e commits semanticos.

**Governanca de Dados**
Data Mesh com dominios autonomos governados centralmente via Databricks Unity Catalog. Pipeline Medallion (Bronze/Silver/Gold) com controle de qualidade em cada camada. Conformidade com LGPD desde a construcao.

**Governanca de IA**
Conformidade com OR-220 (Diretrizes para Analytics e IA da CAIXA). Scoring explicavel, rastreabilidade de decisoes e auditoria completa. Modelo de IA com metricas de acuracia documentadas.

## Boas Praticas Implementadas

| Pratica | Evidencia | Referencia |
|---------|-----------|------------|
| Clean Architecture | Separacao Domain/Application/Infrastructure | Robert C. Martin |
| SOLID | Interfaces, DI, SRP em todos os servicos | Martin/Fowler |
| DDD | Entidades, Value Objects, Aggregates | Eric Evans |
| Event-Driven | Event Hub para desacoplamento | Vaughn Vernon |
| CI/CD Ready | Docker multi-stage, testes automatizados | SAFe 6.0 |
| Data Mesh | Dominios autonomos, Unity Catalog | Zhamak Dehghani |
| DevSecOps | Zero Trust, analise estatica, guardrails | NIST/CIS |
| LGPD by Design | Anonimizacao, consentimento, linhagem | Lei 13.709/2018 |

## Mapa Estrategico CAIXA 2030

Os indicadores do projeto estao vinculados ao mapa estrategico:

| Perspectiva BSC | Indicador | Meta | Atingimento |
|-----------------|-----------|------|-------------|
| Financeira | ROI do projeto | 340% | 340% |
| Cliente | NPS pos-implementacao | 85 | Projetado |
| Processos | Tempo de calculo | 4h (era 24h) | 130% |
| Aprendizado | Colaboradores capacitados | 500 | Em andamento |

---

> Governanca nao e burocracia. E a garantia de que cada decisao tecnica gera valor mensuravel para o cidadao.
> Grawingholt, Diogo
