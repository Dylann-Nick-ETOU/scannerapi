using System.Text;
using System.Threading.RateLimiting;
using ApiSecurityScanner.API.Authentication;
using ApiSecurityScanner.API.Middlewares;
using ApiSecurityScanner.Application;
using ApiSecurityScanner.Domain.Entities;
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
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Authentication"));
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.PasswordHasher<string>>();
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ScanDelete", policy => policy.RequireRole("Admin"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ScanRequests", context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

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
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.AllowAnyHeader().AllowAnyMethod();

        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy.WithOrigins(allowedOrigins);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
    var authOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthOptions>>();
    Microsoft.Extensions.Logging.ILogger logger =
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    try
    {
        if (db.Database.IsNpgsql())
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
        else
        {
            db.Database.EnsureCreated();
        }

        await EnsureSeedUsersAsync(db, authOptions.Value, logger);
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
app.UseRateLimiter();

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
            EnsureScanOwnerColumn(connection, logger);
            EnsureUserTable(connection, logger);
            EnsureUserColumns(connection, logger);
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

static async Task EnsureSeedUsersAsync(
    ApiSecurityScannerDbContext db,
    AuthOptions authOptions,
    Microsoft.Extensions.Logging.ILogger logger)
{
    if (authOptions.SeedUsers.Count == 0)
    {
        return;
    }

    foreach (var seedUser in authOptions.SeedUsers)
    {
        var exists = await db.AppUsers.AnyAsync(x => x.Username.ToLower() == seedUser.Username.ToLower());
        if (exists)
        {
            continue;
        }

        db.AppUsers.Add(new AppUser
        {
            Username = seedUser.Username,
            PasswordHash = seedUser.PasswordHash,
            Role = seedUser.Role
        });
        logger.LogInformation("Seeded auth user {Username}.", seedUser.Username);
    }

    await db.SaveChangesAsync();
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

static void EnsureUserTable(NpgsqlConnection connection, Microsoft.Extensions.Logging.ILogger logger)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS "AppUsers" (
            "Id" uuid NOT NULL,
            "Username" character varying(100) NOT NULL,
            "PasswordHash" character varying(500) NOT NULL,
            "Role" character varying(50) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_AppUsers" PRIMARY KEY ("Id")
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppUsers_Username" ON "AppUsers" ("Username");
        """;
    command.ExecuteNonQuery();
    logger.LogInformation("Ensured AppUsers table exists.");
}

static void EnsureUserColumns(NpgsqlConnection connection, Microsoft.Extensions.Logging.ILogger logger)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        ALTER TABLE "AppUsers"
        ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
        ALTER TABLE "AppUsers"
        ADD COLUMN IF NOT EXISTS "LastLoginAt" timestamp with time zone NULL;
        """;
    command.ExecuteNonQuery();
    logger.LogInformation("Ensured AppUsers security columns exist.");
}

static void EnsureScanOwnerColumn(NpgsqlConnection connection, Microsoft.Extensions.Logging.ILogger logger)
{
    using var checkCommand = connection.CreateCommand();
    checkCommand.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Scans'
          AND column_name = 'OwnerId';
        """;

    var columnExists = (long)(checkCommand.ExecuteScalar() ?? 0L) == 1;
    if (columnExists)
    {
        return;
    }

    logger.LogWarning("Column Scans.OwnerId was not found. Applying compatibility schema update.");

    using var alterCommand = connection.CreateCommand();
    alterCommand.CommandText = """
        ALTER TABLE "Scans"
        ADD COLUMN "OwnerId" character varying(200) NOT NULL DEFAULT 'legacy-user';
        CREATE INDEX IF NOT EXISTS "IX_Scans_OwnerId_CreatedAt" ON "Scans" ("OwnerId", "CreatedAt");
        """;
    alterCommand.ExecuteNonQuery();
}

public partial class Program;
