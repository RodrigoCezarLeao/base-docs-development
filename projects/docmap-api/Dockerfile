# syntax=docker/dockerfile:1
#
# Multi-stage build for the API. Name-agnostic on purpose: the same file works
# for any project created from this base (it discovers the *.Api project at build
# time), so it doubles as the canonical template in guidelines/infra-devops.md.
#
# Built and pushed to GHCR by CI — the VPS never compiles, it only pulls.

# ---- Build stage ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# NuGet.Config (pins nuget.org only) + lockfiles are already in the context.
COPY . .

# --locked-mode fails the build if packages.lock.json is out of date — reproducible.
RUN dotnet restore --locked-mode

# Publish only the web entry project. find keeps this independent of the namespace.
RUN API_CSPROJ="$(find src -maxdepth 2 -name '*.Api.csproj' | head -n1)" \
    && dotnet publish "$API_CSPROJ" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage -------------------------------------------------------------
# Alpine keeps the image small (~110 MB) to save disk on the VPS.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Run as an unprivileged user — never root inside the container.
RUN addgroup -S app && adduser -S -G app app
USER app

COPY --from=build --chown=app:app /app/publish .

# Build metadata (passed by CI via --build-arg) — surfaced at /version and /health.
ARG APP_COMMIT=local
ARG APP_BUILD_TIME=unknown

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_GCHeapHardLimitPercent=50 \
    APP_COMMIT=$APP_COMMIT \
    APP_BUILD_TIME=$APP_BUILD_TIME

EXPOSE 8080

# exec so PID 1 is dotnet (clean SIGTERM handling on `docker compose down`).
# ls *.Api.dll resolves the entry assembly regardless of the project name.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet \"$(ls *.Api.dll)\""]
