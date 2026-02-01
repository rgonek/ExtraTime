# Azure Functions Migration Plan ✅ COMPLETE

## Overview
Migrate background jobs from Hangfire to Azure Functions with Aspire integration. This provides better observability in Aspire dashboard, scales to zero when idle, and uses Azure's free tier (1M executions/month).


---

## Current State (Disabled)

The following Hangfire jobs have been disabled and need to be migrated:

| Job ID | Schedule | Service | Method |
|--------|----------|---------|--------|
| `sync-matches` | Hourly | `IFootballSyncService` | `SyncMatchesAsync(null, null, ct)` |
| `calculate-bet-results` | Every 15 min | Not implemented | Placeholder only |
| `bot-betting` | Daily | `IBotBettingService` | `PlaceBetsForUpcomingMatchesAsync(ct)` |

---

## Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Aspire AppHost                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────┐    ┌──────────┐    ┌──────────────────┐          │
│  │   SQL    │◄───│   API    │    │  Azure Functions │          │
│  │ Server   │    │          │    │   (Isolated)     │          │
│  └────▲─────┘    └──────────┘    └────────┬─────────┘          │
│       │                                    │                    │
│       └────────────────────────────────────┘                    │
│                   Database Access                               │
│                                                                 │
│  Aspire Dashboard: View all services, logs, traces, metrics    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Project Setup

### 1.1 Create Azure Functions Project

**Location:** `src/ExtraTime.Functions/`

```powershell
# Create the project
cd src
dotnet new azurefunctions --worker isolated --target-framework net10.0 --name ExtraTime.Functions

# Or if net10.0 template not available:
dotnet new azurefunctions --worker isolated --target-framework net9.0 --name ExtraTime.Functions
```

### 1.2 Project Structure

```
src/ExtraTime.Functions/
├── ExtraTime.Functions.csproj
├── Program.cs                      # Host configuration with Aspire
├── Functions/
│   ├── SyncMatchesFunction.cs      # Timer: Hourly match sync
│   ├── CalculateBetResultsFunction.cs  # Timer: Every 15 min
│   └── BotBettingFunction.cs       # Timer: Daily bot betting
├── host.json                       # Function host configuration
└── local.settings.json             # Local development settings
```

### 1.3 Project File (ExtraTime.Functions.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Timer" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <!-- Aspire integration -->
    <PackageReference Include="Aspire.Microsoft.EntityFrameworkCore.SqlServer" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference shared projects for services -->
    <ProjectReference Include="..\ExtraTime.Application\ExtraTime.Application.csproj" />
    <ProjectReference Include="..\ExtraTime.Infrastructure\ExtraTime.Infrastructure.csproj" />
    <ProjectReference Include="..\ExtraTime.ServiceDefaults\ExtraTime.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

### 1.4 Add to Solution

```powershell
dotnet sln src/ExtraTime.sln add src/ExtraTime.Functions/ExtraTime.Functions.csproj
```

---

## Part 2: Host Configuration

### 2.1 Program.cs

```csharp
using ExtraTime.Application;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Aspire SQL Server DbContext integration
builder.AddSqlServerDbContext<ApplicationDbContext>("extratime");

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure Functions worker
builder.ConfigureFunctionsWebApplication();

// Build and run
var host = builder.Build();
await host.RunAsync();
```

### 2.2 host.json

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    },
    "logLevel": {
      "default": "Information",
      "Host.Results": "Error",
      "Function": "Information",
      "Host.Aggregator": "Trace"
    }
  },
  "extensions": {
    "timers": {
      "schedule": {
        "maxConcurrentCalls": 1
      }
    }
  },
  "functionTimeout": "00:05:00"
}
```

### 2.3 local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "ConnectionStrings": {
    "extratime": "Server=localhost,1433;Database=extratime;User Id=sa;Password=ExtraTime_Dev123!;TrustServerCertificate=True"
  }
}
```

---

## Part 3: Timer Functions

### 3.1 SyncMatchesFunction.cs

```csharp
using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class SyncMatchesFunction(
    IFootballSyncService footballSyncService,
    ILogger<SyncMatchesFunction> logger)
{
    /// <summary>
    /// Syncs football matches from external API every hour.
    /// CRON: 0 0 * * * * (at minute 0 of every hour)
    /// </summary>
    [Function("SyncMatches")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SyncMatches function started at: {Time}", DateTime.UtcNow);

        try
        {
            var result = await footballSyncService.SyncMatchesAsync(
                competitionIds: null,  // Sync all configured competitions
                dateRange: null,       // Use default date range
                cancellationToken);

            logger.LogInformation(
                "SyncMatches completed: {MatchesCreated} created, {MatchesUpdated} updated",
                result.MatchesCreated,
                result.MatchesUpdated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncMatches function failed");
            throw; // Re-throw to mark function execution as failed
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next SyncMatches scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
```

### 3.2 CalculateBetResultsFunction.cs

