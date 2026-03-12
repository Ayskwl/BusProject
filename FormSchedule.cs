using Npgsql;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormSchedule : Form
    {
        private readonly string _connString;
        private int _routeId;
        private readonly int? _scheduleId;
        public FormSchedule(string connString)
        {
            InitializeComponent();

            _connString = connString;
            _scheduleId = null;

            this.Load += FormSchedule_Load;
        }

        private void FormSchedule_Load(object sender, EventArgs e)
        {
            if (_scheduleId.HasValue)
                LoadScheduleForEdit(_scheduleId.Value);
        }
        public FormSchedule(string connString, int scheduleId)
        {
            InitializeComponent();
            _connString = connString;
            _scheduleId = scheduleId;
            this.Load += FormSchedule_Load;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {

                DateTime firstDeparture = dtpFirstDeparture.Value;
                DateTime lastDeparture = dtpLastDeparture.Value;
                DateTime firstDispatch = dtpFirstDispatch.Value;
                DateTime nthDispatch = dtpNthDispatch.Value;

                if (!mtbMoveInterval.MaskCompleted)
                {
                    MessageBox.Show("Введите интервал движения полностью (например 00:20:00).");
                    return;
                }

                TimeSpan movementInterval;
                if (!TimeSpan.TryParseExact(mtbMoveInterval.Text.Trim(), @"hh\:mm\:ss",
                        CultureInfo.InvariantCulture, out movementInterval))
                {
                    MessageBox.Show("Неверный формат интервала. Пример: 00:20:00");
                    return;
                }

                if (movementInterval <= TimeSpan.Zero)
                {
                    MessageBox.Show("Интервал движения должен быть больше 0.");
                    return;
                }
                if (lastDeparture <= firstDeparture)
                {
                    MessageBox.Show("Последний выезд должен быть позже первого.");
                    return;
                }

                firstDispatch = NormalizeToRouteDay(firstDeparture, firstDispatch);
                nthDispatch = NormalizeToRouteDay(firstDeparture, nthDispatch);

                if (firstDispatch < firstDeparture)
                {
                    MessageBox.Show("Отправление с конечной не может быть раньше первого выезда.");
                    return;
                }

                if (nthDispatch < firstDispatch)
                {
                    MessageBox.Show("Прибытие в парк не может быть раньше отправления с конечной.");
                    return;
                }

                if (firstDeparture + movementInterval > lastDeparture)
                {
                    MessageBox.Show("Интервал слишком большой: следующий выезд после первого уже позже последнего.");
                    return;
                }
                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();

                    if (_scheduleId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(@"
                    UPDATE schedule
                    SET route_id = @route_id,
                        first_departure = @first_departure,
                        last_departure = @last_departure,
                        first_dispatch = @first_dispatch,
                        nth_dispatch = @nth_dispatch,
                        movement_interval = @movement_interval
                WHERE id = @id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _scheduleId.Value);
                            AddScheduleParams(cmd, firstDeparture, lastDeparture, firstDispatch, nthDispatch, movementInterval);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO schedule
                        (route_id, first_departure, last_departure, first_dispatch, nth_dispatch, movement_interval)
                    VALUES
                        (@route_id, @first_departure, @last_departure, @first_dispatch, @nth_dispatch, @movement_interval);", conn))
                        {
                            AddScheduleParams(cmd, firstDeparture, lastDeparture, firstDispatch, nthDispatch, movementInterval);
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
        private void AddScheduleParams(NpgsqlCommand cmd,
            DateTime firstDeparture, DateTime lastDeparture, DateTime firstDispatch, DateTime nthDispatch, TimeSpan movementInterval)
        {
            cmd.Parameters.AddWithValue("@route_id", _routeId);
            cmd.Parameters.AddWithValue("@first_departure", firstDeparture);
            cmd.Parameters.AddWithValue("@last_departure", lastDeparture);
            cmd.Parameters.AddWithValue("@first_dispatch", firstDispatch);
            cmd.Parameters.AddWithValue("@nth_dispatch", nthDispatch);
            cmd.Parameters.AddWithValue("@movement_interval", movementInterval);
        }
        private static DateTime NormalizeToRouteDay(DateTime routeStart, DateTime dt)
        {
            if (dt < routeStart)
                return dt.AddDays(1); 
            return dt;
        }


        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
        private void LoadScheduleForEdit(int scheduleId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(@"
            SELECT route_id, first_departure, last_departure, first_dispatch, nth_dispatch, movement_interval
            FROM schedule
            WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", scheduleId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Расписание не найдено.");

                        DateTime firstDeparture = Convert.ToDateTime(r.GetValue(1));
                        DateTime lastDeparture = Convert.ToDateTime(r.GetValue(2));
                        DateTime firstDispatch = Convert.ToDateTime(r.GetValue(3));
                        DateTime nth_dispatch = Convert.ToDateTime(r.GetValue(4));
                        TimeSpan interval = (TimeSpan)r.GetValue(5);

                        dtpFirstDeparture.Value = firstDeparture;
                        dtpLastDeparture.Value = lastDeparture;
                        dtpFirstDispatch.Value = firstDispatch;
                        dtpNthDispatch.Value = nth_dispatch;

                        mtbMoveInterval.Text = interval.ToString(@"hh\:mm\:ss");
                    }
                }
            }
        }
    }
}
