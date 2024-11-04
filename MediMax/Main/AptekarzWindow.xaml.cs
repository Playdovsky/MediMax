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

        public AptekarzWindow()
        {
            InitializeComponent();
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
            if (LekiListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("Wybierz przynajmniej jeden lek do realizacji.");
                return;
            }

            using (var context = new MediMaxEntities())
            {
                bool allMedicinesRealized = true; 

                foreach (LekDoRealizacji lek in LekiListBox.SelectedItems)
                {
                    var stanMagazynowyDb = context.tbl_StanMagazynowy.FirstOrDefault(s => s.IdLeku == lek.Id);
                    var receptaDb = context.tbl_Recepta
                        .FirstOrDefault(r => r.IdLeku == lek.Id && !r.CzyZrealizowano);

                    if (stanMagazynowyDb != null && receptaDb != null)
                    {
                        if (stanMagazynowyDb.Ilosc > 0)
                        {
                            stanMagazynowyDb.Ilosc -= 1;
                            receptaDb.CzyZrealizowano = true;
                        }
                        else
                        {
                            MessageBox.Show($"Brak w magazynie: {lek.Nazwa}. Zamówienie leku wymagane.");
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
                    MessageBox.Show("Niektóre leki nie mogły być zrealizowane z powodu braku w magazynie.");
                }

                PeselTextBox.Text = string.Empty;
                NumerReceptyTextBox.Text = string.Empty;
                LekiListBox.ItemsSource = null;
                lekiDoRealizacji.Clear();
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
    }


    public class LekDoRealizacji
    {
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public string Typ { get; set; }
        public int StanMagazynowy { get; set; }
        public bool CzyZrealizowano { get; set; }
    }

}
