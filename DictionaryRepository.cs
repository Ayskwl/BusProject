using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Npgsql;

namespace Bus_coursework
{
    public class DictionaryRepository
    {
        private readonly string _connString;

        public DictionaryRepository(string connString)
        {
            _connString = connString;
        }
        public DataTable GetDictionary(string tableName, string columnName, string filter = null)
        {
            var table = new DataTable();

            using (var conn = new NpgsqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = $@"
                SELECT
                    id,
                    {columnName} AS ""Название""
                FROM {tableName}
            ";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    cmd.CommandText += @"
                    WHERE {columnName} ILIKE @p
                ";
                    cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                }

                cmd.CommandText += $" ORDER BY {columnName};";

                using (var r = cmd.ExecuteReader())
                    table.Load(r);
            }

            return table;
        }
    }
}

