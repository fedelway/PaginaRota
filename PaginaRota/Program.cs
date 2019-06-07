using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace PaginaRota
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.Run();
        }
    }
}
