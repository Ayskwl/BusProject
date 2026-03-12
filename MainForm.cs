using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing.Printing;
using System.Drawing;

namespace Bus_coursework
{
    public partial class MainForm : Form
    {
        private readonly string _role;
        private readonly string _login;
        private readonly string _connString;
        private string _currentTable;   
        private string _currentColumn;  
        private string _currentTitle;
        private string _currentEntity;
        private bool _isDictionaryMode = false;
        private DictionaryRepository _dictRepo;
        private SqlTemplateRepository _sqlRepo;
        private CrudButtonsManager _crudManager;
        private PasswordService _passwordService;
        private bool _passwordVisible = false;
        private SearchManager _searchManager;
        private SearchManager _searchManagerSql;
        private DeleteManager _deleteManager;
        private StatsDashboardController _statsController;
        private DataTable _dictTable;
        private NpgsqlDataAdapter _dictAdapter;
        private NpgsqlConnection _dictConnection;
        private bool _dictSuppressAutoSave = false;
        private bool _dictLoading = false;
        private bool _dictSaving = false;
        private int _dictNewRowIndex = -1;
        public MainForm(string role, string login, string connString)
        {
            InitializeComponent();
            _role = role;
            _login = login;
            _connString = connString;
           
            dgvMain.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvMain.MultiSelect = false;
            panelPassword.Visible = false;
            panelPersonal.Visible = false;

            _dictRepo = new DictionaryRepository(_connString);
            _sqlRepo = new SqlTemplateRepository(_connString);
            _crudManager = new CrudButtonsManager(btnAdd, btnEdit, btnDelete);
            _searchManager = new SearchManager(dgvMain, textBoxSearch);
            _searchManagerSql = new SearchManager(dgvSqlResult, textBoxSearch);
            _crudManager.ApplyPermissions(_role);
            _deleteManager = new DeleteManager(dgvMain, _connString);

            var builder = new NpgsqlConnectionStringBuilder(connString)
            {
                Username = CurrentUserSession.Login,
                Password = CurrentUserSession.Password
            };
            textBoxNewPassword.UseSystemPasswordChar = true;
            textBoxPasswordNewNew.UseSystemPasswordChar = true;
            _passwordService = new PasswordService(builder.ConnectionString);
            PasswordService.CueBanner.Set(txtBoxLogin1, "Введите логин", showWhenFocused: false);
            PasswordService.CueBanner.Set(textBoxNewPassword, "Введите новый пароль", showWhenFocused: false);
            PasswordService.CueBanner.Set(textBoxPasswordNewNew, "Подтвердите новый пароль", showWhenFocused: false);
      
            _statsController = new StatsDashboardController(
                _connString,
                cmbStatsMode,
                chartBar,
                chartPie,
                chartLine,
                lblKpiTotal,
                lblKpiAvg,
                lblKpiMax,
                lblKpiTop);

            _statsController.Init();

            cmbStatsMode.SelectedIndexChanged += (s, e) =>
            {
                if (panelContent.Visible)
                    _statsController.Refresh();
            };

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = $"Система управления автобусным парком";
            toolStripStatusLabel1.Text = $"Пользователь: {GetRoleDisplayName(_role)}";
            ApplyRoleRights();

        }
        #region ==================== УПРАВЛЕНИЕ ПРАВАМИ ПО РОЛЯМ ====================

        private void ApplyRoleRights()
        {
            HideAllMenuItems();

            switch (_role)
            {
                case "director":
                    ShowAllMenuItems();
                    break;

                case "dispatcher":

                    toolStripMenuItemRoute.Visible = true;
                    toolStripMenuItemTrip.Visible = true;
                    ToolStripMenuItemDocuments.Visible = true;
                    ToolStripMenuItemRefs.Visible = false;
                    ToolStripMenuItemHelp.Visible = true;
                    ToolStripMenuItemProfile.Visible = true;
                    break;

                case "engineer":
                    ToolStripMenuItemBus.Visible = true;
                    ToolStripMenuItemHelp.Visible = true;
                    ToolStripMenuItemRefs.Visible = false;
                    ToolStripMenuItemProfile.Visible = true;
                    break;

                case "hr_manager":
                    toolStripMenuItemWorker.Visible = true;
                    ToolStripMenuItemDocuments.Visible = true;
                    ToolStripMenuItemRefs.Visible = false;
                    ToolStripMenuItemHelp.Visible = true;
                    ToolStripMenuItemProfile.Visible = true;
                    break;

                default:
                    ToolStripMenuItemBus.Visible = true;
                    toolStripMenuItemTk.Visible = true;
                    toolStripMenuItemWorker.Visible = true;
                    toolStripMenuItemControl.Visible = true;
                    toolStripMenuItemSchedule.Visible = true;
                    toolStripMenuItemRevenue.Visible = true;
                    toolStripMenuItemShift.Visible = true;
                    toolStripMenuItemDriver.Visible = true;
                    toolStripMenuItemTrip.Visible = true;
                    toolStripMenuItemRoute.Visible = true;
                    ToolStripMenuItemDocuments.Visible = false;
                    ToolStripMenuItemRefs.Visible = true;
                    ToolStripMenuItemHelp.Visible = true;
                    break;
            }
        }