```csharp
using ExtraTime.Application.Common.Interfaces;
using Mediator;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class CalculateBetResultsFunction(
    IMediator mediator,
    ILogger<CalculateBetResultsFunction> logger)
{
    /// <summary>
    /// Calculates bet results for recently finished matches every 15 minutes.
    /// CRON: 0 */15 * * * * (every 15 minutes)
    /// </summary>
    [Function("CalculateBetResults")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("CalculateBetResults function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Send command via mediator to calculate results
            // var command = new CalculateBetResultsCommand();
            // var result = await mediator.Send(command, cancellationToken);

            // TODO: Implement CalculateBetResultsCommand in Application layer
            // For now, log placeholder
            logger.LogInformation("CalculateBetResults: Checking for finished matches...");

            // Query matches that:
            // 1. Have status = Finished
            // 2. Have final scores
            // 3. Don't have calculated bet results yet
            // Then calculate points for each bet on those matches
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CalculateBetResults function failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next CalculateBetResults scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
```

### 3.3 BotBettingFunction.cs

```csharp
using ExtraTime.Application.Features.Bots.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class BotBettingFunction(
    IBotBettingService botBettingService,
    ILogger<BotBettingFunction> logger)
{
    /// <summary>
    /// Places bets for all bots on upcoming matches daily at 6:00 AM UTC.
    /// CRON: 0 0 6 * * * (6:00 AM every day)
    /// </summary>
    [Function("BotBetting")]
    public async Task Run(
        [TimerTrigger("0 0 6 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("BotBetting function started at: {Time}", DateTime.UtcNow);

        try
        {
            var betsPlaced = await botBettingService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);

            logger.LogInformation("BotBetting completed: {BetsPlaced} bets placed", betsPlaced);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BotBetting function failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next BotBetting scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
```

---

## Part 4: Aspire Integration

### 4.1 Update AppHost (Program.cs)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// SQL Server database
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("extratime");

// Migration service - runs migrations and seeds data, then exits
var migrations = builder.AddProject<Projects.ExtraTime_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// API project
var api = builder.AddProject<Projects.ExtraTime_API>("api")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints();

// Azure Functions project for background jobs
var functions = builder.AddProject<Projects.ExtraTime_Functions>("functions")
    .WithReference(database)
    .WaitForCompletion(migrations);

// Next.js frontend
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"));

builder.Build().Run();
```

### 4.2 Alternative: Use Azurite for Local Storage

For local development, Azure Functions needs a storage account. Use Azurite emulator:

```csharp
// In AppHost Program.cs
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var functions = builder.AddProject<Projects.ExtraTime_Functions>("functions")
    .WithReference(database)
    .WithReference(storage)
    .WaitForCompletion(migrations);
```

---

## Part 5: DI Adjustments

### 5.1 Fix ICurrentUserService for Background Context

Background jobs don't have an HTTP context, so `ICurrentUserService` needs a fallback:

**Create:** `src/ExtraTime.Infrastructure/Services/BackgroundUserService.cs`

```csharp
using ExtraTime.Application.Common.Interfaces;

namespace ExtraTime.Infrastructure.Services;

/// <summary>
/// User service for background job contexts where there's no HTTP request.
/// Used by Azure Functions and other background processes.
/// </summary>
public sealed class BackgroundUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Email => null;
    public string? Role => "System";
    public bool IsAuthenticated => false;
    public bool IsAdmin => true; // System operations have admin privileges
}
```

### 5.2 Register in Functions Program.cs

```csharp
// Override ICurrentUserService for background context
builder.Services.AddSingleton<ICurrentUserService, BackgroundUserService>();
```

---

## Part 6: Azure Deployment

### 6.1 Bicep Module for Azure Functions

**File:** `infrastructure/modules/functions.bicep`

```bicep
// Azure Functions (Consumption Plan - Free Tier)
// FREE TIER: 1M executions/month, 400,000 GB-seconds

@description('Function App name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Storage account name for Functions')
param storageAccountName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('SQL connection string')
@secure()
param sqlConnectionString string

// Storage Account (required for Functions)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Consumption Plan (serverless, free tier)
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Linux
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|9.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__extratime'
          value: sqlConnectionString
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
```

### 6.2 Update main.bicep

Add to existing `infrastructure/main.bicep`:

```bicep
// Azure Functions for background jobs
module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  scope: rg
  params: {
    name: 'extratime-func-${uniqueSuffix}'
    storageAccountName: 'extratimefn${uniqueSuffix}'
    location: location
    tags: tags
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlConnectionString: sqlServer.outputs.connectionString
  }
}

// Output
output functionsName string = functions.outputs.functionAppName
output functionsHostName string = functions.outputs.functionAppHostName
```

---

## Part 7: Testing

### 7.1 Local Testing with Aspire

```powershell
# Start Aspire AppHost (includes Functions)
cd src/ExtraTime.AppHost
dotnet run

