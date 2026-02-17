using ExtraTime.Application;
using ExtraTime.Infrastructure;
using ExtraTime.MLTrainer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(logging =>
{
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});
services.AddApplicationServices();
services.AddInfrastructureServices(configuration);
services.AddScoped<ModelTrainer>();

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var trainer = scope.ServiceProvider.GetRequiredService<ModelTrainer>();

var league = args.Length > 0 ? args[0] : "PL";
var result = await trainer.TrainAsync(league);

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
    .CreateLogger("ExtraTime.MLTrainer");
logger.LogInformation(
    "Training completed. Version={Version}, Samples={Samples}, HomeMAE={HomeMae:F3}, AwayMAE={AwayMae:F3}",
    result.Version,
    result.TrainingSamples,
    result.HomeModelMetrics.MeanAbsoluteError,
    result.AwayModelMetrics.MeanAbsoluteError);
