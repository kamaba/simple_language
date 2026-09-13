//****************************************************************************
//  File:      ProcessManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/25 12:00:00
//  Description: Front 编译过程管理器。
//               统一管理五个大阶段：
//               RefModule(仅提示) -> File(文件级隔离) -> MetaCore(中断) -> IR(中断) -> Export(中断)
//               每个大阶段由若干小步骤组成；阶段成败由步骤返回值 + 阶段期间新增错误数
//               (Log.errorCount 差值) + 阶段错误策略共同决定。
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.Process
{
    public class ProcessManager
    {
        /// <summary>五个大阶段的固定执行顺序</summary>
        public static readonly ECompilePhase[] PhaseOrder = new ECompilePhase[]
        {
            ECompilePhase.RefModule,
            ECompilePhase.File,
            ECompilePhase.MetaCore,
            ECompilePhase.IR,
            ECompilePhase.Export,
        };

        private static ProcessManager s_Instance = null;
        public static ProcessManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ProcessManager();
                }
                return s_Instance;
            }
        }

        private readonly Dictionary<ECompilePhase, CompilePhaseState> m_PhaseMap = new Dictionary<ECompilePhase, CompilePhaseState>();
        private readonly List<CompilePhaseState> m_PhaseList = new List<CompilePhaseState>();

        /// <summary>当前正在执行的大阶段（无则为 None）</summary>
        public ECompilePhase currentPhase { get; private set; } = ECompilePhase.None;

        private ProcessManager()
        {
            Reset();
        }

        /// <summary>开始一次新的编译过程：清空所有阶段状态并重置策略</summary>
        public void Reset()
        {
            m_PhaseMap.Clear();
            m_PhaseList.Clear();
            // 读取外部 RefModule：错误只提示，不影响后续编译
            m_PhaseMap[ECompilePhase.RefModule] = new CompilePhaseState(ECompilePhase.RefModule, EPhaseErrorPolicy.NotifyOnly);
            // 文件阶段：单文件错误只影响当前文件，全部正确才进入下一阶段
            m_PhaseMap[ECompilePhase.File] = new CompilePhaseState(ECompilePhase.File, EPhaseErrorPolicy.ContinueUnit);
            // MetaCore / IR / Export：出现错误即中断本阶段并阻止后续阶段
            m_PhaseMap[ECompilePhase.MetaCore] = new CompilePhaseState(ECompilePhase.MetaCore, EPhaseErrorPolicy.AbortPhase);
            m_PhaseMap[ECompilePhase.IR] = new CompilePhaseState(ECompilePhase.IR, EPhaseErrorPolicy.AbortPhase);
            m_PhaseMap[ECompilePhase.Export] = new CompilePhaseState(ECompilePhase.Export, EPhaseErrorPolicy.AbortPhase);
            foreach (var phase in PhaseOrder)
            {
                m_PhaseList.Add(m_PhaseMap[phase]);
            }
            currentPhase = ECompilePhase.None;
        }

        public CompilePhaseState GetPhase(ECompilePhase phase)
        {
            m_PhaseMap.TryGetValue(phase, out var ps);
            return ps;
        }

        /// <summary>向某个大阶段注册一个小步骤（按注册顺序执行）</summary>
        public CompileStep AddStep(ECompilePhase phase, string stepName, Func<bool> execute)
        {
            var ps = GetPhase(phase);
            if (ps == null)
            {
                return null;
            }
            return ps.AddStep(stepName, execute);
        }

        /// <summary>前置阶段是否全部成功完成</summary>
        public bool CanEnterPhase(ECompilePhase phase)
        {
            foreach (var ph in PhaseOrder)
            {
                if (ph == phase)
                {
                    break;
                }
                var ps = m_PhaseMap[ph];
                if (ps == null || !ps.isCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>依次执行各阶段，直到 target 阶段（含）。返回最终是否顺利到达并完成 target。</summary>
        public bool RunToPhase(ECompilePhase target)
        {
            foreach (var ph in PhaseOrder)
            {
                var ps = GetPhase(ph);
                if (ps == null || ps.isCompleted)
                {
                    continue;
                }
                if (!RunPhase(ph))
                {
                    return false;
                }
                if (ph == target)
                {
                    break;
                }
            }
            var targetState = GetPhase(target);
            return targetState != null && targetState.isCompleted;
        }

        /// <summary>
        /// 执行单个大阶段（前置阶段必须全部成功，否则本阶段标记为 Skipped）。
        /// 已成功完成的阶段重复调用为幂等。返回本阶段是否成功。
        /// </summary>
        public bool RunPhase(ECompilePhase phase)
        {
            var ps = GetPhase(phase);
            if (ps == null)
            {
                return false;
            }
            if (ps.isCompleted)
            {
                return true;
            }
            if (ps.isRunning)
            {
                return false;
            }
            if (ps.isFailed || ps.isSkipped)
            {
                // 已失败/已跳过的阶段不允许重跑，需 Reset 后重新开始
                return false;
            }

            if (!CanEnterPhase(phase))
            {
                ps.MarkSkipped("前置阶段未全部成功，本阶段被跳过");
                Log.AddProcessLog(LID.ProcessPhaseSkipped, "", phase.ToString());
                return false;
            }

            currentPhase = phase;
            ps.MarkRunning();
            Log.AddProcessLog(LID.ProcessPhaseStart, "", phase.ToString());

            int errorBegin = Log.errorCount;
            bool allStepsOk = RunPhaseSteps(ps);
            ps.errorCount = Log.errorCount - errorBegin;

            bool success;
            if (ps.errorPolicy == EPhaseErrorPolicy.NotifyOnly)
            {
                // 只提示，不影响后续编译：即使有错误/步骤失败也视为阶段完成
                if (ps.errorCount > 0)
                {
                    Log.AddProcessLog(LID.ProcessPhaseErrorNotified, "", phase.ToString(), ps.errorCount.ToString());
                }
                success = true;
            }
            else
            {
                // ContinueUnit / AbortPhase：步骤失败或阶段期间出现错误 => 阶段失败，不进入下一阶段
                success = allStepsOk && ps.errorCount == 0;
            }

            if (success)
            {
                ps.MarkCompleted();
                Log.AddProcessLog(LID.ProcessPhaseEnd, "", phase.ToString());
            }
            else
            {
                string reason = BuildPhaseFailedReason(ps);
                ps.MarkFailed(reason);
                Log.AddProcessLog(LID.ProcessPhaseFailed, "", phase.ToString(), reason);
                // 失败阶段之后的所有阶段标记为跳过
                MarkLaterPhasesSkipped(phase);
            }

            currentPhase = ECompilePhase.None;
            return success;
        }

        private bool RunPhaseSteps(CompilePhaseState ps)
        {
            bool allOk = true;
            foreach (var step in ps.stepList)
            {
                if (step.isCompleted)
                {
                    continue;
                }

                step.MarkRunning();
                int errorBegin = Log.errorCount;
                bool ok;
                try
                {
                    ok = step.execute();
                }
                catch (Exception ex)
                {
                    ok = false;
                    Log.AddProcessLog(LID.ProcessStepFailed, "", step.name,
                        "异常: " + ex.Message + " | StackTrace: " + ex.StackTrace);
                }
                step.errorCount = Log.errorCount - errorBegin;

                if (ok)
                {
                    step.MarkCompleted();
                }
                else
                {
                    step.MarkFailed("步骤执行返回失败");
                    Log.AddProcessLog(LID.ProcessStepFailed, "", step.name, "步骤执行返回失败");
                    allOk = false;
                }

                if (!ok && ps.errorPolicy == EPhaseErrorPolicy.AbortPhase)
                {
                    // 中断策略：本阶段剩余小步骤不再执行
                    foreach (var later in ps.stepList)
                    {
                        if (later.isPending)
                        {
                            later.MarkSkipped();
                        }
                    }
                    break;
                }
                // NotifyOnly / ContinueUnit：继续执行后续小步骤
                // （ContinueUnit 的单元级隔离由步骤内部实现：失败的文件会被跳过，其它文件继续）
            }
            return allOk;
        }

        private void MarkLaterPhasesSkipped(ECompilePhase failedPhase)
        {
            bool after = false;
            foreach (var ph in PhaseOrder)
            {
                if (ph == failedPhase)
                {
                    after = true;
                    continue;
                }
                if (!after)
                {
                    continue;
                }
                var ps = m_PhaseMap[ph];
                if (ps != null && ps.isPending)
                {
                    ps.MarkSkipped("前置阶段[" + failedPhase.ToString() + "]失败");
                    Log.AddProcessLog(LID.ProcessPhaseSkipped, "", ph.ToString());
                }
            }
        }

        private string BuildPhaseFailedReason(CompilePhaseState ps)
        {
            if (ps.errorCount > 0)
            {
                return "阶段内产生了 " + ps.errorCount + " 个错误";
            }
            foreach (var step in ps.stepList)
            {
                if (step.isFailed)
                {
                    return "步骤[" + step.name + "]失败";
                }
            }
            return "阶段失败";
        }

        /// <summary>输出所有阶段/步骤的状态汇总</summary>
        public string ToFormatString()
        {
            var sb = new StringBuilder();
            foreach (var ps in m_PhaseList)
            {
                sb.Append(ps.ToFormatString(0));
            }
            return sb.ToString();
        }

        /// <summary>打印编译过程汇总日志</summary>
        public void PrintSummary()
        {
            Log.AddProcessLog(LID.ProcessSummary, "", ToFormatString());
        }
    }
}
