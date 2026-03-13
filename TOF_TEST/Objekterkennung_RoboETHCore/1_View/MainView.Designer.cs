namespace Objekterkennung
{
    partial class MainView
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            btn_StartSearching = new System.Windows.Forms.Button();
            btn_StartSorting = new System.Windows.Forms.Button();
            pbBox3 = new System.Windows.Forms.PictureBox();
            pbBox4 = new System.Windows.Forms.PictureBox();
            btn_GoToStartPos = new System.Windows.Forms.Button();
            btn_ResetMagazin = new System.Windows.Forms.Button();
            btn_StartCalibration = new System.Windows.Forms.Button();
            btnStartTof = new System.Windows.Forms.Button();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            verbindungenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tOFKameraVerbindenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dKameraVerbindenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            roboterVerbindenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            btnResetRoboter = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)pbBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbBox4).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_StartSearching
            // 
            btn_StartSearching.Location = new System.Drawing.Point(19, 109);
            btn_StartSearching.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btn_StartSearching.Name = "btn_StartSearching";
            btn_StartSearching.Size = new System.Drawing.Size(169, 36);
            btn_StartSearching.TabIndex = 3;
            btn_StartSearching.Text = "Objekte Erkennen";
            btn_StartSearching.UseVisualStyleBackColor = true;
            btn_StartSearching.Click += StartObjectDetection_Click;
            // 
            // btn_StartSorting
            // 
            btn_StartSorting.Location = new System.Drawing.Point(19, 197);
            btn_StartSorting.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btn_StartSorting.Name = "btn_StartSorting";
            btn_StartSorting.Size = new System.Drawing.Size(169, 36);
            btn_StartSorting.TabIndex = 0;
            btn_StartSorting.Text = "Start";
            btn_StartSorting.UseVisualStyleBackColor = true;
            btn_StartSorting.Click += StartSorting_Click;
            // 
            // pbBox3
            // 
            pbBox3.Location = new System.Drawing.Point(199, 109);
            pbBox3.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            pbBox3.Name = "pbBox3";
            pbBox3.Size = new System.Drawing.Size(640, 507);
            pbBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pbBox3.TabIndex = 6;
            pbBox3.TabStop = false;
            // 
            // pbBox4
            // 
            pbBox4.Location = new System.Drawing.Point(848, 109);
            pbBox4.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            pbBox4.Name = "pbBox4";
            pbBox4.Size = new System.Drawing.Size(647, 503);
            pbBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pbBox4.TabIndex = 7;
            pbBox4.TabStop = false;
            // 
            // btn_GoToStartPos
            // 
            btn_GoToStartPos.Location = new System.Drawing.Point(21, 373);
            btn_GoToStartPos.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btn_GoToStartPos.Name = "btn_GoToStartPos";
            btn_GoToStartPos.Size = new System.Drawing.Size(169, 36);
            btn_GoToStartPos.TabIndex = 8;
            btn_GoToStartPos.Text = "Move start pos";
            btn_GoToStartPos.UseVisualStyleBackColor = true;
            btn_GoToStartPos.Click += GoToStartPos_Click;
            // 
            // btn_ResetMagazin
            // 
            btn_ResetMagazin.Location = new System.Drawing.Point(21, 424);
            btn_ResetMagazin.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btn_ResetMagazin.Name = "btn_ResetMagazin";
            btn_ResetMagazin.Size = new System.Drawing.Size(169, 36);
            btn_ResetMagazin.TabIndex = 9;
            btn_ResetMagazin.Text = "Magazin Leer";
            btn_ResetMagazin.UseVisualStyleBackColor = true;
            btn_ResetMagazin.Click += ResetMagazin_Click;
            // 
            // btn_StartCalibration
            // 
            btn_StartCalibration.Location = new System.Drawing.Point(19, 505);
            btn_StartCalibration.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btn_StartCalibration.Name = "btn_StartCalibration";
            btn_StartCalibration.Size = new System.Drawing.Size(170, 36);
            btn_StartCalibration.TabIndex = 10;
            btn_StartCalibration.Text = "Kamera Kalibrieren";
            btn_StartCalibration.UseVisualStyleBackColor = true;
            btn_StartCalibration.Click += StartKalibration_Click;
            // 
            // btnStartTof
            // 
            btnStartTof.Location = new System.Drawing.Point(21, 153);
            btnStartTof.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnStartTof.Name = "btnStartTof";
            btnStartTof.Size = new System.Drawing.Size(169, 36);
            btnStartTof.TabIndex = 14;
            btnStartTof.Text = "TOF-Kamera starten";
            btnStartTof.UseVisualStyleBackColor = true;
            btnStartTof.Visible = false;
            btnStartTof.Click += Start_TOF_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = System.Drawing.Color.Tomato;
            menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { verbindungenToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            menuStrip1.Size = new System.Drawing.Size(2111, 30);
            menuStrip1.TabIndex = 16;
            menuStrip1.Text = "menuStrip1";
            // 
            // verbindungenToolStripMenuItem
            // 
            verbindungenToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tOFKameraVerbindenToolStripMenuItem, dKameraVerbindenToolStripMenuItem, roboterVerbindenToolStripMenuItem });
            verbindungenToolStripMenuItem.Name = "verbindungenToolStripMenuItem";
            verbindungenToolStripMenuItem.Size = new System.Drawing.Size(115, 24);
            verbindungenToolStripMenuItem.Text = "Verbindungen";
            // 
            // tOFKameraVerbindenToolStripMenuItem
            // 
            tOFKameraVerbindenToolStripMenuItem.Name = "tOFKameraVerbindenToolStripMenuItem";
            tOFKameraVerbindenToolStripMenuItem.Size = new System.Drawing.Size(244, 26);
            tOFKameraVerbindenToolStripMenuItem.Text = "TOF-Kamera verbinden";
            tOFKameraVerbindenToolStripMenuItem.Click += tOFKameraVerbindenToolStripMenuItem_Click;
            // 
            // dKameraVerbindenToolStripMenuItem
            // 
            dKameraVerbindenToolStripMenuItem.Name = "dKameraVerbindenToolStripMenuItem";
            dKameraVerbindenToolStripMenuItem.Size = new System.Drawing.Size(244, 26);
            dKameraVerbindenToolStripMenuItem.Text = "2D-Kamera verbinden";
            dKameraVerbindenToolStripMenuItem.Click += dKameraVerbindenToolStripMenuItem_Click;
            // 
            // roboterVerbindenToolStripMenuItem
            // 
            roboterVerbindenToolStripMenuItem.Name = "roboterVerbindenToolStripMenuItem";
            roboterVerbindenToolStripMenuItem.Size = new System.Drawing.Size(244, 26);
            roboterVerbindenToolStripMenuItem.Text = "Roboter verbinden";
            roboterVerbindenToolStripMenuItem.Click += roboterVerbindenToolStripMenuItem_Click;
            // 
            // btnResetRoboter
            // 
            btnResetRoboter.Location = new System.Drawing.Point(21, 329);
            btnResetRoboter.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnResetRoboter.Name = "btnResetRoboter";
            btnResetRoboter.Size = new System.Drawing.Size(169, 36);
            btnResetRoboter.TabIndex = 18;
            btnResetRoboter.Text = "Reset Roboter";
            btnResetRoboter.UseVisualStyleBackColor = true;
            btnResetRoboter.Click += btnResetRoboter_Click;
            // 
            // MainView
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(2111, 632);
            Controls.Add(btnResetRoboter);
            Controls.Add(btnStartTof);
            Controls.Add(btn_StartCalibration);
            Controls.Add(btn_ResetMagazin);
            Controls.Add(btn_GoToStartPos);
            Controls.Add(pbBox4);
            Controls.Add(pbBox3);
            Controls.Add(btn_StartSearching);
            Controls.Add(btn_StartSorting);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            Name = "MainView";
            Text = "Objekterkennung";
            ((System.ComponentModel.ISupportInitialize)pbBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbBox4).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btn_StartSearching;
        private System.Windows.Forms.Button btn_StartSorting;
        private System.Windows.Forms.PictureBox pbBox3;
        private System.Windows.Forms.PictureBox pbBox4;
        private System.Windows.Forms.Button btn_GoToStartPos;
        private System.Windows.Forms.Button btn_ResetMagazin;
        private System.Windows.Forms.Button btn_StartCalibration;
        private System.Windows.Forms.Button btn_ConnectToServer;
        private System.Windows.Forms.Button btn_ConnectToCamera;
        private System.Windows.Forms.Button btnStartTof;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem verbindungenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tOFKameraVerbindenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dKameraVerbindenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem roboterVerbindenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem parameterToolStripMenuItem;
        private System.Windows.Forms.Button btnResetRoboter;
    }
}

