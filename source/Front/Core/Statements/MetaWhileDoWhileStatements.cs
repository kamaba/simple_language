//****************************************************************************
//  File:      MetaWhileDoWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  Handle for loop statements syntax and while/dowhile loop statements syntax !
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaForStatements : MetaStatements
    {
        public bool isForIn => m_IsForIn;
        public MetaVariable forIterateVariable => m_ForIterateVariable;
        public MetaVariable forInContent => m_ForInContent;
        public MetaVariable forInContentIterator => m_ForInContentIterator;
        public MetaVariable ifCeqVariable => m_IfCeqVariable;
        //public MetaVariable indexVariable => m_IndexVariable;
        public MetaMemberFunction hasNextFunction => m_HasNextFunction;
        public MetaMemberFunction nextValueFunction => m_NextValueFunction;
        public MetaBlockStatements thenMetaStatements => m_ThenMetaStatements;
        public MetaDefineVarStatements defineVarStatements => m_DefineVarStatements;
        public MetaAssignStatements assignStatements => m_AssignStatements;
        public MetaExpressNode conditionExpress => m_ConditionExpress;
        public MetaAssignStatements stepStatements => m_StepStatements;

        private bool m_IsForIn = false;
        private MetaVariable m_ForIterateVariable = null;
        private MetaVariable m_ForInContent = null;
        private MetaVariable m_ForInContentIterator = null;
        private MetaVariable m_IfCeqVariable = null;
        //private MetaVariable m_IndexVariable = null;
        private MetaBlockStatements m_ThenMetaStatements = null;
        private MetaDefineVarStatements m_DefineVarStatements = null;
        private MetaAssignStatements m_AssignStatements = null;
        private MetaExpressNode m_ConditionExpress = null;
        private MetaAssignStatements m_StepStatements = null;
        private MetaMemberFunction m_HasNextFunction = null;
        private MetaMemberFunction m_NextValueFunction = null;

        private FileMetaKeyForSyntax m_FileMetaKeyForSyntax = null;
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
                    Log.AddInStructMeta( EError.None, "Error for in express后边没有表达式!!");
                }

                m_ForInContent = null;
                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    CreateExpressParam cep2 = new CreateExpressParam()
                    {
                        ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        metaType = null,
                        fme = m_FileMetaKeyForSyntax.conditionExpress,
                        isStatic = false,
                        isConst = false,
                        parsefrom = EParseFrom.StatementRightExpress
                    };
                    m_ConditionExpress = ExpressManager.CreateExpressNode(cep2);
                    m_ConditionExpress.Parse(new AllowUseSettings());
                    m_ConditionExpress.CalcReturnType();

                    if (m_ConditionExpress is MetaArrayExpressNode maen)
                    {
                        //m_ForInContent = new MetaVariable("auto_" + this.GetHashCode().ToString(), MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, 
                        //    ownerMetaClass, null);
                        m_ConditionExpress = new MetaNewObjectExpressNode(maen, ownerMetaClass, m_OwnerMetaBlockStatements, null );
                        m_ConditionExpress.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                        m_ConditionExpress.CalcReturnType();
                    }
                }

                var mcallEn = m_ConditionExpress as MetaCallLinkExpressNode;
                var mnoen = m_ConditionExpress as MetaNewObjectExpressNode;
                if (mcallEn == null && mnoen == null)
                {
                    Log.AddInStructMeta(EError.None, "Error For in 表达式，应该是个数组形式");
                    return;
                }
                if( mcallEn != null )
                {
                    // Support: for v in EnumType
                    // When the in-expression resolves to an enum type (MetaClass visit), iterate over EnumType.values.
                    if (mcallEn.metaCallLink?.finalCallNode != null
                        && mcallEn.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.Enum
                        && mcallEn.metaCallLink.finalCallNode.callMetaType?.metaClass is MetaEnum men)
                    {
                        m_ForInContent = men.GetOrCreateValuesVariable();
                    }
                    else
                    {
                        m_ForInContent = mcallEn.GetMetaVariable();
                    }
                }
                else
                {
                    m_ForInContent = new MetaVariable("forcontent_" + GetHashCode().ToString(), MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, ownerMetaClass, mnoen.GetReturnMetaDefineType() );
                    m_ThenMetaStatements.UpdateMetaVariableDict(m_ForInContent);
                    mnoen.SetStoreMetaVariable(m_ForInContent);
                }
                MetaType mdt = m_ForInContent.GetFinalMetaType();
                if ( !m_ForInContent.GetIsCanCanIterate() )
                {
                    Log.AddInStructMeta(EError.None, "Error For in 必须是支持迭代器iterate");
                    return;
                }
                MetaClass iterMT = ClassManager.instance.GetClassByName("Core.IIterator<T>", 1);
                m_ForInContentIterator = new MetaVariable("for_iterator_" + GetHashCode().ToString(), 
                    MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, ownerMetaClass, 
                    new MetaType( iterMT, mdt.GetGenTemplateMetaTypeList() ) );
                m_ThenMetaStatements.UpdateMetaVariableDict(m_ForInContentIterator);

                var forMVMC = mdt.GetMetaInputTemplateByIndex();
                if( forMVMC == null )
                {
                    forMVMC = m_ForInContent.defineMetaType;
                }
                var mc = mdt.GetTemplateMetaClass();
                if ( m_FileMetaKeyForSyntax.fileMetaClassDefine is FileMetaDefineVariableSyntax fmcd )
                {
                    string dname = fmcd.name;
                    var dmv = m_ThenMetaStatements.GetMetaVariableByName(dname);
                    if (dmv != null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 在 for .. in 中，不允许从for 外边定义遍历变量!!");
                        return;
                    }
                    else
                    {
                        m_ForIterateVariable = new MetaIteratorVariable(fmcd.fileMetaClassDefine, fmcd.nameToken, ownerMetaClass, m_OwnerMetaBlockStatements, m_ForInContent);
                    }
                }
                else if( m_FileMetaKeyForSyntax.fileMetaClassDefine is FileMetaCallSyntax fmcs )
                {
                    string dname = fmcs.variableRef.name;
                    var dmv = m_ThenMetaStatements.GetMetaVariableByName(dname);
                    if (dmv != null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 在 for .. in 中，不允许从for 外边定义遍历变量!!");
                        return;
                    }
                    else
                    {
                        m_ForIterateVariable = new MetaIteratorVariable( null, fmcs.variableRef.callNodeList[0].token, ownerMetaClass, m_OwnerMetaBlockStatements, m_ForInContent);
                    }
                }
                if(m_ForIterateVariable == null )
                {
                    Log.AddInStructMeta(EError.None, "Error For x in X必须有!!");
                    return;
                }
                m_ForIterateVariable.Parse();

                m_HasNextFunction = m_ForIterateVariable.realMetaType.metaClass.GetFirstMetaMemberFunctionByName("hasNext");
                m_NextValueFunction = m_ForIterateVariable.realMetaType.metaClass.GetFirstMetaMemberFunctionByName("current");

                //if( m_ForInContent.realMetaType.isArray )
                //{
                //    m_IndexVariable = CoreMetaClassManager.arrayMetaClass.GetMetaMemberVariableByName("_index");
                //}
                //else
                //{
                //    m_IndexVariable = m_ForInContent.realMetaType.metaClass.GetMetaMemberVariableByName("_index");
                //}
                m_ThenMetaStatements.UpdateMetaVariableDict(m_ForIterateVariable);
            }
            else
            {
                var fmcd = m_FileMetaKeyForSyntax.fileMetaClassDefine;
                switch (fmcd)
                {
                    case FileMetaDefineVariableSyntax fmcd1:
                        {
                            m_DefineVarStatements = new MetaDefineVarStatements(m_ThenMetaStatements, fmcd1);
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
                                m_DefineVarStatements = new MetaDefineVarStatements(m_ThenMetaStatements, fmoas);
                            }
                            break;
                        }
                    case FileMetaCallSyntax fmcs:
                        {
                            string sname = fmcs.variableRef?.name;

                            if (m_ThenMetaStatements.GetIsMetaVariable(sname))
                            {
                                m_AssignStatements = new MetaAssignStatements(m_ThenMetaStatements);
                            }
                            else
                            {
                                m_ThenMetaStatements.AddOnlyNameMetaVariable(sname);
                                m_DefineVarStatements = new MetaDefineVarStatements(m_ThenMetaStatements, fmcs);
                            }
                        }
                        break;
                }
                if (m_FileMetaKeyForSyntax.stepFileMetaOpAssignSyntax != null)
                {
                    m_StepStatements = new MetaAssignStatements(m_ThenMetaStatements, m_FileMetaKeyForSyntax.stepFileMetaOpAssignSyntax);
                }

                if (m_DefineVarStatements != null)
                {
                    m_ForIterateVariable = m_DefineVarStatements.defineVarMetaVariable;
                }
                else if ( m_AssignStatements != null)
                {
                    m_ForIterateVariable = m_AssignStatements.metaVariable;
                }
                if (m_ForIterateVariable == null)
                {
                    Log.AddInStructMeta(EError.None, "Error 没有找到相应的变量!!");
                }
                m_ThenMetaStatements.UpdateMetaVariableDict(m_ForIterateVariable);

                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    CreateExpressParam cep2 = new CreateExpressParam()
                    {
                        ownerMBS = m_ThenMetaStatements,
                        ownerMetaClass = m_ThenMetaStatements.ownerMetaClass,
                        metaType = new MetaType( CoreMetaClassManager.booleanMetaClass ),
                        fme = m_FileMetaKeyForSyntax.conditionExpress,
                        isStatic = false,
                        isConst = false,
                        parsefrom = EParseFrom.StatementRightExpress
                    };
                    m_ConditionExpress = ExpressManager.CreateExpressNode(cep2);
                    m_ConditionExpress.Parse(new AllowUseSettings());
                    m_ConditionExpress.CalcReturnType();
                }
            }
            //必须放到最后，因为 前边有变量需要建立
            MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyForSyntax.executeBlockSyntax, m_ThenMetaStatements);
        }
        public override void SetDeep(int dp)
        {
            //m_Deep = dp;
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
                sb.Append( this.m_ForIterateVariable.name);
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
                if (m_DefineVarStatements != null)
                {
                    sb.Append(m_DefineVarStatements.ToFormatString());
                }
                if (m_AssignStatements != null)
                {
                    sb.Append(m_AssignStatements.ToFormatString());
                }
                if (m_StepStatements != null)
                {
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append(m_StepStatements.ToFormatString());
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
    public sealed class MetaWhileDoWhileStatements : MetaStatements
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
            m_ThenMetaStatements.SetOwnerMetaStatements(this);

            if (m_FileMetaKeyWhileSyntax.conditionExpress != null)
            {
                MetaType mdt = new MetaType(m_OwnerMetaBlockStatements.ownerMetaClass);

                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaKeyWhileSyntax.conditionExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress
                };
                m_ConditionExpress = ExpressManager.CreateExpressNode(cep2);
            }
            MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyWhileSyntax.executeBlockSyntax, m_ThenMetaStatements );

            if (m_ConditionExpress != null)
            {
                AllowUseSettings auc = new AllowUseSettings();
                m_ConditionExpress.Parse(auc);
                m_ConditionExpress.CalcReturnType();
            }
        }
        public void SetDeep(int dp)
        {
            //m_Deep = dp;
            m_ThenMetaStatements?.SetDeep(dp);
            nextMetaStatements?.SetDeep(dp);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            StringBuilder sb2 = new StringBuilder();
            //if (m_ConditionExpress != null)
            //{
            //    for (int i = 0; i < deep + 1; i++)
            //    {
            //        sb2.Append(Global.tabChar);
            //    }
            //    sb2.Append("if ");
            //    sb2.Append(m_ConditionExpress.ToFormatString());
            //    sb2.Append("{break;}");
            //}

            //for (int i = 0; i < deep; i++)
            //{
            //    sb.Append(Global.tabChar);
            //}
            //sb.Append( m_IsWhile ? "while " : "dowhile ");           
            //sb.Append(Environment.NewLine);
            //for (int i = 0; i < deep; i++)
            //{
            //    sb.Append(Global.tabChar);
            //}
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

            //for (int i = 0; i < deep; i++)
            //{
            //    sb.Append(Global.tabChar);
            //}
            sb.Append("}");
            sb.Append(Environment.NewLine);

            sb.Append(nextMetaStatements?.ToFormatString());
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
}
