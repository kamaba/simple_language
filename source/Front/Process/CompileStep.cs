//****************************************************************************
//  File:      CompileStep.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/25 12:00:00
//  Description: 阶段内的小步骤（如文件阶段的 Token/Node/File，MetaCore 阶段的各整合步骤）
//****************************************************************************

using System;

namespace SimpleLanguage.Compile.Process
{
    /// <summary>
    /// 编译小步骤。execute 返回 false 表示本步骤失败；
    /// 步骤期间新增的 Error 日志数(errorCount)也参与成败判定（由 ProcessManager 统计）。
    /// </summary>
    public class CompileStep : CompileStateBase
    {
        /// <summary>步骤执行体：返回 false 表示失败</summary>
        public Func<bool> execute { get; private set; }

        /// <summary>本步骤执行期间新增的错误数</summary>
        public int errorCount { get; internal set; }

        public CompileStep(string name, Func<bool> execute) : base(name)
        {
            this.execute = execute ?? (() => true);
        }
    }
}
