using System.Windows;

namespace Main
{
    public partial class LekarzWindow : Window
    {
        public LekarzWindow()
        {
            InitializeComponent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close(); 
        }
    }
}
