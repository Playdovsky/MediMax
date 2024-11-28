using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Main
{
    public partial class PodgladReceptWindow : Window
    {
        public PodgladReceptWindow()
        {
            InitializeComponent();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string pesel = PeselTextBox.Text;
            if (!IsPeselValid(pesel))
            {
                MessageBox.Show("Wprowadź prawidłowy numer PESEL.");
                return;
            }

            LoadRecepty(pesel);
        }

        private void LoadRecepty(string pesel)
        {
            using (var context = new MediMaxEntities())
            {
                // Pobieramy podstawowe dane
                var recepty = from r in context.tbl_Recepta
                              join l in context.tbl_Leki on r.IdLeku equals l.Id
                              where r.PESEL == pesel
                              select new
                              {
                                  NumerRecepty = r.NumerRecepty,
                                  LekNazwa = l.Nazwa,
                                  CzyZrealizowano = r.CzyZrealizowano ? "Zrealizowana" : "Nie zrealizowana",
                                  CzyZrealizowanoBool = r.CzyZrealizowano // Dodatkowe pole pomocnicze do filtrowania
                              };

                // Filtr numeru recepty
                if (int.TryParse(NumerReceptyTextBox.Text, out int numerRecepty))
                {
                    recepty = recepty.Where(r => r.NumerRecepty == numerRecepty);
                }

                // Filtr niezrealizowanych recept
                if (NiezrealizowaneCheckBox.IsChecked == true)
                {
                    recepty = recepty.Where(r => r.CzyZrealizowanoBool == false);
                }

                // Pobieramy listę wyników
                var receptyList = recepty.ToList();

                // Wyświetlamy dane lub komunikat o braku wyników
                if (!receptyList.Any())
                {
                    MessageBox.Show("Brak recept spełniających podane kryteria.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReceptyDataGrid.ItemsSource = null;
                }
                else
                {
                    ReceptyDataGrid.ItemsSource = receptyList;
                }
            }
        }



        private void PeselTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
    }
}
