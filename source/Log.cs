using SimpleLanguage.Compile;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleLanguage.Parse
{
    public class LogData
    {
        public int error { get; set; } = 0;
        public string message { get; set; }
        public string filePath { get; set; }
        public int sourceBeginLine { get; set; }         //开始所在行
        public int sourceBeginChar { get; set; }         //开始所在列
        public int sourceEndLine { get; set; }            //结束所在行
        public int sourceEndChar { get; set; }            //结束所在行
        public DateTime time { get; set; }

        public LogData()
        {

        }
        LogData( string msg, string path, int sline, int schar, int eline, int echar )
        {

        }
    }
    public class CodeFileLogData : LogData
    {
    }
    public class LexelLogData : LogData
    {

    }
    public class Log
    {
        static List<LogData> logDataList = new List<LogData>();
        public static void AddCodeFileLog( LogData data )
        {
            logDataList.Add(data);
        }
        public static void AddCodeFileLog( Token token, string msg )
        {
            Console.WriteLine("解析发生错误");
        }
    }
}