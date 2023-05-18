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
    /// Interaction logic for MessageBoxCustom.xaml
    /// </summary>
    public partial class MessageBoxCustom : Window
    {

        public MessageBoxCustom(string Message)
        {

            InitializeComponent();

            tbPoruka.Text = Message;

            
            lbNaslov.Content = "OBAVESTENJE";
            Title = "OBAVESTENJE";
            btnOK.Visibility = Visibility.Visible;
            btnDa.Visibility = Visibility.Collapsed;
            btnNe.Visibility = Visibility.Collapsed;
                    




        }




        public MessageBoxCustom(string Message, MessageType Type)
        {

            InitializeComponent();

            tbPoruka.Text = Message;

            switch (Type)
            {
                case MessageType.Info:
                    lbNaslov.Content = "OBAVESTENJE";
                    Title = "OBAVESTENJE";
                    btnOK.Visibility = Visibility.Visible;
                    btnDa.Visibility = Visibility.Collapsed;
                    btnNe.Visibility = Visibility.Collapsed;
                    break;
                case MessageType.Confirmation:
                    lbNaslov.Content = "PROVERA";
                    Title = "PROVERA";
                    btnOK.Visibility = Visibility.Collapsed;
                    btnDa.Visibility = Visibility.Visible;
                    btnNe.Visibility = Visibility.Visible;
                    btnNe.Focus();
                    break;
                case MessageType.Error:
                    lbNaslov.Content = "GRESKA";
                    Title = "GRESKA";
                    btnOK.Visibility = Visibility.Visible;
                    btnDa.Visibility = Visibility.Collapsed;
                    btnNe.Visibility = Visibility.Collapsed;
                    break;

            }




        }



        public MessageBoxCustom(string Message, MessageType Type, MessageButtons Buttons)
        {
           
            InitializeComponent();

            tbPoruka.Text = Message;

            switch (Type)
            {
                case MessageType.Info:
                    lbNaslov.Content = "OBAVESTENJE";
                    Title = "OBAVESTENJE";
                    break;
                case MessageType.Confirmation:
                    lbNaslov.Content = "PROVERA";
                    Title = "PROVERA";
                    break;
                case MessageType.Error:
                    lbNaslov.Content = "GRESKA";
                    Title = "GRESKA";
                    break;

            }

            switch(Buttons)
            {
                case MessageButtons.Ok:
                    btnDa.Visibility = Visibility.Collapsed;
                    btnNe.Visibility = Visibility.Collapsed;
                    btnOK.Visibility = Visibility.Visible;
                    break;
                case MessageButtons.YesNo:
                    btnOK.Visibility = Visibility.Collapsed;
                    btnDa.Visibility = Visibility.Visible;
                    btnNe.Visibility = Visibility.Visible;
                    btnNe.Focus();
                    break;
            }





        }


        public MessageBoxCustom(string Message, MessageType Type, MessageButtons Buttons,string Naslov, double Height, double Width, double FontSize)
        {

            InitializeComponent();

            tbPoruka.Text = Message;
            lbNaslov.Content = Naslov;

            tbPoruka.FontSize = FontSize;

            this.Height = Height;
            this.Width = Width;


            switch (Type)
            {
                case MessageType.Info:
                    lbNaslov.Content = "OBAVESTENJE";
                    Title = "OBAVESTENJE";
                    break;
                case MessageType.Confirmation:
                    lbNaslov.Content = "PROVERA";
                    Title = "PROVERA";
                    break;
                case MessageType.Error:
                    lbNaslov.Content = "GRESKA";
                    Title = "GRESKA";
                    break;

            }

            switch (Buttons)
            {
                case MessageButtons.Ok:
                    btnDa.Visibility = Visibility.Collapsed;
                    btnNe.Visibility = Visibility.Collapsed;
                    btnOK.Visibility = Visibility.Visible;
                    break;
                case MessageButtons.YesNo:
                    btnOK.Visibility = Visibility.Collapsed;
                    btnDa.Visibility = Visibility.Visible;
                    btnNe.Visibility = Visibility.Visible;
                    btnNe.Focus();
                    break;
            }





        }





        private void YesBtn(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OkBtn(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; 
            this.Close();
        }

        private void NoBtn(object sender, RoutedEventArgs e)
        {
            this.DialogResult= false;
            this.Close();
        }









        public enum MessageType
        {
            Info,
            Confirmation,
            Error,
        }



        public enum MessageButtons
        {
            Ok,
            YesNo,

        }

        private void MouseDownHold(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }//class MessageBoxCustom

}
