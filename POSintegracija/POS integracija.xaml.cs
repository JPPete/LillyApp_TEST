using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Data;
using System.Diagnostics;
using static LillyApp_TEST.MessageBoxCustom;
using DocumentFormat.OpenXml.Spreadsheet;
using Renci.SshNet;
using Irony.Parsing;
using LillyApp_TEST.POSintegracija;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for POS_integracija.xaml
    /// </summary>
    public partial class POS_integracija : Window
    {
        SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodEditConnString;

        //Konekcija za Monitoring bazu
        public const string connMonitoring = Shared.prodMonitoringEditConnString;


        public string Username = MainWindow.Username;


        public POS_integracija()
        {
            InitializeComponent();

            txtPoslovnica.Focus();

        }

        private void Trenutnebtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"Pritisnuo dugme za PRIKAZ VRSTA PLACANJA za poslovnicu {txtPoslovnica.Text}");
            
            
            //provera da li je dobro uneta poslovnica
            if (!CheckPoslInput(txtPoslovnica.Text))
            {
                Shared.LogUser("Pogresno unet broj poslovnice");
                new MessageBoxCustom("Pogresno unet broj poslovnice",MessageType.Error).ShowDialog();
                //MessageBox.Show("Pogresno unet broj poslovnice");
                txtPoslovnica.Focus();
            }
            else//ako je dobro uneta poslovnica
            {
                //Proveravamo da li se pinguje server
                if (PingServer("192.168.1.46"))
                {

                    //ako uneta poslovnica ne postoji u bazi
                    if (!ProveraPosl())
                    {
                        Shared.LogUser("Poslovnica ne postoji u bazi");
                        new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    else//ako uneta poslovnica postoji u bazi
                    {
                        Shared.LogUser($"Prikazujem VRSTA PLACANJA za poslovnicu {txtPoslovnica.Text}");
                        PrikazVrstaPlacanja();
                    }//vi ste moje slepo kucanje


                }//Ako ne pingujemo server
                else
                {
                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                    new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                }
            }

            //TRENUTNEBTN
        }



        private void Integracijaebtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"Pritisnuo dugme za INTEGRACIJU za poslovnicu {txtPoslovnica.Text}");


            //Provera da li je korisnik koji ima dozvolu
            if(Username == "petar.jovancic" || Username == "de.marko" || Username == "denis.cavdarevic" || Username == "nikola.celjovski" )
            {
                #region "button function"
                //Provera da korisnik jos jednom potvrdi da zeli da uradi integraciju
                bool? result = new MessageBoxCustom("Da li ste sigurni da zelite da ubacite nove vrste placanja?", MessageType.Confirmation).ShowDialog();
                Shared.LogUser("Da li ste sigurni da zelite da ubacite nove vrste placanja?");


                //No, cancel i izlaz svi treba da prekinu proces.
                //Samo Yes izvrsava akciju dugmeta
                if (result.Value)
                {

                    Shared.LogUser("Potvrda za pokretanje procesa - YES");
                    #region "AKCIJA DUGMETA"
                    //provera da li je dobro uneta poslovnica
                    if (!CheckPoslInput(txtPoslovnica.Text))
                    {
                        Shared.LogUser("Pogresno unet broj poslovnice");
                        new MessageBoxCustom("Pogresno unet broj poslovnice", MessageType.Error).ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    else//ako je dobro uneta poslovnica
                    {
                        //Proveravamo da li se pinguje server
                        if (PingServer("192.168.1.46"))
                        {

                            //ako uneta poslovnica ne postoji u bazi
                            if (!ProveraPosl())
                            {
                                Shared.LogUser("Poslovnica ne postoji u bazi");
                                new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                                txtPoslovnica.Focus();
                            }
                            else//ako uneta poslovnica postoji u bazi
                            {

                                Shared.LogUser($"Pritisnuo dugme za INTEGRACIJU za poslovnicu {txtPoslovnica.Text}");

                                string posl = txtPoslovnica.Text;

                                string insertQuery = "INSERT INTO BI_NACPLAC_POSL (POSL, NAC_PLAC, SORT) " +
                                    $"VALUES ({posl}, 43, 43),({posl}, 44, 44),({posl}, 45, 45),({posl}, 46, 46),({posl}, 47, 47), ({posl}, 48, 48),({posl}, 49, 49),({posl}, 50, 50);";

                                string deleteQuery = $"DELETE FROM BI_NACPLAC_POSL WHERE POSL = {posl} and NAC_PLAC = 05";

                                Shared.LogUser("Pokrecem upit:");
                                Shared.LogUser("\n" + insertQuery);
                                UpdateQuery(insertQuery);

                                Shared.LogUser("Pokrecem upit:");
                                Shared.LogUser("\n" + deleteQuery);
                                UpdateQuery(deleteQuery);

                                Shared.LogUser("Prikazujem vrste placanja");
                                PrikazVrstaPlacanja();
                                new MessageBoxCustom("Uspesno zavrsena integracija").ShowDialog();


                            }//vi ste moje slepo kucanje


                        }//Ako ne pingujemo server
                        else
                        {
                            Shared.LogUser("Ne pingujem server 192.168.1.46");
                            new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                        }
                    }
                }
                #endregion
                else 
                {
                    Shared.LogUser("Potvrda za pokretanje procesa - NO");
                    new MessageBoxCustom("Prekinut proces zatvaranja blagajne", MessageType.Error).ShowDialog();

                }
                        
                    
            }
            else
            {
                Shared.LogUser($"Korisnik {Username} nema pravo da pritisne dugme za INTEGRACIJU");
                new MessageBoxCustom("Nemate pistup da koristite ovo dugme.\nObratite se administratoru", MessageType.Error).ShowDialog();
                
            }
            #endregion

        }


        private void VratiStarubtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser($"Pritisnuo dugme za VRACANJE STARE VRSTE PLACANJA za poslovnicu {txtPoslovnica.Text}");


            //Provera da li je korisnik koji ima dozvolu
            if (Username == "petar.jovancic" || Username == "de.marko" || Username == "denis.cavdarevic" || Username == "nikola.celjovski" )
            {
                #region "button function"
                //Provera da korisnik jos jednom potvrdi da zeli da uradi integraciju
                bool? result = new MessageBoxCustom("Da li ste sigurni da zelite da vratite stare vrste placanja?", MessageType.Confirmation).ShowDialog();
                Shared.LogUser("Da li ste sigurni da zelite da vratite stare vrste placanja?");

                //No, cancel i izlaz svi treba da prekinu proces.
                //Samo Yes izvrsava akciju dugmeta
                if (result.Value)
                {
                   
                    #region "AKCIJA DUGMETA"
                    Shared.LogUser("Potvrda za pokretanje procesa - YES");
                    //provera da li je dobro uneta poslovnica
                    if (!CheckPoslInput(txtPoslovnica.Text))
                    {
                        Shared.LogUser("Pogresno unet broj poslovnice");
                        new MessageBoxCustom("Pogresno unet broj poslovnice", MessageType.Error).ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    else//ako je dobro uneta poslovnica
                    {
                        //Proveravamo da li se pinguje server
                        if (PingServer("192.168.1.46"))
                        {

                            //ako uneta poslovnica ne postoji u bazi
                            if (!ProveraPosl())
                            {
                                Shared.LogUser("Poslovnica ne postoji u bazi");
                                new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                                txtPoslovnica.Focus();
                            }
                            else//ako uneta poslovnica postoji u bazi
                            {
                                Shared.LogUser($"Pritisnuo dugme za VRACANJE STARE VRSTE PLACANJA za poslovnicu {txtPoslovnica.Text}");

                                string deleteQuery = $"delete from BI_NACPLAC_POSL where posl = {txtPoslovnica.Text} and nac_plac in (43,44,45,46,47,48,49,50)";
                                string instertQuery = $"insert into BI_NACPLAC_POSL (POSL, NAC_PLAC, sort) select {txtPoslovnica.Text}, '05', 5";

                                Shared.LogUser("Pokrecem upit:");
                                Shared.LogUser("\n" + deleteQuery);
                                UpdateQuery(deleteQuery);

                                Shared.LogUser("Pokrecem upit:");
                                Shared.LogUser("\n" + deleteQuery);
                                UpdateQuery(instertQuery);

                                Shared.LogUser("Prikazujem vrste placanja");
                                PrikazVrstaPlacanja();
                                new MessageBoxCustom("Vracene stare vrste placanja", MessageType.Info).ShowDialog();

                            }//vi ste moje slepo kucanje


                        }//Ako ne pingujemo server
                        else
                        {
                            Shared.LogUser("Ne pingujem server 192.168.1.46");
                            new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                        }
                    }
                    #endregion
                        

                    #endregion
                }
                else 
                {
                    Shared.LogUser("Potvrda za pokretanje procesa - NO");
                    new MessageBoxCustom("Prekinut proces zatvaranja blagajne", MessageType.Info).ShowDialog();

                }
            }
            else
            {
                Shared.LogUser($"Korisnik {Username} nema pravo da pritisne dugme za VRACANJE STARE VRSTE PLACANJA");
                new MessageBoxCustom("Nemate pistup da koristite ovo dugme.\nObratite se administratoru", MessageType.Error).ShowDialog();
                
            }



        }


        private async void PingDevicebtn(object sender, RoutedEventArgs e)
        {

            Shared.LogUser($"je pritisnuo dugme da nadje POS terminale u poslovnicu {txtPoslovnica.Text}");

            dgVrstePlac.ItemsSource = null;

            StrpljenjeMsg();


            
            
            #region "button"
            //provera da li je dobro uneta poslovnica
            if (!CheckPoslInput(txtPoslovnica.Text))
            {
                Shared.LogUser("Pogresno unet broj poslovnice");
                new MessageBoxCustom("Pogresno unet broj poslovnice", MessageType.Error).ShowDialog();
                txtPoslovnica.Focus();
            }
            else//ako je dobro uneta poslovnica
            {
                //Proveravamo da li se pinguje server
                if (PingServer("192.168.1.46"))
                {

                    //ako uneta poslovnica ne postoji u bazi
                    if (!ProveraPosl())
                    {
                        Shared.LogUser("Poslovnica ne postoji u bazi");
                        new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    else//ako uneta poslovnica postoji u bazi
                    {

                        #region "Core function of the button"

                        //uzima broj poslovnice iz inputa, nalazi ip, prosledjuje i krece da pinguje
                        List<string> devices = await PingIpRange(await IpRadnje(txtPoslovnica.Text));

                        string ipRouter = await IpRadnje(txtPoslovnica.Text) + "200";


                        string ipToPing = "";

                        //TEST CODE
                        //string[] popisTerminala = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "/POSintegracija/PopisAdresa/PopisTerminala.txt");

                        List<PTerminal> terminali = await GetAllMac(ipRouter, devices);




                        DataTable dtIp = new();


                        dtIp.Columns.Add("IP adresa");
                        dtIp.Columns.Add("MAC adresa");
                        dtIp.Columns.Add("DHCP ili STATIC");



                        if(terminali == null ||  terminali.Count == 0)
                        {
                            Shared.LogUser("Nisu pronadjene MAC adrese");

                            foreach(string device in devices)
                            {
                                dtIp.Rows.Add(device, "", "");
                                ipToPing += "\n" + device + "; ; ";
                            }

                        }
                        else
                        {

                            foreach(PTerminal device in terminali)
                            {
                                dtIp.Rows.Add(device.Ip, device.MAC, device.StatDhcp);
                                
                            }


                        }


                        foreach(string device in devices)
                        {
                            string boro = $"/c ping {device} -t";
                            Process.Start("cmd.exe", boro);
                        }

                        /*
                        foreach (string device in devices)
                        {
                            bool nasao = false;
                            //TEST CODE                  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!                         THIS IS WHERE I TEST GETTING MAC ADRESSES                             !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            
                            foreach(string line in popisTerminala)
                            {
                                //Ako je vec nasao tu ip adresu u spisku da ne gleda sve ostale
                                if (nasao)
                                    break;
                                
                                //0 je ip adresa, 1 je mac, 2 je dhcp ili statica
                                string[] ipMacDhcp = line.Split(';');
                                
                                //provera da li se ip adresa pingovanih uredjaja nalazi u spisku
                                //ako se nalazi
                                if (ipMacDhcp[0] == device)
                                {
                                    
                                    dtIp.Rows.Add( device, ipMacDhcp[1], ipMacDhcp[2] );
                                    nasao= true;
                                    ipToPing += "\n" + device + ";" + ipMacDhcp[1] + ";" + ipMacDhcp[2];
                                }

                            }

                            if (!nasao)
                            {
                                dtIp.Rows.Add(device, "", "");
                                ipToPing += "\n" + device + "; ; ";
                                
                            }
                            

                            //KOD ZAKOMENTARISAN ZA TEST
                            //dtIp.Rows.Add(device);

                            //otvara cmd i pinguje sve uredjaje koje je nasao
                            string boro = $"/c ping {device} -t";
                            Process.Start("cmd.exe", boro);


                            //ipToPing += device + "\n";
                        }
                        
                        */


                        dgVrstePlac.ItemsSource = dtIp.DefaultView;

                        //Shared.LogUser($"Pronadjene IP adrese su:{ipToPing}");
                        //MessageBox.Show("IP adrese gde su terminali:\n" + ipToPing);

                        #endregion


                    }//vi ste moje slepo kucanje


                }//Ako ne pingujemo server
                else
                {
                    Shared.LogUser("Ne pingujem server 192.168.1.46");
                    new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                }
            }
            #endregion
            



        }


        private void KongIntegbtn(object sender, RoutedEventArgs e)
        {

            Shared.LogUser($"je pritisnuo da prebaci konfig za INTEGRACIJU za poslovnicu {txtPoslovnica.Text}");


            //Provera da li je korisnik koji ima dozvolu
            if (Username == "petar.jovancic" || Username == "de.marko" || Username == "denis.cavdarevic" || Username == "nikola.celjovski")
            {
                #region "button function"
                bool? result = new MessageBoxCustom($"Da li ste sigurni da zelite da pustite konfig za poslovnicu {txtPoslovnica.Text}?", MessageType.Confirmation).ShowDialog();

                Shared.LogUser($"Da li ste sigurni da zelite da pustite konfig za poslovnicu {txtPoslovnica.Text}?");
                Shared.LogUser($"odgovor korisnika:\t{result}");


                if (result.Value)
                {
                    
                        #region "Execute the button function"


                        //provera da li je dobro uneta poslovnica
                        if (!CheckPoslInput(txtPoslovnica.Text))
                        {
                            Shared.LogUser("Pogresno unet broj poslovnice");
                            new MessageBoxCustom("Pogresno unet broj poslovnice", MessageType.Error).ShowDialog();
                            txtPoslovnica.Focus();
                        }
                        else//ako je dobro uneta poslovnica
                        {
                            //Proveravamo da li se pinguje server
                            if (PingServer("192.168.1.46"))
                            {

                                //ako uneta poslovnica ne postoji u bazi
                                if (!ProveraPosl())
                                {
                                    Shared.LogUser("Poslovnica ne postoji u bazi");
                                    new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                                    txtPoslovnica.Focus();
                                }
                                else//ako uneta poslovnica postoji u bazi
                                {

                                    #region "MAIN FUNCTION OF THE BUTTON"

                                    List<string> ipAdrese = IpAdresePoslovnice();

                                    //Check if we got the ip adresses
                                    if (ipAdrese.Count != 0)
                                    {
                                        //Iterates tghrough the ip adresses
                                        foreach (string ip in ipAdrese)
                                        {


                                            string ipPosl = ip.Split('.')[2];
                                            string ipKasa = ip.Split(".")[3];


                                            string konfig = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/POSintegracija/konfig.upd.file");

                                            konfig = konfig.Replace("XXX", ipPosl);
                                            konfig = konfig.Replace("YYY", "13" + ipKasa);



                                            Shared.LogUser("Prebacujem konfig na putanju:\t" + @"\\" + ip + @"\RCL\XmlBlag\Prihodi\konfig.upd.file");




                                            //putanja za kase
                                            File.WriteAllText(@"\\" + ip + @"\RCL\XmlBlag\Prihodi\konfig.upd.file", konfig);

                                            //TEST putanja za moj racunar
                                            //string user = Environment.UserName;
                                            //File.WriteAllText($@"C:\Users{user.ToLower()}\Desktop\konfig{ipKasa}.upd.file", konfig);




                                            string[] linesKonfig = File.ReadAllLines(@"\\" + ip + @"\RCL\XmlBlag\Prihodi\konfig.upd.file");

                                            foreach (string line in linesKonfig)
                                            {
                                                if (line.Contains("192.168."))
                                                    Shared.LogUser("sa ip adresom:" + line);
                                            }





                                        }

                                        Shared.LogUser($"zavrsen rad dugmeta - prebaci konfig za INTEGRACIJU za poslovnicu {txtPoslovnica.Text}");
                                        new MessageBoxCustom("Konfig je prebacen na sve kase").ShowDialog();

                                    }
                                    else //if we didnt get Ip adresses
                                    {
                                        new MessageBoxCustom("Program nije uspeo da pronadje IP adrese kasa", MessageType.Error).ShowDialog();
                                    }



                                    #endregion


                                }//vi ste moje slepo kucanje


                            }//Ako ne pingujemo server
                            else
                            {
                                Shared.LogUser("Ne pingujem server 192.168.1.46");
                                new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                            }
                        }


                        #endregion
                        

                }else 
                {
                    Shared.LogUser("Prekinut proces prebacivanja konfiga za INTEGRACIJU - NO");
                    new MessageBoxCustom("Prekinut proces prebacivanja konfiga za INTEGRACIJU").ShowDialog();
                }

                #endregion
            }
            else
            {
                Shared.LogUser($"Korisnik {Username} nema pravo da pritisne dugme za prebacivanje konfiga za INTEGRACIJU");
                new MessageBoxCustom($"Nemate pistup da koristite ovo dugme {Username}.\nObratite se administratoru").ShowDialog();
                
            }



        }


        private void KongVratibtn(object sender, RoutedEventArgs e)
        {

            Shared.LogUser($"je pritisnuo da prebaci stari konfig za SKIDANJE SA INTEGRACIJE za poslovnicu {txtPoslovnica.Text}");


            //Provera da li je korisnik koji ima dozvolu
            if (Username == "petar.jovancic" || Username == "de.marko" || Username == "denis.cavdarevic" || Username == "nikola.celjovski")
            {
                #region "button function"
                bool? result = new MessageBoxCustom($"Da li ste sigurni da zelite da pustite konfig za SKIDANJE SA INTEGRACIJE za poslovnicu {txtPoslovnica.Text}?", MessageType.Confirmation).ShowDialog();

                Shared.LogUser($"Da li ste sigurni da zelite da pustite konfig za SKIDANJE SA INTEGRACIJE za poslovnicu {txtPoslovnica.Text}?");
                Shared.LogUser($"odgovor korisnika:\t{result}");


                if (result.Value)
                {
                    
                    #region "Execute the button function"


                    //provera da li je dobro uneta poslovnica
                    if (!CheckPoslInput(txtPoslovnica.Text))
                    {
                        Shared.LogUser("Pogresno unet broj poslovnice");
                        new MessageBoxCustom("Pogresno unet broj poslovnice", MessageType.Error).ShowDialog();
                        txtPoslovnica.Focus();
                    }
                    else//ako je dobro uneta poslovnica
                    {
                        //Proveravamo da li se pinguje server
                        if (PingServer("192.168.1.46"))
                        {

                            //ako uneta poslovnica ne postoji u bazi
                            if (!ProveraPosl())
                            {
                                Shared.LogUser("Poslovnica ne postoji u bazi");
                                new MessageBoxCustom("Poslovnica ne postoji u bazi", MessageType.Error).ShowDialog();
                                txtPoslovnica.Focus();
                            }
                            else//ako uneta poslovnica postoji u bazi
                            {

                                #region "MAIN FUNCTION OF THE BUTTON"

                                List<string> ipAdrese = IpAdresePoslovnice();

                                //Check if we got the ip adresses
                                if (ipAdrese.Count != 0)
                                {
                                    //Iterates tghrough the ip adresses
                                    foreach (string ip in ipAdrese)
                                    {


                                        string ipPosl = ip.Split('.')[2];
                                        string ipKasa = ip.Split(".")[3];


                                        string konfig = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/POSintegracija/StariKonfig/konfig.upd.file");




                                        Shared.LogUser("Prebacujem konfig na putanju:\t" + @"\\" + ip + @"\RCL\XmlBlag\Prihodi\konfig.upd.file");




                                        //putanja za kase
                                        File.WriteAllText(@"\\" + ip + @"\RCL\XmlBlag\Prihodi\konfig.upd.file", konfig);

                                        //TEST putanja za moj racunar
                                        //string user = Environment.UserName;
                                        //File.WriteAllText($@"C:\Users{user.ToLower()}\Desktop\konfig{ipKasa}.upd.file", konfig);







                                    }

                                    Shared.LogUser($"zavrsen rad dugmeta - prebaci konfig za SKIDANJE SA INTEGRACIJE za poslovnicu {txtPoslovnica.Text}");
                                    new MessageBoxCustom("Konfig je prebacen na sve kase", MessageType.Info).ShowDialog();

                                }
                                else //if we didnt get Ip adresses
                                {
                                    new MessageBoxCustom("Program nije uspeo da pronadje IP adrese kasa", MessageType.Error).ShowDialog();
                                }



                                #endregion


                            }//vi ste moje slepo kucanje


                        }//Ako ne pingujemo server
                        else
                        {
                            Shared.LogUser("Ne pingujem server 192.168.1.46");
                            new MessageBoxCustom("Ne pingujem server 192.168.1.46", MessageType.Error).ShowDialog();
                        }
                    }


                    #endregion
                        

                }
                #endregion
                else 
                {
                    Shared.LogUser("Prekinut proces prebacivanja konfiga za SKIDANJE SA INTEGRACIJE - NO");
                    new MessageBoxCustom("Prekinut proces prebacivanja konfiga za SKIDANJE SA INTEGRACIJE", MessageType.Info);
                }
            }
            else
            {
                Shared.LogUser($"Korisnik {Username} nema pravo da pritisne dugme za prebacivanje konfiga za SKIDANJE SA INTEGRACIJE");
                new MessageBoxCustom("Nemate pistup da koristite ovo dugme.\nObratite se administratoru", MessageType.Error);
                
            }




        }







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









        //Metoda koja vraca Dictionary gde je key ip terminala a value mac terminala
        //Ocekuje prosledjenu ip adresu rutera i spisak ip adresa terminala
        private async Task<List<PTerminal>> GetAllMac(string ipRutera, List<string> IpAdrese)
        {
            //Dictionary<string, string> IpMac = new();

            List<PTerminal> terminali = new();

            List<string> prefiksUredjaja = await GetMacPrefiks();



            try
            {
                await Task.Run(() =>
                {

                    SshClient sshClient = new(ipRutera, Shared.mikrotikUsername, Shared.mikrotikPass);

                    SshCommand cmd;

                    Shared.LogUser($"Uspostavljam konekciju sa ruterom {ipRutera}");
                    sshClient.Connect();



                    foreach (string ip in IpAdrese)
                    {
                        cmd = sshClient.RunCommand($"ip dhcp-server lease print terse where address~\"{ip}\"");


                        string result = cmd.Result;



                        if (result == "\r\n")
                        {
                            Shared.LogUser($"Dobio sam prazan odgovor od mikrotika - trebalo bi da ne postoji {ip} ");

                            Shared.LogUser($"Proveravam da nije podesen na STATIC");


                            SshCommand cmd2;

                            cmd2 = sshClient.RunCommand($"ip arp print terse where address~\"{ip}\"");


                            string result2 = cmd2.Result;

                            string[] arpData = result2.Split(' ');

                            for(int j = 0; j < arpData.Length; j++)
                            {
                                if (arpData[j].Contains("mac-address"))
                                {
                                    terminali.Add(new PTerminal(ip, arpData[j][12..], "STATIC"));
                                }
                            }


                        }
                        else
                        {
                            string[] data = result.Split(' ');

                            for (int i = 0; i < data.Length; i++)
                            {
                                //da je adresa prosledjena i da je drugo slovo d (da ne bi gledao active-adress)
                                if (data[i].Contains("mac-address") && data[i][0] == 'm')
                                {
                                    terminali.Add(new PTerminal(ip, data[i][12..] , "DHCP"));
                                    //IpMac.Add(ip, data[i][12..]);
                                }

                            }
                        }


                    }

                    sshClient.Disconnect();
                    sshClient.Dispose();




                });



            }
            catch (Exception e)
            {

                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }



            return terminali;

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

                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok,"GRESKA", 650,450,10).ShowDialog();
            }



            if(ipRadnje == "" || ipRadnje == null)
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


        //Method that pings the ip range
        //Takes in the parameter from method IpRadnje(string posl)
        //and then adds to it numbers from 124 to 139 and pings them
        //if the ping is successfull then it adds that ip adress to the list
        //and return this list of IP adresses (of devices in the store)
        private static async Task<List<string>> PingIpRange(string ipRadnje)
        {

            List<string> uredjaji = new();

            //ako poslovnica nema ip adresu
            if (ipRadnje == "" || ipRadnje == null)
            {
                return uredjaji;
            }



                Ping x = new();


            for (int i = 124; i < 140; i++)
            {

                PingReply reply = await Task.Run(() => x.Send(IPAddress.Parse(ipRadnje + i), 1000));


                if (reply.Status == IPStatus.Success)
                    uredjaji.Add(ipRadnje + i);


            }


            return uredjaji;
        }





        //Based on the input of Poslovnica, runs a querry
        //Returns a list of IP adresses at that location
        private List<string> IpAdresePoslovnice()
        {
            List<string> ipAdrese = new();


            try
            {

                string query = "SELECT IP_LOKALNI_NASLOV " +
                "FROM BI_BLAGPOSL " +
                "WHERE 1=1 " +
                "AND BLAG_POSL like '____' " +
                $"AND MATICNA_POSL = {txtPoslovnica.Text}";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    ipAdrese.Add(reader["IP_LOKALNI_NASLOV"].ToString());
#pragma warning restore CS8604 // Possible null reference argument.
                }

                //DODAO NAKNADNO TREBA PROVERITI DA LI RADI
                connection.Close();
            }
            catch (Exception e)
            {

                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }


            return ipAdrese;
        }



        public void PrikazVrstaPlacanja()
        {
            try
            {

                string query = $"select posl.NAC_PLAC, nac.OPIS, posl.SORT from BI_NACPLAC_POSL posl inner join BI_NACPLAC nac on posl.NAC_PLAC = nac.NAC_PLAC Where posl = {txtPoslovnica.Text}";
                sqlConnection = new SqlConnection(connectionString);

                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, sqlConnection);

                using (sqlDataAdapter)
                {
                    DataTable blagTable = new DataTable();
                    sqlDataAdapter.Fill(blagTable);



                    dgVrstePlac.ItemsSource = blagTable.DefaultView;
                }
            }
            catch (Exception e)
            {
                Shared.LogUser("\n"+e.ToString()+"\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }



        //Metoda za Update baze
        //Prima string koji se koristi da se update-uje baza
        private void UpdateQuery(string query)
        {
            try
            {
                string updateQuery = query;

                sqlConnection = new SqlConnection(connectionString);

                sqlConnection.Open();

                SqlCommand cmd = new SqlCommand(updateQuery, sqlConnection);

                cmd.CommandText = updateQuery;

                //javlja koliko redova u bazi je izmenjeno
                new MessageBoxCustom($"Promenjeno je {cmd.ExecuteNonQuery()} redova").ShowDialog();


                sqlConnection.Close();

            }
            catch (Exception e)
            {
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }



        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }



        private void EnterUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                PingDevicebtn(sender,e);
                btnPingDevices.Focus();
            }
        }


        //Metoda za proveru da je unet broj poslovnice u dobrom formatu
        //vraca true ako jeste, false ako nije
        private bool CheckPoslInput(string s)
        {
            Regex regex = new Regex(@"2\d\d\d00");
            return regex.IsMatch(s);

        }

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
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (posl != null)
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


        //Shows a msg box that explains that it takes time to get data
        public void StrpljenjeMsg()
        {
            Shared.LogUser("Prikazujem poruku za strpljenje");
            new MessageBoxCustom("Ovaj proces moze da potraje, hvala na strpljenju").ShowDialog();

        }







        //CLASS POS_integracija
    }
}
