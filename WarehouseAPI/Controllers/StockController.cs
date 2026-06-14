using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    /// <summary>
    /// Получить все остатки товаров на складе.
    /// </summary>
    /// <returns>Список остатков.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WarehouseAPI.DTOs.Stock.StockResponseDto>))]
    public async Task<IActionResult> GetAll()
    {
        var stocks = await _stockService.GetAllAsync();
        return Ok(stocks);
    }

    /// <summary>
    /// Получить текущий остаток по конкретному товару.
    /// </summary>
    /// <param name="productId">Идентификатор товара.</param>
    /// <returns>Остаток данного товара.</returns>
    [HttpGet("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseAPI.DTOs.Stock.StockResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var stock = await _stockService.GetByProductIdAsync(productId);
        return stock is null ? NotFound() : Ok(stock);
    }
}