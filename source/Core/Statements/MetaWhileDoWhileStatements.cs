//****************************************************************************
//  File:      MetaWhileDoWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  Handle for loop statements syntax and while/dowhile loop statements syntax !
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaForStatements : MetaStatements
    {
        private bool m_IsForIn = false;
        private MetaVariable m_ForMetaVariable;
        private MetaVariable m_ForInContent = null;
        private FileMetaKeyForSyntax m_FileMetaKeyForSyntax = null;
        private MetaBlockStatements m_ThenMetaStatements = null;
        private MetaNewStatements m_NewStatements = null;
        private MetaAssignStatements m_AssignStatements = null;
        private MetaExpressNode m_ConditionExpress = null;
        private MetaAssignStatements m_StepStatements = null;
        public MetaForStatements(MetaBlockStatements mbs, FileMetaKeyForSyntax fmkfs ) : base(mbs)
        {
            m_FileMetaKeyForSyntax = fmkfs;

            Parse();
        }
        private void Parse()
        {
            m_IsForIn = m_FileMetaKeyForSyntax.isInFor;

            m_ThenMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyForSyntax.executeBlockSyntax);
            m_ThenMetaStatements.SetOwnerMetaStatements(this);

            if ( m_IsForIn )
            {
                if (m_FileMetaKeyForSyntax.conditionExpress == null)
                {
                    Console.WriteLine("Error for in express后边没有表达式!!");
                }


                m_ForInContent = null;
                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    m_ConditionExpress = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaBlockStatements, null, m_FileMetaKeyForSyntax.conditionExpress, false, false);
                    m_ConditionExpress.CalcReturnType();
                }

                var mcallEn = m_ConditionExpress as MetaCallExpressNode;
                var mnoen = m_ConditionExpress as MetaNewObjectExpressNode;
                if (mcallEn == null && mnoen == null)
                {
                    Console.WriteLine("Error For in 表达式，应该是个数组形式");
                    return;
                }
                if( mcallEn != null )
                {
                    m_ForInContent = mcallEn.GetMetaVariable();
                }
                else
                {
                    m_ForInContent = new MetaVariable("forcontent_" + GetHashCode().ToString(), m_OwnerMetaBlockStatements, ownerMetaClass, mnoen.GetReturnMetaDefineType() );
                    m_ThenMetaStatements.UpdateMetaVariable(m_ForInContent);
                }
                MetaType mdt = m_ForInContent.metaDefineType;
                if ( !(mdt.isArray || mdt.isEnum || mdt.isData) )
                {
                    Console.WriteLine("Error For in 表达式，应该是个数组形式!");
                    return;
                }
                var forMVMC = mdt.GetMetaInputTemplateByIndex();

                var fmcd = m_FileMetaKeyForSyntax.fileMetaClassDefine as FileMetaCallSyntax;
                if( fmcd == null )
                {
                    Console.WriteLine("Error For x in X必须有!!");
                    return;
                }
                string dname = fmcd.variableRef.name;
                var dmv = m_ThenMetaStatements.GetMetaVariableByName(dname);
                if (dmv != null )
                {
                    Console.WriteLine("Error 在 for .. in 中，不允许从for 外边定义遍历变量!!");
                    return;
                }
                else
                {
                    m_ForMetaVariable = new IteratorMetaVariable(dname, ownerMetaClass, m_OwnerMetaBlockStatements, m_ForInContent, forMVMC );
                }

                m_ThenMetaStatements.UpdateMetaVariable(m_ForMetaVariable);
            }
            else
            {

                var fmcd = m_FileMetaKeyForSyntax.fileMetaClassDefine;
                switch (fmcd)
                {
                    case FileMetaDefineVariableSyntax fmcd1:
                        {
                            m_NewStatements = new MetaNewStatements(m_ThenMetaStatements, fmcd1);
                        }
                        break;
                    case FileMetaOpAssignSyntax fmoas:
                        {
                            string sname = fmoas.variableRef?.name;

                            if (m_ThenMetaStatements.GetIsMetaVariable(sname))
                            {
                                m_AssignStatements = new MetaAssignStatements(m_ThenMetaStatements, fmoas);
                            }
                            else
                            {
                                m_ThenMetaStatements.AddOnlyNameMetaVariable(sname);
                                m_NewStatements = new MetaNewStatements(m_ThenMetaStatements, fmoas);
                            }
                            break;
                        }
                    case FileMetaCallSyntax fmcs:
                        {
                            string sname = fmcs.variableRef?.name;

                            if (m_ThenMetaStatements.GetIsMetaVariable(sname))
                            {
                                m_AssignStatements = new MetaAssignStatements(m_ThenMetaStatements, null);
                            }
                            else
                            {
                                m_ThenMetaStatements.AddOnlyNameMetaVariable(sname);
                                m_NewStatements = new MetaNewStatements(m_ThenMetaStatements, fmcs);
                            }
                        }
                        break;
                }
                if (m_FileMetaKeyForSyntax.stepFileMetaOpAssignSyntax != null)
                {
                    m_StepStatements = new MetaAssignStatements(m_ThenMetaStatements, m_FileMetaKeyForSyntax.stepFileMetaOpAssignSyntax);
                }

                if ( m_NewStatements != null)
                {
                    m_ForMetaVariable = m_NewStatements.metaVariable;
                }
                else if ( m_AssignStatements != null)
                {
                    m_ForMetaVariable = m_AssignStatements.metaVariable;
                }
                if (m_ForMetaVariable == null)
                {
                    Console.WriteLine("Error 没有找到相应的变量!!");
                }
                m_ThenMetaStatements.UpdateMetaVariable(m_ForMetaVariable);

                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    m_ConditionExpress = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_ThenMetaStatements, m_ForMetaVariable.metaDefineType, m_FileMetaKeyForSyntax.conditionExpress, false, false );
                    m_ConditionExpress.CalcReturnType();
                }
            }
            //必须放到最后，因为 前边有变量需要建立
            MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyForSyntax.executeBlockSyntax, m_ThenMetaStatements);
        }
        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            m_ThenMetaStatements?.SetDeep(dp);
            nextMetaStatements?.SetDeep(dp);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("for ");
            if (m_IsForIn)
            {
                sb.Append(m_ForMetaVariable.name);
                sb.Append(" in ");
                sb.Append(m_ForInContent.name);
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("{");
            sb.Append(Environment.NewLine);

            if (!m_IsForIn)
            {
                for (int i = 0; i < deep + 1; i++)
                {
                    sb.Append(Global.tabChar);
                }
                if ( m_NewStatements != null)
                {
                    sb.Append(m_NewStatements.ToFormatString());
                }
                if ( m_AssignStatements != null)
                {
                    sb.Append(m_AssignStatements.ToFormatString());
                }

                if (m_ConditionExpress != null)
                {
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append("if ");
                    sb.Append(m_ConditionExpress.ToFormatString());
                    sb.Append("{break;}");
                    sb.Append(Environment.NewLine);
                }
                if (m_StepStatements != null)
                {
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append(m_StepStatements.ToFormatString());
                }

                sb.Append(m_ThenMetaStatements?.nextMetaStatements?.ToFormatString());

            }
            else
            {
                sb.Append(m_ThenMetaStatements?.nextMetaStatements?.ToFormatString());
            }

            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("}");
            sb.Append(Environment.NewLine);

            sb.Append(nextMetaStatements?.ToFormatString());
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
    public partial class MetaWhileDoWhileStatements : MetaStatements
    {
        private FileMetaConditionExpressSyntax m_FileMetaKeyWhileSyntax = null;
        private MetaExpressNode m_ConditionExpress = null;
        private MetaBlockStatements m_ThenMetaStatements = null;
        private bool m_IsWhile = false;

        public MetaWhileDoWhileStatements(MetaBlockStatements mbs, FileMetaConditionExpressSyntax whileStatements ):
            base( mbs )
        {
            m_FileMetaKeyWhileSyntax = whileStatements;

            if( m_FileMetaKeyWhileSyntax.token?.type == ETokenType.DoWhile )
            {
                m_IsWhile = false;
            }
            else
            {
                m_IsWhile = true;
            }

            Parse();
        }
        private void Parse()
        {
            m_ThenMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyWhileSyntax.executeBlockSyntax);

            if (m_FileMetaKeyWhileSyntax.conditionExpress != null)
            {
                MetaType mdt = new MetaType(m_OwnerMetaBlockStatements.ownerMetaClass);

                m_ConditionExpress = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements( m_OwnerMetaBlockStatements, null, m_FileMetaKeyWhileSyntax.conditionExpress, false, false );
            }
            MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyWhileSyntax.executeBlockSyntax, m_ThenMetaStatements );

            if (m_ConditionExpress != null)
            {
                AllowUseConst auc = new AllowUseConst();
                m_ConditionExpress.Parse(auc);
                m_ConditionExpress.CalcReturnType();
            }
        }
        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            m_ThenMetaStatements?.SetDeep(dp);
            nextMetaStatements?.SetDeep(dp);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            StringBuilder sb2 = new StringBuilder();
            if (m_ConditionExpress != null)
            {
                for (int i = 0; i < deep + 1; i++)
                {
                    sb2.Append(Global.tabChar);
                }
                sb2.Append("if ");
                sb2.Append(m_ConditionExpress.ToFormatString());
                sb2.Append("{break;}");
            }

            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append( m_IsWhile ? "while " : "dowhile ");           
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("{");
            sb.Append(Environment.NewLine);

            if( m_IsWhile )
            {
                if( !string.IsNullOrEmpty( sb2.ToString() ) )
                {
                    sb.Append(sb2.ToString());
                }
            }
            if(m_ThenMetaStatements?.nextMetaStatements != null )
            {
                sb.Append(m_ThenMetaStatements?.nextMetaStatements.ToFormatString());
                sb.Append(Environment.NewLine);
            }

            if ( !m_IsWhile )
            {
                if (!string.IsNullOrEmpty(sb2.ToString()))
                {
                    sb.Append(sb2.ToString());
                    sb.Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("}");
            sb.Append(Environment.NewLine);

            sb.Append(nextMetaStatements?.ToFormatString());
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
}
