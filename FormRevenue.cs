using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormRevenue : Form
    {
        private readonly string _connString;
        private readonly int? _revenueId;
        public FormRevenue(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _revenueId = null;
            this.Load += FormRevenue_Load;
        }
        public FormRevenue(string connString, int revenueId) : this(connString)
        {
            _revenueId = revenueId;
        }
        private void FormRevenue_Load(object sender, EventArgs e)
        {
            LoadEmployees();
            LoadDrivers();

            cmbWorker.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDriver.DropDownStyle = ComboBoxStyle.DropDownList;

            if (_revenueId.HasValue)
                LoadRevenueForEdit(_revenueId.Value);
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
        private void LoadDrivers()
        {

            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
            SELECT DISTINCT employee_id,
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
                    cmbDriver.ValueMember = "employee_id";
                    cmbDriver.DataSource = dt;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbWorker.SelectedValue == null || cmbDriver.SelectedValue == null)
                {
                    MessageBox.Show("Выберите сотрудника и водителя.");
                    return;
                }

                if (!decimal.TryParse(tbTotal.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму (число больше 0).");
                    return;
                }

                DateTime fromDate = dtpFrom.Value.Date;
                DateTime toDate = dtpTo.Value.Date;

                if (toDate < fromDate)
                {
                    MessageBox.Show("Период 'по' не может быть меньше 'с'.");
                    return;
                }
                int employeeId = Convert.ToInt32(cmbWorker.SelectedValue);
                int driverId = Convert.ToInt32(cmbDriver.SelectedValue);

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    if (_revenueId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            UPDATE revenue
                            SET employee_id = @employee_id,
                                driver_id = @driver_id,
                                amount = @amount,
                                period_start = @from,
                                period_end = @to
                            WHERE id = @id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _revenueId.Value);
                            cmd.Parameters.AddWithValue("@employee_id", employeeId);
                            cmd.Parameters.AddWithValue("@driver_id", driverId);
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.Parameters.AddWithValue("@from", fromDate);
                            cmd.Parameters.AddWithValue("@to", toDate);

                            cmd.ExecuteNonQuery(); 
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO revenue (employee_id, driver_id, amount, period_start, period_end)
                            VALUES (@employee_id, @driver_id, @amount, @from, @to);", conn))
                        {
                            cmd.Parameters.AddWithValue("@employee_id", employeeId);
                            cmd.Parameters.AddWithValue("@driver_id", driverId);
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.Parameters.AddWithValue("@from", fromDate);
                            cmd.Parameters.AddWithValue("@to", toDate);

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
        private void LoadRevenueForEdit(int revenueId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT employee_id, driver_id, amount, period_start, period_end
                    FROM revenue
                    WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", revenueId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new Exception("Запись выручки не найдена.");

                        cmbWorker.SelectedValue = Convert.ToInt32(rd["employee_id"]);
                        cmbDriver.SelectedValue = Convert.ToInt32(rd["driver_id"]);
                        tbTotal.Text = Convert.ToDecimal(rd["amount"]).ToString(CultureInfo.CurrentCulture);
                        dtpFrom.Value = Convert.ToDateTime(rd["period_start"]);
                        dtpTo.Value = Convert.ToDateTime(rd["period_end"]);
                    }
                }
            }
        }
    }
}