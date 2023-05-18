using System;
using System.Data;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using static LillyApp_TEST.MessageBoxCustom;
using System.Threading.Tasks;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for ZatBlag.xaml
    /// </summary>
    public partial class ZatBlag : Window
    {



        SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodEditConnString;

        //!!!!!!!!!!!!!!! ZA TEST !!!!!!!!!!!!!!!!!!
        //public string connectionString = Shared.testConnString;


        public ZatBlag()
        {
            InitializeComponent();
            txtPoslovnica.Focus();
        }

        //Validation da se unose samo brojevi u textbox
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);

        }





        //Dugme PROVERA
        private async void Provera_Click(object sender, RoutedEventArgs e)
        {
            //Ovo koristimo za proveru izabranog datuma
            DateTime now = DateTime.Now;


            string logString = $"je pritisnuo dugme PROVERA za | poslovnicu: {txtPoslovnica.Text} | datum: {dtDatum.SelectedDate} | blagajnu: {txtBlag.Text} |";
            Shared.LogUser(logString);

            lbResult.Content = "Pokrecem proveru...";

            //provera da li je dobro uneta poslovnica
            if (!CheckPoslInput(txtPoslovnica.Text))
            {
                Shared.LogUser("Pogresno unet broj poslovnice");
                new MessageBoxCustom("Pogresno unet broj poslovnice").ShowDialog();
                txtPoslovnica.Focus();
            }
            else
            {
                //Provera da li je datum izabran
                if (dtDatum.SelectedDate == null || dtDatum.SelectedDate >= DateTime.Today)
                {
                    Shared.LogUser("Morate da izaberete datum i da bude pre danasnjeg");
                    new MessageBoxCustom("Morate da izaberete datum i da bude pre danasnjeg").ShowDialog();
                    dtDatum.Focus();
                }
                else
                {
                    //Izabrani datum ne sme da bude stariji od 7 dana
                    if (dtDatum.SelectedDate < now.AddDays(-8))
                    {
                        Shared.LogUser("Izabrani datum ne sme da bude stariji od 7 dana");
                        new MessageBoxCustom("Izabrani datum ne sme da bude stariji od 7 dana").ShowDialog();
                        dtDatum.Focus();
                    }
                    else
                    {
                        //da li je dobro uneta blagajna
                        if (!CheckBlagInput(txtBlag.Text))
                        {
                            Shared.LogUser("Blagajna nije uneta kako treba");
                            new MessageBoxCustom("Blagajna nije uneta kako treba").ShowDialog();
                            txtBlag.Focus();
                        }
                        else
                        {
                            //Ako je sve uneto ok
                            if (CheckPoslInput(txtPoslovnica.Text) && dtDatum.SelectedDate != null && dtDatum.SelectedDate < DateTime.Today && dtDatum.SelectedDate > now.AddDays(-8) && CheckBlagInput(txtBlag.Text))
                            {
                                //pre pokretanja upita ka serveru pingujemo ga
                                if (PingServer("192.168.1.46"))
                                {
                                    //ako uneta poslovnica ne postoji u bazi
                                    if (!ProveraPosl())
                                    {
                                        Shared.LogUser("Poslovnica ne postoji u bazi");
                                        new MessageBoxCustom("Poslovnica ne postoji u bazi").ShowDialog();
                                        txtPoslovnica.Focus();
                                    }
                                    //Poslovnica postoji u bazi
                                    else
                                    {
                                        //ako blagajna ne postoji u bazi ili u toj poslovnici
                                        if (!await ProveraBlag())
                                        {
                                            Shared.LogUser("Blagajna ne postoji u bazi ili u toj poslovnici");
                                            new MessageBoxCustom("Blagajna ne postoji u bazi ili u toj poslovnici").ShowDialog();
                                            txtBlag.Focus();
                                        }
                                        //ako je i blagajna i poslovnica u bazi
                                        else
                                        {
                                            //DateTime datum = (DateTime)dtDatum.SelectedDate;
                                            //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu
                                            //string strDatum = datum.ToString("yyyyMMdd");
                                            string strDatum = dtDatum.SelectedDate.Value.ToString("yyyyMMdd");


                                            string? ipKase = await GetBlagIP();

                                            if (ipKase == null)
                                            {
                                                Shared.LogUser("IP BLAG JE NULL");
                                                new MessageBoxCustom("IP BLAG JE NULL", MessageType.Error).ShowDialog();
                                            }
                                            else
                                            {
                                                //ako ne pingujemo kasu
                                                if (!PingServer(ipKase))
                                                {
                                                    Shared.LogUser("Ne pingujem kasu!");
                                                    new MessageBoxCustom("Ne pingujem kasu!").ShowDialog();
                                                    lbResult.Content = "Ne pingujem kasu!";
                                                    lbResult.FontWeight = FontWeights.Bold;
                                                    lbResult.FontSize = 25;
                                                    lbResult.Foreground = Brushes.Red;
                                                }
                                                //ako pingujemo kasu
                                                else
                                                {
                                                    #region "Ovo treba ubaciti u ping kase"


                                                    try
                                                    {
                                                        //Provera da li su odhodi ok
                                                        if (await CheckOdhodi())
                                                        {
                                                            Shared.LogUser("Odhodi su OK");
                                                            lbResult.Content = "Odhodi su OK :)";
                                                            lbResult.FontWeight = FontWeights.Normal;
                                                            lbResult.FontSize = 20;
                                                            BrushConverter bc = new BrushConverter();
                                                            lbResult.Foreground = bc.ConvertFrom("#F4B41A") as Brush;


                                                            //prikazi podatke blagajni
                                                            await GetDataFromDB();

                                                            //Ako blagajna nije vec zatvorena
                                                            if (await ProveraOdprtaOtpis(strDatum))
                                                            {
                                                                Shared.LogUser("Provera je prosla kako treba - prikazujem dugme ZATVORI");
                                                                //Pokazi dugme zatvori
                                                                btnZatvori.Visibility = Visibility.Visible;

                                                            }
                                                            else
                                                            {
                                                                Shared.LogUser("Blagajna je vec zatvorena");
                                                                new MessageBoxCustom("Blagajna je vec zatvorena").ShowDialog();
                                                            }



                                                        }
                                                        //Ako odhodi imaju vise od 2 file-a
                                                        else
                                                        {
                                                            Shared.LogUser("Odhodi nisu prazni, resetuj sepis");
                                                            lbResult.Content = "Odhodi nisu prazni, resetuj sepis";
                                                            lbResult.FontWeight = FontWeights.Bold;
                                                            lbResult.FontSize = 25;
                                                            lbResult.Foreground = Brushes.Red;

                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Shared.LogUser("\n" + ex.ToString());
                                                        new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                                                    }


                                                    

                                                    #endregion
                                                }
                                            }



                                        }//else od provere blagajne i poslovnice

                                    }//else od provere poslovnice

                                }
                                else
                                {
                                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                                    new MessageBoxCustom("Ne pingujem server 192.168.1.46").ShowDialog();
                                }



                            }//sve je ok

                        }

                    }

                }
            }



        }//Dugme Provera






        //Dugme ZATVORI !
        private async void Zatvori_Click(object sender, RoutedEventArgs e)
        {

            string logString = $"je pritisnuo dugme PROVERA za | poslovnicu: {txtPoslovnica.Text} | datum: {dtDatum.SelectedDate} | blagajnu: {txtBlag.Text} |";
            Shared.LogUser(logString);

            //Provera da korisnik jos jednom potvrdi da zeli da zatvori blagajnu
            bool? result = new MessageBoxCustom("Da li ste sigurni da zelite da zatvorite blagajnu?", MessageType.Confirmation).ShowDialog();

            Shared.LogUser("Da li ste sigurni da zelite da zatvorite blagajnu?");

            switch (result.Value)
            {
                case true:
                    Shared.LogUser($"odgovor korisnika:\tDA");
                    break;
                case false:
                    Shared.LogUser($"odgovor korisnika:\tNE");
                    break;
            }

            //Shared.LogUser($"odgovor korisnika:\t{result}");

            //No, cancel i izlaz svi treba da prekinu proces.
            //Samo Yes zatvara
            if (result.Value && result != null)
            {
                
                    #region "ovo ubaciti pod Da li ste sigurni"

                    //Ovo koristimo za proveru izabranog datuma
                    DateTime now = DateTime.Now;

                    //provera da li je dobro uneta poslovnica
                    if (!CheckPoslInput(txtPoslovnica.Text))
                    {
                        Shared.LogUser("Pogresno unet broj poslovnice");
                        new MessageBoxCustom("Pogresno unet broj poslovnice").ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    //Provera da li je datum izabran
                    if (dtDatum.SelectedDate == null || dtDatum.SelectedDate >= DateTime.Today)
                    {
                        Shared.LogUser("Morate da izaberete datum i da bude pre danasnjeg");
                        new MessageBoxCustom("Morate da izaberete datum i da bude pre danasnjeg").ShowDialog();
                        dtDatum.Focus();
                    }
                    //Izabrani datum ne sme da bude stariji od 3 meseca
                    if (dtDatum.SelectedDate < now.AddDays(-8))
                    {
                        Shared.LogUser("Izabrani datum ne sme da bude stariji od 7 dana");
                        new MessageBoxCustom("Izabrani datum ne sme da bude stariji od 7 dana").ShowDialog();
                        dtDatum.Focus();
                    }
                    //da li je dobro uneta blagajna
                    if (!CheckBlagInput(txtBlag.Text))
                    {
                        Shared.LogUser("Blagajna nije uneta kako treba");
                        new MessageBoxCustom("Blagajna nije uneta kako treba").ShowDialog();
                        txtBlag.Focus();
                    }
                    //Ako je sve ok
                    if (CheckPoslInput(txtPoslovnica.Text) && dtDatum.SelectedDate != null && dtDatum.SelectedDate < DateTime.Today && dtDatum.SelectedDate > now.AddDays(-8) && CheckBlagInput(txtBlag.Text))
                    {
                        if (!ProveraPosl())
                        {
                            Shared.LogUser("Poslovnica ne postoji u bazi");
                            new MessageBoxCustom("Poslovnica ne postoji u bazi").ShowDialog();
                            txtPoslovnica.Focus();
                        }
                        //Poslovnica postoji u bazi
                        else
                        {

                            //pre pokretanja upita ka serveru pingujemo ga
                            if (PingServer("192.168.1.46"))
                            {
                                #region "pinguj server pre"
                                if (!await ProveraBlag())
                                {
                                    Shared.LogUser("Blagajna ne postoji u bazi ili u toj poslovnici");
                                    new MessageBoxCustom("Blagajna ne postoji u bazi ili u toj poslovnici").ShowDialog();
                                    txtBlag.Focus();
                                }
                                //ako je i blagajna i poslovnica u bazi
                                else
                                {
                                    //DateTime datum = (DateTime)dtDatum.SelectedDate;
                                    //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu
                                    //string strDatum = datum.ToString("yyyyMMdd");
                                    string strDatum = dtDatum.SelectedDate.Value.ToString("yyyyMMdd");


                                    string? ipKase = await GetBlagIP();

                                    if (ipKase == null)
                                    {
                                        Shared.LogUser("IP BLAG JE NULL");
                                        new MessageBoxCustom("IP BLAG JE NULL", MessageType.Error).ShowDialog();
                                    }
                                    else
                                    {
                                        //ako ne pingujemo kasu
                                        if (!PingServer(ipKase))
                                        {
                                            Shared.LogUser("Ne pingujem kasu!");
                                            new MessageBoxCustom("Ne pingujem kasu!").ShowDialog();
                                            lbResult.Content = "Ne pingujem kasu!";
                                            lbResult.FontWeight = FontWeights.Bold;
                                            lbResult.FontSize = 25;
                                            lbResult.Foreground = Brushes.Red;
                                        }
                                        //ako pingujemo kasu
                                        else
                                        {
                                            #region "pinguj kasu"

                                            try
                                            {
                                                //Provera da li su odhodi ok
                                                if (await CheckOdhodi())
                                                {
                                                    Shared.LogUser("Odhodi su OK");
                                                    lbResult.Content = "Odhodi su OK :)";
                                                    lbResult.FontWeight = FontWeights.Normal;
                                                    lbResult.FontSize = 20;
                                                    BrushConverter bc = new BrushConverter();
                                                    lbResult.Foreground = bc.ConvertFrom("#F4B41A") as Brush;

                                                    if (PingServer("192.168.1.46"))
                                                    {
                                                        //prikazi podatke blagajni
                                                        await GetDataFromDB();
                                                        Shared.LogUser("Prikazujem podatke iz baze");

                                                        //Ako blagajna nije vec zatvorena
                                                        if (await ProveraOdprtaOtpis(strDatum))
                                                        {
                                                            //ZATVARA
                                                            await UpdateZatvoriBlag(strDatum);
                                                            Shared.LogUser("ODARDJEN JE UPDATE BAZE");

                                                            if (!await ProveraOdprtaOtpis(strDatum))
                                                            {
                                                                Shared.LogUser($"ZATVORENA je | balgajna {txtBlag.Text} | u poslovnici {txtPoslovnica.Text} | za datum {dtDatum.SelectedDate.Value:dd.MM.yyyy}");
                                                                new MessageBoxCustom($"Zatvorena je balgajna {txtBlag.Text} u poslovnici {txtPoslovnica.Text} za datum {dtDatum.SelectedDate.Value:dd.MM.yyyy}").ShowDialog();
                                                            }


                                                            await GetDataFromDB();

                                                            lbResult.Content = $"Blagajna je zatvorena";
                                                            lbResult.FontWeight = FontWeights.Normal;
                                                            lbResult.FontSize = 20;
                                                            lbResult.Foreground = bc.ConvertFrom("#F4B41A") as Brush;

                                                            btnZatvori.Visibility = Visibility.Hidden;


                                                        }
                                                        else
                                                        {
                                                            Shared.LogUser("Blagajna je vec zatvorena");
                                                            new MessageBoxCustom("Blagajna je vec zatvorena").ShowDialog();
                                                        }




                                                    }
                                                    else
                                                    {
                                                        Shared.LogUser("Server is unaccessible");
                                                        new MessageBoxCustom("Server is unaccessible").ShowDialog();
                                                    }



                                                }
                                                //Ako odhodi imaju vise od 2 file-a
                                                else
                                                {
                                                    Shared.LogUser("Odhodi nisu prazni, resetuj sepis");
                                                    lbResult.Content = "Odhodi nisu prazni, resetuj sepis";
                                                    lbResult.FontWeight = FontWeights.Bold;
                                                    lbResult.FontSize = 25;
                                                    lbResult.Foreground = Brushes.Red;

                                                }
                                            }
                                            catch ( Exception ex)
                                            {
                                                Shared.LogUser("\n" + ex.ToString());
                                                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                                            }

                                            

                                            #endregion
                                        }
                                    }

                                }//else od provere blagajne i poslovnice

                                #endregion
                            }
                            else
                            {
                                Shared.LogUser("Ne pingujem server 192.168.1.46");
                                new MessageBoxCustom("Ne pingujem server 192.168.1.46").ShowDialog();
                            }

                        }//else od provere poslovnice


                    }//sve je ok


                    //da kada se jednom pritisne dugme zatvori, da nestane jer ne treba da se pritiska vise puta bez provere
                    btnZatvori.Visibility = Visibility.Hidden;
                    #endregion
                    
            }else
            {
                Shared.LogUser("Prekinut proces zatvaranja blagajne");
                new MessageBoxCustom("Prekinut proces zatvaranja blagajne").ShowDialog();
            }



        }//Dugme ZATVORI





        #region "REGEX za posl i blag input"
        private bool CheckPoslInput(string s)
        {
            Regex regex = new Regex(@"2\d\d\d00");
            return regex.IsMatch(s);

        }

        //Metoda koja proverava input blagajne
        private bool CheckBlagInput(string s)
        {
            //proverimo prvo da li je poslovnica uneta kako treba
            if (CheckPoslInput(txtPoslovnica.Text))
            {
                //da li je duzina blagajne 4 karaktera
                if (s.Length != 4)
                {
                    return false;
                }

                //uzimamo 3 identifikaciona broja poslovnice
                string idPosl = txtPoslovnica.Text.Substring(1, 3);
                //blagajna mora da bude: prvi broj [1-9] pa onda 3 broja
                Regex regex = new Regex(@"[1-9]\d\d\d");
                //da li je uneta blagajna
                if (s == String.Empty)
                    return false;
                else
                {
                    //da li se poslednja 3 broja blagajne slazu sa identifikacionim brojem poslovnice
                    bool isIdOk = idPosl == s.Substring(1, 3);
                    //da li se ispostovala forma blagajne i slazu poslednja 3 broja sa identifikacionim brojem poslovnice
                    return regex.IsMatch(s) && isIdOk;
                }

            }
            else
            {
                new MessageBoxCustom("Poslovnica nije dobro uneta").ShowDialog();
                return false;
            }

        }

        #endregion


        #region "KEY UP"

        private void EnterUpBlag(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnProvera.Focus();
                Provera_Click(sender, e);
            }
        }

        private void EnterUpPosl(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                dtDatum.Focus();
            }
        }

        private void EnterUpDatum(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                txtBlag.Focus();
            }
        }


        #endregion



        //Metoda koja proverava da li postoji uneta poslovnica u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraPosl()
        {
            //ako je null ne postoji posl
            string? posl = null;

            try
            {

                string query = $"select POSL FROM BI_POSL WHERE POSL = {txtPoslovnica.Text} and AKTIVNA = 'D'";

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
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (posl != null)
                return true;
            else
                return false;

        }



        //Metoda koja proverava da li postoji uneta blagajna u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private async Task<bool> ProveraBlag()
        {
            //ako je null ne postoji blag
            string? blag = null;

            try
            {
                string query = $"select BLAG_POSL from BI_BLAGPOSL where BLAG_POSL = {txtBlag.Text} and MATICNA_POSL = {txtPoslovnica.Text} and AKTIVNA = 'D'";

                SqlConnection connection = await Task.Run(() => new SqlConnection(connectionString));
                await Task.Run(() => connection.Open());
                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, connection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                if (reader.Read())
                {
                    blag = reader.GetValue(0).ToString();

                }
                else
                {
                    blag = null;

                }

                connection.Close();
            }
            catch (Exception e)
            {
                blag = null;
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (blag != null)
                return true;
            else
                return false;

        }




        //Provera odhoda za test
        private async Task<int> FilesInOdhodi(string path)
        {
            int fileCount = await Task.Run(() => Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length);

            return fileCount;
        }


        //Provera da li su zatvorene blagajne
        //ako blagajna nije zatvorena vraca true
        //ako je blagajna zatvorena vraca false
        public async Task<bool> ProveraOdprtaOtpis(string datum)
        {
            try
            {
                string query = $"select ODPRTA, TIP_ODPIS, BLAG_POSL from Bip_prom where KODA in ('BK','BZ') and POSL = {txtPoslovnica.Text} and DATUM = '{datum}' order by BLAG_POSL DESC";
                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                SqlDataAdapter sqlDataAdapter = await Task.Run(() => new SqlDataAdapter(query, sqlConnection));


                DataTable blagTable = new DataTable();

                await Task.Run(() =>
                {
                    using (sqlDataAdapter)
                    {
                        sqlDataAdapter.Fill(blagTable);


                    }
                });


                //prolaz kroz tabelu
                for (int i = 0; i < blagTable.Rows.Count; i++)
                {
                    //samo da gleda redove koji su za unetu blagajnu
                    if (blagTable.Rows[i]["BLAG_POSL"].ToString() == txtBlag.Text)
                    {
                        //Ako odprta nije * (tj null je)
                        if (blagTable.Rows[i]["ODPRTA"].ToString() != "*")
                        {
                            //ako tip odpisa nije 4
                            if (blagTable.Rows[i]["TIP_ODPIS"].ToString() != "4")
                            {
                                return true;
                            }

                        }

                    }

                }
                //U suprotnom je blagajna zatvorena
                return false;



            }
            catch (Exception e)
            {
                Shared.LogUser($"\n{e}\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                return false;
            }
        }




        //Pokretanje upita da prikaze podatke
        private async Task GetDataFromDB()
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            DateTime datum = (DateTime)dtDatum.SelectedDate;
#pragma warning restore CS8629 // Nullable value type may be null.
            
            //ovo je konverzija da vidim kako to treba da izgleda za prebacivanje u bazu
            string strDatum = datum.ToString("yyyyMMdd");


            try
            {

                string query = $"select ODPRTA, TIP_ODPIS, BLAG_POSL, KODA from Bip_prom where KODA in ('BK','BZ') and POSL = {txtPoslovnica.Text} and DATUM = '{strDatum}' order by BLAG_POSL DESC";
                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                SqlDataAdapter sqlDataAdapter = await Task.Run(() => new SqlDataAdapter(query, sqlConnection));

                DataTable blagTable = new DataTable();

                await Task.Run(() =>
                {

                    using (sqlDataAdapter)
                    {

                        sqlDataAdapter.Fill(blagTable);




                    }

                });
                
                dataBaza.ItemsSource = blagTable.DefaultView;

            }
            catch (Exception e)
            {
                Shared.LogUser($"\n{e}\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }


        }





        //dobijanje ip adrese blagajne
        //Ako iskoci NO DATA FOUND to znaci da nije nadjena ip adresa
        private async Task<string?> GetBlagIP()
        {
            string? ipAdress = null;
            try
            {

                string query = $"select IP_LOKALNI_NASLOV, blag_posl from BI_BLAGPOSL where MATICNA_POSL = {txtPoslovnica.Text} and blag_posl = {txtBlag.Text}";
                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                await Task.Run(() => sqlConnection.Open());

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                await Task.Run(() =>
                {
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        ipAdress = reader.GetValue(0).ToString();
                    }
                    else
                    {
                        Shared.LogUser("Nisam pronasao IP kase");
                        new MessageBoxCustom("Nisam pronasao IP kase").ShowDialog();
                    }
                });

                await Task.Run(() =>sqlConnection.Close());
            }
            catch (Exception e)
            {
                Shared.LogUser($"\n{e}\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();

            }

            
            return ipAdress;
        }






        //Proverava odhode u kasi
        //ako ima manje od 3 file-a vraca true ako >= 3 vraca false
        private async Task<bool> CheckOdhodi()
        {
            string? ipAdress = await GetBlagIP();



            if (ipAdress != null)
            {
                string path = @"\\" + ipAdress + @"\RCL\xmlblag\odhodi";

                int numOfFiles = await FilesInOdhodi(path);

                if (numOfFiles >= 3)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            //ako GetBlagIP() vrati null vrednost
            else
            {
                Shared.LogUser("IP BLAG JE NULL");
                new MessageBoxCustom("IP BLAG JE NULL", MessageType.Error).ShowDialog();
                return false;
            }


        }



        //Gets an ip adress in string form ("192.162.1.46") 
        //returns true or false based on weather it can see it
        private bool PingServer(string s)
        {
            //Za proveru da li imamo konekciju ka serveru
            Ping x = new Ping();
            PingReply reply = x.Send(IPAddress.Parse(s),5000);

            Thread.Sleep(500);


            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }





        //Metoda za zatvaranje blagajni
        private async Task UpdateZatvoriBlag(string datum)
        {
            try
            {

                string updateQuery = $"update BIP_PROM set ODPRTA = '*', TIP_ODPIS = '4' where KODA in ('BK','BZ') and POSL = {txtPoslovnica.Text} and DATUM = '{datum}' and BLAG_POSL = {txtBlag.Text}";

                sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                await Task.Run(() => sqlConnection.Open());

                SqlCommand cmd = await Task.Run(() => new SqlCommand(updateQuery, sqlConnection));

                cmd.CommandText = updateQuery;

                int promenjeniRedovi = await Task.Run(() => cmd.ExecuteNonQuery());

                Shared.LogUser($"Promenjeno je {promenjeniRedovi} redova");
                //javlja koliko redova u bazi je izmenjeno
                new MessageBoxCustom($"Promenjeno je {promenjeniRedovi} redova").ShowDialog();


                await Task.Run(() => sqlConnection.Close());

            }
            catch (Exception e)
            {
                Shared.LogUser($"\n{e}\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }



        








        //CLASS ZatBlag : Window
    }
}
