using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Objekterkennung_RoboETHCore._3_Model
{
    //Es müssen öfters Pfade geändert werden, deshalb wurde diese Klasse erstellt.
    public static class PathManagment
    {
        public static string GetRelativePath(string filespec)
        {
            return filespec.Replace('\\', '/');
        }

        public static string GetPath(int pZurück = 0, string addPath = "")
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string[] parts = basePath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string shortenedPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts, 0, parts.Length - pZurück);
            string scriptPath = Path.Combine(shortenedPath, "3_Model", addPath);
            return scriptPath;
        }
        public static string ChangePath(int pZurück, string pPath,string pChangePath)
        {
            string[] parts = pPath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string shortenedPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts, 0, parts.Length - pZurück);
            shortenedPath = Path.Combine(shortenedPath, pChangePath);
            return shortenedPath;
        }
    }
}
