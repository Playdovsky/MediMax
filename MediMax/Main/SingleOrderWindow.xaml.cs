using System;
using System.Net.Mail;
using System.Net;
using System.Windows;
using System.Linq;

namespace Main
{
    public partial class SingleOrderWindow : Window
    {
        private int lekId;
        private string lekNazwa;

        public SingleOrderWindow(int lekId, string lekNazwa)
        {
            InitializeComponent();
            this.lekId = lekId;  
            this.lekNazwa = lekNazwa;
        }


        private void ConfirmOrderButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (int.TryParse(OrderQuantityTextBox.Text, out int orderQuantity) && orderQuantity > 0)
            {
                string customerEmail = CustomerEmailTextBox.Text;  

               
                PlaceOrder(lekId, orderQuantity, customerEmail);

                
                SendStockAvailableEmail(customerEmail, lekNazwa);

                MessageBox.Show("Zamówienie zostało złożone.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Proszę wpisać ilość większą niż 0.");
            }
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


        private void SendStockAvailableEmail(string toEmail, string lekName)
        {
            using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential("dkunicki2002@gmail.com", "sceujkgfsqqafymf");

                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress("dkunicki2002@gmail.com");
                    message.To.Add(new MailAddress(toEmail)); 
                    message.Subject = "Lek dostępny w magazynie!";
                    message.Body = $"Drogi kliencie,\nLek {lekName}, który nie był dostępny w naszym magazynie, jest teraz dostępny. Zapraszamy do ponownego zakupu.";

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
