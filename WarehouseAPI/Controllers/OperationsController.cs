using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.DTOs.Operations;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly IOperationService _operationService;

    public OperationsController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    /// <summary>
    /// Получить историю операций с фильтрацией.
    /// </summary>
    /// <param name="filter">Параметры фильтрации (дата, тип, контрагент, товар).</param>
    /// <returns>Страница с операциями.</returns>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OperationResponseDto>))]
    public async Task<IActionResult> GetHistory([FromQuery] OperationFilterDto filter)
    {
        var operations = await _operationService.GetHistoryAsync(filter);
        return Ok(operations);
    }

    /// <summary>
    /// Получить операцию по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор операции.</param>
    /// <returns>Данные операции с её позициями.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var operation = await _operationService.GetByIdAsync(id);
        return operation is null ? NotFound() : Ok(operation);
    }

    /// <summary>
    /// Зарегистрировать приход товара (Income).
    /// </summary>
    /// <param name="dto">Данные о поступлении товара.</param>
    /// <returns>Созданная операция.</returns>
    [HttpPost("income")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OperationResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Income([FromBody] IncomeCreateDto dto)
    {
        var result = await _operationService.IncomeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Зарегистрировать продажу товара (Sale).
    /// </summary>
    /// <param name="dto">Данные о продаже.</param>
    /// <returns>Созданная операция.</returns>
    [HttpPost("sale")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OperationResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sale([FromBody] SaleCreateDto dto)
    {
        var result = await _operationService.SaleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Зарегистрировать перемещение товара (Transfer).
    /// </summary>
    /// <param name="dto">Данные о перемещении.</param>
    /// <returns>Созданная операция.</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OperationResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transfer([FromBody] TransferCreateDto dto)
    {
        var result = await _operationService.TransferAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Зарегистрировать списание товара (WriteOff).
    /// </summary>
    /// <param name="dto">Данные о списании.</param>
    /// <returns>Созданная операция.</returns>
    [HttpPost("writeoff")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OperationResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WriteOff([FromBody] WriteOffCreateDto dto)
    {
        var result = await _operationService.WriteOffAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}