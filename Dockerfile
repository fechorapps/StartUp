# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Production

WORKDIR /src

# Copy solution and project files
COPY ["DoorX.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Build.targets", "./"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/API/API.csproj", "src/API/"]

# Restore dependencies
RUN dotnet restore "DoorX.sln"

# Copy all source code
COPY ["src/", "src/"]

# Build solution
RUN dotnet build "DoorX.sln" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore

# Publish API project
RUN dotnet publish "src/API/API.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Create non-root user
RUN groupadd -r doorx && \
    useradd -r -g doorx -s /bin/false doorx && \
    chown -R doorx:doorx /app

# Copy published files from build stage
COPY --from=build --chown=doorx:doorx /app/publish .

# Switch to non-root user
USER doorx

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "API.dll"]
