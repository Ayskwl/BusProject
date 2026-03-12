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
    public partial class FormShift : Form
    {
        private readonly string _connString;
        private int? _shiftId;
        public FormShift(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _shiftId = null;
            this.Load += FormShift_Load;
        }
        public FormShift(string connString, int shiftId) : this(connString)
        {
            _shiftId = shiftId;
        }
        private void FormShift_Load(object sender, EventArgs e)
        {
            LoadDrivers();
            LoadShiftTypes();
            if (_shiftId.HasValue)
                LoadShiftForEdit(_shiftId.Value);
        }
        private void LoadDrivers()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT driver.id,
                   CONCAT_WS(' ', worker.last_name, worker.first_name, worker.patronymic) AS fio
            FROM driver 
            JOIN worker ON worker.id = driver.employee_id
            ORDER BY fio;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbDriver.DataSource = null;
                    cmbDriver.DisplayMember = "fio";
                    cmbDriver.ValueMember = "id";
                    cmbDriver.DataSource = dt; 
                }
            }
        }
        private void LoadShiftTypes()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT unnest(enum_range(NULL::work_shift_enum ))::text;", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        cmbShiftType.Items.Add(reader.GetString(0));
                }
            }
            if (cmbShiftType.Items.Count > 0)
                cmbShiftType.SelectedIndex = 0;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbDriver.SelectedValue == null)
                {
                    MessageBox.Show("Выберите водителя.");
                    return;
                }
                string shiftType = cmbShiftType.SelectedItem.ToString();
                if (string.IsNullOrWhiteSpace(shiftType))
                {
                    MessageBox.Show("Выберите тип смены.");
                    return;
                }
                
                int driverId = Convert.ToInt32(cmbDriver.SelectedValue);
                DateTime startWork = dtpStartWork.Value;
                DateTime endWork = dtpEndWork.Value;
                if (startWork == endWork)
                {
                    MessageBox.Show("Время начала и окончания не может совпадать.");
                    return;
                }

                if (endWork < startWork)
                    endWork = endWork.AddDays(1);

                TimeSpan duration = endWork - startWork;

                if (duration.TotalHours > 24)
                {
                    MessageBox.Show("Смена не может длиться более 24 часов.");
                    return;
                }
                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    if (_shiftId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                    UPDATE shift
                    SET driver_id = @driver_id,
                        shift_type = @shift_type::work_shift_enum,
                        start_time = @start_time,
                        end_time = @end_time
                    WHERE id = @id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _shiftId.Value);
                            cmd.Parameters.AddWithValue("@driver_id", driverId);
                            cmd.Parameters.AddWithValue("@shift_type", shiftType);
                            cmd.Parameters.AddWithValue("@start_time", startWork);
                            cmd.Parameters.AddWithValue("@end_time", endWork);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO shift
                    (driver_id, shift_type, start_time, end_time)
                    VALUES
                    (@driver_id, @shift_type::work_shift_enum, @start_time, @end_time);", conn))
                        {
                            cmd.Parameters.AddWithValue("@driver_id", driverId);
                            cmd.Parameters.AddWithValue("@shift_type", shiftType);
                            cmd.Parameters.AddWithValue("@start_time", startWork);
                            cmd.Parameters.AddWithValue("@end_time", endWork);

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
        private void LoadShiftForEdit(int shiftId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT driver_id,
                           shift_type::text,
                           start_time,
                           end_time
                    FROM shift
                    WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", shiftId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Смена не найдена.");

                        cmbDriver.SelectedValue = r.GetInt32(0);
                        cmbShiftType.SelectedItem = r.GetString(1);
                        dtpStartWork.Value = r.GetDateTime(2);
                        dtpEndWork.Value = r.GetDateTime(3);
                    }
                }
            }
        }
    }
}



