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
    /// Interaction logic for AltermedijaPage.xaml
    /// </summary>
    public partial class AltermedijaPage : Page
    {



        public SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;



        public AltermedijaPage()
        {
            InitializeComponent();
            txtPosl.Focus();
        }

        private async void PokreniAMbtn(object sender, RoutedEventArgs e)
        {


            Shared.LogUser($"Pritisnuto dugme POKRENI za proveru Odgovora i Faktura od Altermedije | Poslovnica: '{txtPosl.Text}' | " +
                $"Komitent: '{txtKomitent.Text}'");


            

            //Provera da li je datum od dobro izabran
            if (dtDatumOd.SelectedDate == null || dtDatumOd.SelectedDate > DateTime.Today)
            {
                Shared.LogUser("Morate da izaberete datum od i da ne bude u buducnosti");
                new MessageBoxCustom("Morate da izaberete datum i da ne bude u buducnosti").ShowDialog();
                dtDatumOd.Focus();
            }
            else
            {
                //Provera da li je datum do dobro izabran
                if (dtDatumDo.SelectedDate == null || dtDatumDo.SelectedDate > DateTime.Today)
                {
                    Shared.LogUser("Morate da izaberete datum do i da ne bude u buducnosti");
                    new MessageBoxCustom("Morate da izaberete datum i da ne bude u buducnosti").ShowDialog();
                    dtDatumDo.Focus();
                }
                else
                {
                    //provera da li je datum od manji ili jednak datumu do
                    if (dtDatumOd.SelectedDate > dtDatumDo.SelectedDate)
                    {
                        Shared.LogUser("Datum od mora da bude manji ili jednak datumu do");
                        new MessageBoxCustom("Datum od mora da bude manji ili jednak datumu do").ShowDialog();
                        dtDatumOd.Focus();
                    }
                    else
                    {


                        //Check to see if both fields of Poslovnica and Komitent are left empty,if so
                        //doesnt run the query
                        if (txtPosl.Text == string.Empty && txtKomitent.Text == string.Empty)
                        {
                            Shared.LogUser("Morate uneti makar jedno od sledecih polja: Poslovnica - Komitent");
                            new MessageBoxCustom("Morate uneti makar jedno od sledecih polja: Poslovnica - Komitent").ShowDialog();
                            txtPosl.Focus();
                        }
                        else
                        {

                            //Pre upita ka serveru pingujemo ga
                            if (PingServer("192.168.1.46"))
                            {

                                //If the Poslovnica field is filled
                                if (txtPosl.Text != string.Empty)
                                {

                                    if (!CheckPoslInput(txtPosl.Text))
                                    {
                                        Shared.LogUser($"Pogresno unet broj poslovnice --- poslovnica: {txtPosl.Text}");
                                        new MessageBoxCustom("Pogresno unet broj poslovnice").ShowDialog();
                                        txtPosl.Focus();
                                    }
                                    else
                                    {

                                        //Provera da li uneta poslovnica postoji u bazi
                                        if (!ProveraPosl())
                                        {
                                            Shared.LogUser($"Poslovnica ne postoji u bazi --- poslovnica: {txtPosl.Text}");
                                            new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                                            txtPosl.Focus();
                                        }
                                        else
                                        {
                                            //If komitent box is filled
                                            if (txtKomitent.Text != string.Empty)
                                            {

                                                //Provera da li unet komitent postoji u bazi
                                                if (!ProveraKomitent())
                                                {
                                                    Shared.LogUser($"Komitent ne postoji u bazi --- komitenta: {txtKomitent.Text}");
                                                    new MessageBoxCustom("Komitent ne postoji u bazi", MessageType.Error).ShowDialog();
                                                    txtKomitent.Focus();
                                                }
                                                else
                                                {
                                                    //sve je OK

                                                    //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu

                                                    DateTime datumOd = (DateTime)dtDatumOd.SelectedDate;
                                                    DateTime datumDo = (DateTime)dtDatumDo.SelectedDate;

                                                    string strDatumOd = datumOd.ToString("yyyyMMdd");
                                                    string strDatumDo = datumDo.ToString("yyyyMMdd");


                                                    Shared.LogUser("Pokrecem upit");

                                                    var watch = System.Diagnostics.Stopwatch.StartNew();



                                                    string query = "SELECT ID, T.TIP_DOKUMENTA, T.POSL, concat(posl.IME, ' ', posl.IME1) as IME, T.BR_NARUDZBENICE, T.BR_DOKUMENTA, KO.KOMITENT, KO.NAZIV, VREME, NAPAKA as GRESKA, JSON_DOK " +
                                                        "FROM (  " +
                                                        "select AM.ID, AM.STEVEC, AM.POSL - 10 as POSL, AM.NAPAKA, AM.DATUM AS VREME, JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.docNo') AS BR_DOKUMENTA, " +
                                                        "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.OrdRefNum') AS BR_NARUDZBENICE,  " +
                                                        "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.TIDS') AS PIB, " +
                                                        "CASE  " +
                                                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  1 THEN 'ND - Faktura'  " +
                                                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  2 THEN 'NN'  " +
                                                        "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  3 THEN 'NN_O - Odgovor'  " +
                                                        "END AS TIP_DOKUMENTA, AM.PONOVI, CAST(AM.DATA AS VARCHAR(MAX)) AS JSON_DOK  " +
                                                        "from BI_PRENOS_AM am  " +
                                                        "where 1=1 " +
                                                        $"and AM.POSL - 10 = {txtPosl.Text} " +
                                                        $"and cast(am.DATUM as date) between '{strDatumOd}' and '{strDatumDo}' " +
                                                        ") T left OUTER join S_KOMIT ko on t.PIB = ko.DAVCNAST  " +
                                                        "left OUTER join BI_POSL posl on t.POSL = posl.POSL " +
                                                        $"where ko.KOMITENT = {txtKomitent.Text}";

                                                    LoadingLb.Visibility = Visibility.Visible;
                                                    LoadingLb.Content = "Tazim podatke...";

                                                    await GetDataAsync(query);

                                                    Shared.LogUser("\n" + query);

                                                    watch.Stop();

                                                    var elapsed = watch.ElapsedMilliseconds / 1000;

                                                    LoadingLb.Content = $"Upit je trajao: {elapsed}  sec";
                                                    Shared.LogUser($"Upit je trajao: {elapsed} sec");

                                                }

                                            }//IF THERE IS NO KOMITENT ENTERED
                                            else
                                            {

                                                //sve je OK

                                                //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu

                                                DateTime datumOd = (DateTime)dtDatumOd.SelectedDate;
                                                DateTime datumDo = (DateTime)dtDatumDo.SelectedDate;

                                                string strDatumOd = datumOd.ToString("yyyyMMdd");
                                                string strDatumDo = datumDo.ToString("yyyyMMdd");


                                                Shared.LogUser("Pokrecem upit");

                                                var watch = System.Diagnostics.Stopwatch.StartNew();



                                                string query = "SELECT ID, T.TIP_DOKUMENTA, T.POSL, concat(posl.IME, ' ', posl.IME1) as IME, T.BR_NARUDZBENICE, T.BR_DOKUMENTA, KO.KOMITENT, KO.NAZIV, VREME, NAPAKA as GRESKA, JSON_DOK " +
                                                    "FROM (  " +
                                                    "select AM.ID, AM.STEVEC, AM.POSL - 10 as POSL, AM.NAPAKA, AM.DATUM AS VREME, JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.docNo') AS BR_DOKUMENTA, " +
                                                    "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.OrdRefNum') AS BR_NARUDZBENICE,  " +
                                                    "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.TIDS') AS PIB, " +
                                                    "CASE  " +
                                                    "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  1 THEN 'ND - Faktura'  " +
                                                    "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  2 THEN 'NN'  " +
                                                    "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  3 THEN 'NN_O - Odgovor'  " +
                                                    "END AS TIP_DOKUMENTA, AM.PONOVI, CAST(AM.DATA AS VARCHAR(MAX)) AS JSON_DOK  " +
                                                    "from BI_PRENOS_AM am  " +
                                                    "where 1=1 " +
                                                    $"and AM.POSL - 10 = {txtPosl.Text} " +
                                                    $"and AM.DATUM between '{strDatumOd}' and '{strDatumDo}' " +
                                                    ") T left OUTER join S_KOMIT ko on t.PIB = ko.DAVCNAST  " +
                                                    "left OUTER join BI_POSL posl on t.POSL = posl.POSL ";

                                                LoadingLb.Visibility = Visibility.Visible;
                                                LoadingLb.Content = "Tazim podatke...";

                                                await GetDataAsync(query);

                                                Shared.LogUser("\n" + query);

                                                watch.Stop();

                                                var elapsed = watch.ElapsedMilliseconds / 1000;

                                                LoadingLb.Content = $"Upit je trajao: {elapsed}  sec";
                                                Shared.LogUser($"Upit je trajao: {elapsed} sec");

                                            }


                                        }

                                    }



                                }//If the Poslovnica field is empty
                                else
                                {

                                    //Provera da li unet komitent postoji u bazi
                                    if (!ProveraKomitent())
                                    {
                                        Shared.LogUser($"Komitent ne postoji u bazi --- komitenta: {txtKomitent.Text}");
                                        new MessageBoxCustom("Komitent ne postoji u bazi", MessageType.Error).ShowDialog();
                                        txtKomitent.Focus();
                                    }
                                    else
                                    {
                                        //sve je OK

                                        //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu

                                        DateTime datumOd = (DateTime)dtDatumOd.SelectedDate;
                                        DateTime datumDo = (DateTime)dtDatumDo.SelectedDate;

                                        string strDatumOd = datumOd.ToString("yyyyMMdd");
                                        string strDatumDo = datumDo.ToString("yyyyMMdd");


                                        Shared.LogUser("Pokrecem upit");

                                        var watch = System.Diagnostics.Stopwatch.StartNew();



                                        string query = "SELECT ID, T.TIP_DOKUMENTA, T.POSL, concat(posl.IME, ' ', posl.IME1) as IME, T.BR_NARUDZBENICE, T.BR_DOKUMENTA, KO.KOMITENT, KO.NAZIV, VREME, NAPAKA as GRESKA, JSON_DOK " +
                                            "FROM (  " +
                                            "select AM.ID, AM.STEVEC, AM.POSL - 10 as POSL, AM.NAPAKA, AM.DATUM AS VREME, JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.docNo') AS BR_DOKUMENTA, " +
                                            "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.OrdRefNum') AS BR_NARUDZBENICE,  " +
                                            "JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.TIDS') AS PIB, " +
                                            "CASE  " +
                                            "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  1 THEN 'ND - Faktura'  " +
                                            "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  2 THEN 'NN'  " +
                                            "WHEN JSON_VALUE(CAST(AM.DATA AS VARCHAR(MAX)), '$.type') =  3 THEN 'NN_O - Odgovor'  " +
                                            "END AS TIP_DOKUMENTA, AM.PONOVI, CAST(AM.DATA AS VARCHAR(MAX)) AS JSON_DOK  " +
                                            "from BI_PRENOS_AM am  " +
                                            "where 1=1 " +
                                            $"and cast(am.DATUM as date) between '{strDatumOd}' and '{strDatumDo}' " +
                                            ") T left OUTER join S_KOMIT ko on t.PIB = ko.DAVCNAST  " +
                                            "left OUTER join BI_POSL posl on t.POSL = posl.POSL " +
                                            $"where ko.KOMITENT = {txtKomitent.Text}";

                                        LoadingLb.Visibility = Visibility.Visible;
                                        LoadingLb.Content = "Tazim podatke...";

                                        await GetDataAsync(query);

                                        Shared.LogUser("\n" + query);

                                        watch.Stop();

                                        var elapsed = watch.ElapsedMilliseconds / 1000;

                                        LoadingLb.Content = $"Upit je trajao: {elapsed}  sec";
                                        Shared.LogUser($"Upit je trajao: {elapsed} sec");

                                    }


                                }


                            }
                            else
                            {
                                Shared.LogUser("Ne pingujem server 192.168.1.46");
                                new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                            }

                        }

                        
                    }
                }
            }

            



            //DUGME PokreniAMbtn
        }


        //Dugme za prikazivanje koji provajder je za kog dobavljaca
        private async void DobNaEDIbtn(object sender, RoutedEventArgs e)
        {


            Shared.LogUser("pritisnuo dugme Provajderi za EDI");

            //Pinging the server to see if we can access the database
            if (PingServer("192.168.1.46"))
            {
                Shared.LogUser("Pokrecem upit");

                var watch = System.Diagnostics.Stopwatch.StartNew();

                string query = "select a.KOMITENT, b.NAZIV, " +
                    "CASE " +
                    "WHEN a.EDI_PROV = 'AM' THEN 'ALTERMEDIA' " +
                    "WHEN a.EDi_PROV = 'P' THEN 'PANTEON' " +
                    "WHEN a.EDI_PROV = '8BIT' THEN '8BIT' " +
                    "END AS PROVAJDER " +
                    "from bi_komit_e_dokum a " +
                    "inner join s_komit b on a.komitent = b.komitent " +
                    "order by a.edi_prov";


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

            //DobNaEDIbtn DUGME
        }




        #region "Key up events
        private void PoslEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                dtDatumOd.Focus();
            }
        }

        private void DatumOdEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                dtDatumDo.Focus();
            }
        }

        private void DatumDoEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                txtKomitent.Focus();
            }
        }

        private void KomitentEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                PokreniAMbtn(sender, e);
            }
        }

        #endregion





        //Validation da se unose samo brojevi u textbox
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }


        //Metoda koja proverava da li postoji uneta poslovnica u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraPosl()
        {
            //ako je null ne postoji posl
            string? posl = null;

            try
            {

                string query = $"select POSL FROM BI_POSL WHERE POSL = {txtPosl.Text} and AKTIVNA = 'D'";

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
            catch (Exception ex)
            {
                posl = null;
                Shared.LogUser("\n" + ex.ToString() + "\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (posl != null)
                return true;
            else
                return false;

        }



        //Metoda koja proverava da li postoji unet komitent u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraKomitent()
        {
            //ako je null ne postoji posl
            string? komitent = null;

            try
            {

                string query = $"select KOMITENT from S_KOMIT where KOMITENT = {txtKomitent.Text} and AKTIVEN = 'D'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    komitent = reader.GetValue(0).ToString();

                }
                else
                {
                    komitent = null;

                }

                connection.Close();
            }
            catch (Exception ex)
            {
                komitent = null;
                Shared.LogUser("\n" + ex.ToString() + "\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (komitent != null)
                return true;
            else
                return false;

        }



        //Regex that checks Poslovnica input
        private bool CheckPoslInput(string s)
        {
            Regex regex = new Regex(@"2\d\d\d00");
            return regex.IsMatch(s);

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

                AMuPoslDG.ItemsSource = nnTable.DefaultView;

                




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

        

        private void DG_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }









        //CLASS AltermedijaPage
    }
}
