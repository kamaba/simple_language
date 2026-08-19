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
        //public MetaMemberFunction hasNextFunction => m_HasNextFunction;
        //public MetaMemberFunction nextValueFunction => m_NextValueFunction;
        public MetaBlockStatements thenMetaStatements => m_ThenMetaStatements;
        public MetaDefineVarStatements defineVarStatements => m_DefineVarStatements;
        public MetaAssignStatements assignStatements => m_AssignStatements;
        public MetaExpressNodeBase conditionExpress => m_ConditionExpress;
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
        private MetaExpressNodeBase m_ConditionExpress = null;
        private MetaAssignStatements m_StepStatements = null;
        //private MetaMemberFunction m_HasNextFunction = null;
        //private MetaMemberFunction m_NextValueFunction = null;

        private FileMetaKeyForSyntax m_FileMetaKeyForSyntax = null;
        public MetaForStatements(MetaBlockStatements mbs, FileMetaKeyForSyntax fmkfs ) : base(mbs)
        {
            m_FileMetaKeyForSyntax = fmkfs;

            Parse();
        }
        private void Parse()
        {
            m_Token = m_FileMetaKeyForSyntax.token;

            m_IsForIn = m_FileMetaKeyForSyntax.isInFor;

            m_ThenMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyForSyntax.executeBlockSyntax);
            m_ThenMetaStatements.SetOwnerMetaStatements(this);

            if ( m_IsForIn )
            {
                if (m_FileMetaKeyForSyntax.conditionExpress == null)
                {
                    Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error for in express后边没有表达式!!");
                }

                m_ForInContent = null;
                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    CreateExpressParam cep2 = new CreateExpressParam()
                    {
                        ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        metaType = null,
                        fme = m_FileMetaKeyForSyntax.conditionExpress,
                        isStatic = false,
                        isConst = false,
                        parsefrom = EParseFrom.StatementRightExpress
                    };
                    m_ConditionExpress = ExpressManager.CreateExpressNode(cep2);
                    m_ConditionExpress.Parse(new AllowUseSettings() );
                    m_ConditionExpress.CalcReturnType();

                    // Keep for-in right-expression behavior consistent with assignments:
                    // convert `range(...)` / class-call / array-literal into explicit MetaNewObjectExpressNode.
                    var conditionMetaType = m_ConditionExpress.GetReturnMetaType();
                    m_ConditionExpress = ExpressManager.ConvertNewExpress(m_ConditionExpress, conditionMetaType );
                }

                var mcallEn = m_ConditionExpress as MetaCallLinkExpressNode;
                var mnoen = m_ConditionExpress as MetaNewObjectExpressNode;
                if (mcallEn == null && mnoen == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error For in 表达式，应该是个数组形式");
                    return;
                }
                if( mcallEn != null )
                {
                    m_ForInContent = mcallEn.GetReturnMetaVariable();
                }
                else
                {
                    m_ForInContent = new MetaVariable("forcontent_" + GetHashCode().ToString(), MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, ownerMetaClass, mnoen.GetReturnMetaType() );
                    m_ThenMetaStatements.UpdateMetaVariableDict(m_ForInContent);
                }
                MetaType mdt = m_ForInContent.GetFinalMetaType();
                if ( !m_ForInContent.GetIsCanCanIterate() )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreParseForNotSuppoertIterator, m_Token, "", m_ForInContent.name );
                    return;
                }

                // 从content实现的IIterable<T>接口中取出模板参数T，
                // 用T构造IIterator<T>作为迭代器类型，再从IIterator<T>中取出T作为v的类型
                MetaType iterableInterfaceMT = null;
                MetaClass searchMC = mdt.metaClass;
                if (searchMC != null)
                {
                    MetaClass cur = searchMC;
                    while (cur != null)
                    {
                        foreach (var it in cur.interfaceMetaType)
                        {
                            if (it.GetTemplateMetaClass() == CoreMetaClassManager.iterableMetaClass)
                            {
                                iterableInterfaceMT = it;
                                break;
                            }
                        }
                        if (iterableInterfaceMT != null) break;
                        cur = cur.extendClass;
                    }
                }

                MetaClass iterMT = CoreMetaClassManager.iteratorMetaClass;
                List<MetaType> iteratorTemplateList = new List<MetaType>();
                if (iterableInterfaceMT != null)
                {
                    var iterTplList = iterableInterfaceMT.GetGenTemplateMetaTypeList();
                    if (iterTplList != null && iterTplList.Count > 0)
                    {
                        iteratorTemplateList.Add(iterTplList[0]);
                    }
                    else
                    {
                        // 非泛型IIterable: current()返回object
                        iteratorTemplateList.Add(new MetaType(CoreMetaClassManager.objectMetaClass));
                    }
                }
                else
                {
                    // 回退: 使用content自身的模板参数
                    var contentTplList = mdt.GetGenTemplateMetaTypeList();
                    if (contentTplList != null && contentTplList.Count > 0)
                    {
                        iteratorTemplateList.Add(contentTplList[0]);
                    }
                    else
                    {
                        iteratorTemplateList.Add(new MetaType(CoreMetaClassManager.objectMetaClass));
                    }
                }

                m_ForInContentIterator = new MetaVariable("for_iterator_" + GetHashCode().ToString(),
                    MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, ownerMetaClass,
                    new MetaType( iterMT, iteratorTemplateList ) );
                m_ForInContentIterator.SetToken(m_ForInContent.token);
                m_ThenMetaStatements.UpdateMetaVariableDict(m_ForInContentIterator);

                if ( m_FileMetaKeyForSyntax.fileMetaClassDefine is FileMetaDefineVariableSyntax fmcd )
                {
                    string dname = fmcd.name;
                    var dmv = m_ThenMetaStatements.GetMetaVariableByName(dname);
                    if (dmv != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 在 for .. in 中，不允许从for 外边定义遍历变量!!");
                        return;
                    }
                    else
                    {
                        m_ForIterateVariable = new MetaIteratorVariable(fmcd.fileMetaClassDefine, fmcd.nameToken, ownerMetaClass, m_OwnerMetaBlockStatements, m_ForInContentIterator);
                    }
                }
                else if( m_FileMetaKeyForSyntax.fileMetaClassDefine is FileMetaCallSyntax fmcs )
                {
                    string dname = fmcs.variableRef.name;
                    var dmv = m_ThenMetaStatements.GetMetaVariableByName(dname);
                    if (dmv != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 在 for .. in 中，不允许从for 外边定义遍历变量!!");
                        return;
                    }
                    else
                    {
                        m_ForIterateVariable = new MetaIteratorVariable( null, fmcs.variableRef.callNodeList[0].token, ownerMetaClass, m_OwnerMetaBlockStatements, m_ForInContentIterator);
                    }
                }
                if(m_ForIterateVariable == null )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error For x in X必须有!!");
                    return;
                }
                m_ForIterateVariable.ParseRealMetaType();

                //m_HasNextFunction = m_ForIterateVariable.realMetaType.metaClass.GetFirstMetaMemberFunctionByName("hasNext");
                //m_NextValueFunction = m_ForIterateVariable.realMetaType.metaClass.GetFirstMetaMemberFunctionByName("current");

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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 没有找到相应的变量!!");
                }
                m_ThenMetaStatements.UpdateMetaVariableDict(m_ForIterateVariable);

                if (m_FileMetaKeyForSyntax.conditionExpress != null)
                {
                    CreateExpressParam cep2 = new CreateExpressParam()
                    {
                        ownerMBS = m_ThenMetaStatements,
                        ownerMetaBase = m_ThenMetaStatements.ownerMetaClass,
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
        public FileMetaConditionExpressSyntax fileMetaKeyWhileSyntax => m_FileMetaKeyWhileSyntax;
        public MetaExpressNodeBase conditionExpress => m_ConditionExpress;
        public MetaBlockStatements thenMetaStatements => m_ThenMetaStatements;
        public bool isWhile => m_IsWhile;

        private FileMetaConditionExpressSyntax m_FileMetaKeyWhileSyntax = null;
        private MetaExpressNodeBase m_ConditionExpress = null;
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
            m_Token = m_FileMetaKeyWhileSyntax?.token;
            AddPingToken(m_Token);

            m_ThenMetaStatements = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyWhileSyntax.executeBlockSyntax);
            m_ThenMetaStatements.SetOwnerMetaStatements(this);

            if (m_FileMetaKeyWhileSyntax.conditionExpress != null)
            {
                MetaType mdt = new MetaType(CoreMetaClassManager.booleanMetaClass);

                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaKeyWhileSyntax.conditionExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress
                };
                m_ConditionExpress = ExpressManager.CreateExpressNode(cep2);
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error while/dowhile 缺少条件表达式");
            }

            MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyWhileSyntax.executeBlockSyntax, m_ThenMetaStatements );

            if (m_ConditionExpress != null)
            {
                AllowUseSettings auc = new AllowUseSettings();
                m_ConditionExpress.Parse(auc);
                m_ConditionExpress.CalcReturnType();
            }
        }
        public override void SetDeep(int dp)
        {
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
            sb.Append(m_IsWhile ? "while " : "dowhile ");
            sb.Append(m_ConditionExpress?.ToFormatString());
            sb.Append(Environment.NewLine);

            if (m_ThenMetaStatements != null)
            {
                sb.Append(m_ThenMetaStatements.ToFormatString());
                sb.Append(Environment.NewLine);
            }

            sb.Append(nextMetaStatements?.ToFormatString());
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
}
