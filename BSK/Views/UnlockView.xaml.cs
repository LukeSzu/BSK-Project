using BSK.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    /// Interaction logic for UnlockView.xaml
    /// </summary>
    public partial class UnlockView : UserControl
    {
        public UnlockView()
        {
            InitializeComponent();
        }

        private void UnlockButtonClick(object sender, RoutedEventArgs e)
        {
            byte[] private_key;
            var password = PasswordInput.Text;
            byte[] hash;

            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(password));
            }

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var private_path = "keys/private/private.txt";
                    var public_path = "keys/public/public.pem";
                    if (File.Exists(private_path))
                    {
                        using(StreamReader file = new StreamReader(private_path))
                        {
                            byte[] message = Convert.FromBase64String(file.ReadToEnd());
                            using (Aes aes = Aes.Create())
                            {
                                aes.Key = hash;
                                aes.Mode = CipherMode.CBC;
                                aes.BlockSize = 128;
                                aes.IV = new byte[16] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120 };
                                aes.Padding = PaddingMode.PKCS7;
                                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                                {
                                    using (var msDecrypt = new MemoryStream(message))
                                    {
                                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                                        {
                                            using (var srDecrypt = new StreamReader(csDecrypt))
                                            {
                                                private_key =  Convert.FromBase64String(srDecrypt.ReadToEnd());
                                            }
                                }}}
                            }
                        }
                    }
                    else
                    { 
                        using (StreamWriter file = File.AppendText(private_path))
                        {
                            private_key = rsa.ExportRSAPrivateKey();
                            using(Aes aes = Aes.Create())
                            {
                                aes.Key = hash;
                                aes.Mode = CipherMode.CBC;
                                aes.BlockSize = 128;
                                aes.IV = new byte[16] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120 };
                                aes.Padding = PaddingMode.PKCS7;
                                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                                {
                                    using (var target = new MemoryStream())
                                    {
                                        using (var cs = new CryptoStream(target, encryptor, CryptoStreamMode.Write))
                                        {
                                            using (var source = new StreamWriter(cs))
                                            {
                                               source.Write(Convert.ToBase64String(private_key));
                                            }
                                        }
                                        file.Write(Convert.ToBase64String(target.ToArray()));
                                    }
                                }
                            }
                        }
                        using (StreamWriter file = File.AppendText(public_path))
                        {
                            UsefulFunc.ExportPublicKey(rsa, file);
                        }
                    }
                    Globals.FilesButton.IsEnabled = true;
                    Globals.MessengerButton.IsEnabled = true;
                    Globals.ConnectMenuButton.IsEnabled = true;
                    Globals.UnlockButton.IsEnabled = false;
                    UnlockButton.IsEnabled = false;
                    PasswordInput.IsEnabled = false;
                    PasswordInput.Text = "Go to connect panel";
                    Globals.Rsa = new RSACryptoServiceProvider(2048);
                    int no_bytes;
                    Globals.Rsa.ImportRSAPrivateKey(private_key, out no_bytes);
                }
                catch(Exception exc)
                {
                    PasswordInput.Text = "Wrong password";
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        private void PasswordInputMouseClick(object sender, MouseButtonEventArgs e)
        {
            PasswordInput.Text = "";
        }

    }
}
