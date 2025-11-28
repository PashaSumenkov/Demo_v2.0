using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для Расчет_материла.xaml
    /// </summary>
    public partial class Расчет_материла : Window
    {
        Zadanie10202Entities db = new Zadanie10202Entities();
        /// <summary>
        /// Главный класс формы Расчет_материла
        /// </summary>
        public Расчет_материла()
        {
            InitializeComponent();
            loadComboboxData();
        }
        /// <summary>
        /// Загрузка выпадающих списков
        /// </summary>
        private void loadComboboxData()
        {
            try
            {
                productType.ItemsSource = db.Типы_продукции.ToList();
                productType.SelectedValuePath = "Номер_типа_продукции";
                productType.DisplayMemberPath = "Тип_продукции";

                materialType.ItemsSource = db.Типы_материалов.ToList();
                materialType.SelectedValuePath = "Номер_типа_материала";
                materialType.DisplayMemberPath = "Тип_материала";
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        /// <summary>
        /// Расчет количества материала
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calculateBut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!validateInput()) return; 
                int productTypeId = (int)productType.SelectedValue;
                int materialTypeId = (int)materialType.SelectedValue;
                int requiredQuantityy = int.Parse(requiredQuantity.Text);
                int stockQuantityy = int.Parse(stockQuantity.Text);
                double parameter1 = double.Parse(param1.Text);
                double parameter2 = double.Parse(param2.Text);

                int result = calculateMaterialQuantity(
                    productTypeId, materialTypeId, requiredQuantityy,
                    stockQuantityy, parameter1, parameter2);

                DisplayResult(result, productTypeId, materialTypeId);
            }
            catch (Exception exу)
            {
                MessageBox.Show($"Ошибка при расчете: {exу.Message}");
            }
        }
        /// <summary>
        /// Метод валидации данных
        /// </summary>
        /// <returns></returns>
        private bool validateInput()
        {
            if (productType.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип продукции");
                return false;
            }
            if (materialType.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип материала");
                return false;
            }
            if (!int.TryParse(requiredQuantity.Text, out int reqQty) || reqQty <= 0)
            {
                MessageBox.Show("Требуемое количество должно быть положительным целым числом");
                requiredQuantity.Focus();
                return false;
            }
            if (!int.TryParse(stockQuantity.Text, out int stockQty) || stockQty < 0)
            {
                MessageBox.Show("Количество на складе должно быть неотрицательным целым числом");
                stockQuantity.Focus();
                return false;
            }
            if (!double.TryParse(param1.Text, out double parama1) || parama1 <= 0)
            {
                MessageBox.Show("Параметр 1 должен быть положительным числом");
                param1.Focus();
                return false;
            }
            if (!double.TryParse(param2.Text, out double parama2) || parama2 <= 0)
            {
                MessageBox.Show("Параметр 2 должен быть положительным числом");
                param2.Focus();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Переход на главную форму с заявками
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeBut_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Метод для вывода текста результата, смотря на результат
        /// </summary>
        /// <param name="result"></param>
        /// <param name="productTypeId"></param>
        /// <param name="materialTypeId"></param>
        private void DisplayResult(int result, int productTypeId, int materialTypeId)
        {
            if (result == -1)
            {
                resultM.Text = "Ошибка: Проверьте корректность введенных данных.\n" +
                               "Убедитесь, что выбранные типы продукции и материалов существуют в базе данных.";
                resultM.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (result == 0)
            {
                resultM.Text = "Результат: Дополнительный материал не требуется.\n" +
                               "Количество продукции на складе полностью покрывает требуемый объем.";
                resultM.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                var productType = db.Типы_продукции.First(p => p.Номер_типа_продукции == productTypeId);
                var materialType = db.Типы_материалов.First(m => m.Номер_типа_материала == materialTypeId);

                resultM.Text = $"РАСЧЕТ ЗАВЕРШЕН:\n\n" +
                               $"Тип продукции: {productType.Тип_продукции}\n" +
                               $"Тип материала: {materialType.Тип_материала}\n" +
                               $"Процент брака материала: {materialType.Процент_брака_материала * 100:F1}%\n" +
                               $"Коэффициент типа продукции: {productType.Коэффициент_типа_продукции:F2}\n\n" +
                               $"НЕОБХОДИМОЕ КОЛИЧЕСТВО МАТЕРИАЛА: {result} единиц";
                resultM.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }
        /// <summary>
        /// Метод для расчета количества материала
        /// </summary>
        /// <param name="productTypeId"></param>
        /// <param name="materialTypeId"></param>
        /// <param name="requiredQuantity"></param>
        /// <param name="stockQuantity"></param>
        /// <param name="parameter1"></param>
        /// <param name="parameter2"></param>
        /// <returns></returns>
        private int calculateMaterialQuantity(int productTypeId, int materialTypeId,int requiredQuantity, int stockQuantity,double parameter1, double parameter2)
        {
            try
            {
                var productType = db.Типы_продукции.FirstOrDefault(p => p.Номер_типа_продукции == productTypeId);
                var materialType = db.Типы_материалов.FirstOrDefault(m => m.Номер_типа_материала == materialTypeId);

                if (productType == null || materialType == null)
                    return -1;

                int actualProductionQuantity = requiredQuantity - stockQuantity;
                if (actualProductionQuantity <= 0)
                    return 0;

                double productCoefficient = productType.Коэффициент_типа_продукции ?? 1.0;
                double defectPercentage = materialType.Процент_брака_материала ?? 0.0;

                double materialPerUnit = parameter1 * parameter2 * productCoefficient;

                double totalMaterialWithoutDefect = materialPerUnit * actualProductionQuantity;

                double totalMaterialWithDefect = totalMaterialWithoutDefect / (1 - defectPercentage);

                return (int)Math.Ceiling(totalMaterialWithDefect);
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
