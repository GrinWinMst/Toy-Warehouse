using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Получить топ продаваемых товаров за период.
    /// </summary>
    /// <param name="from">Дата начала периода.</param>
    /// <param name="to">Дата окончания периода.</param>
    /// <param name="limit">Количество товаров в топе (от 1 до 100).</param>
    /// <returns>Список товаров с их показателями.</returns>
    [HttpGet("top-products")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WarehouseAPI.DTOs.Analytics.TopProductDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int limit = 10)
    {
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();

        if (from > to)
            return BadRequest(new { message = "Дата начала не может быть позже даты конца" });

        if (limit is < 1 or > 100)
            return BadRequest(new { message = "Лимит должен быть от 1 до 100" });

        var result = await _analyticsService.GetTopProductsAsync(from, to, limit);
        return Ok(result);
    }

    /// <summary>
    /// Получить общие обороты (приход, расход, списание) за период.
    /// </summary>
    /// <param name="from">Дата начала периода.</param>
    /// <param name="to">Дата окончания периода.</param>
    /// <returns>Данные об оборотах с разбивкой по дням.</returns>
    [HttpGet("turnover")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseAPI.DTOs.Analytics.TurnoverDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTurnover(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        from = from.ToUniversalTime();
        to = to.ToUniversalTime();

        if (from > to)
            return BadRequest(new { message = "Дата начала не может быть позже даты конца" });

        var result = await _analyticsService.GetTurnoverAsync(from, to);
        return Ok(result);
    }

    /// <summary>
    /// Получить список товаров, остаток которых ниже минимально допустимого.
    /// </summary>
    /// <param name="minQuantity">Минимально допустимый остаток.</param>
    /// <returns>Список товаров с низким остатком.</returns>
    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WarehouseAPI.DTOs.Analytics.LowStockDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLowStock([FromQuery] decimal minQuantity = 5)
    {
        if (minQuantity < 0)
            return BadRequest(new { message = "Минимальное количество не может быть отрицательным" });

        var result = await _analyticsService.GetLowStockAsync(minQuantity);
        return Ok(result);
    }
}