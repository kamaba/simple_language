//****************************************************************************
//  File:      IRSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/19 12:00:00
//  Description:
//****************************************************************************


using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    /// <summary>
    /// switch 语句 IR 生成。
    /// 匹配逻辑整体下沉到 CVM：只发射一条 Switch 指令（跳转表 payload），
    /// 不再在 Front 层分化成 if/Ceq 测试链。
    ///
    /// Switch 指令 payload（小端）：
    ///   int32  N                  表项数
    ///   int32  kinds[N]           0=int64常量 1=classId 2=float64位模式 3=字符串IR栈id 4=boolean
    ///   int64  values[N]          各 kind 对应的值
    ///   int32  targets[N]         匹配后跳转的指令索引（IRData.id）
    ///   int32  defaultTarget      未命中跳转的指令索引
    /// 栈效果：弹出 1 个值（switch 源）。
    /// </summary>
    public class IRSwitchStatements : IRStatements
    {
        public const int SwitchKindInt64 = 0;
        public const int SwitchKindClassId = 1;
        public const int SwitchKindFloat64 = 2;
        public const int SwitchKindStringId = 3;
        public const int SwitchKindBoolean = 4;

        public IRSwitchStatements(IRMethod method)
        {
            this.irMethod = method;
        }

        /// <summary>switch 跳转表中的一项。</summary>
        public class SwitchTableEntry
        {
            public int kind;
            public long value;
            public IRData targetIRData;
        }

        /// <summary>
        /// 延迟构建的 switch 跳转表。
        /// 发射阶段 targetIRData 引用先登记，IRMethod.Parse() 在全部指令编号完成后
        /// 调用 BuildPendingSwitchPayloads() 把 .id 序列化进 payload。
        /// </summary>
        public class PendingSwitchTable
        {
            public IRData switchIRData;
            public List<SwitchTableEntry> entries = new List<SwitchTableEntry>();
            public IRData defaultTargetIRData;
        }

        /// <summary>单个 case 的发射信息。</summary>
        private class CaseInfo
        {
            public MetaSwitchStatements.MetaCaseStatements metaCase;
            public List<SwitchTableEntry> entries = new List<SwitchTableEntry>();
            public IRNop bodyStartNop;
        }

        private IRBase EmitLoadSource(MetaSwitchStatements ms)
        {
            var ownerMetaClass = ms.matchSourceMv.GetFinalTemplateMetaClass();
            var owirmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv)
                ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.classId) : null);
            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(ms.matchSourceMv.GetFinalMetaType(), owirmc);
            var irmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv);
            return IRLoadVariable.CreateLoadVariable(irmt, irmc, irMethod, ms.matchSourceMv);
        }

        /// <summary>表达式源求值并 Store 到临时变量（仅在首个分发器前发射一次）。</summary>
        private void EmitSourceExpressStore(MetaSwitchStatements ms)
        {
            var irexpress = IRExpressManager.CreateExpress(irMethod, ms.sourceMetaExpress);
            if (irexpress != null)
            {
                m_IRStatements.Add(irexpress);
            }

            var ownerMetaClass = ms.matchSourceMv.GetFinalTemplateMetaClass();
            var owirmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv)
                ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.classId) : null);
            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(ms.matchSourceMv.GetFinalMetaType(), owirmc);
            var irmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv);
            m_IRStatements.Add(IRStoreVariable.CreateIRStoreVariable(irmt, irmc, irMethod, ms.matchSourceMv));
        }

        /// <summary>把 MetaConstExpressNode 转换成跳转表表项。</summary>
        private static SwitchTableEntry CreateEntryByConstNode(MetaConstExpressNode node)
        {
            var entry = new SwitchTableEntry();
            if (node.eType == EType.String)
            {
                entry.kind = SwitchKindStringId;
                entry.value = IRManager.instance.AddStringIRStack(node.value as string ?? string.Empty);
            }
            else if (node.eType == EType.Boolean)
            {
                entry.kind = SwitchKindBoolean;
                entry.value = (System.Convert.ToBoolean(node.value)) ? 1 : 0;
            }
            else if (node.eType == EType.Float32 || node.eType == EType.Float64
                || node.value is double || node.value is float) // Num 字面量按浮点处理
            {
                entry.kind = SwitchKindFloat64;
                entry.value = System.BitConverter.DoubleToInt64Bits(System.Convert.ToDouble(node.value));
            }
            else
            {
                entry.kind = SwitchKindInt64;
                entry.value = System.Convert.ToInt64(node.value);
            }
            return entry;
        }

        /// <summary>枚举 case：取枚举成员的常量值（兼容直接 MetaMemberEnum 与包装变量）。</summary>
        private static SwitchTableEntry CreateEntryByEnumCase(MetaVariable caseMv)
        {
            MetaMemberEnum mme = caseMv as MetaMemberEnum;
            if (mme == null)
            {
                var smv = caseMv?.sourceMetaVariable;
                mme = smv as MetaMemberEnum;
            }
            var constNode = mme?.enumValueConstExpressNode;
            if (constNode == null)
            {
                return null;
            }
            if (constNode.eType == EType.Float32 || constNode.eType == EType.Float64)
            {
                return new SwitchTableEntry
                {
                    kind = SwitchKindFloat64,
                    value = System.BitConverter.DoubleToInt64Bits(System.Convert.ToDouble(constNode.value)),
                };
            }
            return new SwitchTableEntry
            {
                kind = SwitchKindInt64,
                value = System.Convert.ToInt64(constNode.value),
            };
        }

        public List<IRBase> ParseIRStatements(MetaSwitchStatements ms)
        {
            // switch 开始标记
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            insNode.SetDebugInfoByToken(ms?.token, "SwitchStart");
            m_IRStatements.Add(new IRBase(insNode));

            IRNop endIRNop = new IRNop(irMethod);

            IRNop defaultNop = null;
            if (ms.defaultMetaStatements != null)
            {
                defaultNop = new IRNop(irMethod);
            }
            IRData defaultTargetIRData = (defaultNop != null ? defaultNop.data : endIRNop.data);

            // 收集每个 case 的表项（targetIRData 稍后回填）
            List<CaseInfo> caseList = new List<CaseInfo>();
            for (int i = 0; i < ms.metaCaseStatements.Count; i++)
            {
                var meis = ms.metaCaseStatements[i];
                var ci = new CaseInfo();
                ci.metaCase = meis;

                // 按 case 自身形态判定（不能只看 switch 整体 matchType）：
                // switch int 源整体是 ConstValue，但其中的 case is int 是类型匹配；
                // 类型 case（matchTypeClass != null）在任何形态的 switch 里都发射 kind=1 表项
                if (meis.matchTypeClass != null) // case is 类型匹配
                {
                    ci.entries.Add(new SwitchTableEntry
                    {
                        kind = SwitchKindClassId,
                        value = meis.matchTypeClass.classId,
                    });
                }
                else if (meis.matchType == MetaSwitchStatements.SwitchMatchType.EnumValue)
                {
                    var entry = CreateEntryByEnumCase(meis.caseMatchMetaVariable ?? meis.matchMetaVariable);
                    if (entry != null)
                    {
                        ci.entries.Add(entry);
                    }
                }
                else // 常量 case（任意形态 switch 中的字面量 case）
                {
                    for (int j = 0; j < meis.constExpressList.Count; j++)
                    {
                        var entry = CreateEntryByConstNode(meis.constExpressList[j]);
                        if (entry != null)
                        {
                            ci.entries.Add(entry);
                        }
                    }
                }

                caseList.Add(ci);
            }

            // 表达式源( switch( x + y ) ): 求值一次并存入临时变量，后续分发只 Load 临时变量
            if (ms.sourceMetaExpress != null)
            {
                EmitSourceExpressStore(ms);
            }

            // 主分发器：Load 源 + Switch(全部表项)
            EmitDispatcher(ms, caseList, 0, defaultTargetIRData);

            // 每个 case 的体
            for (int i = 0; i < caseList.Count; i++)
            {
                var ci = caseList[i];
                ci.bodyStartNop = new IRNop(irMethod);
                foreach (var e in ci.entries)
                {
                    e.targetIRData = ci.bodyStartNop.data;
                }
                m_IRStatements.Add(ci.bodyStartNop);

                // 类型匹配 case 且带绑定变量：进入体时执行 cast 并存入绑定变量
                // （按 case 自身形态判定，ConstValue 型 switch 里的 case is int n2 同样生效）
                if (ci.metaCase.matchTypeClass != null
                    && ci.metaCase.defineMetaVariable != null)
                {
                    EmitTypeCaseBinding(ms, ci.metaCase);
                }

                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irMethod.PushBreakTarget(endIRNop.data);
                try
                {
                    irbs.ParseAllIRStatements(ci.metaCase.thenMetaStatements);
                }
                finally
                {
                    irMethod.PopBreakTarget();
                }
                m_IRStatements.AddRange(irbs.irStatements);

                if (ci.metaCase.isContinueNext)
                {
                    // next: 本 case 体执行完后继续匹配后续 case
                    EmitDispatcher(ms, caseList, i + 1, defaultTargetIRData);
                }
                else
                {
                    var brEnd = new IRBranch(irMethod, EIROpCode.Br, endIRNop.data);
                    m_IRStatements.Add(brEnd);
                }
            }

            // default 块
            if (ms.defaultMetaStatements != null)
            {
                m_IRStatements.Add(defaultNop);
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irMethod.PushBreakTarget(endIRNop.data);
                try
                {
                    irbs.ParseIRStatements(ms.defaultMetaStatements);
                }
                finally
                {
                    irMethod.PopBreakTarget();
                }
                m_IRStatements.AddRange(irbs.irStatements);
            }

            m_IRStatements.Add(endIRNop);

            return m_IRStatements;
        }

        /// <summary>
        /// 发射一次 switch 分发：Load 源变量 + Switch 指令（fromCaseIdx 起的表项）。
        /// 若无表项则退化为 Br defaultTarget。
        /// </summary>
        private void EmitDispatcher(MetaSwitchStatements ms, List<CaseInfo> caseList, int fromCaseIdx, IRData defaultTargetIRData)
        {
            var entries = new List<SwitchTableEntry>();
            for (int i = fromCaseIdx; i < caseList.Count; i++)
            {
                entries.AddRange(caseList[i].entries);
            }

            if (entries.Count == 0)
            {
                var brDefault = new IRBranch(irMethod, EIROpCode.Br, defaultTargetIRData);
                m_IRStatements.Add(brDefault);
                return;
            }

            m_IRStatements.Add(EmitLoadSource(ms));

            IRData switchData = new IRData();
            switchData.opCode = EIROpCode.Switch;
            switchData.SetDebugInfoByToken(ms?.token, "Switch");
            m_IRStatements.Add(new IRBase(switchData));

            irMethod.RegisterPendingSwitchTable(new PendingSwitchTable
            {
                switchIRData = switchData,
                entries = entries,
                defaultTargetIRData = defaultTargetIRData,
            });
        }

        /// <summary>类型匹配 case 的绑定变量：Load 源 + CastClass(as 语义) + Store 绑定变量。</summary>
        private void EmitTypeCaseBinding(MetaSwitchStatements ms, MetaSwitchStatements.MetaCaseStatements meis)
        {
            if (meis.matchTypeClass == null || meis.defineMetaVariable == null)
            {
                return;
            }

            var ownerMetaClass = ms.matchSourceMv.GetFinalTemplateMetaClass();
            var owirmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv)
                ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.classId) : null);
            var srcMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(ms.matchSourceMv.GetFinalMetaType(), owirmc);
            var srcMc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv);

            // Load 源
            m_IRStatements.Add(IRLoadVariable.CreateLoadVariable(srcMt, srcMc, irMethod, ms.matchSourceMv));

            // CastClass: as 语义（成功 push 转换值，失败 push null），
            // payload 必须是 IRMetaType（序列化成 RuntimeDefType JSON），
            // 与 is/as 表达式的发射一致（见 IRExpress.cs MetaAsIsExpressNode 分支）
            var targetMt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(new MetaType(meis.matchTypeClass), owirmc);
            IRData castData = new IRData { opCode = EIROpCode.CastClass };
            castData.SetOpValue(targetMt);
            castData.SetDebugInfoByToken(meis?.token, "SwitchCaseCast " + meis.matchTypeClass.name);
            m_IRStatements.Add(new IRBase(castData));

            // Store 绑定变量（转换后的值；转换失败时为 null）
            var bindMc = IRManager.GetIRMetaClassByMetaVariable(meis.defineMetaVariable);
            if (bindMc == null)
            {
                var bindTpl = meis.defineMetaVariable.GetFinalTemplateMetaClass();
                if (bindTpl != null)
                {
                    bindMc = IRManager.instance.GetIRMetaClassById(bindTpl.classId);
                }
            }
            if (bindMc == null)
            {
                bindMc = IRManager.GetIRMetaClassByMetaOwner(meis.defineMetaVariable.ownerMetaBase);
            }
            var bindMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(meis.defineMetaVariable.GetFinalMetaType(), owirmc);
            var storeBind = IRStoreVariable.CreateIRStoreVariable(bindMt, bindMc, irMethod, meis.defineMetaVariable);
            m_IRStatements.Add(storeBind);
        }

        /// <summary>
        /// 构建 Switch 指令的跳转表 payload（由 IRMethod.Parse() 在指令编号完成后调用）。
        /// </summary>
        public static byte[] BuildSwitchPayload(PendingSwitchTable table)
        {
            int n = table.entries.Count;
            byte[] payload = new byte[8 + n * 16];

            System.BitConverter.GetBytes(n).CopyTo(payload, 0);
            int offset = 4;
            for (int i = 0; i < n; i++)
            {
                System.BitConverter.GetBytes(table.entries[i].kind).CopyTo(payload, offset);
                offset += 4;
            }
            for (int i = 0; i < n; i++)
            {
                System.BitConverter.GetBytes(table.entries[i].value).CopyTo(payload, offset);
                offset += 8;
            }
            for (int i = 0; i < n; i++)
            {
                int targetIdx = table.entries[i].targetIRData != null ? table.entries[i].targetIRData.id : -1;
                System.BitConverter.GetBytes(targetIdx).CopyTo(payload, offset);
                offset += 4;
            }
            int defaultIdx = table.defaultTargetIRData != null ? table.defaultTargetIRData.id : -1;
            System.BitConverter.GetBytes(defaultIdx).CopyTo(payload, offset);

            return payload;
        }
    }
}
