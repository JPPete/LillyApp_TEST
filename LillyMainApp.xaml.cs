using System;
using System.DirectoryServices;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for LillyApp.xaml
    /// </summary>
    public partial class LillyMainApp : Window
    {
        //public string Username = ((MainWindow)Application.Current.MainWindow).Username;
        //public string Password = ((MainWindow)Application.Current.MainWindow).Password;

        public string Username = MainWindow.Username;
        public string Password = MainWindow.Password;


        public LillyMainApp()
        {
            InitializeComponent();


            if (AuthenticateUserIT(Username, Password) || (Username == Shared.adminUser && Password == Shared.adminPass))
            {
                Shared.LogUser($"Korisnik: {Username} je u IT-u");
                //Shared.LogUser("prikazujem dugme Knjizenje dokumenata");
                //KDbtn.Visibility = Visibility.Visible;
                Integbtn.Visibility = Visibility.Visible;
                
                Dlistabtn.Visibility = Visibility.Hidden;
                Shared.LogUser("Sakrivam dugme D LISTA");




                //Allowed users for D Lista
                if (Username.ToLower() == "aleksa.pavlovic" || Username.ToLower() == "de.marko")
                {
                    Shared.LogUser($"Korisnik je: {Username} i prikazujem dugme D Lista");
                    Dlistabtn.Visibility = Visibility.Visible;
                }

                //Allowed users for POS integracija
                if (Username.ToLower() == "de.marko" || Username.ToLower() == "denis.cavdarevic")
                {
                    Shared.LogUser($"Korisnik je: {Username} i prikazujem dugme POS integracija");
                    Integbtn.Visibility = Visibility.Visible;
                }

                //Show everything to Petar Jovancic
                if (Username.ToLower() == "petar.jovancic" || Username == Shared.adminUser)
                {
                    Shared.LogUser($"Korisnik je {Username} i prikazujem mu sve");
                    Dlistabtn.Visibility = Visibility.Visible;
                    Integbtn.Visibility = Visibility.Visible;
                    //PrekoracenjaIFakturisanjebtn.Visibility = Visibility.Visible;
                }


            }
            else
            {
                Shared.LogUser($"Korisnik: {Username} nije clan IT-a");
                Shared.LogUser("sakrivam dugme Knjizenje dokumenata");
                //PrekoracenjaIFakturisanjebtn.Visibility = Visibility.Hidden;
                Shared.LogUser("sakrivam dugme ZatBlag");
                Zatblagbtn.Visibility = Visibility.Hidden;

                //Loyaltybtn.SetValue(Grid.RowProperty, 2);
                //Loyaltybtn.SetValue(Grid.ColumnProperty, 2);

            }


        }





        #region "BUTTONS"

        private void NarudzbeniceBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t ---- NARUDZBENICE ----");
            Narudzbenice narudzbenice = new();
            narudzbenice.Show();
            WindowState= WindowState.Minimized;
        }

        private void ZatBlagBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- ZATBLAG ----");
            ZatBlag zatblag = new();
            zatblag.Show();
            WindowState = WindowState.Minimized;
        }

        private void PrekoIFakturBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- PREKORACENJA I FAKTURISANJE ----");
            PrekoIFakt prekoIFakt = new();
            prekoIFakt.Show();
            WindowState = WindowState.Minimized;
        }

        private void LoyaltyBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- BROJ LOYALTY KARTICE NA RACUNU ----");
            LoyaltyBrRacun loyaltyBrRacun= new();
            loyaltyBrRacun.Show();
            WindowState = WindowState.Minimized;
        }

        private void DlistaBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- D LISTA ----");
            DLista dLista = new();
            dLista.Show();
            WindowState = WindowState.Minimized;
        }


        private void IntegracijaBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- POS INTEGRACIJA ----");
            POS_integracija pos = new();
            pos.Show();
            WindowState = WindowState.Minimized;
        }


        private void PosTerminaliBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- POS TERMINALI ----");
            POSterminali posTerminali = new();
            posTerminali.Show();

            WindowState = WindowState.Minimized;
            
            
        }


        private void CeneArtBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("POKRENUT PROGRAM \t\t --- CENE ARTIKLA ----");
            CeneArtikla cArt = new();
            cArt.Show();

            WindowState = WindowState.Minimized;

        }




        #endregion





        //Method that checks if the user is part of IT sub group of the AD
        //Thakes in username and password and returns true if it is from IT and false if not
        public static bool AuthenticateUserIT(string userName, string password)
        {
            bool ret = false;

            try
            {
                string domainName = "LDAP://OU=IT,OU=Korisnici,OU=LillySRB,DC=DC,DC=LILLY,DC=RS";

                System.DirectoryServices.DirectoryEntry de = new(domainName, userName, password);
                DirectorySearcher dsearch = new DirectorySearcher(de)
                {
                    SearchScope = SearchScope.Subtree,
                    Filter = "(&(objectCategory=User)(objectClass=person))"
                };


                SearchResultCollection results = dsearch.FindAll();

                foreach (SearchResult result in results)
                {
                    if (result.Properties["sAMAccountName"][0].ToString() == userName.ToLower())
                        ret = true;
                }

                if (results == null)
                {
                    return ret;
                }

                //ret = true;
            }
            catch
            {
                ret = false;
            }

            return ret;



        }


        



        







        //CLASS LILLYAPP
    }
}
