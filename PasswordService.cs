using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Npgsql;

namespace Bus_coursework
{
    public class PasswordService
    {
        private string _adminConnectionString;

        public PasswordService(string adminConnectionString)
        {
            _adminConnectionString = adminConnectionString;
        }

        public static class CueBanner
        {
            private const int EM_SETCUEBANNER = 0x1501;

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
            public static void Set(TextBox tb, string text, bool showWhenFocused = false)
            {
                void Apply() =>
                    SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)(showWhenFocused ? 1 : 0), text);

                if (!tb.IsHandleCreated)
                    tb.HandleCreated += (s, e) => Apply();
                else
                    Apply();
            }
        }

        public void ValidatePasswords(string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                throw new ArgumentException("Пароль и подтверждение не должны быть пустыми.");

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                throw new ArgumentException("Новый пароль и подтверждение не совпадают.");
        }

        public void ChangeCurrentUserPassword(string newPlainPassword)
        {
            if (string.IsNullOrWhiteSpace(newPlainPassword))
                throw new ArgumentException("Новый пароль не указан.", nameof(newPlainPassword));

            using (var conn = new NpgsqlConnection(_adminConnectionString))
            {
                conn.Open();

                string passLiteral;
                using (var q = new NpgsqlCommand("SELECT quote_literal(@p)", conn))
                {
                    q.Parameters.AddWithValue("p", newPlainPassword);
                    passLiteral = (string)q.ExecuteScalar();
                }

                using (var cmd = new NpgsqlCommand(
                    "ALTER ROLE CURRENT_USER WITH PASSWORD " + passLiteral + ";", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            var csb = new NpgsqlConnectionStringBuilder(_adminConnectionString);
            csb.Password = newPlainPassword;
            _adminConnectionString = csb.ConnectionString;
        }


    }
}
