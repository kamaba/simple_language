//****************************************************************************
//  File:      Console.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************


namespace Std
{
    public static class Console
    {
        public static void print(string text, params object[] objs )
        {
            System.Console.Write(text, objs);
        }
        public static void println(string text, params object[] objs )
        {
            System.Console.WriteLine(text, objs);
        }
        public static void write(string text, params object[] objs)
        {
            System.Console.Write(text, objs);
        }
        public static void writeLine(string text, params object[] objs)
        {
            System.Console.WriteLine(text, objs);
        }
        // Overloads to accept VM objects (ClassObject / SObject) coming from the VM layer
        public static void print(SimpleLanguage.VM.ClassObject obj, params object[] objs)
        {
            print(ConvertToString(obj), objs);
        }
        public static void println(SimpleLanguage.VM.ClassObject obj, params object[] objs)
        {
            println(ConvertToString(obj), objs);
        }
        public static void write(SimpleLanguage.VM.ClassObject obj, params object[] objs)
        {
            write(ConvertToString(obj), objs);
        }
        public static void writeLine(SimpleLanguage.VM.ClassObject obj, params object[] objs)
        {
            writeLine(ConvertToString(obj), objs);
        }
        public static string input()
        {
            return System.Console.ReadLine();
        }
        public static string readLine()
        {
            return System.Console.ReadLine();
        }
        public static void println()
        {
            System.Console.WriteLine();
        }

        private static string ConvertToString(object o)
        {
            if (o == null) return "null";
            if (o is string s) return s;
            // handle VM objects
            if (o is SimpleLanguage.VM.SObject sv)
            {
                try
                {
                    var fmt = sv.ToFormatString();
                    if (!string.IsNullOrEmpty(fmt)) return fmt;
                    var val = sv.value;
                    if (val != null) return val.ToString();
                    return "null";
                }
                catch
                {
                    return o.ToString();
                }
            }
            return o.ToString();
        }
    }
}
