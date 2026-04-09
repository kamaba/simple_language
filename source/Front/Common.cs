using System;
using System.Collections.Generic;
using System.IO;


namespace SimpleLanguage
{
    public static class Common
    {
        public static string SetDebugCode(string path)
        {
            return GetDebugCodeDir(path);
        }

        public static string GetDebugCodeDir(string path)
        {
            // current running directory
            var currentDir = Directory.GetCurrentDirectory();

            var relativePath = path ?? string.Empty;
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            var noExtPath = Path.ChangeExtension(relativePath, null) ?? relativePath;
            if (noExtPath.EndsWith("."))
            {
                noExtPath = noExtPath.Substring(0, noExtPath.Length - 1);
            }

            string outDir = Path.Combine(currentDir, "DebugCode", noExtPath);

            if( !Directory.Exists( outDir ) )
            {
                Directory.CreateDirectory(outDir);
            }

            return outDir;
        }

        public static string GetDebugCodeFilePath(string path, string fileName)
        {
            var outDir = GetDebugCodeDir(path);
            return Path.Combine(outDir, fileName);
        }
    }
}