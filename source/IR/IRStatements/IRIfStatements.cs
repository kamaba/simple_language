//****************************************************************************
//  File:      IRIfStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRIfStatements : IRStatements
    {
        public IRIfStatements( IRMethod method )
        {
            this.irMethod = method;
        }
        public class MetaIRElseIfStatements
        {
            public List<IRBase> conditionStatList = new List<IRBase>();
            public List<IRBase> thenStatList = new List<IRBase>();

            public IRBranch ifEndBrach = null;
            public IRBranch ifFalseBreach = null;
            public IRNop startNop = null;

            private IRExpressBase m_IrExpress = null;
            public void ParseIRStatements( IRMethod _irMethod, MetaIfStatements.MetaElseIfStatements mires )
            {
                startNop = new IRNop( _irMethod );
                startNop.data.SetDebugInfoByToken(GetBranchKeyToken(mires), "if/elif/else branch start");
                conditionStatList.Add(startNop);

                if (mires.ifElseState == MetaIfStatements.IfElseState.If || mires.ifElseState == MetaIfStatements.IfElseState.ElseIf)
                {
                    m_IrExpress = IRExpressManager.CreateExpress(_irMethod, mires.finalExpress);
                    conditionStatList.Add(m_IrExpress);

                    if (mires.metaAssignManager?.isNeedSetMetaVariable == true)
                    {
                        IRMetaClass irmc = _irMethod.irManager.GetIRMetaClassByName("S.Core.Bool");
                        IRStoreVariable storeLocal = IRStoreVariable.CreateIRStoreVariable( new IRMetaType(irmc, null ), irmc, _irMethod, mires.boolConditionVariable );
                        SetIRDataListDebugInfo(storeLocal, GetMetaVariableToken(mires.boolConditionVariable), "if/elif store condition var");
                        conditionStatList.Add(storeLocal);

                        IRMetaType irmt = new IRMetaType(irmc, null);
                        IRLoadVariable loadLocal = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mires.boolConditionVariable );
                        SetIRDataListDebugInfo(loadLocal, GetMetaVariableToken(mires.boolConditionVariable), "if/elif load condition var");
                        conditionStatList.Add(loadLocal);
                    }

                    ifFalseBreach = new IRBranch(_irMethod, EIROpCode.BrFalse, null);
                    ifFalseBreach.SetDebugInfoByToken(mires.ifOrElseIfKeySyntax?.token, "if/elif BrFalse");
                    conditionStatList.Add(ifFalseBreach);
                }

                IRBlockStatements irbs = new IRBlockStatements(_irMethod);
                irbs.ParseAllIRStatements(mires.thenMetaStatements);
                thenStatList.AddRange(irbs.irStatements);

                //{}代码执行结束后的位置
                ifEndBrach = new IRBranch(_irMethod, EIROpCode.Br, null);
                ifEndBrach.SetDebugInfoByToken(GetBranchEndBlockToken(mires), "if/elif/else block end Br");
                thenStatList.Add(ifEndBrach);
            }

            public string ToIRString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("#if ");
                //sb.AppendLine(m_FinalExpress.ToFormatString() + "#");

                //for (int i = 0; i < conditionStatList.Count; i++)
                //{
                //    sb.AppendLine(conditionStatList[i].ToIRString());
                //}

                //if(m_ThenMetaStatements != null )
                //{
                //    sb.AppendLine(m_ThenMetaStatements.ToIRString());
                //}

                return sb.ToString();
            }
        }
        public void ParseIRStatements( MetaIfStatements ifstatements )
        {
            IRNop ifEndIRNop = new IRNop(irMethod);
            ifEndIRNop.data.SetDebugInfoByToken(GetIfStatementsEndBlockToken(ifstatements), "if end");
            List<MetaIRElseIfStatements> mirList = new List<MetaIRElseIfStatements>();
            for (int i = 0; i < ifstatements.metaElseIfStatements.Count; i++)
            {
                var meis = ifstatements.metaElseIfStatements[i];

                MetaIRElseIfStatements mire = new MetaIRElseIfStatements();
                mirList.Add(mire);

                mire.ParseIRStatements(irMethod, meis);
                m_IRStatements.AddRange(mire.conditionStatList);
                m_IRStatements.AddRange(mire.thenStatList);
                mire.ifEndBrach.data.opValue = ifEndIRNop.data;
            }
            m_IRStatements.Add(ifEndIRNop);

            List<IRData> irdataList = new List<IRData>();
            for (int i = 0; i < m_IRStatements.Count; i++)
            {
                for (int j = 0; j < m_IRStatements[i].IRDataList.Count; j++)
                {
                    var addIR = m_IRStatements[i].IRDataList[j];
                    irdataList.Add(addIR);
                }
            }

            for ( int i = 0; i < mirList.Count; i++ )
            {
                var mire = mirList[i];
                if (mire.ifFalseBreach != null)
                {
                    if (i < mirList.Count - 1)
                    {
                        mire.ifFalseBreach.data.opValue = mirList[i + 1].startNop.data;
                    }
                    else if (i == mirList.Count - 1)
                    {
                        mire.ifFalseBreach.data.opValue = ifEndIRNop.data;
                    }
                }
            }
        }

        /// <summary>取分支入口的关键字 token（if/elif/else），用于标记分支起点。</summary>
        private static Token GetBranchKeyToken( MetaIfStatements.MetaElseIfStatements mires )
        {
            if (mires == null)
                return null;
            return mires.ifElseState == MetaIfStatements.IfElseState.Else
                ? mires.elseKeySyntax?.token
                : mires.ifOrElseIfKeySyntax?.token;
        }

        /// <summary>取分支代码块的结束 token（'}'），用于标记块结束跳转。</summary>
        private static Token GetBranchEndBlockToken( MetaIfStatements.MetaElseIfStatements mires )
        {
            if (mires == null)
                return null;
            return mires.ifOrElseIfKeySyntax?.executeBlockSyntax?.endBlock
                ?? mires.elseKeySyntax?.executeBlockSyntax?.endBlock;
        }

        /// <summary>取整个 if/elif/else 链的结束 token：优先取最后一个分支块的结束 token。</summary>
        private static Token GetIfStatementsEndBlockToken( MetaIfStatements ifstatements )
        {
            var meisList = ifstatements?.metaElseIfStatements;
            if (meisList != null && meisList.Count > 0)
            {
                var endToken = GetBranchEndBlockToken(meisList[meisList.Count - 1]);
                if (endToken != null)
                    return endToken;
            }
            return ifstatements?.fileMetaKeyIfSyntax?.ifExpressSyntax?.executeBlockSyntax?.endBlock;
        }

        /// <summary>取 MetaVariable 的定位 token：优先定义 token，回退 pingTokenList 首个。</summary>
        private static Token GetMetaVariableToken( MetaVariable mv )
        {
            if (mv == null)
                return null;
            if (mv.token != null)
                return mv.token;
            return (mv.pingTokenList != null && mv.pingTokenList.Count > 0) ? mv.pingTokenList[0] : null;
        }

        /// <summary>对 IRBase 的全部 IRData 设置 debugInfo（IRStoreVariable/IRLoadVariable 的 data 为私有，经 IRDataList 统一设置）。</summary>
        private static void SetIRDataListDebugInfo( IRBase irBase, Token token, string info )
        {
            if (irBase == null)
                return;
            for (int i = 0; i < irBase.IRDataList.Count; i++)
            {
                irBase.IRDataList[i].SetDebugInfoByToken(token, info);
            }
        }
    }
}
