# DoorX - Guía de CI/CD

> Documentación completa del pipeline de integración y despliegue continuo

## 📋 Tabla de Contenidos

1. [Resumen del Pipeline](#resumen-del-pipeline)
2. [Workflows de GitHub Actions](#workflows-de-github-actions)
3. [Configuración de Ambientes](#configuración-de-ambientes)
4. [Configuración de Secrets](#configuración-de-secrets)
5. [Build y Artifacts](#build-y-artifacts)
6. [Docker y Despliegue](#docker-y-despliegue)
7. [Comandos Útiles](#comandos-útiles)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Resumen del Pipeline

### Flujo General

```
┌─────────────┐
│  Git Push   │
└──────┬──────┘
       │
       ├─────────────────────────────────┐
       │                                 │
       ▼                                 ▼
┌──────────────┐              ┌──────────────────┐
│  CI Pipeline │              │  Code Quality    │
├──────────────┤              ├──────────────────┤
│ ✓ Build      │              │ ✓ Analysis       │
│ ✓ Unit Tests │              │ ✓ Security       │
│ ✓ Int. Tests │              │ ✓ Coverage       │
│ ✓ Artifacts  │              │ ✓ Dependencies   │
└──────┬───────┘              └──────────────────┘
       │
       │ (merge to main)
       ▼
┌──────────────┐
│  Deploy Dev  │ ←─── Automático
└──────┬───────┘
       │
       │ (tag v*-rc*)
       ▼
┌──────────────┐
│Deploy Staging│ ←─── Semi-automático
└──────┬───────┘
       │
       │ (tag v* + approval)
       ▼
┌──────────────┐
│ Deploy Prod  │ ←─── Manual con aprobación
└──────────────┘
```

### Workflows Disponibles

| Workflow | Trigger | Duración | Propósito |
|----------|---------|----------|-----------|
| **CI** | Push, PR | ~5 min | Build, tests, artifacts |
| **Code Quality** | Push a main/develop, PR | ~3 min | Análisis, seguridad, cobertura |
| **Deploy Dev** | Merge a main | ~4 min | Deploy automático a Dev |
| **Deploy Staging** | Tag `v*-rc*` | ~5 min | Deploy a Staging |
| **Deploy Prod** | Tag `v*` | ~7 min | Deploy a Production |

---

## 🔄 Workflows de GitHub Actions

### 1. CI Pipeline (`.github/workflows/ci.yml`)

**Trigger:**
- Push a cualquier branch
- Pull requests a `main` o `develop`
- Manual dispatch

**Jobs:**

#### Build
```yaml
- Checkout código
- Setup .NET 8.0
- Cache NuGet packages
- Restore dependencies
- Build solution (Release)
- Upload build artifacts
```

#### Unit Tests
```yaml
- Run Domain.UnitTests
- Run Application.UnitTests
- Run Infrastructure.UnitTests
- Generate coverage reports
- Upload test results
```

#### Integration Tests
```yaml
- Start PostgreSQL service
- Run API.IntegrationTests
- Run Infrastructure.IntegrationTests
- Generate coverage reports
```

#### Publish
```yaml
- Publish API project
- Compress to 7z
- Generate build info
- Upload deployment package
```

**Artifacts generados:**
- `build-output/` - Binarios compilados
- `deployment-package/` - Package 7z con build info
- `unit-test-results/` - Resultados de tests unitarios
- `integration-test-results/` - Resultados de tests de integración
- `*-test-coverage/` - Reportes de cobertura

---

### 2. Code Quality (`.github/workflows/code-quality.yml`)

**Trigger:**
- Push a `main` o `develop`
- Pull requests
- Manual dispatch
- Scheduled (Lunes 9:00 AM UTC)

**Jobs:**

#### Code Analysis
```yaml
- .NET Code Analyzer
- dotnet format check
- Code style enforcement
```

#### Security Scan
```yaml
- Scan vulnerable NuGet packages
- Check transitive dependencies
- Upload vulnerability report
```

#### Secret Scan
```yaml
- TruffleHog secret detection
- Scan commits for exposed credentials
```

#### Test Coverage
```yaml
- Run all tests with coverage
- Generate HTML report
- Check 80% threshold
- Add summary to PR
```

#### Dependency Review (solo PRs)
```yaml
- Review new dependencies
- Check license compliance
- Fail on GPL-3.0, AGPL-3.0
```

---

### 3. Deploy Development (`.github/workflows/deploy-dev.yml`)

**Trigger:**
- Push a `main` (automático)
- Manual dispatch

**Proceso:**

1. **Build Docker Image**
   ```bash
   docker build -t ghcr.io/user/doorx-api:dev-latest .
   docker push ghcr.io/user/doorx-api:dev-latest
   ```

2. **Generate Deployment Package**
   - `docker-compose.dev.yml`
   - `deploy.sh` script
   - Health check scripts

3. **Deploy (opcional con SSH)**
   ```bash
   ssh user@dev-server
   cd /opt/doorx
   docker compose pull
   docker compose up -d
   dotnet ef database update
   ```

4. **Smoke Tests**
   - Health endpoint check
   - Basic API validation

**Ambiente:** `development`
**URL:** Configurada en `vars.DEV_URL`

---

### 4. Deploy Staging (`.github/workflows/deploy-staging.yml`)

**Trigger:**
- Tag `v*-rc*` (ej: `v1.0.0-rc1`)
- Manual dispatch con tag

**Proceso:**

1. **Pre-checks**
   - Validate version format
   - Extract semver

2. **Build Versioned Image**
   ```
   Tags:
   - v1.0.0-rc1
   - 1.0-rc1
   - staging-latest
   ```

3. **Deploy to Staging**
   - Pull images
   - Down current version
   - Up new version
   - Health check with retries

4. **Integration Tests**
   - Run against staging URL
   - Validate endpoints

**Ambiente:** `staging`
**Retención de artifacts:** 90 días

---

### 5. Deploy Production (`.github/workflows/deploy-prod.yml`)

**Trigger:**
- Tag `v*` (ej: `v1.0.0`)
- Manual dispatch con tag

**Proceso:**

1. **Pre-deployment Checks**
   - Validate version format (X.Y.Z)
   - Generate changelog
   - Check existing deployments

2. **Build Production Image**
   ```
   Tags:
   - v1.0.0
   - 1.0
   - 1
   - latest
   - production
   ```

3. **Smoke Tests**
   - Pre-deployment validation
   - Can be skipped with flag

4. **Deploy to Production** (requires approval)
   - Database backup
   - Pull new images
   - Zero-downtime deployment
   - Health check with rollback

5. **Post-deployment**
   - Create GitHub Release
   - Send notifications
   - Update documentation

**Ambiente:** `production`
**Aprobación:** Manual (configurar en GitHub)
**Retención de artifacts:** 365 días

---

## 🌍 Configuración de Ambientes

### Development

```yaml
Environment: development
URL: http://dev.doorx.local
Database: doorx_dev
Auto-deploy: Yes (on push to main)
Approval: None
```

**Variables:**
- `DEV_URL` - URL del ambiente de desarrollo
- `DEV_SERVER_HOST` - Host del servidor (opcional)

**Secrets:**
- `DEV_SERVER_USER` - Usuario SSH
- `DEV_SERVER_SSH_KEY` - Llave SSH privada

---

### Staging

```yaml
Environment: staging
URL: https://staging.doorx.app
Database: doorx_staging
Auto-deploy: No (tag v*-rc*)
Approval: None
```

**Variables:**
- `STAGING_URL` - URL del ambiente de staging
- `STAGING_SERVER_HOST` - Host del servidor

**Secrets:**
- `STAGING_SERVER_USER` - Usuario SSH
- `STAGING_SERVER_SSH_KEY` - Llave SSH privada
- `DATABASE_CONNECTION_STRING` - Connection string

---

### Production

```yaml
Environment: production
URL: https://doorx.app
Database: doorx_production
Auto-deploy: No (tag v* + approval)
Approval: Required
```

**Variables:**
- `PROD_URL` - URL de producción
- `PROD_SERVER_HOST` - Host del servidor

**Secrets:**
- `PROD_SERVER_USER` - Usuario SSH
- `PROD_SERVER_SSH_KEY` - Llave SSH privada
- `DATABASE_CONNECTION_STRING` - Connection string
- `OPENAI_API_KEY` - OpenAI API key
- `TWILIO_ACCOUNT_SID` - Twilio Account SID
- `TWILIO_AUTH_TOKEN` - Twilio Auth Token
- Todos los secrets del `.env.example`

---

## 🔐 Configuración de Secrets

### GitHub Repository Secrets

Ir a: **Settings → Secrets and variables → Actions**

#### Secrets Globales

```bash
# Git/GitHub
GITHUB_TOKEN # (Auto-generado por GitHub)

# Container Registry
# (Se usa GITHUB_TOKEN automáticamente para ghcr.io)
```

#### Secrets por Ambiente

**Development:**
```bash
DEV_SERVER_HOST=dev.doorx.local
DEV_SERVER_USER=deploy
DEV_SERVER_SSH_KEY=<private-key>
```

**Staging:**
```bash
STAGING_SERVER_HOST=staging.doorx.app
STAGING_SERVER_USER=deploy
STAGING_SERVER_SSH_KEY=<private-key>
```

**Production:**
```bash
PROD_SERVER_HOST=doorx.app
PROD_SERVER_USER=deploy
PROD_SERVER_SSH_KEY=<private-key>
DATABASE_CONNECTION_STRING=<connection-string>
OPENAI_API_KEY=<api-key>
TWILIO_ACCOUNT_SID=<account-sid>
TWILIO_AUTH_TOKEN=<auth-token>
```

### Variables de Ambiente

Ir a: **Settings → Environments**

Crear ambientes:
1. `development`
2. `staging`
3. `production` (con aprobación requerida)

Para cada ambiente, configurar variables:
```bash
DEV_URL=http://dev.doorx.local
STAGING_URL=https://staging.doorx.app
PROD_URL=https://doorx.app
```

---

## 📦 Build y Artifacts

### Estructura de Build

```
build/
├── API/
│   ├── Debug/
│   └── Release/
├── Domain/
├── Application/
├── Infrastructure/
└── obj/
    └── <intermediate files>
```

### Estructura de Artifacts

```
artifacts/
├── doorx-api-<commit-sha>.7z
└── build-info.txt
```

**build-info.txt:**
```
Build Information
=================
Repository: fechorapps/doorx
Branch: main
Commit: abc123...
Commit Message: feat: Add new feature
Author: developer
Build Date: 2024-11-24 10:30:00 UTC
Workflow: CI - Build & Test
Run Number: 42
```

### Descarga de Artifacts

Via GitHub Actions UI:
1. Ir a **Actions** tab
2. Seleccionar workflow run
3. Scroll down a **Artifacts**
4. Click para descargar

Via GitHub CLI:
```bash
# Listar artifacts
gh run list --limit 5

# Descargar artifact específico
gh run download <run-id> -n deployment-package

# Extraer 7z
7z x doorx-api-*.7z
```

---

## 🐳 Docker y Despliegue

### Build Local

```bash
# Build imagen
docker build -t doorx-api:local .

# Build con build args
docker build \
  --build-arg BUILD_CONFIGURATION=Release \
  --build-arg ASPNETCORE_ENVIRONMENT=Production \
  -t doorx-api:local .
```

### Run Local con Docker Compose

```bash
# Start todos los servicios
docker compose up -d

# Ver logs
docker compose logs -f doorx-api

# Stop servicios
docker compose down

# Rebuild y start
docker compose up -d --build
```

**Servicios disponibles:**
- **doorx-api**: http://localhost:5000
- **postgres**: localhost:5432
- **pgadmin**: http://localhost:5050

### Deploy Manual

#### Development

```bash
# Pull deployment package del artifact
gh run download <run-id> -n dev-deployment-package

# Extraer
7z x dev-deployment-package.7z

# Deploy
chmod +x deploy.sh
./deploy.sh
```

#### Staging

```bash
# Tag para staging
git tag v1.0.0-rc1
git push origin v1.0.0-rc1

# El workflow se ejecuta automáticamente
# Monitor en GitHub Actions
```

#### Production

```bash
# Tag para producción
git tag v1.0.0
git push origin v1.0.0

# Aprobar deployment en GitHub
# 1. Ir a Actions
# 2. Seleccionar workflow run
# 3. Click "Review deployments"
# 4. Aprobar "production"
```

---

## 🔧 Comandos Útiles

### GitHub CLI

```bash
# Ver runs recientes
gh run list --limit 10

# Ver detalles de un run
gh run view <run-id>

# Re-run failed jobs
gh run rerun <run-id> --failed

# Watch run en tiempo real
gh run watch <run-id>

# Trigger manual workflow
gh workflow run deploy-dev.yml

# Download artifacts
gh run download <run-id>
```

### Docker Commands

```bash
# Ver imágenes
docker images | grep doorx

# Pull imagen del registry
docker pull ghcr.io/fechorapps/doorx-api:latest

# Login a GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Tag y push
docker tag doorx-api:local ghcr.io/fechorapps/doorx-api:custom-tag
docker push ghcr.io/fechorapps/doorx-api:custom-tag

# Ver logs
docker logs doorx-api-prod -f --tail 100

# Execute comando en container
docker exec -it doorx-api-prod dotnet ef database update
```

### Database Migrations

```bash
# Dentro del container
docker compose exec doorx-api dotnet ef database update

# Crear nueva migración (local)
dotnet ef migrations add MigrationName -p src/Infrastructure -s src/API

# Listar migraciones
docker compose exec doorx-api dotnet ef migrations list
```

---

## 🐛 Troubleshooting

### Build Failures

**Problema:** Build falla con errores de restore
```
Solución:
- Verificar que todos los .csproj estén correctos
- Limpiar cache: dotnet clean
- Verificar Directory.Build.props
```

**Problema:** Tests fallan en CI pero pasan localmente
```
Solución:
- Verificar connection string de PostgreSQL
- Revisar que el servicio de postgres esté healthy
- Check environment variables
```

### Docker Issues

**Problema:** Container no inicia
```bash
# Ver logs detallados
docker compose logs doorx-api

# Ver health check status
docker inspect doorx-api-prod | grep -A 10 Health

# Restart container
docker compose restart doorx-api
```

**Problema:** No se puede conectar a la base de datos
```
Solución:
- Verificar que postgres esté running
- Check connection string
- Verificar network: docker network inspect doorx-network
```

### Deployment Failures

**Problema:** Health check falla después del deploy
```
Checklist:
- ✓ Container está running?
- ✓ Logs muestran errores?
- ✓ Database está accessible?
- ✓ Migrations aplicadas?
- ✓ Environment variables correctas?
```

**Problema:** SSH deployment falla
```
Solución:
- Verificar SSH_KEY secret está configurado
- Test SSH: ssh user@host
- Verificar permisos en /opt/doorx
```

### Rollback

**Development/Staging:**
```bash
# Pull previous image
docker compose pull doorx-api:previous-tag
docker compose up -d
```

**Production:**
```bash
# Usar script de rollback
./rollback.sh

# O manualmente
docker compose down
docker pull ghcr.io/fechorapps/doorx-api:v1.0.0  # previous version
docker compose up -d
```

---

## 📚 Referencias

### Documentación Relacionada

- [ARCHITECTURE.md](./ARCHITECTURE.md) - Arquitectura del sistema
- [README.md](../README.md) - Documentación principal
- [.env.example](../.env.example) - Variables de entorno

### Links Útiles

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Docker Documentation](https://docs.docker.com/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [PostgreSQL Docker](https://hub.docker.com/_/postgres)

---

## 🔄 Proceso de Release

### 1. Development

```bash
# Trabajar en feature branch
git checkout -b feature/new-feature
git commit -m "feat: Add new feature"
git push origin feature/new-feature

# Create PR
gh pr create --title "Add new feature" --body "Description"

# CI runs automáticamente
# Code review
# Merge to main → Deploy to Dev automático
```

### 2. Release Candidate

```bash
# Merge main to develop
git checkout develop
git merge main

# Create RC tag
git tag v1.0.0-rc1 -m "Release candidate 1"
git push origin v1.0.0-rc1

# Deploy to Staging automático
# QA testing
```

### 3. Production Release

```bash
# Si QA aprueba
git tag v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Workflow ejecuta
# Requiere aprobación manual
# Deploy to Production
# GitHub Release creado automáticamente
```

---

**Última actualización**: 2024-11-24
**Versión del documento**: 1.0.0
