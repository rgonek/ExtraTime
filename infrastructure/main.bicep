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

// Azure Functions (Consumption Plan - Free Tier)
// COST CONTROLS:
// - Consumption plan (Y1) with pay-per-execution
// - Daily memory quota limited to prevent runaway costs
// - Function scale limit set to 1 to prevent unexpected scaling
module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  scope: rg
  params: {
    name: 'extratime-func-${uniqueSuffix}'
    storageAccountName: 'etfn${uniqueSuffix}'
    location: location
    tags: tags
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlConnectionString: sqlServer.outputs.connectionString
    keyVaultName: keyVault.outputs.name
    footballDataApiKey: footballDataApiKey
    jwtSecret: jwtSecret
    // COST CONTROL: Daily quota of 10000 GB-seconds (free tier allows ~13333/day)
    dailyMemoryTimeQuota: 10000
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
output functionsName string = functions.outputs.functionAppName
output functionsHostName string = functions.outputs.functionAppHostName
output functionsPrincipalId string = functions.outputs.principalId
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppDefaultHostName string = staticWebApp.outputs.defaultHostName
output appInsightsName string = appInsights.outputs.name
output appInsightsConnectionString string = appInsights.outputs.connectionString
