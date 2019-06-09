using System;
using System.Data.SQLite;
using System.IO;

namespace PaginaRota
{
    class DBContext
    {
        private const string NormalDBName = "Data\\basePaginaRota.db";
        private const string AdminDBName = "Data\\baseAdmin.db";

        public static SQLiteConnection GetNormalInstance()
        {
            var db = new SQLiteConnection(
                string.Format("Data Source={0};Version=3;", NormalDBName)
            );

            db.Open();

            return db;
        }

        public static SQLiteConnection GetAdminInstance()
        {
            var db = new SQLiteConnection(
                string.Format("Data Source={0};Version=3;", AdminDBName)
            );

            db.Open();

            return db;
        }
    }
}
