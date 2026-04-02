# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files and restore dependencies first (layer cache friendly)
COPY src/AuthBackend.Domain/AuthBackend.Domain.csproj             src/AuthBackend.Domain/
COPY src/AuthBackend.Application/AuthBackend.Application.csproj   src/AuthBackend.Application/
COPY src/AuthBackend.Infrastructure/AuthBackend.Infrastructure.csproj src/AuthBackend.Infrastructure/

RUN dotnet restore src/AuthBackend.Infrastructure/AuthBackend.Infrastructure.csproj

# Copy the rest of the source and publish
COPY src/ src/
RUN dotnet publish src/AuthBackend.Infrastructure/AuthBackend.Infrastructure.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# SQLite DB lives in a dedicated directory so it can be mounted as a volume
RUN mkdir -p /app/data && chown appuser:appgroup /app/data

COPY --from=build /app/publish .

# Override the connection string to point at the volume-mounted path
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/auth.db"
ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthBackend.Infrastructure.dll"]
