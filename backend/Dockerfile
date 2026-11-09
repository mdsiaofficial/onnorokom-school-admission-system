# ==============================================================================
# Stage 1: Build & Publish
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching dependency restore
COPY ["src/SchoolAdmission.Api/SchoolAdmission.Api.csproj", "src/SchoolAdmission.Api/"]
RUN dotnet restore "src/SchoolAdmission.Api/SchoolAdmission.Api.csproj"

# Copy full repository source
COPY . .

# Build and publish API
WORKDIR "/src/src/SchoolAdmission.Api"
RUN dotnet publish "SchoolAdmission.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 2: Test Runner (used with Docker Compose test profile)
# ==============================================================================
FROM build AS test-runner
WORKDIR /src
ENTRYPOINT ["dotnet", "test", "--logger:console;verbosity=normal"]

# ==============================================================================
# Stage 3: Production Runtime (aspnet:10.0-alpine)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Install icu-libs for globalization and curl/wget for healthchecks
RUN apk add --no-cache icu-libs wget curl

COPY --from=build /app/publish .

# Run as non-root user (app UID 1654 in .NET Alpine images)
USER $APP_UID

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
