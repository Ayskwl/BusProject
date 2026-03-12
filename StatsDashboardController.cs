using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Bus_coursework
{
    public sealed class StatsDashboardController
    {
        private readonly string _connString;

        private readonly ComboBox _modeCombo;
        private readonly Chart _bar;
        private readonly Chart _pie;
        private readonly Chart _line;

        private readonly Label _kpiTotal;
        private readonly Label _kpiAvg;
        private readonly Label _kpiMax;
        private readonly Label _kpiTop;

        private readonly List<DashboardMode> _modes = new List<DashboardMode>();

        public StatsDashboardController(
            string connString,
            ComboBox modeCombo,
            Chart barChart,
            Chart pieChart,
            Chart lineChart,
            Label kpiTotal,
            Label kpiAvg,
            Label kpiMax,
            Label kpiTop)
        {
            _connString = connString ?? throw new ArgumentNullException(nameof(connString));

            _modeCombo = modeCombo ?? throw new ArgumentNullException(nameof(modeCombo));
            _bar = barChart ?? throw new ArgumentNullException(nameof(barChart));
            _pie = pieChart ?? throw new ArgumentNullException(nameof(pieChart));
            _line = lineChart ?? throw new ArgumentNullException(nameof(lineChart));

            _kpiTotal = kpiTotal ?? throw new ArgumentNullException(nameof(kpiTotal));
            _kpiAvg = kpiAvg ?? throw new ArgumentNullException(nameof(kpiAvg));
            _kpiMax = kpiMax ?? throw new ArgumentNullException(nameof(kpiMax));
            _kpiTop = kpiTop ?? throw new ArgumentNullException(nameof(kpiTop));
        }

        
        public void Init()
        {
            _modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeCombo.DisplayMember = "Title";
            _modeCombo.ValueMember = "Key";

            _modes.Clear();

            _modes.Add(new DashboardMode(
                key: "shift",
                title: "Смены",
                barTitle: "Распределение по водителям (ТОП)",
                pieTitle: "Структура распределения по типам смен",
                lineTitle: "Динамика по дням (количество смен)",
                kpiTitle: "Ключевые показатели",
                
                barSql: @"
                  SELECT
                  w.last_name || ' ' || w.first_name AS label,
                  COUNT(*)::int AS value
                FROM shift s
                JOIN driver d ON d.id = s.driver_id
                JOIN worker w ON w.id = d.employee_id
                WHERE s.start_time >= @from AND s.start_time < (@to + INTERVAL '1 day')
                GROUP BY w.last_name, w.first_name
                ORDER BY value DESC
                LIMIT 10;",
               
                pieSql: @"
                    SELECT
                        s.shift_type::text AS label,
                        COUNT(*)::int AS value
                    FROM shift s
                    WHERE s.start_time >= @from AND s.start_time < (@to + INTERVAL '1 day')
                    GROUP BY s.shift_type
                    ORDER BY value DESC;",
                
                lineSql: @"
                    SELECT
                        date_trunc('day', s.start_time)::date AS x,
                        COUNT(*)::int AS y
                    FROM shift s
                    WHERE s.start_time >= @from AND s.start_time < (@to + INTERVAL '1 day')
                    GROUP BY x
                    ORDER BY x;",
                
                kpiSql: @"
                  WITH f AS (
                  SELECT s.*
                  FROM shift s
                  WHERE s.start_time >= @from AND s.start_time < (@to + INTERVAL '1 day')
                ),
                by_day AS (
                  SELECT date_trunc('day', start_time)::date AS d, COUNT(*)::int AS cnt
                  FROM f
                  GROUP BY d
                ),
                top_driver AS (
                  SELECT w.last_name || ' ' || w.first_name AS name, COUNT(*)::int AS cnt
                  FROM f
                  JOIN driver d ON d.id = f.driver_id
                  JOIN worker w ON w.id = d.employee_id
                  GROUP BY w.last_name, w.first_name
                  ORDER BY cnt DESC
                  LIMIT 1
                )
                SELECT
                  COALESCE((SELECT COUNT(*) FROM f), 0)::int AS total,
                  COALESCE((SELECT AVG(cnt) FROM by_day), 0)::numeric(12,2) AS avg,
                  COALESCE((SELECT MAX(cnt) FROM by_day), 0)::int AS max,
                  COALESCE((SELECT name FROM top_driver), '—')::text AS top;"
            ));

            _modes.Add(new DashboardMode(
                key: "schedule",
                title: "Расписания",
                barTitle: "Распределение по маршрутам (ТОП)",
                pieTitle: "Структура распределения (по маршрутам)",
                lineTitle: "Динамика по дням (количество расписаний)",
                kpiTitle: "Ключевые показатели",
                barSql: @"
                    SELECT
                        ('Маршрут #' || s.route_id::text) AS label,
                        COUNT(*)::int AS value
                    FROM schedule s
                    WHERE s.first_departure >= @from AND s.first_departure < (@to + INTERVAL '1 day')
                    GROUP BY s.route_id
                    ORDER BY value DESC
                    LIMIT 10;",
                pieSql: @"
                    SELECT
                        ('Маршрут #' || s.route_id::text) AS label,
                        COUNT(*)::int AS value
                    FROM schedule s
                     WHERE s.first_departure >= @from AND s.first_departure < (@to + INTERVAL '1 day')
                    GROUP BY s.route_id
                    ORDER BY value DESC
                    LIMIT 8;",
                lineSql: @"
                    SELECT
                        date_trunc('day', s.first_departure)::date AS x,
                        COUNT(*)::int AS y
                    FROM schedule s
                    WHERE s.first_departure >= @from AND s.first_departure < (@to + INTERVAL '1 day')
                    GROUP BY x
                    ORDER BY x;",
                kpiSql: @"
                   WITH f AS (
                  SELECT s.*
                  FROM schedule s
                  WHERE s.first_departure >= @from AND s.first_departure < (@to + INTERVAL '1 day')
                ),
                by_day AS (
                  SELECT date_trunc('day', first_departure)::date AS d, COUNT(*)::int AS cnt
                  FROM f
                  GROUP BY d
                ),
                top_route AS (
                  SELECT ('Маршрут #' || route_id::text) AS name, COUNT(*)::int AS cnt
                  FROM f
                  GROUP BY route_id
                  ORDER BY cnt DESC
                  LIMIT 1
                )
                SELECT
                  COALESCE((SELECT COUNT(*) FROM f), 0)::int AS total,
                  COALESCE((SELECT AVG(cnt) FROM by_day), 0)::numeric(12,2) AS avg,
                  COALESCE((SELECT MAX(cnt) FROM by_day), 0)::int AS max,
                  COALESCE((SELECT name FROM top_route), '—')::text AS top;"
            ));
                    _modes.Add(new DashboardMode(
                        key: "revenue",
                        title: "Выручка",
                        barTitle: "Топ водителей по выручке",
                        pieTitle: "Доля выручки по дням",
                        lineTitle: "Динамика выручки по дням",
                        kpiTitle: "Ключевые показатели",
                        barSql: @"
                SELECT
                    w.last_name || ' ' || w.first_name AS label,
                    SUM(r.amount)::numeric(12,2) AS value
                FROM revenue r
                JOIN worker w ON w.id = r.driver_id
                WHERE r.period_start >= @from 
                  AND r.period_start < (@to + INTERVAL '1 day')
                GROUP BY w.last_name, w.first_name
                ORDER BY value DESC
                LIMIT 10;",

            pieSql: @"
                SELECT
                    w.last_name || ' ' || w.first_name AS label,
                    SUM(r.amount)::numeric(12,2) AS value
                FROM revenue r
                JOIN worker w ON w.id = r.driver_id
                WHERE r.period_start >= @from 
                  AND r.period_start < (@to + INTERVAL '1 day')
                GROUP BY w.last_name, w.first_name
                ORDER BY value DESC
                LIMIT 8;",

            lineSql: @"
                SELECT
                    date_trunc('day', r.period_start)::date AS x,
                    SUM(r.amount)::numeric(12,2) AS y
                FROM revenue r
                WHERE r.period_start >= @from 
                  AND r.period_start < (@to + INTERVAL '1 day')
                GROUP BY x
                ORDER BY x;",

            kpiSql: @"
               WITH f AS (
              SELECT r.*
              FROM revenue r
              WHERE r.period_start >= @from AND r.period_start < (@to + INTERVAL '1 day')
            ),
            by_day AS (
              SELECT date_trunc('day', period_start)::date AS d, COALESCE(SUM(amount),0)::numeric(12,2) AS sum_day
              FROM f
              GROUP BY d
            ),
            top_driver AS (
              SELECT w.last_name || ' ' || w.first_name AS name,
                     COALESCE(SUM(r.amount),0)::numeric(12,2) AS s
              FROM f r
              JOIN worker w ON w.id = r.driver_id
              GROUP BY w.last_name, w.first_name
              ORDER BY s DESC
              LIMIT 1
            )
            SELECT
              COALESCE((SELECT SUM(amount) FROM f),0)::numeric(12,2) AS total,
              COALESCE((SELECT AVG(sum_day) FROM by_day),0)::numeric(12,2) AS avg,
              COALESCE((SELECT MAX(sum_day) FROM by_day),0)::numeric(12,2) AS max,
              COALESCE((SELECT name FROM top_driver), '—')::text AS top;"));
            _modes.Add(new DashboardMode(
                key: "control",
                title: "Пассажиры/контроль",
                barTitle: "Распределение по остановкам (ТОП)",
                pieTitle: "Структура распределения (по остановкам)",
                lineTitle: "Динамика по дням (пассажиры)",
                kpiTitle: "Ключевые показатели",
                barSql: @"
                    SELECT
                      st.name AS label,
                      SUM(c.eater_count)::int AS value
                    FROM control c
                    JOIN stop st ON st.id = c.stop_id
                    WHERE c.arrival_time >= @from AND c.arrival_time < (@to + INTERVAL '1 day')
                    GROUP BY st.name
                    ORDER BY value DESC
                    LIMIT 10;",
                pieSql: @"
                    SELECT
                  st.name AS label,
                  SUM(c.eater_count)::int AS value
                FROM control c
                JOIN stop st ON st.id = c.stop_id
                WHERE c.arrival_time >= @from AND c.arrival_time < (@to + INTERVAL '1 day')
                GROUP BY st.name
                ORDER BY value DESC
                LIMIT 8;",
                lineSql: @"
                    SELECT
                  date_trunc('day', c.arrival_time)::date AS x,
                  SUM(c.eater_count)::int AS y
                FROM control c
                WHERE c.arrival_time >= @from AND c.arrival_time < (@to + INTERVAL '1 day')
                GROUP BY x
                ORDER BY x;",
                kpiSql: @"
                WITH f AS (
                  SELECT c.*
                  FROM control c
                  WHERE c.arrival_time >= @from AND c.arrival_time < (@to + INTERVAL '1 day')
                ),
                by_day AS (
                  SELECT date_trunc('day', arrival_time)::date AS d, COALESCE(SUM(eater_count),0)::int AS sum_day
                  FROM f
                  GROUP BY d
                ),
                top_stop AS (
                  SELECT st.name AS name, COALESCE(SUM(c.eater_count),0)::int AS s
                  FROM f c
                  JOIN stop st ON st.id = c.stop_id
                  GROUP BY st.name
                  ORDER BY s DESC
                  LIMIT 1
                )
                SELECT
                  COALESCE((SELECT SUM(eater_count) FROM f),0)::int AS total,
                  COALESCE((SELECT AVG(sum_day) FROM by_day),0)::numeric(12,2) AS avg,
                  COALESCE((SELECT MAX(sum_day) FROM by_day),0)::int AS max,
                  COALESCE((SELECT name FROM top_stop), '—')::text AS top;"
            ));

            _modeCombo.DataSource = null;
            _modeCombo.DataSource = _modes;

            if (_modes.Count > 0)
                _modeCombo.SelectedIndex = 0;

            SetupBarChartStyle(_bar);
            SetupPieChartStyle(_pie);
            SetupLineChartStyle(_line);
        }

        public void Refresh(DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var mode = GetSelectedMode();
            if (mode == null) return;

            DataTable dtBar = Query(mode.BarSql, dateFrom, dateTo);
            DataTable dtPie = Query(mode.PieSql, dateFrom, dateTo);
            DataTable dtLine = Query(mode.LineSql, dateFrom, dateTo);
            DataTable dtKpi = Query(mode.KpiSql, dateFrom, dateTo);

            BuildBar(_bar, dtBar, "label", "value");
            BuildPie(_pie, dtPie, "label", "value");
            BuildLine(_line, dtLine, "x", "y");

            ApplyKpi(dtKpi);
        }

        private DashboardMode GetSelectedMode()
        {
            return _modeCombo.SelectedItem as DashboardMode;
        }

        private DataTable Query(string sql, DateTime? dateFrom, DateTime? dateTo)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (dateFrom.HasValue && dateTo.HasValue)
                    {
                        cmd.Parameters.AddWithValue("from", dateFrom.Value.Date);
                        cmd.Parameters.AddWithValue("to", dateTo.Value.Date);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("from", DateTime.Today.AddYears(-50));
                        cmd.Parameters.AddWithValue("to", DateTime.Today.AddYears(50));
                    }
                    using (var da = new NpgsqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        private static void SetupBarChartStyle(Chart chart)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Titles.Clear();
            chart.Legends.Clear();
            chart.BackColor = Color.White;

            var area = new ChartArea("Main");
            area.BackColor = Color.White;

            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;

            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Angle = -30;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 9);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9);

            chart.ChartAreas.Add(area);

            var s = new Series("Series1")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 9),
                XValueType = ChartValueType.String
            };
            chart.Series.Add(s);
        }

        private static void SetupPieChartStyle(Chart chart)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Titles.Clear();
            chart.Legends.Clear();

            var area = new ChartArea("Main");
            chart.ChartAreas.Add(area);

            var s = new Series("Series1")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true
            };

            s.Label = "#PERCENT{P0}";
            s.LegendText = "#VALX";
            s.ToolTip = "#VALX: #VAL";

            chart.Series.Add(s);

            var legend = new Legend("Legend")
            {
                Docking = Docking.Right,
            };
            chart.Legends.Add(legend);
        }

        private static void SetupLineChartStyle(Chart chart)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Titles.Clear();
            chart.Legends.Clear();

            var area = new ChartArea("Main");

            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;

            area.AxisX.LabelStyle.Format = "dd.MM";

            chart.ChartAreas.Add(area);

            var s = new Series("Series1")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.DateTime,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6
            };
            s.ToolTip = "#VALX{dd.MM.yyyy}: #VAL";
            chart.Series.Add(s);
        }


        private static void BuildBar(Chart chart, DataTable dt, string xCol, string yCol)
        {
            var s = chart.Series[0];
            s.Points.Clear();

            foreach (DataRow r in dt.Rows)
            {
                string label = Convert.ToString(r[xCol]);
                double val = Convert.ToDouble(r[yCol]);
                s.Points.AddXY(label, val);
            }
        }

        private static void BuildPie(Chart chart, DataTable dt, string labelCol, string valueCol)
        {
            var s = chart.Series[0];
            s.Points.Clear();

            foreach (DataRow r in dt.Rows)
            {
                string label = Convert.ToString(r[labelCol]);
                double val = Convert.ToDouble(r[valueCol]);
                s.Points.AddXY(label, val);
            }
        }

        private static void BuildLine(Chart chart, DataTable dt, string xCol, string yCol)
        {
            var s = chart.Series[0];
            s.Points.Clear();

            foreach (DataRow r in dt.Rows)
            {
                DateTime x = Convert.ToDateTime(r[xCol]);
                double y = Convert.ToDouble(r[yCol]);
                s.Points.AddXY(x, y);
            }
        }


        private void ApplyKpi(DataTable dtKpi)
        {
            if (dtKpi.Rows.Count == 0)
            {
                _kpiTotal.Text = "0";
                _kpiAvg.Text = "0";
                _kpiMax.Text = "0";
                _kpiTop.Text = "—";
                return;
            }

            var r = dtKpi.Rows[0];
            _kpiTotal.Text = Convert.ToString(r["total"]);
            _kpiAvg.Text = Convert.ToDouble(r["avg"]).ToString("0.##");
            _kpiMax.Text = Convert.ToString(r["max"]);
            _kpiTop.Text = Convert.ToString(r["top"]);
        }


        private sealed class DashboardMode
        {
            public string Key { get; }
            public string Title { get; }
            public string BarTitle { get; }
            public string PieTitle { get; }
            public string LineTitle { get; }
            public string KpiTitle { get; }

            public string BarSql { get; }
            public string PieSql { get; }
            public string LineSql { get; }
            public string KpiSql { get; }

            public DashboardMode(
                string key,
                string title,
                string barTitle,
                string pieTitle,
                string lineTitle,
                string kpiTitle,
                string barSql,
                string pieSql,
                string lineSql,
                string kpiSql)
            {
                Key = key;
                Title = title;
                BarTitle = barTitle;
                PieTitle = pieTitle;
                LineTitle = lineTitle;
                KpiTitle = kpiTitle;

                BarSql = barSql;
                PieSql = pieSql;
                LineSql = lineSql;
                KpiSql = kpiSql;
            }
        }
    }
}