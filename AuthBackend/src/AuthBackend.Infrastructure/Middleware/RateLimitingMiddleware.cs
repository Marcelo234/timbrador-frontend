using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthBackend.Infrastructure.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)> _requests = new();

    private static readonly HashSet<string> _limitedEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register"
    };

    private const int MaxRequests = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (!_limitedEndpoints.Contains(path))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{path}";
        var now = DateTime.UtcNow;

        var entry = _requests.AddOrUpdate(key,
            _ => (1, now.Add(Window)),
            (_, existing) =>
            {
                if (now >= existing.ResetTime)
                    return (1, now.Add(Window));
                return (existing.Count + 1, existing.ResetTime);
            });

        if (entry.Count > MaxRequests)
        {
            var retryAfter = (int)Math.Ceiling((entry.ResetTime - now).TotalSeconds);
            _logger.LogWarning("Rate limit excedido para {Key}", key);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.ContentType = "application/json";

            var response = new { error = "Demasiadas solicitudes. Intente más tarde.", retryAfter };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }
}
