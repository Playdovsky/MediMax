using LiveCharts.Wpf;
using LiveCharts;
using System.Linq;
using System.Windows;

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
