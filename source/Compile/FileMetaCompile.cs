using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileCompileState : StateBase
    {
        public enum ELoadState
        {
            None,
            LoadStart,
            Loading,
            LoadEnd,
        }
        public bool isInterupt => m_IsInterrupt;

        private ELoadState m_LoadState = ELoadState.None;
        public FileCompileState()
        {

        }
        public void SetLoadState(ELoadState loadState )
        {
            m_LoadState = loadState;
        }
    }
}
