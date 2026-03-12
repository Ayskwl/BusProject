namespace Bus_coursework
{
    partial class FormSchedule
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSchedule));
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.mtbMoveInterval = new System.Windows.Forms.MaskedTextBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.dtpFirstDeparture = new System.Windows.Forms.DateTimePicker();
            this.dtpLastDeparture = new System.Windows.Forms.DateTimePicker();
            this.dtpFirstDispatch = new System.Windows.Forms.DateTimePicker();
            this.dtpNthDispatch = new System.Windows.Forms.DateTimePicker();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(444, 26);
            this.label1.TabIndex = 2;
            this.label1.Text = "Добавление данных о расписании движения";
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Black;
            this.panel2.Location = new System.Drawing.Point(17, 67);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(507, 1);
            this.panel2.TabIndex = 24;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(13, 101);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(182, 19);
            this.label2.TabIndex = 27;
            this.label2.Text = "Выезд первого автобуса";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(13, 166);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(207, 19);
            this.label3.TabIndex = 28;
            this.label3.Text = "Выезд последнего автобуса";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(13, 221);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(163, 38);
            this.label4.TabIndex = 29;
            this.label4.Text = "Время отправления\r\nс конечной остановки";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.Location = new System.Drawing.Point(13, 304);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(173, 19);
            this.label5.TabIndex = 30;
            this.label5.Text = "Время прибытия в парк";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label6.Location = new System.Drawing.Point(13, 373);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(147, 19);
            this.label6.TabIndex = 31;
            this.label6.Text = "Интервал движения";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Black;
            this.panel3.Location = new System.Drawing.Point(17, 434);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(507, 1);
            this.panel3.TabIndex = 25;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.LavenderBlush;
            this.panel1.Controls.Add(this.dtpNthDispatch);
            this.panel1.Controls.Add(this.dtpFirstDispatch);
            this.panel1.Controls.Add(this.dtpLastDeparture);
            this.panel1.Controls.Add(this.dtpFirstDeparture);
            this.panel1.Controls.Add(this.mtbMoveInterval);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(544, 567);
            this.panel1.TabIndex = 0;
            // 
            // mtbMoveInterval
            // 
            this.mtbMoveInterval.Location = new System.Drawing.Point(238, 370);
            this.mtbMoveInterval.Mask = "00:00:00";
            this.mtbMoveInterval.Name = "mtbMoveInterval";
            this.mtbMoveInterval.Size = new System.Drawing.Size(273, 22);
            this.mtbMoveInterval.TabIndex = 38;
            this.mtbMoveInterval.ValidatingType = typeof(System.DateTime);
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(196, 478);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(126, 34);
            this.btnCancel.TabIndex = 33;
            this.btnCancel.Text = "Отменить";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click_1);
            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnSave.Location = new System.Drawing.Point(17, 478);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(125, 34);
            this.btnSave.TabIndex = 31;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // dtpFirstDeparture
            // 
            this.dtpFirstDeparture.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpFirstDeparture.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFirstDeparture.Location = new System.Drawing.Point(238, 101);
            this.dtpFirstDeparture.Name = "dtpFirstDeparture";
            this.dtpFirstDeparture.Size = new System.Drawing.Size(273, 22);
            this.dtpFirstDeparture.TabIndex = 39;
            // 
            // dtpLastDeparture
            // 
            this.dtpLastDeparture.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpLastDeparture.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpLastDeparture.Location = new System.Drawing.Point(238, 166);
            this.dtpLastDeparture.Name = "dtpLastDeparture";
            this.dtpLastDeparture.Size = new System.Drawing.Size(273, 22);
            this.dtpLastDeparture.TabIndex = 40;
            // 
            // dtpFirstDispatch
            // 
            this.dtpFirstDispatch.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpFirstDispatch.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFirstDispatch.Location = new System.Drawing.Point(238, 237);
            this.dtpFirstDispatch.Name = "dtpFirstDispatch";
            this.dtpFirstDispatch.Size = new System.Drawing.Size(273, 22);
            this.dtpFirstDispatch.TabIndex = 41;
            // 
            // dtpNthDispatch
            // 
            this.dtpNthDispatch.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpNthDispatch.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpNthDispatch.Location = new System.Drawing.Point(238, 304);
            this.dtpNthDispatch.Name = "dtpNthDispatch";
            this.dtpNthDispatch.Size = new System.Drawing.Size(273, 22);
            this.dtpNthDispatch.TabIndex = 42;
            // 
            // FormSchedule
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 567);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSchedule";
            this.Text = "Расписание движения";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.MaskedTextBox mtbMoveInterval;
        private System.Windows.Forms.DateTimePicker dtpFirstDeparture;
        private System.Windows.Forms.DateTimePicker dtpNthDispatch;
        private System.Windows.Forms.DateTimePicker dtpFirstDispatch;
        private System.Windows.Forms.DateTimePicker dtpLastDeparture;
    }
}