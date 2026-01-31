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
