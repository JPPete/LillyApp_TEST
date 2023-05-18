using System;
using System.DirectoryServices;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string Username { get; set; }
        public static string Password { get; set; }

        public MainWindow()
        {
            Username = "";
            Password = "";
            InitializeComponent();
            txtUsername.Focus();
        }

        //When you press enter on username txt box it sets focus to passwordbox
        private void EnterUpUsr(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                txtPassword.Focus();
            }
        }

        //When you press enter on password txt box it sets focus to login btn
        private void EnterUpPsw(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnLogin.Focus();
                LoginBtn(sender,e);
            }
        }


        private async void LoginBtn(object sender, RoutedEventArgs e)
        {
            loginLb.Content = "";
            loginLb.Content = null;
            loginLb.Visibility = Visibility.Hidden;


            Username = txtUsername.Text;
            Password = txtPassword.Password;


            Shared.LogUser($"Korisnik : {Username} pokusava da se prijavi");

            if(Username == Shared.adminUser && Password == Shared.adminPass)
            {
                Username = txtUsername.Text;
                Password = txtPassword.Password;

                Shared.LogUser($"Korisnik : {Username} se uspesno prijavio");

                LillyMainApp app = new();
                app.Show();

                Close();
            }
            else
            {
                //Check to see if you can see the AD
                if (PingServer("192.168.1.100"))
                {

                    if (await AuthenticateUser(Username, Password))
                    {
                        Username = txtUsername.Text;
                        Password = txtPassword.Password;

                        Shared.LogUser($"Korisnik : {Username} se uspesno prijavio");

                        LillyMainApp app = new();
                        app.Show();

                        Close();
                    }
                    else
                    {
                        Username = "";
                        Password = "";

                        Shared.LogUser($"Korisnik : {Username} nije uspeo da se prijavi");

                        loginLb.Visibility = Visibility.Visible;
                        loginLb.Content = "Login failed...";
                        txtPassword.Focus();
                    }


                }
                else
                {
                    Shared.LogUser($"Username: {Username} nije uspeo da se prijavi jer nema pristup domenu");
                    loginLb.Visibility = Visibility.Visible;
                    loginLb.Content = "Nemam konekciju prema serveru";
                }
            }


            




        }



        //For authenticating user from DOMAIN
        //Returns true if exists, returns false if not
        public async Task<bool> AuthenticateUser(string userName, string password)
        {


            bool ret = false;

            try
            {
                string domainName = "LDAP://DCLILLY";

                System.DirectoryServices.DirectoryEntry de = await Task.Run(() =>  new System.DirectoryServices.DirectoryEntry(domainName, userName, password));
                DirectorySearcher dsearch = await Task.Run(() => new DirectorySearcher(de));
                SearchResult? results = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                results = await Task.Run(() => dsearch.FindOne());
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                if (results == null)
                {
                    return ret;
                }

                ret = true;
            }
            catch
            {
                ret = false;
            }

            return ret;
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

        private void MouseDownHold(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseBtn(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeBtn(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
