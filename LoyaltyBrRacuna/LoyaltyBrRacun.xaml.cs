using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for LoyaltyBrRacun.xaml
    /// </summary>
    public partial class LoyaltyBrRacun : Window
    {

        public SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;


        public LoyaltyBrRacun()
        {
            InitializeComponent();
            txtBrRacuna.Focus();
        }




        private async void FindBrKarticeBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuto dugme Pronadji");

            txtBrLoyal.Text = "";
            dgVrstePlacanja.ItemsSource = null;

            //checks if the field is empty
            if (!EmptyFieldValidation(txtBrRacuna.Text))
            {
                Shared.LogUser("Molim Vas unesite broj racuna.");
                new MessageBoxCustom("Molim Vas unesite broj racuna.").ShowDialog();
            }
            else
            {
                string? brKartice = await GetKodaKartice(txtBrRacuna.Text);

                //Ako na racunu nema broj kartice izbacuje poruku
                if (brKartice == string.Empty || brKartice == null)
                {
                    Shared.LogUser($"Na racun {txtBrRacuna.Text} nije povezana loyalty kartica");
                    new MessageBoxCustom($"Na racun {txtBrRacuna.Text} nije povezana loyalty kartica").ShowDialog();
                }
                else
                {
                    txtBrLoyal.Text = brKartice;
                    Shared.LogUser($"Prikazujem broj kartice {brKartice} sa racuna {txtBrRacuna.Text}");
                }

                

                await LillyVrstePlacanja();


            }

            

        }





        //When you press enter on broj racuna text box focus is on Pronadji button
        private void EnterUpBrRacuna(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnFindKartica.Focus();
                FindBrKarticeBtn(sender,e);
            }
        }


        //Validation da se unose samo brojevi u textbox
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);

        }


        //Metoda koja Uzima broj racuna i vraca string (vrednost broja loyalty kartice)
        public async Task<string?> GetKodaKartice(string brRacuna)
        {


            string? brKartice = "";

            //Ako nije unet broj racuna ne pokrece se upit
            if (brRacuna == string.Empty)
                return brKartice;

            try
            {
                string query = "select KODA_KARTICE " +
                    "from BIP_PROM " +
                    "where KODA = 'BP' " +
                    $"and ST_DOKUM = {brRacuna} " +
                    "ORDER BY DATUM DESC,URA DESC";

                sqlConnection = new SqlConnection(connectionString);

                await Task.Run(() => sqlConnection.Open() );

                Shared.LogUser($"Pokrecem upit:\n{query}");

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                if (reader.Read())
                {
                    brKartice = reader.GetValue(0).ToString();
                    Shared.LogUser($"Pronadjen je broj kartice {brKartice} za racun {brRacuna}");
                }
                else
                {
                    Shared.LogUser("Nije pronadjen broj loyalty kartice");
                    new MessageBoxCustom("Nije pronadjen broj loyalty kartice").ShowDialog();
                }


            }
            catch (Exception ex)
            {

                Shared.LogUser(ex.ToString());
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }


            return brKartice;
        }


        //Metoda koja vraca lilly platne kartice
        public async Task LillyVrstePlacanja()
        {

            try
            {
                string query = "select plac.NAC_PLAC as NAC_PLAC, OPIS, plac.ZNESEK as IZNOS, plac.BON as KARTICA " +
                    "from BIP_PROM prom " +
                    "inner join BIP_PLAC plac on prom.POSL = plac.POSL and prom.STEVEC = plac.STEVEC " +
                    "inner join BI_NACPLAC nac on prom.NACPLACILA = nac.NAC_PLAC " +
                    "where 1=1 " +
                    "and KODA = 'BP' " +
                    $"and ST_DOKUM = {txtBrRacuna.Text}";


                sqlConnection = new SqlConnection(connectionString);

                await Task.Run(() => sqlConnection.Open());

                Shared.LogUser($"Pokrecem upit:\n{query}");

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                //SqlDataAdapter sqlDataAdapter = await Task.Run(() => new SqlDataAdapter(cmd));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());


                DataTable dtVrstePlac = new();

                dtVrstePlac.Columns.Add("VRSTA KARTICE");
                dtVrstePlac.Columns.Add("BROJ KARTICE");
                dtVrstePlac.Columns.Add("IZNOS");


                while (reader.Read())
                {
                    //41_Lilly Cvetici | 15_Gift Kartica | 07_Kupon poverenja | 06_Poklon Cestitka
                    //Izbacen 41 jer ne prikazuje broj kartice u placanju, a hvata je kada se poveze na racun tako da nije potrebno ovde
                    if (reader["NAC_PLAC"].ToString() == "06" || reader["NAC_PLAC"].ToString() == "07" || reader["NAC_PLAC"].ToString() == "15")
                    {

                        if(reader["NAC_PLAC"].ToString() == "15")
                        {
                            string brGift = await GetGiftNumber();

                            dtVrstePlac.Rows.Add(reader["OPIS"].ToString(), brGift, reader["IZNOS"].ToString());
                        }
                        else
                        {
                            dtVrstePlac.Rows.Add(reader["OPIS"].ToString(), reader["KARTICA"].ToString(), reader["IZNOS"].ToString());
                        }


                        
                    }
                }

                dgVrstePlacanja.ItemsSource = dtVrstePlac.DefaultView;

            }
            catch (Exception ex)
            {
                Shared.LogUser("\n" + ex.ToString() + "\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }




        }



        public async Task<string> GetGiftNumber()
        {
            string query = "select cardNo " +
                "from SINHRONIZACIJA.dbo.GIFT_KARTICE k " +
                "inner join #requests r on k.requestId = r.ID_TRANSAKCIJE collate SQL_Latin1_General_CP1_CI_AS";

            string brGift = "";



            try
            {

                string tmpTableQuery = "drop table if exists #requests " +
                    "select plac.NAC_PLAC as NAC_PLAC, OPIS, plac.ZNESEK as IZNOS, plac.BON as KARTICA, plac.ID_TRANSAKCIJE " +
                    "into #requests " +
                    "from BIP_PROM prom " +
                    "inner join BIP_PLAC plac on prom.POSL = plac.POSL and prom.STEVEC = plac.STEVEC " +
                    "inner join BI_NACPLAC nac on prom.NACPLACILA = nac.NAC_PLAC " +
                    "where 1=1 " +
                    "and KODA = 'BP' " +
                    $"and ST_DOKUM = {txtBrRacuna.Text}";

                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                sqlConnection.Open();

                SqlCommand cmd = await Task.Run(() => new SqlCommand(tmpTableQuery, sqlConnection));

                cmd.CommandText = tmpTableQuery;

                cmd.ExecuteNonQuery();



                cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());


                while (reader.Read())
                {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    brGift = reader["cardNo"].ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                }



                sqlConnection.Close();

            }
            catch (Exception e)
            {
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }


            return brGift;


        }



        //Method for checking if the field is empty
        //Returns true if its not empty, false if it is empty
        private bool EmptyFieldValidation(string input)
        {
            if (input == null || input == string.Empty)
                return false;
            return true;
        }


        



        //Class LoyaltyBrRacuna
    }
}
