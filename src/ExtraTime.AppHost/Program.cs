var builder = DistributedApplication.CreateBuilder(args);

// SQL Server database
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("extratime");

// Migration service - runs migrations and seeds data, then exits
var migrations = builder.AddProject<Projects.ExtraTime_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// API project with reference to the database - waits for migrations to complete
var api = builder.AddProject<Projects.ExtraTime_API>("api")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints();

// Azure Functions project for background jobs
var functions = builder.AddProject<Projects.ExtraTime_Functions>("functions")
    .WithReference(database)
    .WaitForCompletion(migrations);

// Next.js frontend (using Bun)
var web = builder.AddExecutable("web", "bun", "../../web", "run", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"));

builder.Build().Run();
