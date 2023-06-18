using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BSK
{
    public static class Globals
    {
        public static bool Connected { get; set; }
        public static TcpListener tcpListener { get; set; }
        public static TcpClient Client { get; set; }
        public static NetworkStream clientStream { get; set; }
        public static DockPanel DockPanel { get; set; }
        public static Thread Listener { get; set; }
        public static Thread Tester { get; set; }
        public static bool Listening { get; set; }

        public static Button AcceptButton { get; set; }
        public static Button DisconnectButton { get; set; }
        public static Button ConnectButton { get; set; }


        public static Button UnlockButton { get; set; }
        public static Button ConnectMenuButton { get; set; }
        public static Button MessengerButton { get; set; }
        public static Button FilesButton { get; set; }

        public static RSACryptoServiceProvider Rsa { get; set; }
        public static RSACryptoServiceProvider Brsa { get; set; }

        public static byte[] sessionKey { get; set; }
    }
}