        private void HideAllMenuItems()
        {
            ToolStripMenuItemBus.Visible = false;
            toolStripMenuItemRoute.Visible = false;
            toolStripMenuItemTrip.Visible = false;
            toolStripMenuItemSchedule.Visible = false;
            toolStripMenuItemControl.Visible = false;
            toolStripMenuItemRevenue.Visible = false;
            toolStripMenuItemWorker.Visible = false;
            toolStripMenuItemShift.Visible = false;
            toolStripMenuItemTk.Visible = false;
            toolStripMenuItemDriver.Visible = false;
        }

        private void ShowAllMenuItems()
        {
            ToolStripMenuItemBus.Visible = true;
            toolStripMenuItemRoute.Visible = true;
            toolStripMenuItemTrip.Visible = true;
            toolStripMenuItemSchedule.Visible = true;
            toolStripMenuItemControl.Visible = true;
            toolStripMenuItemRevenue.Visible = true;
            toolStripMenuItemWorker.Visible = true;
            toolStripMenuItemShift.Visible = true;
            toolStripMenuItemTk.Visible = true;
            toolStripMenuItemDriver.Visible = true;
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role)
            {
                case "director": return "Директор";
                case "dispatcher": return "Диспетчер";
                case "hr_manager": return "Менеджер по кадрам";
                case "engineer": return "Инженер гаража";
                case "guest": return "Гость";
                default: return "Пользователь";
            }
        }
        private bool RoleCanSeeCrudButtons()
        {
            var r = _role?.ToLowerInvariant();
            return r == "director" || r == "dispatcher" || r == "hr_manager";
        }
        #endregion

