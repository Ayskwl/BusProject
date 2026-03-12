using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;

namespace Bus_coursework
{
    public class SqlTemplateRepository
    {
        private readonly string _connString;

        public SqlTemplateRepository(string connString)
        {
            _connString = connString ?? throw new ArgumentNullException(nameof(connString));
        }

        public List<SqlTemplateItem> GetAllTemplates()
        {
            var list = new List<SqlTemplateItem>();

            using (var conn = new NpgsqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
                    SELECT id, name
                    FROM sql_template
                    ORDER BY name;";

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new SqlTemplateItem
                        {
                            Id = r.GetInt32(0),
                            Name = r.GetString(1)
                        });
                    }
                }
            }

            return list;
        }

        public void SaveTemplate(string name, string sqlText)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название шаблона не может быть пустым.", nameof(name));

            if (string.IsNullOrWhiteSpace(sqlText))
                throw new ArgumentException("SQL текст не может быть пустым.", nameof(sqlText));

            using (var conn = new NpgsqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
                    INSERT INTO sql_template (name, sql_text)
                    VALUES (@n, @t);";

                cmd.Parameters.AddWithValue("n", name.Trim());
                cmd.Parameters.AddWithValue("t", sqlText);

                cmd.ExecuteNonQuery();
            }
        }

        public string GetTemplateSql(int id)
        {
            using (var conn = new NpgsqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
                    SELECT sql_text
                    FROM sql_template
                    WHERE id = @id;";

                cmd.Parameters.AddWithValue("id", id);

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : (string)result;
            }
        }

        public bool IsDangerousSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return true;

            string s = sql.Trim().ToLowerInvariant();

            if (s.Contains(";"))
                return true;

            if (!s.StartsWith("select "))
                return true;

            string[] forbidden =
            {
                "insert ", "update ", "delete ", "truncate ", "merge ",
                "drop ", "alter ", "create ",
                "grant ", "revoke ",
                "begin", "commit", "rollback",
                "call ", "do ", "execute ",
                "copy ",
                "vacuum", "analyze",
                "set ", "reset ", "show "
            };

            foreach (var word in forbidden)
            {
                if (s.Contains(word))
                    return true;
            }

            return false;
        }

        public DataTable ExecuteSafeSelect(string sql)
        {
            if (IsDangerousSql(sql))
                throw new InvalidOperationException(
                    "Опасный SQL-запрос. Разрешены только операции чтения (SELECT) без ';'.");

            using (var conn = new NpgsqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = sql;

                var table = new DataTable();
                using (var r = cmd.ExecuteReader())
                    table.Load(r);

                return table;
            }
        }
    }
}