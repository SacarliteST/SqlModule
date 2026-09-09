using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;

namespace SQLModule.Web.Common;

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
                    .GetRequiredService<ILogger<IWebMarker>>();

                logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(ApiProblemFactory.Create(
                    statusCode,
                    GetTitle(statusCode),
                    GetSafeDetail(statusCode),
                    GetCode(statusCode)));
            });
        });

        return app;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Ресурс не найден",
        StatusCodes.Status400BadRequest => "Некорректный запрос",
        _ => "Внутренняя ошибка"
    };

    private static string GetCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Unhandled.NotFound",
        StatusCodes.Status400BadRequest => "Unhandled.BadRequest",
        _ => "Unhandled.InternalServerError"
    };

    internal static string GetSafeDetail(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Запрошенный ресурс не найден.",
        StatusCodes.Status400BadRequest => "Запрос содержит некорректные данные.",
        _ => "При обработке запроса произошла внутренняя ошибка."
    };
}
