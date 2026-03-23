# ============================================================================
# Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
# Autor:   Grawingholt, Diogo
# ============================================================================

# =============================================================================
# CAIXA - Dockerfile para Worker Service de Processamento de Crédito
# Multi-stage build para imagem otimizada de produção
# =============================================================================

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CreditoPrice.Worker/CreditoPrice.Worker.csproj", "CreditoPrice.Worker/"]
COPY ["src/CreditoPrice.Domain/CreditoPrice.Domain.csproj", "CreditoPrice.Domain/"]
COPY ["src/CreditoPrice.Infrastructure/CreditoPrice.Infrastructure.csproj", "CreditoPrice.Infrastructure/"]
RUN dotnet restore "CreditoPrice.Worker/CreditoPrice.Worker.csproj"
COPY src/ .
WORKDIR "/src/CreditoPrice.Worker"
RUN dotnet build "CreditoPrice.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditoPrice.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CreditoPrice.Worker.dll"]
