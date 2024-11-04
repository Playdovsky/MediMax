using System.Linq;
using System.Windows;


namespace Main
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text; 
            string haslo = PasswordTextBox.Password;

            using (var context = new MediMaxEntities()) 
            {
                
                var uzytkownik = context.tbl_Uzytkownik
                                        .Include("tbl_Rola")
                                        .FirstOrDefault(u => u.Email == email && u.Haslo == haslo);

                if (uzytkownik != null)
                {
                    
                    if (uzytkownik.tbl_Rola != null)
                    {
                        if (uzytkownik.tbl_Rola.Nazwa == "Lekarz" || uzytkownik.IdRola == 1)
                        {
                            
                            LekarzWindow lekarzWindow = new LekarzWindow();
                            lekarzWindow.Show();
                            this.Close(); 
                        }
                        else if (uzytkownik.tbl_Rola.Nazwa == "Aptekarz" || uzytkownik.IdRola == 2)
                        {
                            
                            AptekarzWindow aptekarzWindow = new AptekarzWindow();
                            aptekarzWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Nieznana rola użytkownika.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Rola użytkownika nie została przypisana.");
                    }
                }
                else
                {
                    MessageBox.Show("Niepoprawny email lub hasło.");
                }
            }

        }
        private void ContactTechnicianButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
        }

    }
}
