using SQLModule.Web;

namespace SQLModule.Host;

internal static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder) =>
        builder.Services.AddApplicationComposition(builder.Configuration);

    /// <summary>Bootstrap-фабрика логирования; владелец обязан освободить её по завершении работы.</summary>
    public static ILoggerFactory CreateLoggerFactory() =>
        LoggerFactory.Create(options => options
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace));

    public static void ConfigureApp(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseWeb();
    }
}
