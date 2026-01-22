using ExtraTime.Application;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using ExtraTime.API.Features.Health;

var builder = WebApplication.CreateBuilder(args);

// Clean Architecture services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapHealthEndpoints();
app.MapHealthChecks("/health");

app.Run();
