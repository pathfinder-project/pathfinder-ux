using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder
{
    static class Extension
    {
        public static int? GetInt32OrNull(this SQLiteDataReader rdr, int i)
        {
            bool isNull = rdr.IsDBNull(i);
            if (isNull)
            {
                return null;
            }
            else
            {
                return rdr.GetInt32(i);
            }
        }

        public static string Info(this int? x)
        {
            return x == null ? "null" : x.Value.ToString();
        }
    }
}
