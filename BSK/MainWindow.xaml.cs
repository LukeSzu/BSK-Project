using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BSK.ViewModels;
using BSK.Views;

namespace BSK
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();
            Globals.tcpListener = new TcpListener(System.Net.IPAddress.Any, 6938);
            Globals.dockPanel = dockPanel;
            Globals.Listening = false;


            Globals.dockPanel.Background = new SolidColorBrush(Colors.Red);
            Globals.Connected = false;
        }
        

        private void Menu_Button_Connect_Clicked(object sender, RoutedEventArgs e)
        {
            DataContext = new ConnectViewModel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Globals.Connected = false;

            if (Globals.client != null)
            {
                Globals.client.GetStream().Dispose();
                Globals.client.Close();
            }
            if(Globals.Listening)
                Globals.tcpListener.Stop();

            if(Globals.Listener != null)
                if(Globals.Listener.IsAlive)
                    Globals.Listener.Join();

            if (Globals.Tester != null)
                if (Globals.Tester.IsAlive)
                    Globals.Tester.Join();
        }
    }
}
