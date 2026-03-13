using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;
using Size = System.Drawing.Size;


namespace Objekterkennung._3_Model
{
    public class Bildverarbeitung
    {
        //-----Attribute-----//
        private uEye.Camera aCamera;
        private uEye.Types.SensorInfo aSensorInfo;

        protected Bitmap aBitmapBild;
        protected Mat aGrayFilter = new Mat();
        protected Mat aBlurFilter = new Mat();
        protected Mat aCannyFilter = new Mat();
       
        //-----Paramerter-----//
        private int aRefAreaSquare = 850;
        private int aRefAreaCircle = 900;
        private int aRefAreaZylinder = 1800;
        private int aRefAreaRectangle = 2500;
        private int aToleranzArea = 100;       //400
        private int aToleranzQuadrat = 300;     //200
        private int aToleranzAreaZylinder = 300;
        private int aToleranzAreaCircle = 350;
        private int aToleranzAreaRectangle = 300;
        private List<Point2D> aMidpoints=new List<Point2D>();

        //-----Konstruktor-----//
        #region Konstruktor
        public Bildverarbeitung()
        {
            //-----Kamera initialisieren-----//
            aCamera = new uEye.Camera();
            aCamera.Init();
            aCamera.Memory.Allocate();
            aCamera.Information.GetSensorInfo(out aSensorInfo);
            aCamera.Acquisition.Freeze();
            if(aCamera.IsOpened== false)
            {
               throw new Exception("Kamera konnte nicht verbunden werden werden");
            }
        }
      
        ~Bildverarbeitung()
        {
            this.aBitmapBild = null;
            this.aGrayFilter.Dispose();
            this.aBlurFilter.Dispose();
            this.aCannyFilter.Dispose();
            this.aMidpoints.Clear();
            this.aCamera.Exit();
        }
        #endregion
        //-----GET/SET-Methoden-----//
        #region GET/SET-Methoden
        public bool GetConnectionStatus()
        {
            return this.aCamera.IsOpened;
        }
        public uEye.Camera GetCamera()
        {
            return this.aCamera;
        }
        public List<Point2D> GetMidPoints()
        {
            return this.aMidpoints;
        }
        public Bitmap GetBlurFilter()
        {
            return BitmapExtension.ToBitmap(this.aBlurFilter);
        }
        public Bitmap GetGrayFilter()
        {
            return BitmapExtension.ToBitmap(this.aGrayFilter);
        }
        public Bitmap GetCannyFilter()
        {
            return BitmapExtension.ToBitmap(this.aCannyFilter);
        }
        public void LiveFeed()
        {
            this.aCamera.Acquisition.Capture();
        }

       
        public void LiveStream()
        {
            this.aCamera.Acquisition.Capture();
        }
        public void CapturePic()
        {
            this.aCamera.Acquisition.Freeze();
        }

        public void Beenden()
        {
            this.aCamera.Exit();
        }
        #endregion
        //-----Methoden-----//
        #region Methoden
        public Mat TakePicture()
        {
           
            this.aCamera.Acquisition.Freeze();

            int aMemID;
            this.aCamera.Memory.GetLast(out aMemID);

            int width, height;
            this.aCamera.Memory.GetSize(aMemID, out width, out height);

            byte[] imageData;
            this.aCamera.Memory.CopyToArray(aMemID, out imageData);

            Mat matImage = new Mat(height, width, DepthType.Cv8U, 3); 
            this.aCamera.Acquisition.Capture();
            matImage.SetTo(imageData);


            return matImage;
        }
       

        public Mat UndistortPicture(Mat pImg,Mat pCameraMatrix,Mat pDistortionCoefficients)
        {
            //-----Bild entzerren-----//
            if (pCameraMatrix.IsEmpty)
            {
                throw new Exception("Kamera nicht kalibriert");
            }
            else
            {
                Mat result = new Mat();
                CvInvoke.Undistort(pImg, result, pCameraMatrix, pDistortionCoefficients);
                return result;
            }
          
        }
        public Mat ApplyHomography(Mat pInputImage,Mat pHomographyMatrix)
        {
            //-----Homographie anwenden-----//
            if (pHomographyMatrix.IsEmpty)
            {
                throw new Exception("Homographie nicht berechnet.");
            }
            //-----Bild  skalieren-----//
            Size targetSize = new Size(pInputImage.Width/4, pInputImage.Height/4);

            Mat outputImage = new Mat(targetSize, DepthType.Cv8U, 3);
            CvInvoke.WarpPerspective(pInputImage, outputImage, pHomographyMatrix, targetSize);

            return outputImage;
        }


