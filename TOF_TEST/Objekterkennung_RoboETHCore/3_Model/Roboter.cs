using NPOI.OpenXmlFormats.Dml.Diagram;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;

namespace Objekterkennung_RoboETH._3_Model
{
    public class Roboter
    {
        //-----------------Attribute-----------------//
        #region Attribute
        private const string aAdress = "127.0.0.1";
        private const int aPort = 32572;
        private Socket aSocket;
        private Point[] aMagazin=new Point[5];
        private int aMagazinCounter = 0;
       

        //-----------------Parameter-----------------//
        private int aOffsetX = 113;             
        private int aOffsetY = 497;             
        private int aOffsetZ = 70;
        private double aScalingX = 1;     
        private double aScalingY = 1;
        #endregion
        //-----------------Konstruktor-----------------//
        #region Konstruktor
        public Roboter()
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostByName(aAdress);     
                IPEndPoint ep = new IPEndPoint(hostInfo.AddressList[0], aPort);
                aSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                aSocket.Connect(ep);
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Herstellen der Verbindung zum Server", ex);
            }
            this.aMagazin[0]= new Point(175, 225);
            this.aMagazin[1] = new Point(220, 250);
            this.aMagazin[2] = new Point(180, 275);
            this.aMagazin[3] = new Point(220, 300);
            this.aMagazin[4] = new Point(180, 330);
            SendData("RS");
            Thread.Sleep(100);
            SendData("OG");
            Thread.Sleep(200);
            SendData("RS");
            Thread.Sleep(200);
            GoToStartPos();
           

        }
        ~Roboter()
        {
            CloseSocket();
        }
        #endregion
        //-----------------Get-Set-Methoden------------//
        #region Get-Set-Methoden
        public bool GetConnectionStatus()
        {
            return this.aSocket.Connected;
        }
        #endregion
        //-----------------Methoden-----------------//
        #region Methoden
        public void ConnectToServer()
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostByName(aAdress);      
                IPEndPoint ep = new IPEndPoint(hostInfo.AddressList[0], aPort);
                this.aSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.aSocket.Connect(ep);
                if (!this.aSocket.Connected) 
                {
                    throw new Exception("Verbindung zum Server konnte nicht hergestellt werden");

                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Herstellen der Verbindung zum Server: {ex.Message}" );
            }

        }
        public void SendData(string pData)
        {
            //Sende Typ : "MP ," + varX + ", " + varY + ", " + varZ + ", "varNEIGUNG +, "varROLL"");
            byte[] data = System.Text.Encoding.ASCII.GetBytes(pData);
            aSocket.Send(data, SocketFlags.None);
            data = null;
            Thread.Sleep(800);
            //Man kann hier die Wartezeit anpassen, je nachdem wie lange der Roboter braucht um die Bewegung auszuführen
    
        }
        public string RecieveDataSocket()
        {
            byte[] buffer = new byte[1024];
            int irx = aSocket.Receive(buffer);
            char[] chars = new char[irx];

            Decoder decoder = Encoding.UTF8.GetDecoder();
            int charLen = decoder.GetChars(buffer, 0, irx, chars, 0);
            String recv = new String(chars);
            Thread.Sleep(800);

            return recv;

        }
        public void CloseSocket()
        {
            aSocket.Close();

        }
        public List<PoseAttributes> ChangeKoordinations(List<PoseAttributes> pPoints)
        {
            for (int i = 0; i < pPoints.Count; i++)
            {
                PoseAttributes original = pPoints[i];
                PoseAttributes transformed = new PoseAttributes();

                // Koordinaten tauschen und negieren
                double transformedX = -original.Y;
                double transformedY = -original.X;

                // Skalieren und Verschieben
                transformedX = transformedX * this.aScalingX + this.aOffsetX;
                transformedY = transformedY * this.aScalingY + this.aOffsetY;

                transformed.X = (int)transformedX;
                transformed.Y = (int)transformedY;
                transformed.Z = original.Z;

                // Yaw anpassen
                float yawCorrection = (float)(Math.Atan2(transformed.X, transformed.Y) * (180.0 / Math.PI));
                transformed.Yaw = original.Yaw + yawCorrection;

                // Neigung anpassen:
                transformed.Neigung = -1 * (90 - original.Neigung);

                transformed.Roll = original.Roll;

                pPoints[i] = transformed;
            }

            return pPoints;
        }

        public void SendKoordinations(List<PoseAttributes> pPoints)
        {
            //Beispiel: MO 0, 300, 200, -70 - 40
            //Parameter 0: X - Achse
            //Parameter 1: Y - Achse
            //Parameter 2: Z - Achse
            //Parameter 3: Neigungswinkel
            //Parameter 4: Rollwinkel
            for (int i = 0; i < pPoints.Count; i++)
            {
                PoseAttributes p = pPoints[i];
                int yaw = (int)p.Yaw;
                int neigung = (int)p.Neigung;
                string data = "MP " + p.X.ToString() + ", " + p.Y.ToString() + ", " + this.aOffsetZ.ToString() + ", "+ neigung.ToString()+ ", " + yaw.ToString();
                SendData(data);
                Thread.Sleep(3000);
                data = "MP " + p.X.ToString() + ", " + p.Y.ToString() + ", "+p.Z.ToString() + ", "+ neigung.ToString() + ", " + yaw.ToString();
                SendData(data);
                Thread.Sleep(3000);
                SendData("GC");
                Thread.Sleep(500);
                data = "MP " + p.X.ToString() + ", " + p.Y.ToString() + ", " + this.aOffsetZ.ToString() + ", -90, 0";
                SendData(data);
                Thread.Sleep(2000);
                GoToMagazin();
            }

            SendData("OG");
            Thread.Sleep(1000);
            SendData("MJ 90, 0, 0, 0, 0");
            Thread.Sleep(1000);
        }
        
        private void GoToMagazin()
        {
            string data = "";
            if(this.aMagazinCounter==5)
            {
                throw new Exception("Das Magazin ist voll. Bitte leeren Sie das Magazin und bestätigen es");
            }
            else
            {
                data = "MP " + aMagazin[aMagazinCounter].X.ToString() + ", " + aMagazin[aMagazinCounter].Y.ToString() + ", " + this.aOffsetZ.ToString() + ", -90, 0";
                SendData(data);
                Thread.Sleep(5000);
                data = "MP " + aMagazin[aMagazinCounter].X.ToString() + ", " + aMagazin[aMagazinCounter].Y.ToString() + ",6 " + ", -90, 0";
                SendData(data);
                Thread.Sleep(1000);
                SendData("GO");
                Thread.Sleep(1000);
                data = "MP " + aMagazin[aMagazinCounter].X.ToString() + ", " + aMagazin[aMagazinCounter].Y.ToString() + ", " + this.aOffsetZ.ToString() + ", -90, 0";
                SendData(data);
                Thread.Sleep(1000);
                data = "MP " + aMagazin[aMagazinCounter].X.ToString() + ", " + aMagazin[aMagazinCounter].Y.ToString() + ", " + this.aOffsetZ.ToString() + ", -90, 0";
                SendData(data);
                Thread.Sleep(1000);
                this.aMagazinCounter++;
            }
        }
        public void GoToStartPos()
        {
            SendData("MP +553.1,+202.1,+300.0,.0,.0");
            Thread.Sleep(1000);
            SendData("GO");
        }
        //Position, in welcher der Roboterarm aus dem Sichtfeld der TOF-Kamera gefahren wird
        //sonst muss immer der richtige ablauf (zuerst 2d-kamera verbinden - kalibrieren - objekte erkennen, dann 3d-kamera verbinden -tof starten, dann roboter verbinden)
        //diese Funktion soll beim Aktivieren den Robotor zur Seite drehen und das Sichtfeld der TOF-Kamera freimachen

        public void GoToParkPosition() 
        {

            Thread.Sleep(1000);
            SendData("GO");
        }
        public void ResetMagazin()
        {
            this.aMagazinCounter = 0;
        }
        #endregion
    }
}
