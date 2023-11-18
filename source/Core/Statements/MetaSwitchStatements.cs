//****************************************************************************
//  File:      MetaSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  Metadata Switch statements 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaSwitchStatements : MetaStatements
    {
        public enum SwitchMatchType
        {
            None,
            ClassType,
            ClassValue,
            ConstValue,
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
            public enum SwitchCaseType
            {
                None,
                Const,
                ClassType
            }

            public SwitchCaseType switchCaseType = SwitchCaseType.None;
            public List<MetaConstExpressNode> constExpressList = new List<MetaConstExpressNode>(); //常量表达示
            public MetaClass matchTypeClass;                        // 匹配的定义类型
            public MetaVariable matchMetaVariable;                  // 上边关联的匹配变量
            public MetaVariable defineMetaVariable;                 // 如果使用类型匹配，后边可跟一个定义变量
            public MetaBlockStatements thenMetaStatements;          //执行语句

            public bool isContinueNext { get; set; } = false;

            private FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax m_FileMetaKeyCaseSyntax = null;

            public MetaCaseStatements(FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax fmkcs, MetaBlockStatements mbs )
            {
                m_FileMetaKeyCaseSyntax = fmkcs;
                m_OwnerMetaBlockStatements = mbs;
                thenMetaStatements = new MetaBlockStatements(mbs, fmkcs.executeBlockSyntax);
                if( fmkcs.isContinueNextCastSyntax )
                {
                    isContinueNext = true;
                }

                Parse();

                MetaMemberFunction.CreateMetaSyntax(fmkcs.executeBlockSyntax, thenMetaStatements);
            }
            private void Parse()
            {
                if ( m_FileMetaKeyCaseSyntax.defineClassCallLink != null )
                {
                    MetaCallExpressNode mcen = new MetaCallExpressNode(m_FileMetaKeyCaseSyntax.defineClassCallLink, null, null);
                    mcen.Parse(new AllowUseConst() { });
                    mcen.CalcReturnType();

                    matchTypeClass = mcen.metaCallLink.finalCallNode.callerMetaClass;

                    if (matchTypeClass != null)
                    {
                        switchCaseType = SwitchCaseType.ClassType;
                    }
                    else
                    {
                        EType etype = EType.None;
                        string tname = "";
                        //if (MetaTypeFactory.MatchSystemType(m_FileMetaKeyCaseSyntax.defineClassToken.lexeme.ToString(), ref etype, ref tname))
                        //{
                        //    matchTypeClass = ClassManager.instance.GetClassByMetaType(new MetaType(etype, tname));
                        //    switchCaseType = SwitchCaseType.ClassType;
                        //}
                    }

                    if (m_FileMetaKeyCaseSyntax.variableToken != null)
                    {
                        if (matchTypeClass == null)
                        {
                            Console.WriteLine("Error 解析case中，前边的类型没有找到!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                            return;
                        }
                        string token2name = m_FileMetaKeyCaseSyntax.variableToken.lexeme.ToString();
                        if (thenMetaStatements.GetIsMetaVariable(token2name))
                        {
                            Console.WriteLine("Error 已有定义变量名称!!" + m_FileMetaKeyCaseSyntax.variableToken.ToLexemeAllString());
                            return;
                        }
                        MetaType mdt = new MetaType(matchTypeClass);
                        defineMetaVariable = new MetaVariable(token2name, m_OwnerMetaBlockStatements,
                            m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                        thenMetaStatements.AddMetaVariable(defineMetaVariable);

                        switchCaseType = SwitchCaseType.ClassType;
                    }                                         
                }
                else
                {
                    var list = m_FileMetaKeyCaseSyntax.constValueTokenList;
                    if (list.Count>0)
                    {
                        switchCaseType = SwitchCaseType.Const;
                        for (int i = 0; i < list.Count; i++)
                        {
                            var constExpress = new MetaConstExpressNode(list[i]);
                            constExpressList.Add(constExpress);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error 解析case 中，内容为空!!");
                        return;
                    }
                }
                if( switchCaseType == SwitchCaseType.None )
                {
                    Console.WriteLine("Error 解析Case失败!!");
                    return;
                }
                thenMetaStatements.SetTRMetaVariable(trMetaVariable);

                if (switchCaseType == SwitchCaseType.Const)
                {
                    for (int i = 0; i < constExpressList.Count; i++)
                    {
                        constExpressList[i].CalcReturnType();
                    }
                }
                else if (switchCaseType == SwitchCaseType.ClassType)
                {

                }
                return;
            }
            public override void SetDeep(int dp)
            {
                m_Deep = dp;
                thenMetaStatements?.SetDeep(dp);
            }
            public void SetMatchMetaVariable(MetaVariable matchMV )
            {
                matchMetaVariable = matchMV;
            }
            public override string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < deep; i++)
                {
                    sb.Append(Global.tabChar);
                }
                sb.Append("case ");
                if(switchCaseType == SwitchCaseType.Const )
                {
                    for( int i = 0; i < constExpressList.Count; i++ )
                    {
                        sb.Append(constExpressList[i].ToFormatString());
                        if( i < constExpressList.Count - 1 )
                            sb.Append(",");
                    }
                }
                else if(switchCaseType == SwitchCaseType.ClassType)
                {
                    sb.Append( matchTypeClass?.allName );
                    sb.Append(" ");
                    if(defineMetaVariable != null )
                    {
                        sb.Append(defineMetaVariable.name);
                    }
                }
                sb.Append(Environment.NewLine);
                sb.Append(thenMetaStatements.ToFormatString());
                if (isContinueNext)
                {
                    sb.Append("next;");
                }

                return sb.ToString();
            }
        }

        public SwitchMatchType m_MatchType;

        List<MetaCaseStatements> metaCaseStatements = new List<MetaCaseStatements>();
        public MetaBlockStatements defaultMetaStatements;

        public MetaVariable matchMV = null;
        public MetaCallLink m_MetaCallLink = null;

        FileMetaKeySwitchSyntax m_FileMetaKeySwitchSyntax;
        public MetaSwitchStatements(MetaBlockStatements mbs, FileMetaKeySwitchSyntax fmkss, MetaVariable retMv = null) : base(mbs)
        {
            m_FileMetaKeySwitchSyntax = fmkss;
            m_TrMetaVariable = retMv;
            Parse();
        }
        private void Parse()
        {
            if(m_FileMetaKeySwitchSyntax.fileMetaVariableRef != null )
            {
                m_MetaCallLink = new MetaCallLink(m_FileMetaKeySwitchSyntax.fileMetaVariableRef, ownerMetaClass, m_OwnerMetaBlockStatements);               
            }
            else
            {
                m_MatchType = SwitchMatchType.ClassType;
                matchMV = null;
            }

            for (int i = 0; i < m_FileMetaKeySwitchSyntax.fileMetaKeyCaseSyntaxList.Count; i++)
            {
                var cmcs = m_FileMetaKeySwitchSyntax.fileMetaKeyCaseSyntaxList[i];

                MetaCaseStatements mcs = new MetaCaseStatements(cmcs, m_OwnerMetaBlockStatements);

                metaCaseStatements.Add(mcs);
            }

            if (m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax != null)
            {
                defaultMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax);

                MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeySwitchSyntax.defaultExecuteBlockSyntax, defaultMetaStatements );
            }
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].SetTRMetaVariable(trMetaVariable);
            }
            if (defaultMetaStatements != null)
            {
                defaultMetaStatements.SetTRMetaVariable(trMetaVariable);
            }
            if (m_MetaCallLink != null)
            {
                AllowUseConst auc = new AllowUseConst();
                auc.callConstructFunction = false;
                m_MetaCallLink.Parse(auc);
                m_MetaCallLink.CalcReturnType();
                matchMV = m_MetaCallLink.ExecuteGetMetaVariable();
                var mv = m_OwnerMetaBlockStatements.GetMetaVariableByName(matchMV.name);
                if (mv == matchMV)//如果直接调用其它地方的metavariable，需要生成一个临时的metavariable 
                {
                    m_MatchType = SwitchMatchType.ConstValue;
                }
            }
            for (int i = 0; i < metaCaseStatements.Count; i++)
            {
                metaCaseStatements[i].SetMatchMetaVariable(matchMV);
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
            if(m_MatchType == SwitchMatchType.None )
            {
            }
            else if( m_MatchType == SwitchMatchType.ConstValue )
            {
                sb.Append(matchMV?.name);
            }
            else if (m_MatchType == SwitchMatchType.ClassType)
            {
                sb.Append(matchMV?.name);
                sb.Append(" ");
                sb.Append(m_MetaCallLink?.ToFormatString());
            }
            else if (m_MatchType == SwitchMatchType.ClassType)
            {
                sb.Append(matchMV?.name);
                sb.Append(" ");
                sb.Append(m_MetaCallLink?.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("{");
            for( int i = 0; i < metaCaseStatements.Count; i++ )
            {
                sb.Append(Environment.NewLine);
                sb.Append(metaCaseStatements[i].ToFormatString());
            }
            if(defaultMetaStatements != null )
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep+1; i++)
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
