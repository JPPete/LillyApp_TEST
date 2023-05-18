using System;
using System.IO;
using System.Windows;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for Narudzbenice.xaml
    /// </summary>
    public partial class Narudzbenice : Window
    {



        public Narudzbenice()
        {
            InitializeComponent();
        }



        private void ProveraNNbtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvorena stanica za\tPROVERU NARUDZBENICA");
            NNframe.Content = new ProveraNNPage();
        }

        private void NNuPosltbn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvorena stanica za\tNARUDZBENICE U POSL");
            NNframe.Content = new NNuPoslPage();
        }

        private void NNKomitentbtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvorena stanica za\tPROVERU KOMITENTA");
            NNframe.Content = new NNKomitentPage();
        }

        private void AMbtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("otvorena stanica za\tPROVERU ALTERMEDIJE");
            NNframe.Content = new AltermedijaPage();
        }



    }
}
