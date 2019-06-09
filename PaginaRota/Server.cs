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
            var res = context.Response;

            if (path == "/login.html" && req.HttpMethod == "POST")
            {
                HandleLogin(context);
                return;
            }
            if(path == "/register.html" && req.HttpMethod == "POST")
            {
                HandleRegistration(context);
                return;
            }
            if( path.Contains("/scriptLoco") && req.HttpMethod == "POST")
            {
                HandleScriptLoco(context);
                return;
            }
            if( path.Contains("/ingresarScript") && req.HttpMethod == "POST")
            {
                HandleIngresarScript(context);
                return;
            }

            if (path == "/")
            {
                context.Response.Redirect("/login.html");
                context.Response.Close();
                return;
            }

            //Quito la / inicial del path
            path = path.Substring(1, path.Length - 1);

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
                string passHash = CreateMD5(newUser[1]);
                using (var instance = DBContext.GetNormalInstance())
                {
                    var command = instance.CreateCommand();

                    command.CommandText = "Insert into Usuarios(Username,Password,isAdmin) values('"
                        + newUser[0] + "','"
                        + passHash + "','N'"
                        + ")";

                    command.ExecuteNonQuery();

                    resp = "Usuario creado satisfactoriamente!";
                }
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
            string resp = "";
            try
            {
                var credentials = GetRequestBodyAsQueryString(req);
                using (var instance = DBContext.GetNormalInstance())
                {
                    var command = instance.CreateCommand();

                    string hash = CreateMD5(credentials[1]);
                    command.CommandText = "Select Password FROM Usuarios WHERE Username = @User AND Password = @Pass";
                    command.Parameters.AddWithValue("@User", credentials[0]);
                    command.Parameters.AddWithValue("@Pass", hash);

                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        
                        string cookieString = GenerateCookie();
                        var cookie = new Cookie("Auth", cookieString)
                        {
                            Expires = DateTime.Now.AddMinutes(30)
                        };

                        reader.Close();

                        command = instance.CreateCommand();
                        command.CommandText = "INSERT INTO Cookies(Cookie,Username,Expires) values(@cookie,@user,@expires);";

                        command.Parameters.AddWithValue("@cookie", cookieString);
                        command.Parameters.AddWithValue("@user", credentials[0]);
                        command.Parameters.AddWithValue("@expires", DateTime.Now.AddMinutes(30).ToString());

                        command.ExecuteNonQuery();

                        context.Response.SetCookie(cookie);
                        resp = "User '" + credentials[0] + "' logged in. Cookie assigned: " + cookieString;
                        File.AppendAllText("Logs\\Log.txt", DateTime.Now.ToString() + "|Login: " + resp + Environment.NewLine);

                        //if (reader["Password"].ToString() == credentials[1])
                        //    resp += " Login OK!!!";
                        //else resp += " Login Failed :(";

                        resp = "Login OK! Ya estas dentro del sistema";
                        reader.Close();
                    }
                    else resp += " Usuario o contraseña invalidos";
                }
            }
            catch(Exception ex)
            {
                resp = ex.Message;
            }

            var buf = Encoding.UTF8.GetBytes(resp);

            SendResponse(buf, context.Response);
        }

        //Aca tenemos el script compilado dinamicamente
        private void HandleScriptLoco(HttpListenerContext context)
        {
            var scriptName = this.GetRequestBodyAsQueryString(context.Request);
            string response;
            string code;

            if (!Security.IsUserAdmin(context.Request))
            {
                HandleNotAuthenticated(context);
                return;
            }
            
            try
            {
                using (var instance = DBContext.GetAdminInstance())
                {
                    var command = instance.CreateCommand();

                    command.CommandText = "Select ScriptCode from Scripts where ScriptName = '" + scriptName[0] + "'";

                    var reader = command.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        response = "No se encuentra el script";
                    }
                    else
                    {
                        reader.Read();

                        code = reader[0].ToString();
                        reader.Close();

                        var rc = new RuntimeCompiler(code);
                        var results = rc.CompileSourceCodeDom();

                        if (results.Errors.HasErrors)
                        {
                            response = "Errores de compilacion: ";
                            results.Errors.Cast<CompilerError>().ToList().ForEach(e => response += e.ErrorText + Environment.NewLine);
                        }
                        else
                        {
                            var assembly = rc.GetAssembly();
                            var returnValue = rc.ExecuteFromAssembly(assembly, "DynamicClass", "DynamicMethod") as string;
                            response = "Metodo dinamico ejecutado correctamente. Resultado: " + returnValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }
            
            var buf = Encoding.UTF8.GetBytes(response);
            SendResponse(buf, context.Response);
        }

        private void HandleIngresarScript(HttpListenerContext context)
        {
            if (!Security.IsUserAdmin(context.Request))
            {
                HandleNotAuthenticated(context);
                return;
            }

            string response;
            try
            {
                var newScript = GetRequestBodyAsQueryString(context.Request);

                var instance = DBContext.GetAdminInstance();
                var command = instance.CreateCommand();

                command.CommandText = "Insert into Scripts(ScriptName,ScriptCode) values('" + newScript[0] + "','" + newScript[1] + "');";

                command.ExecuteNonQuery();

                response = "Script Agregado Satisfactoriamente.";
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }

            var buf = Encoding.UTF8.GetBytes(response);
            SendResponse(buf, context.Response);
        }

        private void HandleNotAuthenticated(HttpListenerContext context)
        {
            var buf = Encoding.UTF8.GetBytes("Ud no esta autenticado");
            SendResponse(buf, context.Response);
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

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string GenerateCookie()
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            return GuidString;
        }

    }
}
