using OpenTK.Graphics.ES11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace Main
{
    public partial class AptekarzWindow : Window
    {
        private List<LekDoRealizacji> lekiDoRealizacji = new List<LekDoRealizacji>();
        private List<LekDoRealizacji> lekiBezRecepty = new List<LekDoRealizacji>();

        public AptekarzWindow()
        {
            InitializeComponent();
            RemoveExpiredMedicines();
            CheckLowStockMedicines();
            LoadNonPrescriptionMedicines();
        }

        private void CheckLowStockMedicines()
        {
            var lowStockMedicines = GetLowStockMedicines();
            if (lowStockMedicines.Any())
            {
                MessageBox.Show("Brakuje niektórych leków, naciśnij zamów po więcej szczegółów");
            }
        }
          public List<tbl_Leki> GetLowStockMedicines()
    {
        using (var context = new MediMaxEntities())
        {
            var lowStockMedicines = context.tbl_Leki
                .Where(lek =>
                    lek.tbl_StanMagazynowy.Any(sm => sm.Ilosc < 0.2 * lek.tbl_ZapotrzebowanieLeku.Sum(z => z.IloscPrzepisanych)))
                .ToList();

            return lowStockMedicines;
        }
    }
        private void RemoveExpiredMedicines()
        {
            using (var context = new MediMaxEntities())
            {
                var expiredMedicines = context.tbl_Zamowienia
                    .Where(z => z.DataWaznosci < DateTime.Now)
                    .ToList();

                if (expiredMedicines.Any()) 
                {
                    foreach (var order in expiredMedicines)
                    {
                        var stock = context.tbl_StanMagazynowy.FirstOrDefault(sm => sm.IdLeku == order.IdLeku);
                        if (stock != null)
                        {
                            stock.Ilosc -= order.Ilosc;
                        }

                        context.tbl_Zamowienia.Remove(order);
                    }

                    context.SaveChanges();

                    MessageBox.Show("Przeterminowane leki zostały usunięte z magazynu");
                }
            }
        }
        private void LoadNonPrescriptionMedicines()
        {
            using (var context = new MediMaxEntities())
            {
                lekiBezRecepty = context.tbl_Leki
                    .Where(lek => !lek.CzyNaRecepte)
                    .Select(lek => new LekDoRealizacji
                    {
                        Id = lek.Id,
                        Nazwa = lek.Nazwa,
                        Typ = lek.Typ,
                        StanMagazynowy = lek.tbl_StanMagazynowy.Sum(s => s.Ilosc),
                        CzyZrealizowano = false,
                        CzyPremiowany = false
                    })
                    .ToList();

                var random = new Random();
                var premiowaneLeki = lekiBezRecepty.OrderBy(x => random.Next()).Take(2).ToList();
                foreach (var lek in premiowaneLeki)
                {
                    lek.CzyPremiowany = true;
                }

                LekiBezReceptyListBox.ItemsSource = lekiBezRecepty;
            }
        }

        private void WyszukajRecepta_Click(object sender, RoutedEventArgs e)
        {
            string pesel = PeselTextBox.Text;
            int numerRecepty;

            if (!int.TryParse(NumerReceptyTextBox.Text, out numerRecepty) || !IsPeselValid(pesel))
            {
                MessageBox.Show("Wprowadź prawidłowy numer PESEL oraz numer recepty.");
                return;
            }

            WyszukajLekiDoRealizacji(pesel, numerRecepty);
        }

        private void WyszukajLekiDoRealizacji(string pesel, int numerRecepty)
        {
            using (var context = new MediMaxEntities())
            {
                lekiDoRealizacji = context.tbl_Recepta
                    .Where(r => r.PESEL == pesel && r.NumerRecepty == numerRecepty && !r.CzyZrealizowano)
                    .Select(r => new LekDoRealizacji
                    {
                        Id = r.IdLeku,
                        Nazwa = r.tbl_Leki.Nazwa,
                        Typ = r.tbl_Leki.Typ,
                        StanMagazynowy = context.tbl_StanMagazynowy
                                          .Where(s => s.IdLeku == r.IdLeku)
                                          .Select(s => s.Ilosc)
                                          .FirstOrDefault(),
                        CzyZrealizowano = r.CzyZrealizowano
                    })
                    .ToList();

                if (lekiDoRealizacji.Count == 0)
                {
                    MessageBox.Show("Nie znaleziono leków do realizacji dla podanych danych.");
                }
                else
                {
                    LekiListBox.ItemsSource = lekiDoRealizacji;
                }
            }
        }

        private void ZrealizujLeki_Click(object sender, RoutedEventArgs e)
        {
            var selectedLeki = LekiListBox.SelectedItems.Cast<LekDoRealizacji>().ToList();
            var selectedNonPrescriptionLeki = LekiBezReceptyListBox.SelectedItems.Cast<LekDoRealizacji>().ToList();

            using (var context = new MediMaxEntities())
            {
                bool allMedicinesRealized = true;

                foreach (var lek in selectedLeki)
                {
                    var receptaDb = context.tbl_Recepta
                        .FirstOrDefault(r => r.IdLeku == lek.Id && !r.CzyZrealizowano);

                    var stanMagazynowyDb = context.tbl_StanMagazynowy.FirstOrDefault(s => s.IdLeku == lek.Id);
                    var sprzedazDb = context.tbl_Sprzedaz;

                    if (receptaDb != null && stanMagazynowyDb != null)
                    {
                        if (stanMagazynowyDb.Ilosc > 0)
                        {
                            stanMagazynowyDb.Ilosc -= 1;
                            receptaDb.CzyZrealizowano = true;
                            sprzedazDb.Add(new tbl_Sprzedaz
                            {
                                IdLekuSprzedanego = lek.Id,
                                DataSprzedazy = DateTime.Now,
                                IdAptekarza = 1
                            }) ;

                        }
                        else
                        {
                            SingleOrderWindow orderWindow = new SingleOrderWindow(lek.Id);
                            orderWindow.ShowDialog();
                            allMedicinesRealized = false;
                        }
                    }
                }

                foreach (var lek in selectedNonPrescriptionLeki)
                {
                    var stanMagazynowyDb = context.tbl_StanMagazynowy.FirstOrDefault(s => s.IdLeku == lek.Id);
                    var sprzedazDb = context.tbl_Sprzedaz;


                    if (stanMagazynowyDb != null)
                    {
                        if (stanMagazynowyDb.Ilosc > 0)
                        {
                            stanMagazynowyDb.Ilosc -= 1;
                            sprzedazDb.Add(new tbl_Sprzedaz
                            {
                                IdLekuSprzedanego = lek.Id,
                                DataSprzedazy = DateTime.Now,
                                IdAptekarza = 1
                            });
                        }
                        else
                        {
                            SingleOrderWindow orderWindow = new SingleOrderWindow(lek.Id);
                            orderWindow.ShowDialog();
                            allMedicinesRealized = false;
                        }
                    }
                }

                context.SaveChanges();

                if (allMedicinesRealized)
                {
                    MessageBox.Show("Wybrane leki zostały zrealizowane.");
                }
                else
                {
                    MessageBox.Show("Niektóre leki nie mogły być zrealizowane z powodu braku w magazynie. Zostały zamówione.");
                }

                LekiListBox.ItemsSource = null;
                LekiBezReceptyListBox.ItemsSource = null;
                lekiDoRealizacji.Clear();
                lekiBezRecepty.Clear();
                LoadNonPrescriptionMedicines();
            }
        }


        private bool IsPeselValid(string pesel)
        {
            if (pesel.Length != 11 || !long.TryParse(pesel, out _)) return false;
            int[] weights = { 9, 7, 3, 1, 9, 7, 3, 1, 9, 7 };
            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                sum += (pesel[i] - '0') * weights[i];
            }
            int checksum = sum % 10;
            return checksum == (pesel[10] - '0');
        }
        private void PeselTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            LowStockOrderWindow orderWindow = new LowStockOrderWindow();
            orderWindow.ShowDialog();
        }

        private void PrzeprowadzStatystyke_Click(object sender, RoutedEventArgs e)
        {
            StatystykaWindow statystykaWindow = new StatystykaWindow();
            statystykaWindow.ShowDialog();
        }
    }


    public class LekDoRealizacji
    {
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public string Typ { get; set; }
        public int StanMagazynowy { get; set; }
        public bool CzyZrealizowano { get; set; }
        public bool CzyPremiowany { get; set; }
    }

}
