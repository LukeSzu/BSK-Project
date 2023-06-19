using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
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

namespace BSK.Views
{
    /// <summary>
    /// Interaction logic for MessengerView.xaml
    /// </summary>
    public partial class MessengerView : UserControl
    {
        public MessengerView()
        {
            InitializeComponent();
            Messages.Text = Globals.Messages;
            Globals.MessagesBox = Messages;
        }
        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Globals.Connected && MessageInput.Text != "")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Text");
                    bool mode = true;
                    if (cbcMode.IsChecked ?? false)
                    {
                        sb.AppendLine("cbc");
                    }
                    else
                    {
                        sb.AppendLine("ebc");
                        mode = false;
                    }

                    using(Aes aes1 = Aes.Create())
                    using(Aes aes2 = Aes.Create())
                    {
                        aes1.BlockSize = 128;
                        aes1.Mode = CipherMode.CBC;
                        aes1.Padding = PaddingMode.PKCS7;
                        aes1.IV = new byte[16] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120 };
                        aes1.Key = Globals.sessionKey;

                        if (mode)
                            aes2.Mode = CipherMode.CBC;
                        else
                            aes2.Mode = CipherMode.ECB;

                        aes2.BlockSize = 128;
                        sb.AppendLine("128");
                        aes2.Key = Globals.sessionKey;
                        sb.AppendLine(Convert.ToBase64String(Globals.sessionKey));
                        aes2.GenerateIV();
                        sb.AppendLine(Convert.ToBase64String(aes2.IV));
                        aes2.Padding = PaddingMode.PKCS7;
                        sb.AppendLine(Convert.ToString(PaddingMode.PKCS7));
                        

                        send(aes1, sb, Globals.clientStream);
                        sb.Clear();
                        
                        sb.AppendLine(MessageInput.Text);
                        MessageInput.Text = "";
                        send(aes2, sb, Globals.clientStream);
                        Globals.Messages += ("Me: " + sb.ToString());
                        Messages.Text += ("Me: " + sb.ToString());
                        
                        
                    }

                }
            }
            catch(Exception ex)
            {

            }
            

        }
        private void send(Aes aes, StringBuilder sb, NetworkStream ns)
        {
            using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                using (var target = new MemoryStream())
                {
                    using (var cs = new CryptoStream(target, encryptor, CryptoStreamMode.Write))
                    {
                        using (var source = new StreamWriter(cs))
                        {
                            source.Write(sb.ToString());
                        }
                    }
                    ns.Write(target.ToArray(), 0 , target.ToArray().Length);
                }
            }
        }
    }
}
