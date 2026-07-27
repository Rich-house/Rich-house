using Hangfire;
using Hangfire.SqlServer;
using Marketify.CatalogImport;
using Marketify;
using Marketify.Date;
using Marketify.PaymentServices;
using Marketify.Roles;
using Marketify.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var richHouseImportCommand = RichHouseImportCommand.Parse(args, builder.Environment.ContentRootPath);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} " +
        "[{Level:u3}] " +
        "[{SourceContext}] " +
        "{Message:lj}" +
        "{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDependencies(builder.Configuration);

var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp",
        policy =>
        {
            if (corsSettings.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
});

var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("Connection string 'HangfireConnection' was not found.");

builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(
              hangfireConnectionString,
              new SqlServerStorageOptions
              {
                  PrepareSchemaIfNecessary = true,
                  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                  QueuePollInterval = TimeSpan.FromSeconds(15),
                  UseRecommendedIsolationLevel = true,
                  DisableGlobalLocks = true
              }));

builder.Services.AddHangfireServer();

builder.Services.AddRateLimiter(options =>
{
    options.AddConcurrencyLimiter("concurrency", opt =>
    {
        opt.PermitLimit = 2;

        opt.QueueLimit = 1;

        opt.QueueProcessingOrder =
            QueueProcessingOrder.OldestFirst;
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<PaymobService>();

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });

    app.MapMethods("/", ["GET", "HEAD"], () => Results.Redirect("/swagger/index.html"));
    app.MapMethods("/swagger/index.html", ["HEAD"], () => Results.Ok());
}

await InitializeApplicationDatabasesAsync(
    app.Services,
    startupLogger,
    app.Environment,
    app.Lifetime.ApplicationStopping);
startupLogger.LogInformation("Hangfire storage is ready for database {DatabaseName}.", GetDatabaseName(hangfireConnectionString));

if (richHouseImportCommand.ShouldRun)
{
    await RunRichHouseCatalogImportAsync(app.Services, startupLogger, richHouseImportCommand, app.Lifetime.ApplicationStopping);
    Log.CloseAndFlush();
    return;
}

app.UseRateLimiter();
app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontendApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).AllowAnonymous();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Count > 0 ? string.Join(", ", app.Urls) : "configured endpoints";
    startupLogger.LogInformation("Rich House API started on {Urls}.", urls);
});
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

static async Task InitializeApplicationDatabasesAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger,
    IHostEnvironment environment,
    CancellationToken cancellationToken)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(3);

    if (!environment.IsDevelopment())
    {
        await InitializeApplicationDatabasesOnceAsync(services, logger, environment, cancellationToken);
        return;
    }

    Exception? lastTransientException = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            if (attempt > 1)
            {
                logger.LogInformation(
                    "Retrying SQL Server startup initialization for Development (attempt {Attempt} of {MaxAttempts}).",
                    attempt,
                    maxAttempts);
            }

            await InitializeApplicationDatabasesOnceAsync(services, logger, environment, cancellationToken);

            if (attempt > 1)
            {
                logger.LogInformation(
                    "SQL Server startup initialization completed successfully on attempt {Attempt} of {MaxAttempts}.",
                    attempt,
                    maxAttempts);
            }

            return;
        }
        catch (Exception ex) when (IsSqlAuthenticationFailure(ex))
        {
            logger.LogError(
                ex,
                "Database startup failed because SQL Server rejected the configured credentials. Update the Development connection strings or local secrets to match the running SQL Server login.");
            throw;
        }
        catch (Exception ex) when (IsTransientSqlStartupDelay(ex))
        {
            lastTransientException = ex;

            if (attempt == maxAttempts)
            {
                break;
            }

            logger.LogWarning(
                ex,
                "SQL Server is not ready yet during Development startup. Waiting {DelaySeconds} seconds before retry {NextAttempt} of {MaxAttempts}.",
                delay.TotalSeconds,
                attempt + 1,
                maxAttempts);

            await Task.Delay(delay, cancellationToken);
        }
    }

    throw new InvalidOperationException(
        $"SQL Server was still unavailable after {maxAttempts} startup attempts in Development. Confirm Docker is running, the '{GetContainerNameHint()}' container is ready, and the local SQL credentials are correct.",
        lastTransientException);
}

static async Task InitializeApplicationDatabasesOnceAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger,
    IHostEnvironment environment,
    CancellationToken cancellationToken)
{
    await EnsureApplicationDatabasesAsync(services, logger, cancellationToken);
    await ApplyMigrationsAndSeedAsync(services, logger, environment, cancellationToken);
}

static async Task EnsureApplicationDatabasesAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    foreach (var connectionName in new[] { "DefaultConnection", "HangfireConnection" })
    {
        var connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' was not found.");

        await EnsureDatabaseExistsAsync(connectionString, logger, cancellationToken);
    }
}

