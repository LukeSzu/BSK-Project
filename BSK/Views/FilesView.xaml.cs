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
    /// Interaction logic for FilesView.xaml
    /// </summary>
    public partial class FilesView : UserControl
    {
        public FilesView()
        {
            InitializeComponent();
        }
        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Globals.Connected && PathBox.Text != "" && Globals.is_transmision == false)
                {
                    Globals.is_transmision = true;
                    Globals.transmission_progress = 0.0;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("File");
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

                    using (Aes aes1 = Aes.Create())
                    using (Aes aes2 = Aes.Create())
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
                        sb.AppendLine(aes2.BlockSize.ToString());
                        aes2.Key = Globals.sessionKey;
                        sb.AppendLine(Convert.ToBase64String(Globals.sessionKey));
                        aes2.GenerateIV();
                        sb.AppendLine(Convert.ToBase64String(aes2.IV));
                        aes2.Padding = PaddingMode.PKCS7;
                        sb.AppendLine(Convert.ToString(PaddingMode.PKCS7));


                        

                        int bufferReadSize = 1024;
                        byte[] bufferRead = null;
                        FileStream fs = new FileStream(PathBox.Text, FileMode.Open);
                        bool read = true;
                        int bufferCount = Convert.ToInt32(Math.Ceiling((double)fs.Length / (double)bufferReadSize));


                        sb.AppendLine(fs.Length.ToString());
                        sb.AppendLine(System.IO.Path.GetFileName(PathBox.Text));

                        send(aes1, sb, Globals.clientStream);
                        sb.Clear();
                        Thread.Sleep(500);

                        
                        for (int i = 0; i < bufferCount; i++)
                        {
                            bufferRead = new byte[bufferReadSize];
                            int size = fs.Read(bufferRead, 0, bufferReadSize);
                            using (ICryptoTransform encryptor = aes2.CreateEncryptor(aes2.Key, aes2.IV))
                            {
                                using (var target = new MemoryStream())
                                {
                                    using (var cs = new CryptoStream(target, encryptor, CryptoStreamMode.Write))
                                    {
                                        using (var source = new StreamWriter(cs))
                                        {
                                            source.Write(Convert.ToBase64String(bufferRead));
                                        }
                                        if(Globals.clientStream.CanWrite)
                                            Globals.clientStream.Write(target.ToArray(), 0, target.ToArray().Length);
                                    }
                                }
                            
                            }
                            //Globals.transmission_progress = (double)(((i + 1) / bufferCount)*100);
                            //progress.Value = Globals.transmission_progress;
                        }
                        Globals.is_transmision = false;


                        fs.Close();
                    }
                }
            }
            catch (Exception ex)
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
                    ns.Write(target.ToArray(), 0, target.ToArray().Length);
                }
            }
        }
        private void SelectButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".pdf";
            dlg.Filter = "PDF Files (*.pdf)|*.pdf|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|Text Files (*.txt)|*.txt|Avi Files (*.avi)|*.avi";

            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                string filename = dlg.FileName;
                PathBox.Text = filename;
            }
        }
    }
}
