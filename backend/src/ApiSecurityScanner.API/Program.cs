using System.Text;
using ApiSecurityScanner.API.Middlewares;
using ApiSecurityScanner.Application;
using ApiSecurityScanner.Infrastructure;
using ApiSecurityScanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-security-scanner-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ApiSecurityScanner";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ApiSecurityScanner.Frontend";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey config.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiSecurityScanner.API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
    Microsoft.Extensions.Logging.ILogger logger =
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    try
    {
        var hasMigrations = db.Database.GetMigrations().Any();

        if (hasMigrations)
        {
            db.Database.Migrate();
        }
        else
        {
            logger.LogWarning("No EF Core migrations were found. Falling back to direct schema bootstrap.");
            EnsureApplicationSchema(db, logger);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed at startup");
        throw;
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseForwardedHeaders();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

static void EnsureApplicationSchema(
    ApiSecurityScannerDbContext db,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var connection = (NpgsqlConnection)db.Database.GetDbConnection();
    var wasClosed = connection.State != System.Data.ConnectionState.Open;

    if (wasClosed)
    {
        connection.Open();
    }

    try
    {
        if (ApplicationTablesExist(connection))
        {
            logger.LogInformation("Application tables already exist.");
            return;
        }

        logger.LogWarning("Application tables were not found. Creating schema via relational database creator.");
        db.GetService<IRelationalDatabaseCreator>().CreateTables();
    }
    finally
    {
        if (wasClosed)
        {
            connection.Close();
        }
    }
}

static bool ApplicationTablesExist(NpgsqlConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name IN ('Scans', 'SecurityIssues');
        """;

    var count = (long)(command.ExecuteScalar() ?? 0L);
    return count == 2;
}
