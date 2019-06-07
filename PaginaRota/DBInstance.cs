using System;
using System.Data.SQLite;
using System.IO;

namespace PaginaRota
{
    class DBContext
    {
        private const string DBName = "Data\\basePaginaRota.db";
        
        public static SQLiteConnection GetInstance()
        {
            var db = new SQLiteConnection(
                string.Format("Data Source={0};Version=3;", DBName)
            );

            db.Open();

            return db;
        }
    }
}
