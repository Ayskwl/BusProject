using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Bus_coursework
{
    public partial class BusForm : Form
    {
        private readonly string _connString;
        private readonly int? _busId;
        public BusForm(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _busId = null;
            this.Load += BusForm_Load;
        }
        public BusForm(string connString, int busId) : this(connString)
        {
            _busId = busId;
        }

        private void btnSaveBus_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateForm())
                {
                    MessageBox.Show(
                        "Исправьте ошибки в форме",
                        "Ошибка ввода",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string registration_number = tbStateNumber.Text.Trim();
                string bodyNumber = tbBodyNumber.Text.Trim();
                string chassisNumber = tbChassisNumber.Text.Trim();
                string identification_number = tbInventoryNumber.Text.Trim();
                string brand_name = tbBrand.Text.Trim();
                string model_name = tbModel.Text.Trim();
                string status = cbCondition.Text.Trim();
                DateTime releaseDate = dtpReleaseDate.Value.Date;


                if (!int.TryParse(tbCapacity.Text.Trim(), out int capacity))
                {
                    MessageBox.Show("Вместимость должна быть числом.");
                    return;
                }


                if (string.IsNullOrWhiteSpace(registration_number) ||
                    string.IsNullOrWhiteSpace(brand_name) ||
                    string.IsNullOrWhiteSpace(model_name))
                {
                    MessageBox.Show("Заполните: Гос. номер, Марка, Модель.");
                    return;
                }

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            int brandId = GetOrCreateBrandId(conn, brand_name);
                            int modelId = GetOrCreateModelId(conn, model_name);

                            if (_busId.HasValue)
                            {
                                using (var cmd = new NpgsqlCommand(@"
                    UPDATE bus
                    SET registration_number = @registration_number,
                        mileage = @mileage,
                        capacity = @capacity,
                        body_number = @body_number,
                        chassis_number = @chassis_number,
                        identification_number = @identification_number,
                        brand_id = @brand_id,
                        model_id = @model_id,
                        status = @status::bus_status_enum,
                        release_date = @release_date
                    WHERE id = @id;", conn))
                                {

                                    cmd.Parameters.AddWithValue("@id", _busId.Value);
                                    cmd.Parameters.AddWithValue("@registration_number", registration_number);
                                    cmd.Parameters.AddWithValue("@mileage", (int)nudMileage.Value);
                                    cmd.Parameters.AddWithValue("@capacity", capacity);
                                    cmd.Parameters.AddWithValue("@body_number", (object)bodyNumber ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@chassis_number", (object)chassisNumber ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@identification_number", identification_number);
                                    cmd.Parameters.AddWithValue("@brand_id", brandId);
                                    cmd.Parameters.AddWithValue("@model_id", modelId);
                                    cmd.Parameters.AddWithValue("@status", status);
                                    cmd.Parameters.AddWithValue("@release_date", releaseDate);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO bus
                        (registration_number, mileage, capacity, body_number, chassis_number,
                         identification_number, brand_id, model_id, status, release_date)
                    VALUES
                        (@registration_number, @mileage, @capacity, @body_number, @chassis_number,
                         @identification_number, @brand_id, @model_id, @status::bus_status_enum, @release_date);", conn))
                                {

                                    cmd.Parameters.AddWithValue("@registration_number", registration_number);
                                    cmd.Parameters.AddWithValue("@mileage", (int)nudMileage.Value);
                                    cmd.Parameters.AddWithValue("@capacity", capacity);
                                    cmd.Parameters.AddWithValue("@body_number", (object)bodyNumber ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@chassis_number", (object)chassisNumber ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@identification_number", identification_number);
                                    cmd.Parameters.AddWithValue("@brand_id", brandId);
                                    cmd.Parameters.AddWithValue("@model_id", modelId);
                                    cmd.Parameters.AddWithValue("@status", status);
                                    cmd.Parameters.AddWithValue("@release_date", releaseDate);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }
        private int GetOrCreateBrandId(NpgsqlConnection conn, string brandId)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT id FROM brand WHERE brand_name = @name LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@name", brandId);
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            using (var cmd = new NpgsqlCommand(@"
        INSERT INTO brand (brand_name)
        VALUES (@name)
        RETURNING id;", conn))
            {
                cmd.Parameters.AddWithValue("@name", brandId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private int GetOrCreateModelId(NpgsqlConnection conn, string modelId)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT id FROM model WHERE model_name = @name LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@name", modelId);
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            using (var cmd = new NpgsqlCommand(@"
        INSERT INTO model (model_name)
        VALUES (@name)
        RETURNING id;", conn))
            {
                cmd.Parameters.AddWithValue("@name", modelId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void btnCancelBus_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tbStateNumber_TextChanged(object sender, EventArgs e)
        {
            if (lblPlateError.Visible)
                ValidateForm();
        }
        bool ValidateForm()
        {
            bool valid = true;

            string registration_number = tbStateNumber.Text
                .Trim()
                .ToUpper()
                .Replace(" ", "");

            tbStateNumber.Text = registration_number;

            if (string.IsNullOrEmpty(registration_number))
            {
                SetError(tbStateNumber, lblPlateError, "Введите гос. номер");
                valid = false;
            }
            else if (!Regex.IsMatch(registration_number, @"^[A-ЯA-Z0-9]+$"))
            {
                SetError(tbStateNumber, lblPlateError,
                    "Только заглавные буквы и цифры. Пример: А123ВС77");
                valid = false;
            }
            else
            {
                ClearError(tbStateNumber, lblPlateError);
            }

            if (!int.TryParse(tbCapacity.Text.Trim(), out int capacity) || capacity <= 0)
            {
                SetError(tbCapacity, lblCapacityError, "Вместимость должна быть > 0");
                valid = false;
            }
            else
            {
                ClearError(tbCapacity, lblCapacityError);
            }

            if (string.IsNullOrWhiteSpace(tbBrand.Text))
            {
                SetError(tbBrand, lblBrandError, "Введите марку");
                valid = false;
            }
            else
            {
                ClearError(tbBrand, lblBrandError);
            }

            if (string.IsNullOrWhiteSpace(tbModel.Text))
            {
                SetError(tbModel, lblModelError, "Введите модель");
                valid = false;
            }
            else
            {
                ClearError(tbModel, lblModelError);
            }

            return valid;
        }


        void SetError(Control control, Label label, string message)
        {
            control.BackColor = Color.MistyRose;
            label.Text = message;
            label.Visible = true;
        }

        void ClearError(Control control, Label label)
        {
            control.BackColor = Color.White;
            label.Visible = false;
        }
        private void BusForm_Load(object sender, EventArgs e)
        {
            if (_busId.HasValue)
                LoadBusForEdit(_busId.Value);
        }
        private void LoadBusForEdit(int busId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
        SELECT bus.registration_number, bus.mileage, bus.capacity, bus.body_number, bus.chassis_number,
               bus.identification_number, brand.brand_name, model.model_name, bus.status::text, bus.release_date
        FROM bus 
        JOIN brand ON brand.id = bus.brand_id
        JOIN model ON model.id  = bus.model_id
        WHERE bus.id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", busId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Запись автобуса не найдена.");

                        tbStateNumber.Text = r.GetString(0);
                        nudMileage.Value = r.GetInt32(1);
                        tbCapacity.Text = r.GetInt32(2).ToString();
                        tbBodyNumber.Text = r.IsDBNull(3) ? "" : r.GetString(3);
                        tbChassisNumber.Text = r.IsDBNull(4) ? "" : r.GetString(4);
                        tbInventoryNumber.Text = r.IsDBNull(5) ? "" : r.GetString(5);
                        tbBrand.Text = r.GetString(6);
                        tbModel.Text = r.GetString(7);
                        cbCondition.Text = r.GetString(8);
                        dtpReleaseDate.Value = r.GetDateTime(9);
                    }
                }
            }
        }
    }
}
