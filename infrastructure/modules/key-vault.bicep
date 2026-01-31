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
