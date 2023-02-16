using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.source.Compile.Process
{
    public class ProjectCompileState : CompileStateBase
    {
        public enum ELoadState
        {
            None,
            LoadProjectFile,
            LexerParse,
            TokenParse,
            StructParse,
            PorjectParse,
            CompileClassParse,
            CompileBeforeExec,
            CoreMetaClassInit,
            CompileConfigFile,
            CompileAfterExec,
            RuntimeExec,
        }
        public enum EError
        {

        }
        public bool isInterupt => m_IsInterrupt;

        private ELoadState m_LoadState = ELoadState.None;
        public ProjectCompileState()
        {

        }
        public void SetLoadState(ELoadState loadState)
        {
            m_LoadState = loadState;
        }
    }
}
