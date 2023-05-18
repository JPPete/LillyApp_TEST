using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data;
using Renci.SshNet;
using static LillyApp_TEST.MessageBoxCustom;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Office.Word;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for POSterminali.xaml
    /// </summary>
    public partial class POSterminali : Window
    {

        SqlConnection sqlConnection;

        //private readonly string[] prefiksiIngenio = { "B4:00:16", "54:E1:40", "54:7F:54", "38:EF:E3", "00:03:81", "10:1E:DA", "F8:43:60"};

        

        //Konekcija na TEST SQL SERVER preko integrated security
        public const string connectionString = Shared.prodViewConnString;


        //Konekcija za TEST bazu na mom lapotpu gde sam uneo POS TERMINALE
        public const string connMonitoring = Shared.prodMonitoringEditConnString;

        
        public POSterminali()
        {
            InitializeComponent();

            if (Shared.UserInADgroup(MainWindow.Username, "LillyApp_Admin"))
            {
                Shared.LogUser($"Korisnik {MainWindow.Username} je u Admin, prikazujem dugme OBRISI");
                btnObrisi.Visibility = Visibility.Visible;
            }


        }



        //Funkcije dugmica

        #region "BUTTONS"

        //Dugme koje vraca podatke iz baze na osnovu inputa
        private async void PronadjiBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"Pokrece se PRONALAZENJE POS terminala, sa podacima: POSL: {txtPosl.Text} | BLAG: {txtBlag.Text} | TID: {txtTID.Text} | MAC: {txtMAC.Text}");


            string mainQuerry = "select POSL, pos.BLAGAJNA, concat(posl.IME, ' ', posl.IME1) as NAZIV, TID, MAC, IP_TERMINALA, blag.IP_LOKALNI_NASLOV as IP_KASE, DATUM_UNOSA " +
                        "from TERMINALI_STANJE pos " +
                        "inner join LILLY_RCL_SRB.dbo.BI_BLAGPOSL blag on pos.BLAGAJNA = blag.BLAG_POSL " +
                        "inner join LILLY_RCL_SRB.dbo.BI_POSL posl on blag.MATICNA_POSL = posl.POSL " +
                        "where 1=1 ";
            string orderMainQ = " \norder by posl,blagajna,tid";

            //da li ima pristup serveru gde je baza
            if (!PingServer("192.168.1.46"))
            {
                Shared.LogUser("Ne pingujem server 192.168.1.46");
                new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                
            }
            else 
            {

                //Sve su prazne - Vraca celu tabelu
                if (txtPosl.Text == string.Empty && txtBlag.Text == string.Empty
                    && txtTID.Text == string.Empty && txtMAC.Text == string.Empty)
                {

                    string query = mainQuerry + orderMainQ;

                    Shared.LogUser($"Porekcem upit:\n{query}");

                    //dgPosTerminali.FontSize = 15;

                    await GetDataAsync(query);

                }

                #region "SAMO JEDAN PODATAK JE UNET"

                //Samo Poslovnica je uneta
                else if (txtPosl.Text != string.Empty && txtBlag.Text == string.Empty
                    && txtTID.Text == string.Empty && txtMAC.Text == string.Empty)
                {

                    //string query = "select t.* " +
                    //    "from terminali_stanje t " +
                    //    "inner join [LILLY_RCL_SRB].[dbo].BI_BLAGPOSL blag on t.blagajna = blag.BLAG_POSL " +
                    //    "where 1=1 " +
                    //    $"and MATICNA_POSL = '{txtPosl.Text}'";


                    string query = mainQuerry + $" \nand posl = {txtPosl.Text}" + orderMainQ;


                    Shared.LogUser($"Porekcem upit:\n{query}");


                    await GetDataAsync(query);

                }

                //Samo Blagajna je uneta
                else if (txtPosl.Text == string.Empty && txtBlag.Text != string.Empty
                    && txtTID.Text == string.Empty && txtMAC.Text == string.Empty)
                {

                    //string query = "select * " +
                    //    "from terminali_stanje " +
                    //    "where 1=1 " +
                    //    $"and blagajna = {txtBlag.Text}";


                    string query = mainQuerry + $" \nand pos.BLAGAJNA = {txtBlag.Text}" + orderMainQ;



                    Shared.LogUser($"Porekcem upit:\n{query}");

                    await GetDataAsync(query);

                }

                //Samo TID je unet
                else if (txtPosl.Text == string.Empty && txtBlag.Text == string.Empty
                    && txtTID.Text != string.Empty && txtMAC.Text == string.Empty)
                {

                    //string query = "select * " +
                    //    "from terminali_stanje " +
                    //    "where 1=1 " +
                    //    $"and TID = '{txtTID.Text}'";

                    string query = mainQuerry + $" \nand TID like '%{txtTID.Text}%'" + orderMainQ;


                    Shared.LogUser($"Porekcem upit:\n{query}");

                    await GetDataAsync(query);

                }

                //Samo MAC je unet
                else if (txtPosl.Text == string.Empty && txtBlag.Text == string.Empty
                    && txtTID.Text == string.Empty && txtMAC.Text != string.Empty)
                {

                    //string query = "select * " +
                    //    "from terminali_stanje " +
                    //    "where 1=1 " +
                    //    $"and MAC = '{txtMAC.Text}'";

                    string query = mainQuerry + $" \nand MAC like '%{txtMAC.Text}%'" + orderMainQ;


                    Shared.LogUser($"Porekcem upit:\n{query}");

                    await GetDataAsync(query);

                }


                //Ako je uneto vise od 1 podatka
                else if(txtBlag.Text != string.Empty)
                {
                    
                    //string query = "select * " +
                    //                "from terminali_stanje " +
                    //                "where 1=1 " +
                    //                $"and blagajna = {txtBlag.Text}";

                    string query = mainQuerry + $" \nand pos.BLAGAJNA = {txtBlag.Text}" + orderMainQ;



                    Shared.LogUser($"Porekcem upit:\n{query}");

                    await GetDataAsync(query);
                    
                    
                }

                #endregion


            }//else ping server










        }//PronadjiBtn DUGME


        //Dugme koje unis/menja podatke u bazi na osnovu inputa
        [STAThread]
        private async void UnesiBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"Pritisnuto je dugme UNESI za podatke: POSL: {txtPosl.Text} | BLAG: {txtBlag.Text} | TID: {txtTID.Text} | MAC: {txtMAC.Text}");

            if (!PingServer("192.168.1.46"))
            {
                Shared.LogUser("Ne pingujem server 192.168.1.46");
                new MessageBoxCustom("Ne vidim server 192.168.1.46", MessageType.Error).ShowDialog();
            }
            else
            { 
                
                //da li je popunjeno polje blag
                if(txtBlag.Text == string.Empty)
                {
                    Shared.LogUser($"Blagajna mora biti uneta: BLAG: {txtBlag.Text}");
                    new MessageBoxCustom("Blagajna mora biti popunjena da bi dugme UNESI radilo").ShowDialog();
                }
                else
                {
                    //provera da li su uneti i TID i MAC
                    if(txtTID.Text == string.Empty || txtMAC.Text == string.Empty)
                    {
                        Shared.LogUser($"Mora biti unet i TID i MAC: TID: {txtTID.Text} | MAC: {txtMAC.Text}");
                        new MessageBoxCustom("Mora biti unet i TID i MAC").ShowDialog();
                    }
                    else
                    {

                        //Provera da li je blagajna uneta kako treba
                        if (!ProveraBlag(txtBlag.Text))
                        {
                            Shared.LogUser($"Blagajna nije dobro uneta (nije pronadjena u bazi), uneta je: BLAG: {txtBlag.Text}");
                            new MessageBoxCustom("Blagajna nije pronadjena u aktivnim blagajnama u bazi").ShowDialog();
                        }
                        else
                        {

                            //Provera da li je MAC unet u dobrom formatu
                            if(!CheckMacInput(txtMAC.Text))
                            {
                                Shared.LogUser("MAC adresa nije uneta u dobrom formatu.\nSeparator za MAC adresu mora da bude ':'");
                                new MessageBoxCustom($"MAC adresa nije uneta u dobrom formatu, MAC: {txtMAC.Text}").ShowDialog();
                            }
                            else
                            {

                                //Provera da korisnik jos jednom potvrdi da zeli da promeni podatke
                                bool? result = new MessageBoxCustom($"Da li ste sigurni da zelite da unesete podatke:\n TID: {txtTID.Text} i MAC: {txtMAC.Text}\nZa BLAGAJNU: {txtBlag.Text}?", MessageType.Confirmation).ShowDialog();

                                Shared.LogUser($"Da li ste sigurni da zelite da unesete podatke: TID: {txtTID.Text} i MAC: {txtMAC.Text}\nZa BLAGAJNU: {txtBlag.Text}?");
                                Shared.LogUser($"odgovor korisnika:\t{result}");


                                //No, cancel i izlaz svi treba da prekinu proces.
                                //Samo Yes radi update
                                if (result.Value)
                                {
                                    #region "PROCEDURE"

                                    string tid = txtTID.Text.ToUpper().Trim();

                                    string mac = txtMAC.Text.ToUpper().Trim();

                                    string? ip = await IpAdresaTerminala(txtBlag.Text);

                                    string prefiksMac = mac[..8];



                                    bool proveraMac = await ProveraMacPrefiksa(prefiksMac);


                                    //Uneta MAC adresa nije od ingenico terminala
                                    if (!proveraMac)
                                    {
                                        Shared.LogUser($"Nije dobar prefiks MAC adrese");
                                        new MessageBoxCustom("Uneta MAC adresa nije od proizvodjaca Ingenico.\nU slucaju da jeste, obavestite administratora da doda u prefikse.").ShowDialog();
                                    }
                                    else
                                    {
                                        //Provera da li smo nasli IP adresu terminala
                                        if (ip == null)
                                        {
                                            new MessageBoxCustom("Neuspesno pronalazenje ip adrese terminala").ShowDialog();
                                            Shared.LogUser($"Neuspesno pronalazenje ip adrese terminala za blag: {txtBlag.Text}");
                                        }
                                        else
                                        {

                                            Shared.LogUser("-----POKRECEM PROCEDURU----");
                                            Shared.LogUser($"Parametri: {txtBlag.Text} | {tid} | {mac} | {ip}");

                                            bool nemaProblem = await ExecSqlProcedureUPSERT(txtBlag.Text, tid, mac, ip);

                                            string query = "SELECT * " +
                                                "FROM terminali_stanje " +
                                                $"WHERE blagajna = '{txtBlag.Text}'";

                                            await GetDataAsync(query);

                                            if (nemaProblem)
                                            {
                                                bool? pitanjeZaTerminal = new MessageBoxCustom($"Da li zelite da podesite i terminal na ruteru?", MessageType.Confirmation).ShowDialog();

                                                Shared.LogUser("Da li zelite da podesite i terminal na ruteru?");
                                                Shared.LogUser($"odgovor korisnika:\t{pitanjeZaTerminal}");

                                                if (pitanjeZaTerminal.Value)
                                                {

                                                    #region "DA"

                                                    Shared.LogUser("-----POKRECEM PODESAVANJE TERMINALA----");

                                                    string posl = string.Concat("2", txtBlag.Text.AsSpan(1), "00");

                                                    string routerIP = await IpRadnje(posl) + "200";

                                                    //new MessageBoxCustom($"TEST| routerIP {routerIP} | mac {mac} | ip {ip}").ShowDialog();

                                                    //!!!!!!!!!!!!!!!!!!! PROMENJENE VREDNOSTI ZA TEST !!!!!!!!!!!!!!!!
                                                    //mac = "54:7F:54:55:C9:7F";

                                                    //ip = "192.168.15.136";

                                                    //routerIP = "192.168.15.200";

                                                    await SetTerminalOnRouter(routerIP, mac, ip);


                                                    Thread.Sleep(100);

                                                    bool podesenTerminal = await ProveraIpTerminala(routerIP, mac, ip);

                                                    if (podesenTerminal)
                                                    {
                                                        new MessageBoxCustom("Proces je zavrsen").ShowDialog();
                                                        Shared.LogUser("Proces je zavrsen");
                                                    }
                                                    else
                                                    {
                                                        Shared.LogUser($"Terminal {mac} - nije uspesno podesen na mikrotiku {routerIP} | da ima adresu {ip}");
                                                        new MessageBoxCustom("Terminal nije uspesno podesen na mikrotiku", MessageType.Error).ShowDialog();
                                                    }


                                                    #endregion

                                                }
                                                else
                                                {
                                                    Shared.LogUser("Prekinut proces podesavanja terminala");
                                                    new MessageBoxCustom("Prekinut proces podesavanja terminala").ShowDialog();
                                                }

                                                

                                            }
                                            else
                                            {
                                                Shared.LogUser("SQL je vratio gresku prilikom pokusaja unosa podataka");
                                            }



                                        }//else od provere da li smo dobili IP kase
                                    }//proveraMAC-a


                                    


                                        
                                    #endregion
                                        
                                }
                                else
                                {
                                    Shared.LogUser("Prekinut proces promene podataka");
                                    new MessageBoxCustom("Prekinut proces promene podataka");
                                }



                            }//else od provere MAC format

                        }//else provera blagajne

                        

                    }//else od provere TIDa i MACa


                }//else provera da li su popunjeni posl i blag

            
            }//else od PingServer



            }//IzmeniBtn DUGME



        //Dugme koje brise red na osnovu inputa korisnika
        private async void ObrisiBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"------------- Pritisnuto je dugme OBRISI za podatke:  BLAG: {txtBlag.Text}  ---------------");

            if (!PingServer("192.168.1.46"))
            {
                Shared.LogUser("Ne pingujem server 192.168.1.46");
                new MessageBoxCustom("Ne vidim server 192.168.1.46", MessageType.Error).ShowDialog();
            }
            else
            {
                //da li je popunjeno polje blag
                if (txtBlag.Text == string.Empty)
                {
                    Shared.LogUser($"Blagajna mora biti uneta: BLAG: {txtBlag.Text}");
                    new MessageBoxCustom("Blagajna mora biti popunjena da bi dugme UNESI radilo").ShowDialog();
                }
                else
                {
                    //Provera da li je blagajna uneta kako treba
                    if (!ProveraBlag(txtBlag.Text))
                    {
                        Shared.LogUser($"Blagajna nije dobro uneta (nije pronadjena u bazi), uneta je: BLAG: {txtBlag.Text}");
                        new MessageBoxCustom("Blagajna nije pronadjena u aktivnim blagajnama u bazi").ShowDialog();
                    }
                    else
                    {
                        Shared.LogUser("Prikazujem podatke za tu blagajnu");

                        string query = "select * " +
                        "from terminali_stanje " +
                        "where 1=1 " +
                        $"and blagajna = {txtBlag.Text}";

                        await GetDataAsync(query);

                        //Provera da korisnik jos jednom potvrdi da zeli da promeni podatke
                        bool? result = new MessageBoxCustom($"Da li ste sigurni da zelite da obrisete podatke za BLAGAJNU: {txtBlag.Text}?", MessageType.Confirmation).ShowDialog();

                        Shared.LogUser($"Da li ste sigurni da zelite da obrisete podatke za BLAGAJNU: {txtBlag.Text}");
                        Shared.LogUser($"odgovor korisnika:\t{result}");

                        if (result.Value)
                        {
                            Shared.LogUser("-----POKRECEM BRISANJA BLAGAJNE----");

                            await ExecSqlProcedureDELETE(txtBlag.Text);

                            await GetDataAsync(query);


                            Shared.LogUser("Proces je zavrsen");
                            new MessageBoxCustom("Proces je zavrsen").ShowDialog();
                            
                        }
                        else
                        {
                            Shared.LogUser("Prekinut proces promene podataka");
                            new MessageBoxCustom("Prekinut proces promene podataka");
                        }//odbili da nastave



                    }//else proveraBlag
                }//else prazna blag
            }//else ping server





        }




        //Dugme koje vraca MAC adrese pronadjene na ruteru
        private async void FindMacbtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("-------- Pritisnuto je dugme FindMacbtn ---------");

            lbMacAdresses.Items.Clear();


            //Provera da li je uneta blagajna
            if (!Shared.EmptyFieldValidation(txtBlag.Text))
            {
                new MessageBoxCustom("Da bi nasli MAC adrese u poslovnici, morate uneti broj blagajne").ShowDialog();
                Shared.LogUser("Da bi nasli MAC adrese u poslovnici, morate uneti broj blagajne");
            }
            else
            {
                //Provera da li se pinguje baza na 46
                if (!PingServer("192.168.1.46"))
                {
                    new MessageBoxCustom("Ne pingujem 192.168.1.46", MessageType.Error).ShowDialog();
                    Shared.LogUser("Ne pingujem 192.168.1.46");
                }
                else
                {
                    //Provera da li se uneta blagajna nalazi u bazi
                    if (!ProveraBlag(txtBlag.Text))
                    {
                        new MessageBoxCustom("Uneta blagajne ne postoji u bazi", MessageType.Error).ShowDialog();
                        Shared.LogUser($"Uneta blagajne ne postoji u bazi - uneta: {txtBlag.Text}");
                    }
                    else
                    {



                        string blag = txtBlag.Text;

                        string posl = "2" + blag.Substring(1, blag.Length - 1) + "00";

                        Shared.LogUser($"Blagajna {txtBlag.Text} se nalazi u poslovnici {posl}");


                        string ipRutera = await IpRadnje(posl) + "200";

                        Shared.LogUser($"IP adresa rutera je: {ipRutera}");


                        //!!!!!!!!!!!!!!!!!!!!!     za test     !!!!!!!!!!!!!!!!
                        //ipRutera = "192.168.15.200";


                        //Provera da li se pinguje mikrotik
                        if (!PingServer(ipRutera))
                        {
                            new MessageBoxCustom("Ne pingujem ruter", MessageType.Error).ShowDialog();
                            Shared.LogUser($"Ne pingujem ruter - {ipRutera}");
                        }
                        else
                        {


                            laSearchMac.Content = "Trazim...";

                            Shared.LogUser("Zapocinjem potragu MAC adresa");
                            await GetMacAdressesFromRouter(ipRutera);

                        }//esle ping mikrotik



                        


                    }//else provera blag u bazi

                }//else ping 1.46
                
                
            }//else provera unete blag



        }//Dugme FindMacbtn




        //Pritiskom na znak pitanja dobijamo info panel
        private void InfoPanel(object sender, MouseButtonEventArgs e)
        {
            Shared.LogUser("Prikazujem info panel");

            PosTerminaliInfoPanel info = new();
            info.Show();
            
        }




        #endregion


        //POKTREBNO TESTIRATI MOZE BITI PROBLEMA SA TASK.RUN


        //Metoda koja pokrece proceduru za upis terminala
        //Prima 4 parametra, svi su string
        
        private async Task<bool> ExecSqlProcedureUPSERT(string blag, string tid, string mac, string ip)
        {
            bool sveOk = true;

            bool blagParse = int.TryParse(blag, out int blagInt);

            string tidUpper = tid.ToUpper();

            string macUpper = mac.ToUpper();

            if( blagParse )
            {
                try
                {

                    await Task.Run(async () =>
                    {

                        //!!!!!!!!!!!!!!!!!!!! treba promeniti connection string !!!!!!!!!!!!!!!!!!!!!!!!
                        SqlConnection sqlConnection = new(connMonitoring);

                        sqlConnection.Open();


                        SqlCommand cmd = new SqlCommand("[Monitoring].[dbo].[TERMINAL_UPSERT]", sqlConnection);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@blagajna", SqlDbType.Int).Value = blagInt;
                        cmd.Parameters.Add("@tid", SqlDbType.VarChar).Value = tidUpper;
                        cmd.Parameters.Add("@mac", SqlDbType.VarChar).Value = macUpper;
                        cmd.Parameters.Add("@ip", SqlDbType.VarChar).Value = ip;


                        //ovo se koristi jer bez toga dolazi do problema sa threadovima
                        //error The calling thread must be STA
                        Dispatcher.Invoke((Action)delegate 
                        {

                            SqlDataReader reader = cmd.ExecuteReader();

                            string error = "";

                            while (reader.Read())
                            {
                                if (reader[0] != null)
                                {
                                    error = reader[0].ToString();
                                    sveOk = false;
                                    new MessageBoxCustom(error, MessageType.Error).ShowDialog();
                                    Shared.LogUser(error);


                                }


                            }

                        });

                        sqlConnection.Close();

                        
                    });


                    return sveOk;


                }
                catch (Exception e)
                {
                    sveOk = false;
                    
                    Shared.LogUser("\n" + e.ToString() + "\n");
                    new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 11).ShowDialog();
                    return sveOk;
                }




            }
            else
            {
                sveOk = false;
                new MessageBoxCustom($"Nije uspela konverzija blagajne u INT - {blag}", MessageType.Error).ShowDialog();
                Shared.LogUser($"Nije uspela konverzija blagajne u INT (metoda ExecSglProcedure - {blag}");
                return sveOk;
            }



        }




        //Metoda koja pokrece proceduru za brisanje terminala
        //Prima 1 parametra, svi su string
        private async Task ExecSqlProcedureDELETE(string blag)
        {


            bool blagParse = int.TryParse(blag, out int blagInt);

            if (blagParse)
            {
                try
                {

                    await Task.Run(() =>
                    {

                        //!!!!!!!!!!!!!!!!!!!! treba promeniti connection string !!!!!!!!!!!!!!!!!!!!!!!!
                        SqlConnection sqlConnection = new(connMonitoring);

                        sqlConnection.Open();


                        SqlCommand cmd = new SqlCommand("[Monitoring].[dbo].[TERMINAL_DELETE]", sqlConnection);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@blagajna", SqlDbType.Int).Value = blagInt;


                        //ovo se koristi jer bez toga dolazi do problema sa threadovima
                        //error The calling thread must be STA
                        Dispatcher.Invoke((Action)delegate 
                        {
                            SqlDataReader reader = cmd.ExecuteReader();

                            string error = "";

                            while (reader.Read())
                            {
                                if (reader[0] != null)
                                {
                                    error = reader[0].ToString();
                                    new MessageBoxCustom(reader[0].ToString(), MessageType.Error).ShowDialog();
                                    Shared.LogUser(reader[0].ToString());

                                }


                            }

                        });

                            



                        sqlConnection.Close();


                    });





                }
                catch (Exception e)
                {

                    Shared.LogUser("\n" + e.ToString() + "\n");
                    new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                }




            }
            else
            {
                new MessageBoxCustom($"Nije uspela konverzija blagajne u INT - {blag}", MessageType.Error).ShowDialog();
                Shared.LogUser($"Nije uspela konverzija blagajne u INT (metoda ExecSglProcedure - {blag}");
            }



        }








        //Meotda koja prima ip rutera kao argument i ispisuje MAC adrese u lisbox
        private async Task GetMacAdressesFromRouter(string ipRutera)
        {

            List<string> macAdrese = new();

            List<string> prefiksUredjaja = await GetMacPrefiks();

            if(prefiksUredjaja == null)
            {
                Shared.LogUser($"Nisu uspesno pronadjeni prefiksi {ipRutera}");
                laSearchMac.Content = "Nema MAC adresa";
                new MessageBoxCustom("Nisu uspesno pronadjeni prefiksi mac adresa").ShowDialog();
            }
            else
            {
                await Task.Run(() =>
                {

                    SshClient sshClient = new(ipRutera, Shared.mikrotikUsername, Shared.mikrotikPass);

                    SshCommand cmd;

                    Shared.LogUser($"Uspostavljam konekciju sa ruterom {ipRutera}");
                    sshClient.Connect();

                    cmd = sshClient.RunCommand("ip dhcp-server lease print where dynamic");

                    Shared.LogUser("Pokrecem komandu: ip dhcp-server lease print where dynamic");

                    string result = cmd.Result;




                    string[] lines = result.Split('\n');

                    for (int i = 2; i < lines.Length - 1; i++)
                    {
                        string[] data = lines[i].Split(" ");


                        for (int j = 0; j < data.Length; j++)
                        {
                            if (data[j].Contains(':'))
                            {
                                for (int k = 0; k < prefiksUredjaja.Count - 1; k++)
                                {
                                    if (data[j].Contains(prefiksUredjaja[k]))
                                    {
                                        macAdrese.Add(data[j]);
                                    }
                                }

                            }
                        }

                    }

                });

                if (macAdrese.Count > 0)
                {
                    Shared.LogUser("Pronadjene MAC adrese su:\n");
                    for (int i = 0; i < macAdrese.Count; i++)
                    {
                        lbMacAdresses.Items.Add(macAdrese[i]);
                        Shared.LogUser(macAdrese[i]);
                    }

                    Shared.LogUser("\n");
                    laSearchMac.Content = "";
                }
                else
                {
                    Shared.LogUser($"Nije pronadjena ni jedna MAC adresa na ruteru {ipRutera}");
                    laSearchMac.Content = "Nema MAC adresa";
                }
            }

        }//Method GetMacAdressesFromRouter


        //Method that returns a list of strings that are allowed mac prefikses
        private static async Task<List<string>> GetMacPrefiks()
        {
            List<string> spisakPrefiksa = new();

            //string connectionString = Shared.prodViewConnString;

            try
            {

                string query = "SELECT PREFIKS " +
                "FROM MAC_PREFIKS " +
                "WHERE 1=1 ";

                SqlConnection connection = await Task.Run(() => new SqlConnection(connMonitoring));
                connection.Open();
                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, connection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                while (reader.Read())
                {
                    spisakPrefiksa.Add(reader["PREFIKS"].ToString());
                }


                connection.Close();
            }
            catch (Exception e)
            {
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            //Ako nismo nasli MAC prefkse vracamo null
            if (spisakPrefiksa.Count == 0)
                return null;

            //Ako smo nasli IP adresu kase, uzimamo poslednji index '.' i posle njega dodajemo 13 da bi dobili ip adresu terminala
            //string ipTerminala = ipAdresaKase.Insert(ipAdresaKase.LastIndexOf('.') + 1, "13");

            return spisakPrefiksa;
        }




        //Metoda koja podesava terminal na ruteru
        //1.Stavlja da bude static lease
        //2.Brise stari
        //3.Dodeljuje mu IP
        //4.Postavlja ga na DHCP server i stalja mu komentar
        private async Task SetTerminalOnRouter(string routerIp, string terminalMac, string terminalIP)
        {
            //Dictionary koji koristimo da bi dobili komentar koja kasa je u pitanju
            Dictionary<char, string> kase = new Dictionary<char, string>
            {
                { '9', "DK1" },
                { '2', "DK2" },
                { '3', "DK3" },
                { '4', "DK4" },
                { '5', "AP1" },
                { '6', "AP2" },
                { '7', "AP3" },
                { '8', "AP4" },

            };


            string kasa = string.Empty;

            try
            {
                //Uzimamo poslednji karakter iz ip-a terminala da bi znali koja kasa je u pitanju
                kasa = kase[terminalIP[^1]];
            }
            catch (Exception ex)
            {
                Shared.LogUser($"NIJE USPESNO PRONADNJENA KASA ZA KOMENTAR:\n{ex.ToString()}");
                new MessageBoxCustom("Nije uspelo pronalazenje kase za komentar na mikrotiku").ShowDialog();
            }
            

            


            using(SshClient client = new(routerIp, Shared.mikrotikUsername, Shared.mikrotikPass))
            {
                try
                {
                    client.Connect();


                    SshCommand cmd = await Task.Run(() => client.RunCommand($"ip dhcp-server lease print terse where mac-address~\"{terminalMac}\""));

                    string result = cmd.Result;


                    bool vecPodesenIP = false;

                    //bool vecPodesenKom = false;


                    if (result == "\r\n")
                    {
                        Shared.LogUser($"Dobio sam prazan odgovor od mikrotika - trebalo bi da ne postoji {terminalMac} ");
                    }
                    else
                    {
                        

                        string[] data = result.Split(' ');

                        for (int i = 0; i < data.Length; i++)
                        {
                            //da je adresa prosledjena i da je drugo slovo d (da ne bi gledao active-adress)
                            if (data[i].Contains($"address={terminalIP}") && data[i][1] == 'd')
                            {
                                vecPodesenIP = true;
                            }

                            //if (data[i].Contains($"comment= POS {kasa}"))
                            //{
                            //    vecPodesenKom = true;
                            //}
                            
                        }
                    }

                    if(vecPodesenIP)
                    {
                        Shared.LogUser("Terminal je vec podesen - ne podesavam");
                        new MessageBoxCustom("Terminal je vec podesen - ne podesavam").ShowDialog();
                    }
                    else
                    {
                        await Task.Run(() =>
                        {

                            client.RunCommand($"ip dhcp-server lease make-static [find mac-address={terminalMac}]");

                            client.RunCommand($"ip dhcp-server lease remove [find address={terminalIP}]");

                            //client.RunCommand($"ip dhcp-server lease add address={terminalIP} mac-address=\"{terminalMac}\" server=dhcp_lan");

                            client.RunCommand($"ip dhcp-server lease set address={terminalIP} [find mac-address={terminalMac}]");

                            if (kasa != string.Empty)
                            {
                                client.RunCommand($"ip dhcp-server lease set comment=\"POS {kasa}\" [find mac-address={terminalMac}]");
                            }




                        });
                    }


                    



                    client.Disconnect();
                    client.Dispose();

                }
                catch (Exception ex)
                {

                    new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                    Shared.LogUser("\n" + ex.ToString() + "\n");
                }
            }



        }






        //Metoda koja proverava da li prosledjena MAC adresa ima prosledjen IP, na prosledjenom ruteru
        //vraca true ako ima, vraca false ako nema ili nije dobio podatak
        private async Task<bool> ProveraIpTerminala(string routerIp, string mac, string ip)
        {
            bool uspesno = false;

            await Task.Run(() => 
            {

                try
                {
                    SshClient client = new(routerIp, Shared.mikrotikUsername, Shared.mikrotikPass);

                    SshCommand cmd;

                    client.Connect();

                    cmd = client.RunCommand($"ip dhcp-server lease print terse where mac-address~\"{mac}\"");

                    client.Disconnect();
                    client.Dispose();

                    string result = cmd.Result;

                    if(result == "\r\n")
                    {
                        Shared.LogUser($"Dobio sam prazan odgovor od mikrotika - trebalo bi da ne postoji {mac} ");
                    }
                    else
                    {
                        string[] data = result.Split(' ');

                        for(int i = 0; i < data.Length; i++)
                        {
                            //da je adresa prosledjena i da je drugo slovo d (da ne bi gledao active-adress)
                            if (data[i].Contains($"address={ip}") && data[i][1] == 'd')
                                uspesno = true;
                        }
                    }


                }
                catch (Exception ex)
                {
                    new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
                    Shared.LogUser("\n" + ex.ToString() + "\n");
                }
            
            
            
            });


            return uspesno;
        }






        //Find the part of Ip adress that specifies the store
        //return example: 192.168.208.
        private async Task<string> IpRadnje(string posl)
        {
            string ipRadnje = "";

            try
            {
                string query = "SELECT IP_LOKALNI_NASLOV " +
                    "FROM BI_BLAGPOSL " +
                    "WHERE 1=1 " +
                    "AND BLAG_POSL like '____' " +
                    $"AND MATICNA_POSL = {posl}";

                await Task.Run(() => {

                    SqlConnection connection = new SqlConnection(connectionString);
                    connection.Open();
                    SqlCommand cmd = new(query, connection);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        ipRadnje = reader.GetValue(0).ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    }

                });




            }
            catch (Exception e)
            {
                Shared.LogUser($"\n{e}\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }



            if (ipRadnje == "" || ipRadnje == null)
            {
                return ipRadnje;
            }
            else
            {
                string[] split = ipRadnje.Split(".");


                ipRadnje = split[0] + "." + split[1] + "." + split[2] + ".";

                return ipRadnje;
            }

            //string[] split = ipRadnje.Split(".");


            //ipRadnje = split[0] + "." + split[1] + "." + split[2] + ".";

            //return ipRadnje;
        }





        //Metoda koja vraca IP terminala
        //Prima parametar blag (npr 5165) i vraca string (moze da vrati i null)
        private async Task<string?> IpAdresaTerminala(string blag)
        {
            string? ipAdresaKase = "";

            //string connectionString = Shared.prodViewConnString;

            try
            {

                string query = "SELECT IP_LOKALNI_NASLOV " +
                "FROM BI_BLAGPOSL " +
                "WHERE 1=1 " +
                $"AND BLAG_POSL = '{blag}'";

                SqlConnection connection = await Task.Run(() => new SqlConnection(connectionString));
                connection.Open();
                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, connection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                while (reader.Read())
                {
                    ipAdresaKase = reader["IP_LOKALNI_NASLOV"].ToString();
                }


                connection.Close();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString()); ;
            }

            //Ako nismo nasli IP adresu kase vracamo null
            if (ipAdresaKase == null)
                return null;

            //Ako smo nasli IP adresu kase, uzimamo poslednji index '.' i posle njega dodajemo 13 da bi dobili ip adresu terminala
            string ipTerminala = ipAdresaKase.Insert(ipAdresaKase.LastIndexOf('.') + 1, "13");

            return ipTerminala;
        }




        //Runs the Query that is passed through and shows data in DataGrid
        public async Task GetDataAsync(string query)
        {
            dgPosTerminali.ItemsSource = null;

            try
            {


                sqlConnection = await Task.Run(() => new SqlConnection(connMonitoring));

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

                dgPosTerminali.ItemsSource = nnTable.DefaultView;


            }
            catch (Exception ex)
            {
                Shared.LogUser($"\n{ex}");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();


            }


        }






        //Metoda za proveru da je uneta MAC adresa u dobrom formatu
        //vraca true ako jeste, false ako nije
        private bool CheckMacInput(string s)
        {
            Regex regex = new Regex(@"^(?:[0-9A-Fa-f]{2}[:]){5}(?:[0-9A-Fa-f]{2})$");
            return regex.IsMatch(s);

        }







        //Metoda koja proverava da li postoji prosledjena blagajna u bazi.
        //Ako postoji vraca true, ako ne postoji vraca false
        private bool ProveraBlag(string blagajna)
        {
            //ako je null ne postoji blag
            string? blag = null;

            try
            {

                string query = "SELECT BLAG_POSL " +
                    "FROM BI_BLAGPOSL " +
                    "WHERE 1=1 " +
                    $"AND BLAG_POSL = {blagajna} " +
                    "AND AKTIVNA = 'D'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

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
            catch (Exception ex)
            {
                blag = null;
                Shared.LogUser("\n" + ex.ToString());
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (blag != null)
                return true;
            else
                return false;

        }


        //Metoda koja proverava da li se proslednjeni prefiks nalazi u tabeli za dozvoljene prefikse
        //vraca true ako je prefiks u tabeli false ako ne
        private async Task<bool> ProveraMacPrefiksa(string macPrefiks)
        {
            //ako je null ne postoji blag
            string? prefiks = null;

            try
            {
                string query = $@"select PREFIKS from MAC_PREFIKS where PREFIKS = '{macPrefiks}'";

                SqlConnection connection = await Task.Run(() => new SqlConnection(connMonitoring));
                await Task.Run(() => connection.Open());
                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, connection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                if (reader.Read())
                {
                    prefiks = reader.GetValue(0).ToString();

                }
                else
                {
                    prefiks = null;

                }

                connection.Close();
            }
            catch (Exception e)
            {
                prefiks = null;
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (prefiks != null)
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




        //Provera da se u text box unose samo brojevi (PreviewTextInput)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }




        

        //Sta se desava na Key up events
        #region "KEY UP"


        private void PoslEnter(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                txtBlag.Focus();
            }
        }

        private void BlagEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                txtTID.Focus();
            }
        }

        private void TIDEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                txtMAC.Focus();
            }
        }

        private void MACEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                PronadjiBtn(sender, e);
            }
        }

        //What happens when you select an item in listbox
        private void SelectedValuelb(object sender, SelectionChangedEventArgs e)
        {
            if(lbMacAdresses.SelectedItem != null)
            {
                string? selectedMac = lbMacAdresses.SelectedItem.ToString();

                txtMAC.Text = string.Empty;



                if (selectedMac != null || selectedMac != string.Empty)
                {
                    txtMAC.Text = selectedMac;
                }
            }
            



        }




        #endregion

        
    }//CLASS POSterminali


}//NAMESPACE LILLYAP



