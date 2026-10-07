using SQLModule.Web;

namespace SQLModule.Host;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        using var bootstrapLoggerFactory = Startup.CreateLoggerFactory();
        var logger = bootstrapLoggerFactory.CreateLogger<Program>();
        var builder = WebApplication.CreateBuilder(args);

        try
        {
            logger.LogInformation("The application has been started");
            Startup.ConfigureServices(builder);

            var app = builder.Build();

            Startup.ConfigureApp(app);

            await app.InitializeWebAsync();

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            logger.LogInformation("Stopping application...");
        }
    }
}
