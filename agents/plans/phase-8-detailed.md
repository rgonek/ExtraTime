# Phase 8: Deployment & Launch - Updated Plan

## Overview
Deploy the ExtraTime MVP to Azure production environment using **Infrastructure as Code (Bicep)** with zero-cost guarantees.

**Region**: North Europe
**Domain**: extratime.gonek.net
**Background Jobs**: Azure Functions (Consumption plan - replaces Hangfire)
**Deployment**: GitHub Actions pipeline with manual approval gates

---

## Implementation Status

### Done (already in codebase)
| Item | Files |
|------|-------|
| Bicep IaC (all modules) | `infrastructure/main.bicep`, `infrastructure/modules/*.bicep` |
| Deploy/teardown scripts | `infrastructure/scripts/deploy.ps1`, `grant-keyvault-access.ps1`, `teardown.ps1` |
| CI/CD pipeline (3-stage) | `.github/workflows/pipeline.yml` |
| Production app settings | `src/ExtraTime.API/appsettings.Production.json` |
| Key Vault integration | `Program.cs` - `DefaultAzureCredential()` in production |
| Frontend env template | `web/.env.production` |
| Azure Functions Bicep | `infrastructure/modules/functions.bicep` (Consumption Y1, daily quota, scale limit 1) |

### Remaining Code Changes

#### 1. Remove Hangfire config from appsettings.Production.json
Hangfire was replaced by Azure Functions. Remove the stale config:
```json
// REMOVE this block from appsettings.Production.json:
"Hangfire": {
  "DashboardPath": "/hangfire",
  "WorkerCount": 1
}
```

#### 2. Add Azure Functions deployment to pipeline.yml
The pipeline deploys API + SWA but not Azure Functions. Add to the deploy job:
```yaml
- name: Publish Functions
  run: dotnet publish src/ExtraTime.Functions/ExtraTime.Functions.csproj -c Release -o ./publish/functions

- name: Deploy Azure Functions
  uses: Azure/functions-action@v1
  with:
    app-name: ${{ secrets.AZURE_FUNCTIONS_NAME }}
    package: ./publish/functions
    publish-profile: ${{ secrets.AZURE_FUNCTIONS_PUBLISH_PROFILE }}
```

New GitHub secrets needed:
- `AZURE_FUNCTIONS_NAME` - Function App name from deployment outputs
- `AZURE_FUNCTIONS_PUBLISH_PROFILE` - Function App publish profile

