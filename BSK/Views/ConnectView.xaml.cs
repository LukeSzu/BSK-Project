using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
using BSK;

namespace BSK.Views
{
    /// <summary>
    /// Interaction logic for ConnectView.xaml
    /// </summary>
    public partial class ConnectView : UserControl
    {
        public ConnectView()
        {
            InitializeComponent();
            Globals.AcceptButton = AcceptButton;
            Globals.ConnectButton = ConnectButton;
            Globals.DisconnectButton = DisconnectButton;
            if (Globals.Listening)
            {
                AcceptButton.Content = "Accepting";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = false;
            }
            else
            {
                AcceptButton.Content = "Not Accepting";
                if(Globals.Client != null)
                {
                    string ip = Globals.Client.Client.RemoteEndPoint.ToString();
                    string pattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";

                    IPInput.Text = Regex.Match(ip, pattern).Value;
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                    AcceptButton.IsEnabled = false;
                }
                else
                {
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    AcceptButton.IsEnabled = true;
                }
            }
        }

        private void ConnectButtonClick(object sender, RoutedEventArgs e)
        {
            string ip = IPInput.Text;
            string pattern = "^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (Globals.Connected == false)
            {
                if (Regex.IsMatch(ip, pattern))
                {
                    try
                    {
                        TcpClient client = new TcpClient();
                        client.Connect(ip, 6938);
                        if (client.Connected)
                        {
                            Globals.Client = client;
                            this.Dispatcher.Invoke(() =>
                            {
                                Globals.DockPanel.Background = new SolidColorBrush(Colors.SeaGreen);
                            });

                            Globals.Tester = new Thread(TestConnection);
                            Globals.Connected = true;
                            Globals.Tester.Start();
                            AcceptButton.IsEnabled = false;
                            ConnectButton.IsEnabled = false;
                            DisconnectButton.IsEnabled = true;

                            Globals.FilesButton.IsEnabled = true;
                            Globals.MessengerButton.IsEnabled = true;

                            //TODO
                            //wysłanie klucza publicznego
                        }
                    }
                    catch(SocketException ex)
                    {
                        IPInput.Text = "Connection refused";
                    }
                    
                }
                else
                {
                    IPInput.Text = "IP doesn't match regex policy";  
                }
            }
            else
            {
                IPInput.Text = "Disconnect first";
            }
        }

        private void DisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            if (Globals.Connected)
            {
                Globals.Client.GetStream().Dispose();
                Globals.Client.Close();

                this.Dispatcher.Invoke(() =>
                {
                    Globals.DockPanel.Background = new SolidColorBrush(Colors.Red);
                });
                Globals.Connected = false;
                AcceptButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;

                Globals.Listening = false;
                AcceptButton.Content = "Not Accepting";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = true;
                Globals.Client = null;

                Globals.FilesButton.IsEnabled = false;
                Globals.MessengerButton.IsEnabled = false;
            }
            else
            {
                IPInput.Text = "Connect first";
            }
        }

        private void AcceptButtonClick(object sender, RoutedEventArgs e)
        {
            if (Globals.Listening)
            {
                Globals.Listening = false;
                AcceptButton.Content = "Not Accepting";
                Globals.tcpListener.Stop();
                Globals.Listener.Join();
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
            }
            else
            {
                Globals.Listening = true;
                AcceptButton.Content = "Accepting";

                Globals.Listener = new Thread(ListenConnection);
                Globals.tcpListener.Start();
                Globals.Listener.Start();
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = false;
            }
        }
        public void ListenConnection()
        {
            try
            {
                Globals.Client = Globals.tcpListener.AcceptTcpClient();

                this.Dispatcher.Invoke(() =>
                {
                    Globals.DockPanel.Background = new SolidColorBrush(Colors.SeaGreen);
                    Globals.AcceptButton.IsEnabled = false;
                    Globals.ConnectButton.IsEnabled = false;
                    Globals.DisconnectButton.IsEnabled = true;
                    string ip = Globals.Client.Client.RemoteEndPoint.ToString();
                    string pattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                    IPInput.Text = Regex.Match(ip, pattern).Value;
                });
                Globals.Connected = true;
                Globals.Tester = new Thread(TestConnection);
                Globals.tcpListener.Stop();
                Globals.Tester.Start();
                Globals.Listening = false;

                //TODO
                //odebranie klucza publicznego
            }
            catch (SocketException ex)
            {

            }
        }

        private void TestConnection()
        {
            Socket s = Globals.Client.GetStream().Socket;
            while (Globals.Connected == true)
            {
                try
                {
                    bool pt1 = s.Poll(1000, SelectMode.SelectRead);
                    bool pt2 = (s.Available == 0);
                    if (pt1 && pt2)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Globals.DockPanel.Background = new SolidColorBrush(Colors.Red);
                            Globals.AcceptButton.IsEnabled = true;
                            Globals.ConnectButton.IsEnabled = true;
                            Globals.DisconnectButton.IsEnabled = false;
                            Globals.Connected = false;
                            Globals.Listening = false;
                            AcceptButton.Content = "Not Accepting";
                            Globals.Client.GetStream().Dispose();
                            Globals.Client.Close();
                            Globals.Client = null;

                            Globals.FilesButton.IsEnabled = false;
                            Globals.MessengerButton.IsEnabled = false;
                        });
                        

                    }
                }
                catch(Exception ex)
                {
                    if(ex is ObjectDisposedException || ex is SocketException)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Globals.DockPanel.Background = new SolidColorBrush(Colors.Red);
                            Globals.AcceptButton.IsEnabled = true;
                            Globals.ConnectButton.IsEnabled = true;
                            Globals.DisconnectButton.IsEnabled = false;
                            Globals.Connected = false;
                            Globals.Listening = false;
                            AcceptButton.Content = "Not Accepting";
                            Globals.Client.GetStream().Dispose();
                            Globals.Client.Close();
                            Globals.Client = null;

                            Globals.FilesButton.IsEnabled = false;
                            Globals.MessengerButton.IsEnabled = false;
                        });
                    }
                    
                } 
            }
        }
    }
}
