
using TemplateApi.Data.Core.Migrations;

namespace TemplateApi.Host;
internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var logger = Startup.CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        try
        {
            logger.LogInformation("The application has been started");
            Startup.ConfigureServices(builder);

            var app = builder.Build();

            Startup.ConfigureApp(app);

            using (var scope = app.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IMigrationManager>().MigrateAsync();
            }

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{exception} Host terminated unexpectedly");
            logger.LogError("{Exception} Host terminated unexpectedly", exception);
            throw;
        }
        finally
        {
            Console.WriteLine("Stopping application...");
            logger.LogInformation($"Stopping application...");
        }
    }
}
