using System;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for ProveraNNPage.xaml
    /// </summary>
    public partial class ProveraNNPage : Page
    {



        public SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;


        public ProveraNNPage()
        {
            InitializeComponent();
            brNNtxt.Focus();
        }

        //Validation da se unose samo brojevi u textbox
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }


        //Dugme "Proveri NN i vezane dokumente"
        private async void ProveraNNbtn(object sender, RoutedEventArgs e)
        {



            Shared.LogUser("pritisnuo dugme Proveri NN i vezane dokumente");

            

            //Checking if the field for number of order is empty
            if (!EmptyFieldValidation(brNNtxt.Text))
            {
                Shared.LogUser($"Nije unet broj narudzbenice. Polje za broj je {brNNtxt.Text}");
                new MessageBoxCustom("Morate uneti broj narudzbenice!").ShowDialog();
            }
            else
            {
                StrpljenjeMsg();

                //Pinging the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {
                    Shared.LogUser("Porecem upit");

                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    string query = "select STEVEC, POSL, KODA as TIP_DOKUMENTA, ST_DOKUM as BR_DOKUMENTA, prom.KOMITENT,komit.NAZIV, PREJEMNIK as PROGRAM, prom.TIP_ODPIS, prom.UPORABNIK as KORISNIK, DATUM,URA as VREME, VEZA_NN, SK_ZNESEK as IZNOS " +
                        "from BIP_PROM prom inner join s_komit komit on prom.KOMITENT = komit.KOMITENT " +
                        $"where  1=1 and VEZA_NN = {brNNtxt.Text} order by DATUM,URA";

                    LoadingLb.Visibility = Visibility.Visible;
                    LoadingLb.Content = "Tazim podatke...";

                    await GetDataAsync(query);

                    Shared.LogUser($"\n{query}");

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
            }

            


            //ProveraNNbtn DUGME
        }





        //Dugme "Artikli na NN i prihvacene stavke"
        private async void Artiklibtn(object sender, RoutedEventArgs e)
        {


            Shared.LogUser("pritisnuo dugme Artikli na NN i prihvacene stavke");


            //Checking if the field for number of order is empty
            if (!EmptyFieldValidation(brNNtxt.Text))
            {
                Shared.LogUser($"Nije unet broj narudzbenice. Polje za broj je {brNNtxt.Text}");
                new MessageBoxCustom("Morate uneti broj narudzbenice!").ShowDialog();
            }
            else
            {
                //Pinging the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {
                    Shared.LogUser("Pokrecem upit");

                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    string query = "select ppro.POZICIJA, ppro.ARTIKEL as ARTIKAL, ppro.EAN as BARKOD, CONCAT(OP_AR1, ' ', OP_AR2) as NAZIV, KOLICINA, KOLICINA_RZ as PRIHVACENO, ST_DOKUM as BR_DOKUMENTA " +
                        "from BIP_PPRO ppro inner join BIP_PPRODOD pprodod on ppro.posl = pprodod.posl and ppro.STEVEC = pprodod.STEVEC and ppro.POZICIJA = pprodod.POZICIJA " +
                        "inner join BI_ARTIK art on ppro.ARTIKEL = art.ARTIKEL " +
                        "where 1=1 and KODA = 'NN' " +
                        $"and ST_DOKUM = {brNNtxt.Text} " +
                        "order by ppro.POZICIJA";


                    LoadingLb.Visibility = Visibility.Visible;
                    LoadingLb.Content = "Tazim podatke...";

                    await GetDataAsync(query);

                    Shared.LogUser($"\n{query}");

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
            }

            

            //Artiklibtn DUGME
        }





        //Dugme "Dokumenti Altermedije"
        private async void AMbtn(object sender, RoutedEventArgs e)
        {

            Shared.LogUser("pritisnuo dugme Artikli na NN i prihvacene stavke");

            


            //Checking if the field for number of order is empty
            if (!EmptyFieldValidation(brNNtxt.Text))
            {
                Shared.LogUser($"Nije unet broj narudzbenice. Polje za broj je {brNNtxt.Text}");
                new MessageBoxCustom("Morate uneti broj narudzbenice!").ShowDialog();
            }
            else
            {

                StrpljenjeMsg();

                //Pinging the server to see if we can access the database
                if (PingServer("192.168.1.46"))
                {
                    Shared.LogUser("Pokrecem upit");

                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    string query = "SELECT ID, T.TIP_DOKUMENTA, T.POSL, concat(posl.IME, ' ', posl.IME1) as IME, T.BR_NARUDZBENICE, T.BR_DOKUMENTA, KO.KOMITENT, KO.NAZIV, VREME, NAPAKA, JSON_DOK " +
                        "FROM ( " +
                        "select AM.ID, AM.STEVEC, AM.POSL - 10 as POSL, AM.NAPAKA, AM.DATUM AS VREME, JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.docNo') AS BR_DOKUMENTA, " +
                        "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.OrdRefNum') AS BR_NARUDZBENICE, " +
                        "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.TIDS') AS PIB, " +
                        "CASE " +
                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  1 THEN 'ND - Faktura' " +
                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  2 THEN 'NN' " +
                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  3 THEN 'NN_O - Odgovor' " +
                        "END AS TIP_DOKUMENTA, AM.PONOVI, CAST(AM.DATA AS VARCHAR(MAX)) AS JSON_DOK " +
                        "from BI_PRENOS_AM am " +
                        $"where JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.OrdRefNum') = '{brNNtxt.Text}' " +
                        ") T left OUTER join S_KOMIT ko on t.PIB = ko.DAVCNAST " +
                        "left OUTER join BI_POSL posl on t.POSL = posl.POSL ORDER BY VREME";




                    LoadingLb.Visibility = Visibility.Visible;
                    LoadingLb.Content = "Tazim podatke...";


                    await GetDataAsync(query);


                    Shared.LogUser($"\n{query}");

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
            }

            //AMbtn DUGME
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

                NarudzbeniceDG.ItemsSource = nnTable.DefaultView;


            }
            catch (Exception ex)
            {
                Shared.LogUser($"\n{ex}\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();


            }


        }



        //Shows a msg box that explains that it takes time to get data
        public void StrpljenjeMsg()
        {
            Shared.LogUser("Prikazujem poruku za strpljenje");
            new MessageBoxCustom("Prikupljanje podataka moze da potraje, hvala na strpljenju").ShowDialog();

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

        private void DG_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }











        //CALSS ProveraNNPage
    }
}
