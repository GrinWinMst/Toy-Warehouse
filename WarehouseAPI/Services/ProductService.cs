using WarehouseAPI.DTOs.Products;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
    {
        _logger.LogInformation("[DB] Запрос списка всех товаров");
        var products = await _productRepository.GetAllAsync();
        _logger.LogInformation("[DB] Получено {Count} товаров", products.Count());
        return products.Select(MapToResponse);
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("[DB] Запрос товара по Id={Id}", id);
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            _logger.LogWarning("[DB] Товар с Id={Id} не найден", id);
        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
    {
        _logger.LogInformation("[DB] Создание товара: артикул='{Article}', наименование='{Name}'", dto.Article, dto.Name);

        // Проверяем уникальность артикула
        if (await _productRepository.ArticleExistsAsync(dto.Article))
        {
            _logger.LogWarning("[BUSINESS] Попытка создать дубль артикула '{Article}'", dto.Article);
            throw new InvalidOperationException($"Товар с артикулом '{dto.Article}' уже существует");
        }

        var product = new Product
        {
            Name = dto.Name,
            Article = dto.Article,
            Unit = dto.Unit,
            Price = dto.Price,
            CreatedAt = DateTime.UtcNow,
            // Сразу создаём запись остатка с нулём
            Stock = new Stock { Quantity = 0, UpdatedAt = DateTime.UtcNow }
        };

        var created = await _productRepository.CreateAsync(product);
        _logger.LogInformation("[DB] Товар создан успешно: Id={Id}, артикул='{Article}'", created.Id, created.Article);
        return MapToResponse(created);
    }

    public async Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto)
    {
        _logger.LogInformation("[DB] Обновление товара Id={Id}", id);
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogWarning("[DB] Товар с Id={Id} не найден при попытке обновления", id);
            return null;
        }

        // Проверяем артикул только если он изменился
        if (product.Article != dto.Article &&
            await _productRepository.ArticleExistsAsync(dto.Article, excludeId: id))
        {
            _logger.LogWarning("[BUSINESS] Попытка задать уже используемый артикул '{Article}' для товара Id={Id}", dto.Article, id);
            throw new InvalidOperationException($"Товар с артикулом '{dto.Article}' уже существует");
        }

        product.Name = dto.Name;
        product.Article = dto.Article;
        product.Unit = dto.Unit;
        product.Price = dto.Price;

        var updated = await _productRepository.UpdateAsync(product);
        _logger.LogInformation("[DB] Товар Id={Id} успешно обновлён", id);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("[DB] Запрос на удаление товара Id={Id}", id);
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogWarning("[DB] Товар с Id={Id} не найден при попытке удаления", id);
            return false;
        }

        // Не даём удалить товар если есть ненулевой остаток
        if (product.Stock is not null && product.Stock.Quantity > 0)
        {
            _logger.LogWarning(
                "[BUSINESS] Блокировка удаления товара Id={Id} ('{Name}'): остаток на складе = {Qty} {Unit}",
                id, product.Name, product.Stock.Quantity, product.Unit);
            throw new InvalidOperationException(
                $"Нельзя удалить товар '{product.Name}': остаток на складе {product.Stock.Quantity} {product.Unit}");
        }

        await _productRepository.DeleteAsync(product);
        _logger.LogInformation("[DB] Товар Id={Id} ('{Name}') успешно удалён", id, product.Name);
        return true;
    }

    // Маппинг модели → DTO, в одном месте
    private static ProductResponseDto MapToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Article = p.Article,
        Unit = p.Unit,
        Price = p.Price,
        CurrentStock = p.Stock?.Quantity ?? 0,
        CreatedAt = p.CreatedAt
    };
}