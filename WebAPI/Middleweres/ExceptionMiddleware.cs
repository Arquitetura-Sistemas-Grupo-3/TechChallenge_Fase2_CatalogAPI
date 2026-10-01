using System.Net;
using Infra.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (ExceptionBase ex)
        {
            _logger.LogWarning(ex, "Erro de negócio: {Message}", ex.Message);
            await HandleExceptionAsync(httpContext, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            await HandleExceptionAsync(httpContext, (int)HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno.");
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        var result = JsonConvert.SerializeObject(new { error = message });

        var details = new ProblemDetails
        {
            Title = message,
            Status = statusCode,
            Detail = result,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(details);
    }
}
