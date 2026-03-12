using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormRoute : Form
    {
        private readonly string _connString;
        private readonly int? _routeId;
        public FormRoute(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _routeId = null;
            this.Load += FormRoute_Load;
        }
        public FormRoute(string connString, int routeId) : this(connString)
        {
            _routeId = routeId;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string startStopName = tbStartStation.Text.Trim();
                string endStopName = tbEndStation.Text.Trim();
                string fullTimeText = mtbTime.Text.Trim();

                if (string.IsNullOrWhiteSpace(startStopName) ||
                    string.IsNullOrWhiteSpace(endStopName) ||
                    string.IsNullOrWhiteSpace(fullTimeText))
                {
                    MessageBox.Show("Заполните: начальная остановка, конечная остановка и время полного оборота.");
                    return;
                }

                TimeSpan fullTurnTime;
                if (!TimeSpan.TryParse(fullTimeText, out fullTurnTime))
                {
                    MessageBox.Show("Время полного оборота должно быть в формате ЧЧ:ММ (например 01:30).");
                    return;
                }

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            int startStopId = GetOrCreateStopId(conn, tx, startStopName);
                            int endStopId = GetOrCreateStopId(conn, tx, endStopName);

                            using (var cmd = new NpgsqlCommand())
                            {
                                cmd.Connection = conn;
                                cmd.Transaction = tx;

                                if (_routeId.HasValue)
                                {
                                    cmd.CommandText = @"
                                    UPDATE route
                                    SET start_stop_id = @start_stop_id,
                                        end_stop_id = @end_stop_id,
                                        full_turn_time = @full_turn_time
                                    WHERE id = @id;";
                                    cmd.Parameters.AddWithValue("@id", _routeId.Value);
                                }
                                else
                                {
                                    cmd.CommandText = @"
                                    INSERT INTO route (start_stop_id, end_stop_id, full_turn_time)
                                    VALUES (@start_stop_id, @end_stop_id, @full_turn_time);";
                                }

                                cmd.Parameters.AddWithValue("@start_stop_id", startStopId);
                                cmd.Parameters.AddWithValue("@end_stop_id", endStopId);
                                cmd.Parameters.AddWithValue("@full_turn_time", fullTurnTime);

                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private int GetOrCreateStopId(NpgsqlConnection conn, NpgsqlTransaction tx, string stopName)
        {
            stopName = (stopName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(stopName))
                throw new Exception("Название остановки не заполнено.");

            using (var cmd = new NpgsqlCommand(@"
                SELECT id
                FROM stop
                WHERE name = @name
                LIMIT 1;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@name", stopName);
                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO stop (name)
                VALUES (@name)
                RETURNING id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@name", stopName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void FormRoute_Load(object sender, EventArgs e)
        {
            if (_routeId.HasValue)
                LoadRouteForEdit(_routeId.Value);
        }
        private void LoadRouteForEdit(int routeId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
        SELECT s1.name,
                   s2.name,
                   r.full_turn_time::text
            FROM route r
            JOIN stop s1 ON s1.id = r.start_stop_id
            JOIN stop s2 ON s2.id = r.end_stop_id
            WHERE r.id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", routeId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Запись маршрута не найдена.");

                        tbStartStation.Text = r.GetString(0);
                        tbEndStation.Text = r.GetString(1);
                        string t = r.GetString(2);          
                        mtbTime.Text = (t != null && t.Length >= 5) ? t.Substring(0, 5) : t;
                    }
                }
            }
        }
    }
}
