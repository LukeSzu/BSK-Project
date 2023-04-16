using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        public static TcpClient client { get; set; }
        public static DockPanel dockPanel { get; set; }
        public static Thread Listener { get; set; }
        public static Thread Tester { get; set; }
        public static bool Listening { get; set; }

        public static Button acc { get; set; }
        public static Button dsc { get; set; }
        public static Button con { get; set; }


    }
}
