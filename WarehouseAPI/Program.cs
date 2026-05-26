using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Filters;
using WarehouseAPI.Middleware;
using WarehouseAPI.Repositories;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services;
using WarehouseAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool usePostgres = false;

if (!string.IsNullOrWhiteSpace(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("Server=")))
{
    string host = "localhost";
    int port = 5432;
    
    try
    {
        var parts = connectionString.Split(';')
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);
            
        if (parts.TryGetValue("Host", out var h)) host = h;
        else if (parts.TryGetValue("Server", out var s)) host = s;
        
        if (parts.TryGetValue("Port", out var pStr)) int.TryParse(pStr, out port);
        
        // Быстрая проверка доступности порта PostgreSQL по TCP (таймаут 400 мс)
        using (var tcpClient = new System.Net.Sockets.TcpClient())
        {
            var connectTask = tcpClient.ConnectAsync(host, port);
            if (Task.WhenAny(connectTask, Task.Delay(400)).Result == connectTask)
            {
                if (tcpClient.Connected)
                {
                    usePostgres = true;
                }
            }
        }
    }
    catch
    {
        usePostgres = false;
    }
}

if (usePostgres)
{
    Console.WriteLine("DB CONFIG: PostgreSQL server is active. Using PostgreSQL database.");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("DB CONFIG: PostgreSQL is unreachable or not configured. Gracefully falling back to SQLite (warehouse.db).");
    var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "warehouse.db");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={sqlitePath}"));
}

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

// Инициализация базы данных и сидирование демонстрационных данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Создаем БД, если она не существует (автоматически создает все таблицы)
        context.Database.EnsureCreated();
        
        // Наполняем демо-данными
        await WarehouseAPI.Data.DbSeeder.SeedAsync(context);
        
        logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();