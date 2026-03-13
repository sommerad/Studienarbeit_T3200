using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Objekterkennung_RoboETHCore._3_Model
{
    public class PointCloudProcessing
    {
        //----------Strukturen----------//
        #region Strukturen
        [StructLayout(LayoutKind.Sequential)]
        public struct Parameters
        {
            
            public string dataPath;
            public string calibrationPath;
            public int selectedCamera;
            public bool twoViewports;
            public float maxDistanceMeasure;
            public float minDistanceMeasure;
            public float backgroundRemoveThresshold;
            public float calibrationThresholdBackground;
            public float planeDetectionThreshold;
            public float cylinderDetectionThreshold;
            public float RANSACmaxIteration;
            public float cylinderMinRadius;
            public float cylinderMaxRadius;
            public double textScale;
            public double coorSystemScale;
            public int windowWidth;
            public int windowHeight;
            public float xMinROI;
            public float xMaxROI;
            public float yMinROI;
            public float yMaxROI;
            public float zMinROI;
            public float zMaxROI;
            public float xKoordinateOffset;
            public float yKoordinateOffset;
            public float zKoordinateOffset;
            public float rotationAroungZDeg;
            public float xKoordianteSkaling;
            public float yKoordianteSkaling;
            public float zKoordianteSkaling;
            public float vogelGridSize;
            public float statisticalOutlierRemovalRadius;
            public int statisticalOutlierRemovalNeighbors;
            public float RadiusOutlierRemovalRadius;
            public int RadiusOutlierRemovalMinNeighbors;
            public float pointCloudScale;

        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PoseAttributes
        {
            public float X;
            public float Y;
            public float Z;
            public float Neigung;
            public float Roll;
            public float Yaw;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Point2D
        {
            public float X;
            public float Y;
            public float Radius;
            public float Winkel;
            public int ObjektType;  // 0 = plane, 1 = cylinder, 2 = circle, 3 = square
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Point3D
        {
            public float X;
            public float Y;
            public float Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectPose
        {
            public Point3D Position;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public float[] Orientation;
            public int type;  // 0 = plane, 1 = cylinder, 2 = circle, 3 = square
        }
        #endregion
        //----------DLL-Managment----------//
        #region DLL-Importe
        private string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloud_viewer.dll");
        //private string dllPath = PathManagment.ChangePath(4, AppDomain.CurrentDomain.BaseDirectory, "PointCloudDll\\build\\Debug\\cloud_viewer.dll");
        private const string orderName = "AM_T100_Python_Apps_003\\";
        private const string dataName = "data.pcd";
        private Parameters aParameter;
        private List<PoseAttributes> aObjektePose = null;



        // Funktion zum dynamischen Laden der DLL
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StartVisualizerThreadDelegate(Parameters pParameter);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StopVisualizerThreadDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ProcessPointsDelegate([In] Point2D[] inputPoints,int inputCount,[Out] ObjectPose[] outputPoses,ref int outputCount);

        private IntPtr dllHandle;
        private ProcessPointsDelegate processPoints;
        private StartVisualizerThreadDelegate startVisualizerThread;
        private StopVisualizerThreadDelegate stopVisualizerThread;

        #endregion
        //----------Konstruktor/Destruktor----------//
        #region Konstruktor/Destruktor
        private void InitParameters(int auswahlKamera)
        {
            this.aObjektePose = new List<PoseAttributes>();
            this.aParameter = new Parameters();
            this.aParameter.selectedCamera = auswahlKamera;
            this.aParameter.calibrationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calibrationData.json");
            this.aParameter.twoViewports = false;

            this.aParameter.windowWidth = 1863;
            this.aParameter.windowHeight = 600;
            this.aParameter.textScale = 4;

            if (auswahlKamera == 1)
            {
                this.aParameter.dataPath = PathManagment.GetRelativePath(Path.Combine(PathManagment.GetPath(3, "Data"), "data.ply"));
                this.aParameter.maxDistanceMeasure = -4;
                this.aParameter.minDistanceMeasure = -40;

                this.aParameter.backgroundRemoveThresshold = 3f;     
                this.aParameter.calibrationThresholdBackground = 5.0f;
                this.aParameter.planeDetectionThreshold = 0.1f;
                this.aParameter.cylinderDetectionThreshold = 10.5f;

                this.aParameter.RANSACmaxIteration = 5000;
                this.aParameter.cylinderMinRadius = 0.01f;
                this.aParameter.cylinderMaxRadius = 100.0f;
                this.aParameter.coorSystemScale = 20;
                this.aParameter.pointCloudScale = 1000.0f;



                this.aParameter.xMinROI = -20;
                this.aParameter.yMinROI = -20;
                this.aParameter.zMinROI = 0;
                this.aParameter.xMaxROI = 330;
                this.aParameter.yMaxROI = 260;        
                this.aParameter.zMaxROI = -68;      // >10000 ist der Filter vom Hintergrund aus und es wird der Hintergrund ebenfalls angezeigt 

                this.aParameter.xKoordinateOffset = 126;        //Verschiebt das Koordinatensystem der Punktwolke
                this.aParameter.yKoordinateOffset = 94; 
                this.aParameter.zKoordinateOffset = 0;   
                this.aParameter.rotationAroungZDeg = 1;
                this.aParameter.xKoordianteSkaling = 1;
                this.aParameter.yKoordianteSkaling = 1;
                this.aParameter.zKoordianteSkaling = 1;

                this.aParameter.vogelGridSize = 0.005f;                         //Filter Parameter
                this.aParameter.statisticalOutlierRemovalRadius = 0.0005f;
                this.aParameter.statisticalOutlierRemovalNeighbors = 5;
                this.aParameter.RadiusOutlierRemovalRadius = 5;
                this.aParameter.RadiusOutlierRemovalMinNeighbors = 5;
            }
            else if(auswahlKamera == 2) 
            {
                this.aParameter.dataPath = PathManagment.GetRelativePath(Path.Combine(PathManagment.GetPath(3, orderName), dataName));
                this.aParameter.maxDistanceMeasure = 27;
                this.aParameter.minDistanceMeasure = -42;

                this.aParameter.backgroundRemoveThresshold = 8.0f;    
                this.aParameter.calibrationThresholdBackground = 5.0f;
                this.aParameter.planeDetectionThreshold = 0.1f;
                this.aParameter.cylinderDetectionThreshold = 10.5f;

                this.aParameter.RANSACmaxIteration = 5000;
                this.aParameter.cylinderMinRadius = 0.01f;
                this.aParameter.cylinderMaxRadius = 100.0f;
                this.aParameter.coorSystemScale = 20;



                this.aParameter.xMinROI = -200;
                this.aParameter.yMinROI = -200;
                this.aParameter.zMinROI = 0;
                this.aParameter.xMaxROI = 200;
                this.aParameter.yMaxROI = 100;        
                this.aParameter.zMaxROI = -100;     

                this.aParameter.xKoordinateOffset = 350;        
                this.aParameter.yKoordinateOffset = 250;
                this.aParameter.zKoordinateOffset = -700;   
                this.aParameter.rotationAroungZDeg = 6;
                this.aParameter.xKoordianteSkaling = 1;
                this.aParameter.yKoordianteSkaling = 1;
                this.aParameter.zKoordianteSkaling = 1;

                this.aParameter.vogelGridSize = 0.005f;
                this.aParameter.statisticalOutlierRemovalRadius = 0.0005f;
                this.aParameter.statisticalOutlierRemovalNeighbors = 50;
                this.aParameter.RadiusOutlierRemovalRadius = 5;
                this.aParameter.RadiusOutlierRemovalMinNeighbors = 20;
            }


           

           

        }
        private void InitDll()
        {

            if (!File.Exists(this.aParameter.calibrationPath))
            {
                throw new FileNotFoundException("Kalibrierungsdaten nicht gefunden");
            }

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException("PointCloudProcessing.dll nicht gefunden");
            }

            // Lade die DLL
            dllHandle = LoadLibrary(dllPath);
            if (dllHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Fehler beim Laden der DLL. Fehlercode: {Marshal.GetLastWin32Error()}");
            }

            // Lade die Funktionen dynamisch
            IntPtr startVisualizerThreadPtr = GetProcAddress(dllHandle, "StartVisualizerThread");
            IntPtr stopVisualizerThreadPtr = GetProcAddress(dllHandle, "StopVisualizerThread");


            IntPtr processPointsPtr = GetProcAddress(dllHandle, "ProcessPoints");
            if (processPointsPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Fehler beim Laden der Funktion ProcessPoints.");
            }
            processPoints = (ProcessPointsDelegate)Marshal.GetDelegateForFunctionPointer(processPointsPtr, typeof(ProcessPointsDelegate));

            // Erstelle die Delegates für die geladenen Funktionen
            startVisualizerThread = (StartVisualizerThreadDelegate)Marshal.GetDelegateForFunctionPointer(startVisualizerThreadPtr, typeof(StartVisualizerThreadDelegate));
            stopVisualizerThread = (StopVisualizerThreadDelegate)Marshal.GetDelegateForFunctionPointer(stopVisualizerThreadPtr, typeof(StopVisualizerThreadDelegate));



            // Starte den Visualizer-Thread
            Task.Run(() => startVisualizerThread(aParameter));
        }
        public PointCloudProcessing(int auswahlKamera)
        {

            InitParameters(auswahlKamera);
            InitDll();

        }

        ~PointCloudProcessing()
        {
            // Stoppe den Visualizer-Thread beim Zerstören der Instanz
            stopVisualizerThread();

            if (dllHandle != IntPtr.Zero)
            {
                FreeLibrary(dllHandle);
            }
        }
        #endregion

        public List<ObjectPose> ProcessPoints(List<Point2D> points)
        {
            if (processPoints == null)
            {
                throw new InvalidOperationException("Die Funktion ProcessPoints ist nicht geladen.");
            }

            // Konvertiere List<Point2D> in ein Array
            Point2D[] inputPoints = points.ToArray();
            int inputCount = inputPoints.Length;

            // Platz für die Ausgabe vorbereiten
            ObjectPose[] outputPoses = new ObjectPose[inputCount]; 
            int outputCount = 0;

            // DLL-Funktion aufrufen
            int result = processPoints(inputPoints, inputCount, outputPoses, ref outputCount);

            if (result != 0)
            {
                throw new Exception($"ProcessPoints fehlgeschlagen: {MapResultCodeToMessage(result)}.");
            }

            // Ergebnisse in eine Liste umwandeln
            return outputPoses.Take(outputCount).ToList();
        }
        public List<PoseAttributes> GetObjektPoses()
        {
            return this.aObjektePose;
        }
        public void SetObjectPose(List<PoseAttributes> poses)
        {
            this.aObjektePose = poses;
        }
        public List<PoseAttributes> ConvertToPoseAttributes(List<ObjectPose> objectPoses)
        {

            List<PoseAttributes> poses = new List<PoseAttributes>();

            foreach (var pose in objectPoses)
            {
             
               

                float[] orientation = pose.Orientation;
                //             X-Achse              Y-Achse               Z-Achse
                float R11 = orientation[0], R12 = orientation[1], R13 = orientation[2]; //X-Werte
                float R21 = orientation[3], R22 = orientation[4], R23 = orientation[5]; //Y-Werte
                float R31 = orientation[6], R32 = orientation[7], R33 = orientation[8]; //Z-Werte
               
                
                if (pose.type == 1) // Cylinder
                {
                    PoseAttributes attributes = new PoseAttributes();

                  

                    float yawRad = (float)Math.Atan2(R12, R11);
                    float yawDeg = yawRad * 180f / (float)Math.PI;

                    const float yawTolerance = 5f;

                    if (yawDeg > yawTolerance)
                    {
                        yawDeg = yawDeg - 270f;
                    }
                    else if (yawDeg < -yawTolerance)
                    {
                        yawDeg = yawDeg - 90f; 
                    }
                    else
                    {
                        yawDeg = 0f;
                    }
                    attributes.Yaw = yawDeg;
                    attributes.Neigung = 0;
                    attributes.Roll = 0;

                    attributes.X = pose.Position.X;
                    attributes.Y = pose.Position.Y;
                    attributes.Z = 11;

                    poses.Add(attributes);
                }
                else if (pose.type == 0) // Rechteck
                {
                    // Berechne die Euler-Winkel (Pitch, Roll, Yaw)
                    float pitch = (float)Math.Atan2(R32, R33);
                    pitch = (float)(pitch * 180 / Math.PI);
                    float roll = (float)Math.Atan2(-R31, Math.Sqrt(R32 * R32 + R33 * R33));
                    roll = (float)(roll * 180 / Math.PI);
                    float yaw = (float)Math.Atan2(R21, R11);
                    yaw = (float)(yaw * 180 / Math.PI);

                    if (pose.Position.Z < 29)
                    {
                        // Ein Objekt mit Z = 11
                        PoseAttributes attributes = new PoseAttributes
                        {
                            X = pose.Position.X,
                            Y = pose.Position.Y,
                            Z = 11,
                            Neigung = pitch,
                            Roll = roll,
                            Yaw = yaw
                        };
                        poses.Add(attributes);
                    }
                    else
                    {
                        // Zwei Objekte: Z = 11 und Z = 32
                        PoseAttributes lower = new PoseAttributes
                        {
                            X = pose.Position.X,
                            Y = pose.Position.Y,
                            Z = 11,
                            Neigung = pitch,
                            Roll = roll,
                            Yaw = yaw
                        };
                        PoseAttributes upper = new PoseAttributes
                        {
                            X = pose.Position.X,
                            Y = pose.Position.Y,
                            Z = 32,
                            Neigung = pitch,
                            Roll = roll,
                            Yaw = yaw
                        };
                        poses.Add(lower);
                        poses.Add(upper);
                    }
                }

                else if (pose.type == 2) // Kreis
                {
                    // Invertiertes Z vom Detektor
                    float zCam = -1 * pose.Position.Z;

                    int circleCount = 0;
                    if (zCam >= 50 && zCam <= 65)
                    {
                        circleCount = 3;
                    }
                    else if (zCam >= 30 && zCam <= 49)
                    {
                        circleCount = 2;
                    }
                    else if (zCam >= 2 && zCam <= 29)
                    {
                        circleCount = 1;
                    }


                    // Z-Werte für Roboter-Greifpositionen
                    int[] targetZ = { 6, 26, 46 };
                   
                    for (int i = circleCount - 1; i >= 0; i--) // von oben nach unten
                    {
                        PoseAttributes circleAttr = new PoseAttributes();
                        circleAttr.Neigung = 0;
                        circleAttr.Roll = 0;
                        circleAttr.Yaw = 0;
                        circleAttr.X = pose.Position.X;
                        circleAttr.Y = pose.Position.Y;
                        circleAttr.Z = targetZ[i];

                        poses.Add(circleAttr);
                    }
                }
                else
                {
                    PoseAttributes attributes = new PoseAttributes();
                    attributes.Neigung = 0;
                    attributes.Roll = 0;
                    attributes.Yaw = 0;
                    attributes.X = pose.Position.X;
                    attributes.Y = pose.Position.Y;
                    attributes.Z = pose.Position.Z;

                    poses.Add(attributes);
                }
            
            }

            return poses;
        }
        private string MapResultCodeToMessage(int resultCode)
        {
            return resultCode switch
            {
                0 => "Operation erfolgreich abgeschlossen.",
                1 => "Kalibrierungsdaten der 2D-Kamera nicht gefunden",
                2 => "Kein Hintergrund beim Kalibrieren gefunden, ggf. Threshold anpassen",
                3 => "Kein Hintergrund beim Entfernen des Hintergrund gefunden, ggf. Threshold anpassen",
                4 => "Keine Inputs beim Aufruf der ProcessingCloud funktion vorhanden",
                5 => "Keine Punktwolke gefunden",
                _ => $"Unbekannter Fehlercode: {resultCode}"
            };
        }



    }
}
