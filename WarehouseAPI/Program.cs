using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Filters;
using WarehouseAPI.Middleware;
using WarehouseAPI.Repositories;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services;
using WarehouseAPI.Services.Interfaces;

// Загружаем секреты из файла .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Собираем строку подключения из переменных окружения
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "warehouse_db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

// PostgreSQL — единственная поддерживаемая СУБД
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Репозитории
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICounterpartyRepository, CounterpartyRepository>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

// Сервисы
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICounterpartyService, CounterpartyService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// ValidationFilter глобально — применяется ко всем контроллерам
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Middleware первым — перехватывает все исключения ниже по pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        app.Logger.LogWarning(
            "Connection string 'DefaultConnection' is not configured. Database features will be unavailable.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.MapGet("/", () => Results.Ok(new { service = "WarehouseAPI", status = "running" }));
app.MapGet("/health", () => Results.Ok("ok"));

app.MapControllers();

// Применяем миграции автоматически при запуске
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
    }
}

app.Run();