        #region ==================== СПРАВОЧНИКИ ====================
        private void ToolStripMenuItemRefs_Click(object sender, EventArgs e)
        {
            _crudManager.SetVisible(true);
            if (RoleCanSeeCrudButtons())
                _crudManager.Show();
            else
                _crudManager.Hide();
            ClearSearch();
        }
        private void toolStripMenuItemBrand_Click_1(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "brand";
            _currentColumn = "brand_name";
            _currentTitle = "Марка";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();

        }
        private void toolStripMenuIteModel_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "model";
            _currentColumn = "model_name";
            _currentTitle = "Модель";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }


        private void toolStripMenuItemStation_Click_1(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "stop";
            _currentColumn = "name";
            _currentTitle = "Остановка";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemCity_Click_1(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "city";
            _currentColumn = "city_name";
            _currentTitle = "Город";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemStreet_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "street";
            _currentColumn = "street_name";
            _currentTitle = "Улица";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void ToolStripMenuItemSpecilty_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "specialty";
            _currentColumn = "name";
            _currentTitle = "Специальность";
            toolStripStatusLabel2.Text = _currentTitle;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void ToolStripMenuItemQualification_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "qualification";
            _currentColumn = "name";
            _currentTitle = "Квалификация";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
        }

        private void ToolStripMenuItemDepartment_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "Department";
            _currentColumn = "name";
            _currentTitle = "Структурное подразделение";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void ToolStripMenuItemPost_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = true;
            _currentEntity = null;
            _currentTable = "Post";
            _currentColumn = "name";
            _currentTitle = "Должность";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }

        private void ToolStripMenuItemWorkPlace_Click_1(object sender, EventArgs e)
        {

            _currentTable = "workplace";
            _currentColumn = "name";
            _currentTitle = "Место работы";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadDictionary();
            UpdateCrudButtons();
            ClearSearch();
        }
        private void LoadDictionary(string filter = null)
        {
            if (string.IsNullOrEmpty(_currentTable) || string.IsNullOrEmpty(_currentColumn))
                return;

            try
            {
                _dictLoading = true;
                _isDictionaryMode = true;
                _currentEntity = null;

                _dictConnection?.Close();
                _dictConnection = new NpgsqlConnection(_connString);
                _dictConnection.Open();

                string sql = $"SELECT id, {_currentColumn} FROM {_currentTable}";

                if (!string.IsNullOrWhiteSpace(filter))
                    sql += $" WHERE CAST({_currentColumn} AS text) ILIKE @p";

                sql += $" ORDER BY {_currentColumn};";

                _dictAdapter = new NpgsqlDataAdapter(sql, _dictConnection);

                if (!string.IsNullOrWhiteSpace(filter))
                    _dictAdapter.SelectCommand.Parameters.AddWithValue("p", "%" + filter + "%");

                var builder = new NpgsqlCommandBuilder(_dictAdapter);

                _dictTable = new DataTable();
                _dictAdapter.Fill(_dictTable);

                dgvMain.DataSource = _dictTable;

                if (dgvMain.Columns.Contains("id"))
                    dgvMain.Columns["id"].Visible = false;

                if (dgvMain.Columns.Contains(_currentColumn))
                    dgvMain.Columns[_currentColumn].HeaderText = _currentTitle;

                dgvMain.CellEndEdit -= DgvMain_CellEndEdit;
                dgvMain.CellEndEdit += DgvMain_CellEndEdit;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочника:\n" + ex.Message);
            }
            finally
            {
                _dictLoading = false;
            }

            
        }
        private void DgvMain_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (!_isDictionaryMode) return;
            if (_dictTable == null) return;
            if (_dictLoading) return;
            if (_dictSaving) return;
            SaveDictionaryChanges();
            
        }
        private void DgvMain_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (!_isDictionaryMode) return;
            if (_dictTable == null) return;
            if (_dictSuppressAutoSave) return;
            SaveDictionaryChanges();
        }
        private void SaveDictionaryChanges()
        {
            try
            {
                _dictSaving = true;
                dgvMain.EndEdit();
                this.Validate();

                var changes = _dictTable.GetChanges();
                if (changes == null) return;

                _dictAdapter.Update(_dictTable);
                _dictTable.AcceptChanges();

            }
            catch (Exception ex)
            {
                MessageBox.Show(("Ошибка сохранения:\n") + ex.Message);
                LoadDictionary(textBoxSearch.Text.Trim());
            }
            finally
            {
                _dictSaving = false;
            }
        }
        private bool ValidateDictionaryValue(string table, string value, out string error)
        {
            error = null;
            table = (table ?? "").ToLowerInvariant();
            value = (value ?? "").Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Поле не может быть пустым.";
                return false;
            }

            if (table == "city" && value.Length < 2)
            {
                error = "Название города должно быть не менее 2 символов.";
                return false;
            }

            if ((table == "brand" || table == "street") &&
                !System.Text.RegularExpressions.Regex.IsMatch(value, @"^[А-Яа-яA-Za-z .\-]+$"))
            {
                error = "Допустимы только буквы, пробел, точка и дефис.";
                return false;
            }


            if (table == "department")
            {
                var allowed = new[] { "Администрация" };
                if (!allowed.Contains(value))
                {
                    error = "Допустимо только значение: Администрация.";
                    return false;
                }
            }

            if (table == "profession")
            {
                var allowed = new[] { "Водитель автобуса" };
                if (!allowed.Contains(value))
                {
                    error = "Допустимо только значение: Водитель автобуса.";
                    return false;
                }
            }

            return true;
        }
        private void DgvMain_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (!_isDictionaryMode) return;
            if (_dictTable == null) return;
            if (string.IsNullOrWhiteSpace(_currentColumn)) return;

            var row = dgvMain.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            string value = row.Cells[_currentColumn].Value?.ToString() ?? "";

            if (!ValidateDictionaryValue(_currentTable, value, out string error))
            {
                row.ErrorText = error;
                e.Cancel = true;
                return;
            }

            row.ErrorText = "";
        }

        #endregion

        #region ==================== ДОКУМЕНТЫ ====================

        private void ToolStripMenuItemDocuments_Click(object sender, EventArgs e)
        {
            _crudManager.SetVisible(false);
            toolStripStatusLabel2.Visible = false;
        }
        private void toolStripMenuItemSQL_Click(object sender, EventArgs e)
        {
            dgvMain.Visible = false;
            pnlSql.Visible = true;
            panelPassword.Visible = false;
            panelPersonal.Visible = false;
            panelContent.Visible = false;      
            panelStatistics.Visible = false;
            LoadSqlTemplates();
            toolStripStatusLabel2.Visible = false;
        }
        private void ToolStripMenuItemAnalytics_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Visible = false;
            panelPersonal.Visible = false;
            ShowStatisticsPage();
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date;
            if (from > to)
            {
                MessageBox.Show("Дата начала больше даты окончания периода.");
            }
            _statsController.Refresh(from, to);

        }
        private void btnRefreshStats_Click(object sender, EventArgs e)
        {
            _statsController.Refresh();
        }
        private void buttonPdf_Click(object sender, EventArgs e)
        {
            Control target = panelStatistics;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF файл (*.pdf) | *.pdf";
                sfd.FileName = "Аналитика.pdf";
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    Bitmap bmp = new Bitmap(target.Width, target.Height);
                    target.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

                    PrintDocument pd = new PrintDocument();
                    pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";

                    pd.PrinterSettings.PrintToFile = true;
                    pd.PrinterSettings.PrintFileName = sfd.FileName;

                    pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
                    pd.DefaultPageSettings.Landscape = target.Width > target.Height;

                    pd.PrintPage += (s, ev) =>
                    {
                        Rectangle m = ev.MarginBounds;

                        float scale = Math.Min((float)m.Width / bmp.Width, (float)m.Height / bmp.Height);
                        int w = (int)(bmp.Width * scale);
                        int h = (int)(bmp.Height * scale);

                        int x = m.Left + (m.Width - w) / 2;
                        int y = m.Top + (m.Height - h) / 2;

                        ev.Graphics.DrawImage(bmp, new Rectangle(x, y, w, h));
                        ev.HasMorePages = false;
                    };

                    pd.Print();

                    bmp.Dispose();

                    MessageBox.Show("PDF сформирован:\n" + sfd.FileName, "Готово",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка экспорта в PDF:\n" + ex.Message, "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void ShowStatisticsPage()
        {
            dgvMain.Visible = false;
            pnlSql.Visible = false;
            panelStatistics.Visible = true;
            panelContent.Visible = true;

            panelStatistics.BringToFront();

            panelStatistics.Refresh();
            panelContent.Refresh();
            _statsController.Refresh(dtpFrom.Value.Date, dtpTo.Value.Date);
            panelContent.Dock = DockStyle.Top;
            panelStatistics.Dock = DockStyle.Fill;
        }
        #endregion

        #region ==================== СПРАВКА ====================

        private void toolStripMenuItemUsers_Click(object sender, EventArgs e)
        {
            FormUserGuide userGuideForm = new FormUserGuide();
            userGuideForm.ShowDialog();
        }

        private void toolStripMenuItemProgram_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                " Система управления автобусным парком\n\n" +
                "Версия: 1.0\n" +
                "Разработчик: Студент группы АП-326 Бабаева Дарья\n" +
                "© 2025\n\n" +
                "Эта система предназначена для управления\n" +
                "данными автобусного парка.",
                "О программе",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        #endregion

        #region ==================== ПРОФИЛЬ ====================

        private void ToolStripMenuItemHelp_Click_1(object sender, EventArgs e)
        {
            _crudManager.SetVisible(false);
            toolStripStatusLabel2.Visible = false;
        }
        private void ToolStripMenuItemPersonal_Click(object sender, EventArgs e)
        {
            panelPersonal.Visible = true;
            panelPassword.Visible = false;
            panelNewPassword.Visible = false;
            panelContent.Visible = false;
            panelStatistics.Visible = false;
            panelPersonal.Parent = this;
            panelPersonal.BringToFront();
            LoadCurrentUserProfile();
            LoadUserPhoto();
        }
        private void ToolStripMenuItemPasswordChange_Click(object sender, EventArgs e)
        {
            panelPersonal.Visible = false;
            toolStripStatusLabel2.Visible = false;
            pnlSql.Visible = false;
            dgvMain.Visible = false;
            _crudManager.SetVisible(false);

            panelPassword.Parent = this;          
            panelPassword.Dock = DockStyle.Fill;
            panelPassword.Visible = true;
            panelPassword.BringToFront();        

            panelNewPassword.Parent = panelPassword;
            panelNewPassword.Dock = DockStyle.None;
            panelNewPassword.Visible = true;
            panelNewPassword.BringToFront();
        }
        private void btnAddPhoto_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                File.Copy(ofd.FileName, GetUserPhotoPath(), true); 
                LoadUserPhoto();
            }
        }
        private string GetUserPhotoPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BusPark", "Photos");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, CurrentUserSession.Login + ".jpg");
        }
        private void LoadUserPhoto()
        {
            string p = GetUserPhotoPath();
            pbPhoto.SizeMode = PictureBoxSizeMode.Zoom;

            if (File.Exists(p))
                pbPhoto.Load(p);
            else
                pbPhoto.Image = null;
        }

        private void btnDeletePhoto_Click(object sender, EventArgs e)
        {
            if (pbPhoto.Image != null)
            {
                pbPhoto.Image.Dispose();
                pbPhoto.Image = null;
            }
        }
        private void LoadCurrentUserProfile()
        {
            if (!CurrentUserSession.IsLoggedIn())
            {
                MessageBox.Show("Авторизуйтесь для просмотра профиля.");
                return;
            }

            try
            {
                string sql =
        @"SELECT last_name, first_name, patronymic, gender, birth_date
  FROM worker
  WHERE login = @login;";

                using (var conn = new NpgsqlConnection(CurrentUserSession.ConnectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("login", CurrentUserSession.Login);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tbLastName.Text = reader["last_name"] == DBNull.Value ? "" : reader["last_name"].ToString();
                                tbFirstName.Text = reader["first_name"] == DBNull.Value ? "" : reader["first_name"].ToString();
                                tbPatronymic.Text = reader["patronymic"] == DBNull.Value ? "" : reader["patronymic"].ToString();

                                tbGender.Text = reader["gender"] == DBNull.Value ? "" : reader["gender"].ToString();

                                dtBirth.Value = reader["birth_date"] == DBNull.Value
                                    ? DateTime.Today
                                    : (DateTime)reader["birth_date"];

                                tbPosition.Text = GetRoleDisplayName(CurrentUserSession.Role);

                                MakeFieldsReadOnly(true);
                            }
                            else
                            {
                                ClearProfileFields();
                                MakeFieldsReadOnly(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки профиля:\n" + ex.Message,
                                "Ошибка",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    
        private void MakeFieldsReadOnly(bool readOnly)
        {
            tbLastName.ReadOnly = readOnly;
            tbFirstName.ReadOnly = readOnly;
            tbPatronymic.ReadOnly = readOnly;
            tbGender.Enabled = readOnly;
            tbPosition.Enabled = !readOnly;
            dtBirth.Enabled = readOnly;
        }
        private void ClearProfileFields()
        {
            tbLastName.Text = "";
            tbFirstName.Text = "";
            tbPatronymic.Text = "";

            tbGender.Text = "";
            dtBirth.Value = DateTime.Today;

            tbPosition.Text = "";  
        }



        #endregion

        #region ==================== ВЫХОД ====================

        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show(
            "Вы уверены, что хотите выйти из профиля?",
            "Выход",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

            CurrentUserSession.Role = null;
            CurrentUserSession.ConnectionString = null;

            LoginForm loginForm = new LoginForm();
            loginForm.Show();

            this.Hide();

        }

        #endregion

        #region ==================== КНОПКИ ====================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            

                if (string.IsNullOrWhiteSpace(_currentEntity))
            {
                MessageBox.Show("Сначала выберите сущность в меню 'Сущности'.");
                return;
            }
            Form dlg = null;

            switch (_currentEntity)
            {
                case "Bus":
                    dlg = new BusForm(_connString);
                    break;

                case "Control":
                    dlg = new FormControl(_connString);
                    break;
                case "Driver":
                    dlg = new FormDriver(_connString);
                    break;
                case "Revenue":
                    dlg = new FormRevenue(_connString);
                    break;
                case "Route":
                    dlg = new FormRoute(_connString);
                    break;
                case "Schedule":
                    dlg = new FormSchedule(_connString);
                    break;
                case "Shift":
                    dlg = new FormShift(_connString);
                    break;
                case "Trip":
                    dlg = new FormTrip(_connString);
                    break;
                case "Worker":
                    dlg = new FormWorker(_connString);
                    break;
                case "Tk":
                    dlg = new FormTk(_connString);
                    break;
                    
            }

            if (dlg.ShowDialog(this) == DialogResult.OK) 
            {
                switch (_currentEntity)
                {
                    case "bus": LoadBus(null); break;
                    case "control": LoadControl(null); break;
                    case "route": LoadRoute(null); break;
                    case "driver": LoadDriver(null); break;
                    case "revenue": LoadRevenue(null); break;
                    case "schedule": LoadSchedule(null); break;
                    case "shift": LoadShift(null); break;
                    case "tk": LoadTk(null); break;
                    case "trip": LoadTrip(null); break;
                    case "worker": LoadWorker(null); break;
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
           if (_isDictionaryMode)
            {
                if (dgvMain.CurrentRow == null)
                {
                    MessageBox.Show("Выберите строку для изменения.");
                    return;
                }

                dgvMain.ReadOnly = false;
                dgvMain.CurrentCell = dgvMain.CurrentRow.Cells[_currentColumn];
                dgvMain.BeginEdit(true);
                return;
            }

            if (dgvMain.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку для изменения.");
                return;
            }

            object cellValue = dgvMain.CurrentRow.Cells["id"].Value;
            if (cellValue == null || cellValue == DBNull.Value)
            {
                MessageBox.Show("Не удалось определить выбранную строку.");
                return;
            }

            int id = Convert.ToInt32(cellValue);

            if (_currentEntity == "Bus")
            {
                using (var f = new BusForm(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }

            if (_currentEntity == "Driver")
            {
                using (var f = new FormDriver(_connString, id)) 
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Control")
            {
                using (var f = new FormControl(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Revenue")
            {
                using (var f = new FormRevenue(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Route")
            {
                using (var f = new FormRoute(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Schedule")
            {
                using (var f = new FormSchedule(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Shift")
            {
                using (var f = new FormShift(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Trip")
            {
                using (var f = new FormTrip(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Worker")
            {
                using (var f = new FormWorker(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
            if (_currentEntity == "Tk")
            {
                using (var f = new FormTk(_connString, id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                        LoadEntity(null);
                }
                return;
            }
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            string key = !string.IsNullOrEmpty(_currentEntity)
        ? _currentEntity
        : _currentTable;

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Не выбрана сущность или справочник.");
                return;
            }

            bool deleted = _deleteManager.DeleteCurrentRow(key);

            if (deleted)
            {
                string filter = textBoxSearch.Text.Trim();

                if (!string.IsNullOrEmpty(_currentEntity))
                    LoadEntity(filter);
                else
                    LoadDictionary(filter);
            }
        }
        private void ClearSearch()
        {
            textBoxSearch.Clear();   
        }

        private void BindTable(DataTable table)
        {
            _searchManager.SetTable(table);
        }
        private void BindSqlTable(DataTable table)
        {
            _searchManagerSql.SetTable(table);
        }
        private void UpdateCrudButtons()
        {
            bool canEditByRole = RoleCanSeeCrudButtons();
            bool hasRow = dgvMain.CurrentRow != null;
            btnAdd.Enabled = canEditByRole;
            btnEdit.Enabled = canEditByRole && hasRow;
            btnDelete.Enabled = canEditByRole && hasRow;
;
        }
        #endregion

        #region ===================== СУЩНОСТИ ===================
        private void ToolStripMenuItemBus_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Bus";
            _currentTable = null;
            _currentTitle = "Автобус";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemRoute_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Route";
            _currentTable = null;
            _currentTitle = "Маршрут";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemTrip_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Trip";
            _currentTable = null;
            _currentTitle = "Рейс";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemSchedule_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Schedule";
            _currentTable = null;
            _currentTitle = "Расписание движения";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemRevenue_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Revenue";
            _currentTable = null;
            _currentTitle = "Выручка";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemControl_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Control";
            _currentTable = null;
            _currentTitle = "Контроль";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemWorker_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Worker";
            _currentTable = null;
            _currentTitle = "Сотрудник";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemDriver_Click(object sender, EventArgs e)
        {
            _currentEntity = "Driver";
            _currentTable = null;
            _currentTitle = "Водитель";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void toolStripMenuItemShift_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Shift";
            _currentTable = null;
            _currentTitle = "Смена";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }


        private void toolStripMenuItemTk_Click_1(object sender, EventArgs e)
        {
            _currentEntity = "Tk";
            _currentTable = null;
            _currentTitle = "Трудовая книжка";
            toolStripStatusLabel2.Text = _currentTitle;
            toolStripStatusLabel2.Visible = true;
            LoadEntity(null);
            UpdateCrudButtons();
            ClearSearch();
        }

        private void LoadEntity(string filter)
        {
            switch (_currentEntity)
            {
                case "Bus":
                    LoadBus(filter);
                    break;

                case "Route":
                    LoadRoute(filter);
                    break;

                case "Trip":
                    LoadTrip(filter);
                    break;

                case "Worker":
                    LoadWorker(filter);
                    break;

                case "Schedule":
                    LoadSchedule(filter);
                    break;

                case "Tk":
                    LoadTk(filter);
                    break;

                case "Shift":
                    LoadShift(filter);
                    break;

                case "Driver":
                    LoadDriver(filter);
                    break;

                case "Revenue":
                    LoadRevenue(filter);
                    break;

                case "Control":
                    LoadControl(filter);
                    break;


                default:
                    dgvMain.DataSource = null;
                    break;
            }
        }
        private void LoadBus(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT 
                    bus.id,
                    bus.registration_number   AS ""Гос. номер"",
                    bus.body_number AS ""Номер кузова"",
                    bus.chassis_number AS ""Номер шасси"",
                    bus.identification_number AS ""Инвентарный номер"",
                    brand.brand_name        AS ""Марка"",
                    model.model_name         AS ""Модель"",
                    bus.status        AS ""Состояние"",
                    bus.release_date AS ""Дата выпуска"",
                    bus.mileage      AS ""Пробег"",
                    bus.capacity     AS ""Вместимость""
                FROM bus 
                JOIN brand  ON bus.brand_id = brand.id
                JOIN model   ON bus.model_id  = model.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE bus.registration_number ILIKE @p
                       OR brand.brand_name      ILIKE @p
                       OR model.model_name       ILIKE @p
                       OR bus.color      ILIKE @p
                       OR bus.state      ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader()) table.Load(r);

                    BindTable(table);

                    dgvMain.DataSource = table;
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки автобусов:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void LoadWorker(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT 
                    worker.id,
                    worker.last_name    AS ""Фамилия"",
                    worker.first_name   AS ""Имя"",
                    worker.patronymic   AS ""Отчество"",
                    worker.gender       AS ""Пол"",
                    worker.birth_date   AS ""Дата рождения"",
                    worker.work_experience   AS ""Стаж"",
                    city.city_name         AS ""Город"",
                    street.street_name         AS ""Улица"",
                    worker.house_number AS ""Номер дома"",
                    worker.salary       AS ""Зарплата""
                FROM worker 
                JOIN city    ON worker.city_id   = city.id
                JOIN street  ON worker.street_id = street.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE worker.last_name  ILIKE @p
                       OR worker.first_name ILIKE @p
                       OR city.name       ILIKE @p
                       OR street.street_name       ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader()) table.Load(r);

                    BindTable(table);

                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки работников:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadControl(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    cmd.CommandText = @"
                SELECT
                    control.id,
                    incident.reason    AS ""Происшествие"",
                    stop.name    AS ""Остановка"",
                    control.arrival_time AS ""Время прибытия"",
                    control.eater_count  AS ""Количество пассажиров""
                FROM control 
                JOIN incident  ON control.incident_id = incident.id
                JOIN stop      ON control.stop_id     = stop.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE incident.name ILIKE @p
                       OR stop.name ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);

                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Контроль:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRoute(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    cmd.CommandText = @"
                SELECT
                    route.id,
                    s1.name AS ""Начальная остановка"",
                    s2.name AS ""Конечная остановка"",
                    route.full_turn_time AS ""Время полного оборота""
                FROM route 
                JOIN stop s1 ON route.start_stop_id = s1.id
                JOIN stop s2 ON route.end_stop_id   = s2.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE s1.name ILIKE @p
                       OR s2.name ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Маршрут:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTrip(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT
                    trip.id,
                    bus.registration_number AS ""Автобус"",
                    route.id    AS ""Маршрут"",
                    trip.number    AS ""Номер рейса""
                FROM trip 
                JOIN bus    ON trip.bus_id   = bus.id
                JOIN route  ON trip.route_id = route.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE registration_number ILIKE @p
                       OR CAST(route.id AS text) ILIKE @p
                       OR CAST(trip.number AS text) ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Рейс:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRevenue(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT
                    revenue.id,
                    CONCAT(worker.last_name, ' ', worker.first_name, ' ', COALESCE(worker.patronymic, '')) AS ""Сотрудник"",
                    CONCAT(wd.last_name, ' ', wd.first_name, ' ', COALESCE(wd.patronymic, '')) AS ""Водитель"",
                    revenue.amount       AS ""Сумма"",
                    revenue.period_start AS ""Период с"",
                    revenue.period_end   AS ""Период по""
                FROM revenue 
                JOIN worker  ON revenue.driver_id   = worker.id      -- если driver_id = worker.id
                JOIN worker wd   ON revenue.employee_id = wd.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE worker.last_name   ILIKE @p
                       OR worker.first_name  ILIKE @p
                       OR wd.last_name  ILIKE @p
                       OR wd.first_name ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Выручка:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDriver(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT
                    driver.id,
                    bus.registration_number                    AS ""Автобус"",
                    CONCAT(worker.last_name, ' ', worker.first_name, ' ', COALESCE(worker.patronymic, '')) AS ""Сотрудник"",
                    driver.category                       AS ""Категория""
                FROM driver 
                JOIN bus     ON driver.bus_id      = bus.id
                JOIN worker  ON driver.employee_id = worker.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE bus.registration_number ILIKE @p
                       OR worker.last_name  ILIKE @p
                       OR worker.first_name ILIKE @p
                       OR driver.category   ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Водитель:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadShift(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
                SELECT
                    shift.id,
                    CONCAT(worker.last_name, ' ', worker.first_name, ' ', COALESCE(worker.patronymic, '')) AS ""Водитель"",
                    shift.shift_type   AS ""Тип смены"",
                    shift.start_time   AS ""Начало"",
                    shift.end_time     AS ""Окончание""
                FROM shift 
                JOIN driver  ON shift.driver_id   = driver.id
                JOIN worker  ON driver.employee_id = worker.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE worker.last_name  ILIKE @p
                       OR worker.first_name ILIKE @p
                       OR shift.shift_type ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;


                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Смена:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTk(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    cmd.CommandText = @"
                SELECT
                    tk.id,
                    CONCAT(worker.last_name, ' ', worker.first_name, ' ', COALESCE(worker.patronymic, '')) AS ""Сотрудник"",
                    workplace.name       AS ""Место работы"",
                    profession.name       AS ""Профессия"",
                    specialty.name       AS ""Специальность"",
                    qualification.name        AS ""Квалификация"",
                    post.name        AS ""Должность"",
                    department.name        AS ""Отдел"",
                    tk.hire_date   AS ""Дата приема"",
                    tk.dismissal_date      AS ""Дата увольнения"",
                    tk.termination_reason  AS ""Причина увольнения"",
                    tk.event_basis         AS ""Основание статуса"",
                    tk.event_type          AS ""Статус работника""
                FROM tk 
                JOIN worker        ON tk.employee_id     = worker.id
                JOIN workplace    ON tk.workplace_id    = workplace.id
                JOIN profession   ON tk.profession_id   = profession.id
                JOIN specialty    ON tk.specialty_id    = specialty.id
                JOIN qualification  ON tk.qualification_id = qualification.id
                JOIN post         ON tk.post_id         = post.id
                JOIN department    ON tk.department_id   = department.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE worker.last_name   ILIKE @p
                       OR worker.first_name  ILIKE @p
                       OR workplace.name       ILIKE @p
                       OR profession.name       ILIKE @p
                       OR ppst.name        ILIKE @p
                       OR department.name        ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;


                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Трудовая книжка:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSchedule(string filter)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    cmd.CommandText = @"
                        SELECT
                    schedule.id,
                    route.id          AS ""Маршрут"",
                    schedule.first_departure  AS ""Выезд первого автобуса"",
                    schedule.last_departure   AS ""Выезд последнего автобуса"",
                    schedule.first_dispatch   AS ""Время отправления
с конечной остановки"",
                    schedule.nth_dispatch     AS ""Время прибытия в парк"",
                    schedule.movement_interval AS ""Интервал движения""
                FROM schedule 
                JOIN route  ON schedule.route_id = route.id
            ";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        cmd.CommandText += @"
                    WHERE CAST(route.id AS text) ILIKE @p
                       OR CAST(schedule.first_departure  AS text) ILIKE @p
                       OR CAST(schedule.last_departure   AS text) ILIKE @p
                       OR CAST(schedule.movement_interval AS text) ILIKE @p
                ";
                        cmd.Parameters.AddWithValue("p", "%" + filter + "%");
                    }

                    var table = new DataTable();
                    using (var r = cmd.ExecuteReader())
                        table.Load(r);

                    BindTable(table);
                    if (dgvMain.Columns.Contains("id"))
                        dgvMain.Columns["id"].Visible = false;

                    foreach (DataGridViewColumn col in dgvMain.Columns)
                        col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы Расписание:\n{ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ToolStripMenuItemEntity_Click(object sender, EventArgs e)
        {
            _isDictionaryMode = false;
            dgvMain.RowValidated -= DgvMain_RowValidated;
            pnlSql.Visible = false;
            dgvMain.Visible = true;
            panelPassword.Visible = false;

            if (RoleCanSeeCrudButtons())
                _crudManager.Show();   
            else
                _crudManager.Hide();

        }
        #endregion

        #region ======================= SQL ========================
        private void btnSqlInsert_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Изменение данных и структуры БД через SQL‑вкладку запрещено.",
         "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void btnTemplateLoad_Click(object sender, EventArgs e)
        {
           
            SqlTemplateItem selected = cmbSqlTemplates.SelectedItem as SqlTemplateItem;
            if (selected == null)
            {
                MessageBox.Show("Выберите шаблон из списка.");
                return;
            }

            try
            {
                string sqlText = _sqlRepo.GetTemplateSql(selected.Id);

               
                if (!string.IsNullOrEmpty(sqlText))
                {
                    txtSql.Text = sqlText;
                    txtTemplateName.Text = selected.Name;
                }
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Ошибка чтения шаблона:\n" + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTempplateSave_Click(object sender, EventArgs e)
        {
            var name = txtTemplateName.Text.Trim();
            var sqlBody = txtSql.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название шаблона.");
                return;
            }
            if (string.IsNullOrEmpty(sqlBody))
            {
                MessageBox.Show("Нет текста запроса.");
                return;
            }

            _sqlRepo.SaveTemplate(name, sqlBody);
            LoadSqlTemplates();
        }

        private void btnSqlExecute_Click(object sender, EventArgs e)
        {
            var sql = txtSql.Text.Trim();
            if (string.IsNullOrEmpty(sql))
            {
                MessageBox.Show("Введите SQL‑запрос.");
                return;
            }

            try
            {
                var table = _sqlRepo.ExecuteSafeSelect(sql);
                BindSqlTable(table);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Запрос заблокирован",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (PostgresException ex)
            {
                MessageBox.Show("Ошибка SQL:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadSqlTemplates()
        {
            cmbSqlTemplates.Items.Clear();
            List<SqlTemplateItem> items = _sqlRepo.GetAllTemplates();
            foreach (var item in items)
                cmbSqlTemplates.Items.Add(item);

            if (cmbSqlTemplates.Items.Count > 0)
                cmbSqlTemplates.SelectedIndex = 0;
        }

        private void btnSqlClear_Click(object sender, EventArgs e)
        {
            txtSql.Clear();
            dgvSqlResult.DataSource = null;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dgvSqlResult.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV файл (*.csv)|*.csv|Текстовый файл (*.txt)|*.txt";
                sfd.Title = "Сохранить результаты";
                sfd.FileName = "result.csv";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                using (var sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                {
                    for (int i = 0; i < dgvSqlResult.Columns.Count; i++)
                    {
                        if (i > 0) sw.Write(";");
                        sw.Write(dgvSqlResult.Columns[i].HeaderText);
                    }
                    sw.WriteLine();

                    foreach (DataGridViewRow row in dgvSqlResult.Rows)
                    {
                        if (row.IsNewRow) continue;

                        for (int i = 0; i < dgvSqlResult.Columns.Count; i++)
                        {
                            if (i > 0) sw.Write(";");
                            var value = row.Cells[i].Value?.ToString() ?? "";
                            value = value.Replace("\"", "\"\"");
                            sw.Write($"\"{value}\"");
                        }
                        sw.WriteLine();
                    }
                }

                MessageBox.Show("Данные успешно сохранены.");
            }
        }
        #endregion

        #region ====================== ПАРОЛЬ ======================
        private void buttonSave_Click_1(object sender, EventArgs e)
        {
            string login = txtBoxLogin1.Text.Trim();
            string newPass = textBoxNewPassword.Text;
            string repeat = textBoxPasswordNewNew.Text;

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtBoxLogin1.Focus();
                return;
            }

            try
            {
                _passwordService.ValidatePasswords(newPass, repeat);
                _passwordService.ChangeCurrentUserPassword(newPass);

                CurrentUserSession.Password = newPass;

                var csb = new NpgsqlConnectionStringBuilder(_connString)
                {
                        Username = CurrentUserSession.Login,
                        Password = CurrentUserSession.Password
                };
                _passwordService = new PasswordService(csb.ConnectionString);
                

                MessageBox.Show("Пароль успешно изменён.", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtBoxLogin1.Clear();
                textBoxNewPassword.Clear();
                textBoxPasswordNewNew.Clear();
                txtBoxLogin1.Focus();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (PostgresException pgEx)
            {
                MessageBox.Show("Ошибка базы данных:\n" + pgEx.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (NpgsqlException npgEx)
            {
                MessageBox.Show("Ошибка подключения к БД:\n" + npgEx.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось изменить пароль:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonEye1_Click_1(object sender, EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                textBoxNewPassword.UseSystemPasswordChar = false;
                textBoxPasswordNewNew.UseSystemPasswordChar = false;
            }
            else
            {
                textBoxNewPassword.UseSystemPasswordChar = true;
                textBoxPasswordNewNew.UseSystemPasswordChar = true;
            }
        }
        #endregion

        
    }

}
