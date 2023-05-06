using SimpleLanguage.source.Compile.Process;
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

        List<CompileStateBase> CompileStateList = new List<CompileStateBase>();
        public void AddCompileError( string str )
        {

        }
        public void AddProjectCompileState(ProjectCompileState.ELoadState state, ELevelInfo info, ProjectCompileState.EError error, string str )
        {

        }
    }
}
