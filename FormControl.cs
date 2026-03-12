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
    public partial class FormControl : Form
    {
        private readonly string _connString;
        private readonly int? _controlId;

        public FormControl(string connString)
        {
            InitializeComponent();
            _connString = connString;
            _controlId = null;
            this.Load += FormControl_Load;
        }
        public FormControl(string connString, int controlId) : this(connString)
        {
            _controlId = controlId;
        }
        private void FormControl_Load(object sender, EventArgs e)
        {
            LoadIncidentsToCombo();
            if (_controlId.HasValue)
                LoadControlForEdit(_controlId.Value);
        }
        private void LoadIncidentsToCombo()
        {
            cmbIncident.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbIncident.Items.Clear();

            cmbIncident.Items.Add("Поломка двигателя");
            cmbIncident.Items.Add("Перегрев");
            cmbIncident.Items.Add("Неисправность тормозов");
            cmbIncident.Items.Add("Проблемы с дверями");
            cmbIncident.Items.Add("Электрическая неисправность");
            cmbIncident.Items.Add("Прокол колеса");
            cmbIncident.Items.Add("Отказ АКПП");
            cmbIncident.Items.Add("Дым из моторного отсека");
            cmbIncident.Items.Add("Проблемы с рулевым");
            cmbIncident.Items.Add("Неисправность печки");

            if (cmbIncident.Items.Count > 0)
                cmbIncident.SelectedIndex = 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {

                string incident = cmbIncident.Text.Trim();
                string StopStation = tbStopStation.Text.Trim();
                DateTime аrrivedTime = dtpFirstDeparture.Value;

                if (string.IsNullOrWhiteSpace(incident) ||
                    string.IsNullOrWhiteSpace(StopStation)) 

                {
                    MessageBox.Show("Заполните: происшествие, остановка, количество пассажиров");
                    return;
                }
                
                int passengerCount = (int)nudPassenger.Value;

                using (var conn = new NpgsqlConnection(_connString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            int stopId = GetOrCreateStopStationId(conn, tx, StopStation);
                            int incidentId = CreateIncidentAndReturnId(conn, tx, incident);

                            if (_controlId.HasValue)
                            {
                                using (var cmd = new NpgsqlCommand(@"
                                    UPDATE control
                                    SET stop_id = @stop_id,
                                        incident_id = @incident_id,
                                        arrival_time = @arrival_time,
                                        eater_count = @eater_count
                                    WHERE id = @id;", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", _controlId.Value);
                                    cmd.Parameters.AddWithValue("@stop_id", stopId);
                                    cmd.Parameters.AddWithValue("@incident_id", incidentId);
                                    /*cmd.Parameters.AddWithValue("@arrival_time", arrivedTime);*/
                                    cmd.Parameters.AddWithValue("@eater_count", passengerCount);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = new NpgsqlCommand(@"
                                    INSERT INTO control (stop_id, incident_id, arrival_time, eater_count)
                                    VALUES (@stop_id, @incident_id, @arrival_time, @eater_count);", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@stop_id", stopId);
                                    cmd.Parameters.AddWithValue("@incident_id", incidentId);
                                   /* cmd.Parameters.AddWithValue("@arrival_time", arrivedTime);*/
                                    cmd.Parameters.AddWithValue("@eater_count", passengerCount);
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

        private int CreateIncidentAndReturnId(NpgsqlConnection conn, NpgsqlTransaction tx, string reason)
        {
            reason = (reason ?? "").Trim();
            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Причина происшествия не заполнена.");

            using (var cmd = new NpgsqlCommand(@"
        INSERT INTO incident (removal_datetime, reason)
        VALUES (@removal_datetime, @reason)
        RETURNING id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@removal_datetime", DateTime.Now);
                cmd.Parameters.AddWithValue("@reason", reason);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private int GetOrCreateStopStationId(NpgsqlConnection conn, NpgsqlTransaction tx, string stopName)
        {
            stopName = (stopName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(stopName))
                throw new Exception("Остановка не заполнена.");

            using (var cmd = new NpgsqlCommand(@"
                SELECT id
                FROM stop
                WHERE name = @name
                LIMIT 1;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@name", stopName);
                var res = cmd.ExecuteScalar();
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
        private void LoadControlForEdit(int revenueId)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT arrival_time, eater_count,
                           stop.name AS stop_name,
                           incident.reason AS incident_reason
                    FROM control 
                    JOIN stop ON id = control.stop_id
                    JOIN incident ON id = control.incident_id
                    WHERE id = @id;", conn))
                {
                    cmd.Parameters.AddWithValue("@id", revenueId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            throw new Exception("Запись выручки не найдена.");

                        tbStopStation.Text = rd["stop_name"].ToString();
                        nudPassenger.Value = Convert.ToDecimal(rd["eater_count"]);

                        var ts = (TimeSpan)rd["arrival_time"];
                        dtpFirstDeparture.Text = ts.ToString(@"hh\:mm\:ss");

                        string reason = rd["incident_reason"].ToString();
                        int idx = cmbIncident.FindStringExact(reason);
                        if (idx >= 0) cmbIncident.SelectedIndex = idx;
                        else cmbIncident.Text = reason;
                    }
                }
            }
        }
    }
}
