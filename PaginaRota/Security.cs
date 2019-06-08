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
        public static bool IsUserAuthenticated(HttpListenerRequest req)
        {
            try
            {
                var authCookie = req.Cookies["Auth"];
                if (authCookie.Expired)
                    return false;

                var user = GetUserFromCookie(authCookie);

                var instance = DBContext.GetInstance();
                var command = instance.CreateCommand();
                
                command.CommandText = "Select isAdmin from Usuarios Where Username = '" + user + "';";

                var reader = command.ExecuteReader();
                reader.Read();

                return (string)reader[0] == "S";
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
    }
}
