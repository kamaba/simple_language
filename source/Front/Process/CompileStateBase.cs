//****************************************************************************
//  File:      CompileStateBase.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description: 过程状态基类：大阶段(Phase)/小步骤(Step)/文件单元(File)共用的状态载体
//****************************************************************************

using System;
using System.Text;

namespace SimpleLanguage.Compile.Process
{
    public class CompileStateBase
    {
        public string name { get; protected set; }
        public EProcessState state { get; protected set; } = EProcessState.Pending;
        public DateTime startTime { get; protected set; }
        public DateTime endTime { get; protected set; }
        public string errorMessage { get; protected set; }

        public bool isPending => state == EProcessState.Pending;
        public bool isRunning => state == EProcessState.Running;
        public bool isCompleted => state == EProcessState.Completed;
        public bool isFailed => state == EProcessState.Failed;
        public bool isSkipped => state == EProcessState.Skipped;

        public CompileStateBase(string name)
        {
            this.name = name ?? string.Empty;
        }

        public void MarkRunning()
        {
            state = EProcessState.Running;
            startTime = DateTime.Now;
        }

        public void MarkCompleted()
        {
            state = EProcessState.Completed;
            endTime = DateTime.Now;
        }

        public void MarkFailed(string message = null)
        {
            state = EProcessState.Failed;
            endTime = DateTime.Now;
            if (!string.IsNullOrEmpty(message))
            {
                errorMessage = message;
            }
        }

        public void MarkSkipped(string message = null)
        {
            state = EProcessState.Skipped;
            endTime = DateTime.Now;
            if (!string.IsNullOrEmpty(message))
            {
                errorMessage = message;
            }
        }

        public string stateString => state.ToString();

        public virtual string ToFormatString(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 2);
            sb.Append('[').Append(state.ToString()).Append("] ").Append(name);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                sb.Append(" : ").Append(errorMessage);
            }
            return sb.ToString();
        }
    }
}
