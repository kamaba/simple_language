using Simple;
using Simple.NA.NNA;
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using SimpleLanguage.Project;
using System;

namespace SimpleLanguage
{
    public class C1
    {
        int CV = 1;
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
    class Program
    {
        static void Main(string[] args)
        {
            C1 c1 = new C1();
            int a = C2.CV_1;
            a = C2.CV2;
            a = C2.CV33;
            a = C2.CV3;
            a = C2.CV4;
            var vv = C2.abc;
            int mm = Simple.NA.NNA.NB.AA(10);
            MetaExpressNode M;

            CommandInputArgs inputArgs = new CommandInputArgs(args);
            ProjectManager.Run("BaseTest\\test1.sp", inputArgs );
            //Console.WriteLine("Hello World!");

            Console.ReadKey();            
        }
    }
}
