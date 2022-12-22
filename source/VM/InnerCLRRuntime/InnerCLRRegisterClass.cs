using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM.InnerCLRRuntime
{
    public class InnerCLRRegisterClass
    {
        public static InnerCLRRegisterClass s_Instance = null;
        public static InnerCLRRegisterClass instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new InnerCLRRegisterClass();
                }
                return s_Instance;
            }
        }
        public void RegisterDymnicClass()
        {

        }
    }
}
