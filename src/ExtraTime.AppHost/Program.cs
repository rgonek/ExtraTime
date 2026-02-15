using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Resource groups for dashboard organization
var infrastructure = builder.AddResource(new ParameterResource("infrastructure", _ => "Infrastructure"));
var services = builder.AddResource(new ParameterResource("services", _ => "Services"));
var devTriggers = builder.AddResource(new ParameterResource("dev-triggers", _ => "Dev Triggers"));

// External API Keys/Secrets
var footballDataKey = builder.AddParameter("FootballDataApiKey", secret: true);

// SQL Server database
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("extratime-sql")
    .WithContainerRuntimeArgs("--label", "com.docker.compose.project=extratime")
    .WithParentRelationship(infrastructure);

var database = sqlServer.AddDatabase("extratime");

// Azure Storage emulator for Functions
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(config => config
        .WithContainerName("extratime-storage")
        .WithContainerRuntimeArgs("--label", "com.docker.compose.project=extratime"))
    .WithParentRelationship(infrastructure);

// Migration service
var migrations = builder.AddProject<Projects.ExtraTime_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database)
    .WithParentRelationship(services);

// API project
var api = builder.AddProject<Projects.ExtraTime_API>("api")
    .WithReference(database)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithParentRelationship(services);

// Azure Functions
var functions = builder.AddAzureFunctionsProject<Projects.ExtraTime_Functions>("functions")
    .WithHostStorage(storage)
    .WithReference(database)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WaitForCompletion(migrations)
    .WithExplicitStart()
    .WithParentRelationship(services);

// Dev trigger resources - each has its own isolated log stream
// These run operations in separate processes with full application logging
// Restart a resource in the dashboard to trigger its operation again
// Manual start mode prevents them from running automatically on dashboard start

// Full sync - use this to initialize a fresh database
var syncAll = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-all")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-all")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var syncCompetitions = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-competitions")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-competitions")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var syncTeams = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-teams")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-teams")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var syncStandings = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-standings")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-standings")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var syncMatches = builder.AddProject<Projects.ExtraTime_DevTriggers>("sync-matches")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithEnvironment("FootballData__ApiKey", footballDataKey)
    .WithArgs("sync-matches")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var calculateBets = builder.AddProject<Projects.ExtraTime_DevTriggers>("calculate-bets")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithArgs("calculate-bets")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

var botBetting = builder.AddProject<Projects.ExtraTime_DevTriggers>("bot-betting")
    .WithReference(database)
    .WithParentRelationship(devTriggers)
    .WithArgs("bot-betting")
    .WaitForCompletion(migrations)
    .WithExplicitStart();

// Next.js frontend
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"))
    .WithParentRelationship(services);

builder.Build().Run();
