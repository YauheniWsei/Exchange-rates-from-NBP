using System.Globalization;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace NBPwpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CurrencyComboBox.ItemsSource = new List<string> { "USD", "EUR", "CHF", "GBP" };
            CurrencyComboBox.SelectedIndex = 0;

            DatePicker.SelectedDate = DateTime.Today;
            DatePickerNr2.SelectedDate = DateTime.Today;
        }

        private async void GetButton_Click(object sender, RoutedEventArgs e)
        {
            string currencyCode = CurrencyComboBox.SelectedItem.ToString();
            DateTime startDate = DatePicker.SelectedDate ?? DateTime.Today;
            DateTime endDate = DatePickerNr2.SelectedDate ?? DateTime.Today;

            if (startDate > endDate)
            {
                MessageBox.Show("The start date cannot be later than the end date.");
                return;
            }

            List<string> fileList = await System.Threading.Tasks.Task.Run(() => DataHelper.GetFileList(startDate, endDate));

            if (fileList.Count == 0)
            {
                MessageBox.Show("There is no data available for this period.");
                return;
            }

            List<CurrencyRate> currencyRates = await System.Threading.Tasks.Task.Run(() => DataHelper.GetCurrencyRates(fileList, currencyCode, startDate, endDate));

            if (currencyRates.Count == 0)
            {
                MessageBox.Show("No data for the specified currency for the specified period.");
                return;
            }

            foreach (var rate in currencyRates)
            {
                rate.AverageRate = (rate.BuyRate + rate.SellRate) / 2;
            }

            decimal averageOfAverageRates = currencyRates.Average(cr => cr.AverageRate);

            decimal sumOfSquaredDifferences = currencyRates.Sum(cr => (cr.AverageRate - averageOfAverageRates) * (cr.AverageRate - averageOfAverageRates));

            decimal standardDeviation = (decimal)Math.Sqrt((double)(sumOfSquaredDifferences / currencyRates.Count));
            Odch.Text = $"Odchylenie standardowe: {standardDeviation}";

            decimal overallAverageRate = currencyRates.Average(cr => cr.AverageRate);
            InfoTextBlock.Text = $"Srednia: {overallAverageRate}";

            decimal minAverageRate = currencyRates.Min(cr => cr.AverageRate);
            decimal maxAverageRate = currencyRates.Max(cr => cr.AverageRate);

            Min.Text = $"Min śr: {minAverageRate}";
            Max.Text = $"Max śr: {maxAverageRate}";

            DataGrid.ItemsSource = currencyRates;
        }
    }

}