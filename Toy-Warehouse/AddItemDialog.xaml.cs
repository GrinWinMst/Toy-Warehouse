using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WarehouseApp
{
    public partial class AddItemDialog : Window
    {
        private string _tableName;
        private Dictionary<string, object> _formData;
        private Dictionary<Control, string> _fieldNames;

        public Dictionary<string, object> FormData => _formData;

        public AddItemDialog(string tableName)
        {
            InitializeComponent();
            _tableName = tableName;
            _formData = new Dictionary<string, object>();
            _fieldNames = new Dictionary<Control, string>();
            BuildForm();
        }

        private void BuildForm()
        {
            TitleText.Text = "➕ Добавление " + GetRussianName();

            switch (_tableName)
            {
                case "products":
                    AddProductFields();
                    break;
                case "stock":
                    AddStockFields();
                    break;
                case "counterparties":
                    AddCounterpartyFields();
                    break;
                case "contacts":
                    AddContactFields();
                    break;
                case "operations":
                    AddOperationFields();
                    break;
                case "operationitems":
                    AddOperationItemFields();
                    break;
            }
        }

        private string GetRussianName()
        {
            switch (_tableName)
            {
                case "products": return "товара";
                case "stock": return "остатка";
                case "counterparties": return "контрагента";
                case "contacts": return "контакта";
                case "operations": return "операции";
                case "operationitems": return "детали операции";
                default: return "записи";
            }
        }

        private void AddProductFields()
        {
            AddField("Название", "name", "Введите название", FieldType.TextBox);
            AddField("Артикул", "article", "Введите артикул", FieldType.TextBox);
            AddComboBox("Ед. изм.", "unit", new[] { "шт", "кг", "л", "уп", "м" });
            AddField("Цена", "price", "Введите цену", FieldType.Numeric);
            AddField("Дата создания", "createdAt", "", FieldType.Date);
        }

        private void AddStockFields()
        {
            AddField("ID товара", "productId", "Введите ID", FieldType.Numeric);
            AddField("Количество", "quantity", "Введите кол-во", FieldType.Numeric);
            AddField("Дата обновления", "updatedAt", "", FieldType.Date);
        }

        private void AddCounterpartyFields()
        {
            AddField("Название", "name", "Введите название", FieldType.TextBox);
            AddComboBox("Тип", "type", new[] { "Поставщик", "Покупатель", "Оба" });
            AddField("ИНН", "inn", "Введите ИНН", FieldType.TextBox);
            AddField("Адрес", "address", "Введите адрес", FieldType.TextBox);
        }

        private void AddContactFields()
        {
            AddField("ID контрагента", "counterpartyId", "Введите ID", FieldType.Numeric);
            AddField("Контактное лицо", "name", "Введите ФИО", FieldType.TextBox);
            AddField("Телефон", "phone", "Введите телефон", FieldType.TextBox);
        }

        private void AddOperationFields()
        {
            AddComboBox("Тип операции", "type", new[] { "Приход", "Расход" });
            AddField("Дата", "date", "", FieldType.Date);
            AddField("Комментарий", "comment", "Введите комментарий", FieldType.TextBox);
            AddField("ID контрагента", "counterpartyId", "Введите ID", FieldType.Numeric);
        }

        private void AddOperationItemFields()
        {
            AddField("ID операции", "operationId", "Введите ID", FieldType.Numeric);
            AddField("ID товара", "productId", "Введите ID", FieldType.Numeric);
            AddField("Количество", "quantity", "Введите кол-во", FieldType.Numeric);
            AddField("Цена", "price", "Введите цену", FieldType.Numeric);
        }

        private enum FieldType { TextBox, Numeric, ComboBox, Date }

        // Вспомогательный метод для конвертации цвета
        private Brush GetBrush(string colorHex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        private void AddField(string label, string fieldName, string placeholder, FieldType fieldType)
        {

            var border = new Border
            {
                Background = GetBrush("#161B22"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Width = 470
            };

            var stackPanel = new StackPanel();

            // Label
            var labelControl = new TextBlock
            {
                Text = label,
                Style = (Style)FindResource("FormLabel"),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(labelControl);

            // Control
            FrameworkElement control = null;

            switch (fieldType)
            {
                case FieldType.TextBox:
                    var textBox = new TextBox
                    {
                        Style = (Style)FindResource("FormTextBox"),
                        Text = placeholder
                    };
                    textBox.GotFocus += (s, e) => {
                        if (textBox.Text == placeholder) textBox.Text = "";
                    };
                    textBox.LostFocus += (s, e) => {
                        if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = placeholder;
                    };
                    control = textBox;
                    _fieldNames[textBox] = fieldName;
                    break;

                case FieldType.Numeric:
                    var numericBox = new TextBox
                    {
                        Style = (Style)FindResource("FormTextBox"),
                        Text = placeholder
                    };
                    numericBox.PreviewTextInput += NumericBox_PreviewTextInput;
                    numericBox.GotFocus += (s, e) => {
                        if (numericBox.Text == placeholder) numericBox.Text = "";
                    };
                    numericBox.LostFocus += (s, e) => {
                        if (string.IsNullOrWhiteSpace(numericBox.Text)) numericBox.Text = placeholder;
                    };
                    control = numericBox;
                    _fieldNames[numericBox] = fieldName;
                    break;

                case FieldType.Date:
                    var datePicker = new DatePicker
                    {
                        Style = (Style)FindResource("FormDatePicker"),
                        SelectedDate = DateTime.Now
                    };
                    control = datePicker;
                    _fieldNames[datePicker] = fieldName;
                    break;
            }

            if (control != null)
            {
                control.Margin = new Thickness(0);
                stackPanel.Children.Add(control);
                border.Child = stackPanel;
                FormFields.Children.Add(border);
            }
        }

        private void AddComboBox(string label, string fieldName, string[] items)
        {
            var border = new Border
            {
                Background = GetBrush("#161B22"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Width = 470
            };

            var stackPanel = new StackPanel();

            // Label
            var labelControl = new TextBlock
            {
                Text = label,
                Style = (Style)FindResource("FormLabel"),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(labelControl);

            // ComboBox
            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("FormComboBox"),
                ItemsSource = items,
                SelectedIndex = 0
            };
            stackPanel.Children.Add(comboBox);
            _fieldNames[comboBox] = fieldName;
            border.Child = stackPanel;
            FormFields.Children.Add(border);
        }

        private void NumericBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _formData.Clear();

            foreach (var kvp in _fieldNames)
            {
                var control = kvp.Key;
                string fieldName = kvp.Value;

                if (control is TextBox textBox)
                {
                    string value = textBox.Text;
                    
                    if (value == "Введите название" ||
                        value == "Введите артикул" ||
                        value == "Введите цену" ||
                        value == "Введите ID" ||
                        value == "Введите кол-во" ||
                        value == "Введите ИНН" ||
                        value == "Введите адрес" ||
                        value == "Введите ФИО" ||
                        value == "Введите телефон" ||
                        value == "Введите комментарий")
                    {
                        _formData[fieldName] = "";
                    }
                    else
                    {
                        _formData[fieldName] = value;
                    }
                }
                else if (control is ComboBox comboBox)
                {
                    _formData[fieldName] = comboBox.SelectedItem?.ToString() ?? "";
                }
                else if (control is DatePicker datePicker)
                {
                    _formData[fieldName] = datePicker.SelectedDate ?? DateTime.Now;
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}