using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Objekterkennung._3_Model;
using System;
using System.Drawing;

namespace Objekterkennung_RoboETH._3_Model
{
    public class Homographie:Kalibrierung
    {
        //-----Attribute-----//

        //-----Konstruktor-----//
        #region Konstruktor
        public Homographie()
        {

        }
        #endregion
        //-----Methoden-----//
        #region Methoden
        public void ComputeHomography(Mat chessboardImage)
        {
            //----Bild filtern----//
            Mat grayImage = new Mat();
            CvInvoke.CvtColor(chessboardImage, grayImage, ColorConversion.Bgr2Gray);

            //----Schachbrettmuster finden----//
            VectorOfPointF corners = new VectorOfPointF();
            bool found = CvInvoke.FindChessboardCorners(grayImage, this.aPatternSize, corners, CalibCbType.AdaptiveThresh | CalibCbType.NormalizeImage);

            if (found)
            {
                //-----Kanten verbessern----//
                CvInvoke.CornerSubPix(grayImage, corners, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));
               
                PointF[] imagePoints = corners.ToArray();
                PointF[] objectPoints2D = new PointF[this.aPatternSize.Width * this.aPatternSize.Height];
                //----Objektpunkte sammeln----//
                for (int i = 0; i < this.aPatternSize.Height; i++)
                {
                    for (int j = 0; j < this.aPatternSize.Width; j++)
                    {
                        //!!Hier muss ein anderer Wert rein. Er macht hier aus PX direkt mm vergößert man es, ändert es den Skalierungsfaktor
                        objectPoints2D[i * this.aPatternSize.Width + j] = new PointF(j * this.aSquareSize, i * this.aSquareSize);
                    }
                }

                //----Homography berechnen----//
                this.aHomography = CvInvoke.FindHomography(imagePoints, objectPoints2D);
                return;
            }
            else
            {
                throw new Exception("Homography fehlgeschlagen: Schachbrettmuster nicht gefunden.");
            }
        }
        #endregion

    }
}
