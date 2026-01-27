FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

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

# Create a non-root user to run the application
RUN adduser --disabled-password --gecos '' --uid 1000 appuser && \
    chown -R appuser:appuser /app

COPY --from=publish /app/publish .

# Add health check to monitor application status
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:80/health || exit 1

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "Cobryx.Api.dll"]
