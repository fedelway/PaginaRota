using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Reflection;
using System.Collections.Specialized;
using System.Linq;
using System.CodeDom.Compiler;

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
            if(path == "register.html" && req.HttpMethod == "POST")
            {
                HandleRegistration(context);
                return;
            }
            if( path.Contains("scriptLoco") && req.HttpMethod == "POST")
            {
                HandleScriptLoco(context);
                return;
            }

            if (path == "")
            {
                context.Response.Redirect("/index.html");
                context.Response.Close();
                return;
            }

            //Non-mapped routes just serve the file in the server directory
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

        private void HandleRegistration(HttpListenerContext context)
        {
            var req = context.Request;

            string resp;
            try
            {
                var newUser = this.GetRequestBodyAsQueryString(req);

                var instance = DBContext.GetInstance();
                var command = instance.CreateCommand();

                command.CommandText = "Insert into Usuarios(Username,Password) values('"
                    + newUser[0] + "','"
                    + newUser[1] + "'"
                    + ")";

                command.ExecuteNonQuery();

                resp = "Usuario creado satisfactoriamente!";
            }
            catch( Exception ex)
            {
                resp = ex.Message;
            }

            var buf = Encoding.UTF8.GetBytes(resp);
            SendResponse(buf, context.Response);
        }

        private void HandleLogin(HttpListenerContext context)
        {
            var req = context.Request;
            using (var stream = new StreamReader(req.InputStream) )
            {
                var credentials = HttpUtility.ParseQueryString(stream.ReadToEnd());
                var resp = "Te loggeaste como user: " + credentials[0] + " con pass: " + credentials[1];
                try
                {
                    File.AppendAllText("Logs\\Log.txt", DateTime.Now.ToString() + "|Login: " + resp + Environment.NewLine);

                    var instance = DBContext.GetInstance();
                    var command = instance.CreateCommand();

                    command.CommandText = "SELECT Password FROM Usuarios WHERE Username = '" + credentials[0] + "' AND Password = '" + credentials[1] + "'";
                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();

                        resp = "Login OK!!!";

                        //if (reader["Password"].ToString() == credentials[1])
                        //    resp += " Login OK!!!";
                        //else resp += " Login Failed :(";
                    }
                    else resp += " Usuario o contraseña invalidos";
                }
                catch(Exception ex)
                {
                    resp = ex.Message;
                }

                var buf = Encoding.UTF8.GetBytes(resp);

                SendResponse(buf, context.Response);
            }
        }

        //Aca tenemos el script compilado dinamicamente
        private void HandleScriptLoco(HttpListenerContext context)
        {
            var scriptName = this.GetRequestBodyAsQueryString(context.Request);
            string response;
            string code;
            try
            {
                var instance = DBContext.GetInstance();
                var command = instance.CreateCommand();

                command.CommandText = "Select ScriptCode from Scripts where ScriptName = '" + scriptName[0] + "'";

                var reader = command.ExecuteReader();

                reader.Read();

                code = reader[0].ToString();

                var rc = new RuntimeCompiler();
                var results = rc.CompileSourceCodeDom(code);

                if (results.Errors.HasErrors)
                {
                    response = "Errores de compilacion: ";
                    results.Errors.Cast<CompilerError>().ToList().ForEach(e => response += e.ErrorText + Environment.NewLine);
                }
                else
                {
                    var assembly = rc.GetAssembly();
                    rc.ExecuteFromAssembly(assembly, "DynamicClass", "DynamicMethod");
                    response = "Metodo dinamico ejecutado correctamente";
                }
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }

            var buf = Encoding.UTF8.GetBytes(response);
            SendResponse(buf, context.Response);

            /*var rc = new RuntimeCompiler();
            
            var assembly = rc.CompileSourceCodeDom(@"using System;using System.IO;
                    public class DynamicClass
                    {
                        public static void DynamicMethod()
                        {
                            File.WriteAllText( ""ArchivoDinamico.txt"", ""Hola como estas?"" );
                        }
                    }
            
                    ");
            
            rc.ExecuteFromAssembly(assembly, "DynamicClass", "DynamicMethod");
            */
        }

        private NameValueCollection GetRequestBodyAsQueryString(HttpListenerRequest req)
        {
            using (var stream = new StreamReader(req.InputStream))
            {
                var body = stream.ReadToEnd();

                return HttpUtility.ParseQueryString(body);
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
