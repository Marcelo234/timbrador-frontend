using AuthBackend.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Property tests para RateLimitingMiddleware
public class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<RateLimitingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext(string path, string ip = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        return context;
    }

    // Property 32: Después de 5 requests, el 6to retorna 429
    [Fact]
    public async Task RateLimit_DespuesDe5Requests_Retorna429()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // 5 requests permitidos
        for (int i = 0; i < 5; i++)
        {
            var ctx = CreateContext("/api/auth/login", "10.0.0.1");
            await middleware.InvokeAsync(ctx);
            Assert.NotEqual(429, ctx.Response.StatusCode);
        }

        // El 6to debe ser bloqueado
        var blocked = CreateContext("/api/auth/login", "10.0.0.1");
        await middleware.InvokeAsync(blocked);
        Assert.Equal(429, blocked.Response.StatusCode);
    }

    // Property 33: Respuesta 429 incluye header Retry-After con valor > 0
    [Fact]
    public async Task RateLimit_Respuesta429_IncluyeRetryAfter()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        for (int i = 0; i < 6; i++)
        {
            var ctx = CreateContext("/api/auth/login", "10.0.0.2");
            await middleware.InvokeAsync(ctx);
        }

        var blocked = CreateContext("/api/auth/login", "10.0.0.2");
        await middleware.InvokeAsync(blocked);

        Assert.Equal(429, blocked.Response.StatusCode);
        Assert.True(blocked.Response.Headers.ContainsKey("Retry-After"));
        Assert.True(int.Parse(blocked.Response.Headers["Retry-After"]!) > 0);
    }

    // Endpoints no limitados no son afectados
    [Fact]
    public async Task RateLimit_EndpointNoLimitado_NuncaRetorna429()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        for (int i = 0; i < 20; i++)
        {
            var ctx = CreateContext("/api/auth/refresh", "10.0.0.3");
            await middleware.InvokeAsync(ctx);
            Assert.NotEqual(429, ctx.Response.StatusCode);
        }
    }

    // IPs distintas tienen contadores independientes
    [Fact]
    public async Task RateLimit_IPsDistintas_ContadoresIndependientes()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // IP1 agota su límite
        for (int i = 0; i < 6; i++)
            await middleware.InvokeAsync(CreateContext("/api/auth/register", "192.168.1.1"));

        var blockedIp1 = CreateContext("/api/auth/register", "192.168.1.1");
        await middleware.InvokeAsync(blockedIp1);
        Assert.Equal(429, blockedIp1.Response.StatusCode);

        // IP2 no debe estar bloqueada
        var ip2 = CreateContext("/api/auth/register", "192.168.1.2");
        await middleware.InvokeAsync(ip2);
        Assert.NotEqual(429, ip2.Response.StatusCode);
    }
}
