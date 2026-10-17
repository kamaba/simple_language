//****************************************************************************
//  File:      FileCompileState.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/25 12:00:00
//  Description: 文件编译阶段中单个文件的状态（文件阶段分 Load->Token->Node->File 小步骤）
//****************************************************************************

namespace SimpleLanguage.Compile.Process
{
    /// <summary>
    /// 单个编译文件的状态。文件阶段策略为 ContinueUnit：
    /// 某一步骤失败(或期间产生 Error)则本文件标记失败，后续小步骤跳过该文件，但不影响其它文件。
    /// </summary>
    public class FileCompileState : CompileStateBase
    {
        public enum EFileStep
        {
            None = 0,

            /// <summary>读取文件内容</summary>
            Load,

            /// <summary>Lexer -> Token</summary>
            Token,

            /// <summary>Token -> Node</summary>
            Node,

            /// <summary>Node -> FileMeta</summary>
            File,
        }

        /// <summary>当前进行到的小步骤</summary>
        public EFileStep currentStep { get; private set; } = EFileStep.None;

        public FileCompileState(string fileName) : base(fileName)
        {
        }

        public void SetStep(EFileStep step)
        {
            currentStep = step;
        }

        /// <summary>本文件是否可以进入下一个小步骤（未失败/未跳过）</summary>
        public bool CanEnterNextStep()
        {
            return !isFailed && !isSkipped;
        }
    }
}
