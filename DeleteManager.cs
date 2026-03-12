using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Forms;
using Npgsql;

namespace Bus_coursework
{
    public class DeleteManager
    {
        private readonly DataGridView _grid;
        private readonly string _connectionString;
        private readonly Dictionary<string, string> _entityTables;

        public DeleteManager(DataGridView grid, string connectionString)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            _entityTables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bus"] = "bus",
                ["Route"] = "route",
                ["Trip"] = "trip",
                ["Worker"] = "worker",
                ["Schedule"] = "schedule",
                ["Tk"] = "tk",
                ["Shift"] = "shift",
                ["Driver"] = "driver",
                ["Revenue"] = "revenue",
                ["Control"] = "control",

                ["brand"] = "brand",
                ["model"] = "model",
                ["city"] = "city",
                ["street"] = "street",
                ["post"] = "post",
                ["stop"] = "stop",
                ["department"] = "department",
                ["specialty"] = "specialty",
                ["qualification"] = "qualification",
                ["workplace"] = "workplace"
            };
        }

        private int? GetSelectedId()
        {
            if (_grid.CurrentRow == null) return null;
            if (!_grid.Columns.Contains("id")) return null;

            var val = _grid.CurrentRow.Cells["id"].Value;
            if (val == null || val == DBNull.Value) return null;

            return Convert.ToInt32(val);
        }

        public bool DeleteCurrentRow(string entityKey)
        {
            if (string.IsNullOrWhiteSpace(entityKey))
            {
                MessageBox.Show("Не определена текущая сущность для удаления.");
                return false;
            }

            var id = GetSelectedId();
            if (id == null)
            {
                MessageBox.Show("Выберите строку с колонкой id для удаления.");
                return false;
            }

            if (MessageBox.Show("Удалить выбранную запись?",
                                "Подтверждение",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) != DialogResult.Yes)
                return false;

            if (!_entityTables.TryGetValue(entityKey, out string tableName))
            {
                MessageBox.Show("Для этой таблицы удаление не настроено.");
                return false;
            }

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = $"DELETE FROM {tableName} WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", id.Value);
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Ошибка базы данных при удалении:\n" + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении:\n" + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
