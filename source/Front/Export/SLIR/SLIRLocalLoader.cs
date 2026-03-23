using System;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage.Export.SLIR
{
    public static class SLIRLocalLoader
    {
        public static SLIRReader.Module Load(string slirPath)
        {
            return SLIRReader.ReadModule(slirPath);
        }

        internal static SLModulePackage LoadPackageJson(string packageJsonPath)
        {
            return SLModulePackageWriter.Read(packageJsonPath);
        }

        public static void LoadAndDump(string slirPath, string outputTxt)
        {
            SLIRDump.DumpToText(slirPath, outputTxt);
        }
    }
}
