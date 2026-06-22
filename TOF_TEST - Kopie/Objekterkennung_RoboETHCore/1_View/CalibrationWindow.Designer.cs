namespace Objekterkennung
{
    partial class CalibrationWindow
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
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.label1 = new System.Windows.Forms.Label();
            this.bStartKalibration = new System.Windows.Forms.Button();
            this.btn_SaveMat = new System.Windows.Forms.Button();
            this.btn_SetHomography = new System.Windows.Forms.Button();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.DisplayWindow = new System.Windows.Forms.PictureBox();
            this.Refresh = new System.Windows.Forms.Button();
            this.Button_Quit = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.DisplayWindow)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(542, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(296, 26);
            this.label1.TabIndex = 5;
            this.label1.Text = "Nehmen Sie 10 Bilder mit einem Schachbrettmuster\r\naus unterschiedlichen Winkeln a" +
    "uf.\r\n";
            // 
            // bStartKalibration
            // 
            this.bStartKalibration.Location = new System.Drawing.Point(567, 87);
            this.bStartKalibration.Name = "bStartKalibration";
            this.bStartKalibration.Size = new System.Drawing.Size(143, 25);
            this.bStartKalibration.TabIndex = 6;
            this.bStartKalibration.Text = "Start Kalibration";
            this.bStartKalibration.UseVisualStyleBackColor = true;
            this.bStartKalibration.Click += new System.EventHandler(this.StartKalibration_Click);
            // 
            // btn_SaveMat
            // 
            this.btn_SaveMat.Location = new System.Drawing.Point(567, 387);
            this.btn_SaveMat.Name = "btn_SaveMat";
            this.btn_SaveMat.Size = new System.Drawing.Size(143, 23);
            this.btn_SaveMat.TabIndex = 7;
            this.btn_SaveMat.Text = "Daten Speichern";
            this.btn_SaveMat.UseVisualStyleBackColor = true;
            this.btn_SaveMat.Click += new System.EventHandler(this.SaveMat_Click);
            // 
            // btn_SetHomography
            // 
            this.btn_SetHomography.Location = new System.Drawing.Point(567, 358);
            this.btn_SetHomography.Name = "btn_SetHomography";
            this.btn_SetHomography.Size = new System.Drawing.Size(143, 23);
            this.btn_SetHomography.TabIndex = 8;
            this.btn_SetHomography.Text = "Arbeitsbereich definieren";
            this.btn_SetHomography.UseVisualStyleBackColor = true;
            this.btn_SetHomography.Click += new System.EventHandler(this.SetHomography_Click);
            // 
            // DisplayWindow
            // 
            this.DisplayWindow.Location = new System.Drawing.Point(12, 12);
            this.DisplayWindow.Name = "DisplayWindow";
            this.DisplayWindow.Size = new System.Drawing.Size(524, 449);
            this.DisplayWindow.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.DisplayWindow.TabIndex = 9;
            this.DisplayWindow.TabStop = false;
            // 
            // Refresh
            // 
            this.Refresh.Location = new System.Drawing.Point(567, 43);
            this.Refresh.Name = "Refresh";
            this.Refresh.Size = new System.Drawing.Size(143, 38);
            this.Refresh.TabIndex = 10;
            this.Refresh.Text = "Foto Aufnehmen";
            this.Refresh.UseVisualStyleBackColor = true;
            this.Refresh.Click += new System.EventHandler(this.TakePicture_Click);
            // 
            // Button_Quit
            // 
            this.Button_Quit.Location = new System.Drawing.Point(567, 416);
            this.Button_Quit.Name = "Button_Quit";
            this.Button_Quit.Size = new System.Drawing.Size(143, 45);
            this.Button_Quit.TabIndex = 11;
            this.Button_Quit.Text = "Quit";
            this.Button_Quit.UseVisualStyleBackColor = true;
            this.Button_Quit.Click += new System.EventHandler(this.Quit_Click);
            // 
            // CalibrationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(835, 469);
            this.Controls.Add(this.Button_Quit);
            this.Controls.Add(this.Refresh);
            this.Controls.Add(this.DisplayWindow);
            this.Controls.Add(this.btn_SetHomography);
            this.Controls.Add(this.btn_SaveMat);
            this.Controls.Add(this.bStartKalibration);
            this.Controls.Add(this.label1);
            this.MinimumSize = new System.Drawing.Size(320, 240);
            this.Name = "CalibrationWindow";
            this.Text = "Kalibrierung";
            ((System.ComponentModel.ISupportInitialize)(this.DisplayWindow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bStartKalibration;
        private System.Windows.Forms.Button btn_SaveMat;
        private System.Windows.Forms.Button btn_SetHomography;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.Windows.Forms.PictureBox DisplayWindow;
        private System.Windows.Forms.Button Refresh;
        private System.Windows.Forms.Button Button_Quit;
    }
}