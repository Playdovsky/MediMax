using System.Windows;

namespace Main
{
    public partial class AptekarzWindow : Window
    {
        public AptekarzWindow()
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
