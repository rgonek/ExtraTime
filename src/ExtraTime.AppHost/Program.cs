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

// Get the API endpoint URL to pass to the scripts
var apiEndpoint = api.GetEndpoint("http");

// 4. Separate trigger resources with dedicated log streams
// Each resource has its own log stream in the dashboard
// Restart a resource in the dashboard to trigger its operation again
var syncMatches = builder.AddExecutable("sync-matches", "pwsh", "src/ExtraTime.AppHost/scripts", "-File", "trigger-sync-matches.ps1", "-ApiUrl", apiEndpoint)
    .WithReference(api)
    .WaitFor(api);

var calculateBets = builder.AddExecutable("calculate-bets", "pwsh", "src/ExtraTime.AppHost/scripts", "-File", "trigger-calculate-bets.ps1", "-ApiUrl", apiEndpoint)
    .WithReference(api)
    .WaitFor(api);

var botBetting = builder.AddExecutable("bot-betting", "pwsh", "src/ExtraTime.AppHost/scripts", "-File", "trigger-bot-betting.ps1", "-ApiUrl", apiEndpoint)
    .WithReference(api)
    .WaitFor(api);

// Next.js frontend
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"));

builder.Build().Run();
