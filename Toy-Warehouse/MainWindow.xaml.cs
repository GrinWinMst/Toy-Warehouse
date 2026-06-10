using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WarehouseApp
{
    // Реальные модели данных для биндинга
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Article { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FormattedPrice => $"{Price:N0} ₽";
        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
    }

    public class Stock
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductArticle { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FormattedDate => UpdatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        public string QuantityWarning => Quantity < 10 ? $"⚠️ {Quantity}" : Quantity.ToString();
    }

    public class Counterparty
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Клиент, Поставщик, Оба
        public string INN { get; set; }
        public string Address { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public int CounterpartyId { get; set; }
        public string CounterpartyName { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
    }

    public class Operation
    {
        public int Id { get; set; }
        public string Type { get; set; } // Приход, Расход, Перемещение, Списание
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public int CounterpartyId { get; set; }
        public string CounterpartyName { get; set; }
        public string FormattedDate => Date.ToLocalTime().ToString("dd.MM.yyyy");
    }

    public class OperationItem
    {
        public int OperationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
        public string FormattedPrice => $"{Price:N0} ₽";
        public string FormattedTotal => $"{Total:N0} ₽";
    }

    // Классы-отчеты для аналитики
    public class TopProductReportRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductArticle { get; set; }
        public string Unit { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public int OperationsCount { get; set; }
        public string FormattedAmount => $"{TotalAmount:N0} ₽";
    }

    public class TurnoverReportRow
    {
        public string FormattedDate { get; set; }
        public decimal IncomeAmount { get; set; }
        public decimal SaleAmount { get; set; }
        public string FormattedIncome => $"{IncomeAmount:N0} ₽";
        public string FormattedSale => $"{SaleAmount:N0} ₽";
    }

    public class LowStockReportRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductArticle { get; set; }
        public decimal CurrentQuantity { get; set; }
        public string Unit { get; set; }
        public decimal MinQuantity { get; set; }
        public string CurrentQuantityWarning => CurrentQuantity < MinQuantity ? $"⚠️ {CurrentQuantity}" : CurrentQuantity.ToString();
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private ObservableCollection<Stock> _stock = new ObservableCollection<Stock>();
        private ObservableCollection<Counterparty> _counterparties = new ObservableCollection<Counterparty>();
        private ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        private ObservableCollection<Operation> _operations = new ObservableCollection<Operation>();
        private ObservableCollection<OperationItem> _operationItems = new ObservableCollection<OperationItem>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализация дат для аналитики
            dpTopFrom.SelectedDate = DateTime.Now.AddDays(-30);
            dpTopTo.SelectedDate = DateTime.Now;
            dpTurnoverFrom.SelectedDate = DateTime.Now.AddDays(-30);
            dpTurnoverTo.SelectedDate = DateTime.Now;

            statusText.Text = "Подключение к API...";
            await RefreshActiveTabAsync();
        }

        // Асинхронная загрузка данных с API
        private async Task RefreshActiveTabAsync()
        {
            try
            {
                // Защита от NullReferenceException во время выполнения InitializeComponent()
                if (dataGrid == null ||
                    navAnalytics == null ||
                    filtersBorder == null ||
                    gridBorder == null ||
                    analyticsPanel == null ||
                    addButton == null ||
                    deleteButton == null ||
                    statusText == null)
                {
                    return;
                }

                // Переключение видимости в зависимости от того, выбрана ли аналитика
                if (navAnalytics.IsChecked == true)
                {
                    filtersBorder.Visibility = Visibility.Collapsed;
                    gridBorder.Visibility = Visibility.Collapsed;
                    analyticsPanel.Visibility = Visibility.Visible;
                    addButton.Visibility = Visibility.Collapsed;
                    deleteButton.Visibility = Visibility.Collapsed;
                    titleText.Text = "Аналитика";

                    // По умолчанию загружаем данные первой вкладки аналитики
                    await RefreshAnalyticsAsync();
                    statusText.Text = "Аналитика загружена";
                    return;
                }

                // Обычные разделы
                filtersBorder.Visibility = Visibility.Visible;
                gridBorder.Visibility = Visibility.Visible;
                analyticsPanel.Visibility = Visibility.Collapsed;
                addButton.Visibility = Visibility.Visible;
                deleteButton.Visibility = Visibility.Visible;

                ConfigureDataGrids();

                if (navProducts.IsChecked == true)
                {
                    statusText.Text = "Загрузка товаров...";
                    var data = await ApiClient.GetProductsAsync();
                    _products.Clear();
                    foreach (var d in data)
                    {
                        _products.Add(new Product
                        {
                            Id = Convert.ToInt32(d["id"]),
                            Name = d["name"]?.ToString(),
                            Article = d["article"]?.ToString(),
                            Unit = d["unit"]?.ToString(),
                            Price = Convert.ToDecimal(d["price"]),
                            CreatedAt = DateTime.Parse(d["createdAt"]?.ToString())
                        });
                    }
                    dataGrid.ItemsSource = _products;
                }
                else if (navStock.IsChecked == true)
                {
                    statusText.Text = "Загрузка остатков...";
                    var data = await ApiClient.GetStockAsync();
                    _stock.Clear();
                    foreach (var d in data)
                    {
                        _stock.Add(new Stock
                        {
                            ProductId = Convert.ToInt32(d["productId"]),
                            ProductName = d["productName"]?.ToString(),
                            ProductArticle = d["productArticle"]?.ToString(),
                            Unit = d["unit"]?.ToString(),
                            Quantity = Convert.ToDecimal(d["quantity"]),
                            UpdatedAt = DateTime.Parse(d["updatedAt"]?.ToString())
                        });
                    }
                    dataGrid.ItemsSource = _stock;
                }
                else if (navCounterparties.IsChecked == true)
                {
                    statusText.Text = "Загрузка контрагентов...";
                    var data = await ApiClient.GetCounterpartiesAsync();
                    _counterparties.Clear();
                    foreach (var d in data)
                    {
                        string typeStr = d["type"]?.ToString();
                        if (typeStr == "0" || typeStr == "Client") typeStr = "Покупатель";
                        else if (typeStr == "1" || typeStr == "Supplier") typeStr = "Поставщик";
                        else if (typeStr == "2" || typeStr == "Company") typeStr = "Оба";

                        _counterparties.Add(new Counterparty
                        {
                            Id = Convert.ToInt32(d["id"]),
                            Name = d["name"]?.ToString(),
                            Type = typeStr,
                            INN = d.ContainsKey("inn") && d["inn"] != null ? d["inn"].ToString() : "—",
                            Address = d.ContainsKey("address") && d["address"] != null ? d["address"].ToString() : "—"
                        });
                    }
                    dataGrid.ItemsSource = _counterparties;
                }
                else if (navContacts.IsChecked == true)
                {
                    statusText.Text = "Загрузка контактов...";
                    var data = await ApiClient.GetCounterpartiesAsync();
                    _contacts.Clear();
                    foreach (var cp in data)
                    {
                        int cpId = Convert.ToInt32(cp["id"]);
                        string cpName = cp["name"]?.ToString();
                        if (cp.ContainsKey("contacts") && cp["contacts"] is System.Collections.IEnumerable contactsArr)
                        {
                            foreach (Dictionary<string, object> c in contactsArr)
                            {
                                _contacts.Add(new Contact
                                {
                                    Id = Convert.ToInt32(c["id"]),
                                    CounterpartyId = cpId,
                                    CounterpartyName = cpName,
                                    Name = c["name"]?.ToString(),
                                    Phone = c.ContainsKey("phone") && c["phone"] != null ? c["phone"].ToString() : "—"
                                });
                            }
                        }
                    }
                    dataGrid.ItemsSource = _contacts;
                }
                else if (navOperations.IsChecked == true)
                {
                    statusText.Text = "Загрузка операций...";
                    var data = await ApiClient.GetOperationsAsync();
                    _operations.Clear();
                    foreach (var d in data)
                    {
                        string typeStr = d["type"]?.ToString();
                        if (typeStr == "0" || typeStr == "Income") typeStr = "Приход";
                        else if (typeStr == "1" || typeStr == "Sale") typeStr = "Расход";
                        else if (typeStr == "2" || typeStr == "Transfer") typeStr = "Перемещение";
                        else if (typeStr == "3" || typeStr == "WriteOff") typeStr = "Списание";

                        _operations.Add(new Operation
                        {
                            Id = Convert.ToInt32(d["id"]),
                            Type = typeStr,
                            Date = DateTime.Parse(d["date"]?.ToString()),
                            Comment = d.ContainsKey("comment") && d["comment"] != null ? d["comment"].ToString() : "",
                            CounterpartyId = d.ContainsKey("counterpartyId") && d["counterpartyId"] != null ? Convert.ToInt32(d["counterpartyId"]) : 0,
                            CounterpartyName = d.ContainsKey("counterpartyName") && d["counterpartyName"] != null ? d["counterpartyName"].ToString() : "—"
                        });
                    }
                    dataGrid.ItemsSource = _operations;
                }
                else if (navOperationItems.IsChecked == true)
                {
                    statusText.Text = "Загрузка деталей операций...";
                    var data = await ApiClient.GetOperationsAsync();
                    _operationItems.Clear();
                    foreach (var op in data)
                    {
                        int opId = Convert.ToInt32(op["id"]);
                        if (op.ContainsKey("items") && op["items"] is System.Collections.IEnumerable itemsArr)
                        {
                            foreach (Dictionary<string, object> i in itemsArr)
                            {
                                _operationItems.Add(new OperationItem
                                {
                                    OperationId = opId,
                                    ProductId = Convert.ToInt32(i["productId"]),
                                    ProductName = i["productName"]?.ToString(),
                                    Quantity = Convert.ToDecimal(i["quantity"]),
                                    Price = Convert.ToDecimal(i["price"])
                                });
                            }
                        }
                    }
                    dataGrid.ItemsSource = _operationItems;
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка подключения к API", MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "Ошибка API";
            }
        }

        private void ConfigureDataGrids()
        {
            if (dataGrid == null) return;

            dataGrid.Columns.Clear();

            if (navProducts.IsChecked == true)
            {
                titleText.Text = "Товары";
                filterLabel.Visibility = Visibility.Visible;
                filterCombo.Visibility = Visibility.Visible;
                filterCombo.ItemsSource = new[] { "Все", "шт", "кг", "л", "уп" };
                filterCombo.SelectedIndex = 0;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new System.Windows.Data.Binding("Name"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Артикул", Binding = new System.Windows.Data.Binding("Article"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Ед. изм.", Binding = new System.Windows.Data.Binding("Unit"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new System.Windows.Data.Binding("FormattedPrice"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата создания", Binding = new System.Windows.Data.Binding("FormattedDate"), Width = 130 });
            }
            else if (navStock.IsChecked == true)
            {
                titleText.Text = "Остатки на складе";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID товара", Binding = new System.Windows.Data.Binding("ProductId"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Товар", Binding = new System.Windows.Data.Binding("ProductName"), Width = 250 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Артикул", Binding = new System.Windows.Data.Binding("ProductArticle"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Ед. изм.", Binding = new System.Windows.Data.Binding("Unit"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Количество", Binding = new System.Windows.Data.Binding("QuantityWarning"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата обновления", Binding = new System.Windows.Data.Binding("FormattedDate"), Width = 150 });
            }
            else if (navCounterparties.IsChecked == true)
            {
                titleText.Text = "Контрагенты";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new System.Windows.Data.Binding("Name"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new System.Windows.Data.Binding("Type"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ИНН", Binding = new System.Windows.Data.Binding("INN"), Width = 150 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Адрес", Binding = new System.Windows.Data.Binding("Address"), Width = 300 });
            }
            else if (navContacts.IsChecked == true)
            {
                titleText.Text = "Контакты";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Контрагент", Binding = new System.Windows.Data.Binding("CounterpartyName"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Контактное лицо", Binding = new System.Windows.Data.Binding("Name"), Width = 180 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Телефон", Binding = new System.Windows.Data.Binding("Phone"), Width = 150 });
            }
            else if (navOperations.IsChecked == true)
            {
                titleText.Text = "Операции";
                filterLabel.Visibility = Visibility.Visible;
                filterCombo.Visibility = Visibility.Visible;
                filterCombo.ItemsSource = new[] { "Все", "Приход", "Расход" };
                filterCombo.SelectedIndex = 0;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new System.Windows.Data.Binding("Type"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new System.Windows.Data.Binding("FormattedDate"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Контрагент", Binding = new System.Windows.Data.Binding("CounterpartyName"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Комментарий", Binding = new System.Windows.Data.Binding("Comment"), Width = 250 });
            }
            else if (navOperationItems.IsChecked == true)
            {
                titleText.Text = "Детали операций";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID операции", Binding = new System.Windows.Data.Binding("OperationId"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID товара", Binding = new System.Windows.Data.Binding("ProductId"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Товар", Binding = new System.Windows.Data.Binding("ProductName"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Кол-во", Binding = new System.Windows.Data.Binding("Quantity"), Width = 80 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new System.Windows.Data.Binding("FormattedPrice"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new System.Windows.Data.Binding("FormattedTotal"), Width = 130 });
            }
        }

        private async void Nav_Checked(object sender, RoutedEventArgs e)
        {
            await RefreshActiveTabAsync();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dataGrid == null) return;

            var searchText = searchBox.Text.ToLower();

            if (navProducts.IsChecked == true && _products != null)
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    dataGrid.ItemsSource = _products;
                else
                    dataGrid.ItemsSource = new ObservableCollection<Product>(
                        _products.Where(p => p.Name.ToLower().Contains(searchText) ||
                                            p.Article.ToLower().Contains(searchText)));
            }
            else if (navOperations.IsChecked == true && _operations != null)
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    dataGrid.ItemsSource = _operations;
                else
                    dataGrid.ItemsSource = new ObservableCollection<Operation>(
                        _operations.Where(o => o.Comment.ToLower().Contains(searchText) ||
                                              o.CounterpartyName.ToLower().Contains(searchText)));
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string tableName = "";

            if (navProducts.IsChecked == true)
                tableName = "products";
            else if (navStock.IsChecked == true)
                tableName = "stock";
            else if (navCounterparties.IsChecked == true)
                tableName = "counterparties";
            else if (navContacts.IsChecked == true)
                tableName = "contacts";
            else if (navOperations.IsChecked == true)
                tableName = "operations";
            else if (navOperationItems.IsChecked == true)
                tableName = "operationitems";
            else
                return;

            var dialog = new AddItemDialog(tableName);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var formData = dialog.FormData;

                try
                {
                    statusText.Text = "Добавление записи...";

                    if (tableName == "products")
                    {
                        var product = new Dictionary<string, object>
                        {
                            { "name", formData["name"] },
                            { "article", formData["article"] },
                            { "unit", formData["unit"] },
                            { "price", Convert.ToDecimal(formData["price"]) }
                        };
                        await ApiClient.CreateProductAsync(product);
                    }
                    else if (tableName == "counterparties")
                    {
                        string typeStr = formData["type"]?.ToString();
                        string apiType = "Company";
                        if (typeStr == "Покупатель") apiType = "Client";
                        else if (typeStr == "Поставщик") apiType = "Supplier";

                        var counterparty = new Dictionary<string, object>
                        {
                            { "name", formData["name"] },
                            { "type", apiType },
                            { "inn", formData["inn"] },
                            { "address", formData["address"] },
                            { "contacts", new List<object>() }
                        };
                        await ApiClient.CreateCounterpartyAsync(counterparty);
                    }
                    else if (tableName == "contacts")
                    {
                        int cpId = Convert.ToInt32(formData["counterpartyId"]);
                        // Загружаем контрагента по ID
                        var cp = await ApiClient.GetAsync<Dictionary<string, object>>($"counterparties/{cpId}");

                        var contactsList = new List<object>();
                        if (cp.ContainsKey("contacts") && cp["contacts"] is System.Collections.IEnumerable contactsArr)
                        {
                            foreach (var c in contactsArr) contactsList.Add(c);
                        }

                        contactsList.Add(new Dictionary<string, object>
                        {
                            { "name", formData["name"] },
                            { "phone", formData["phone"] }
                        });

                        // В бэкенде DTO ожидает тип в виде строки или int
                        string cpType = cp["type"]?.ToString();

                        var updatedCp = new Dictionary<string, object>
                        {
                            { "name", cp["name"] },
                            { "type", cpType },
                            { "inn", cp.ContainsKey("inn") ? cp["inn"] : "" },
                            { "address", cp.ContainsKey("address") ? cp["address"] : "" },
                            { "contacts", contactsList }
                        };

                        await ApiClient.UpdateCounterpartyAsync(cpId, updatedCp);
                    }
                    else if (tableName == "operations")
                    {
                        // В AddItemDialog вводится: тип, комментарий, ID контрагента.
                        // Также нужен список товаров (в UI AddItemDialog этого нет, так что отправляем пустой список или один дефолтный)
                        string formTypeStr = formData["type"]?.ToString();
                        string apiType = formTypeStr == "Приход" ? "income" : "sale";

                        int cpId = Convert.ToInt32(formData["counterpartyId"]);

                        var operation = new Dictionary<string, object>
                        {
                            { "counterpartyId", cpId },
                            { "comment", formData["comment"] },
                            { "items", new List<object>() } // создаем пустую шапку операции
                        };

                        await ApiClient.CreateOperationAsync(apiType, operation);
                    }
                    else
                    {
                        MessageBox.Show("Для добавления записей этой таблицы воспользуйтесь связующими разделами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    statusText.Text = "Запись успешно добавлена!";
                    await RefreshActiveTabAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при добавлении", MessageBoxButton.OK, MessageBoxImage.Error);
                    statusText.Text = "Ошибка сохранения";
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dataGrid?.SelectedItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                statusText.Text = "Удаление записи...";

                if (selectedItem is Product product)
                {
                    await ApiClient.DeleteProductAsync(product.Id);
                }
                else if (selectedItem is Counterparty cp)
                {
                    await ApiClient.DeleteCounterpartyAsync(cp.Id);
                }
                else
                {
                    MessageBox.Show("Каскадное удаление поддерживается для основных справочников (Товары, Контрагенты).", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                statusText.Text = "Запись успешно удалена";
                await RefreshActiveTabAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при удалении", MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "Ошибка удаления";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshActiveTabAsync();
        }

        private void UpdateStatusBar()
        {
            if (dataGrid?.ItemsSource != null)
            {
                var count = (dataGrid.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? 0;
                statusText.Text = $"Записей: {count} | Данные актуальны на: {DateTime.Now:HH:mm:ss}";
            }
        }

        // ─── ЛОГИКА ФОРМЫ АНАЛИТИКИ ────────────────────────────────────────────────

        private async void AnalyticsTab_Checked(object sender, RoutedEventArgs e)
        {
            await RefreshAnalyticsAsync();
        }

        private async Task RefreshAnalyticsAsync()
        {
            // Защита от NullReferenceException во время выполнения InitializeComponent()
            if (analyticsPanel == null ||
                tabTopProducts == null ||
                tabTurnover == null ||
                tabLowStock == null ||
                panelTopProducts == null ||
                panelTurnover == null ||
                panelLowStock == null)
            {
                return;
            }

            if (tabTopProducts.IsChecked == true)
            {
                panelTopProducts.Visibility = Visibility.Visible;
                panelTurnover.Visibility = Visibility.Collapsed;
                panelLowStock.Visibility = Visibility.Collapsed;
                await LoadTopProductsAsync();
            }
            else if (tabTurnover.IsChecked == true)
            {
                panelTopProducts.Visibility = Visibility.Collapsed;
                panelTurnover.Visibility = Visibility.Visible;
                panelLowStock.Visibility = Visibility.Collapsed;
                await LoadTurnoverAsync();
            }
            else if (tabLowStock.IsChecked == true)
            {
                panelTopProducts.Visibility = Visibility.Collapsed;
                panelTurnover.Visibility = Visibility.Collapsed;
                panelLowStock.Visibility = Visibility.Visible;
                await LoadLowStockAsync();
            }
        }

        // 1. Загрузка лидеров продаж
        private async Task LoadTopProductsAsync()
        {
            try
            {
                if (dpTopFrom.SelectedDate == null || dpTopTo.SelectedDate == null) return;

                string fromStr = dpTopFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
                string toStr = dpTopTo.SelectedDate.Value.ToString("yyyy-MM-dd");
                int limit = int.TryParse(txtTopLimit.Text, out int lim) ? lim : 10;

                statusText.Text = "Расчет лидеров продаж...";
                var data = await ApiClient.GetTopProductsAsync(fromStr, toStr, limit);

                var list = new List<TopProductReportRow>();
                foreach (var d in data)
                {
                    list.Add(new TopProductReportRow
                    {
                        ProductId = Convert.ToInt32(d["productId"]),
                        ProductName = d["productName"]?.ToString(),
                        ProductArticle = d["productArticle"]?.ToString(),
                        Unit = d["unit"]?.ToString(),
                        TotalQuantity = Convert.ToDecimal(d["totalQuantity"]),
                        TotalAmount = Convert.ToDecimal(d["totalAmount"]),
                        OperationsCount = Convert.ToInt32(d["operationsCount"])
                    });
                }

                dgTopProducts.ItemsSource = list;
                statusText.Text = $"Рассчитано лидеров: {list.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка загрузки отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Загрузка финансовых оборотов
        private async Task LoadTurnoverAsync()
        {
            try
            {
                if (dpTurnoverFrom.SelectedDate == null || dpTurnoverTo.SelectedDate == null) return;

                string fromStr = dpTurnoverFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
                string toStr = dpTurnoverTo.SelectedDate.Value.ToString("yyyy-MM-dd");

                statusText.Text = "Расчет финансовых оборотов...";

                // Получаем TurnoverDto
                var data = await ApiClient.GetAsync<Dictionary<string, object>>($"analytics/turnover?from={fromStr}&to={toStr}");

                decimal income = Convert.ToDecimal(data["incomeAmount"]);
                decimal sales = Convert.ToDecimal(data["saleAmount"]);
                decimal writeoff = Convert.ToDecimal(data["writeOffAmount"]);

                txtTotalIncome.Text = $"{income:N0} ₽";
                txtTotalSales.Text = $"{sales:N0} ₽";
                txtTotalWriteoff.Text = $"{writeoff:N0} ₽";

                var list = new List<TurnoverReportRow>();
                if (data.ContainsKey("byDay") && data["byDay"] is System.Collections.IEnumerable daysArr)
                {
                    foreach (Dictionary<string, object> d in daysArr)
                    {
                        DateTime date = DateTime.Parse(d["date"]?.ToString());
                        list.Add(new TurnoverReportRow
                        {
                            FormattedDate = date.ToString("dd.MM.yyyy"),
                            IncomeAmount = Convert.ToDecimal(d["incomeAmount"]),
                            SaleAmount = Convert.ToDecimal(d["saleAmount"])
                        });
                    }
                }

                dgTurnover.ItemsSource = list;
                statusText.Text = "Обороты успешно рассчитаны!";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка загрузки оборотов", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. Загрузка товаров с дефицитом
        private async Task LoadLowStockAsync()
        {
            try
            {
                decimal minQty = decimal.TryParse(txtMinQty.Text, out decimal q) ? q : 5;

                statusText.Text = "Поиск дефицитных товаров...";
                var data = await ApiClient.GetLowStockAsync(minQty);

                var list = new List<LowStockReportRow>();
                foreach (var d in data)
                {
                    list.Add(new LowStockReportRow
                    {
                        ProductId = Convert.ToInt32(d["productId"]),
                        ProductName = d["productName"]?.ToString(),
                        ProductArticle = d["productArticle"]?.ToString(),
                        CurrentQuantity = Convert.ToDecimal(d["currentQuantity"]),
                        Unit = d["unit"]?.ToString(),
                        MinQuantity = Convert.ToDecimal(d["minQuantity"])
                    });
                }

                dgLowStock.ItemsSource = list;
                statusText.Text = $"Найдено дефицитных товаров: {list.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка поиска дефицита", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnLoadTopProducts_Click(object sender, RoutedEventArgs e)
        {
            await LoadTopProductsAsync();
        }

        private async void BtnLoadTurnover_Click(object sender, RoutedEventArgs e)
        {
            await LoadTurnoverAsync();
        }

        private async void BtnLoadLowStock_Click(object sender, RoutedEventArgs e)
        {
            await LoadLowStockAsync();
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
