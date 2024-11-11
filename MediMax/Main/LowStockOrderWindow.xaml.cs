using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Main
{
    public partial class LowStockOrderWindow : Window
    {
        public LowStockOrderWindow()
        {
            InitializeComponent();
            LoadLowStockMedicines();
        }

        private void LoadLowStockMedicines()
        {
            var lowStockMedicines = GetLowStockMedicines();
            foreach (var item in lowStockMedicines)
            {
                var stackPanelItem = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var medicineName = new TextBlock
                {
                    Text = item.Nazwa,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 200
                };

                
                var orderQuantityBox = new TextBox
                {
                    Text = "0", 
                    Width = 100,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = item 
                };

                stackPanelItem.Children.Add(medicineName);
                stackPanelItem.Children.Add(orderQuantityBox);

                MedicinesStackPanel.Children.Add(stackPanelItem);
            }
        }


        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            bool isAnyQuantityEntered = false; 

            foreach (var child in MedicinesStackPanel.Children.OfType<StackPanel>())
            {
                var quantityTextBox = child.Children.OfType<TextBox>().FirstOrDefault();

                if (quantityTextBox != null && int.TryParse(quantityTextBox.Text, out var quantity) && quantity > 0)
                {
                    isAnyQuantityEntered = true;

                    var medicineName = child.Children.OfType<TextBlock>().FirstOrDefault()?.Text;
                    var selectedMedicine = (tbl_Leki)quantityTextBox.Tag;

                    PlaceOrder(selectedMedicine.Id, quantity, "supplier@example.com");
                }
            }

            if (!isAnyQuantityEntered)
            {
                
                MessageBox.Show("Proszę wpisać odpowiednią liczbę leków do zamówienia.");
            }
            else
            {

                MessageBox.Show("Zamówienia zostały złożone.");
                this.Close();  
            }
        }




        private int CalculateOrderQuantity(tbl_Leki item, decimal currentStock)
        {
            
            var totalDemand = item.tbl_ZapotrzebowanieLeku.Sum(z => z.IloscPrzepisanych);
            var threshold = totalDemand * 0.2m; 

            if (currentStock < threshold)
            {
                return (int)(threshold - currentStock);
            }

            return 0;
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
                    context.SaveChanges();
                }
                else
                {
                    MessageBox.Show("Nie znaleziono leku w magazynie.");
                    return;
                }

                context.SaveChanges();
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
    }
}
