using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace Main
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text;
            string email = EmailTextBox.Text;
            string userType = (UserTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userType))
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola.");
                return;
            }

            string messageBody = $"Nowa rejestracja:\nImię i Nazwisko: {fullName}\nEmail: {email}\nTyp: {userType}";

            SendRegistrationEmail("MediMaxApteka@gmail.com", messageBody);
            MessageBox.Show("Formularz został wysłany!");
            this.Close(); // Zamknij okno po wysłaniu
        }

        private void SendRegistrationEmail(string toEmail, string body)
        {
            using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential("dkunicki2002@gmail.com", "sceujkgfsqqafymf");

                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress("dkunicki2002@gmail.com");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = "Nowa rejestracja";
                    message.Body = body;

                    try
                    {
                        client.Send(message);
                    }
                    catch (SmtpException smtpEx)
                    {
                        MessageBox.Show($"Wystąpił błąd podczas wysyłania wiadomości: {smtpEx.Message}");
                    }
                }
            }
        }

    }
}
