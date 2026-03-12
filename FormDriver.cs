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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Bus_coursework
{
    public partial class FormDriver : Form
    {
        private readonly string _connString;
        private readonly int? _driverId;
        public FormDriver(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _driverId = null;
            this.Load += FormDriver_Load;
        }
        public FormDriver(string connString, int driverId) : this(connString)
        {
            _driverId = driverId;
        }
        private void FormDriver_Load(object sender, EventArgs e)
        {
            LoadBuses();
            LoadEmployees();
            LoadCategories();

            if (_driverId.HasValue)
                LoadDriverForEdit(_driverId.Value);
        }

        private void LoadBuses()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, registration_number FROM bus ORDER BY registration_number;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbNumBus.DataSource = null;
                    cmbNumBus.ValueMember = "id";
                    cmbNumBus.DisplayMember = "registration_number";
                    cmbNumBus.DataSource = dt; 
                }
            }
        }

        private void LoadEmployees()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT id,
                   CONCAT_WS(' ', worker.last_name, worker.first_name, worker.patronymic) AS fio
            FROM worker
            ORDER BY fio;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbWorker.DataSource = null;
                    cmbWorker.DisplayMember = "fio";
                    cmbWorker.ValueMember = "id";
                    cmbWorker.DataSource = dt;
                }
            }
        }

        private void LoadCategories()
        {
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.DataSource = new[] { "D", "D1" };
        }

        private void LoadDriverForEdit(int driverId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
            SELECT bus_id, employee_id, category::text
            FROM driver
            WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", driverId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Водитель не найден.");

                        cmbNumBus.SelectedValue = r.GetInt32(0);
                        cmbWorker.SelectedValue = r.GetInt32(1);

                        string category = r.GetString(2);
                        cmbCategory.SelectedItem = category;
                    }
                }
            }
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbNumBus.SelectedValue == null || cmbWorker.SelectedValue == null || cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Выберите: номер автобуса, ФИО и категорию.");
                return;
            }

            int busId = Convert.ToInt32(cmbNumBus.SelectedValue);
            int employeeId = Convert.ToInt32(cmbWorker.SelectedValue);
            string category = cmbCategory.SelectedItem.ToString();

            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (_driverId.HasValue)
                        {
                            using (var cmd = new NpgsqlCommand(@"
                        UPDATE driver
                        SET bus_id = @bus_id,
                            employee_id = @employee_id,
                            category = @category::driver_enum
                        WHERE id = @id;", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", _driverId.Value);
                                cmd.Parameters.AddWithValue("@bus_id", busId);
                                cmd.Parameters.AddWithValue("@employee_id", employeeId);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO driver (bus_id, employee_id, category)
                        VALUES (@bus_id, @employee_id, @category::driver_enum);", conn))
                            {
                                cmd.Parameters.AddWithValue("@bus_id", busId);
                                cmd.Parameters.AddWithValue("@employee_id", employeeId);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        MessageBox.Show("Ошибка сохранения: " + ex.Message);
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
