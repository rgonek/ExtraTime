using ExtraTime.Infrastructure.Data;
using ExtraTime.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ApplicationDbContext>("extratime");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
