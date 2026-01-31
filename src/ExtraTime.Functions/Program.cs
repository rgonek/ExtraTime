using ExtraTime.Application;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
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

// Override ICurrentUserService for background context (no HTTP request)
builder.Services.AddSingleton<ICurrentUserService, BackgroundUserService>();

// Build and run
var host = builder.Build();
await host.RunAsync();
