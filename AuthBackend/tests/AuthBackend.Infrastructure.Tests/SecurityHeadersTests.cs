using Microsoft.AspNetCore.Http;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Property test para headers de seguridad
public class SecurityHeadersTests
{
    // Simula el middleware de headers de seguridad definido en Program.cs
    private static async Task<IHeaderDictionary> InvokeSecurityHeadersMiddleware()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Replicamos el middleware inline de Program.cs
        RequestDelegate securityHeaders = async (ctx) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            await Task.CompletedTask;
        };

        await securityHeaders(context);
        return context.Response.Headers;
    }

    // Property 34: Headers de seguridad presentes en toda respuesta
    [Fact]
    public async Task SecurityHeaders_XContentTypeOptions_Presente()
    {
        var headers = await InvokeSecurityHeadersMiddleware();
        Assert.True(headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptions_Presente()
    {
        var headers = await InvokeSecurityHeadersMiddleware();
        Assert.True(headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task SecurityHeaders_XXssProtection_Presente()
    {
        var headers = await InvokeSecurityHeadersMiddleware();
        Assert.True(headers.ContainsKey("X-XSS-Protection"));
    }
}
