using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Models;

namespace WarehouseAPI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Проверяем, есть ли уже товары в базе
            if (await context.Products.AnyAsync())
            {
                return; // База уже заполнена
            }

            // 1. Создаём товары
            var products = new List<Product>
            {
                new Product { Name = "Ноутбук Lenovo", Article = "LNV-001", Unit = "шт", Price = 45000, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new Product { Name = "Мышь беспроводная", Article = "MOU-002", Unit = "шт", Price = 1200, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new Product { Name = "Клавиатура механическая", Article = "KEY-003", Unit = "шт", Price = 3500, CreatedAt = DateTime.UtcNow.AddDays(-15) },
                new Product { Name = "Монитор 24''", Article = "MON-004", Unit = "шт", Price = 18500, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new Product { Name = "SSD диск 1TB", Article = "SSD-005", Unit = "шт", Price = 8900, CreatedAt = DateTime.UtcNow.AddDays(-5) }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // 2. Создаём остатки
            var stocks = new List<Stock>
            {
                new Stock { ProductId = products[0].Id, Quantity = 12, UpdatedAt = DateTime.UtcNow },
                new Stock { ProductId = products[1].Id, Quantity = 45, UpdatedAt = DateTime.UtcNow },
                new Stock { ProductId = products[2].Id, Quantity = 8, UpdatedAt = DateTime.UtcNow },
                new Stock { ProductId = products[3].Id, Quantity = 3, UpdatedAt = DateTime.UtcNow },
                new Stock { ProductId = products[4].Id, Quantity = 0, UpdatedAt = DateTime.UtcNow }
            };

            await context.Stocks.AddRangeAsync(stocks);

            // 3. Создаём контрагентов
            var counterparties = new List<Counterparty>
            {
                new Counterparty { Name = "ООО ТехноПоставка", Type = CounterpartyType.Supplier, Inn = "7701234567", Address = "г. Москва, ул. Складская, 15" },
                new Counterparty { Name = "ИП Иванов А.А.", Type = CounterpartyType.Client, Inn = "7723456789", Address = "г. Москва, ул. Торговая, 8" },
                new Counterparty { Name = "ЗАО Электроника", Type = CounterpartyType.Company, Inn = "7734567890", Address = "г. Санкт-Петербург, Невский пр., 100" }
            };

            await context.Counterparties.AddRangeAsync(counterparties);
            await context.SaveChangesAsync();

            // 4. Создаём контакты
            var contacts = new List<Contact>
            {
                new Contact { CounterpartyId = counterparties[0].Id, Name = "Петров Иван", Phone = "+7 (495) 123-45-67", Email = "petrov@tehnopost.ru" },
                new Contact { CounterpartyId = counterparties[0].Id, Name = "Сидорова Мария", Phone = "+7 (495) 765-43-21", Email = "sidorova@tehnopost.ru" },
                new Contact { CounterpartyId = counterparties[1].Id, Name = "Иванов Алексей", Phone = "+7 (916) 111-22-33", Email = "ivanov@mail.ru" },
                new Contact { CounterpartyId = counterparties[2].Id, Name = "Смирнов Дмитрий", Phone = "+7 (812) 444-55-66", Email = "smirnov@electronics.ru" }
            };

            await context.Contacts.AddRangeAsync(contacts);

            // 5. Создаём складские операции
            var operations = new List<Operation>
            {
                new Operation { Type = OperationType.Income, Date = DateTime.UtcNow.AddDays(-5), Comment = "Поставка товаров", CounterpartyId = counterparties[0].Id },
                new Operation { Type = OperationType.Sale, Date = DateTime.UtcNow.AddDays(-3), Comment = "Продажа клиенту", CounterpartyId = counterparties[1].Id },
                new Operation { Type = OperationType.Income, Date = DateTime.UtcNow.AddDays(-1), Comment = "Дополнительная поставка", CounterpartyId = counterparties[0].Id }
            };

            await context.Operations.AddRangeAsync(operations);
            await context.SaveChangesAsync();

            // 6. Создаём детали складских операций (строки)
            var operationItems = new List<OperationItem>
            {
                new OperationItem { OperationId = operations[0].Id, ProductId = products[0].Id, Quantity = 5, Price = 45000 },
                new OperationItem { OperationId = operations[0].Id, ProductId = products[1].Id, Quantity = 20, Price = 1200 },
                new OperationItem { OperationId = operations[1].Id, ProductId = products[2].Id, Quantity = 2, Price = 3500 },
                new OperationItem { OperationId = operations[2].Id, ProductId = products[4].Id, Quantity = 10, Price = 8900 }
            };

            await context.OperationItems.AddRangeAsync(operationItems);
            await context.SaveChangesAsync();
        }
    }
}
