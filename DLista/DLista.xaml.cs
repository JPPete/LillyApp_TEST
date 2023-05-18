using System;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for DLista.xaml
    /// </summary>
    public partial class DLista : Window
    {
        SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodEditConnString;


        public DLista()
        {
            InitializeComponent();
        }


        #region "Key UP"
        private void EnterUpIdArtikla(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                txtCena.Focus();
            }
        }

        private void EnterUpCena(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                RunDListaBtn(sender, e);
            }
        }
        #endregion


        #region "Input Validation"
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CenaValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion





        private void RunDListaBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuo dugme RunDlistaBtn");
            Shared.LogUser($"Uneti podaci - ID Artikla: {txtIDArtikla.Text} | Cena Artikla: {txtCena.Text}");

            


            //Check if article ID is entered
            if (!EmptyFieldValidation(txtIDArtikla.Text))
            {
                Shared.LogUser("Morate da unesete ID artikla za koji zelite da unesete cenu");
                new MessageBoxCustom("Morate da unesete ID artikla za koji zelite da unesete cenu", MessageType.Error).ShowDialog();
                txtIDArtikla.Focus();
            }
            else
            {
                //Check if price of article is entered
                if (!EmptyFieldValidation(txtCena.Text))
                {
                    Shared.LogUser("Morate da unesete cenu za uneti artikal");
                    new MessageBoxCustom("Morate da unesete cenu za uneti artikal", MessageType.Error).ShowDialog();
                    txtCena.Focus();
                }
                else
                {

                    //Check if in the price of the article, the user put in multiple periods (decimal 
                    if (CheckMultiplePeriod(txtCena.Text))
                    {
                        Shared.LogUser($"Korisnik je uneo vise od jedne tacke u ceni - {txtCena.Text}");
                        new MessageBoxCustom("Ne mozete da unesete vise od jedne tacke u ceni artikla", MessageType.Error).ShowDialog();
                        txtCena.Focus();
                    }
                    else
                    {
                        //Check if article ID is only numbers and has no white spaces
                        if (!CheckIdNumbers(txtIDArtikla.Text) || CheckForWhiteSpace(txtIDArtikla.Text))
                        {
                            Shared.LogUser("ID artikla mogu samo da budu brojevi (obratite paznju da nemate space)");
                            new MessageBoxCustom("ID artikla mogu samo da budu brojevi (obratite paznju da nemate space)", MessageType.Error).ShowDialog();
                            txtIDArtikla.Focus();
                        }
                        else
                        {
                            //Check if article price only has numbers and period and no white spaces
                            if(!CheckOnlyNumOrPeriod(txtCena.Text) || CheckForWhiteSpace(txtCena.Text))
                            {
                                Shared.LogUser("Cena artikla moze samo da bude brojevi i tacka (obratite paznju da nemate space)");
                                new MessageBoxCustom("Cena artikla moze samo da bude brojevi i tacka (obratite paznju da nemate space)", MessageType.Error).ShowDialog();
                                txtCena.Focus();
                            }
                            else
                            {

                                //We ping the SQL server
                                if (!PingServer("192.168.1.46"))
                                {
                                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                                    new MessageBoxCustom("Ne vidim server 192.168.1.46", MessageType.Error).ShowDialog();
                                }
                                else
                                {
                                    //Check if the entered article exists in the BI_ARTIK table
                                    if (!CheckArtikel())
                                    {
                                        Shared.LogUser("Uneti artikal ne postoji u tabeli BI_ARTIK ili nije aktivan");
                                        new MessageBoxCustom("Artikal koji ste uneli nije u bazi artikala ili nije aktivan", MessageType.Error).ShowDialog();
                                        txtIDArtikla.Focus();
                                    }
                                    else
                                    {
                                        //Check if article ID already exists in DB table
                                        //Bc it exists, the price needs to be updated
                                        if (ProveraArtikla(txtIDArtikla.Text))
                                        {
                                            Shared.LogUser($"Uradjen update za artikal {txtIDArtikla.Text} sa cenom {txtCena.Text}");
                                            UpdatePromeniCenu();
                                            txtIDArtikla.Text = "";
                                            txtCena.Text = "";
                                        }
                                        //Article doesnt exit in DB table and needs to be inserted
                                        else
                                        {
                                            Shared.LogUser($"Dodat (insert) artikal {txtIDArtikla.Text} sa cenom {txtCena.Text}");
                                            InsertArticleAndPrice();
                                            txtIDArtikla.Text = "";
                                            txtCena.Text = "";
                                        }
                                    }

                                }

                            }
                        }
                    }


                }
            }



            //RunDListaBtn BUTTON
        }







        //Metod for inserting a new article with price
        private void InsertArticleAndPrice()
        {
            try
            {

                string insertQuery = "insert into BI_CK_RFZO (ARTIKEL,CE_NAB1) " +
                    $"VALUES ('{txtIDArtikla.Text}', {txtCena.Text})";

                sqlConnection = new SqlConnection(connectionString);

                sqlConnection.Open();

                SqlCommand cmd = new SqlCommand(insertQuery, sqlConnection);

                cmd.CommandText = insertQuery;

                cmd.ExecuteNonQuery();

                sqlConnection.Close();



                new MessageBoxCustom($"Dodat je artikal {txtIDArtikla.Text} sa cenom {txtCena.Text}").ShowDialog();

            }
            catch (Exception e)
            {
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }



        //Metod for updating the price of the article
        private void UpdatePromeniCenu()
        {
            try
            {

                string updateQuery = "update BI_CK_RFZO " +
                    $"set CE_NAB1 = {txtCena.Text} " +
                    $"where ARTIKEL = {txtIDArtikla.Text}";

                sqlConnection = new SqlConnection(connectionString);

                sqlConnection.Open();

                SqlCommand cmd = new SqlCommand(updateQuery, sqlConnection);

                cmd.CommandText = updateQuery;

                cmd.ExecuteNonQuery();

                sqlConnection.Close();



                new MessageBoxCustom($"Promenjena je cena artikla {txtIDArtikla.Text}").ShowDialog();

            }
            catch (Exception e)
            {
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }




        //Metod that checks if the article ID exists in the DB table
        //If it exists it returns true, if not then false
        private bool ProveraArtikla(string input)
        {


            //ako je null ne postoji posl
            string? artikal = null;

            try
            {

                string query = "select * " +
                    "from BI_CK_RFZO " +
                    $"where ARTIKEL = {input}";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    artikal = reader.GetValue(0).ToString();

                }
                else
                {
                    artikal = null;

                }

                connection.Close();
            }
            catch (Exception e)
            {
                artikal = null;
                Shared.LogUser("\n" +e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (artikal != null)
                return true;
            else
                return false;

        }


        //Method that checks if the string that is passed in has multiple '.'
        //If it has more then 1 it returns true 
        private bool CheckMultiplePeriod(string input)
        {
            int counter = 0;

            for(int i = 0; i < input.Length; i++)
            {
                if (input[i] == '.')
                {
                    counter++;
                    //early break/return
                    if(counter > 1)
                        return true;
                }
            }

            return false;
        }


        //Check if passed string only has numbers
        //Returns true if its only numbers
        private bool CheckIdNumbers(string input)
        {
            Regex num = new(@"^\d+$");

            if (num.IsMatch(input))
                return true;
            return false;
        }


        //Checks if passed in string has any white spaces
        //Returns true if it has
        private bool CheckForWhiteSpace(string input)
        {
            Regex space = new(@"\s");

            if (space.IsMatch(input))
                return true;
            return false;
        }

        //Check if passed in string only uses numbers and a period
        private bool CheckOnlyNumOrPeriod(string input)
        {
            Regex numPeriod = new(@"^\d*\.?\d*$");

            if (numPeriod.IsMatch(input))
                return true;
            return false;
        }


        //Check if the artikle ID exists in BI_ARTIK table
        //if it exists it returns true
        private bool CheckArtikel()
        {
            //ako je null ne postoji posl
            string? posl = null;

            try
            {

                string query = "select ARTIKEL,AKTIVEN " +
                    "from BI_ARTIK " +
                    $"where ARTIKEL = '{txtIDArtikla.Text}' " +
                    "AND AKTIVEN = 'D'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    posl = reader.GetValue(0).ToString();

                }
                else
                {
                    posl = null;

                }

                connection.Close();
            }
            catch (Exception e)
            {
                posl = null;
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (posl != null)
                return true;
            else
                return false;
        }


        //Gets an ip adress in string form ("192.162.1.46") 
        //returns true or false based on weather it can see it
        private bool PingServer(string s)
        {
            //Za proveru da li imamo konekciju ka serveru
            Ping x = new Ping();
            PingReply reply = x.Send(IPAddress.Parse(s));

            Thread.Sleep(500);

            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }




        //Method for checking if the field is empty
        //Returns true if its not empty, false if it is empty
        private bool EmptyFieldValidation(string input)
        {
            if (input == null || input == string.Empty)
                return false;
            return true;
        }




        //CLASS DLista
    }
}
