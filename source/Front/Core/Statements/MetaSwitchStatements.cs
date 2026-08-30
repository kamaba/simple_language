//****************************************************************************
//  File:      MetaSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  Metadata Switch statements 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

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
            /// <summary>枚举 case 匹配的枚举成员变量（Parse 阶段记录，不会被 SetMatchMetaVariable 覆盖）。</summary>
            public MetaVariable caseMatchMetaVariable => m_CaseMatchMetaVariable;
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
            private MetaVariable m_CaseMatchMetaVariable = null;
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
                // 关联 case 体块与 case 语句: MetaNextStatements/MetaBreakStatements 据此识别 switch case 上下文
                m_ThenMetaStatements.SetOwnerMetaStatements(this);
                // 注意: 体语句的解析延迟到 Parse() 中，保证 case is ClassA a 的绑定变量 a
                // 在体内语句解析之前已注册到 thenMetaStatements 的局部变量表
            }
            public void Parse()
            {
                if ( m_FileMetaKeyCaseSyntax.defineClassCallLink  != null )
                {
                    MetaCallLinkExpressNode mcen = new MetaCallLinkExpressNode(m_FileMetaKeyCaseSyntax.defineClassCallLink, m_OwnerMetaBlockStatements?.ownerMetaClass,
                        m_OwnerMetaBlockStatements, null);
                    mcen.Parse(new AllowUseSettings() { });
                    mcen.CalcReturnType();

                    if( m_OwnerSwitch.matchType == SwitchMatchType.EnumValue )
                    {
                        m_MatchMetaVariable = mcen.metaCallLink.finalCallNode?.GetReturnMetaVariable();
                        m_CaseMatchMetaVariable = m_MatchMetaVariable;
                    }
                    else
                    {
                        // 纯类名(如 Class3)解析走 MetaVisitNode.CreateByVisitMetaClass 时只设置
                        // 私有 m_ReturnMetaType（经 GetMetaType() 可取），不设置 callMetaType
                        m_MatchTypeClass = mcen.metaCallLink.finalCallNode?.callMetaType?.metaClass
                            ?? mcen.metaCallLink.GetMetaType()?.metaClass;
                    }

                    if (m_FileMetaKeyCaseSyntax.variableToken != null)
                    {
                        if (matchTypeClass != null)
                        {
                            string token2name = m_FileMetaKeyCaseSyntax.variableToken.lexeme.ToString();
                            if (thenMetaStatements.GetIsMetaVariable(token2name))
                            {
                                Debug.Write("Error 已有定义变量名称!!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                            }
                            else
                            {
                                MetaType mdt = new MetaType(matchTypeClass);
                                m_DefineMetaVariable = new MetaVariable(token2name, EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements,
                                    m_OwnerMetaBlockStatements.ownerMetaClass, mdt);
                                m_ThenMetaStatements.AddMetaVariable(m_DefineMetaVariable);
                            }
                        }
                        else
                        {
                            Debug.Write("Error 解析case中，前边的类型没有找到!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                        }
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
                    }
                }
                // 体语句在这里解析（绑定变量已注册完毕，体内可引用 defineMetaVariable）
                MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyCaseSyntax.executeBlockSyntax, m_ThenMetaStatements);
                m_ThenMetaStatements.SetTRMetaVariable(trMetaVariable);
                // case 体内含 next 语句: fall-through 语义，体执行完后继续匹配后续 case
                // (next 语句本身在 IR 层不发射指令，由 IRSwitchStatements 的 isContinueNext 分发处理)
                if (m_ThenMetaStatements.ContainsStatement<SimpleLanguage.Core.MetaNextStatements>())
                {
                    m_IsContinueNext = true;
                }
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
        /// <summary>表达式源（switch( x + y )），求值结果存入 matchSourceMv 临时变量。</summary>
        public MetaExpressNodeBase sourceMetaExpress => m_SourceMetaExpress;

        private SwitchMatchType m_MatchType =  SwitchMatchType.None;
        private FileMetaKeySwitchSyntax m_FileMetaKeySwitchSyntax = null;
        private List<MetaCaseStatements> m_MetaCaseStatements = new List<MetaCaseStatements>();
        private MetaBlockStatements m_DefaultMetaStatements = null;
        private MetaVariable m_MatchSourceMv = null;
        private MetaCallLink m_MetaCallLink = null;
        private MetaExpressNodeBase m_SourceMetaExpress = null;
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
                //m_MetaCallLink.CalcReturnType();
                m_MatchSourceMv = m_MetaCallLink.GetReturnMetaVariable();
                var mv = m_OwnerMetaBlockStatements.GetMetaVariableByName(m_MatchSourceMv.name);
                if (mv == m_MatchSourceMv)//如果直接调用其它地方的metavariable，需要生成一个临时的metavariable
                {
                    var fmt = mv.GetFinalMetaType();
                    if (fmt == null)
                    {
                        Debug.Assert(false, "");
                    }
                    // fmt.metaClass == null 且 enumValue != null: color = SwitchColor.Red 推断出的
                    // MetaEnumValue 包装类型（枚举成员对象），同样按枚举值匹配
                    if (fmt.metaClass is MetaEnum || fmt.enumValue != null)
                    {
                        m_MatchType = SwitchMatchType.EnumValue;
                    }
                    else if (NumberManager.IsNumberClass(fmt.metaClass))
                    {
                        m_MatchType = SwitchMatchType.ConstValue;
                    }
                    else if (fmt.metaClass == CoreMetaClassManager.stringMetaClass
                        || fmt.eType == EType.Boolean)
                    {
                        // string/bool 源同样按常量值匹配（跳转表 kind 3/4）
                        m_MatchType = SwitchMatchType.ConstValue;
                    }
                    else
                    {
                        m_MatchType = SwitchMatchType.ClassType;
                    }
                }
            }
            else if (m_FileMetaKeySwitchSyntax.sourceExpress != null)
            {
                // switch( x + y ) 表达式源: 求值一次存入临时变量，后续分发只读临时变量
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMetaBase = ownerMetaClass,
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = null,
                    fme = m_FileMetaKeySwitchSyntax.sourceExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress
                };
                m_SourceMetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
                if (m_SourceMetaExpress != null)
                {
                    m_SourceMetaExpress.Parse(new AllowUseSettings() { setterFunction = false, getterFunction = true });
                    m_SourceMetaExpress.CalcReturnType();
                }
                var retMt = m_SourceMetaExpress?.GetReturnMetaType();
                if (retMt == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaKeySwitchSyntax.token,
                        "Error switch 的源表达式解析失败!");
                    retMt = new MetaType(EType.Int32);
                }
                // 生成不冲突的临时变量名
                string tmpName = "#switchSrc";
                int idx = 0;
                while (m_OwnerMetaBlockStatements.GetMetaVariableByName(tmpName) != null)
                {
                    tmpName = "#switchSrc" + (idx++);
                }
                m_MatchSourceMv = new MetaVariable(tmpName, EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements,
                    m_OwnerMetaBlockStatements.ownerMetaClass, retMt);
                m_OwnerMetaBlockStatements.AddMetaVariable(m_MatchSourceMv);

                var fmt = m_MatchSourceMv.GetFinalMetaType();
                // 同上: MetaEnumValue 包装类型（metaClass == null, enumValue != null）按枚举值匹配
                if (fmt != null && (fmt.metaClass is MetaEnum || fmt.enumValue != null))
                {
                    m_MatchType = SwitchMatchType.EnumValue;
                }
                else if (fmt != null && (NumberManager.IsNumberClass(fmt.metaClass)
                    || fmt.metaClass == CoreMetaClassManager.stringMetaClass
                    || fmt.eType == EType.Boolean))
                {
                    m_MatchType = SwitchMatchType.ConstValue;
                }
                else
                {
                    m_MatchType = SwitchMatchType.ClassType;
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
