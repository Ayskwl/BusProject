using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using System.Configuration;
using System.Security.Cryptography;


namespace Bus_coursework
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            try
            {
                _connectionString = ConfigurationManager
                    .ConnectionStrings["bus"].ConnectionString;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке конфигурации: {ex.Message}",
                    "Критическая ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _connectionString = "";
            }
        }

        private readonly string _connectionString;

        private bool _passwordVisible = false;

        private void Log_in_Click(object sender, EventArgs e)
        {
           
            try
            {
                string login = Login.Text.Trim();
                string password = Password.Text;

                if (!ValidateInput(login, password))
                {
                    return; 
                }

                if (!TestDatabaseConnection())
                {
                    MessageBox.Show(
                        " Ошибка подключения к базе данных.\n" +
                        "Убедитесь, что PostgreSQL запущен и доступен.",
                        "Ошибка подключения",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                bool isAuthenticated = AuthenticateUser(login, password);

                if (!isAuthenticated)
                {

                    MessageBox.Show(
                        "Неверный логин или пароль",
                        "Ошибка авторизации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    Password.Clear();
                    Password.Focus();
                    return;
                }
                if (!string.IsNullOrEmpty(login))
                {
                    var logins = Properties.Settings.Default.Logins
                                 ?? new System.Collections.Specialized.StringCollection();

                    if (!logins.Contains(login))
                    {
                        logins.Add(login);
                        Properties.Settings.Default.Logins = logins;
                        Properties.Settings.Default.Save();
                    }
                }

                string userRole = GetUserRole(login);

                CurrentUserSession.Login = login;
                CurrentUserSession.Role = userRole;
                CurrentUserSession.ConnectionString = _connectionString;
                CurrentUserSession.Password = password;

                MainForm mainForm = new MainForm(userRole, login, _connectionString);
                this.Hide();
                mainForm.ShowDialog();

               
                this.Hide();
            }
            catch (PostgresException pgEx)
            {
                MessageBox.Show(
                    "Ошибка PostgreSQL:\n" + pgEx.Message,
                    "Ошибка базы данных",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Неожиданная ошибка:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private bool ValidateInput(string login, string password)
        {
           
            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show(
                    "Пожалуйста, введите логин",
                    "Валидация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Login.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Пожалуйста, введите пароль",
                    "Валидация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Password.Focus();
                return false;
            }
            if (login.Length < 2)
            {
                MessageBox.Show(
                    "Логин должен содержать минимум 2 символа",
                    "Валидация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Login.Focus();
                return false;
            }
            if (password.Length < 1)
            {
                MessageBox.Show(
                    "Пароль не может быть пустым",
                    "Валидация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Password.Focus();
                return false;
            }

            return true; 
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private bool AuthenticateUser(string login, string password)
        {
            try
            {
                
                var builder = new NpgsqlConnectionStringBuilder(_connectionString)
                {
                    Username = login,
                    Password = password
                };
                string userConnectionString = builder.ConnectionString;
                using (var conn = new NpgsqlConnection(userConnectionString))
                {
                    conn.Open(); 
                    return true; 
                }
            }
            catch (PostgresException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private string GetUserRole(string dbUser)
        {
            switch (dbUser.ToLower())
            {
                case "superuser1":
                case "superuser2":
                case "postgres":
                    return "director";

                case "dispatcher":
                case "user2":
                    return "dispatcher";

                case "hr_manager":
                case "user3":
                    return "hr_manager";

                case "engineer":
                case "user4":
                    return "engineer";

                default:
                    return "guest";
            }
        }
       
       
        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            Password.UseSystemPasswordChar = !_passwordVisible;
        }
        public static class PasswordHelper
        {
            public static string HashPassword(string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Пароль не может быть пустым");

                using (SHA256 sha = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(password);

                    byte[] hash = sha.ComputeHash(bytes);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            public static bool VerifyPassword(string password, string hash)
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false; 
                    string hashOfInput = HashPassword(password); 
                    return hashOfInput == hash;
                }
            }


        private void btnGuest_Click(object sender, EventArgs e)
        {
            string login = "user1";
            string password = "123";
            string role = GetUserRole(login);

            CurrentUserSession.Login = login;
            CurrentUserSession.Role = role;
            CurrentUserSession.ConnectionString = _connectionString;
            CurrentUserSession.Password = password;

            MainForm mainForm = new MainForm(role, login, _connectionString);
            this.Hide();
            mainForm.ShowDialog();
            this.Hide();
        }
        private void LoginForm_Load(object sender, EventArgs e)
        {
            var source = new AutoCompleteStringCollection();

            if (Properties.Settings.Default.Logins != null)
            {
                source.AddRange(
                    Properties.Settings.Default.Logins.Cast<string>().ToArray()
                );
            }

            Login.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            Login.AutoCompleteSource = AutoCompleteSource.CustomSource;
            Login.AutoCompleteCustomSource = source;
            Password.Multiline = false;
            Password.UseSystemPasswordChar = true;
            Login.Focus();

        }

    }
    public static class CurrentUserSession
    {
       
        public static string Login { get; set; }
        public static string Role { get; set; }
        public static string ConnectionString { get; set; }
        public static string Password { get; set; }

        public static bool HasRole(string role)
        {
            return Role == role;
        }
        public static bool IsAdmin()
        {
            return Role == "director";
        }

        public static bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(Role);
        }
        public static void Logout()
        {
            Login = null;
            Role = null;
            ConnectionString = null;
            Password = null;
        }
    }
}
