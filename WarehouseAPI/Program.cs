using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using WarehouseAPI.Data;
using WarehouseAPI.Filters;
using WarehouseAPI.Middleware;
using WarehouseAPI.Repositories;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services;
using WarehouseAPI.Services.Interfaces;

// ─── SERILOG: Настройка логгера ДО создания приложения ────────────────────────
// Это позволяет перехватить ошибки даже на стадии старта (Fatal bootstrap errors)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // Понижаем уровень для шумных системных логов ASP.NET Core
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    // Канал 1: Консоль (формат для Docker / человеко-читаемый)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u4}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    // Канал 2: Файл с ротацией (10 МБ, 5 архивных копий, разбивка по дням)
    .WriteTo.File(
        path: "logs/app.log",
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u4}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024,   // 10 МБ
        retainedFileCountLimit: 5,               // хранить не более 5 файлов
        rollOnFileSizeLimit: true,
        shared: true)
    .CreateLogger();

// Загружаем секреты из файла .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Подключаем Serilog как провайдер логирования ASP.NET Core
builder.Host.UseSerilog();

// Собираем строку подключения из переменных окружения
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "warehouse_db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

Log.Information("[STARTUP] Сборка строки подключения: Host={Host}, Port={Port}, DB={DB}, User={User}",
    dbHost, dbPort, dbName, dbUser);

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
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Middleware первым — перехватывает все исключения ниже по pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
        Log.Information("[DB] Применение миграций базы данных...");
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Log.Information("[DB] Миграции применены успешно. Подключение к PostgreSQL установлено");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex,
            "[DB] КРИТИЧЕСКАЯ ОШИБКА: не удалось применить миграции или подключиться к СУБД. " +
            "Проверьте настройки .env. Подробности: {Message}",
            ex.Message);
    }
}

Log.Information("[STARTUP] WarehouseAPI запущен успешно. Swagger: /swagger");

try
{
    app.Run();
}
finally
{
    // Гарантируем что все буферизованные логи записаны на диск перед завершением
    Log.CloseAndFlush();
}