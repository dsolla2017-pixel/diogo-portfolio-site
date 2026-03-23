# CESOA Forward — Excelencia em Servicos Digitais CAIXA 2030

**Autor:** Grawingholt, Diogo
**Projeto:** Proposta de Lideranca Tecnica PSI-CTI (Coordenador de TI - CESOA)
**Framework:** SAFe 6.0 + Clean Architecture + Data Mesh + DevSecOps

---

## Indice de Navegacao

Este repositorio esta organizado em **12 pastas numeradas** que representam a jornada completa do projeto. Cada pasta e autocontida, com seu proprio README explicando o conteudo, o proposito e a conexao com o mapa estrategico da CAIXA 2030.

A sequencia segue a logica de avaliacao: primeiro entenda o projeto, depois percorra a jornada tecnica, e ao final avalie a governanca.

| Etapa | Pasta | Conteudo | Tempo |
|-------|-------|----------|-------|
| 1 | `01-ENTENDA-O-PROJETO` | Visao geral, proposta estrategica e Solution (.sln) | 5 min |
| 2 | `02-JORNADA-DO-AVALIADOR` | Roteiro de avaliacao, checklist de requisitos vs. superacao | 8 min |
| 3 | `03-ARQUITETURA-E-DESIGN` | Visao de futuro, modelo operativo digital, diagramas | 5 min |
| 4 | `04-DOMINIO-E-REGRAS-DE-NEGOCIO` | Entidades, Value Objects, Interfaces (Clean Architecture) | 5 min |
| 5 | `05-API-MOTOR-DE-CALCULO` | API REST, motor Price/SAC/SACRE, Swagger | 10 min |
| 6 | `06-WORKER-PROCESSAMENTO-ASSINCRONO` | BackgroundService, fila, idempotencia | 5 min |
| 7 | `07-INFRAESTRUTURA-E-DADOS` | Event Hub, Fabric, Medallion, Data Mesh, SQL | 8 min |
| 8 | `08-INTELIGENCIA-ARTIFICIAL` | Agente IA, Rede Neural, Copilot RAG, CRM | 10 min |
| 9 | `09-TESTES-E-QUALIDADE` | xUnit, cobertura, validacao matematica | 3 min |
| 10 | `10-DEVOPS-E-CONTAINERIZACAO` | Docker, docker-compose, Dockerfiles | 3 min |
| 11 | `11-PAINEL-INTERATIVO` | Dashboard React com 12 secoes e KPIs | 5 min |
| 12 | `12-GOVERNANCA-AGIL` | Framework SAFe, LGPD, OR-220, boas praticas | 5 min |

**Tempo total estimado: 72 minutos**

---

## Principios de Organizacao

Este repositorio segue tres frameworks de referencia para governanca de codigo:

**Clean Architecture (Robert C. Martin)**
Separacao rigorosa em camadas: Domain (regras de negocio), Application (casos de uso), Infrastructure (implementacoes externas) e Presentation (API/UI). Nenhuma camada interna depende de camadas externas.

**SAFe 6.0 (Scaled Agile Framework)**
Entregas incrementais organizadas por valor de negocio. Cada pasta representa um incremento avaliavel de forma independente. A sequencia numerada reflete a prioridade do backlog.

**Data Mesh (Zhamak Dehghani)**
Dados como produto. Cada dominio de dados e autonomo, mas governado centralmente via Unity Catalog. A pasta 07 demonstra essa abordagem com pipeline Medallion e governanca Databricks.

---

## Como Avaliar

1. Comece pela pasta `01-ENTENDA-O-PROJETO` para contexto geral
2. Siga para `02-JORNADA-DO-AVALIADOR` para o roteiro detalhado
3. Percorra as pastas 03 a 10 na ordem para ver a construcao tecnica
4. Acesse o painel interativo em `11-PAINEL-INTERATIVO` (link para o site)
5. Finalize com `12-GOVERNANCA-AGIL` para ver a visao de governanca

Cada pasta contem um arquivo `README.md` com explicacao do conteudo.

---

## Links Rapidos

| Recurso | URL |
|---------|-----|
| Painel Interativo | [planodgrstransformador.org](https://planodgrstransformador.org) |
| Portfolio | [diogograwingholt.com.br](https://diogograwingholt.com.br) |
| LinkedIn | [linkedin.com/in/diogo-grawingholt](https://www.linkedin.com/in/diogo-grawingholt/) |

---

> "Nao digitalizamos processos. Reinventamos a relacao entre pessoas e servicos financeiros."
> CESOA Forward, Grawingholt, Diogo
