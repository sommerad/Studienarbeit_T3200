using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;
using System.Configuration;


namespace Objekterkennung_RoboETHCore._3_Model
{
    public class TofKamera_SICK
    {
        string aDataPath = PathManagment.GetRelativePath(Path.Combine(PathManagment.GetPath(3, "Data"), "data.ply"));
        //Auslagern des Pfades in die app.config
        //private string dllPath;
        //private string mySickDll;
        //private string mySickDll = ConfigurationManager.AppSettings["Path_Sick_TofKamera"] + "Sick_TofKamera_Projekt.dll";
        //private string dllPath = PathManagment.ChangePath(4, AppDomain.CurrentDomain.BaseDirectory, mySickDll);
        private string dllPath = PathManagment.ChangePath(4, AppDomain.CurrentDomain.BaseDirectory, "Sick_Kamera\\visionary_welcome\\cpp\\Projekt\\Debug\\Sick_TofKamera_Projekt.dll");
        //private string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloud_processingDLL.dll");
        private float offsetX = 0;
        private float offsetY = 0;
        private float offsetZ = 0;
        private float scaleX = 1;
        private float scaleY = 1;
        private float scaleZ = 1;
        private float offsetYaw = 0;
        private float offsetPitch = 0;
        private float offsetRoll = 0;

        // DLL-Funktionen definieren
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // Delegaten für die Funktionen definieren
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int InitCameraDelegate([MarshalAs(UnmanagedType.LPStr)] string outputPath);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int CaptureFrameDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int ResetCameraDelegate();

        // Funktionszeiger
        private IntPtr dllHandle;
        private InitCameraDelegate initCamera;
        private CaptureFrameDelegate captureFrame;
        private ResetCameraDelegate resetCamera;

        public TofKamera_SICK()
        {

            //mySickDll = ConfigurationManager.AppSettings["Path_Sick_TofKamera"] + "Sick_TofKamera_Projekt.dll";
            //dllPath = PathManagment.ChangePath(4, AppDomain.CurrentDomain.BaseDirectory, mySickDll);



            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException("DLL nicht gefunden: " + dllPath);
            }

            // Lade die DLL
            dllHandle = LoadLibrary(dllPath);
            if (dllHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Fehler beim Laden der DLL. Fehlercode: {Marshal.GetLastWin32Error()}");
            }

            // Lade die Funktionen
            LoadFunction<InitCameraDelegate>("initCamera", ref initCamera);
            LoadFunction<CaptureFrameDelegate>("captureFrame", ref captureFrame);
            LoadFunction<ResetCameraDelegate>("resetCamera", ref resetCamera);

            
            int answer = InitCamera();
            if (answer != 0)
            {
                throw new InvalidOperationException($"Fehler beim Initialisieren der Kamera. Fehlercode: {answer}");
            }

        }
        public List<Point2D> ChangeKorrSystemTof(List<Point2D> midpoints)
        {
            List<Point2D> transformedPoints = new List<Point2D>();
            foreach (var point in midpoints)
            {
                //Rechnet das 2D-Koordinatensystem in das Koordinatensystem der Kamera um.
                Point2D newPoint = new Point2D();
                newPoint.X = point.X * this.scaleX + this.offsetX;
                newPoint.Y = point.Y * this.scaleY + this.offsetY;
                newPoint.Radius = point.Radius;
                newPoint.Winkel = point.Winkel;

                newPoint.ObjektType = point.ObjektType;

                transformedPoints.Add(newPoint);
            }
            return transformedPoints;

        }
        private void LoadFunction<T>(string functionName, ref T functionDelegate) where T : Delegate
        {
            IntPtr functionPtr = GetProcAddress(dllHandle, functionName);
            if (functionPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Fehler beim Laden der Funktion {functionName}.");
            }

            functionDelegate = Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }

        public int InitCamera()
        {
            if (initCamera == null)
            {
                throw new InvalidOperationException("Funktion initCamera nicht geladen.");
            }
            //DLL-Funktion initCamera aufrufen
            return initCamera(this.aDataPath);
        }

        public int CaptureFrame()
        {
            if (captureFrame == null)
            {
                 throw new InvalidOperationException("Funktion captureFrame nicht geladen.");
            }
            //DLL-Funktion captureFrame aufrufen
            return captureFrame();
        }

        public int ResetCamera()
        {
            if (resetCamera == null)
            {
                throw new InvalidOperationException("Funktion resetCamera nicht geladen.");
            }

            //DLL-Funktion resetCamera aufrufen
            return resetCamera();
        }

        ~TofKamera_SICK()
        {
            ResetCamera();
            if (dllHandle != IntPtr.Zero)
            {
                FreeLibrary(dllHandle);
            }
            dllHandle = IntPtr.Zero;
            initCamera = null;
            captureFrame = null;
            resetCamera = null;
           

        }
    }
}
