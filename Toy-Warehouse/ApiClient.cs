using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WarehouseApp
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5023/api/")
        };

        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        static ApiClient()
        {
            // Увеличиваем лимит сериализации, если данных будет много
            _serializer.MaxJsonLength = 20971520;
        }

        // Общий метод для GET запросов
        public static async Task<T> GetAsync<T>(string path)
        {
            try
            {
                var response = await _httpClient.GetAsync(path);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return _serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении данных ({path}): {ex.Message}", ex);
            }
        }

        // Общий метод для POST запросов
        public static async Task<TResult> PostAsync<TInput, TResult>(string path, TInput data)
        {
            try
            {
                var jsonInput = _serializer.Serialize(data);
                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(path, content);
                
                var jsonOutput = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    // Пытаемся извлечь красивую ошибку бэкенда
                    try
                    {
                        var errorObj = _serializer.Deserialize<Dictionary<string, object>>(jsonOutput);
                        if (errorObj.ContainsKey("message"))
                            throw new Exception(errorObj["message"].ToString());
                    }
                    catch (Exception ex) when (!(ex is Exception && ex.Source == null))
                    {
                        // Если распарсить не вышло, кидаем стандартную ошибку
                    }
                    response.EnsureSuccessStatusCode();
                }

                return _serializer.Deserialize<TResult>(jsonOutput);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при отправке данных ({path}): {ex.Message}", ex);
            }
        }

        // Общий метод для PUT запросов
        public static async Task<TResult> PutAsync<TInput, TResult>(string path, TInput data)
        {
            try
            {
                var jsonInput = _serializer.Serialize(data);
                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(path, content);
                
                var jsonOutput = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorObj = _serializer.Deserialize<Dictionary<string, object>>(jsonOutput);
                        if (errorObj.ContainsKey("message"))
                            throw new Exception(errorObj["message"].ToString());
                    }
                    catch (Exception ex) when (!(ex is Exception && ex.Source == null))
                    {
                    }
                    response.EnsureSuccessStatusCode();
                }

                return _serializer.Deserialize<TResult>(jsonOutput);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обновлении данных ({path}): {ex.Message}", ex);
            }
        }

        // Общий метод для DELETE запросов
        public static async Task DeleteAsync(string path)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(path);
                if (!response.IsSuccessStatusCode)
                {
                    var jsonOutput = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = _serializer.Deserialize<Dictionary<string, object>>(jsonOutput);
                        if (errorObj.ContainsKey("message"))
                            throw new Exception(errorObj["message"].ToString());
                    }
                    catch (Exception ex) when (!(ex is Exception && ex.Source == null))
                    {
                    }
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении ({path}): {ex.Message}", ex);
            }
        }

        // ─── API ЗВЕРШЕННЫЕ МЕТОДЫ ───────────────────────────────────────────────

        // Товары
        public static Task<List<Dictionary<string, object>>> GetProductsAsync() => 
            GetAsync<List<Dictionary<string, object>>>("products");

        public static Task<Dictionary<string, object>> CreateProductAsync(Dictionary<string, object> product) =>
            PostAsync<Dictionary<string, object>, Dictionary<string, object>>("products", product);

        public static Task DeleteProductAsync(int id) =>
            DeleteAsync($"products/{id}");

        // Остатки
        public static Task<List<Dictionary<string, object>>> GetStockAsync() =>
            GetAsync<List<Dictionary<string, object>>>("stock");

        // Контрагенты
        public static Task<List<Dictionary<string, object>>> GetCounterpartiesAsync() =>
            GetAsync<List<Dictionary<string, object>>>("counterparties");

        public static Task<Dictionary<string, object>> CreateCounterpartyAsync(Dictionary<string, object> counterparty) =>
            PostAsync<Dictionary<string, object>, Dictionary<string, object>>("counterparties", counterparty);

        public static Task DeleteCounterpartyAsync(int id) =>
            DeleteAsync($"counterparties/{id}");

        public static Task<Dictionary<string, object>> UpdateCounterpartyAsync(int id, Dictionary<string, object> counterparty) =>
            PutAsync<Dictionary<string, object>, Dictionary<string, object>>($"counterparties/{id}", counterparty);

        // Складские операции
        public static Task<List<Dictionary<string, object>>> GetOperationsAsync() =>
            GetAsync<List<Dictionary<string, object>>>("operations/history");

        public static Task<Dictionary<string, object>> CreateOperationAsync(string type, Dictionary<string, object> operation) =>
            PostAsync<Dictionary<string, object>, Dictionary<string, object>>($"operations/{type}", operation);

        // Аналитика
        public static Task<List<Dictionary<string, object>>> GetTopProductsAsync(string from, string to, int limit) =>
            GetAsync<List<Dictionary<string, object>>>($"analytics/top-products?from={from}&to={to}&limit={limit}");

        public static Task<List<Dictionary<string, object>>> GetTurnoverAsync(string from, string to) =>
            GetAsync<List<Dictionary<string, object>>>($"analytics/turnover?from={from}&to={to}");

        public static Task<List<Dictionary<string, object>>> GetLowStockAsync(decimal minQuantity) =>
            GetAsync<List<Dictionary<string, object>>>($"analytics/low-stock?minQuantity={minQuantity}");
    }
}