#### 3. Create web/staticwebapp.config.json
SPA fallback routing for Azure Static Web Apps:
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/*.ico", "/*.svg", "/*.png"]
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  }
}
```

---

## Remaining Manual Steps

### 1. Azure Account & Prerequisites
- [ ] Sign up at https://azure.microsoft.com/free (credit card required, won't be charged for free tier)
- [ ] Install Azure CLI: `winget install Microsoft.AzureCLI`
- [ ] Login: `az login`
- [ ] Register providers:
  ```powershell
  az provider register --namespace Microsoft.Sql
  az provider register --namespace Microsoft.Web
  az provider register --namespace Microsoft.KeyVault
  az provider register --namespace Microsoft.Insights
  ```

### 2. Deploy Infrastructure
- [ ] Prepare secrets: SQL password (8+ chars, complex), JWT secret (32+ chars), Football Data API key
- [ ] Preview deployment:
  ```powershell
  ./infrastructure/scripts/deploy.ps1 -WhatIf `
      -SqlAdminPassword (Read-Host -AsSecureString "SQL Password") `
      -JwtSecret (Read-Host -AsSecureString "JWT Secret") `
      -FootballDataApiKey (Read-Host -AsSecureString "Football API Key")
  ```
- [ ] Deploy for real (same command without `-WhatIf`)
- [ ] Run Key Vault access script:
  ```powershell
  ./infrastructure/scripts/grant-keyvault-access.ps1 `
      -ResourceGroupName "extratime-prod-rg" `
      -WebAppName <from deployment output> `
      -KeyVaultName <from deployment output>
  ```

### 3. Update Config with Real Values
- [ ] Set `KeyVault.Name` in `appsettings.Production.json` to actual vault name
- [ ] Set `NEXT_PUBLIC_API_URL` in `web/.env.production` to actual API URL
- [ ] Commit these changes

### 4. Database Setup (first time)
```powershell
$sqlFqdn = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.sqlServerFqdn.value" -o tsv

$env:ConnectionStrings__DefaultConnection = "Server=tcp:$sqlFqdn,1433;Initial Catalog=extratime-db;User ID=extratimeadmin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

dotnet ef database update `
    --project src/ExtraTime.Infrastructure `
    --startup-project src/ExtraTime.API `
    --configuration Release
```
Note: Subsequent migrations run automatically in the pipeline deploy stage.

### 5. GitHub Configuration
#### Secrets (Settings > Secrets > Actions)
| Secret | Source |
|--------|--------|
| `DB_CONNECTION_STRING` | SQL Server connection string from deployment |
| `AZURE_WEBAPP_NAME` | App Service name from deployment output |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Azure Portal > App Service > Get publish profile |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | `az staticwebapp secrets list --name <swa-name>` |
| `AZURE_FUNCTIONS_NAME` | Function App name from deployment output |
| `AZURE_FUNCTIONS_PUBLISH_PROFILE` | Azure Portal > Function App > Get publish profile |

#### Environments (Settings > Environments)
- [ ] Create `verification` environment - required reviewers: yourself
- [ ] Create `production` environment - required reviewers: yourself, branch: `main` only

### 6. Custom Domain
- [ ] Add DNS CNAME: `extratime` -> `<swa-default-hostname>.azurestaticapps.net` (TTL: 3600)
- [ ] Add DNS TXT: `_dnsauth.extratime` -> value from Azure Portal > SWA > Custom domains
- [ ] Register domain:
  ```powershell
  az staticwebapp hostname set `
      --name <swa-name> `
      --resource-group extratime-prod-rg `
      --hostname extratime.gonek.net
  ```
- [ ] Wait for SSL certificate provisioning (~5 min)

---

## Post-Deployment

### Smoke Tests
```powershell
$apiUrl = "https://extratime-api-xxx.azurewebsites.net"
$frontendUrl = "https://extratime.gonek.net"

Invoke-RestMethod -Uri "$apiUrl/api/health"
Invoke-RestMethod -Uri "$apiUrl/api/competitions"
Invoke-WebRequest -Uri $frontendUrl -UseBasicParsing
```

### Critical Flow Checklist
- [ ] User registration
- [ ] User login
- [ ] Create league
- [ ] Join league via invite code
- [ ] Place bet on upcoming match
- [ ] View leaderboard
- [ ] Azure Functions running (check Application Insights)

---

## Zero-Cost Verification

### Free Tier Limits
| Service | Free Limit | Safeguard |
|---------|-----------|-----------|
| Azure SQL | 100K vCore sec/month, 32 GB | `freeLimitExhaustionBehavior: AutoPause` |
| App Service F1 | 60 CPU min/day, 1 GB RAM | Hard limit (always free) |
| Azure Functions Y1 | 1M exec/month, 400K GB-sec | Daily quota: 10,000 GB-sec, scale limit: 1 |
| Static Web Apps | 100 GB bandwidth/month | Free tier SKU |
| Application Insights | 5 GB/month | Daily cap: 0.16 GB |
| Key Vault | 10K operations/month | Standard tier |
| Storage (Functions) | LRS, Cool tier | Soft delete disabled |

### Budget Alert
```powershell
az consumption budget create `
    --resource-group extratime-prod-rg `
    --budget-name "zero-cost-alert" `
    --amount 0.01 `
    --time-grain Monthly `
    --category Cost
```

### Monthly Verification
- [ ] Check Azure Cost Management dashboard
- [ ] Verify SQL database auto-pauses when idle
- [ ] Check Application Insights ingestion < 5 GB
- [ ] Verify Functions execution count < 1M

---

## Security Checklist
- [ ] SQL Server firewall only allows Azure services
- [ ] Key Vault uses RBAC authorization
- [ ] Managed identity for Key Vault access (no secrets in code)
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured for specific origins only
- [ ] Secrets stored in Key Vault, not in code/config
- [ ] SQL connections use encryption
- [ ] Application Insights not logging sensitive data
- [ ] No secrets in Git repository

---

## Rollback

### Backend
```powershell
# Redeploy previous version from GitHub Actions
# Go to Actions > Select previous successful run > Re-run deploy job
```

### Full Infrastructure
```powershell
./infrastructure/scripts/teardown.ps1   # Deletes everything
./infrastructure/scripts/deploy.ps1 ... # Redeploy from scratch
```

---

## Quick Reference
```powershell
# Deploy infrastructure
./infrastructure/scripts/deploy.ps1 -SqlAdminPassword ... -JwtSecret ... -FootballDataApiKey ...

# View deployment outputs
az deployment sub show --name "extratime-deployment-*" --query properties.outputs

# Stream API logs
az webapp log tail --name <webapp-name> --resource-group extratime-prod-rg

# Check SQL database status
az sql db show --name extratime-db --server <sql-server> --resource-group extratime-prod-rg --query status

# Teardown
./infrastructure/scripts/teardown.ps1
```
