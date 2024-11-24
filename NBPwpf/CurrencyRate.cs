using NBPwpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NBPwpf
{
    public class CurrencyRate
    {
        public DateTime Date { get; set; }
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public decimal AverageRate { get; set; }
        public decimal StandardDeviation { get; set; }
    }

    public static class DataHelper
    {
        public static List<string> GetFileList(DateTime startDate, DateTime endDate)
        {
            List<string> fileList = new List<string>();
            int startYear = startDate.Year;
            int endYear = endDate.Year;

            for (int year = startYear; year <= endYear; year++)
            {
                string dirUrl = $"https://static.nbp.pl/dane/kursy/xml/dir{year}.txt";
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string fileContent = client.DownloadString(dirUrl);
                        var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (IsValidFile(line, startDate, endDate))
                            {
                                fileList.Add(line);
                            }
                        }
                    }
                }
                catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"not found: {dirUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while accessing directory {dirUrl}: {ex.Message}");
                }
            }

            try
            {
                string currentYearDirUrl = "https://static.nbp.pl/dane/kursy/xml/dir.txt";
                using (WebClient client = new WebClient())
                {
                    string fileContent = client.DownloadString(currentYearDirUrl);
                    var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (IsValidFile(line, startDate, endDate))
                        {
                            fileList.Add(line);
                        }
                    }
                }
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return fileList;
        }

        private static bool IsValidFile(string fileName, DateTime startDate, DateTime endDate)
        {
            if (fileName.Length < 11) return false;

            string datePart = fileName.Substring(5, 6);
            if (DateTime.TryParseExact(datePart, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
            {
                return fileDate >= startDate && fileDate <= endDate;
            }

            return false;
        }

        public static List<CurrencyRate> GetCurrencyRates(List<string> fileList, string currencyCode, DateTime startDate, DateTime endDate)
        {
            List<CurrencyRate> currencyRates = new List<CurrencyRate>();

            foreach (var fileName in fileList)
            {
                string fileUrl = $"https://static.nbp.pl/dane/kursy/xml/{fileName}.xml";
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string xmlContent = client.DownloadString(fileUrl);
                        XDocument doc = XDocument.Parse(xmlContent);

                        var rates = doc.Descendants("pozycja")
                                       .Where(p => (string)p.Element("kod_waluty") == currencyCode)
                                       .Select(p =>
                                       {
                                           var kursKupnaElement = p.Element("kurs_kupna");
                                           var kursSprzedazyElement = p.Element("kurs_sprzedazy");
                                           if (kursKupnaElement == null || kursSprzedazyElement == null)
                                           {
                                               return null;
                                           }

                                           return new CurrencyRate
                                           {
                                               Date = DateTime.ParseExact(fileName.Substring(5, 6), "yyMMdd", CultureInfo.InvariantCulture),
                                               BuyRate = decimal.Parse(kursKupnaElement.Value),
                                               SellRate = decimal.Parse(kursSprzedazyElement.Value)
                                           };
                                       })
                                       .Where(r => r != null)
                                       .ToList();

                        currencyRates.AddRange(rates);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while downloading or parsing file {fileUrl}: {ex.Message}");
                }

            }

            return currencyRates;
        }

        public static void CalculateAndDisplayStatistics(List<CurrencyRate> currencyRates)
        {
            var buyRates = currencyRates.Select(cr => cr.BuyRate).ToList();
            var sellRates = currencyRates.Select(cr => cr.SellRate).ToList();

            DisplayStatistics("Kupno", buyRates);
            DisplayStatistics("Sprzedaż", sellRates);

            var maxDifference = currencyRates.MaxBy(cr => cr.SellRate - cr.BuyRate);
            Console.WriteLine($"Największa różnica kursowa: {maxDifference.SellRate - maxDifference.BuyRate} dnia {maxDifference.Date:yyyy-MM-dd}");
        }

        private static void DisplayStatistics(string rateType, List<decimal> rates)
        {
            decimal averageRate = rates.Average();
            decimal standardDeviation = (decimal)Math.Sqrt((double)rates.Average(r => Math.Pow((double)(r - averageRate), 2)));
            decimal minRate = rates.Min();
            decimal maxRate = rates.Max();

            Console.WriteLine($"Statystyki dla {rateType}:");
            Console.WriteLine($"Średni kurs: {averageRate}");
            Console.WriteLine($"Odchylenie standardowe: {standardDeviation}");
            Console.WriteLine($"Kurs minimalny: {minRate}");
            Console.WriteLine($"Kurs maksymalny: {maxRate}");
        }
    }
}
