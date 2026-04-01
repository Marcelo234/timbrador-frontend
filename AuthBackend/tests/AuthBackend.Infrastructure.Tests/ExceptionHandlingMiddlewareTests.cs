using System.Text.Json;
using AuthBackend.Domain.Exceptions;
using AuthBackend.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Unit tests para ExceptionHandlingMiddleware
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, string Body)> InvokeWithException(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw ex,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task UsuarioYaExisteException_Retorna400()
    {
        var (status, body) = await InvokeWithException(new UsuarioYaExisteException("Email ya existe"));
        Assert.Equal(400, status);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Email ya existe", json.GetProperty("error").GetString());
        Assert.True(json.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task CredencialesInvalidasException_Retorna401()
    {
        var (status, _) = await InvokeWithException(new CredencialesInvalidasException("Credenciales inválidas"));
        Assert.Equal(401, status);
    }

    [Fact]
    public async Task TokenInvalidoException_Retorna401()
    {
        var (status, _) = await InvokeWithException(new TokenInvalidoException("Token inválido"));
        Assert.Equal(401, status);
    }

    [Fact]
    public async Task UsuarioInactivoException_Retorna403()
    {
        var (status, _) = await InvokeWithException(new UsuarioInactivoException("Usuario inactivo"));
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task ExcepcionGenerica_Retorna500()
    {
        var (status, body) = await InvokeWithException(new Exception("Error inesperado"));
        Assert.Equal(500, status);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Ha ocurrido un error interno", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task RespuestaJSON_ContieneErrorYTimestamp()
    {
        var (_, body) = await InvokeWithException(new CredencialesInvalidasException("msg"));
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.True(json.TryGetProperty("error", out _));
        Assert.True(json.TryGetProperty("timestamp", out _));
    }
}
