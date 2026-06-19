using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;

namespace SQLModule.Host.Common;

/// <summary>
/// Метод подключения глобального обработчика необработанных исключений.
/// </summary>
internal static class ExceptionHandlerExtensions
{
    /// <summary>
    /// Подключает <c>UseExceptionHandler</c> с маппингом
    /// доменных исключений на HTTP-статусы и записью в лог.
    /// <list type="bullet">
    ///   <item><see cref="NotFoundException"/> → 404</item>
    ///   <item><see cref="ArgumentException"/> → 400</item>
    ///   <item>Всё остальное → 500</item>
    /// </list>
    /// Ответ всегда в формате <see cref="ProblemDetails"/> (<c>application/problem+json</c>).
    /// </summary>
    /// <param name="app">Экземпляр <see cref="WebApplication"/>.</param>
    public static WebApplication UseApiExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                var statusCode = exception switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                var logger = context.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = statusCode,
                    Title = GetTitle(statusCode),
                    Detail = exception?.Message
                });
            });
        });

        return app;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status400BadRequest => "Bad Request",
        _ => "Internal Server Error"
    };
}
