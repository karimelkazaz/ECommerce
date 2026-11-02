FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src

# Copy project files for caching restore layer
COPY ["src/ECommerce.Domain/ECommerce.Domain.csproj", "src/ECommerce.Domain/"]
COPY ["src/ECommerce.Application/ECommerce.Application.csproj", "src/ECommerce.Application/"]
COPY ["src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "src/ECommerce.Infrastructure/"]
COPY ["src/ECommerce.Api/ECommerce.Api.csproj", "src/ECommerce.Api/"]
COPY ["Directory.Build.props", "./"]

RUN dotnet restore "src/ECommerce.Api/ECommerce.Api.csproj"

# Copy remaining source code and publish
COPY . .
WORKDIR "/src/src/ECommerce.Api"
RUN dotnet publish "ECommerce.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID

# Probes the in-app health endpoint (SQL Server + Redis checks);
# non-zero exit → container reported Unhealthy.
HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=30s \
    CMD curl --fail --silent --show-error --output /dev/null http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
