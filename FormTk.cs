using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormTk : Form
    {
        private readonly string _connString;
        private int? _TkId;
        public FormTk(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _TkId = null;
            this.Load += FormTk_Load;
        }
        public FormTk(string connString, int TkId) : this(connString)
        {
            _TkId = TkId;
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void FormTk_Load(object sender, EventArgs e)
        {
            LoadEmployees();
            LoadSimpleTable("workplace", cmbWorkplace);
            LoadSimpleTable("profession", cmbProfession);
            LoadSimpleTable("specialty", cmbSpecialty);
            LoadSimpleTable("qualification", cmbQualification);
            LoadSimpleTable("post", cmbPost);
            LoadSimpleTable("department", cmbDepartment);
            if (!checkBoxWorking.Checked && !checkBoxFired.Checked)
                checkBoxWorking.Checked = true;
            if (_TkId.HasValue)
                LoadTkForEdit(_TkId.Value);
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

                    cmbEmployee.DataSource = null;
                    cmbEmployee.DisplayMember = "fio";
                    cmbEmployee.ValueMember = "id";
                    cmbEmployee.DataSource = dt;
                }
            }
        }
        private void LoadSimpleTable(string table, ComboBox combo)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(
                    $"SELECT id, name FROM {table} ORDER BY name;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    combo.DataSource = null;
                    combo.DisplayMember = "name";
                    combo.ValueMember = "id";
                    combo.DataSource = dt;
                }
            }
        }
        private void LoadTkForEdit(int shiftId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT employee_id,
                           workplace_id, 
                           profession_id,
                           specialty_id, 
                           qualification_id, 
                           post_id, 
                           department_id, 
                           hire_date, 
                           dismissal_date, 
                           termination_reason, 
                           event_basis, 
                           event_type::text
                    FROM tk
                    WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", shiftId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Трудовая книжка не найдена.");
                        cmbEmployee.SelectedValue = r.GetInt32(0);
                        cmbWorkplace.SelectedValue = r.GetInt32(1);
                        cmbProfession.SelectedValue = r.GetInt32(2);
                        cmbSpecialty.SelectedValue = r.GetInt32(3);
                        cmbQualification.SelectedValue = r.GetInt32(4);
                        cmbPost.SelectedValue = r.GetInt32(5);
                        cmbDepartment.SelectedValue = r.GetInt32(6);

                        dtpHireDate.Value = r.GetDateTime(7);
                        if (!r.IsDBNull(8))
                            dtpDismissalDate.Value = r.GetDateTime(8);
                        txtTerminationReason.Text = r.IsDBNull(9) ? "" : r.GetString(9);
                        txtEventBasis.Text = r.IsDBNull(10) ? "" : r.GetString(10);

                        string eventType = r.IsDBNull(11) ? "Приём" : r.GetString(11);
                        if (eventType == "Увольнение")
                        {
                            checkBoxFired.Checked = true;
                        }
                        else
                        {
                            checkBoxWorking.Checked = true;
                        }
                    }
                }
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbEmployee.SelectedValue == null)
                {
                    MessageBox.Show("Выберите сотрудника.");
                    return;
                }

                if (cmbWorkplace.SelectedValue == null ||
                    cmbProfession.SelectedValue == null ||
                    cmbSpecialty.SelectedValue == null ||
                    cmbQualification.SelectedValue == null ||
                    cmbPost.SelectedValue == null ||
                    cmbDepartment.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все обязательные поля.");
                    return;
                }

                if (!checkBoxWorking.Checked && !checkBoxFired.Checked)
                {
                    MessageBox.Show("Выберите статус сотрудника.");
                    return;
                }

                int employeeId = Convert.ToInt32(cmbEmployee.SelectedValue);
                int workplaceId = Convert.ToInt32(cmbWorkplace.SelectedValue);
                int professionId = Convert.ToInt32(cmbProfession.SelectedValue);
                int specialtyId = Convert.ToInt32(cmbSpecialty.SelectedValue);
                int qualificationId = Convert.ToInt32(cmbQualification.SelectedValue);
                int postId = Convert.ToInt32(cmbPost.SelectedValue);
                int departmentId = Convert.ToInt32(cmbDepartment.SelectedValue);

                DateTime hireDate = dtpHireDate.Value.Date;

                string eventBasis = txtEventBasis.Text.Trim();
                string terminationReason = txtTerminationReason.Text.Trim();

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    string sql;

                    if (_TkId.HasValue)
                    {
                        sql = @"
                            UPDATE tk
                            SET employee_id = @employee_id,
                                workplace_id = @workplace_id,
                                profession_id = @profession_id,
                                specialty_id = @specialty_id,
                                qualification_id = @qualification_id,
                                post_id = @post_id,
                                department_id = @department_id,
                                hire_date = @hire_date,
                                dismissal_date = @dismissal_date,
                                termination_reason = @termination_reason,
                                event_basis = @event_basis,
                                event_type = @event_type
                            WHERE id = @id;";
                    }
                    else
                    {
                        sql = @"
                            INSERT INTO tk
                            (employee_id, workplace_id, profession_id,
                             specialty_id, qualification_id, post_id,
                             department_id, hire_date, dismissal_date,
                             termination_reason, event_basis, event_type)
                            VALUES
                            (@employee_id, @workplace_id, @profession_id,
                             @specialty_id, @qualification_id, @post_id,
                             @department_id, @hire_date, @dismissal_date,
                             @termination_reason, @event_basis, @event_type);";
                    }

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (_TkId.HasValue)
                            cmd.Parameters.AddWithValue("@id", _TkId.Value);

                        cmd.Parameters.AddWithValue("@employee_id", employeeId);
                        cmd.Parameters.AddWithValue("@workplace_id", workplaceId);
                        cmd.Parameters.AddWithValue("@profession_id", professionId);
                        cmd.Parameters.AddWithValue("@specialty_id", specialtyId);
                        cmd.Parameters.AddWithValue("@qualification_id", qualificationId);
                        cmd.Parameters.AddWithValue("@post_id", postId);
                        cmd.Parameters.AddWithValue("@department_id", departmentId);
                        cmd.Parameters.AddWithValue("@hire_date", hireDate);

                        cmd.Parameters.AddWithValue("@event_basis",
                            string.IsNullOrWhiteSpace(eventBasis) ? (object)DBNull.Value : eventBasis);

                        if (checkBoxWorking.Checked)
                        {
                            cmd.Parameters.Add("@event_type", NpgsqlTypes.NpgsqlDbType.Unknown).Value = "Приём";
                            cmd.Parameters.AddWithValue("@dismissal_date", DBNull.Value);
                            cmd.Parameters.AddWithValue("@termination_reason", DBNull.Value);
                        }
                        else
                        {
                            DateTime dismissalDate = dtpDismissalDate.Value.Date;

                            if (dismissalDate < hireDate)
                            {
                                MessageBox.Show("Дата увольнения не может быть раньше даты приема.");
                                return;
                            }

                            cmd.Parameters.Add("@event_type", NpgsqlTypes.NpgsqlDbType.Unknown).Value = "Увольнение";
                            cmd.Parameters.AddWithValue("@dismissal_date", dismissalDate);
                            cmd.Parameters.AddWithValue("@termination_reason",
                                string.IsNullOrWhiteSpace(terminationReason) ? (object)DBNull.Value : terminationReason);
                        }

                        cmd.ExecuteNonQuery();
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
        private void checkBoxWorking_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxWorking.Checked)
            {
                checkBoxFired.Checked = false;

                dtpDismissalDate.Enabled = false;
                txtTerminationReason.Enabled = false;

                dtpDismissalDate.Value = DateTime.Now;
                txtTerminationReason.Clear();
            }
        }

        private void checkBoxFired_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxFired.Checked)
            {
                checkBoxWorking.Checked = false;

                dtpDismissalDate.Enabled = true;
                txtTerminationReason.Enabled = true;
            }
        }
    }
}
