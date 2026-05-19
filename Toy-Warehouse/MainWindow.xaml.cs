using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WarehouseApp
{
    // Временые модели данных
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Article { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FormattedPrice => $"{Price:N0} ₽";
        public string FormattedDate => CreatedAt.ToString("dd.MM.yyyy");
    }

    public class Stock
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FormattedDate => UpdatedAt.ToString("dd.MM.yyyy HH:mm");
        public string QuantityWarning => Quantity < 10 ? $"⚠️ {Quantity}" : Quantity.ToString();
    }

    public class Counterparty
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
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
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
        public int CounterpartyId { get; set; }
        public string CounterpartyName { get; set; }
        public string FormattedDate => Date.ToString("dd.MM.yyyy");
    }

    public class OperationItem
    {
        public int Id { get; set; }
        public int OperationId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
        public string FormattedPrice => $"{Price:N0} ₽";
        public string FormattedTotal => $"{Total:N0} ₽";
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<Product> _products;
        private ObservableCollection<Stock> _stock;
        private ObservableCollection<Counterparty> _counterparties;
        private ObservableCollection<Contact> _contacts;
        private ObservableCollection<Operation> _operations;
        private ObservableCollection<OperationItem> _operationItems;

        public MainWindow()
        {
            InitializeComponent();

           
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            InitializeSampleData();
            ConfigureDataGrids();
            UpdateStatusBar();
        }

        private void InitializeSampleData()
        {
            _products = new ObservableCollection<Product>
            {
                new Product { Id = 1, Name = "Ноутбук Lenovo", Article = "LNV-001", Unit = "шт", Price = 45000, CreatedAt = DateTime.Now.AddDays(-30) },
                new Product { Id = 2, Name = "Мышь беспроводная", Article = "MOU-002", Unit = "шт", Price = 1200, CreatedAt = DateTime.Now.AddDays(-20) },
                new Product { Id = 3, Name = "Клавиатура механическая", Article = "KEY-003", Unit = "шт", Price = 3500, CreatedAt = DateTime.Now.AddDays(-15) },
                new Product { Id = 4, Name = "Монитор 24''", Article = "MON-004", Unit = "шт", Price = 18500, CreatedAt = DateTime.Now.AddDays(-10) },
                new Product { Id = 5, Name = "SSD диск 1TB", Article = "SSD-005", Unit = "шт", Price = 8900, CreatedAt = DateTime.Now.AddDays(-5) }
            };

            _stock = new ObservableCollection<Stock>
            {
                new Stock { Id = 1, ProductId = 1, ProductName = "Ноутбук Lenovo", Quantity = 12, UpdatedAt = DateTime.Now },
                new Stock { Id = 2, ProductId = 2, ProductName = "Мышь беспроводная", Quantity = 45, UpdatedAt = DateTime.Now },
                new Stock { Id = 3, ProductId = 3, ProductName = "Клавиатура механическая", Quantity = 8, UpdatedAt = DateTime.Now },
                new Stock { Id = 4, ProductId = 4, ProductName = "Монитор 24''", Quantity = 3, UpdatedAt = DateTime.Now },
                new Stock { Id = 5, ProductId = 5, ProductName = "SSD диск 1TB", Quantity = 0, UpdatedAt = DateTime.Now }
            };

            _counterparties = new ObservableCollection<Counterparty>
            {
                new Counterparty { Id = 1, Name = "ООО ТехноПоставка", Type = "Поставщик", INN = "7701234567", Address = "г. Москва, ул. Складская, 15" },
                new Counterparty { Id = 2, Name = "ИП Иванов А.А.", Type = "Покупатель", INN = "7723456789", Address = "г. Москва, ул. Торговая, 8" },
                new Counterparty { Id = 3, Name = "ЗАО Электроника", Type = "Оба", INN = "7734567890", Address = "г. Санкт-Петербург, Невский пр., 100" }
            };

            _contacts = new ObservableCollection<Contact>
            {
                new Contact { Id = 1, CounterpartyId = 1, CounterpartyName = "ООО ТехноПоставка", Name = "Петров Иван", Phone = "+7 (495) 123-45-67" },
                new Contact { Id = 2, CounterpartyId = 1, CounterpartyName = "ООО ТехноПоставка", Name = "Сидорова Мария", Phone = "+7 (495) 765-43-21" },
                new Contact { Id = 3, CounterpartyId = 2, CounterpartyName = "ИП Иванов А.А.", Name = "Иванов Алексей", Phone = "+7 (916) 111-22-33" },
                new Contact { Id = 4, CounterpartyId = 3, CounterpartyName = "ЗАО Электроника", Name = "Смирнов Дмитрий", Phone = "+7 (812) 444-55-66" }
            };

            _operations = new ObservableCollection<Operation>
            {
                new Operation { Id = 1, Type = "Приход", Date = DateTime.Now.AddDays(-5), Comment = "Поставка товаров", CounterpartyId = 1, CounterpartyName = "ООО ТехноПоставка" },
                new Operation { Id = 2, Type = "Расход", Date = DateTime.Now.AddDays(-3), Comment = "Продажа клиенту", CounterpartyId = 2, CounterpartyName = "ИП Иванов А.А." },
                new Operation { Id = 3, Type = "Приход", Date = DateTime.Now.AddDays(-1), Comment = "Дополнительная поставка", CounterpartyId = 1, CounterpartyName = "ООО ТехноПоставка" }
            };

            _operationItems = new ObservableCollection<OperationItem>
            {
                new OperationItem { Id = 1, OperationId = 1, ProductId = 1, ProductName = "Ноутбук Lenovo", Quantity = 5, Price = 45000 },
                new OperationItem { Id = 2, OperationId = 1, ProductId = 2, ProductName = "Мышь беспроводная", Quantity = 20, Price = 1200 },
                new OperationItem { Id = 3, OperationId = 2, ProductId = 3, ProductName = "Клавиатура механическая", Quantity = 2, Price = 3500 },
                new OperationItem { Id = 4, OperationId = 3, ProductId = 5, ProductName = "SSD диск 1TB", Quantity = 10, Price = 8900 }
            };
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
                dataGrid.ItemsSource = _products;
            }
            else if (navStock.IsChecked == true)
            {
                titleText.Text = "Остатки на складе";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Товар", Binding = new System.Windows.Data.Binding("ProductName"), Width = 250 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Количество", Binding = new System.Windows.Data.Binding("QuantityWarning"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата обновления", Binding = new System.Windows.Data.Binding("FormattedDate"), Width = 150 });
                dataGrid.ItemsSource = _stock;
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
                dataGrid.ItemsSource = _counterparties;
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
                dataGrid.ItemsSource = _contacts;
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
                dataGrid.ItemsSource = _operations;
            }
            else if (navOperationItems.IsChecked == true)
            {
                titleText.Text = "Детали операций";
                filterLabel.Visibility = Visibility.Collapsed;
                filterCombo.Visibility = Visibility.Collapsed;

                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID операции", Binding = new System.Windows.Data.Binding("OperationId"), Width = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Товар", Binding = new System.Windows.Data.Binding("ProductName"), Width = 200 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Кол-во", Binding = new System.Windows.Data.Binding("Quantity"), Width = 80 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Цена", Binding = new System.Windows.Data.Binding("FormattedPrice"), Width = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Сумма", Binding = new System.Windows.Data.Binding("FormattedTotal"), Width = 130 });
                dataGrid.ItemsSource = _operationItems;
            }
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            ConfigureDataGrids();
            UpdateStatusBar();
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

                // Формируем сообщение с введёнными данными
                string message = "Введённые данные:\n\n";
                foreach (var item in formData)
                {
                    message += $"{item.Key}: {item.Value}\n";
                }

                MessageBox.Show(message, "Новая запись", MessageBoxButton.OK, MessageBoxImage.Information);
                statusText.Text = $"Добавлена новая запись в {tableName}";

                // Здесь будет вызов API после его подключения
                // await _apiService.CreateXXX(...);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid?.SelectedItem != null)
            {
                var result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    statusText.Text = "Запись удалена";
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigureDataGrids();
            statusText.Text = $"Данные обновлены: {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateStatusBar()
        {
            if (dataGrid?.ItemsSource != null)
            {
                var count = (dataGrid.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? 0;
                statusText.Text = $"Записей: {count} | Готово";
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}