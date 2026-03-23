# 05 — API Motor de Calculo

**Autor:** Grawingholt, Diogo

**O que encontrar aqui:** API REST com motor de calculo Price, SAC e SACRE.

| Pasta/Arquivo | Descricao |
|---------------|-----------|
| `Controllers/PriceController.cs` | Endpoint POST /api/price/calcular |
| `Services/CalculoPriceService.cs` | Motor de calculo Price com evolucao diaria |
| `Services/MotorMultiAmortizacao.cs` | Suporte a SAC, Price e SACRE |
| `Program.cs` | Configuracao de DI, middleware e Swagger |
| `appsettings.json` | Configuracoes (connection strings sanitizadas) |

Resultado: tempo de calculo reduzido de 24h para 4h. Capacidade de 500 contratos/hora.

**Proximo passo:** Siga para `06-WORKER-PROCESSAMENTO-ASSINCRONO`
