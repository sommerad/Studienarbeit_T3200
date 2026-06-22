using Emgu.CV;
using NPOI.SS.Formula.Functions;
using Objekterkennung._3_Model;
using Objekterkennung_RoboETH._3_Model;
using Objekterkennung_RoboETHCore._3_Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;


namespace Objekterkennung._2_Controller
{
    public class Controller
    {
        //----------Attribute----------//
        protected MainView dieView;
        protected Bildverarbeitung aBild;
        protected CalibrationWindow dieViewKalibrierung;
        protected Roboter aRoboter;
        protected Homographie aKalibrierung;
        protected List<Bitmap> aKalibrierBilder = new List<Bitmap>();
        //protected TofKamera_Schmersal aTofKamera_Schmersal;
        protected int aAuswahlKamera = 0;//0=Nichts, 1=TOF-Kamera SICK
        protected PointCloudProcessing aPointCloudProcessing;
        protected TofKamera_SICK aTofKamera_SICK;

        //----------Konstruktor----------//
        #region Konstruktor
        public Controller(MainView pMainView)
        {
            this.dieView = pMainView;
            this.aKalibrierung = new Homographie();
            string modelPath = @"C:\Users\sommerad\Desktop\TOF_TEST - Kopie\Objekterkennung_RoboETHCore\2_Controller\Hand_haar_cascade.xml";
            this.aBild = new Bildverarbeitung();
            this.aBild.LoadHandModel(modelPath);
        }
        ~Controller()
        {
            if (this.aBild != null)
            {
                this.aBild = null;
            }
            if (this.aRoboter != null)
            {
                this.aRoboter.CloseSocket();
                this.aRoboter = null;
            }
            if (this.aKalibrierung != null)
            {
                this.aKalibrierung = null;
            }
            this.aKalibrierBilder.Clear();
            this.aKalibrierBilder = null;
            if (this.aTofKamera_SICK != null)
            {
                this.aTofKamera_SICK = null;
            }
            if (this.aPointCloudProcessing != null)
            {
                this.aPointCloudProcessing = null;
            }
            this.dieView = null;
            this.dieViewKalibrierung = null;
        }
        #endregion
        //----------Methoden GET/SET----------//
        #region Methoden GET/SET
        public void SetDieViewKalibrierung(CalibrationWindow pView)
        {
            this.dieViewKalibrierung = pView;
        }
        public bool SetRoboter()
        {
            if (this.aRoboter == null)
            {
                this.aRoboter = new Roboter();
                this.aBild.OnHandDetectedAsync += async () =>
                {
                    if (this.aRoboter?.Pipeline != null)
                    {
                        // Ruft den kooperativen Abbruch auf (löscht Puffer und sendet "RS")
                        this.aRoboter.Pipeline.EmergencyStopAndClear();
                    }
                    await Task.CompletedTask;
                };
                return true;
            }
            else if (!this.aRoboter.GetConnectionStatus())
            {
                this.aRoboter.ConnectToServer();
                return true;
            }
            else
            {
                throw new Exception("Server bereits verbunden");
            }
        }
        public bool SetCamera()
        {
            if (this.aBild == null)
            {
                this.aBild = new Bildverarbeitung();
                return true;
            }
            else if (!this.aBild.GetConnectionStatus())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool SetTofKamera(int kamera)
        {
            if (kamera == 1)
            {
                this.aAuswahlKamera = 1;
                //this.aTofKamera_Schmersal = null;
                this.aTofKamera_SICK = new TofKamera_SICK();
                this.aPointCloudProcessing = new PointCloudProcessing(aAuswahlKamera);
                return true;
            }
            else
            {
                throw new Exception("Falsche Kamera ausgewählt");
            }
        }


        public Mat GetHomography()
        {
            return this.aKalibrierung.GetHomography();
        }
        public bool GetSavedStatus()
        {
            return this.aKalibrierung.GetSavedStatus();
        }
        public void SetRoboterStartPos()
        {
            this.aRoboter.GoToStartPos();
        }
     
        public List<Bitmap> GetCalibrationImages()
        {
            return this.aKalibrierBilder;
        }
        public uEye.Camera GetCamera()
        {
            if (this.aBild == null)
            {
                throw new Exception("Kamera wurde noch nicht initialisiert");
            }
            return this.aBild.GetCamera();
        }

        public Roboter GetRoboter()
        {
            //Achtung: Passe "derRoboter" an den tatsächlichen Namen deiner Variable an!
            return this.aRoboter;
        }

        #endregion
        //----------Methoden----------//
        #region Methoden

        public async Task StartTOFCamera()
        {
            if (aBild == null)
            {
                throw new Exception("2D-Kamera ist nicht verbunden");
            }

            List<Point2D> midpoints = this.aBild.GetMidPoints();



            if (midpoints.Count < 0)
            {
                this.dieView.SetMessage("Keine Objekte gefunden");
                return;
            }
            else
            {

                if (this.aAuswahlKamera == 1)   //Sick Kamera startet die Aufnahme
                {

                    midpoints = this.aTofKamera_SICK.ChangeKorrSystemTof(midpoints);
                    await Task.Run(() => aTofKamera_SICK.CaptureFrame());

                }
                else
                {
                    throw new Exception("Keine Kamera ausgewählt");
                }

                //-----Punktwolkenverarbeitung-----//
                List<ObjectPose> processedPoints = await Task.Run(() => this.aPointCloudProcessing.ProcessPoints(midpoints));
                //-----Ergebnis in Poses Umwandeln-----//
                List<PoseAttributes> poseAttributesList = this.aPointCloudProcessing.ConvertToPoseAttributes(processedPoints);

                //-----Ergebnis speichern-----//
                this.aPointCloudProcessing.SetObjectPose(poseAttributesList);
            }


        }

        public void ClearCalibrationImages()
        {
            this.aKalibrierBilder.Clear();

        }
        public void StartSorting()
        {
            if (this.aRoboter == null)
            {
                throw new Exception("Roboter noch nicht verbunden");
            }
            //-----Objekt Posen holen-----//
            List<PoseAttributes> objektPoses = this.aPointCloudProcessing.GetObjektPoses();
            if (objektPoses.Count == 0)
            {
                throw new Exception("Keine 3D-Objekte gefunden");
            }
            //-----Objekt Posen in Roboter Koordinaten umwandeln-----//
            List<PoseAttributes> newMidPoints = this.aRoboter.ChangeKoordinations(objektPoses);

            //-----Objekt Posen an Roboter senden-----//
            //this.aRoboter.SendKoordinations(newMidPoints);

            //-----Objektposen per Pipeline an Roboter senden---- NEU Adrian Sommer 23.03.2026//
            this.aRoboter.SendCoordinatesViaPipeline(newMidPoints);
        }
        public void ResetRobi()
        {
            if (this.aRoboter == null)
            {
                throw new Exception("Roboter noch nicht verbunden");
            }

            this.aRoboter.SendData("RS");
        }
        public void SaveCalibration()
        {
            this.aKalibrierung.SaveCalibrationData(
                this.aKalibrierung.GetHomography(),
                this.aKalibrierung.GetCameraMatrix(),
                this.aKalibrierung.GetDistCoeffs(),
                this.aKalibrierung.GetRotationVecs(),
                this.aKalibrierung.GetTranslationVecs(),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calibrationData.json"));
        }
        public bool CheckCalibration()
        {
            if (this.aKalibrierung.GetIsCalibrated())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StartObjectDetection()
        {
            //-----Variablen-----//

            Mat imgMat = this.aBild.TakePicture();
            Mat imgMat2 = imgMat.Clone();

            //-----Bildverarbeitung-----//
            imgMat = this.aBild.UndistortPicture(imgMat, this.aKalibrierung.GetCameraMatrix(), this.aKalibrierung.GetDistCoeffs());
            imgMat = this.aBild.ApplyHomography(imgMat, this.aKalibrierung.GetHomography());
            imgMat = this.aBild.ImageProcessing(imgMat);
            //imgMatline = this.aBild.DetectLineGaps(imgMat);

            bool handFound = false;
            Mat handImg = this.aBild.DetectHands(imgMat2, out handFound);
            if (handFound == true) {
                System.Diagnostics.Debug.WriteLine("Hand erkannt!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Keine Hand erkannt!");
            }


                //-----Ergebnis anzeigen-----//
            this.dieView.SetDisplayImage(BitmapExtension.ToBitmap(imgMat));
            this.dieView.SetDisplayImageCanny(aBild.GetCannyFilter());

            this.dieView.SetDisplayImage(BitmapExtension.ToBitmap(handImg));
            this.dieView.SetDisplayImageCanny(aBild.GetCannyFilter());
            imgMat.Dispose();
            handImg.Dispose();
            imgMat2.Dispose();
            ;
        }

        public void ResetMagazin()
        {
            this.aRoboter.ResetMagazin();
        }
        public int TakePhotoCalibration()
        {
            //------Fotos zwischen Speichern-----//
            if (this.aKalibrierBilder.Count < 10)
            {

                this.aKalibrierBilder.Add(BitmapExtension.ToBitmap(this.aBild.TakePicture()));
                this.aBild.LiveFeed();
                return this.aKalibrierBilder.Count;
            }
            else
            {
                throw new Exception("Es wurden bereits 10 Bilder aufgenommen");
            }
        }

        public void StartCalibration()
        {
            //-------Fotos in Kalibrierung einfügen un starten-------//
            if (this.aKalibrierBilder.Count() == 10)
            {

                for (int i = 0; i < this.aKalibrierBilder.Count; i++)
                {
                    Mat imgMat = new Mat();
                    BitmapExtension.ToMat(this.aKalibrierBilder[i], imgMat);
                    this.aKalibrierung.AddChessboardImage(imgMat, i);
                    this.aKalibrierBilder[i].Dispose();
                }
                this.aKalibrierung.CalibrateCamera();
                this.dieView.SetButtonColor();
            }
            else
            {
                throw new Exception("Es wurden nicht genügend Bilder aufgenommen");
            }

        }

        public void StartHomography()
        {
            Mat imgMat = this.aBild.TakePicture();
            Mat homography = new Mat();
            this.aKalibrierung.ComputeHomography(this.aBild.UndistortPicture(imgMat, this.aKalibrierung.GetCameraMatrix(), this.aKalibrierung.GetDistCoeffs()));

        }
        #endregion
        public async Task temp()
        {
           
            await Task.Delay(1);
        }
    }
}
