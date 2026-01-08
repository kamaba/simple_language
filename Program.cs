using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using SimpleLanguage.Project;
using System;
using System.Diagnostics;

namespace SimpleLanguage
{
    public class C1
    {
#pragma warning disable CS0414 // 字段“C1.CV”已被赋值，但从未使用过它的值
        int CV = 1;
#pragma warning restore CS0414 // 字段“C1.CV”已被赋值，但从未使用过它的值
        const int j20 = 11 + 20;
        int CV2 = C2.CV_1;
        public C1()
        {

        }
    }
    public class C2
    {
        public static object a = 20.ToString() + C3.m / 30 * 100;
        public static int CV_1 = 10 + CV2;
        public static int CV2 = CV_1 + CV3;
        public static int CV33 = CV3;
        public static int CV4 = GetInt();
        public static int CV3 = 20;
        public static object abc = "aaa" + CV3.ToString() + 1.2;
        static int CV10 = C3.m / 11 + 15;

        public static int GetInt()
        {
            return CV3 + 20;
        }
    }
    public class C3
    {
        public static int m = 20;
    }
    public class L<T>
    {
        public T m;
        public T laa( T t )
        {
            T mw = t;
            if( typeof( T ) == typeof( Int32) )
            {
                
            }
            return mw;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            CommandInputArgs inputArgs = new CommandInputArgs(args);
            ProjectManager.Run("../../../test/BaseTest/test1.sp", inputArgs );
            Console.ReadKey();
        }
    }
}
