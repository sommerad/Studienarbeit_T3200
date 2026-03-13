using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objekterkennung_RoboETH._3_Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace Objekterkennung._3_Model
{
    public class Kalibrierung
    {
        //-----Attribute-----//
        #region Attribute
        protected List<Mat> aChessboardImages = new List<Mat>(10); // Liste zum Speichern der Schachbrettbilder
        protected int aZähler = 0;
        protected Size aPatternSize;  // Anzahl der inneren Ecken des Schachbretts (z.B. 9x6)
        protected float aSquareSize=20;  // Größe eines Quadrats auf dem Schachbrett in Millimetern
        protected int aChessboardWidth = 17;       
        protected int aChessboardHeight = 12;     
        protected bool aIsCalibrated = false;
        protected bool aIsSaved = false;
        protected Mat aCameraMatrix = new Mat(3, 3, DepthType.Cv64F, 1);
        protected Mat aDistCoeffs = new Mat(1, 5, DepthType.Cv64F, 1); // Verzerrungskoeffizienten
        protected Mat[] aRotationVecs, aTranslationVecs=null;
        protected Mat aHomography = new Mat();
        #endregion
        //-----Konstruktor-----//
        #region Konstruktor
        public Kalibrierung()
        {
            this.aPatternSize = new Size(aChessboardWidth, aChessboardHeight); // Anzahl der Ecken auf dem Schachbrett
            for (int i = 0; i < 10; i++)
            {
                this.aChessboardImages.Add(new Mat());
            }
           this.aIsCalibrated= this.CheckKalibration();
        }
        ~Kalibrierung()
        {
            this.aChessboardImages.Clear();
        }
        #endregion
        //-----Get-Set-----//
        #region Get-Set
       
        public void SetHomography(Mat pHomography)
        {
            this.aHomography = pHomography;
        }
        public bool GetSavedStatus() {return this.aIsSaved; }
        public Mat GetCameraMatrix() {return this.aCameraMatrix; }
        public Mat GetDistCoeffs() { return this.aDistCoeffs; }
        public Mat GetHomography() { return this.aHomography; }
        public Mat[] GetRotationVecs() { return this.aRotationVecs; }
        public Mat[] GetTranslationVecs() {return this.aTranslationVecs; }
        public bool GetIsCalibrated() { return this.aIsCalibrated; }

        public void AddChessboardImage(Mat image,int pZähler)
        {
            this.aChessboardImages[pZähler] = image;
        }
        #endregion
        //-----Methoden-----//
        #region Methoden
        public void ClearChessboardImages()
        {
            for (int i = 0; i < this.aChessboardImages.Count; i++)
            {
                this.aChessboardImages[i].Dispose();
            }

        }
        public int CountChessboardImages()
        {
            return this.aChessboardImages.Count;
        }
        public bool CheckKalibration()
        {
            try
            {
                bool isCalibrated = LoadCalibrationData(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                                                "calibrationData.json"),
                                                                                out this.aCameraMatrix,
                                                                                out this.aDistCoeffs,
                                                                                out this.aRotationVecs,
                                                                                out this.aTranslationVecs,
                                                                                out this.aHomography);
                if (isCalibrated)
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message +"\nFehler von:\n" +e.Source);
                return false;
            }


        }
        
        public void CalibrateCamera()
        {
            //------Variabeln------//
            List<MCvPoint3D32f[]> objectPointsList = new List<MCvPoint3D32f[]>();
            List<PointF[]> imagePointsList = new List<PointF[]>();
            MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[this.aPatternSize.Width * this.aPatternSize.Height];
           
            if (this.aChessboardImages.Count < 10)
            {
                throw new Exception("Es wurden nur" + this.aChessboardImages.Count + " Bilder aufgenommen");
            }

            //-----Objektpunkte sammeln-----//
            for (int i = 0; i < this.aPatternSize.Height; i++)
            {
                for (int j = 0; j < this.aPatternSize.Width; j++)
                {
                    objectPoints[i * this.aPatternSize.Width + j] = new MCvPoint3D32f(j * this.aSquareSize, i * this.aSquareSize, 0);
                }
            }

            //------Objektpunkte in den Bildern finden-----//
            for (int i = 0; i < this.aChessboardImages.Count; i++)
            {
               
                Mat grayImage = new Mat();
                CvInvoke.CvtColor(this.aChessboardImages[i], grayImage, ColorConversion.Bgr2Gray);

                //-----Schachbrettmuster finden-----//
                VectorOfPointF corners = new VectorOfPointF();
                bool found = CvInvoke.FindChessboardCorners(grayImage, this.aPatternSize, corners, CalibCbType.AdaptiveThresh | CalibCbType.NormalizeImage);

                if (found)
                {
                    //----Kanten verbessern-----//
                    CvInvoke.CornerSubPix(grayImage, corners, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));

                    //----Bildpunkte speichern-----//
                    imagePointsList.Add(corners.ToArray());
                    objectPointsList.Add(objectPoints);
                }
            }

            //----Kamera kalibrieren-----//
            double reprojectionError = CvInvoke.CalibrateCamera(objectPointsList.ToArray(), 
                                                                imagePointsList.ToArray(), 
                                                                this.aChessboardImages[0].Size, 
                                                                this.aCameraMatrix, 
                                                                this.aDistCoeffs, 
                                                                CalibType.Default, 
                                                                new MCvTermCriteria(30, 0.1), 
                                                                out this.aRotationVecs, 
                                                                out this.aTranslationVecs);
            
            this.aIsCalibrated = true;
            this.aIsSaved = false;
            this.ClearChessboardImages();
            return;
        }
        
        public void SaveCalibrationData(Mat pHomography, Mat pCameraMatrix, Mat pDistCoeffs, Mat[] pRotationVec, Mat[] pTranslationVec, string pFilePath)
        {
            if ((pHomography == null)||(pCameraMatrix==null)||(pDistCoeffs==null)||(pRotationVec==null)||(pTranslationVec==null))
            {
                throw new Exception("Konnte nicht gespeichert werden, da die Matritzen nicht gefüllt sind");
            }
            //-----Matritzen in Arrays umwandeln-----//
            #region MatToArray
            double[,] cameraMatrixArray = new double[pCameraMatrix.Rows, pCameraMatrix.Cols];
            for (int i = 0; i < pCameraMatrix.Rows; i++)
            {
                for (int j = 0; j < pCameraMatrix.Cols; j++)
                {
                    cameraMatrixArray[i, j] = MatExtension.GetValue(pCameraMatrix, i, j);

                }
            }

            double[,] distCoeffsArray = new double[pDistCoeffs.Rows, pDistCoeffs.Cols];
            for (int i = 0; i < pDistCoeffs.Rows; i++)
            {
                for (int j = 0; j < pDistCoeffs.Cols; j++)
                {
                    distCoeffsArray[i, j] = MatExtension.GetValue(pDistCoeffs, i, j);  
                }
            }
            double[,] homographyArray = new double[pHomography.Rows, pHomography.Cols];
            for (int i = 0; i < pHomography.Rows; i++)
            {
                for (int j = 0; j < pHomography.Cols; j++)
                {
                    homographyArray[i, j] = MatExtension.GetValue(pHomography, i, j);
                }
            }

            var rvecsArray = pRotationVec.Select(r => 
            {
                r.ConvertTo(r, DepthType.Cv64F); 
                double[,] rvecArray = new double[r.Rows, r.Cols];
                for (int i = 0; i < r.Rows; i++)
                {
                    for (int j = 0; j < r.Cols; j++)
                    {
                        rvecArray[i, j] = r.GetValue(i, j);
                    }
                }
                return rvecArray;
            }).ToArray();

            var tvecsArray = pTranslationVec.Select(t => 
            {
                t.ConvertTo(t, DepthType.Cv64F); 
                double[,] tvecArray = new double[t.Rows, t.Cols];
                for (int i = 0; i < t.Rows; i++)
                {
                    for (int j = 0; j < t.Cols; j++)
                    {
                        tvecArray[i, j] = t.GetValue(i, j);
                    }
                }
                return tvecArray;
            }).ToArray();
            #endregion

            //-----Daten in JSON umwandeln-----//
            var calibrationData = new
            {
                CameraMatrix = cameraMatrixArray,
                DistCoeffs = distCoeffsArray,
                Homography= homographyArray,
                Rvecs = rvecsArray,
                Tvecs = tvecsArray
            };
            //-----Daten speichern-----//
            var json = JsonConvert.SerializeObject(calibrationData, Formatting.Indented);
            File.WriteAllText(pFilePath, json);
            this.aIsSaved = true;
        }

        public bool LoadCalibrationData(string pFilePath, out Mat pCameraMatrix, out Mat pDistCoeffs, out Mat[] pRotationVec, out Mat[] pTranslationVec, out Mat pHomography)
        {
            
            if (!File.Exists(pFilePath))
            {
                
                pCameraMatrix = new Mat(3, 3, DepthType.Cv64F, 1);;
                pDistCoeffs = new Mat(1, 5, DepthType.Cv64F, 1);;
                pRotationVec = null;
                pTranslationVec = null;
                pHomography = null;
                return false;
            }
            //-----Daten laden-----//
            var json = File.ReadAllText(pFilePath);
            var calibrationData = JsonConvert.DeserializeObject<dynamic>(json);

            //-----Daten in Matritzen umwandeln-----//
            #region ArrayToMat

            double[,] cameraMatrixArray = calibrationData.CameraMatrix.ToObject<double[,]>();
            pCameraMatrix = new Mat(cameraMatrixArray.GetLength(0), cameraMatrixArray.GetLength(1), DepthType.Cv64F, 1);
            for (int i = 0; i < pCameraMatrix.Rows; i++)
            {
                for (int j = 0; j < pCameraMatrix.Cols; j++)
                {
                    pCameraMatrix.SetValue(i, j, cameraMatrixArray[i, j]);
                }
            }

            double[,] distCoeffsArray = calibrationData.DistCoeffs.ToObject<double[,]>();
            pDistCoeffs = new Mat(distCoeffsArray.GetLength(0), distCoeffsArray.GetLength(1), DepthType.Cv64F, 1);
            for (int i = 0; i < pDistCoeffs.Rows; i++)
            {
                for (int j = 0; j < pDistCoeffs.Cols; j++)
                {
                    pDistCoeffs.SetValue(i, j, distCoeffsArray[i, j]);
                }
            }
            double[,] homographyArray = calibrationData.Homography.ToObject<double[,]>();
            pHomography = new Mat(homographyArray.GetLength(0), homographyArray.GetLength(1), DepthType.Cv64F, 1);
            for (int i = 0; i < pHomography.Rows; i++)
            {
                for (int j = 0; j < pHomography.Cols; j++)
                {
                    pHomography.SetValue(i, j, homographyArray[i, j]);
                }
            }

            JArray rvecsList = calibrationData.Rvecs;
            pRotationVec = new Mat[rvecsList.Count];
            for (int index = 0; index < rvecsList.Count; index++)
            {
                JArray rvecArray = (JArray)rvecsList[index];
                double[,] rvecArray2D = new double[rvecArray.Count, ((JArray)rvecArray[0]).Count];

                for (int i = 0; i < rvecArray.Count; i++)
                {
                    JArray row = (JArray)rvecArray[i];
                    for (int j = 0; j < row.Count; j++)
                    {
                        rvecArray2D[i, j] = row[j].ToObject<double>();
                    }
                }

                pRotationVec[index] = new Mat(rvecArray2D.GetLength(0), rvecArray2D.GetLength(1), DepthType.Cv64F, 1);
                for (int i = 0; i < rvecArray2D.GetLength(0); i++)
                {
                    for (int j = 0; j < rvecArray2D.GetLength(1); j++)
                    {
                        pRotationVec[index].SetValue(i, j, rvecArray2D[i, j]);
                    }
                }
            }

            JArray tvecsList = calibrationData.Tvecs;
            pTranslationVec = new Mat[tvecsList.Count];
            for (int index = 0; index < tvecsList.Count; index++)
            {
                JArray tvecArray = (JArray)tvecsList[index];
                double[,] tvecArray2D = new double[tvecArray.Count, ((JArray)tvecArray[0]).Count];

                for (int i = 0; i < tvecArray.Count; i++)
                {
                    JArray row = (JArray)tvecArray[i];
                    for (int j = 0; j < row.Count; j++)
                    {
                        tvecArray2D[i, j] = row[j].ToObject<double>();
                    }
                }

                pTranslationVec[index] = new Mat(tvecArray2D.GetLength(0), tvecArray2D.GetLength(1), DepthType.Cv64F, 1);
                for (int i = 0; i < tvecArray2D.GetLength(0); i++)
                {
                    for (int j = 0; j < tvecArray2D.GetLength(1); j++)
                    {
                        pTranslationVec[index].SetValue(i, j, tvecArray2D[i, j]);
                    }
                }
            }
            return true;
            #endregion
        }

        #endregion
    }

}

