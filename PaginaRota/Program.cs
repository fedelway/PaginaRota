using System;
using System.IO;
using System.Net;

namespace PaginaRota
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpListener = new HttpListener();

            httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Start();

            while (true)
            {
                var context = httpListener.GetContext();
                ProcesarRequest(context);
            }
        }

        private static void ProcesarRequest(HttpListenerContext context)
        {
            var path = context.Request.RawUrl;
            //Quito la / inicial del path
            path = path.Substring(1, path.Length - 1);
            var res = context.Response;

            byte[] buffer;
            try
            {
                buffer = File.ReadAllBytes(path);
            }
            catch(Exception ex)
            {
                buffer = System.Text.Encoding.UTF8.GetBytes(ex.Message);
            }

            res.ContentLength64 = buffer.LongLength;

            using (var output = res.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
