using System.Windows;

namespace Main
{
    public partial class ConfirmationDialog : Window
    {
        public bool IsConfirmed { get; private set; }

        public ConfirmationDialog()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}
