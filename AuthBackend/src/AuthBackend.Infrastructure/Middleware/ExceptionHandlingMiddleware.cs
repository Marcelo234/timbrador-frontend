using System.Text.Json;
using AuthBackend.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthBackend.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            UsuarioYaExisteException e => (StatusCodes.Status400BadRequest, e.Message),
            CredencialesInvalidasException e => (StatusCodes.Status401Unauthorized, e.Message),
            TokenInvalidoException e => (StatusCodes.Status401Unauthorized, e.Message),
            UsuarioInactivoException e => (StatusCodes.Status403Forbidden, e.Message),
            ValidationException e => (StatusCodes.Status400BadRequest, string.Join("; ", e.Errors.Select(err => err.ErrorMessage))),
            _ => (StatusCodes.Status500InternalServerError, "Ha ocurrido un error interno")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado");
        else
            _logger.LogWarning(exception, "Error de negocio: {Message}", message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { error = message, timestamp = DateTime.UtcNow };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
