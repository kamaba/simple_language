using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;


namespace SimpleLanguage
{
    public static class Common
    {
        public static string SetDebugCode(string path)
        {
            // current running directory
            var currentDir = Directory.GetCurrentDirectory();

            path  = path.Substring(0, path.Length - 3);
            string outDir = Path.Combine(currentDir, "DebugCode", path );

            if( !Directory.Exists( outDir ) )
            {
                Directory.CreateDirectory(outDir);
            }

            return outDir;
        }
    }
}