using Microsoft.VisualBasic;
using Objekterkennung._2_Controller;

using System;
using System.Drawing;
using System.Security.AccessControl;
using System.Windows.Forms;

namespace Objekterkennung
{
    public partial class MainView : Form
    {
        //----------Attribute----------//
        protected Controller derController;
        //----------Konstruktor----------//
        #region Konstruktor
        public MainView()
        {
            InitializeComponent();
            this.derController = new Controller(this);
            btn_StartSearching.Visible = false;
            btn_StartSorting.Visible = false;
            btn_GoToStartPos.Visible = false;
            btn_ResetMagazin.Visible = false;
            btn_StartCalibration.Visible = false;
            btnResetRoboter.Visible = false;
            btn_StartCalibration.BackColor = Color.OrangeRed;
            if (derController.CheckCalibration())
            {
                btn_StartCalibration.BackColor = Color.LightGreen;

            }

        }
        #endregion
        //----------Get-Set----------//
        #region Get-Set
        public Controller GetController()
        {
            return this.derController;
        }
        public Form GetForm()
        {
            return this;
        }
        #endregion
        //----------Methoden----------//
        #region Methoden
        private void StartSorting_Click(object sender, EventArgs e)
        {
            try
            {
                this.derController.StartSorting();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StartObjectDetection_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.derController.CheckCalibration())
                {
                    this.derController.StartObjectDetection();
                }
                else
                {
                    MessageBox.Show("Bitte Kalibrieren Sie die Kamera");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }


        public void SetDisplayImageCanny(Bitmap pBitmap)
        {
            try
            {
                pbBox3.Image = pBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        public void SetDisplayImage(Bitmap pBitmap)
        {
            try
            {
                pbBox4.Image = pBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void GoToStartPos_Click(object sender, EventArgs e)
        {
            try
            {
                this.derController.SetRoboterStartPos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void GoToParkPosition(object sender, EventArgs e) //neu von Luis & Adrian
        {
            try
            {
                this.derController.setRoboterParkPos(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ResetMagazin_Click(object sender, EventArgs e)
        {
            try
            {
                this.derController.ResetMagazin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void SetButtonColor()
        {
            btn_StartCalibration.BackColor = Color.LightGreen;
        }
        private void StartKalibration_Click(object sender, EventArgs e)
        {
            if (this.derController.CheckCalibration())
            {

                DialogResult result = MessageBox.Show("Kamera bereits Kalibriert, wollen Sie die Kamera erneut Kalibrieren?", "Kamera Kalibrieren", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        CalibrationWindow calibrationWindow = new CalibrationWindow(this);
                        calibrationWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    CalibrationWindow calibrationWindow = new CalibrationWindow(this);
                    calibrationWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ConnectToServer_Click(object sender, EventArgs e)
        {
            try
            {
                bool connected = this.derController.SetRoboter();
                if (connected)
                {
                    btn_GoToStartPos.Visible = true;
                    btn_ResetMagazin.Visible = true;
                    btn_ConnectToServer.BackColor = Color.LightGreen;
                    btn_ConnectToServer.Text = "Mit Server Verbunden";
                    btn_ConnectToServer.Enabled = false;
                    MessageBox.Show("Erfolgreich mit dem Server verbunden");
                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ConnectToCamera_Click(object sender, EventArgs e)
        {
            try
            {
                bool connected = this.derController.SetCamera();
                if (connected)
                {
                    MessageBox.Show("Erfolgreich mit der Kamera verbunden");
                    btn_StartSearching.Visible = true;
                    btn_StartCalibration.Visible = true;
                    btn_StartSorting.Visible = true;
                    btn_ConnectToCamera.BackColor = Color.LightGreen;
                    btn_ConnectToCamera.Text = "Mit Kamera Verbunden";
                    btn_ConnectToCamera.Enabled = false;
                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private async void Start_TOF_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                await this.derController.StartTOFCamera();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        public void SetMessage(string message)
        {
            MessageBox.Show(message);
        }

        #endregion



        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                
                bool connected = this.derController.SetTofKamera(1);
                if (connected)
                {

                    tOFKameraVerbindenToolStripMenuItem.Checked = true;
                    btnStartTof.Visible = true;

                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void tOFKameraVerbindenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string input = Interaction.InputBox("Mit welcher Kamera möchten Sie sich verbinden?\n1=SICK  2=Schmersal");
                int inputInt = Convert.ToInt32(input);
                if (inputInt != 1 && inputInt != 2)
                {
                    MessageBox.Show("Bitte geben Sie 1 ein! (Schmersal nicht mehr unterstützt)");
                    return;
                }
                bool connected = this.derController.SetTofKamera(inputInt);
                if (connected)
                {
                    MessageBox.Show("Erfolgreich mit der Kamera verbunden");
                    tOFKameraVerbindenToolStripMenuItem.Checked = true;
                    btnStartTof.Visible = true;

                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void dKameraVerbindenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                bool connected = this.derController.SetCamera();
                if (connected)
                {
                    MessageBox.Show("Erfolgreich mit der Kamera verbunden");
                    btn_StartSearching.Visible = true;
                    btn_StartCalibration.Visible = true;
                    btn_StartSorting.Visible = true;
                    dKameraVerbindenToolStripMenuItem.Checked = true;
                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void roboterVerbindenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                bool connected = this.derController.SetRoboter();
                if (connected)
                {
                    btn_GoToStartPos.Visible = true;
                    btn_ResetMagazin.Visible = true;
                    btnResetRoboter.Visible = true;
                    roboterVerbindenToolStripMenuItem.Checked = true;
                    MessageBox.Show("Erfolgreich mit dem Server verbunden");
                }
                else
                {
                    MessageBox.Show("Fehler beim Verbinden");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                await this.derController.temp();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void btnResetRoboter_Click(object sender, EventArgs e)
        {
            try
            {
                this.derController.ResetRobi();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
    }
}
