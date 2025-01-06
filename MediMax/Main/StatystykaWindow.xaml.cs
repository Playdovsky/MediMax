using LiveCharts.Wpf;
using LiveCharts;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System;

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
                var sprzedaneLeki = context.tbl_Recepta
                    .Where(r => r.CzyZrealizowano)
                    .GroupBy(r => r.IdLeku)
                    .Select(group => new
                    {
                        IdLeku = group.Key,
                        IloscSprzedazy = group.Count()
                    })
                    .OrderByDescending(lek => lek.IloscSprzedazy)
                    .Take(5)
                    .ToList();

                var statystyki = sprzedaneLeki
                    .Join(context.tbl_Leki,
                          sprzedaz => sprzedaz.IdLeku,
                          lek => lek.Id,
                          (sprzedaz, lek) => new
                          {
                              NazwaLeku = lek.Nazwa,
                              IloscSprzedazy = sprzedaz.IloscSprzedazy
                          })
                    .ToList();

                // Tworzymy listę nazw leków i ilości sprzedaży do wyświetlenia
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
                // Pobierz dane dla wybranego leku
                var statystyki = GetStatystykiDlaLeku(wybranyLek);

                // Wyświetl szczegóły leku
                CalkowitaSprzedazLabel.Content = statystyki.CalkowitaSprzedaz.ToString();
                SredniaCenaLabel.Content = $"{statystyki.SredniaCena:C}";
                LacznePrzychodyLabel.Content = $"{statystyki.LacznePrzychody:C}";

                // Zaktualizuj wykres
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
            GetStatystykiDlaLeku(string nazwaLeku)
        {
            using (var context = new MediMaxEntities())
            {
                // Znajdź ID leku na podstawie nazwy
                var lek = context.tbl_Leki.FirstOrDefault(l => l.Nazwa == nazwaLeku);
                if (lek == null)
                {
                    return (0, 0, 0, new List<int>(), new List<DateTime>());
                }

                int idLeku = lek.Id;

                var zamowienia = context.tbl_Zamowienia
                    .Where(z => z.IdLeku == idLeku)
                    .OrderBy(z => z.DataZamowienia)
                    .ToList();

                if (!zamowienia.Any())
                {
                    return (0, 0, 0, new List<int>(), new List<DateTime>());
                }

                int calkowitaSprzedaz = zamowienia.Sum(z => z.Ilosc);
                decimal sredniaCena = zamowienia.Average(z => z.tbl_Leki.Cena ?? 0);
                decimal lacznePrzychody = zamowienia.Sum(z => z.Ilosc * (z.tbl_Leki.Cena ?? 0));

                var ilosciZamowien = zamowienia.Select(z => z.Ilosc).ToList();
                var datyZamowien = zamowienia.Select(z => z.DataZamowienia).ToList();

                return (calkowitaSprzedaz, sredniaCena, lacznePrzychody, ilosciZamowien, datyZamowien);
            }
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
