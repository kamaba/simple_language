using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class CompileManager
    {
        public static bool isInterrupt
        {
            get { return false; }
        }
        public static CompileManager s_Instance = null;
        public static CompileManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CompileManager();
                }
                return s_Instance;
            }
        }
        public void AddCompileError( string str )
        {

        }       
    }
}
