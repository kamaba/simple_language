//****************************************************************************
//  File:      CompilePhaseState.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/25 12:00:00
//  Description: 编译大阶段：由若干小步骤组成，携带错误处理策略
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.Process
{
    /// <summary>
    /// 编译大阶段。阶段成败由 ProcessManager 依据
    /// 步骤返回值 + 阶段期间新增 Error 日志数(Log.errorCount 差值) + 错误策略 共同判定。
    /// </summary>
    public class CompilePhaseState : CompileStateBase
    {
        public ECompilePhase phase { get; private set; }
        public EPhaseErrorPolicy errorPolicy { get; private set; }

        public List<CompileStep> stepList { get; } = new List<CompileStep>();

        /// <summary>本阶段执行期间新增的错误数</summary>
        public int errorCount { get; internal set; }

        public CompilePhaseState(ECompilePhase phase, EPhaseErrorPolicy policy) : base(phase.ToString())
        {
            this.phase = phase;
            this.errorPolicy = policy;
        }

        public CompileStep AddStep(string stepName, Func<bool> execute)
        {
            var step = new CompileStep(stepName, execute);
            stepList.Add(step);
            return step;
        }

        public override string ToFormatString(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(base.ToFormatString(indent));
            if (errorCount > 0)
            {
                sb.Append(" errors: ").Append(errorCount);
            }
            sb.AppendLine();
            foreach (var step in stepList)
            {
                sb.Append(step.ToFormatString(indent + 1));
                if (step.errorCount > 0)
                {
                    sb.Append(" errors: ").Append(step.errorCount);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
