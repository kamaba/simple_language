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

            var baseName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(baseName)) baseName = "Tokens";

            var outDir = Path.Combine(currentDir, "DebugCode", baseName);
            Directory.CreateDirectory(outDir);

            return outDir;
        }
    }
}