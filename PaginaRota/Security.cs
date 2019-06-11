using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PaginaRota
{
    //Insecure Security Methods
    class Security
    {
        public static bool IsUserAdmin(HttpListenerRequest req)
        {
            try
            {
                var authCookie = req.Cookies["Auth"];
                if (authCookie.Expired)
                    return false;

                var user = GetUserFromCookie(authCookie);

                using (var instance = DBContext.GetNormalInstance())
                {
                    var command = instance.CreateCommand();

                    command.CommandText = "Select Usuarios.isAdmin Cookies.Expires from Usuarios, Cookies WHERE Usuarios.Username = Cookies.Username AND Cookies.Cookie = @cookie;";
                    command.Parameters.AddWithValue("@cookie", user);

                    //command.CommandText = "Select isAdmin from Usuarios Where Username = '" + user + "';";

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();

                        var isAdmin = (string)reader[0] == "S";
                        var expirationDate = DateTime.Parse((string)reader[1]);

                        return isAdmin && DateTime.Now < expirationDate;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetUserFromCookie(Cookie cookie)
        {
            return cookie.Value;
        }

        public static bool isUserLoguedIn(HttpListenerRequest req)
        {
            try
            {
                var authCookie = req.Cookies["Auth"];
                return authCookie != null && !authCookie.Expired;
            }
            catch
            {
                return false;
            }
        }
    }

}