static async Task EnsureDatabaseExistsAsync(
    string connectionString,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    var connectionBuilder = new SqlConnectionStringBuilder(connectionString);
    var databaseName = connectionBuilder.InitialCatalog;

    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("The SQL Server connection string must include a database name.");
    }

    logger.LogInformation(
        "Ensuring SQL Server database {DatabaseName} exists on {DataSource}.",
        databaseName,
        connectionBuilder.DataSource);

    var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
    {
        InitialCatalog = "master"
    }.ConnectionString;

    await using var connection = new SqlConnection(masterConnectionString);
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText =
        $"""
        IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL
        BEGIN
            EXEC('CREATE DATABASE [{databaseName.Replace("]", "]]")}]');
        END
        """;

    await command.ExecuteNonQueryAsync(cancellationToken);
}

static async Task ApplyMigrationsAndSeedAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger,
    IHostEnvironment environment,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    logger.LogInformation(
        "Applying EF Core migrations to database {DatabaseName}.",
        dbContext.Database.GetDbConnection().Database);

    await dbContext.Database.MigrateAsync(cancellationToken);

    logger.LogInformation("EF Core migrations completed successfully.");
    logger.LogInformation("Starting identity role seeding.");

    await DatabaseSeeder.SeedRolesAsync(scope.ServiceProvider, logger, cancellationToken);

    logger.LogInformation("Identity role seeding completed successfully.");

    if (environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedDevelopmentSuperAdminAsync(scope.ServiceProvider, logger, cancellationToken);
    }
}

static string GetDatabaseName(string connectionString)
{
    return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
}

static bool IsTransientSqlStartupDelay(Exception exception)
{
    if (IsSqlAuthenticationFailure(exception))
    {
        return false;
    }

    return FlattenExceptions(exception).Any(static ex =>
        ex is TimeoutException
        || ex is SqlException sqlException && IsTransientSqlStartupNumber(sqlException.Number)
        || ex is DbException dbException && ContainsTransientSqlStartupMessage(dbException.Message)
        || ex is InvalidOperationException invalidOperationException && ContainsTransientSqlStartupMessage(invalidOperationException.Message)
        || ContainsTransientSqlStartupMessage(ex.Message));
}

static bool IsSqlAuthenticationFailure(Exception exception)
{
    return FlattenExceptions(exception).Any(static ex =>
        ex is SqlException sqlException && sqlException.Number == 18456
        || ex.Message.Contains("Login failed for user", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("password did not match", StringComparison.OrdinalIgnoreCase));
}

static bool IsTransientSqlStartupNumber(int errorNumber)
{
    return errorNumber is -2 or 2 or 20 or 53 or 64 or 233 or 258 or 11001;
}

static bool ContainsTransientSqlStartupMessage(string message)
{
    return message.Contains("Could not open a connection to SQL Server", StringComparison.OrdinalIgnoreCase)
        || message.Contains("server was not found or was not accessible", StringComparison.OrdinalIgnoreCase)
        || message.Contains("network-related or instance-specific error occurred", StringComparison.OrdinalIgnoreCase)
        || message.Contains("provider: TCP Provider, error: 40", StringComparison.OrdinalIgnoreCase)
        || message.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Login timeout expired", StringComparison.OrdinalIgnoreCase)
        || message.Contains("connection refused", StringComparison.OrdinalIgnoreCase);
}

static IEnumerable<Exception> FlattenExceptions(Exception exception)
{
    var queue = new Queue<Exception>();
    queue.Enqueue(exception);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        yield return current;

        if (current is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                queue.Enqueue(innerException);
            }
        }

        if (current.InnerException is not null)
        {
            queue.Enqueue(current.InnerException);
        }
    }
}

static string GetContainerNameHint()
{
    return "marketify-sql";
}

static async Task RunRichHouseCatalogImportAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger,
    RichHouseImportCommand command,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<IRichHouseCatalogImporter>();

    logger.LogInformation(
        "Starting Rich House catalog import from zip {ZipPath}.",
        command.ZipPath);

    var summary = await importer.ImportAsync(command, cancellationToken);

    logger.LogInformation(
        "Rich House catalog import finished. Products: {ProductsImported}, Categories: {CategoriesActivated}, AssignedImages: {AssignedImages}, Duplicates: {DuplicateGroups}, UnassignedFiles: {UnassignedFiles}.",
        summary.ProductsImported,
        summary.CategoriesActivated,
        summary.AssignedImages,
        summary.DuplicateGroups.Count,
        summary.UnassignedFiles.Count);

    if (!string.IsNullOrWhiteSpace(command.ReportPath))
    {
        logger.LogInformation("Image import report written to {ReportPath}.", command.ReportPath);
    }
}
