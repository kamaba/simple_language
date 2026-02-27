//****************************************************************************
//  File:      MetaSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  Metadata Switch statements 
//****************************************************************************

using SimpleLanguage.Compile;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.Core
{
    public partial class MetaSwitchStatements : MetaStatements
    {
        public enum SwitchMatchType
        {
            None,
            ClassType,              //switch( 变量名称 ){ case ClassA class1:{} } 
            ConstValue,             // switch( int/string ){ case 1: {} case 2:{} }
            EnumValue               // switch( EnumType ){ case EnumType.Value1: {} case EnumType.Value2:{} }
        }
        public class MetaNextStatements : MetaStatements
        {
            public Token token;

            public MetaNextStatements( Token _token )
            {
                token = _token;
            }
            public override string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("next;");
                sb.Append(nextMetaStatements?.ToFormatString());

                return sb.ToString();
            }
        }
        public class MetaCaseStatements : MetaStatements
        {
            public MetaVariable matchMetaVariable => m_MatchMetaVariable;                       // 上边关联的匹配变量
            public List<MetaConstExpressNode> constExpressList => m_ConstExpressList;           //常量表达示
            public MetaClass matchTypeClass => m_MatchTypeClass;                                // 匹配的定义类型
            public MetaVariable defineMetaVariable => m_DefineMetaVariable;                     // 如果使用类型匹配，后边可跟一个定义变量
            public MetaBlockStatements thenMetaStatements => m_ThenMetaStatements;                //执行语句
            public SwitchMatchType matchType => m_OwnerSwitch?.matchType ?? SwitchMatchType.None;


            public bool isContinueNext => m_IsContinueNext;

            private FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax m_FileMetaKeyCaseSyntax = null;
            public List<MetaConstExpressNode> m_ConstExpressList = new List<MetaConstExpressNode>(); //常量表达示
            private MetaSwitchStatements m_OwnerSwitch = null;
            private MetaVariable m_MatchMetaVariable = null;
            private MetaClass m_MatchTypeClass = null;
            private MetaBlockStatements m_ThenMetaStatements;
            private MetaVariable m_DefineMetaVariable = null;
            private bool m_IsContinueNext = false;

            public MetaCaseStatements(MetaSwitchStatements mss, FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax fmkcs, MetaBlockStatements mbs )
            {
                m_OwnerSwitch = mss;
                m_FileMetaKeyCaseSyntax = fmkcs;
                m_OwnerMetaBlockStatements = mbs;
                m_ThenMetaStatements = new MetaBlockStatements(mbs, fmkcs.executeBlockSyntax);

                MetaMemberFunction.CreateMetaSyntax(fmkcs.executeBlockSyntax, m_ThenMetaStatements);
            }
            public void Parse()
            {
                // `next` is only valid as the last statement in the case block.
                // semantic: continue matching next case.
                if (m_ThenMetaStatements.ContainsStatement<SimpleLanguage.Core.MetaNextStatements>())
                {
                    m_ThenMetaStatements.ValidateStatementMustBeLast<SimpleLanguage.Core.MetaNextStatements>(
                        m_FileMetaKeyCaseSyntax?.executeBlockSyntax?.endBlock,
                        "Error next 必须放在case语句块的结尾");

                    m_IsContinueNext = m_ThenMetaStatements.IsLastStatement<SimpleLanguage.Core.MetaNextStatements>(out _);
                }
                else
                {
                    m_IsContinueNext = false;
                }

                if ( m_FileMetaKeyCaseSyntax.defineClassCallLink  != null )
                {
                    MetaCallLinkExpressNode mcen = new MetaCallLinkExpressNode(m_FileMetaKeyCaseSyntax.defineClassCallLink, m_OwnerMetaBlockStatements?.ownerMetaClass,
                        m_OwnerMetaBlockStatements, null);
                    mcen.Parse(new AllowUseSettings() { });
                    mcen.CalcReturnType();

                    if( m_OwnerSwitch.matchType == SwitchMatchType.EnumValue )
                    {
                        m_MatchMetaVariable = mcen.metaCallLink.finalCallNode.GetRetMetaVariable();
                    }
                    else
                    {
                        m_MatchTypeClass = mcen.metaCallLink.finalCallNode?.callMetaType?.metaClass;
                    }

                    if (m_FileMetaKeyCaseSyntax.variableToken != null)
                    {
                        if (matchTypeClass == null)
                        {
                            Debug.Write("Error 解析case中，前边的类型没有找到!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                            return;
                        }
                        string token2name = m_FileMetaKeyCaseSyntax.variableToken.lexeme.ToString();
                        if (thenMetaStatements.GetIsMetaVariable(token2name))
                        {
                            Debug.Write("Error 已有定义变量名称!!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                            return;
                        }
                        MetaType mdt = new MetaType(matchTypeClass);
                        m_DefineMetaVariable = new MetaVariable(token2name, EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements,
                            m_OwnerMetaBlockStatements.ownerMetaClass, mdt);
                        m_ThenMetaStatements.AddMetaVariable(m_DefineMetaVariable);
                    }                                         
                }
                else
                {
                    var list = m_FileMetaKeyCaseSyntax.constValueTokenList;
                    if (list.Count>0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var constExpress = new MetaConstExpressNode( m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, list[i]);
                            constExpressList.Add(constExpress);
                        }
                    }
                    else
                    {
                        Debug.Write("Error 解析case 中，内容为空!!");
                        return;
                    }
                }
                m_ThenMetaStatements.SetTRMetaVariable(trMetaVariable);
                return;
            }
            public override void SetDeep(int dp)
            {
                m_Deep = dp;
                m_ThenMetaStatements?.SetDeep(dp);
            }
            public void SetMatchMetaVariable(MetaVariable matchMV )
            {
                m_MatchMetaVariable = matchMV;
            }
            public override string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < deep; i++)
                {
                    sb.Append(Global.tabChar);
                }
                sb.Append("case ");
                //if(switchCaseType == SwitchCaseType.Const )
                //{
                //    for( int i = 0; i < constExpressList.Count; i++ )
                //    {
                //        sb.Append(constExpressList[i].ToFormatString());
                //        if( i < constExpressList.Count - 1 )
                //            sb.Append(",");
                //    }
                //}
                //else if(switchCaseType == SwitchCaseType.ClassType)
                //{
                //    sb.Append( matchTypeClass?.allClassName );
                //    sb.Append(" ");
                //    if(defineMetaVariable != null )
                //    {
                //        sb.Append(defineMetaVariable.name);
                //    }
                //}
                sb.Append(Environment.NewLine);
                sb.Append(thenMetaStatements.ToFormatString());
                if (isContinueNext)
                {
                    sb.Append("next;");
                }

                return sb.ToString();
            }
        }


        public SwitchMatchType matchType => m_MatchType;
        public MetaBlockStatements defaultMetaStatements => m_DefaultMetaStatements;
        public List<MetaCaseStatements> metaCaseStatements => m_MetaCaseStatements;
        public MetaVariable matchSourceMv => m_MatchSourceMv;
        public MetaVariable boolConditionVariable => m_BoolConditionVariable;
        public MetaCallLink metaCallLink => m_MetaCallLink;

        private SwitchMatchType m_MatchType =  SwitchMatchType.None;
        private FileMetaKeySwitchSyntax m_FileMetaKeySwitchSyntax = null;
        private List<MetaCaseStatements> m_MetaCaseStatements = new List<MetaCaseStatements>();
        private MetaBlockStatements m_DefaultMetaStatements = null;
        private MetaVariable m_MatchSourceMv = null;
        private MetaCallLink m_MetaCallLink = null;
        private MetaVariable m_BoolConditionVariable = null;
        public MetaSwitchStatements(MetaBlockStatements mbs, FileMetaKeySwitchSyntax fmkss, MetaVariable retMv = null) : base(mbs)
        {
            m_FileMetaKeySwitchSyntax = fmkss;
            m_TrMetaVariable = retMv;

            m_BoolConditionVariable = new MetaVariable("boolCondition", EVariableFrom.LocalStatement, mbs, mbs.ownerMetaClass, new MetaType(EType.Boolean));
            mbs.AddMetaVariable(m_BoolConditionVariable);

            if (m_FileMetaKeySwitchSyntax.fileMetaVariableRef != null)
            {
                m_MetaCallLink = new MetaCallLink(m_FileMetaKeySwitchSyntax.fileMetaVariableRef, ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
            }

            Parse();
        }
        private void Parse()
        {
            if (m_MetaCallLink != null)
            {
                AllowUseSettings auc = new AllowUseSettings();
                auc.callConstructFunction = false;
                m_MetaCallLink.Parse(auc);
                m_MetaCallLink.CalcReturnType();
                m_MatchSourceMv = m_MetaCallLink.ExecuteGetMetaVariable();
                var mv = m_OwnerMetaBlockStatements.GetMetaVariableByName(m_MatchSourceMv.name);
                if (mv == m_MatchSourceMv)//如果直接调用其它地方的metavariable，需要生成一个临时的metavariable 
                {
                    var fmt = mv.GetFinalMetaType();
                    if (fmt == null)
                    {
                        Debug.Assert(false, "");
                    }
                    if (fmt.metaClass is MetaEnum)
                    {
                        m_MatchType = SwitchMatchType.EnumValue;
                    }
                    else if (ClassManager.IsNumberClass(fmt.metaClass))
                    {
                        m_MatchType = SwitchMatchType.ConstValue;
                    }
                    else
                    {
                        m_MatchType = SwitchMatchType.ClassType;
                    }
                }
            }

            Debug.Assert(m_MatchSourceMv != null, "原变量为空!");

            for (int i = 0; i < m_FileMetaKeySwitchSyntax.fileMetaKeyCaseSyntaxList.Count; i++)
            {
                var cmcs = m_FileMetaKeySwitchSyntax.fileMetaKeyCaseSyntaxList[i];

                MetaCaseStatements mcs = new MetaCaseStatements( this, cmcs, m_OwnerMetaBlockStatements);

                metaCaseStatements.Add(mcs);
            }

            if (m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax != null)
            {
                m_DefaultMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax);

                MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax, m_DefaultMetaStatements);
            }
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].SetTRMetaVariable(trMetaVariable);
            }
            if (defaultMetaStatements != null)
            {
                defaultMetaStatements.SetTRMetaVariable(trMetaVariable);
            }
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].Parse();
                metaCaseStatements[i].SetMatchMetaVariable(m_MatchSourceMv);
            }
        }
        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].SetDeep(dp+1);
            }
            if (defaultMetaStatements != null)
            {
                defaultMetaStatements.SetDeep(dp+1);
            }
            nextMetaStatements?.SetDeep(dp);
        }
        public override void SetTRMetaVariable(MetaVariable mv)
        {
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].SetTRMetaVariable(mv);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("switch ");
            if (m_MatchType == SwitchMatchType.None)
            {
            }
            else if (m_MatchType == SwitchMatchType.ConstValue)
            {
                sb.Append(m_MatchSourceMv?.name);
            }
            else if (m_MatchType == SwitchMatchType.ClassType)
            {
                sb.Append(m_MatchSourceMv?.name);
                sb.Append(" ");
                sb.Append(m_MetaCallLink?.ToFormatString());
            }
            else if (m_MatchType == SwitchMatchType.ClassType)
            {
                sb.Append(m_MatchSourceMv?.name);
                sb.Append(" ");
                sb.Append(m_MetaCallLink?.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("{");
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(metaCaseStatements[i].ToFormatString());
            }
            if (defaultMetaStatements != null)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep + 1; i++)
                {
                    sb.Append(Global.tabChar);
                }
                sb.Append("default");
                sb.Append(Environment.NewLine);
                sb.Append(defaultMetaStatements?.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("}");
            sb.Append(Environment.NewLine);

            sb.Append(nextMetaStatements?.ToFormatString());

            return sb.ToString();
        }
    }
}