# Functions will appear in Aspire Dashboard at https://localhost:15xxx
# - View function executions in traces
# - View logs in real-time
# - Monitor health status
```

### 7.2 Manual Function Trigger (Development)

Azure Functions CLI allows manual triggers:

```powershell
# Install Azure Functions Core Tools
winget install Microsoft.Azure.FunctionsCoreTools

# Run functions locally (outside Aspire for testing)
cd src/ExtraTime.Functions
func start

# Trigger a specific function manually
func invoke SyncMatches --non-interactive
```

### 7.3 Unit Testing Functions

```csharp
[Fact]
public async Task SyncMatchesFunction_ShouldCallSyncService()
{
    // Arrange
    var mockSyncService = Substitute.For<IFootballSyncService>();
    mockSyncService.SyncMatchesAsync(null, null, Arg.Any<CancellationToken>())
        .Returns(new SyncResult { MatchesCreated = 5, MatchesUpdated = 3 });

    var mockLogger = Substitute.For<ILogger<SyncMatchesFunction>>();
    var function = new SyncMatchesFunction(mockSyncService, mockLogger);

    var timerInfo = new TimerInfo(null, new ScheduleStatus());

    // Act
    await function.Run(timerInfo, CancellationToken.None);

    // Assert
    await mockSyncService.Received(1).SyncMatchesAsync(null, null, Arg.Any<CancellationToken>());
}
```

---

## Part 8: Cleanup

### 8.1 Files to Delete

After migration is complete and tested:

```
src/ExtraTime.API/Features/BackgroundJobs/           # Already deleted
src/ExtraTime.Infrastructure/Services/Bots/BotBettingBackgroundService.cs
src/ExtraTime.Infrastructure/Services/Bots/FormCacheBackgroundService.cs
```

### 8.2 Remove Hangfire from Directory.Packages.props

```xml
<!-- Remove these lines -->
<PackageVersion Include="Hangfire.AspNetCore" Version="..." />
<PackageVersion Include="Hangfire.SqlServer" Version="..." />
```

### 8.3 Database Cleanup (Optional)

Hangfire creates tables in the database. Remove if no longer needed:

```sql
-- Remove Hangfire schema (run in SQL Server)
DROP TABLE IF EXISTS [HangFire].[AggregatedCounter];
DROP TABLE IF EXISTS [HangFire].[Counter];
DROP TABLE IF EXISTS [HangFire].[Hash];
DROP TABLE IF EXISTS [HangFire].[Job];
DROP TABLE IF EXISTS [HangFire].[JobParameter];
DROP TABLE IF EXISTS [HangFire].[JobQueue];
DROP TABLE IF EXISTS [HangFire].[List];
DROP TABLE IF EXISTS [HangFire].[Schema];
DROP TABLE IF EXISTS [HangFire].[Server];
DROP TABLE IF EXISTS [HangFire].[Set];
DROP TABLE IF EXISTS [HangFire].[State];
DROP SCHEMA IF EXISTS [HangFire];
```

---

## Implementation Checklist

### Phase 1: Project Setup (1-2 hours)
- [ ] Create ExtraTime.Functions project
- [ ] Add to solution
- [ ] Configure host.json and local.settings.json
- [ ] Add project references

### Phase 2: Function Implementation (2-3 hours)
- [ ] Implement SyncMatchesFunction
- [ ] Implement BotBettingFunction
- [ ] Implement CalculateBetResultsFunction (or create command)
- [ ] Create BackgroundUserService for DI

### Phase 3: Aspire Integration (1 hour)
- [ ] Update AppHost to include Functions project
- [ ] Configure Azurite for local storage
- [ ] Test in Aspire dashboard

### Phase 4: Testing (1-2 hours)
- [ ] Local testing with Aspire
- [ ] Manual function triggers
- [ ] Unit tests for functions

### Phase 5: Azure Deployment (1-2 hours)
- [ ] Create Bicep module for Functions
- [ ] Update main.bicep
- [ ] Deploy and test in Azure

### Phase 6: Cleanup (30 min)
- [ ] Remove old background service files
- [ ] Remove Hangfire packages from Directory.Packages.props
- [ ] Clean up Hangfire database tables (optional)

---

## Cost Analysis

| Resource | Free Tier Limit | Expected Usage |
|----------|-----------------|----------------|
| Function Executions | 1,000,000/month | ~3,000/month (3 functions * 30 days * ~30 runs) |
| Compute (GB-s) | 400,000/month | ~500/month |
| Storage Account | N/A | ~$0.01/month |

**Total Monthly Cost: ~$0.01** (storage only)

---

## Rollback Plan

If Azure Functions doesn't work as expected:

1. Re-enable Hangfire in API project
2. Revert Program.cs changes
3. Add Hangfire packages back to csproj
4. Remove Functions project from AppHost

The API project changes are minimal and easily reversible.
