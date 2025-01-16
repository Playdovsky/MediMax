using LiveCharts.Wpf;
using LiveCharts;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Main
{

    public partial class StatystykaWindow : Window
    {
        public StatystykaWindow()
        {
            InitializeComponent();
            ReceptyStatystyka();
            PodsumowanieLekow();
            DaneWykresu();
            LoadLekComboBox();
            NajczesciejSprzedawaneLeki();
        }
        private void NajczesciejSprzedawaneLeki()
        {
            using (var context = new MediMaxEntities())
            {
                var sprzedaneLeki = context.tbl_Sprzedaz
                    .GroupBy(r => r.IdLekuSprzedanego)
                    .Select(group => new
                    {
                        IdLekuSprzedanego = group.Key,
                        IloscSprzedazy = group.Count()
                    })
                    .OrderByDescending(lek => lek.IloscSprzedazy)
                    .Take(5)
                    .ToList();

                var statystyki = sprzedaneLeki
                    .Join(context.tbl_Leki.AsEnumerable(),
                          sprzedaz => sprzedaz.IdLekuSprzedanego,
                          lek => lek.Id,                        
                          (sprzedaz, lek) => new
                          {
                              NazwaLeku = lek.Nazwa,
                              IloscSprzedazy = sprzedaz.IloscSprzedazy
                          })
                    .ToList();

                NajczesciejSprzedawaneLekiListBox.Items.Clear();
                foreach (var item in statystyki)
                {
                    NajczesciejSprzedawaneLekiListBox.Items.Add($"{item.NazwaLeku} - {item.IloscSprzedazy} razy");
                }
            }
        }


        private void LoadLekComboBox()
        {
            using (var context = new MediMaxEntities()) 
            {
               
                var leki = context.tbl_Leki
                    .Select(lek => lek.Nazwa)
                    .ToList();

                LekComboBox.ItemsSource = leki;
            }
        }

        private void LekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string wybranyLek = LekComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(wybranyLek))
            {
                DateTime? startDate = StartDatePicker?.SelectedDate;
                DateTime? endDate = EndDatePicker?.SelectedDate;

                var statystyki = GetStatystykiDlaLeku(wybranyLek, startDate, endDate);

                CalkowitaSprzedazLabel.Content = statystyki.CalkowitaSprzedaz.ToString();
                SredniaCenaLabel.Content = $"{statystyki.SredniaCena:C}";
                LacznePrzychodyLabel.Content = $"{statystyki.LacznePrzychody:C}";

                HistoriaZamowienWykres.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = wybranyLek,
                Values = new ChartValues<int>(statystyki.IstoriaZamowien)
            }
        };

                HistoriaZamowienWykres.AxisX[0].Labels = statystyki.DatyZamowien.Select(d => d.ToShortDateString()).ToArray();
            }
        }


        private (int CalkowitaSprzedaz, decimal SredniaCena, decimal LacznePrzychody, List<int> IstoriaZamowien, List<DateTime> DatyZamowien)
     GetStatystykiDlaLeku(string nazwaLeku, DateTime? startDate, DateTime? endDate)
        {
            using (var context = new MediMaxEntities())
            {
                var lek = context.tbl_Leki.FirstOrDefault(l => l.Nazwa == nazwaLeku);
                if (lek == null)
                {
                    return (0, 0, 0, new List<int>(), new List<DateTime>());
                }

                int idLeku = lek.Id;
                var zamowienia = context.tbl_Zamowienia
                    .Where(z => z.IdLeku == idLeku);

                if (startDate.HasValue)
                    zamowienia = zamowienia.Where(z => z.DataZamowienia >= startDate.Value);
                if (endDate.HasValue)
                    zamowienia = zamowienia.Where(z => z.DataZamowienia <= endDate.Value);

                var zamowieniaLista = zamowienia.OrderBy(z => z.DataZamowienia).ToList();

                if (!zamowieniaLista.Any())
                {
                    return (0, 0, 0, new List<int>(), new List<DateTime>());
                }

                int calkowitaSprzedaz = zamowieniaLista.Sum(z => z.Ilosc);
                decimal sredniaCena = zamowieniaLista.Average(z => z.tbl_Leki.Cena ?? 0);
                decimal lacznePrzychody = zamowieniaLista.Sum(z => z.Ilosc * (z.tbl_Leki.Cena ?? 0));

                var ilosciZamowien = zamowieniaLista.Select(z => z.Ilosc).ToList();
                var datyZamowien = zamowieniaLista.Select(z => z.DataZamowienia).ToList();

                return (calkowitaSprzedaz, sredniaCena, lacznePrzychody, ilosciZamowien, datyZamowien);
            }
        }
        private void AktualizujWykres_Click(object sender, RoutedEventArgs e)
        {
            string wybranyLek = LekComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(wybranyLek))
            {
                DateTime? startDate = StartDatePicker.SelectedDate;
                DateTime? endDate = EndDatePicker.SelectedDate;

                var statystyki = GetStatystykiDlaLeku(wybranyLek, startDate, endDate);

                CalkowitaSprzedazLabel.Content = statystyki.CalkowitaSprzedaz.ToString();
                SredniaCenaLabel.Content = $"{statystyki.SredniaCena:C}";
                LacznePrzychodyLabel.Content = $"{statystyki.LacznePrzychody:C}";

                HistoriaZamowienWykres.Series = new SeriesCollection
        {
            new LineSeries
            {
                Title = wybranyLek,
                Values = new ChartValues<int>(statystyki.IstoriaZamowien)
            }
        };

                HistoriaZamowienWykres.AxisX[0].Labels = statystyki.DatyZamowien.Select(d => d.ToShortDateString()).ToArray();
            }
        }
        private void EksportujPDF_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Zapisz wykres jako PDF"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)HistoriaZamowienWykres.ActualWidth,
                        (int)HistoriaZamowienWykres.ActualHeight,
                        96, 96, PixelFormats.Pbgra32);

                    renderBitmap.Render(HistoriaZamowienWykres);

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (MemoryStream stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        byte[] imageData = stream.ToArray();

                        using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                        {
                            Document document = new Document();
                            PdfWriter writer = PdfWriter.GetInstance(document, fs);
                            document.Open();

                            iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(imageData);
                            pdfImage.ScaleToFit(500, 500);
                            document.Add(pdfImage);

                            document.Close();
                            writer.Close();
                        }
                    }

                    MessageBox.Show("Wykres został zapisany do PDF.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd zapisu PDF: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ResetujDaty_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
        }

        public void ReceptyStatystyka()
        {
            using (var context = new MediMaxEntities())
            {
                int niezrealizowaneRecepty = 0;
                int zrealizowaneRecepty = 0;

                var numeryRecept = context.tbl_Recepta
                    .Select(r => r.NumerRecepty)
                    .Distinct()
                    .ToList();

                foreach (var numerRecepty in numeryRecept)
                {
                    var lekiDlaRecepty = context.tbl_Recepta
                        .Where(r => r.NumerRecepty == numerRecepty)
                        .ToList();

                    if (lekiDlaRecepty.Any(r => !r.CzyZrealizowano))
                    {
                        niezrealizowaneRecepty++;
                    }
                    else
                    {
                        zrealizowaneRecepty++;
                    }
                }

                ZrealizowaneLabel.Content = zrealizowaneRecepty.ToString();
                NiezrealizowaneLabel.Content = niezrealizowaneRecepty.ToString();
            }
        }

        public void PodsumowanieLekow()
        {
            using (var context = new MediMaxEntities())
            {
                int iloscLekowBezRecepty = context.tbl_Leki.Count(lek => lek.CzyNaRecepte == false);
                int iloscLekowNaRecepte = context.tbl_Leki.Count(lek => lek.CzyNaRecepte == true);
                var najdrozszyLek = context.tbl_Leki.OrderByDescending(lek => lek.Cena).FirstOrDefault();
                var najtanszyLek = context.tbl_Leki.OrderBy(lek => lek.Cena).FirstOrDefault();
                var najczesciejPrzepisywany = context.tbl_ZapotrzebowanieLeku.OrderByDescending(lek => lek.IloscPrzepisanych).FirstOrDefault();
                var najrzadziejPrzepisywany = context.tbl_ZapotrzebowanieLeku.OrderBy(lek => lek.IloscPrzepisanych).FirstOrDefault();
                string najczesciejPrzepisywanyNazwa = context.tbl_Leki.Where(lek => lek.Id == najczesciejPrzepisywany.IdLeku).Select(lek => lek.Nazwa).FirstOrDefault();
                string najrzadziejPrzepisywanyNazwa = context.tbl_Leki.Where(lek => lek.Id == najrzadziejPrzepisywany.IdLeku).Select(lek => lek.Nazwa).FirstOrDefault();

                BezReceptyLabel.Content = iloscLekowBezRecepty.ToString();
                NaRecepteLabel.Content = iloscLekowNaRecepte.ToString();
                NajdrozszyLabel.Content = najdrozszyLek.Nazwa;
                NajtanszyLabel.Content = najtanszyLek.Nazwa;
                NajczesciejPrzepisywanyLabel.Content = najczesciejPrzepisywanyNazwa;
                NajrzadziejPrzepisywanyLabel.Content = najrzadziejPrzepisywanyNazwa;
            }
        }

        private void DaneWykresu()
        {
            using (var context = new MediMaxEntities())
            {
                var lekiZMagazynu = context.tbl_StanMagazynowy
                    .GroupBy(s => s.IdLeku)
                    .Select(group => new
                    {
                        IdLeku = group.Key,
                        Ilosc = group.Sum(s => s.Ilosc)
                    }).ToList();

                var daneDoWykresu = lekiZMagazynu
                    .Join(context.tbl_Leki,
                          stan => stan.IdLeku,
                          lek => lek.Id,
                          (stan, lek) => new
                          {
                              NazwaLeku = lek.Nazwa,
                              Ilosc = stan.Ilosc
                          }).ToList();

                SeriesCollection series = new SeriesCollection();

                foreach (var pozycja in daneDoWykresu)
                {
                    series.Add(new PieSeries
                    {
                        Title = pozycja.NazwaLeku,
                        Values = new ChartValues<int> { pozycja.Ilosc },
                        DataLabels = true
                    });
                }

                WykresLekow.Series = series;
            }
        }
    }
}
