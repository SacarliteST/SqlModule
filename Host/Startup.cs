using SQLModule.Web;

namespace SQLModule.Host;

internal static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder) =>
        builder.Services.AddWeb(builder.Configuration);

    public static ILogger CreateLogger()
    {
        using var factory = LoggerFactory.Create(options => options
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace));

        return factory.CreateLogger<Program>();
    }

    public static void ConfigureApp(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseWeb();
    }
}
