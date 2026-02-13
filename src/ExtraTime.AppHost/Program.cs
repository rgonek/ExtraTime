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

// 4. Dev trigger resources - each has its own isolated log stream
// These run operations in separate processes with full application logging
// Restart a resource in the dashboard to trigger its operation again
var funcGroup = builder.AddResource(new ParameterResource("functions", _ => "Group"));
var syncMatches = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-matches")
    .WithReference(database)
    .WithParentRelationship(funcGroup)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-matches")
    .WaitForCompletion(migrations);

var syncStandings = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-standings")
    .WithReference(database)
    .WithParentRelationship(funcGroup)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-standings")
    .WaitForCompletion(migrations);

var calculateBets = builder.AddProject<Projects.ExtraTime_DevTriggers>("calculate-bets")
    .WithReference(database)
    .WithParentRelationship(funcGroup)
    .WithArgs("calculate-bets")
    .WaitForCompletion(migrations);

var botBetting = builder.AddProject<Projects.ExtraTime_DevTriggers>("bot-betting")
    .WithReference(database)
    .WithParentRelationship(funcGroup)
    .WithArgs("bot-betting")
    .WaitForCompletion(migrations);

// Next.js frontend
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"));

builder.Build().Run();
