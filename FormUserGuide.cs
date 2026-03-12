using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bus_coursework
{
    public partial class FormUserGuide : Form
    {
        public FormUserGuide()
        {
            InitializeComponent();
        }

        private void linkLabelUserGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.google.com/document/d/1hp7o0MucWd-B88xN-L_ecYGx86a2lE1xZiQJ-WY7E0E/edit?usp=sharing",
                UseShellExecute = true
            });
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
