using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly ICounterpartyRepository _counterpartyRepository;
    private readonly ILogger<CounterpartyService> _logger;

    public CounterpartyService(ICounterpartyRepository counterpartyRepository, ILogger<CounterpartyService> logger)
    {
        _counterpartyRepository = counterpartyRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CounterpartyResponseDto>> GetAllAsync(CounterpartyType? type = null)
    {
        _logger.LogInformation("[DB] Запрос списка контрагентов, фильтр по типу: {Type}", type?.ToString() ?? "все");
        var counterparties = await _counterpartyRepository.GetAllAsync(type);
        _logger.LogInformation("[DB] Получено {Count} контрагентов", counterparties.Count());
        return counterparties.Select(MapToResponse);
    }

    public async Task<CounterpartyResponseDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("[DB] Запрос контрагента по Id={Id}", id);
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        if (counterparty is null)
            _logger.LogWarning("[DB] Контрагент с Id={Id} не найден", id);
        return counterparty is null ? null : MapToResponse(counterparty);
    }

    public async Task<CounterpartyResponseDto> CreateAsync(CounterpartyCreateDto dto)
    {
        _logger.LogInformation("[DB] Создание контрагента: наименование='{Name}', тип={Type}", dto.Name, dto.Type);
        var counterparty = new Counterparty
        {
            Name = dto.Name,
            Type = dto.Type,
            Inn = dto.Inn,
            Address = dto.Address,
            Contacts = dto.Contacts.Select(c => new Contact
            {
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email
            }).ToList()
        };

        var created = await _counterpartyRepository.CreateAsync(counterparty);
        _logger.LogInformation("[DB] Контрагент создан успешно: Id={Id}, наименование='{Name}'", created.Id, created.Name);
        return MapToResponse(created);
    }

    public async Task<CounterpartyResponseDto?> UpdateAsync(int id, CounterpartyUpdateDto dto)
    {
        _logger.LogInformation("[DB] Обновление контрагента Id={Id}", id);
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        if (counterparty is null)
        {
            _logger.LogWarning("[DB] Контрагент с Id={Id} не найден при попытке обновления", id);
            return null;
        }

        counterparty.Name = dto.Name;
        counterparty.Type = dto.Type;
        counterparty.Inn = dto.Inn;
        counterparty.Address = dto.Address;

        // Обновляем контакты: новые добавляем, существующие обновляем, удалённые убираем
        var incomingIds = dto.Contacts
            .Where(c => c.Id.HasValue)
            .Select(c => c.Id!.Value)
            .ToHashSet();

        // Удаляем контакты которых нет в запросе
        counterparty.Contacts.RemoveAll(c => !incomingIds.Contains(c.Id));

        foreach (var contactDto in dto.Contacts)
        {
            if (contactDto.Id.HasValue)
            {
                // Обновляем существующий
                var existing = counterparty.Contacts.FirstOrDefault(c => c.Id == contactDto.Id.Value);
                if (existing is not null)
                {
                    existing.Name = contactDto.Name;
                    existing.Phone = contactDto.Phone;
                    existing.Email = contactDto.Email;
                }
            }
            else
            {
                // Добавляем новый
                counterparty.Contacts.Add(new Contact
                {
                    Name = contactDto.Name,
                    Phone = contactDto.Phone,
                    Email = contactDto.Email
                });
            }
        }

        var updated = await _counterpartyRepository.UpdateAsync(counterparty);
        _logger.LogInformation("[DB] Контрагент Id={Id} успешно обновлён", id);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("[DB] Запрос на удаление контрагента Id={Id}", id);
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        if (counterparty is null)
        {
            _logger.LogWarning("[DB] Контрагент с Id={Id} не найден при попытке удаления", id);
            return false;
        }

        await _counterpartyRepository.DeleteAsync(counterparty);
        _logger.LogInformation("[DB] Контрагент Id={Id} ('{Name}') успешно удалён", id, counterparty.Name);
        return true;
    }

    private static CounterpartyResponseDto MapToResponse(Counterparty c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Type = c.Type,
        Inn = c.Inn,
        Address = c.Address,
        Contacts = c.Contacts.Select(contact => new ContactResponseDto
        {
            Id = contact.Id,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email
        }).ToList()
    };
}