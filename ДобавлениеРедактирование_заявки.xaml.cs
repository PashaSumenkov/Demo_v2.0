using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Zadanie10202
{
    /// <summary>
    /// Логика взаимодействия для ДобавлениеРедактирование_заявки.xaml
    /// </summary>
    public partial class ДобавлениеРедактирование_заявки : Window
    {
        Zadanie10202Entities db = new Zadanie10202Entities();
        private Данные_партнёров currentPartner;

        private bool isEditMode;

        private List<ВременнаяПродукция> selectedProducts = new List<ВременнаяПродукция>();
        /// <summary>
        /// Класс для хранения временной продукции
        /// </summary>
        public class ВременнаяПродукция
        {
            /// <summary>
            /// свойства для хранения продукции
            /// </summary>
            public Продукции Продукция { get; set; }
            /// <summary>
            /// Свойчтво для хранения количества продукции
            /// </summary>
            public int Количество { get; set; }
            /// <summary>
            /// Свойство для хранеия стоимости продукции
            /// </summary>
            public decimal Стоимость => Количество * (Продукция?.Минимальная_стоимость_для_партнера ?? 0);
        }
        /// <summary>
        /// конструктор для создания заявки
        /// </summary>
        public ДобавлениеРедактирование_заявки() // конструктор для создания заявки
        {
            InitializeComponent();
            isEditMode = false;
            loadComboTypePartner();
            loadComboProducts();
            infoText.Text = "Предложена вся продукция компании";

            dobProd.Visibility = Visibility.Collapsed;
            prodVZaivki.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// конструктор для редактирования заявки
        /// </summary>
        /// <param name="партнер"></param>
        public ДобавлениеРедактирование_заявки(Данные_партнёров партнер) // конструктор для редактирования заявки
        {
            InitializeComponent();
            currentPartner = партнер;
            isEditMode = true;
            loadComboTypePartner();
            loadComboDataPartner();
            loadComboProductsKotoriyBilZakazan();
            infoText.Text = "Предложена продукция для конкретного партнера";

            loadDataGridData();
            itogoSum.Text = "Итого: " + calculateTotalSum();
        }
        /// <summary>
        /// Загрузка данных в список
        /// </summary>
        private void loadComboTypePartner()
        {
            typeOfPartner.ItemsSource = db.Типы_партнёров.ToList();
            typeOfPartner.DisplayMemberPath = "Тип_партнёров";
            typeOfPartner.SelectedValuePath = "Номер_типа_партнёров";
        }
        /// <summary>
        /// Загрузка всей продукции для нового партнера
        /// </summary>
        private void loadComboProducts() // Загрузка всей продукции для нового партнера
        {
            productsComboBox.ItemsSource = db.Продукции.ToList();
            productsComboBox.SelectedValuePath = "Номер_продукции";
            productsComboBox.DisplayMemberPath = "Наименование_продукции";
        }
        /// <summary>
        /// Загрузка продукции, предлагаемая партнеру, которая уже была в заявке у него
        /// </summary>
        private void loadComboProductsKotoriyBilZakazan() // Загрузка продукции, предлагаемая партнеру, которая уже была в заявке у него
        {
            try
            {
                var productBilZakazaniy = db.Количества_продукции
                    .Where(p => p.Наименование_партнера == currentPartner.Номер_партнера)
                    .Select(p => p.Продукция)
                    .Distinct()
                    .ToList();

                var filterProducts = db.Продукции
                    .Where(p => productBilZakazaniy.Contains(p.Номер_продукции))
                    .ToList();

                productsComboBox.ItemsSource = filterProducts;
                productsComboBox.SelectedValuePath = "Номер_продукции";
                productsComboBox.DisplayMemberPath = "Наименование_продукции";
            }
            catch (Exception exe)
            {
                MessageBox.Show($"Ошибка загрузки{exe.Message}");
            }
        }
        /// <summary>
        /// Загрузка данных ввременной продукции добавленной в заявку
        /// </summary>
        private void loadDataGridData()
        {
            if (!isEditMode) return;

            // Загружаем существующие заявки партнера
            var zaivkiPartnera = db.Количества_продукции
                .Where(z => z.Наименование_партнера == currentPartner.Номер_партнера)
                .Include("Продукции")
                .ToList();

            // Преобразуем в нашу временную коллекцию
            selectedProducts = zaivkiPartnera.Select(z => new ВременнаяПродукция
            {
                Продукция = z.Продукции,
                Количество = int.TryParse(z.Количество_продукции, out int qty) ? qty : 0
            }).ToList();

            UpdateDataGrid();
            UpdateTotalSum();
        }
        /// <summary>
        /// Загрузка итоговой суммы
        /// </summary>
        /// <returns></returns>
        public string calculateTotalSum() // Загрузка итоговой суммы
        {
            try
            {
                var поставки = db.Количества_продукции
                    .Where(k => k.Наименование_партнера == currentPartner.Номер_партнера)
                    .ToList();
                decimal total = 0;
                foreach (var поставка in поставки)
                {
                    var продукция = db.Продукции.FirstOrDefault(p => p.Номер_продукции == поставка.Продукция);
                    if (продукция != null && int.TryParse(поставка.Количество_продукции, out int количество))
                    {
                        total += количество * (продукция.Минимальная_стоимость_для_партнера ?? 0);
                    }
                }
                return $"{total:N2} руб.";
            }
            catch (Exception)
            {
                return "Ошибка расчета";
            }
        }
        /// <summary>
        /// Загрузка данных партнера
        /// </summary>
        private void loadComboDataPartner()
        {
            if (currentPartner == null) return;
            try
            {
                currentPartner = db.Данные_партнёров
                    .Include("Типы_партнёров")
                    .Include("Юридические_адреса")
                    .FirstOrDefault(p => p.Номер_партнера == currentPartner.Номер_партнера);

                if (currentPartner != null)
                {
                    // Основная информация
                    typeOfPartner.SelectedValue = currentPartner.Тип_партнера;
                    namePartner.Text = currentPartner.Наименование_партнера;
                    nameDirector.Text = currentPartner.Директор;
                    reitPart.Text = currentPartner.Рейтинг.ToString();
                    telPart.Text = currentPartner.Телефон_партнера;
                    emailPart.Text = currentPartner.Электронная_почта_партнера;
                    innPart.Text = currentPartner.ИНН;

                    // Адресная информация
                    if (currentPartner.Юридические_адреса != null)
                    {
                        var adress = currentPartner.Юридические_адреса;
                        indexPart.Text = adress.Индекс;
                        oblastPart.Text = adress.Область;
                        cityPart.Text = adress.Город;
                        streetPart.Text = adress.Улица;
                        domPart.Text = adress.Дом.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка загрузки: {e.Message}");
            }
        }
        /// <summary>
        /// Метод для сохранении заявки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(reitPart.Text) && (!int.TryParse(reitPart.Text, out int rating) || rating < 0))
            {
                MessageBox.Show("Рейтинг должен быть целым неотрицательным числом", "Ошибка");
                reitPart.Text = null;
                return;
            }

            if (!string.IsNullOrEmpty(domPart.Text) && (!int.TryParse(domPart.Text, out int domic) || domic < 0))
            {
                MessageBox.Show("Дом должен быть целым неотрицательным числом", "Ошибка");
                domPart.Text = null;
                return;
            }

            if (!string.IsNullOrEmpty(indexPart.Text) && ((!long.TryParse(indexPart.Text, out long index)) || indexPart.Text.Length != 6))
            {
                MessageBox.Show("Индекс это комбинация из 6 чисел", "Ошибка");
                indexPart.Text = null;
                return;
            }

            if (!string.IsNullOrEmpty(innPart.Text) && ((!long.TryParse(innPart.Text, out long inn)) || innPart.Text.Length != 10))
            {
                MessageBox.Show("ИНН это комбинация из 10 чисел", "Ошибка");
                innPart.Text = null;
                return;
            }

            if (!string.IsNullOrEmpty(telPart.Text) && ((!long.TryParse(telPart.Text, out long tel)) || telPart.Text.Length != 11))
            {
                MessageBox.Show("Номер телефона это комбинация из 11 чисел", "Ошибка");
                telPart.Text = null;
                return;
            }

            try
            {
                if (isEditMode)
                {
                    updatePartner();
                    SaveProductsToDatabase(currentPartner.Номер_партнера);
                    db.SaveChanges();
                    MessageBox.Show("Заявка успешно отредактирована!");
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    if (currentPartner == null)
                    {
                        createPartner();
                        return;
                    }
                    else
                    {
                        SaveProductsToDatabase(currentPartner.Номер_партнера);
                        db.SaveChanges();
                        MessageBox.Show("Заявка успешно создана!");
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }
        /// <summary>
        /// Метод для сохранения продукции в бд
        /// </summary>
        /// <param name="partnerId"></param>
        private void SaveProductsToDatabase(int partnerId)
        {
            if (selectedProducts.Count == 0) return;

            foreach (var продукт in selectedProducts)
            {
                var новаяЗапись = new Количества_продукции
                {
                    Наименование_партнера = partnerId,
                    Продукция = продукт.Продукция.Номер_продукции,
                    Количество_продукции = продукт.Количество.ToString()
                };

                db.Количества_продукции.Add(новаяЗапись);
            }
        }
        /// <summary>
        /// метод для обновленния партнера
        /// </summary>
        private void updatePartner()
        {
            if (currentPartner == null) return;
            currentPartner.Тип_партнера = (int)typeOfPartner.SelectedValue;
            currentPartner.Наименование_партнера = namePartner.Text;
            currentPartner.Директор = nameDirector.Text;
            currentPartner.Рейтинг = int.Parse(reitPart.Text);
            currentPartner.Телефон_партнера = telPart.Text;
            currentPartner.Электронная_почта_партнера = emailPart.Text;
            currentPartner.ИНН = innPart.Text;

            if (currentPartner.Юридические_адреса != null)
            {
                currentPartner.Юридические_адреса.Индекс = indexPart.Text;
                currentPartner.Юридические_адреса.Область = oblastPart.Text;
                currentPartner.Юридические_адреса.Город = cityPart.Text;
                currentPartner.Юридические_адреса.Улица = streetPart.Text;
                currentPartner.Юридические_адреса.Дом = int.Parse(domPart.Text);
            }
        }
        /// <summary>
        /// Метод для создания партнера
        /// </summary>
        private void createPartner()
        {
            // Создаем новый адрес
            var newAdress = new Юридические_адреса
            {
                Индекс = indexPart.Text,
                Область = oblastPart.Text,
                Город = cityPart.Text,
                Улица = streetPart.Text,
                Дом = int.Parse(domPart.Text)
            };
            db.Юридические_адреса.Add(newAdress);
            db.SaveChanges();

            // Создаем нового партера
            var newParner = new Данные_партнёров
            {
                Тип_партнера = (int)typeOfPartner.SelectedValue,
                Наименование_партнера = namePartner.Text,
                Директор = nameDirector.Text,
                Юридический_адрес_партнера = newAdress.Номер_юридического_адреса,
                Рейтинг = int.Parse(reitPart.Text),
                Телефон_партнера = telPart.Text,
                Электронная_почта_партнера = emailPart.Text,
                ИНН = innPart.Text
            };
            db.Данные_партнёров.Add(newParner);
            db.SaveChanges();
            currentPartner = newParner;
            dobProd.Visibility = Visibility.Visible;
            prodVZaivki.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Метод для открытия главного окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelRequest_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Метод для добавления новой продукции в dataGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addProduct_Click(object sender, RoutedEventArgs e)
        {
            if (currentPartner == null)
            {
                MessageBox.Show("Сначала создайте партнера");
                return;
            }

            var selectedProduct = productsComboBox.SelectedItem as Продукции;
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите продукт");
                return;
            }

            if (!int.TryParse(quantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество");
                quantityTextBox.Text = "1";
                return;
            }

            try
            {
                var новаяПродукция = new ВременнаяПродукция
                {
                    Продукция = selectedProduct,
                    Количество = quantity
                };

                selectedProducts.Add(новаяПродукция);
                UpdateDataGrid();

                UpdateTotalSum();

                quantityTextBox.Text = "1";
                productsComboBox.SelectedIndex = -1;

                MessageBox.Show("Продукция добавлена в заявку!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении продукции: {ex.Message}");
            }
        }
        /// <summary>
        /// Метод для обнавления dataGrid
        /// </summary>
        private void UpdateDataGrid()
        {
            selectedProductsDataGrid.ItemsSource = null;
            selectedProductsDataGrid.ItemsSource = selectedProducts;
        }
        /// <summary>
        /// Метод для добавления суммы заявки
        /// </summary>
        private void UpdateTotalSum()
        {
            decimal total = selectedProducts.Sum(p => p.Стоимость);
            itogoSum.Text = $"ИТОГО: {total:N2} руб.";
        }
    }
}
