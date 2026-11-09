using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace KYLMA.Data
{
    /// <summary>Connection and reader helpers. The connection string name is "Mangubat" in App.config.</summary>
    public static class Db
    {
        public const string ConnectionName = "Mangubat";

        public static string ConnectionString
        {
            get
            {
                var setting = ConfigurationManager.ConnectionStrings[ConnectionName];
                if (setting == null)
                    throw new InvalidOperationException("The configuration file has no connection string named '" + ConnectionName + "'.");
                return setting.ConnectionString;
            }
        }

        public static SqlConnection Open()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public static SqlCommand Proc(SqlConnection connection, string procedure)
        {
            return new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        }

        /// <summary>Maps null to DBNull so optional parameters can be passed.</summary>
        public static object Val(object value)
        {
            return value ?? DBNull.Value;
        }

        public static string Str(SqlDataReader r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? null : Convert.ToString(v);
        }

        public static int Int(SqlDataReader r, string column) { return Convert.ToInt32(r[column]); }
        public static decimal Dec(SqlDataReader r, string column) { return Convert.ToDecimal(r[column]); }
        public static bool Bool(SqlDataReader r, string column) { return Convert.ToBoolean(r[column]); }
        public static DateTime Date(SqlDataReader r, string column) { return Convert.ToDateTime(r[column]); }

        public static DateTime? DateOrNull(SqlDataReader r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(v);
        }
    }
}
