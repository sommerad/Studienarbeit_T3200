using Objekterkennung_RoboETHCore._3_Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;

namespace Objekterkennung_RoboETH._3_Model
{

    public class TofKamera_Schmersal
    {
        private const string orderName = "AM_T100_Python_Apps_003\\";
        private const string scriptName = "AM_T100_one_shot";
        private const string ctiName = "dmvc-producer.cti";
        private const string dataName = "data.pcd";
        private const string funktionName = "main";
        private const string dllPath = "Local\\Programs\\Python\\Python311\\python311.dll";
        private List<string> aRangeSetting= new List<string> { "Range1500", "Range1875", "Range2000", "Range6000", "Range7500", "Range30000" };
        private readonly int rangeSetting = 0;
        private float offsetX = -7.07f;
        private float offsetY = 21.35f;
        private float offsetZ = 0;
        private float scaleX = 0.9094f;
        private float scaleY = 0.9094f;
        private float scaleZ = 1.0f;
        private float offsetYaw = 0;
        private float offsetPitch = 0;
        private float offsetRoll = 0;

        public TofKamera_Schmersal()
        {
            string ans = RunScript();
            if (ans == null)
            {
                throw new InvalidOperationException("Keine Kamera verbunden");
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

        public string RunScript()
        {
            // Initialisiere Python-Engine
            string dllPythonPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dllPythonPath = PathManagment.ChangePath(1, dllPythonPath,dllPath);
            if (!File.Exists(dllPythonPath))
            {
                throw new FileNotFoundException("Python Version 3.11 nicht Installiert");
            }
            else
            {
                Runtime.PythonDLL = dllPythonPath;
                PythonEngine.Initialize();
            }
            string scriptPath = PathManagment.GetPath(3, orderName);

            string ctiPath = PathManagment.GetRelativePath(Path.Combine(PathManagment.GetPath(3, orderName), ctiName));
            var ctiPathPython=new PyString(ctiPath);
            string dataPath = PathManagment.GetRelativePath(Path.Combine(PathManagment.GetPath(3, orderName), dataName));
            var dataPathPython = new PyString(dataPath);
            var rangeSettingPython = new PyString(aRangeSetting[rangeSetting]);

            try
            {
                // Führe das Python-Skript aus
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(scriptPath);
                    dynamic pyScript = Py.Import(scriptName);
                    
                   
                    var data =pyScript.InvokeMethod(funktionName, new PyObject[] { ctiPathPython, dataPathPython, rangeSettingPython });
                    return dataPath;


                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }

            finally
            {
                
                PythonEngine.Shutdown();
            }
        }
       

    }
}


