# ============================================================================
# Projeto: CESOA Forward — Excelência em Serviços Digitais CAIXA 2030
# Autor:   Grawingholt, Diogo
# ============================================================================

# =============================================================================
# CAIXA - Dockerfile para API de Cálculo PRICE
# Multi-stage build para imagem otimizada de produção
# =============================================================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CreditoPrice.Api/CreditoPrice.Api.csproj", "CreditoPrice.Api/"]
COPY ["src/CreditoPrice.Domain/CreditoPrice.Domain.csproj", "CreditoPrice.Domain/"]
RUN dotnet restore "CreditoPrice.Api/CreditoPrice.Api.csproj"
COPY src/ .
WORKDIR "/src/CreditoPrice.Api"
RUN dotnet build "CreditoPrice.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditoPrice.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CreditoPrice.Api.dll"]
