using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CounterpartiesController : ControllerBase
{
    private readonly ICounterpartyService _counterpartyService;

    public CounterpartiesController(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    /// <summary>
    /// Получить список всех контрагентов с опциональной фильтрацией по типу.
    /// </summary>
    /// <param name="type">Тип контрагента (Client, Supplier, Company).</param>
    /// <returns>Список контрагентов.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetAll([FromQuery] CounterpartyType? type = null)
    {
        var counterparties = await _counterpartyService.GetAllAsync(type);
        return Ok(counterparties);
    }

    /// <summary>
    /// Получить контрагента по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор контрагента.</param>
    /// <returns>Данные контрагента.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CounterpartyResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var counterparty = await _counterpartyService.GetByIdAsync(id);
        return counterparty is null ? NotFound() : Ok(counterparty);
    }

    /// <summary>
    /// Получить список всех клиентов.
    /// </summary>
    /// <returns>Список контрагентов типа "Client".</returns>
    [HttpGet("clients")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _counterpartyService.GetAllAsync(CounterpartyType.Client);
        return Ok(clients);
    }

    /// <summary>
    /// Получить список всех поставщиков.
    /// </summary>
    /// <returns>Список контрагентов типа "Supplier".</returns>
    [HttpGet("suppliers")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _counterpartyService.GetAllAsync(CounterpartyType.Supplier);
        return Ok(suppliers);
    }

    /// <summary>
    /// Получить список наших компаний.
    /// </summary>
    /// <returns>Список контрагентов типа "Company".</returns>
    [HttpGet("companies")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await _counterpartyService.GetAllAsync(CounterpartyType.Company);
        return Ok(companies);
    }

    /// <summary>
    /// Создать нового контрагента.
    /// </summary>
    /// <param name="dto">Данные контрагента.</param>
    /// <returns>Созданный контрагент.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CounterpartyResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CounterpartyCreateDto dto)
    {
        var created = await _counterpartyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Обновить данные контрагента.
    /// </summary>
    /// <param name="id">Идентификатор контрагента.</param>
    /// <param name="dto">Новые данные контрагента.</param>
    /// <returns>Обновленный контрагент.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CounterpartyResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] CounterpartyUpdateDto dto)
    {
        var updated = await _counterpartyService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Удалить контрагента.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого контрагента.</param>
    /// <returns>Успешность операции.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _counterpartyService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}