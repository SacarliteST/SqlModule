using Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TemplateApi.Host.Common;

internal static class ExceptionHandlerExtensions
{
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
