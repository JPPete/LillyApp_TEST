using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for PrekoIFakt.xaml
    /// </summary>
    partial class PrekoIFakt : Window
    {



        public PrekoIFakt()
        {
            InitializeComponent();
        }





        private void Telekombtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvoren page za - TELEKOM PREKORACENJA");
            PrekoIFaktframe.Content = new Prekoracenja();
            Naslov.Content = "TELEKOM PREKORACENJA";
        }

        private void Fakturisanjebtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvoren page za - IZVESTAJ ZA FAKTURISANJE");
            PrekoIFaktframe.Content = new FakturisanjePage();
            Naslov.Content = "PODACI ZA FAKTURISANJE";
        }







    }
}
