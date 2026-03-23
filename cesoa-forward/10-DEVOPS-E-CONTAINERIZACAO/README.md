# 10 — DevOps e Containerizacao

**Autor:** Grawingholt, Diogo

**O que encontrar aqui:** Docker Compose, Dockerfiles e configuracao de ambiente.

| Arquivo | Descricao |
|---------|-----------|
| `docker-compose.yml` | Orquestracao de API + Worker + SQL Server |
| `Dockerfile.Api` | Imagem da API (multi-stage build) |
| `Dockerfile.Worker` | Imagem do Worker (multi-stage build) |

Para executar: `docker-compose up -d --build`
API disponivel em `http://localhost:5100/swagger`

**Proximo passo:** Siga para `11-PAINEL-INTERATIVO`
