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
