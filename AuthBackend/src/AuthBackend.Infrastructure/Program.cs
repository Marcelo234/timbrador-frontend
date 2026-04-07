using System.Text;
using AuthBackend.Application.UseCases;
using AuthBackend.Domain.Interfaces;
using AuthBackend.Infrastructure.Middleware;
using AuthBackend.Infrastructure.Persistence;
using AuthBackend.Infrastructure.Persistence.Repositories;
using AuthBackend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

// Show full PII in JWT errors (dev only — remove before production)
IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AuthBackend API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa el token JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── EF Core ────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Repositorios ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ISesionRepository, SesionRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();

// ─── Servicios ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordEncoder, PasswordEncoder>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ─── Use Cases ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<RegistrarUsuarioUseCase>();
builder.Services.AddScoped<IniciarSesionUseCase>();
builder.Services.AddScoped<CerrarSesionUseCase>();
builder.Services.AddScoped<SolicitarRecuperacionPasswordUseCase>();
builder.Services.AddScoped<RestablecerPasswordUseCase>();

// ─── JWT Authentication ─────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey no configurado");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] *** AUTH FAILED ***");
                Console.WriteLine($"[JWT] Exception: {ctx.Exception}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine($"[JWT] Token valid. Sub: {ctx.Principal?.FindFirst("sub")?.Value}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($"[JWT] Challenge. Error: {ctx.Error} | Desc: {ctx.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnMessageReceived = ctx =>
            {
                var header = ctx.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[JWT] Authorization header received: '{header[..Math.Min(50, header.Length)]}...'");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://shimmering-capybara-d503e2.netlify.app" // <--- El link de tu FRONTEND
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // <--- OBLIGATORIO PARA QUE EL LOGIN PASE LOS DATOS
    });
});

// ─── Build ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Swagger UI (always available) ──────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthBackend API v1"));

app.UseCors("FrontendPolicy");

// ─── Security Headers ────────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    await next();
});

// ─── Middleware Pipeline ─────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

// UseHttpsRedirection removed — dev server runs on HTTP only.
// Re-enable behind a reverse proxy (nginx/IIS) in production.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─── Auto-apply pending migrations ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

app.Run();
