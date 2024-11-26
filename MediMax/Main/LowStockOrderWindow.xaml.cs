using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Main
{
    public partial class LowStockOrderWindow : Window
    {
        public class LekViewModel
        {
            public string Nazwa { get; set; }
            public int StanMagazynowy { get; set; }
            public int Zapotrzebowanie { get; set; }
            public int IloscDoZamowienia { get; set; }
            public int Id { get; set; }
        }
        public LowStockOrderWindow()
        {
            InitializeComponent();
            LoadLowStockMedicines();
        }

        private void LoadLowStockMedicines()
        {
            var lowStockMedicines = GetLowStockMedicines().Select(medicine => new LekViewModel
            {
                Id = medicine.Id,
                Nazwa = medicine.Nazwa,
                StanMagazynowy = medicine.tbl_StanMagazynowy.FirstOrDefault()?.Ilosc ?? 0,
                Zapotrzebowanie = medicine.tbl_ZapotrzebowanieLeku.Sum(z => z.IloscPrzepisanych),
                IloscDoZamowienia = 0
            }).ToList();

            MedicinesList.ItemsSource = lowStockMedicines;
        }

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(MedicinesList.ItemsSource is List<LekViewModel> medicines))
                return;

            var selectedMedicines = medicines.Where(m => m.IloscDoZamowienia > 0).ToList();

            if (!selectedMedicines.Any())
            {
                MessageBox.Show("Proszę wpisać odpowiednią liczbę leków do zamówienia.");
                return;
            }

            var confirmationDialog = new ConfirmationDialog();
            confirmationDialog.ShowDialog();

            if (!confirmationDialog.IsConfirmed)
            {
                MessageBox.Show("Zamówienie zostało anulowane.");
                return;
            }

            foreach (var medicine in selectedMedicines)
            {
                PlaceOrder(medicine.Id, medicine.IloscDoZamowienia, "");
            }

            MessageBox.Show("Zamówienia zostały złożone.");
            this.Close();
        }

        private void PlaceOrder(int lekId, int quantity, string kontakt)
        {
            using (var context = new MediMaxEntities())
            {
                var order = new tbl_Zamowienia
                {
                    IdLeku = lekId,
                    Ilosc = quantity,
                    DataZamowienia = DateTime.Now,
                    Kontakt = kontakt
                };
                context.tbl_Zamowienia.Add(order);

                var stock = context.tbl_StanMagazynowy.FirstOrDefault(sm => sm.IdLeku == lekId);
                if (stock != null)
                {
                    stock.Ilosc += quantity;
                }
                else
                {
                    MessageBox.Show($"Nie znaleziono leku o ID {lekId} w magazynie.");
                    return;
                }

                context.SaveChanges();
            }
        }

        private List<tbl_Leki> GetLowStockMedicines()
        {
            using (var context = new MediMaxEntities())
            {
                return context.tbl_Leki
                    .Include("tbl_StanMagazynowy")
                    .Include("tbl_ZapotrzebowanieLeku")
                    .Where(lek =>
                        lek.tbl_StanMagazynowy.Any(sm => sm.Ilosc < 0.2 * lek.tbl_ZapotrzebowanieLeku.Sum(z => z.IloscPrzepisanych)))
                    .ToList();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
