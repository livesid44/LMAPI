# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# Copy solution + project files first for layer-cached restore
COPY LMAPI.slnx ./
COPY src/LMAPI/LMAPI.csproj ./src/LMAPI/
COPY tests/LMAPI.Tests/LMAPI.Tests.csproj ./tests/LMAPI.Tests/

RUN dotnet restore LMAPI.slnx

# Copy source and test code
COPY src/ ./src/
COPY tests/ ./tests/

# Run unit tests
RUN dotnet test tests/LMAPI.Tests/LMAPI.Tests.csproj \
        --configuration Release \
        --no-restore \
        --logger "console;verbosity=minimal"

# Publish the API
RUN dotnet publish src/LMAPI/LMAPI.csproj \
        --configuration Release \
        --no-restore \
        --output /publish

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for least-privilege execution
RUN addgroup --gid 1001 appgroup && \
    adduser  --uid 1001 --gid 1001 --disabled-password --gecos "" appuser

COPY --from=build --chown=appuser:appgroup /publish ./

USER appuser

# Azure Container Apps uses port 8080 by default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LMAPI.dll"]
