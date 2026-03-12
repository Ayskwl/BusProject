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
    public partial class FormTrip : Form
    {
        private readonly string _connString;
        private readonly int? _tripId;
        public FormTrip(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _tripId = null;
            this.Load += FormTrip_Load;
        }
        public FormTrip(string connString, int busId) : this(connString)
        {
            _tripId = busId;
        }
        private void FormTrip_Load(object sender, EventArgs e)
        {
            LoadBuses();
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
            SELECT id
            FROM route
            ORDER BY id;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cbRouteNumber.DisplayMember = "id";
                    cbRouteNumber.ValueMember = "id";
                    cbRouteNumber.DataSource = dt;
                }
            }

            if (_tripId.HasValue)
                LoadTripForEdit(_tripId.Value);
        }
        private void LoadBuses()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT id, body_number
                    FROM bus
                    ORDER BY body_number;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbBus.DisplayMember = "body_number";
                    cmbBus.ValueMember = "id";
                    cmbBus.DataSource = dt;   
                }
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbBus.SelectedValue == null || cbRouteNumber.SelectedValue == null)
                {
                    MessageBox.Show("Выберите автобус и маршрут.");
                    return;
                }

                int tripNumber;
                if (!int.TryParse(tbTripNumber.Text.Trim(), out tripNumber) || tripNumber <= 0)
                {
                    MessageBox.Show("Номер рейса должен быть целым числом больше 0.");
                    return;
                }

                int busId = Convert.ToInt32(cmbBus.SelectedValue);
                int routeId = Convert.ToInt32(cbRouteNumber.SelectedValue);

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    using (var check = new NpgsqlCommand("SELECT 1 FROM route WHERE id = @id;", conn))
                    {
                        check.Parameters.AddWithValue("@id", routeId);
                        var ok = check.ExecuteScalar();
                        if (ok == null)
                        {
                            MessageBox.Show("Маршрут (id) не найден. Введите существующий id маршрута.");
                            return;
                        }
                    }

                    if (_tripId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            UPDATE trip
                            SET bus_id = @bus_id,
                                route_id = @route_id,
                                number = @number
                            WHERE id = @id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _tripId.Value);
                            cmd.Parameters.AddWithValue("@bus_id", busId);
                            cmd.Parameters.AddWithValue("@route_id", routeId);
                            cmd.Parameters.AddWithValue("@number", tripNumber);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO trip (bus_id, route_id, number)
                            VALUES (@bus_id, @route_id, @number);", conn))
                        {
                            cmd.Parameters.AddWithValue("@bus_id", busId);
                            cmd.Parameters.AddWithValue("@route_id", routeId);
                            cmd.Parameters.AddWithValue("@number", tripNumber);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
       
        private void LoadTripForEdit(int tripId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"SELECT bus_id, route_id, number
                    FROM trip
                    WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", tripId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Рейс не найден.");

                        cmbBus.SelectedValue = r.GetInt32(0);
                        cbRouteNumber.SelectedValue = r.GetInt32(1);
                        tbTripNumber.Text = Convert.ToString(r.GetValue(2));
                    }
                }
            }
        }
    }
}
