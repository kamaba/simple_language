using System;

namespace SimpleLanguage.Export.SLIR
{
    public static class SLIRLocalLoader
    {
        public static SLIRReader.Module Load(string slirPath)
        {
            return SLIRReader.ReadModule(slirPath);
        }

        public static void LoadAndDump(string slirPath, string outputTxt)
        {
            SLIRDump.DumpToText(slirPath, outputTxt);
        }
    }
}
