# GitHub Actions CI/CD

Este directorio contiene los workflows de GitHub Actions para CI/CD de DoorX.

## 📁 Estructura

```
.github/
└── workflows/
    ├── ci.yml              # Build & Test principal
    ├── code-quality.yml    # Análisis de código y seguridad
    ├── deploy-dev.yml      # Deploy a Development
    ├── deploy-staging.yml  # Deploy a Staging
    └── deploy-prod.yml     # Deploy a Production
```

## 🚀 Quick Start

### 1. Configurar Secrets

Ir a: **Settings → Secrets and variables → Actions → New repository secret**

Agregar:
```
DEV_SERVER_HOST
DEV_SERVER_USER
DEV_SERVER_SSH_KEY
STAGING_SERVER_HOST
STAGING_SERVER_USER
STAGING_SERVER_SSH_KEY
PROD_SERVER_HOST
PROD_SERVER_USER
PROD_SERVER_SSH_KEY
```

### 2. Configurar Environments

Ir a: **Settings → Environments → New environment**

Crear 3 ambientes:
1. **development**
2. **staging**
3. **production** (marcar "Required reviewers")

Para cada ambiente, agregar variables:
```
DEV_URL=http://dev.doorx.local
STAGING_URL=https://staging.doorx.app
PROD_URL=https://doorx.app
```

### 3. Habilitar Container Registry

Los workflows usan GitHub Container Registry (ghcr.io) automáticamente.

Permisos necesarios:
- Settings → Actions → General → Workflow permissions
- Marcar "Read and write permissions"

## 📋 Workflows

### CI Pipeline

**Trigger:** Push, PR
**Duración:** ~5 minutos

```yaml
Jobs:
  - build         # Compilar solución
  - test-unit     # Tests unitarios
  - test-integration # Tests de integración
  - publish       # Generar artifacts
```

**Artifacts:**
- `build-output/` - Binarios compilados
- `deployment-package/` - Package 7z
- Test results y coverage

### Code Quality

**Trigger:** Push a main/develop, PR, Semanal
**Duración:** ~3 minutos

```yaml
Jobs:
  - code-analysis     # .NET analyzers
  - security-scan     # Vulnerabilidades
  - secret-scan       # Secrets expuestos
  - test-coverage     # Cobertura 80%
  - dependency-review # Revisión de deps
```

### Deploy Development

**Trigger:** Push a main (automático)

```yaml
Steps:
  1. Build Docker image
  2. Push to ghcr.io
  3. Generate docker-compose
  4. Deploy (opcional via SSH)
  5. Smoke tests
```

**Tags generados:**
- `dev-latest`
- `main-{sha}`

### Deploy Staging

**Trigger:** Tag `v*-rc*` (ej: `v1.0.0-rc1`)

```yaml
Steps:
  1. Extract version
  2. Build versioned image
  3. Deploy to staging
  4. Integration tests
```

**Tags generados:**
- `v1.0.0-rc1`
- `1.0-rc1`
- `staging-latest`

### Deploy Production

**Trigger:** Tag `v*` (ej: `v1.0.0`) + Aprobación manual

```yaml
Steps:
  1. Pre-deployment checks
  2. Build production image
  3. Smoke tests
  4. Deploy (requiere aprobación)
  5. Create GitHub Release
  6. Post-deployment tasks
```

**Tags generados:**
- `v1.0.0`
- `1.0`
- `1`
- `latest`
- `production`

## 🎯 Uso

### Development

```bash
# Push a main → Deploy automático a Dev
git checkout main
git merge feature/my-feature
git push
```

### Staging

```bash
# Tag RC → Deploy a Staging
git tag v1.0.0-rc1 -m "Release candidate 1"
git push origin v1.0.0-rc1
```

### Production

```bash
# Tag release → Requiere aprobación
git tag v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Ir a Actions → Aprobar deployment
```

### Manual Trigger

```bash
# Via GitHub CLI
gh workflow run deploy-dev.yml

# Via GitHub UI
Actions → Select workflow → Run workflow
```

## 📦 Artifacts

### Download via GitHub CLI

```bash
# Listar runs
gh run list --limit 5

# Download artifact
gh run download <run-id> -n deployment-package

# Extraer 7z
7z x doorx-api-*.7z
```

### Download via UI

1. Actions → Select workflow run
2. Scroll to Artifacts
3. Click to download

## 🐛 Troubleshooting

### Build falla

```bash
# Verificar logs
gh run view <run-id> --log

# Re-run failed jobs
gh run rerun <run-id> --failed
```

### Tests fallan

```bash
# Download test results
gh run download <run-id> -n unit-test-results

# Ver TRX files
cat Domain.UnitTests/*.trx
```

### Deploy falla

```bash
# Check secrets están configurados
gh secret list

# Verificar environment variables
# Settings → Environments → [env] → Variables
```

## 📚 Documentación Completa

Ver [docs/CICD.md](../docs/CICD.md) para documentación detallada.

## 🔗 Links Útiles

- [GitHub Actions Docs](https://docs.github.com/actions)
- [Workflow Syntax](https://docs.github.com/actions/reference/workflow-syntax-for-github-actions)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [GitHub Container Registry](https://docs.github.com/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
