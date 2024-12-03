using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SkiaSharp;
namespace Main
{
    public partial class LekarzWindow : Window
    {
        private List<Lek> allLeki = new List<Lek>();

        public LekarzWindow()
        {
            InitializeComponent();
            LoadTypyLekow();
            LoadLeki();
        }

        private void LoadTypyLekow()
        {
            using (var context = new MediMaxEntities())
            {
                var typy = context.tbl_Leki.Select(l => l.Typ).Distinct().ToList();

                foreach (var typ in typy)
                {
                    CheckBox checkBox = new CheckBox
                    {
                        Content = typ,
                        Tag = typ,
                        Margin = new Thickness(5)
                    };
                    checkBox.Checked += FilterLeki;
                    checkBox.Unchecked += FilterLeki;
                    TypyLekowPanel.Items.Add(checkBox);
                }
            }
        }

        private void LoadLeki()
        {
            using (var context = new MediMaxEntities())
            {
                allLeki = context.tbl_Leki.Select(l => new Lek
                {
                    Id = l.Id,
                    Nazwa = l.Nazwa,
                    Typ = l.Typ,
                    Cena = l.Cena ?? 0
                }).ToList();
            }
            FilterLeki(null, null);
        }

        private void FilterLeki(object sender, RoutedEventArgs e)
        {
            var selectedTypes = TypyLekowPanel.Items
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag.ToString())
                .ToList();

            var filteredLeki = allLeki.Where(l => selectedTypes.Contains(l.Typ)).ToList();
            LekiListBox.ItemsSource = filteredLeki.Select(l => l.Nazwa).ToList();
        }

        private void CreateRecepta_Click(object sender, RoutedEventArgs e)
        {
            string pesel = PeselTextBox.Text;
            if (!IsPeselValid(pesel))
            {
                MessageBox.Show("Wprowadź prawidłowy numer PESEL.");
                return;
            }

            int numerRecepty = GenerateUniqueReceptaNumber(pesel);
            SaveRecepta(pesel, numerRecepty);
        }

        private int GenerateUniqueReceptaNumber(string pesel)
        {
            Random random = new Random();
            int numerRecepty;
            bool isUnique;

            do
            {
                numerRecepty = random.Next(1000, 9999);
                isUnique = CheckUniqueReceptaNumber(pesel, numerRecepty);
            } while (!isUnique);

            return numerRecepty;
        }

        private bool CheckUniqueReceptaNumber(string pesel, int numerRecepty)
        {
            using (var context = new MediMaxEntities())
            {
                return !context.tbl_Recepta.Any(r => r.PESEL == pesel && r.NumerRecepty == numerRecepty);
            }
        }

        private void SaveRecepta(string pesel, int numerRecepty)
        {
            try
            {
                using (var context = new MediMaxEntities())
                {
                    if (LekiListBox.SelectedItems.Count == 0)
                    {
                        MessageBox.Show("Wybierz przynajmniej jeden lek do recepty.");
                        return;
                    }

                    List<tbl_Recepta> recepty = new List<tbl_Recepta>();

                    foreach (var lekName in LekiListBox.SelectedItems)
                    {
                        var lek = allLeki.FirstOrDefault(l => l.Nazwa == lekName.ToString());
                        if (lek != null)
                        {
                            var recepta = new tbl_Recepta
                            {
                                IdLeku = lek.Id,
                                NumerRecepty = numerRecepty,
                                PESEL = pesel,
                                CzyZrealizowano = false
                            };
                            recepty.Add(recepta);
                        }
                    }

                    var zalecenie = new tbl_ReceptaZalecenia
                    {
                        NumerRecepty = numerRecepty,
                        Zalecenia = ZaleceniaTextBox.Text
                    };
                    SaveReceptaAsImage(pesel, numerRecepty, recepty.Select(r => allLeki.First(l => l.Id == r.IdLeku)).ToList(), ZaleceniaTextBox.Text);
                    context.tbl_Recepta.AddRange(recepty);
                    context.tbl_ReceptaZalecenia.Add(zalecenie);
                    context.SaveChanges();
                }

                MessageBox.Show($"Recepta została wystawiona! Numer recepty: {numerRecepty}");

                PeselTextBox.Text = string.Empty;
                ZaleceniaTextBox.Text = string.Empty;
                LekiListBox.SelectedItems.Clear();

                foreach (CheckBox checkBox in TypyLekowPanel.Items.OfType<CheckBox>())
                {
                    checkBox.IsChecked = false;
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji bazy danych: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas zapisywania recepty: {ex.Message}");
            }
        }


        private void PeselTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+"); 
            return !regex.IsMatch(text);
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void PodgladReceptButton_Click(object sender, RoutedEventArgs e)
        {
            PodgladReceptWindow podgladWindow = new PodgladReceptWindow();
            this.Hide();
            podgladWindow.ShowDialog();
            this.Show();
        }
        private void SaveReceptaAsImage(string pesel, int numerRecepty, List<Lek> leki, string zalecenia)
        {
            string fileName = $"Recepta_{numerRecepty}.png";

            try
            {
                int width = 800, height = 1000;

                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    var backgroundPaint = new SKPaint
                    {
                        Color = SKColor.Parse("#F0F0F0") 
                    };
                    canvas.DrawRect(0, 0, width, height, backgroundPaint);

                    var headerPaint = new SKPaint
                    {
                        TextSize = 32,
                        IsAntialias = true,
                        Color = SKColors.DarkBlue,
                        FakeBoldText = true
                    };

                    var textPaint = new SKPaint
                    {
                        TextSize = 24,
                        IsAntialias = true,
                        Color = SKColors.Black
                    };

                    canvas.DrawText($"Recepta: {numerRecepty}", 20, 50, headerPaint);
                    canvas.DrawText($"PESEL: {pesel}", 20, 90, headerPaint);

                    var separatorPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        StrokeWidth = 2
                    };
                    canvas.DrawLine(20, 110, width - 20, 110, separatorPaint);

                    int yOffset = 130;
                    foreach (var lek in leki)
                    {
                        canvas.DrawText($"- {lek.Nazwa} ({lek.Typ}) - {lek.Cena:C}", 20, yOffset, textPaint);
                        yOffset += 40; 
                    }

                    canvas.DrawLine(20, yOffset + 10, width - 20, yOffset + 10, separatorPaint);

                    int zaleceniaOffset = yOffset + 40;
                    var zaleceniaPaint = new SKPaint
                    {
                        TextSize = 24,
                        IsAntialias = true,
                        Color = SKColors.DarkGreen
                    };

                    canvas.DrawText($"Zalecenia: {zalecenia}", 20, zaleceniaOffset, zaleceniaPaint);

                    var footerPaint = new SKPaint
                    {
                        TextSize = 20,
                        IsAntialias = true,
                        Color = SKColors.Gray
                    };
                    canvas.DrawText("Wygenerowane przez aplikację MediMax", 20, height - 40, footerPaint);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var stream = System.IO.File.OpenWrite(fileName))
                    {
                        data.SaveTo(stream);
                    }
                }

                MessageBox.Show($"Recepta została zapisana w pliku: {fileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisu obrazu: {ex.Message}");
            }
        }

    }

    public class Lek
    {
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public string Typ { get; set; }
        public decimal Cena { get; set; }
    }
}
