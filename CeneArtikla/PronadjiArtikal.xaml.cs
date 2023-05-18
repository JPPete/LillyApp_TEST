using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for PronadjiArtikal.xaml
    /// </summary>
    public partial class PronadjiArtikal : Window
    {


        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;



        public PronadjiArtikal()
        {
            InitializeComponent();
        }






        private void EnterUpNazivArtikla(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return) 
            {
                FindArticleBtn(sender, e);
            }
        }


        private void EnterUpNazivPosl(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FindPoslBtn(sender, e);
            }
        }





        #region "BUTTONS"

        private async void FindArticleBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("pritisnuo dugme FindArticleBtn");



            if (!AtLeastThreeLong(txtNazivArtikla.Text))
            {
                Shared.LogUser($"Unet naziv za pretragu nije dovoljno dugacal, mora da ima makar 3 karaktera - {txtNazivArtikla.Text}");
                new MessageBoxCustom($"Unet naziv za pretragu nije dovoljno dugacal, mora da ima makar 3 karaktera - {txtNazivArtikla.Text}", MessageType.Error).ShowDialog();

            }
            else
            {
                //Ping the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {

                    string nazivArtikla = ReplaceStar(txtNazivArtikla.Text);

                    Shared.LogUser($"Zamenjene zvezdice sa procentom, pretraga po: {nazivArtikla}");

                    string query = "select ARTIKEL, EAN, concat(OP_AR1, ' ', OP_AR2) as NAZIV " +
                        "from BI_ARTIK " +
                        $"WHERE concat(OP_AR1, ' ', OP_AR2) like '%{nazivArtikla}%'" +
                        "ORDER BY NAZIV";


                    Shared.LogUser($"Pokrecem upit:\n{query}");

                    await GetDataAsync(query);


                }
                else
                {
                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                    new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                }
            }



        }//FindArticleBtn



        private async void FindPoslBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("pritisnuo dugme FindPoslBtn");


            //Ping the server to see if we can access the database
            if (PingServer("192.168.1.46"))
            {

                string nazivPosl = ReplaceStar(txtNazivPosl.Text);

                Shared.LogUser($"Zamenjene zvezdice sa procentom, pretraga po: {nazivPosl}");

                string query = "select posl.POSL, concat (ime, ' ' , ime1) as NAZIV, P_NASLOV as ADRESA, P_POSTA as PostanskiBROJ, P_KRAJ as GRAD " +
                        "from BI_POSL posl inner join BI_POSL_KONF konf on posl.POSL = konf.POSL " +
                        "where 1=1  " +
                        "and posl.POSL like '%00' " +
                        $"and concat (IME, ' ' , IME1) like '%{nazivPosl}%' " +
                        "and AKTIVNA = 'D'";

                Shared.LogUser($"Pokrecem upit:\n{query}");

                await GetDataAsync(query);


            }
            else
            {
                Shared.LogUser("Ne pingujem server 192.168.1.46");
                new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
            }



        }//FindPoslBtn

        #endregion


        #region "METODE"


        //Menja * sa % u prosledjenom stringu i vraca novi string
        //Ovo se koristi za pretragu naziva da se formatuje za SQL
        private string ReplaceStar(string text)
        {
            string naziv = text.Replace('*', '%');
            return naziv;
        }


        
        //private async Task<string> GetZaliha()
        //{
        //    string poslZal = txtNazivPosl.Text.Remove(3,2) + "10";
            



        //    try
        //    {
                


        //        SqlConnection conn = await Task.Run(() => new SqlConnection(connectionString));

        //        SqlCommand cmd = await Task.Run(() => new SqlCommand)
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}





        //Runs the Query that is passed through and shows data in DataGrid
        public async Task GetDataAsync(string query)
        {
            try
            {

                SqlConnection sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                cmd.CommandTimeout = 90;


                SqlDataAdapter adapter = await Task.Run(() => new SqlDataAdapter(cmd));

                DataTable dataTable = new DataTable();

                await Task.Run(() =>
                {
                    using (adapter)
                    {
                        adapter.Fill(dataTable);
                    }
                });

                dgArtikalDetails.ItemsSource = dataTable.DefaultView;

            }
            catch (Exception ex)
            {

                Shared.LogUser($"\n{ex}\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
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




        //Provera da li je uneti string duzine makar 3 karaktera
        private bool AtLeastThreeLong(string s)
        {
            if (s.Length >= 3)
                return true;
            else return false;
        }




        

        #endregion

        
    }//class
}
