# Phase 8: Deployment & Launch - Detailed Plan

## Overview
Deploy the ExtraTime MVP to Azure production environment using **Infrastructure as Code (Bicep)** with zero-cost guarantees.

**Region**: North Europe (closest to Poland, low latency)
**Environment**: Production only
**Domain**: extratime.gonek.net
**Secrets**: Azure Key Vault (free tier - 10,000 transactions/month)
**Background Jobs**: Hangfire (integrated with App Service - no extra cost)
**Deployment**: Manual approval gate with automated build/test on push

---

## Table of Contents
1. [Prerequisites & Azure Setup](#1-prerequisites--azure-setup)
2. [Infrastructure as Code (Bicep)](#2-infrastructure-as-code-bicep)
3. [Deploy Infrastructure](#3-deploy-infrastructure)
4. [Database Setup](#4-database-setup)
5. [Secrets Management](#5-secrets-management)
6. [Backend Deployment](#6-backend-deployment)
7. [Frontend Deployment](#7-frontend-deployment)
8. [Hangfire Background Jobs](#8-hangfire-background-jobs)
9. [Custom Domain Setup](#9-custom-domain-setup)
10. [CI/CD Pipeline](#10-cicd-pipeline)
11. [Monitoring & Logging](#11-monitoring--logging)
12. [Post-Deployment Testing](#12-post-deployment-testing)
13. [Rollback Plan](#13-rollback-plan)
14. [Zero-Cost Verification](#14-zero-cost-verification)

---

## 1. Prerequisites & Azure Setup

### 1.1 Create Azure Account
- [ ] Sign up at https://azure.microsoft.com/free
- [ ] Verify with credit card (required, but won't be charged for free tier resources)
- [ ] Claim $200 free credit for 30 days (useful for testing)

### 1.2 Install Required Tools
```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Install Bicep CLI (included with Azure CLI, but ensure latest)
az bicep install
az bicep upgrade

# Verify installations
az --version
az bicep version
```

### 1.3 Login to Azure
```powershell
az login
# Opens browser for authentication

# List subscriptions
az account list --output table

# Set default subscription
az account set --subscription "Your-Subscription-Name"
```

### 1.4 Register Required Resource Providers
```powershell
# These must be registered before deploying resources
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.KeyVault
az provider register --namespace Microsoft.Insights
```

---

## 2. Infrastructure as Code (Bicep)

### 2.1 Directory Structure

Create the following structure in the repository:

```
infrastructure/
├── main.bicep                    # Main orchestration template
├── main.bicepparam               # Parameter file for production
├── modules/
│   ├── sql-server.bicep          # Azure SQL Server + Database
│   ├── app-service.bicep         # App Service Plan + Web App
│   ├── static-web-app.bicep      # Static Web App for frontend
│   ├── key-vault.bicep           # Key Vault for secrets
│   └── app-insights.bicep        # Application Insights with daily cap
└── scripts/
    ├── deploy.ps1                # Deployment script
    └── teardown.ps1              # Cleanup script
```

### 2.2 Main Bicep Template

**File: `infrastructure/main.bicep`**

```bicep
// ============================================================================
// ExtraTime Production Infrastructure
// ============================================================================
// This template deploys all Azure resources for ExtraTime MVP
// All resources use FREE TIER with cost safeguards enabled
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Environment name')
@allowed(['prod'])
param environment string = 'prod'

@description('Azure region for resources')
param location string = 'northeurope'

@description('SQL Server administrator username')
param sqlAdminUsername string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@secure()
@description('JWT signing secret (min 32 characters)')
@minLength(32)
param jwtSecret string

@secure()
@description('Football Data API key')
param footballDataApiKey string

@description('GitHub repository URL for Static Web App')
param githubRepoUrl string = ''

@description('GitHub token for Static Web App deployment')
@secure()
param githubToken string = ''

// ============================================================================
// VARIABLES
// ============================================================================

var resourceGroupName = 'extratime-${environment}-rg'
var tags = {
  Environment: environment
  Project: 'ExtraTime'
  ManagedBy: 'Bicep'
  CostCenter: 'FreeTier'
}

// Unique suffix for globally unique resource names
var uniqueSuffix = uniqueString(subscription().subscriptionId, resourceGroupName)

// ============================================================================
// RESOURCE GROUP
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// MODULES
// ============================================================================

// Application Insights (with daily cap for zero cost)
module appInsights 'modules/app-insights.bicep' = {
  name: 'appInsights-deployment'
  scope: rg
  params: {
    name: 'extratime-insights-${environment}'
    location: location
    tags: tags
    dailyCapGb: '0.16' // ~5 GB/month (free tier limit)
  }
}

// Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault-deployment'
  scope: rg
  params: {
    name: 'extratime-kv-${uniqueSuffix}'
    location: location
    tags: tags
    sqlConnectionString: sqlServer.outputs.connectionString
    jwtSecret: jwtSecret
    footballDataApiKey: footballDataApiKey
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// SQL Server and Database (Free Tier with auto-pause)
module sqlServer 'modules/sql-server.bicep' = {
  name: 'sqlServer-deployment'
  scope: rg
  params: {
    serverName: 'extratime-sql-${uniqueSuffix}'
    databaseName: 'extratime-db'
    location: location
    tags: tags
    adminUsername: sqlAdminUsername
    adminPassword: sqlAdminPassword
  }
}

// App Service (Free Tier F1)
module appService 'modules/app-service.bicep' = {
  name: 'appService-deployment'
  scope: rg
  params: {
    appServicePlanName: 'extratime-plan-${environment}'
    webAppName: 'extratime-api-${uniqueSuffix}'
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// Static Web App (Free Tier)
module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'staticWebApp-deployment'
  scope: rg
  params: {
    name: 'extratime-web-${environment}'
    location: location
    tags: tags
    apiBaseUrl: appService.outputs.defaultHostName
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output resourceGroupName string = rg.name
output sqlServerName string = sqlServer.outputs.serverName
output sqlServerFqdn string = sqlServer.outputs.fullyQualifiedDomainName
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output appServiceName string = appService.outputs.webAppName
output appServiceDefaultHostName string = appService.outputs.defaultHostName
output appServicePrincipalId string = appService.outputs.principalId
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppDefaultHostName string = staticWebApp.outputs.defaultHostName
output appInsightsName string = appInsights.outputs.name
output appInsightsConnectionString string = appInsights.outputs.connectionString
```

### 2.3 SQL Server Module

**File: `infrastructure/modules/sql-server.bicep`**

```bicep
// ============================================================================
// Azure SQL Server + Database (Free Tier)
// ============================================================================
// FREE TIER LIMITS:
// - 100,000 vCore seconds/month
// - 32 GB storage
// - Auto-pause when limit exhausted (NO CHARGES)
// ============================================================================

@description('SQL Server name')
param serverName string

@description('Database name')
param databaseName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Administrator username')
param adminUsername string

@secure()
@description('Administrator password')
param adminPassword string

// ============================================================================
// SQL SERVER
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access (required for App Service)
resource firewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// DATABASE (Free Tier)
// ============================================================================

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    autoPauseDelay: 60 // Auto-pause after 60 minutes of inactivity
    minCapacity: json('0.5')
    requestedBackupStorageRedundancy: 'Local'
    // FREE TIER CONFIGURATION - CRITICAL FOR ZERO COST
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause' // STOPS when limit reached, NO CHARGES
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output serverName string = sqlServer.name
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
```

### 2.4 App Service Module

**File: `infrastructure/modules/app-service.bicep`**

```bicep
// ============================================================================
// Azure App Service (Free Tier F1)
// ============================================================================
// FREE TIER LIMITS:
// - 1 GB RAM
// - 1 GB storage
// - Shared infrastructure
// - 60 CPU minutes/day
// - No custom domain SSL (use Static Web App for frontend)
// ============================================================================

@description('App Service Plan name')
param appServicePlanName string

@description('Web App name')
param webAppName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Key Vault name for secret references')
param keyVaultName string

@description('Application Insights connection string')
param appInsightsConnectionString string

// ============================================================================
// APP SERVICE PLAN (Free Tier)
// ============================================================================

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'F1'      // FREE TIER
    tier: 'Free'
    size: 'F1'
    family: 'F'
    capacity: 1
  }
  properties: {
    reserved: true  // Required for Linux
  }
}

// ============================================================================
// WEB APP
// ============================================================================

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned' // Managed identity for Key Vault access
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: false // Not available in Free tier
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'KeyVault__Name'
          value: keyVaultName
        }
      ]
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output webAppName string = webApp.name
output defaultHostName string = 'https://${webApp.properties.defaultHostName}'
output principalId string = webApp.identity.principalId
output appServicePlanId string = appServicePlan.id
```

### 2.5 Key Vault Module

**File: `infrastructure/modules/key-vault.bicep`**

```bicep
// ============================================================================
// Azure Key Vault
// ============================================================================
// FREE TIER LIMITS:
// - 10,000 transactions/month (Standard tier)
// - Secrets, keys, certificates storage is free
// - Only charged for operations beyond limit
// ============================================================================

@description('Key Vault name (must be globally unique)')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@secure()
@description('SQL connection string')
param sqlConnectionString string

@secure()
@description('JWT secret')
param jwtSecret string

@secure()
@description('Football Data API key')
param footballDataApiKey string

@description('Application Insights connection string')
param appInsightsConnectionString string

// ============================================================================
// KEY VAULT
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true // Use RBAC instead of access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7 // Minimum retention
    enablePurgeProtection: false // Allow purge for dev/test
    publicNetworkAccess: 'Enabled'
  }
}

// ============================================================================
// SECRETS
// ============================================================================

resource secretSqlConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabaseConnectionString'
  properties: {
    value: sqlConnectionString
  }
}

resource secretJwt 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSecret'
  properties: {
    value: jwtSecret
  }
}

resource secretFootballApi 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FootballDataApiKey'
  properties: {
    value: footballDataApiKey
  }
}

resource secretAppInsights 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ApplicationInsightsConnectionString'
  properties: {
    value: appInsightsConnectionString
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
output resourceId string = keyVault.id
```

### 2.6 Application Insights Module

**File: `infrastructure/modules/app-insights.bicep`**

```bicep
// ============================================================================
// Application Insights (with Daily Cap for Zero Cost)
// ============================================================================
// FREE TIER LIMITS:
// - 5 GB data ingestion/month
// - 90 days data retention (free)
// - Daily cap prevents overage charges
// ============================================================================

@description('Application Insights name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Daily ingestion cap in GB (0.16 = ~5 GB/month)')
param dailyCapGb string = '0.16'

// ============================================================================
// LOG ANALYTICS WORKSPACE (Required for App Insights)
// ============================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-workspace'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018' // Pay-as-you-go (but we cap ingestion)
    }
    retentionInDays: 30 // Minimum to save costs
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    // Daily cap is set at Application Insights level
  }
}

// ============================================================================
// APPLICATION INSIGHTS
// ============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    // Sampling reduces data volume
    SamplingPercentage: 50
  }
}

// ============================================================================
// DAILY CAP (CRITICAL FOR ZERO COST)
// ============================================================================

resource dailyCap 'Microsoft.Insights/components/CurrentBillingFeatures@2015-05-01' = {
  name: '${appInsights.name}/CurrentBillingFeatures'
  properties: {
    CurrentBillingFeatures: ['Basic']
    DataVolumeCap: {
      Cap: json(dailyCapGb)
      ResetTime: 0
      StopSendNotificationWhenHitCap: false
      WarningThreshold: 80
      StopSendNotificationWhenHitThreshold: false
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output name string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
output workspaceId string = logAnalytics.id
```

### 2.7 Static Web App Module

**File: `infrastructure/modules/static-web-app.bicep`**

```bicep
// ============================================================================
// Azure Static Web App (Free Tier)
// ============================================================================
// FREE TIER LIMITS:
// - 100 GB bandwidth/month
// - 2 custom domains
// - Free SSL certificates
// - 0.5 GB storage per app
// ============================================================================

@description('Static Web App name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Backend API base URL')
param apiBaseUrl string

// ============================================================================
// STATIC WEB APP
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled' // Save resources
    allowConfigFileUpdates: true
    buildProperties: {
      appLocation: 'frontend'
      outputLocation: 'dist'
      skipGithubActionWorkflowGeneration: true // We manage our own workflow
    }
  }
}

// ============================================================================
// APP SETTINGS
// ============================================================================

resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    NEXT_PUBLIC_API_URL: apiBaseUrl
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output name string = staticWebApp.name
output defaultHostName string = staticWebApp.properties.defaultHostname
output resourceId string = staticWebApp.id
```

### 2.8 Parameter File

**File: `infrastructure/main.bicepparam`**

```bicep
using './main.bicep'

// Environment
param environment = 'prod'
param location = 'northeurope'

// SQL Server credentials (will be prompted or set via environment variable)
param sqlAdminUsername = readEnvironmentVariable('SQL_ADMIN_USERNAME', 'extratimeadmin')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')

// Secrets (will be prompted or set via environment variable)
param jwtSecret = readEnvironmentVariable('JWT_SECRET', '')
param footballDataApiKey = readEnvironmentVariable('FOOTBALL_DATA_API_KEY', '')

// GitHub (optional - for automated SWA deployment)
param githubRepoUrl = readEnvironmentVariable('GITHUB_REPO_URL', '')
param githubToken = readEnvironmentVariable('GITHUB_TOKEN', '')
```

### 2.9 Deployment Script

**File: `infrastructure/scripts/deploy.ps1`**

```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys ExtraTime infrastructure to Azure using Bicep
.DESCRIPTION
    This script deploys all Azure resources for ExtraTime MVP.
    All resources use FREE TIER with cost safeguards enabled.
.PARAMETER SubscriptionId
    Azure subscription ID to deploy to
.PARAMETER SqlAdminPassword
    Password for SQL Server administrator
.PARAMETER JwtSecret
    JWT signing secret (minimum 32 characters)
.PARAMETER FootballDataApiKey
    API key for football-data.org
.PARAMETER WhatIf
    Preview changes without deploying
.EXAMPLE
    ./deploy.ps1 -SqlAdminPassword "YourPassword123!" -JwtSecret "your-32-char-secret-key-here!!" -FootballDataApiKey "abc123"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $true)]
    [SecureString]$SqlAdminPassword,

    [Parameter(Mandatory = $true)]
    [SecureString]$JwtSecret,

    [Parameter(Mandatory = $true)]
    [SecureString]$FootballDataApiKey,

    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Configuration
$Location = "northeurope"
$Environment = "prod"
$ResourceGroupName = "extratime-$Environment-rg"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ExtraTime Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host ""

# Verify Azure CLI is installed and logged in
Write-Host "Checking Azure CLI..." -ForegroundColor Gray
$azVersion = az version 2>$null | ConvertFrom-Json
if (-not $azVersion) {
    throw "Azure CLI is not installed. Install with: winget install Microsoft.AzureCLI"
}
Write-Host "Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green

# Verify logged in
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}
Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "Subscription: $($account.name)" -ForegroundColor Green

# Set subscription if specified
if ($SubscriptionId) {
    Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
}

# Verify Bicep is installed
Write-Host "Checking Bicep..." -ForegroundColor Gray
$bicepVersion = az bicep version 2>$null
if (-not $bicepVersion) {
    Write-Host "Installing Bicep..." -ForegroundColor Yellow
    az bicep install
}
Write-Host "Bicep installed" -ForegroundColor Green

# Convert SecureStrings to plain text for Azure CLI
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$SqlAdminPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($JwtSecret)
$JwtSecretPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($FootballDataApiKey)
$FootballDataApiKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Validate JWT secret length
if ($JwtSecretPlain.Length -lt 32) {
    throw "JWT secret must be at least 32 characters long"
}

# Build deployment command
$deploymentName = "extratime-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$templateFile = Join-Path $PSScriptRoot ".." "main.bicep"

Write-Host ""
Write-Host "Starting deployment: $deploymentName" -ForegroundColor Cyan

$deployArgs = @(
    "deployment", "sub", "create",
    "--name", $deploymentName,
    "--location", $Location,
    "--template-file", $templateFile,
    "--parameters", "sqlAdminUsername=extratimeadmin",
    "--parameters", "sqlAdminPassword=$SqlAdminPasswordPlain",
    "--parameters", "jwtSecret=$JwtSecretPlain",
    "--parameters", "footballDataApiKey=$FootballDataApiKeyPlain"
)

if ($WhatIf) {
    $deployArgs += "--what-if"
    Write-Host "Running in What-If mode (no changes will be made)" -ForegroundColor Yellow
}

# Execute deployment
az @deployArgs

if ($LASTEXITCODE -ne 0) {
    throw "Deployment failed with exit code $LASTEXITCODE"
}

# Clear sensitive data from memory
$SqlAdminPasswordPlain = $null
$JwtSecretPlain = $null
$FootballDataApiKeyPlain = $null
[System.GC]::Collect()

if (-not $WhatIf) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    # Get outputs
    Write-Host ""
    Write-Host "Fetching deployment outputs..." -ForegroundColor Gray
    $outputs = az deployment sub show --name $deploymentName --query properties.outputs | ConvertFrom-Json

    Write-Host ""
    Write-Host "Resource URLs:" -ForegroundColor Cyan
    Write-Host "  API: $($outputs.appServiceDefaultHostName.value)" -ForegroundColor White
    Write-Host "  Frontend: https://$($outputs.staticWebAppDefaultHostName.value)" -ForegroundColor White
    Write-Host "  Key Vault: $($outputs.keyVaultUri.value)" -ForegroundColor White

    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Grant Key Vault access to App Service managed identity"
    Write-Host "  2. Run database migrations"
    Write-Host "  3. Deploy application code"
    Write-Host "  4. Configure custom domain"
}
```

### 2.10 Grant Key Vault Access Script

**File: `infrastructure/scripts/grant-keyvault-access.ps1`**

```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Grants App Service managed identity access to Key Vault
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName
)

$ErrorActionPreference = "Stop"

Write-Host "Granting Key Vault access..." -ForegroundColor Cyan

# Get the Web App's managed identity principal ID
$principalId = az webapp identity show `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --query principalId `
    --output tsv

if (-not $principalId) {
    throw "Could not get managed identity for Web App: $WebAppName"
}

Write-Host "Web App Principal ID: $principalId" -ForegroundColor Gray

# Get Key Vault resource ID
$keyVaultId = az keyvault show `
    --name $KeyVaultName `
    --resource-group $ResourceGroupName `
    --query id `
    --output tsv

# Assign Key Vault Secrets User role
Write-Host "Assigning 'Key Vault Secrets User' role..." -ForegroundColor Gray
az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee-object-id $principalId `
    --assignee-principal-type ServicePrincipal `
    --scope $keyVaultId

Write-Host "Key Vault access granted successfully!" -ForegroundColor Green
```

### 2.11 Teardown Script

**File: `infrastructure/scripts/teardown.ps1`**

```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Removes all ExtraTime Azure resources
.DESCRIPTION
    Deletes the resource group and all contained resources.
    This action is IRREVERSIBLE.
#>

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ResourceGroupName = "extratime-prod-rg"

Write-Host "========================================" -ForegroundColor Red
Write-Host "WARNING: Resource Deletion" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "This will delete resource group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "This action is IRREVERSIBLE!" -ForegroundColor Red
Write-Host ""

if (-not $Force) {
    $confirmation = Read-Host "Type 'DELETE' to confirm"
    if ($confirmation -ne 'DELETE') {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "Deleting resource group..." -ForegroundColor Yellow

az group delete --name $ResourceGroupName --yes --no-wait

Write-Host "Deletion initiated. Resources will be removed in the background." -ForegroundColor Green
Write-Host "Use 'az group show -n $ResourceGroupName' to check status." -ForegroundColor Gray
```

---

## 3. Deploy Infrastructure

### 3.1 Pre-Deployment Checklist

- [ ] Azure CLI installed and logged in
- [ ] Bicep CLI installed
- [ ] SQL admin password prepared (complex, 8+ chars)
- [ ] JWT secret prepared (32+ chars)
- [ ] Football Data API key obtained from https://www.football-data.org/

### 3.2 Deploy

```powershell
cd infrastructure/scripts

# Preview changes (recommended first)
./deploy.ps1 -WhatIf `
    -SqlAdminPassword (Read-Host -AsSecureString "SQL Password") `
    -JwtSecret (Read-Host -AsSecureString "JWT Secret") `
    -FootballDataApiKey (Read-Host -AsSecureString "Football API Key")

# Deploy for real
./deploy.ps1 `
    -SqlAdminPassword (Read-Host -AsSecureString "SQL Password") `
    -JwtSecret (Read-Host -AsSecureString "JWT Secret") `
    -FootballDataApiKey (Read-Host -AsSecureString "Football API Key")
```

### 3.3 Post-Deployment: Grant Key Vault Access

```powershell
# Get resource names from deployment outputs
$outputs = az deployment sub show --name "extratime-deployment-*" --query properties.outputs | ConvertFrom-Json

./grant-keyvault-access.ps1 `
    -ResourceGroupName "extratime-prod-rg" `
    -WebAppName $outputs.appServiceName.value `
    -KeyVaultName $outputs.keyVaultName.value
```

---

## 4. Database Setup

### 4.1 Run Migrations

```powershell
# Get SQL Server connection details
$sqlFqdn = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.sqlServerFqdn.value" -o tsv

# Set connection string for migrations
$env:ConnectionStrings__DefaultConnection = "Server=tcp:$sqlFqdn,1433;Initial Catalog=extratime-db;User ID=extratimeadmin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

# Run migrations
cd src/ExtraTime.Infrastructure
dotnet ef database update --startup-project ../ExtraTime.API --configuration Release
```

### 4.2 Verify Database

```powershell
# Install SQL Server extension
az extension add --name sql

# List tables (verify migrations applied)
az sql query --server extratime-sql-UNIQUEID --database extratime-db `
    --query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

---

## 5. Secrets Management

### 5.1 Backend Configuration

Update `src/ExtraTime.API/appsettings.Production.json`:

```json
{
  "KeyVault": {
    "Name": "extratime-kv-UNIQUEID"
  },
  "Jwt": {
    "Issuer": "https://extratime-api-UNIQUEID.azurewebsites.net",
    "Audience": "https://extratime.gonek.net",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "FootballData": {
    "BaseUrl": "https://api.football-data.org/v4",
    "RateLimitPerMinute": 10
  },
  "Cors": {
    "AllowedOrigins": [
      "https://extratime.gonek.net",
      "https://extratime-web-prod.azurestaticapps.net"
    ]
  }
}
```

### 5.2 Add Key Vault Integration to Program.cs

```csharp
// In Program.cs, add Azure Key Vault configuration
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}
```

### 5.3 Frontend Environment

Create `frontend/.env.production`:

```env
NEXT_PUBLIC_API_URL=https://extratime-api-UNIQUEID.azurewebsites.net/api
NEXT_PUBLIC_APP_URL=https://extratime.gonek.net
```

---

## 6. Backend Deployment

### 6.1 Build & Publish

```powershell
cd src/ExtraTime.API

# Build in Release mode
dotnet publish -c Release -o ./publish

# Verify output
Get-ChildItem ./publish
```

### 6.2 Deploy to Azure

```powershell
# Get Web App name
$webAppName = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.appServiceName.value" -o tsv

# Deploy via ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

az webapp deployment source config-zip `
    --resource-group extratime-prod-rg `
    --name $webAppName `
    --src ./deploy.zip
```

### 6.3 Verify Deployment

```powershell
# Check status
az webapp show --name $webAppName --resource-group extratime-prod-rg --query state

# Test health endpoint
$apiUrl = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.appServiceDefaultHostName.value" -o tsv

Invoke-RestMethod -Uri "$apiUrl/api/health"
```

---

## 7. Frontend Deployment

### 7.1 Build Production Bundle

```powershell
cd frontend

# Install dependencies
npm ci

# Build for production
npm run build

# Verify build
Test-Path ./dist
```

### 7.2 Deploy to Static Web Apps

```powershell
# Get SWA deployment token
$swaName = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.staticWebAppName.value" -o tsv

$deploymentToken = az staticwebapp secrets list `
    --name $swaName `
    --resource-group extratime-prod-rg `
    --query properties.apiKey -o tsv

# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# Deploy
swa deploy ./dist --deployment-token $deploymentToken --env production
```

### 7.3 Configure CORS on Backend

```powershell
$webAppName = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.appServiceName.value" -o tsv

$swaHostname = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.staticWebAppDefaultHostName.value" -o tsv

az webapp cors add `
    --name $webAppName `
    --resource-group extratime-prod-rg `
    --allowed-origins "https://$swaHostname" "https://extratime.gonek.net"
```

---

## 8. Hangfire Background Jobs

### 8.1 Install Hangfire Packages

```powershell
cd src/ExtraTime.API
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
```

### 8.2 Configure Hangfire

**In `Program.cs`:**

```csharp
// Add Hangfire services
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

// Add Hangfire server (processes jobs)
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Minimize resource usage on free tier
});

// ...

// After app.Build()
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Register recurring jobs
RecurringJob.AddOrUpdate<IFootballDataSyncService>(
    "sync-matches",
    x => x.SyncMatchesAsync(CancellationToken.None),
    Cron.Hourly); // Sync matches every hour

RecurringJob.AddOrUpdate<IBetResultCalculator>(
    "calculate-bet-results",
    x => x.CalculateResultsAsync(CancellationToken.None),
    "*/15 * * * *"); // Every 15 minutes

RecurringJob.AddOrUpdate<IBotBettingService>(
    "bot-betting",
    x => x.PlaceBotBetsAsync(CancellationToken.None),
    Cron.Daily); // Daily bot bets
```

### 8.3 Hangfire Authorization Filter

```csharp
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In production, check for admin role
        if (httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsProduction())
        {
            return httpContext.User.IsInRole("Admin");
        }

        // Allow all in development
        return true;
    }
}
```

---

## 9. Custom Domain Setup

### 9.1 DNS Configuration

Add the following DNS records at your domain registrar (for gonek.net):

**For Static Web App (Frontend):**
```
Type: CNAME
Name: extratime
Value: extratime-web-prod.azurestaticapps.net
TTL: 3600
```

**For TXT verification:**
```
Type: TXT
Name: _dnsauth.extratime
Value: [Get from Azure Portal > Static Web App > Custom domains]
TTL: 3600
```

### 9.2 Add Custom Domain to Static Web App

```powershell
$swaName = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.staticWebAppName.value" -o tsv

# Add custom domain
az staticwebapp hostname set `
    --name $swaName `
    --resource-group extratime-prod-rg `
    --hostname extratime.gonek.net

# Verify (may take a few minutes for SSL certificate)
az staticwebapp hostname show `
    --name $swaName `
    --resource-group extratime-prod-rg `
    --hostname extratime.gonek.net
```

### 9.3 Update CORS for Custom Domain

```powershell
$webAppName = az deployment sub show --name "extratime-deployment-*" `
    --query "properties.outputs.appServiceName.value" -o tsv

az webapp cors add `
    --name $webAppName `
    --resource-group extratime-prod-rg `
    --allowed-origins "https://extratime.gonek.net"
```

---

## 10. CI/CD Pipeline

### 10.1 GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: Build and Deploy

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
        default: 'false'
        type: boolean
      deploy_frontend:
        description: 'Deploy Frontend'
        required: true
        default: 'false'
        type: boolean

env:
  DOTNET_VERSION: '9.0.x'
  NODE_VERSION: '20'
  AZURE_WEBAPP_NAME: ${{ secrets.AZURE_WEBAPP_NAME }}

jobs:
  # ============================================
  # BUILD JOBS (Run on every push)
  # ============================================

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
        run: dotnet test src/ExtraTime.sln --configuration Release --no-build --verbosity normal

      - name: Publish
        run: dotnet publish src/ExtraTime.API/ExtraTime.API.csproj -c Release -o ./publish/backend

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: backend-artifact
          path: ./publish/backend
          retention-days: 7

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
        env:
          NEXT_PUBLIC_API_URL: ${{ secrets.API_URL }}
          NEXT_PUBLIC_APP_URL: https://extratime.gonek.net
        run: npm run build

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: frontend-artifact
          path: frontend/dist
          retention-days: 7

  # ============================================
  # DEPLOY JOBS (Manual trigger only)
  # ============================================

  deploy-backend:
    runs-on: ubuntu-latest
    needs: build-backend
    if: github.event.inputs.deploy_backend == 'true'
    environment:
      name: production-backend
      url: ${{ secrets.API_URL }}

    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: backend-artifact
          path: ./publish

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: build-frontend
    if: github.event.inputs.deploy_frontend == 'true'
    environment:
      name: production-frontend
      url: https://extratime.gonek.net

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
          action: 'upload'
          app_location: './dist'
          skip_app_build: true
```

### 10.2 GitHub Secrets Configuration

Add these secrets in GitHub repository settings (Settings > Secrets > Actions):

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_WEBAPP_NAME` | App Service name | Deployment output |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | App Service publish profile | Azure Portal > App Service > Get publish profile |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deployment token | `az staticwebapp secrets list --name <swa-name>` |
| `API_URL` | Backend API URL | `https://extratime-api-xxx.azurewebsites.net/api` |

### 10.3 Environment Protection Rules

Configure in GitHub Settings > Environments:

1. Create environment: `production-backend`
   - Required reviewers: Add yourself
   - Deployment branches: `main` only

2. Create environment: `production-frontend`
   - Required reviewers: Add yourself
   - Deployment branches: `main` only

---

## 11. Monitoring & Logging

### 11.1 Application Insights Setup

Application Insights is already configured with a daily cap of 0.16 GB (~5 GB/month free tier limit).

Add to `Program.cs`:

```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsightsConnectionString"];
});

// Configure sampling to reduce data volume
builder.Services.Configure<TelemetryConfiguration>(config =>
{
    var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    builder.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5);
    builder.Build();
});
```

### 11.2 Health Check Endpoint

Verify health checks exist in `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database",
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck("api", () => HealthCheckResult.Healthy());

// Map endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/api/health");
```

### 11.3 View Logs

```powershell
# Stream live logs
az webapp log tail --name $webAppName --resource-group extratime-prod-rg

# Download logs
az webapp log download --name $webAppName --resource-group extratime-prod-rg --log-file logs.zip
```

---

## 12. Post-Deployment Testing

### 12.1 Smoke Tests

```powershell
$apiUrl = "https://extratime-api-xxx.azurewebsites.net"
$frontendUrl = "https://extratime.gonek.net"

# Test API health
$health = Invoke-RestMethod -Uri "$apiUrl/api/health"
Write-Host "API Health: $($health.status)"

# Test competitions endpoint
$competitions = Invoke-RestMethod -Uri "$apiUrl/api/competitions"
Write-Host "Competitions: $($competitions.Count) found"

# Test frontend loads
$response = Invoke-WebRequest -Uri $frontendUrl -UseBasicParsing
Write-Host "Frontend Status: $($response.StatusCode)"
```

### 12.2 Critical Flow Checklist

- [ ] User registration
- [ ] User login
- [ ] Create league
- [ ] Join league via invite code
- [ ] Place bet on upcoming match
- [ ] View leaderboard
- [ ] View match list
- [ ] Hangfire dashboard accessible at `/hangfire` (admin only)

### 12.3 Mobile Testing

- [ ] Test on iPhone Safari
- [ ] Test on Android Chrome
- [ ] Verify touch targets
- [ ] Check responsive breakpoints

---

## 13. Rollback Plan

### 13.1 Backend Rollback

```powershell
# View deployment history
az webapp deployment list --name $webAppName --resource-group extratime-prod-rg

# Redeploy previous version from GitHub Actions
# Go to Actions > Select previous successful run > Re-run jobs
```

### 13.2 Frontend Rollback

```powershell
# Static Web Apps keeps deployment history
# Redeploy from previous GitHub Actions artifact
```

### 13.3 Database Rollback

```powershell
# BEFORE any deployment, create a backup
az sql db export `
    --name extratime-db `
    --server extratime-sql-xxx `
    --resource-group extratime-prod-rg `
    --admin-user extratimeadmin `
    --admin-password "YOUR_PASSWORD" `
    --storage-key-type SharedAccessKey `
    --storage-key "YOUR_STORAGE_SAS" `
    --storage-uri "https://yourstorage.blob.core.windows.net/backups/backup.bacpac"
```

### 13.4 Full Infrastructure Rollback

```powershell
# Delete everything and redeploy
./infrastructure/scripts/teardown.ps1 -Force
./infrastructure/scripts/deploy.ps1 ...
```

---

## 14. Zero-Cost Verification

### 14.1 Free Tier Limits Summary

| Service | Free Tier Limit | Safeguard |
|---------|-----------------|-----------|
| **Azure SQL** | 100,000 vCore seconds/month, 32 GB | `freeLimitExhaustionBehavior: AutoPause` |
| **App Service F1** | 60 CPU min/day, 1 GB RAM | Always free (hard limit) |
| **Static Web Apps** | 100 GB bandwidth/month | Free tier SKU |
| **Application Insights** | 5 GB/month | Daily cap: 0.16 GB |
| **Key Vault** | 10,000 operations/month | Standard tier (operations beyond limit are cheap) |

### 14.2 Cost Monitoring

```powershell
# View current costs
az consumption usage list --query "[?contains(instanceId, 'extratime')]" --output table

# Set budget alert at $0
az consumption budget create `
    --resource-group extratime-prod-rg `
    --budget-name "zero-cost-alert" `
    --amount 0.01 `
    --time-grain Monthly `
    --category Cost `
    --start-date (Get-Date -Format "yyyy-MM-01") `
    --end-date (Get-Date).AddYears(1).ToString("yyyy-MM-01")
```

### 14.3 Monthly Cost Verification Checklist

- [ ] Check Azure Cost Management dashboard
- [ ] Verify SQL database is auto-pausing when idle
- [ ] Check Application Insights data ingestion under 5 GB
- [ ] Verify no surprise charges

---

## 15. Security Checklist

- [ ] SQL Server firewall only allows Azure services
- [ ] Key Vault uses RBAC (not access policies)
- [ ] Managed identity used for Key Vault access (no secrets in code)
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured for specific origins only
- [ ] JWT secrets stored in Key Vault
- [ ] SQL connections use encryption
- [ ] Application Insights doesn't log sensitive data
- [ ] Hangfire dashboard requires admin authentication
- [ ] No secrets in Git repository

---

## 16. Post-Launch Tasks

- [ ] Monitor error rates for 24 hours
- [ ] Check Application Insights for performance issues
- [ ] Verify Hangfire jobs are running (check `/hangfire`)
- [ ] Test football data sync is working
- [ ] Create admin user account
- [ ] Seed initial competitions data
- [ ] Configure DNS TTL to lower value during initial testing
- [ ] Collect feedback for Phase 9 planning

---

## Quick Reference Commands

```powershell
# Deploy infrastructure
./infrastructure/scripts/deploy.ps1 -SqlAdminPassword ... -JwtSecret ... -FootballDataApiKey ...

# View deployment outputs
az deployment sub show --name "extratime-deployment-*" --query properties.outputs

# Stream API logs
az webapp log tail --name extratime-api-xxx --resource-group extratime-prod-rg

# Check SQL database status
az sql db show --name extratime-db --server extratime-sql-xxx --resource-group extratime-prod-rg --query status

# View Hangfire dashboard
# Navigate to: https://extratime-api-xxx.azurewebsites.net/hangfire

# Teardown everything
./infrastructure/scripts/teardown.ps1
```
