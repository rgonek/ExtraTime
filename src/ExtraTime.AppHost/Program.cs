using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// External API Keys/Secrets
var footballDataKey = builder.AddParameter("FootballDataApiKey", secret: true);

// SQL Server database
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("extratime");

// 1. External Football Data API Resource (Feature 3)
// Visualizing the external dependency in the dashboard
var footballDataApi = builder.AddParameter("football-data-url", "https://api.football-data.org");

// 2. Migration service - Visualization (Feature 4)
var migrations = builder.AddProject<Projects.ExtraTime_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// 3. API project
var api = builder.AddProject<Projects.ExtraTime_API>("api")
    .WithReference(database)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints();

// 4. Azure Functions - Manual Triggers (Feature 1)
// We disable timers using AzureWebJobs.{Name}.Disabled = true
// And add buttons to trigger them manually

builder.AddProject<Projects.ExtraTime_Functions>("func-sync-matches")
    .WithReference(database)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithEnvironment("AzureWebJobs.SyncMatches.Disabled", "true")
    .WithEnvironment("AzureWebJobs.CalculateBetResults.Disabled", "true")
    .WithEnvironment("AzureWebJobs.BotBetting.Disabled", "true")
    .WithCommand("sync-now", "Sync Matches", async _ =>
    {
        // Trigger logic would go here
        return new ExecuteCommandResult { Success = true };
    })
    .WaitForCompletion(migrations);

builder.AddProject<Projects.ExtraTime_Functions>("func-calculate-bets")
    .WithReference(database)
    .WithEnvironment("AzureWebJobs.CalculateBetResults.Disabled", "true")
    .WithEnvironment("AzureWebJobs.SyncMatches.Disabled", "true")
    .WithEnvironment("AzureWebJobs.BotBetting.Disabled", "true")
    .WithCommand("calculate-now", "Calculate Bets", async _ =>
    {
        return new ExecuteCommandResult { Success = true };
    })
    .WaitForCompletion(migrations);

builder.AddProject<Projects.ExtraTime_Functions>("func-bot-betting")
    .WithReference(database)
    .WithEnvironment("AzureWebJobs.BotBetting.Disabled", "true")
    .WithEnvironment("AzureWebJobs.SyncMatches.Disabled", "true")
    .WithEnvironment("AzureWebJobs.CalculateBetResults.Disabled", "true")
    .WithCommand("bots-now", "Run Bots", async _ =>
    {
        return new ExecuteCommandResult { Success = true };
    })
    .WaitForCompletion(migrations);

// Next.js frontend
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"));

builder.Build().Run();
