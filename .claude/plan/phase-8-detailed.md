# Phase 8: Deployment & Launch - Detailed Plan

## Overview
Deploy the ExtraTime MVP to Azure production environment with manual deployment approval and automated CI/CD pipeline.

**Region**: North Europe (closest to Poland, low latency)  
**Environment**: Production only  
**Domain**: Default Azure URLs  
**Secrets**: Azure Key Vault (free tier - 10,000 transactions/month)  
**Deployment**: Manual approval gate with automated build/test on push

---

## Table of Contents
1. [Prerequisites & Azure Setup](#1-prerequisites--azure-setup)
2. [Infrastructure Provisioning](#2-infrastructure-provisioning)
3. [Database Setup](#3-database-setup)
4. [Secrets Management](#4-secrets-management)
5. [Backend Deployment](#5-backend-deployment)
6. [Frontend Deployment](#6-frontend-deployment)
7. [Azure Functions Setup](#7-azure-functions-setup)
8. [CI/CD Pipeline](#8-cicd-pipeline)
9. [Monitoring & Logging](#9-monitoring--logging)
10. [Post-Deployment Testing](#10-post-deployment-testing)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Prerequisites & Azure Setup

### 1.1 Create Azure Account
- [ ] Sign up at https://azure.microsoft.com/free
- [ ] Verify with credit card (required, but won't be charged for free tier resources)
- [ ] Claim $200 free credit for 30 days (useful for testing paid features)

### 1.2 Install Azure CLI
```powershell
# Windows (PowerShell)
winget install Microsoft.AzureCLI

# Verify installation
az --version
```

### 1.3 Login to Azure
```powershell
az login
# Opens browser for authentication
```

### 1.4 Set Default Subscription & Region
```powershell
# List subscriptions
az account list --output table

# Set default subscription
az account set --subscription "Your-Subscription-Name"

# Set default location
az config set defaults.location=northeurope
```

### 1.5 Create Resource Group
```powershell
az group create --name extratime-prod-rg --location northeurope
```

---

## 2. Infrastructure Provisioning

### 2.1 Azure SQL Database (Free Tier)

```powershell
# Create SQL Server
az sql server create `
  --name extratime-prod-sql `
  --resource-group extratime-prod-rg `
  --location northeurope `
  --admin-user extratimeadmin `
  --admin-password "YourStrongPassword123!"

# Allow Azure services to access
az sql server firewall-rule create `
  --resource-group extratime-prod-rg `
  --server extratime-prod-sql `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Create database (Free tier - 32GB limit)
az sql db create `
  --resource-group extratime-prod-rg `
  --server extratime-prod-sql `
  --name extratime-db `
  --service-objective Free `
  --max-size 32GB
```

**Connection String** (save for later):
```
Server=tcp:extratime-prod-sql.database.windows.net,1433;Initial Catalog=extratime-db;Persist Security Info=False;User ID=extratimeadmin;Password=YourStrongPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 2.2 Azure Key Vault (Free Tier)

```powershell
# Create Key Vault (unique name required globally)
az keyvault create `
  --name extratime-prod-kv `
  --resource-group extratime-prod-rg `
  --location northeurope `
  --sku standard

# Add secrets
az keyvault secret set `
  --vault-name extratime-prod-kv `
  --name DatabaseConnectionString `
  --value "Server=tcp:extratime-prod-sql.database.windows.net,1433;Initial Catalog=extratime-db;..."

az keyvault secret set `
  --vault-name extratime-prod-kv `
  --name JwtSecret `
  --value "your-super-secret-jwt-key-min-32-chars-long"

az keyvault secret set `
  --vault-name extratime-prod-kv `
  --name FootballDataApiKey `
  --value "your-football-data-api-key"
```

### 2.3 Azure App Service (Backend API)

```powershell
# Create App Service Plan (Free tier F1)
az appservice plan create `
  --name extratime-prod-plan `
  --resource-group extratime-prod-rg `
  --sku F1 `
  --is-linux `
  --location northeurope

# Create Web App
az webapp create `
  --name extratime-api-prod `
  --resource-group extratime-prod-rg `
  --plan extratime-prod-plan `
  --runtime "DOTNETCORE:9.0"

# Enable managed identity for Key Vault access
az webapp identity assign `
  --name extratime-api-prod `
  --resource-group extratime-prod-rg

# Grant Key Vault access to managed identity
$principalId = az webapp identity show --name extratime-api-prod --resource-group extratime-prod-rg --query principalId -o tsv
az keyvault set-policy `
  --name extratime-prod-kv `
  --object-id $principalId `
  --secret-permissions get list
```

### 2.4 Azure Static Web Apps (Frontend)

```powershell
# Create Static Web App
az staticwebapp create `
  --name extratime-web-prod `
  --resource-group extratime-prod-rg `
  --location northeurope `
  --source https://github.com/yourusername/extratime `
  --branch main `
  --app-location "frontend" `
  --output-location "dist" `
  --login-with-github

# Note: This creates the resource but we'll configure deployment separately
# for manual approval workflow
```

### 2.5 Azure Functions (Background Jobs)

```powershell
# Create Function App
az functionapp create `
  --name extratime-functions-prod `
  --resource-group extratime-prod-rg `
  --consumption-plan-location northeurope `
  --runtime dotnet-isolated `
  --functions-version 4 `
  --storage-account extratimestorageprod `
  --os-type Windows

# Enable managed identity
az functionapp identity assign `
  --name extratime-functions-prod `
  --resource-group extratime-prod-rg

# Grant Key Vault access
$funcPrincipalId = az functionapp identity show --name extratime-functions-prod --resource-group extratime-prod-rg --query principalId -o tsv
az keyvault set-policy `
  --name extratime-prod-kv `
  --object-id $funcPrincipalId `
  --secret-permissions get list
```

### 2.6 Application Insights (Monitoring)

```powershell
# Create Application Insights
az monitor app-insights component create `
  --app extratime-insights-prod `
  --location northeurope `
  --resource-group extratime-prod-rg `
  --application-type web

# Get instrumentation key
az monitor app-insights component show `
  --app extratime-insights-prod `
  --resource-group extratime-prod-rg `
  --query instrumentationKey -o tsv
```

---

## 3. Database Setup

### 3.1 Run Migrations

**Option A: Local Tooling (Recommended for first deployment)**
```powershell
# Install EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Set environment variable for production connection
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:ConnectionStrings__DefaultConnection="Server=tcp:extratime-prod-sql.database.windows.net,1433;Initial Catalog=extratime-db;..."

# Run migrations
cd src/ExtraTime.Infrastructure
dotnet ef database update --startup-project ../ExtraTime.API
```

**Option B: Azure DevOps Pipeline (For subsequent deployments)**
- Add migration step to CI/CD pipeline
- Use Azure Service Connection for secure database access

### 3.2 Seed Initial Data

```powershell
# Run seeder (create admin user, initial competitions)
# This will be done via API endpoint or direct SQL
```

---

## 4. Secrets Management

### 4.1 Backend Configuration

Update `appsettings.Production.json`:
```json
{
  "KeyVault": {
    "Name": "extratime-prod-kv"
  },
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretName=DatabaseConnectionString)"
  },
  "Jwt": {
    "Secret": "@Microsoft.KeyVault(SecretName=JwtSecret)",
    "Issuer": "extratime-api-prod.azurewebsites.net",
    "Audience": "extratime-web-prod.azurestaticapps.net",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "FootballData": {
    "ApiKey": "@Microsoft.KeyVault(SecretName=FootballDataApiKey)",
    "BaseUrl": "https://api.football-data.org/v4",
    "RateLimitPerMinute": 10
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

### 4.2 Configure App Service Settings

```powershell
# Set general settings
az webapp config appsettings set `
  --name extratime-api-prod `
  --resource-group extratime-prod-rg `
  --settings ASPNETCORE_ENVIRONMENT=Production

# Enable Key Vault reference resolution
az webapp config appsettings set `
  --name extratime-api-prod `
  --resource-group extratime-prod-rg `
  --settings @appsettings.Production.json
```

### 4.3 Frontend Environment Variables

Create `frontend/.env.production`:
```env
NEXT_PUBLIC_API_URL=https://extratime-api-prod.azurewebsites.net/api
NEXT_PUBLIC_APP_URL=https://extratime-web-prod.azurestaticapps.net
```

---

## 5. Backend Deployment

### 5.1 Build & Publish

```powershell
# Build in Release mode
cd src/ExtraTime.API
dotnet publish -c Release -o ./publish

# Create deployment zip
Compress-Archive -Path ./publish/* -DestinationPath ./backend-deploy.zip -Force
```

### 5.2 Deploy to Azure

```powershell
# Deploy via ZIP deployment
az webapp deployment source config-zip `
  --resource-group extratime-prod-rg `
  --name extratime-api-prod `
  --src ./backend-deploy.zip
```

### 5.3 Verify Deployment

```powershell
# Check deployment status
az webapp show --name extratime-api-prod --resource-group extratime-prod-rg --query state

# Test health endpoint
curl https://extratime-api-prod.azurewebsites.net/api/health
```

---

## 6. Frontend Deployment

### 6.1 Build Production Bundle

```powershell
cd frontend

# Install dependencies
npm ci

# Build production bundle
npm run build

# Verify build output exists
Test-Path ./dist
```

### 6.2 Deploy to Static Web Apps

```powershell
# Deploy using SWA CLI
npm install -g @azure/static-web-apps-cli

swa deploy ./dist `
  --env production `
  --app-name extratime-web-prod `
  --resource-group extratime-prod-rg
```

### 6.3 Configure CORS

```powershell
# Allow frontend to call backend
az webapp cors add `
  --name extratime-api-prod `
  --resource-group extratime-prod-rg `
  --allowed-origins https://extratime-web-prod.azurestaticapps.net
```

---

## 7. Azure Functions Setup

### 7.1 Project Structure

Create new project `src/ExtraTime.Functions`:
```
ExtraTime.Functions/
├── Functions/
│   ├── SyncMatchesFunction.cs
│   ├── CalculateBetResultsFunction.cs
│   └── BotBettingFunction.cs
├── host.json
├── local.settings.json
└── ExtraTime.Functions.csproj
```

### 7.2 Deploy Functions

```powershell
cd src/ExtraTime.Functions

# Build and publish
func azure functionapp publish extratime-functions-prod
```

### 7.3 Configure Timer Triggers

Update `host.json`:
```json
{
  "version": "2.0",
  "functionTimeout": "00:10:00",
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  },
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  }
}
```

---

## 8. CI/CD Pipeline

### 8.1 GitHub Actions Workflow

Create `.github/workflows/ci-cd.yml`:

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:
    inputs:
      deploy_backend:
        description: 'Deploy Backend'
        required: true
        default: false
        type: boolean
      deploy_frontend:
        description: 'Deploy Frontend'
        required: true
        default: false
        type: boolean
      deploy_functions:
        description: 'Deploy Functions'
        required: true
        default: false
        type: boolean

env:
  AZURE_WEBAPP_NAME_BACKEND: extratime-api-prod
  AZURE_WEBAPP_NAME_FUNCTIONS: extratime-functions-prod
  AZURE_STATICWEBAPP_NAME: extratime-web-prod
  DOTNET_VERSION: '9.0.x'
  NODE_VERSION: '20'

jobs:
  # ==========================================
  # BUILD & TEST JOBS (Run on every push)
  # ==========================================
  
  build-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore src/ExtraTime.sln
      
      - name: Build
        run: dotnet build src/ExtraTime.sln --configuration Release --no-restore
      
      - name: Test
        run: dotnet test src/ExtraTime.sln --no-build --verbosity normal
      
      - name: Publish
        run: dotnet publish src/ExtraTime.API/ExtraTime.API.csproj -c Release -o ./publish/backend
      
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: backend-artifact
          path: ./publish/backend

  build-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      
      - name: Install dependencies
        working-directory: frontend
        run: npm ci
      
      - name: Lint
        working-directory: frontend
        run: npm run lint
      
      - name: Type check
        working-directory: frontend
        run: npm run typecheck
      
      - name: Build
        working-directory: frontend
        run: npm run build
      
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: frontend-artifact
          path: frontend/dist

  build-functions:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore src/ExtraTime.Functions/ExtraTime.Functions.csproj
      
      - name: Build
        run: dotnet build src/ExtraTime.Functions/ExtraTime.Functions.csproj --configuration Release
      
      - name: Publish
        run: dotnet publish src/ExtraTime.Functions/ExtraTime.Functions.csproj -c Release -o ./publish/functions
      
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: functions-artifact
          path: ./publish/functions

  # ==========================================
  # DEPLOYMENT JOBS (Manual approval required)
  # ==========================================
  
  deploy-backend:
    runs-on: ubuntu-latest
    needs: build-backend
    if: github.event.inputs.deploy_backend == 'true'
    environment:
      name: production-backend
      url: https://${{ env.AZURE_WEBAPP_NAME_BACKEND }}.azurewebsites.net
    
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: backend-artifact
          path: ./publish
      
      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME_BACKEND }}
          publish-profile: ${{ secrets.AZUREAPPSERVICE_PUBLISHPROFILE_BACKEND }}
          package: ./publish

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: build-frontend
    if: github.event.inputs.deploy_frontend == 'true'
    environment:
      name: production-frontend
      url: https://${{ env.AZURE_STATICWEBAPP_NAME }}.azurestaticapps.net
    
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: frontend-artifact
          path: ./dist
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "./dist"
          skip_app_build: true

  deploy-functions:
    runs-on: ubuntu-latest
    needs: build-functions
    if: github.event.inputs.deploy_functions == 'true'
    environment:
      name: production-functions
    
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: functions-artifact
          path: ./publish
      
      - name: Deploy to Azure Functions
        uses: azure/functions-action@v1
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME_FUNCTIONS }}
          package: ./publish
          publish-profile: ${{ secrets.AZUREAPPSERVICE_PUBLISHPROFILE_FUNCTIONS }}
```

### 8.2 Configure GitHub Secrets

Add these secrets in GitHub repository settings:

```
AZUREAPPSERVICE_PUBLISHPROFILE_BACKEND    # Download from Azure Portal > API App > Get publish profile
AZUREAPPSERVICE_PUBLISHPROFILE_FUNCTIONS  # Download from Azure Portal > Functions App > Get publish profile
AZURE_STATIC_WEB_APPS_API_TOKEN          # Get from Azure Portal > Static Web App > Manage deployment token
```

### 8.3 Environment Protection Rules

Configure in GitHub Settings > Environments:

**production-backend:**
- Required reviewers: 1 (your GitHub username)
- Wait timer: 0 minutes
- Deployment branches: main only

**production-frontend:**
- Required reviewers: 1 (your GitHub username)
- Wait timer: 0 minutes
- Deployment branches: main only

**production-functions:**
- Required reviewers: 1 (your GitHub username)
- Wait timer: 0 minutes
- Deployment branches: main only

---

## 9. Monitoring & Logging

### 9.1 Application Insights Configuration

**Backend - Program.cs:**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

**Frontend - Add to _app.tsx or layout:**
```typescript
import { ApplicationInsights } from '@microsoft/applicationinsights-web';

const appInsights = new ApplicationInsights({
  config: {
    instrumentationKey: process.env.NEXT_PUBLIC_APPINSIGHTS_INSTRUMENTATIONKEY,
    enableAutoRouteTracking: true,
  },
});

if (typeof window !== 'undefined') {
  appInsights.loadAppInsights();
}
```

### 9.2 Health Check Endpoint

Verify existing health check in `ExtraTime.API/Program.cs`:
```csharp
app.MapHealthChecks("/api/health");
app.MapHealthChecks("/health");
```

### 9.3 Azure Monitor Alerts

```powershell
# Create alert for high error rate
az monitor metrics alert create `
  --name "HighErrorRate" `
  --resource-group extratime-prod-rg `
  --scopes "/subscriptions/.../resourceGroups/extratime-prod-rg/providers/Microsoft.Web/sites/extratime-api-prod" `
  --condition "avg requests/failed > 10" `
  --evaluation-frequency 5m `
  --window-size 5m `
  --action "/subscriptions/.../resourceGroups/extratime-prod-rg/providers/Microsoft.Insights/actionGroups/email-action-group"
```

---

## 10. Post-Deployment Testing

### 10.1 Smoke Tests

```powershell
# Test health endpoint
$health = Invoke-RestMethod -Uri "https://extratime-api-prod.azurewebsites.net/api/health"
Write-Host "Health check: $($health.status)"

# Test API endpoints
$competitions = Invoke-RestMethod -Uri "https://extratime-api-prod.azurewebsites.net/api/competitions"
Write-Host "Competitions endpoint: OK ($($competitions.length) items)"

# Test frontend loads
$response = Invoke-WebRequest -Uri "https://extratime-web-prod.azurestaticapps.net"
Write-Host "Frontend status: $($response.StatusCode)"
```

### 10.2 Critical Flow Testing

- [ ] User registration
- [ ] User login
- [ ] Create league
- [ ] Join league via invite code
- [ ] Place bet on upcoming match
- [ ] View leaderboard
- [ ] View match list

### 10.3 Mobile Testing

- [ ] Test on iPhone Safari
- [ ] Test on Android Chrome
- [ ] Test responsive breakpoints
- [ ] Verify touch targets are large enough

---

## 11. Rollback Plan

### 11.1 Backend Rollback

```powershell
# View deployment slots
az webapp deployment list-publishing-profiles --name extratime-api-prod --resource-group extratime-prod-rg

# Rollback to previous deployment (if using deployment slots)
az webapp deployment slot swap --name extratime-api-prod --resource-group extratime-prod-rg --slot staging --target-slot production

# Or redeploy previous version from GitHub Actions
# Go to Actions tab > Select previous successful run > Re-run jobs
```

### 11.2 Frontend Rollback

```powershell
# Static Web Apps keeps deployment history
# Use Azure Portal to revert to previous deployment
# Or redeploy from previous GitHub Actions artifact
```

### 11.3 Database Rollback

```powershell
# Create backup before deployment
az sql db export `
  --name extratime-db `
  --server extratime-prod-sql `
  --storage-key-type SharedAccessKey `
  --storage-key "?sv=..." `
  --storage-uri "https://extratimestorageprod.blob.core.windows.net/backups/extratime-db-backup.bacpac" `
  --admin-user extratimeadmin `
  --admin-password "YourStrongPassword123!"

# Restore from backup if needed
az sql db import `
  --name extratime-db `
  --server extratime-prod-sql `
  --storage-key-type SharedAccessKey `
  --storage-key "?sv=..." `
  --storage-uri "https://extratimestorageprod.blob.core.windows.net/backups/extratime-db-backup.bacpac" `
  --admin-user extratimeadmin `
  --admin-password "YourStrongPassword123!"
```

---

## 12. Cost Monitoring

### 12.1 Free Tier Limits

| Service | Free Tier Limit | Current Usage |
|---------|----------------|---------------|
| Azure SQL | 32 GB storage | Monitor in Portal |
| App Service F1 | 1 GB RAM, shared CPU | Always free |
| Static Web Apps | 100 GB bandwidth/mo | Monitor in Portal |
| Functions | 1M executions/mo | Monitor in Portal |
| Key Vault | 10,000 transactions/mo | Monitor in Portal |
| Application Insights | 5 GB data/mo | Monitor in Portal |

### 12.2 Set Budget Alert

```powershell
# Create budget (optional - for peace of mind)
az consumption budget create `
  --resource-group extratime-prod-rg `
  --amount 10 `
  --time-grain monthly `
  --start-date 2026-01-01 `
  --end-date 2026-12-31 `
  --category cost `
  --notification-email your-email@example.com
```

---

## 13. Security Checklist

- [ ] SQL Server firewall rules restrict access to Azure services only
- [ ] Key Vault access limited to managed identities
- [ ] No secrets in code or environment variables (use Key Vault references)
- [ ] HTTPS only enforced on all endpoints
- [ ] CORS configured to allow only frontend origin
- [ ] JWT tokens use secure, rotated secrets
- [ ] Database connection strings use encrypted connections
- [ ] Application Insights doesn't log sensitive data
- [ ] Admin endpoints require authentication

---

## 14. Post-Launch Tasks

- [ ] Monitor error rates for 24 hours
- [ ] Check Application Insights for performance issues
- [ ] Verify scheduled jobs are running (Functions)
- [ ] Test football data sync is working
- [ ] Create admin user account
- [ ] Seed initial competitions data
- [ ] Send launch announcement to friends
- [ ] Collect feedback for Phase 9 planning

---

## Estimated Timeline

| Task | Estimated Time |
|------|---------------|
| Azure account setup | 30 min |
| Infrastructure provisioning | 1 hour |
| Database setup & migrations | 30 min |
| Secrets configuration | 30 min |
| Backend deployment | 30 min |
| Frontend deployment | 30 min |
| Functions setup | 1 hour |
| CI/CD pipeline setup | 1 hour |
| Testing & verification | 1 hour |
| **Total** | **~6-7 hours** |

---

## Resources & References

- [Azure Free Tier Documentation](https://azure.microsoft.com/free/)
- [Azure SQL Free Tier](https://docs.microsoft.com/azure/azure-sql/database/free-offer)
- [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps/)
- [Azure Functions Pricing](https://azure.microsoft.com/pricing/details/functions/)
- [GitHub Actions Environments](https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment)
