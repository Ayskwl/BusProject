using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormWorker : Form
    {
        private readonly string _connString;
        private readonly int? _workerId;

        public FormWorker(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _workerId = null;
            this.Load += FormWorker_Load;
        }

        public FormWorker(string connString, int workerId) : this(connString)
        {
            _workerId = workerId;
        }

        private void FormWorker_Load(object sender, EventArgs e)
        {
            LoadCitiesDistinct();
            LoadStreets();

            if (_workerId.HasValue)
                LoadWorkerForEdit(_workerId.Value);
        }

        private void LoadCitiesDistinct()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT DISTINCT city_name
                    FROM city
                    WHERE city_name IS NOT NULL AND city_name <> ''
                    ORDER BY city_name;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dtCity = new DataTable();
                    da.Fill(dtCity);

                    cmbCity.DropDownStyle = ComboBoxStyle.DropDown; 
                    cmbCity.DisplayMember = "city_name";
                    cmbCity.ValueMember = "city_name";
                    cmbCity.DataSource = dtCity;
                }
            }
        }

        private void LoadStreets()
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT id, street_name
                    FROM street
                    WHERE street_name IS NOT NULL AND street_name <> ''
                    ORDER BY street_name;", conn))
                using (var da = new NpgsqlDataAdapter(cmd))
                {
                    var dtStreet = new DataTable();
                    da.Fill(dtStreet);

                    cmbStreet.DropDownStyle = ComboBoxStyle.DropDown; 
                    cmbStreet.DisplayMember = "street_name";
                    cmbStreet.ValueMember = "id";
                    cmbStreet.DataSource = dtStreet;
                }
            }
        }

        private void LoadWorkerForEdit(int workerId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT city_id, street_id,
                last_name, first_name, patronymic, gender, birth_date, work_experience, house_number,
                salary, city.city_name
                FROM worker
                JOIN city ON city.id = worker.city_id
                WHERE worker.id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", workerId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new Exception("Сотрудник не найден.");

                        tbLastName.Text = rd["last_name"].ToString();
                        tbFirstName.Text = rd["first_name"].ToString();
                        tbPatronymic.Text = rd["patronymic"].ToString();

                        dtpBirthDate.Value = Convert.ToDateTime(rd["birth_date"]);
                        tbExperience.Text = rd["work_experience"].ToString();
                        tbSalary.Text = rd["salary"].ToString();
                        tbHouseNumber.Text = rd["house_number"].ToString();

                        string gender = rd["gender"].ToString();
                        chkMale.Checked = (gender == "Мужской");
                        chkFemale.Checked = (gender == "Женский");

                        cmbCity.Text = rd["city_name"].ToString();
                        cmbStreet.SelectedValue = Convert.ToInt32(rd["street_id"]);

                        /*rd.Close();

                        using (var cmdCity = new NpgsqlCommand(@"
                            SELECT city_name
                            FROM city
                            WHERE id = @id;", conn))
                        {
                            cmdCity.Parameters.AddWithValue("@id", cityId);
                            object cityNameObj = cmdCity.ExecuteScalar();
                            if (cityNameObj != null)
                                cmbCity.Text = cityNameObj.ToString(); 
                        }

                        cmbStreet.SelectedValue = streetId;*/
                    }
                }
            }
        }

        private void btnSaveBus_Click(object sender, EventArgs e)
        {
            try
            {
                string lastName = tbLastName.Text.Trim();
                string firstName = tbFirstName.Text.Trim();
                string patronymic = tbPatronymic.Text.Trim();

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
                {
                    MessageBox.Show("Введите фамилию и имя.");
                    return;
                }

                string gender = null;
                if (chkFemale.Checked && chkMale.Checked)
                {
                    MessageBox.Show("Выберите только один пол.");
                    return;
                }
                if (chkFemale.Checked) gender = "Женский";
                if (chkMale.Checked) gender = "Мужской";
                if (string.IsNullOrWhiteSpace(gender))
                {
                    MessageBox.Show("Выберите пол.");
                    return;
                }

                DateTime birthDate = dtpBirthDate.Value.Date;

                int experience;
                if (!int.TryParse(tbExperience.Text.Trim(), out experience) || experience < 0)
                {
                    MessageBox.Show("Стаж должен быть целым числом (0 и больше).");
                    return;
                }

                string cityName = cmbCity.Text.Trim();     
                string streetName = cmbStreet.Text.Trim(); 
                if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(streetName))
                {
                    MessageBox.Show("Введите город и улицу.");
                    return;
                }

                string houseNumber = tbHouseNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(houseNumber))
                {
                    MessageBox.Show("Введите номер дома.");
                    return;
                }

                decimal salary;
                if (!decimal.TryParse(tbSalary.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out salary) || salary < 0)
                {
                    MessageBox.Show("Зарплата должна быть числом (0 и больше).");
                    return;
                }

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    int cityId;
                    using (var cmdCitySel = new NpgsqlCommand(
                        "SELECT id FROM city WHERE city_name = @name LIMIT 1;", conn))
                    {
                        cmdCitySel.Parameters.AddWithValue("@name", cityName);
                        object v = cmdCitySel.ExecuteScalar();

                        if (v != null && v != DBNull.Value)
                            cityId = Convert.ToInt32(v);
                        else
                        {
                            using (var cmdCityIns = new NpgsqlCommand(
                                "INSERT INTO city (city_name) VALUES (@name) RETURNING id;", conn))
                            {
                                cmdCityIns.Parameters.AddWithValue("@name", cityName);
                                cityId = Convert.ToInt32(cmdCityIns.ExecuteScalar()); // RETURNING -> scalar [web:1873]
                            }
                        }
                    }

                    int streetId;
                    using (var cmdStreetSel = new NpgsqlCommand(
                        "SELECT id FROM street WHERE street_name = @name LIMIT 1;", conn))
                    {
                        cmdStreetSel.Parameters.AddWithValue("@name", streetName);
                        object v = cmdStreetSel.ExecuteScalar();

                        if (v != null && v != DBNull.Value)
                            streetId = Convert.ToInt32(v);
                        else
                        {
                            using (var cmdStreetIns = new NpgsqlCommand(
                                "INSERT INTO street (street_name) VALUES (@name) RETURNING id;", conn))
                            {
                                cmdStreetIns.Parameters.AddWithValue("@name", streetName);
                                streetId = Convert.ToInt32(cmdStreetIns.ExecuteScalar()); // RETURNING -> scalar [web:1873]
                            }
                        }
                    }

                    if (_workerId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            UPDATE worker
                            SET last_name = @last_name,
                                first_name = @first_name,
                                patronymic = @patronymic,
                                gender = @gender::employee_enum,
                                birth_date = @birth_date,
                                work_experience = @work_experience,
                                city_id = @city_id,
                                street_id = @street_id,
                                house_number = @house_number,
                                salary = @salary
                            WHERE id = @id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _workerId.Value);

                            cmd.Parameters.AddWithValue("@last_name", lastName);
                            cmd.Parameters.AddWithValue("@first_name", firstName);
                            cmd.Parameters.AddWithValue("@patronymic", patronymic);
                            cmd.Parameters.AddWithValue("@gender", gender);

                            cmd.Parameters.Add("@birth_date", NpgsqlDbType.Date).Value = birthDate;
                            cmd.Parameters.AddWithValue("@work_experience", experience);

                            cmd.Parameters.AddWithValue("@city_id", cityId);
                            cmd.Parameters.AddWithValue("@street_id", streetId);

                            cmd.Parameters.AddWithValue("@house_number", houseNumber);
                            cmd.Parameters.AddWithValue("@salary", salary);

                            int affected = cmd.ExecuteNonQuery();
                            if (affected == 0)
                                throw new Exception("Сотрудник для изменения не найден.");
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO worker
                                (last_name, first_name, patronymic, gender, birth_date,
                                 work_experience, city_id, street_id, house_number, salary)
                            VALUES
                                (@last_name, @first_name, @patronymic, @gender::employee_enum, @birth_date,
                                 @work_experience, @city_id, @street_id, @house_number, @salary);", conn))
                        {
                            cmd.Parameters.AddWithValue("@last_name", lastName);
                            cmd.Parameters.AddWithValue("@first_name", firstName);
                            cmd.Parameters.AddWithValue("@patronymic", patronymic);
                            cmd.Parameters.AddWithValue("@gender", gender);

                            cmd.Parameters.Add("@birth_date", NpgsqlDbType.Date).Value = birthDate;
                            cmd.Parameters.AddWithValue("@work_experience", experience);

                            cmd.Parameters.AddWithValue("@city_id", cityId);
                            cmd.Parameters.AddWithValue("@street_id", streetId);

                            cmd.Parameters.AddWithValue("@house_number", houseNumber);
                            cmd.Parameters.AddWithValue("@salary", salary);

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

        private void btnCancelBus_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