        public Mat ImageProcessing(Mat pCleanImage)
        {
            int area = 0;
            this.aMidpoints.Clear();
            Mat filterMat = new Mat();

            // Bild filtern
            CvInvoke.CvtColor(pCleanImage, aGrayFilter, ColorConversion.Bgr2Gray);
            CvInvoke.GaussianBlur(aGrayFilter, aBlurFilter, new Size(5, 5), 0);
            CvInvoke.Canny(aBlurFilter, aCannyFilter, 15, 120);
            CvInvoke.Dilate(aCannyFilter, filterMat, null, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(1));

            // Objekte erkennen
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(filterMat, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            int count = contours.Size;
            List<Point2D> detectedMidpoints = new List<Point2D>();

            double aspectRatioThresholdLong = 3;
            double aspectRatioThresholdShort = 1.5;

            for (int i = 0; i < count; i++)
            {
                var rotatedRect = CvInvoke.MinAreaRect(contours[i]);
                SizeF size = rotatedRect.Size;
                float angle = rotatedRect.Angle;

                // Längere und kürzere Seite identifizieren
                double width = size.Width;
                double height = size.Height;
                if (width < height)
                {
                    double temp = width;
                    width = height;
                    height = temp;
                    angle += 90;
                }

                area = (int)(width * height);
                bool isObjekt = false;
                double aspectRatio = width / height;
                double perimeter = CvInvoke.ArcLength(contours[i], true);
                double circularity = (4 * Math.PI * area) / (perimeter * perimeter);

                bool isCircle = circularity > 0.8 && circularity <= 1.2;
                bool isLongRectangle = aspectRatio > aspectRatioThresholdLong;
                bool isZylinder = !isLongRectangle && (aspectRatio > aspectRatioThresholdShort);
                bool isSquare = !isLongRectangle && !isZylinder && (area > aRefAreaSquare - aToleranzArea) && (area < aRefAreaSquare + aToleranzArea) && (Math.Abs(width - height) < aToleranzQuadrat);

                Point midpoint = new Point((int)rotatedRect.Center.X, (int)rotatedRect.Center.Y);
                Point2D midPunktRadius = new Point2D { X = midpoint.X, Y = midpoint.Y, Radius = (int)(width / 2)+10 };

                MCvScalar color = new MCvScalar(255, 0, 0);
                string label = "";

                // 0 = plane, 1 = cylinder, 2 = circle
                if (isCircle || isSquare)
                {
                   if(IstFlaecheInnerhalbToleranz(area,this.aRefAreaCircle,this.aToleranzAreaCircle))
                   {
                        isObjekt = true;
                        color = new MCvScalar(0, 255, 255);
                        label = "Kreis";
                        midPunktRadius.ObjektType = 2;
                        midPunktRadius.Radius = 20;
                        midPunktRadius.Winkel = 0;
                   }
                   
                    
                }
                else if (isLongRectangle)
                {
                    if (IstFlaecheInnerhalbToleranz(area,this.aRefAreaRectangle, this.aToleranzAreaRectangle))
                    {
                        isObjekt = true;
                        color = new MCvScalar(255, 0, 255);
                        label = "Langes Rechteck";
                        midPunktRadius.ObjektType = 0;
                        midPunktRadius.Radius = 10;
                        midPunktRadius.Winkel = angle;
                    }
                }
                else if (isZylinder)
                {
                    if (IstFlaecheInnerhalbToleranz(area,this.aRefAreaZylinder, this.aToleranzAreaZylinder))
                    {
                        isObjekt = true;
                        color = new MCvScalar(0, 165, 255);
                        label = "Zylinder";
                        midPunktRadius.ObjektType = 1;
                        midPunktRadius.Radius = 60;
                        midPunktRadius.Winkel = angle;
                    }
                }

                bool isDuplicate = detectedMidpoints.Any(p => Math.Abs(p.X - midPunktRadius.X) < 10 && Math.Abs(p.Y - midPunktRadius.Y) < 10);
                if (!isDuplicate&& isObjekt)
                {
                    detectedMidpoints.Add(midPunktRadius);
                    Point[] boxPoints = Array.ConvertAll(rotatedRect.GetVertices(), p => new Point((int)p.X, (int)p.Y));
                    CvInvoke.Polylines(aGrayFilter, new VectorOfPoint(boxPoints), true, color, 2);
                    CvInvoke.PutText(aGrayFilter, label, new Point(midpoint.X, midpoint.Y - 10), FontFace.HersheyComplex, 0.8, color, 2);
                    CvInvoke.DrawMarker(aGrayFilter, midpoint, new MCvScalar(100, 100, 100), MarkerTypes.Cross, 10, 2);
                    this.aMidpoints.Add(midPunktRadius);
                }
            }

            filterMat = aGrayFilter.Clone();
            contours.Dispose();
            return filterMat;
        }
        public bool IstFlaecheInnerhalbToleranz(float flaeche, float soll, float toleranz)
        {
            // Berechne den unteren und oberen Grenzwert
            float untereGrenze = soll - toleranz;
            float obereGrenze = soll + toleranz;

            // Prüfe, ob die Fläche innerhalb der Toleranz liegt
            return flaeche >= untereGrenze && flaeche <= obereGrenze;
        }


        #endregion
    }

}
