using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace PaginaRota
{
    class Server
    {
        public void Run()
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

        private void ProcesarRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var path = context.Request.RawUrl;
            //Quito la / inicial del path
            path = path.Substring(1, path.Length - 1);
            var res = context.Response;

            if (path == "login.html" && req.HttpMethod == "POST")
            {
                HandleLogin(context);
                return;
            }

            if (path == "")
            {
                context.Response.Redirect("/index.html");
                context.Response.Close();
                return;
            }

            byte[] buffer;
            try
            {
                buffer = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                buffer = Encoding.UTF8.GetBytes(ex.Message);
            }

            res.ContentLength64 = buffer.LongLength;

            using (var output = res.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);

                Console.WriteLine("Respuesta: " + Encoding.UTF8.GetString(buffer));
            }
        }

        private void HandleLogin(HttpListenerContext context)
        {
            var req = context.Request;
            using (var stream = new StreamReader(req.InputStream))
            {
                var credentials = HttpUtility.ParseQueryString(stream.ReadToEnd());
                var resp = "Te loggeaste como user: " + credentials[0] + " con pass: " + credentials[1];

                var buf = Encoding.UTF8.GetBytes(resp);

                SendResponse(buf, context.Response);
            }
        }

        private void SendResponse(byte[] buffer, HttpListenerResponse res)
        {
            res.ContentLength64 = buffer.LongLength;

            using (var output = res.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);

                Console.WriteLine("Respuesta: " + Encoding.UTF8.GetString(buffer));
            }
        }
    }
}
