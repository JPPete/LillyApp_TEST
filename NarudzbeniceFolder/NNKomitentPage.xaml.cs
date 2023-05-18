using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for NNKomitentPage.xaml
    /// </summary>
    public partial class NNKomitentPage : Page
    {




        public SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;


        public NNKomitentPage()
        {
            InitializeComponent();
            nazivKomitenttxt.Focus();

        }

        private async void NadjiKomitentabtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuo dugme PRONADJI ID komitenta ili poslovnice");

            //Provera da li je korisnik popunio oba polja
            if (nazivKomitenttxt.Text != string.Empty && nazivPoslovnicetxt.Text != string.Empty)
            {
                Shared.LogUser("Mozete da trazite ili komitenta ili poslovnicu! --- OBA POLJA SU PRAZNA");
                new MessageBoxCustom("Mozete da trazite ili komitenta ili poslovnicu!").ShowDialog();
                nazivKomitenttxt.Focus();

                //Provera da li je korisnik ostavio oba mesta prazna
            }
            else if (nazivKomitenttxt.Text == string.Empty && nazivPoslovnicetxt.Text == string.Empty)
            {
                Shared.LogUser($"Mozete da trazite ili komitenta ili poslovnicu! --- OBA POLJA SU POPUNJENA --- komitenta: {nazivKomitenttxt.Text} | poslovnica: {nazivPoslovnicetxt.Text}");
                new MessageBoxCustom("Mozete da trazite ili komitenta ili poslovnicu!").ShowDialog();
                nazivKomitenttxt.Focus();

                //Provera da li je korisnik uneo samo komitenta
            }
            else if (nazivKomitenttxt.Text != string.Empty && nazivPoslovnicetxt.Text == string.Empty)
            {

                #region "FIND KOMITENT

                Shared.LogUser($"Pritisnuto je dugme PRONADJI ID komitenta za naziv {nazivKomitenttxt.Text}");

                var watch = Stopwatch.StartNew();


                //Pinging the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {
                    string query = "select KOMITENT, NAZIV " +
                        "from S_KOMIT " +
                        $"where 1=1 " +
                        $"and NAZIV like '%{nazivKomitenttxt.Text}%' " +
                        "and AKTIVEN = 'D'";

                    LoadingLb.Visibility = Visibility.Visible;
                    LoadingLb.Content = "Tazim podatke...";

                    await GetDataAsync(query);

                    Shared.LogUser("\n" + query);

                    watch.Stop();

                    var elapsed = watch.ElapsedMilliseconds / 1000;

                    LoadingLb.Content = $"Upit je trajao: {elapsed}  sec";
                    Shared.LogUser($"Upit je trajao: {elapsed}  sec");

                }
                else
                {
                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                    new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                }

                #endregion


                //Provera da li je korisnik uneo samo poslovnicu
            }
            else if (nazivKomitenttxt.Text == string.Empty && nazivPoslovnicetxt.Text != string.Empty)
            {

                #region "FIND POSLOVNICA

                Shared.LogUser($"Pritisnuto je dugme PRONADJI ID poslovnice za naziv {nazivPoslovnicetxt.Text}");

                var watch = Stopwatch.StartNew();


                //Pinging the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {
                    string query = "select posl.POSL, concat (ime, ' ' , ime1) as NAZIV, P_NASLOV as ADRESA, P_POSTA as PostanskiBROJ, P_KRAJ as GRAD " +
                        "from BI_POSL posl inner join BI_POSL_KONF konf on posl.POSL = konf.POSL " +
                        "where 1=1  " +
                        "and posl.POSL like '%00' " +
                        $"and concat (IME, ' ' , IME1) like '%{nazivPoslovnicetxt.Text}%' " +
                        "and AKTIVNA = 'D'";


                    LoadingLb.Visibility = Visibility.Visible;
                    LoadingLb.Content = "Tazim podatke...";

                    await GetDataAsync(query);

                    Shared.LogUser("\n" + query);

                    watch.Stop();

                    var elapsed = watch.ElapsedMilliseconds / 1000;

                    LoadingLb.Content = $"Upit je trajao: {elapsed} sec";
                    Shared.LogUser($"Upit je trajao: {elapsed} sec");

                }
                else
                {
                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                    new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                }

                #endregion

            }



        }


        //Pressing enter runs NadjiKomitenabtn
        private void KomitentEnter(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                NadjiKomitentabtn(sender, e);
            }
        }

        




        //Runs the Query that is passed through and shows data in DataGrid
        public async Task GetDataAsync(string query)
        {
            try
            {


                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                //If the query doesnt get excecuted in 2 min Timout Exception will be thrown out
                cmd.CommandTimeout = 120;


                SqlDataAdapter sqlDataAdapter = await Task.Run(() => new SqlDataAdapter(cmd));

                DataTable nnTable = new DataTable();
                await Task.Run(() =>
                {

                    using (sqlDataAdapter)
                    {


                        sqlDataAdapter.Fill(nnTable);


                    }

                });

                NNKomitentDG.ItemsSource = nnTable.DefaultView;


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








        //CLASS NNKomitentPage
    }
}
