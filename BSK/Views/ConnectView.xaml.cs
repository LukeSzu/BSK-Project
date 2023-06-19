using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
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
                            Globals.clientStream = client.GetStream();
                            this.Dispatcher.Invoke(() =>
                            {
                                Globals.DockPanel.Background = new SolidColorBrush(Colors.SeaGreen);
                                Globals.MessengerButton.IsEnabled = true;
                                Globals.FilesButton.IsEnabled = true;
                            });

                            Globals.Tester = new Thread(TestConnection);
                            Globals.Connected = true;
                            Globals.Tester.Start();
                            AcceptButton.IsEnabled = false;
                            ConnectButton.IsEnabled = false;
                            DisconnectButton.IsEnabled = true;

                            Globals.FilesButton.IsEnabled = true;
                            Globals.MessengerButton.IsEnabled = true;

                            //1
                            //Wyslij klucz publiczny 
                            var publicKeyXml = Globals.Rsa.ToXmlString(false);
                            byte[] publicKeyBytes = System.Text.Encoding.ASCII.GetBytes(publicKeyXml);
                            Globals.clientStream.Write(publicKeyBytes, 0, publicKeyBytes.Length);

                            //4
                            //Otrzymaj zaszyfrowany klucz sesji
                            byte[] readBuffer = new byte[256];
                            int numberOfBytesRead = 0;
                            numberOfBytesRead = Globals.clientStream.Read(readBuffer, 0, readBuffer.Length);
                            var sessionKey = Globals.Rsa.Decrypt(readBuffer, false);
                            Globals.sessionKey = sessionKey;

                            Globals.FileMessListener = new Thread(FileMessListen);
                            Globals.FileMessListener.Start();


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
        public static byte[] GenerateKey()
        {
            using(Aes aes = Aes.Create())
            {
                return aes.Key;
            }
        }
        public void ListenConnection()
        {
            try
            {
                Globals.Client = Globals.tcpListener.AcceptTcpClient();
                
                Globals.clientStream = Globals.Client.GetStream();

                this.Dispatcher.Invoke(() =>
                {
                    Globals.DockPanel.Background = new SolidColorBrush(Colors.SeaGreen);
                    Globals.AcceptButton.IsEnabled = false;
                    Globals.ConnectButton.IsEnabled = false;
                    Globals.DisconnectButton.IsEnabled = true;
                    string ip = Globals.Client.Client.RemoteEndPoint.ToString();
                    string pattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                    IPInput.Text = Regex.Match(ip, pattern).Value;
                    Globals.MessengerButton.IsEnabled = true;
                    Globals.FilesButton.IsEnabled = true;
                });
                Globals.Connected = true;
                Globals.Tester = new Thread(TestConnection);
                Globals.tcpListener.Stop();
                Globals.Tester.Start();
                Globals.Listening = false;

                Globals.Brsa = new RSACryptoServiceProvider(2048);
                Globals.Brsa.PersistKeyInCsp = false;

                //2
                //Otrzymaj klucz publiczny i go ustaw
                byte[] keybuffer = new byte[2048];
                StringBuilder keyStringBuilder = new StringBuilder();
                int nobytes = Globals.clientStream.Read(keybuffer, 0, keybuffer.Length);
                keyStringBuilder.AppendFormat("{0}", Encoding.ASCII.GetString(keybuffer, 0, nobytes));
                Globals.Brsa.FromXmlString(keyStringBuilder.ToString());

                //3
                //Wygeneruj klucz sesji i go wyslij
                Globals.sessionKey = GenerateKey();
                byte[] message = Globals.sessionKey;
                var encryptedData = Globals.Brsa.Encrypt(message, false);
                Globals.clientStream.Write(encryptedData, 0, encryptedData.Length);

                Globals.FileMessListener = new Thread(FileMessListen);
                Globals.FileMessListener.Start();

                
            }
            catch (SocketException ex)
            {
                
            }
        }
        private void FileMessListen()
        {
            NetworkStream ns = Globals.clientStream;
            while (Globals.Connected)
            {
                if (ns.DataAvailable)
                {
                    Globals.is_transmision = true;
                    byte[] readBuffer = new byte[2048];
                    StringBuilder optionStrings = new StringBuilder();
                    int bytesRead = ns.Read(readBuffer, 0, readBuffer.Length);
                    ns.Flush();
                    string[] options;

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = Globals.sessionKey;
                        aes.Mode = CipherMode.CBC;
                        aes.BlockSize = 128;
                        aes.IV = new byte[16] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120 };
                        aes.Padding = PaddingMode.PKCS7;

                        using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                        {
                            using (var msDecrypt = new MemoryStream(readBuffer, 0, bytesRead))
                            {
                                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                                {
                                    csDecrypt.Write(readBuffer, 0, bytesRead);
                                    csDecrypt.FlushFinalBlock();
                                    byte[] message = msDecrypt.ToArray();
                                    optionStrings.AppendFormat("{0}", Encoding.ASCII.GetString(message, 0, message.Length));
                                    options = optionStrings.ToString().Split("\r\n");
                                }
                            }
                        }
                    }
                    if(options[0] == "Text")
                    {
                        using(Aes aes = Aes.Create())
                        {
                            if (options[1] == "cbc")
                                aes.Mode = CipherMode.CBC;
                            else if (options[1] == "ebc")
                                aes.Mode = CipherMode.ECB;

                            aes.BlockSize = int.Parse(options[2]);
                            aes.Key = Convert.FromBase64String(options[3]);
                            aes.IV = Convert.FromBase64String(options[4]);

                            if (options[5] == "PKCS7")
                                aes.Padding = PaddingMode.PKCS7;

                            StringBuilder lines = new StringBuilder();
                            bytesRead = ns.Read(readBuffer, 0, readBuffer.Length);
                            ns.Flush();
                            string[] line;

                            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                            {
                                using (var msDecrypt = new MemoryStream(readBuffer, 0, bytesRead))
                                {
                                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                                    {
                                        csDecrypt.Write(readBuffer, 0, bytesRead);
                                        csDecrypt.FlushFinalBlock();
                                        byte[] message = msDecrypt.ToArray();
                                        lines.AppendFormat("{0}", Encoding.ASCII.GetString(message, 0, message.Length));
                                        line = lines.ToString().Split("\r\n");

                                        this.Dispatcher.Invoke(() =>
                                        {
                                            Globals.MessagesBox.Text += ("Sb: " + line[0] + '\n');
                                            Globals.Messages += ("Sb: " + line[0] + '\n');
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else if(options[0] == "File")
                    {
                        using (Aes aes = Aes.Create())
                        {
                            if (options[1] == "cbc")
                                aes.Mode = CipherMode.CBC;
                            else if (options[1] == "ebc")
                                aes.Mode = CipherMode.ECB;

                            aes.BlockSize = int.Parse(options[2]);
                            aes.Key = Convert.FromBase64String(options[3]);
                            aes.IV = Convert.FromBase64String(options[4]);

                            if (options[5] == "PKCS7")
                                aes.Padding = PaddingMode.PKCS7;

                            long filesize = long.Parse(options[6]);
                            string name = options[7];


                            int bufferSize = 1376;
                            byte[] buffer = null;

                            int bufferSize2 = 1024;
                            int bufferCount = Convert.ToInt32(Math.Ceiling((double)filesize / (double)bufferSize));
                            FileStream fs = new FileStream("received/"+name, FileMode.OpenOrCreate);

                            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                            {
                                while(filesize > 0)
                                {
                                    buffer = new byte[bufferSize];
                                    if (Globals.clientStream.CanRead)
                                    {
                                        int size = Globals.clientStream.Read(buffer, 0, bufferSize);

                                        using (var msDecrypt = new MemoryStream(buffer))
                                        {
                                            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                                            {
                                                using (var srDecrypt = new StreamReader(csDecrypt))
                                                {
                                                    byte[] buffer2 = Convert.FromBase64String(srDecrypt.ReadToEnd());
                                                    fs.Write(buffer2, 0, buffer2.Length);
                                                    filesize -= buffer2.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                                
                            }
                            fs.Close();
                            Globals.is_transmision = false;
                        }
                    }

                }
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
                            Globals.ConnectMenuButton.IsEnabled = true;
                        });


                    }
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is SocketException)
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
                            Globals.ConnectMenuButton.IsEnabled = true;
                        });
                    }

                }
                Thread.Sleep(1000);
            }
        }
    }
}
