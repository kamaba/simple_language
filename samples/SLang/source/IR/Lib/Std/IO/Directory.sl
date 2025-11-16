//****************************************************************************
//  File:      Console.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************


namespace SimpleLanguage.Lib
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
    }
}
