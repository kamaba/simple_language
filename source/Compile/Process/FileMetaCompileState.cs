using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.source.Compile.Process
{
    public class FileCompileState : CompileStateBase
    {
        public enum ELoadState
        {
            None,
            LoadStart,
            Loading,
            LoadEnd,
            LexerParse,
            TokenParse,
            StructParseBegin,
            StructParseImport,
            StructParseClass,
            StructParseData,
            StructParseEnum,
            StructParseEnd,
        }
        public bool isInterupt => m_IsInterrupt;

        private ELoadState m_LoadState = ELoadState.None;
        public FileCompileState()
        {

        }
        public void SetLoadState(ELoadState loadState)
        {
            m_LoadState = loadState;
        }
    }
}
