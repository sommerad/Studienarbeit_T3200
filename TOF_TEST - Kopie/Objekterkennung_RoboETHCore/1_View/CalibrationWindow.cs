using Emgu.CV;
using NPOI.SS.Formula.Functions;
using Objekterkennung;
using Objekterkennung._2_Controller;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Objekterkennung       
{
    //Ist wegen der Live-Funktion nicht ganz nach dem MVC-Modell haben keine andere Möglichkeit gefunden
    public partial class CalibrationWindow : Form
    {
        //-----------Attribute-----------//
       
        protected uEye.Camera aCamera;
        protected IntPtr aDisplayHandle = IntPtr.Zero;
        protected Controller aController;
        protected Form aForm1;

        //-----------Konstruktor-----------//
        #region Konstruktor
        public CalibrationWindow(MainView pForm1)
        {
            InitializeComponent();
            this.aForm1 = pForm1;
            this.aController = pForm1.GetController();
            this.aController.SetDieViewKalibrierung(this);
            this.aDisplayHandle = DisplayWindow.Handle;
            this.aCamera =aController.GetCamera();
            btn_SetHomography.Visible = false;
            this.aCamera.EventFrame += onFrameEvent;
            this.aCamera.Acquisition.Capture();

            if(this.aController.CheckCalibration()) 
            {
                btn_SetHomography.Visible = true;
            }
        }

        #endregion

        //-----------Methoden-----------//
        #region Methoden
        private void onFrameEvent(object sender, EventArgs e)
        {
            uEye.Camera camera = sender as uEye.Camera;

            camera.Display.Render(aDisplayHandle, uEye.Defines.DisplayRenderMode.FitToWindow);
        }


        private void Quit_Click(object sender, EventArgs e)
        {
            if(!this.aController.GetSavedStatus())
            {
                DialogResult result = MessageBox.Show("Möchten Sie die Kalibrierung speichern?", "Kalibrierung speichern", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    this.aController.SaveCalibration();
                    Close();
                }
                else if (result == DialogResult.No)
                {
                    this.aController.ClearCalibrationImages();
                    Close();
                }
            }
            else
            {
                Close();
            }
            this.aController.ClearCalibrationImages();
            Close();
        }

        private void TakePicture_Click(object sender, EventArgs e)
        {
            
            try
            {
               int anzahl = this.aController.TakePhotoCalibration();
               label1.Text = "Anzahl der Kalibrierungsbilder: " + anzahl;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
       
        private void StartKalibration_Click(object sender, EventArgs e)
        {
            if (this.aController.GetCalibrationImages().Count < 10)
            {
                MessageBox.Show("Es wurden erst "+this.aController.GetCalibrationImages().Count.ToString()+" Bilder aufgenommen");
            }
            else
            {
                this.aController.StartCalibration();
                DialogResult result = MessageBox.Show("Kalibrierung abgeschlossen. Bitte legen Sie das Schachbrett flach auf den Arbeitsbereich um die Ebene zu definieren und klicken Sie auf 'OK'.", "Homography", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    this.aController.StartHomography();
                    MessageBox.Show("Homographie abgeschlossen");
                    btn_SetHomography.Visible = true;
                }
            }
        }
       
        private void SaveMat_Click(object sender, EventArgs e)
        {
            this.aController.SaveCalibration();
            MessageBox.Show("Kalibrierung gespeichert");
        }

        private void SetHomography_Click(object sender, EventArgs e)
        {
            
            if((this.aController.GetHomography()==null)|| this.aController.GetHomography().IsEmpty)
            {
                DialogResult result = MessageBox.Show("Platzieren Sie das Schachbrett auf den Arbeitsbereich und klicken Sie auf 'Ok'", "Ebene Definieren", MessageBoxButtons.OKCancel);
                if(result == DialogResult.OK) 
                {
                    this.aController.StartHomography();
                }
            }
            else
            {
                DialogResult result = MessageBox.Show("Die Ebene wurde bereits gesetzt, wollen Sie es erneut setzten? ", "Ebene Definieren", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    MessageBox.Show("Platzieren Sie das Schachbrett auf den Arbeitsbereich und klicken Sie auf 'Ok'");
                    this.aController.StartHomography();
                }
            }
        }

        #endregion

    }
}