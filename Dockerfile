FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Cobryx.Api/Cobryx.Api.csproj", "Cobryx.Api/"]
COPY ["Cobryx.Application/Cobryx.Application.csproj", "Cobryx.Application/"]
COPY ["Cobryx.Domain/Cobryx.Domain.csproj", "Cobryx.Domain/"]
COPY ["Cobryx.Infrastructure/Cobryx.Infrastructure.csproj", "Cobryx.Infrastructure/"]
RUN dotnet restore "Cobryx.Api/Cobryx.Api.csproj"
COPY . .
WORKDIR "/src/Cobryx.Api"
RUN dotnet build "Cobryx.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cobryx.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Cloud Run sets PORT env var (default 8080). Bind to it.
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Cloud Run uses its own HTTP health probes externally.
# Chiseled images have no shell/curl, so Docker-level HEALTHCHECK is not feasible.
HEALTHCHECK NONE

# Chiseled images run as non-root (uid 1654) by default.
USER 1654

ENTRYPOINT ["dotnet", "Cobryx.Api.dll"]
