//****************************************************************************
//  File:      MetaNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaDefineVarStatements : MetaStatements
    {
        public MetaExpressNodeBase expressNode => m_ExpressNode;
        public MetaVariable defineVarMetaVariable => m_DefineVarMetaVariable;

        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;

        private MetaVariable m_DefineVarMetaVariable = null;
        private MetaExpressNodeBase m_ExpressNode = null;
        public MetaDefineVarStatements( MetaBlockStatements mbs ) : base(mbs)
        {
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaDefineVariableSyntax fmdvs ) : base( mbs )
        {
            m_FileMetaDefineVariableSyntax = fmdvs;
            m_Name = fmdvs.name;
            m_Token = fmdvs.nameToken;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaOpAssignSyntax fmoas ): base( mbs )
        {
            m_FileMetaOpAssignSyntax = fmoas;
            m_Token = fmoas.token;
            m_Name = m_FileMetaOpAssignSyntax.variableRef.name;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaDefineVarStatements( MetaBlockStatements mbs, FileMetaCallSyntax callSyntax ):base( mbs )
        {
            m_FileMetaCallSyntax = callSyntax;
            m_Name = callSyntax.variableRef.name;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);
            Parse();
        }
        private void Parse()
        {
            string defineName = m_Name;
            MetaType leftMt = null;
            var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;

            bool isSynamicData = false;
            FileMetaBaseTerm fileExpress = null;
            if ( m_FileMetaDefineVariableSyntax != null )
            {
                var fmcd = m_FileMetaDefineVariableSyntax.fileMetaClassDefine;
                leftMt = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, fmcd);
                if(leftMt == null )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreNotFoundMetaTypeByFMClassDefine, fmcd.classNameToken, "DefineStatements", fmcd.name );
                    return;
                }

                if(leftMt.metaClass is MetaGenTemplateClass mgtc )
                {
                    mgtc.ParseGenTemplateClass(mgtc);
                    mgtc.ParseGenMemberVarible();
                }

                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt );
                m_DefineVarMetaVariable.SetIsDefineMetaType(true);
                m_DefineVarMetaVariable.SetIsConst(m_FileMetaDefineVariableSyntax.constToken != null);
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaDefineVariableSyntax.token);
                fileExpress = m_FileMetaDefineVariableSyntax.express;

                Node node = new Node(m_Token);
                FileMetaCallNode fmcn = new FileMetaCallNode(m_FileMetaDefineVariableSyntax.fileMeta, node);
                MetaCallNode mcn = new MetaCallNode( null, fmcn, ownerMetaBase, m_OwnerMetaBlockStatements, leftMt);
                mcn.SetAllowUseSettings(new AllowUseSettings());
                mcn.SetToken(m_Token);
                mcn.GetFirstNode(m_Name, ownerMetaBase, 0);
                if (mcn.callNodeType != ECallNodeType.None)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, $"名称{m_Name}与{mcn.callNodeType} 有重复");
                    return;
                }
            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt);
                m_DefineVarMetaVariable.SetIsConst(m_FileMetaOpAssignSyntax.constToken != null);
                m_Token = m_FileMetaOpAssignSyntax.variableRef.callNodeList[0].token;
                AddPingToken(m_Token);
                m_DefineVarMetaVariable.AddPingToken(m_Token);
                //if ( m_FileMetaOpAssignSyntax.dynamicToken != null )
                //{
                //    isDynamicClass = true;
                //    leftMt = null;// new MetaType(CoreMetaClassManager.dynamicMetaClass);
                //}
                //else 
                    if( m_FileMetaOpAssignSyntax.dataToken != null )
                {
                    isSynamicData = true;
                    leftMt = new MetaType(CoreMetaClassManager.dynamicMetaData );
                }
                if (m_FileMetaOpAssignSyntax.variableRef != null)
                {
                    if( m_FileMetaOpAssignSyntax.variableRef.callNodeList.Count != 1 )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "call node list is not count equal 1");
                        return;
                    }
                    MetaCallNode mcn = new MetaCallNode( null, m_FileMetaOpAssignSyntax.variableRef.callNodeList[0],ownerMetaBase, m_OwnerMetaBlockStatements, leftMt);
                    mcn.SetAllowUseSettings(new AllowUseSettings());
                    mcn.SetToken(m_Token);
                    mcn.GetFirstNode(m_Name, ownerMetaBase, 0);
                    if( mcn.callNodeType != ECallNodeType.None )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, $"名称{m_Name}与{mcn.callNodeType } 有重复");
                        return;
                    }
                }

                fileExpress = m_FileMetaOpAssignSyntax.express;
            }
            else if (m_FileMetaCallSyntax!= null )
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt);
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaCallSyntax.token);
            }
            if(m_DefineVarMetaVariable == null )
            {
                Log.AddMetaCoreLog( LID.MetaCoreDefineVariableParseIsNull, m_Token, "" +  defineName);
                return;
            }
            m_OwnerMetaBlockStatements.UpdateMetaVariableDict(m_DefineVarMetaVariable);

            MetaType expressRetMetaDefineType = null;
            if (fileExpress != null)
            {
                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fileExpress;
                cep.equalMetaVariable = m_DefineVarMetaVariable;
                cep.metaType = leftMt;
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass;

                m_ExpressNode = ExpressManager.CreateExpressNodeByCEP(cep);
                if (m_ExpressNode == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 解析新建变量语句时，表达式解析为空!!__1");
                    return;
                }
                m_ExpressNode.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_ExpressNode.CalcReturnType();

                m_ExpressNode = ExpressManager.ConvertNewExpress(m_ExpressNode, m_ExpressNode.GetReturnMetaType(), m_DefineVarMetaVariable );
                expressRetMetaDefineType = m_ExpressNode.GetReturnMetaType();        
            }

            if (expressRetMetaDefineType == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 解析新建变量语句时，表达式返回类型为空!!__2", defineName);
                return;
            }
            if (!m_DefineVarMetaVariable.isDefineMetaType )
            {
                m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
            }
            else
            {
                if (TypeManager.CompareLeftRightMetaType(m_DefineVarMetaVariable.defineMetaType, expressRetMetaDefineType, m_Token,
                            out var convertMetaType))
                {
                    if (convertMetaType != null)
                    {
                        m_DefineVarMetaVariable.SetRealMetaType(convertMetaType);
                    }
                    else
                    {
                        m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 表达式中返回定义类型为空 " );
                }
            }
            SetTRMetaVariable(m_DefineVarMetaVariable);
        }        
        public override void SetTRMetaVariable(MetaVariable mv)
        {
            if(m_ExpressNode != null && m_ExpressNode is MetaExecuteStatementsNode )
            {
                (m_ExpressNode as MetaExecuteStatementsNode).UpdateTrMetaVariable(mv);
            }
            if (nextMetaStatements != null)
            {
                nextMetaStatements.SetTRMetaVariable(mv);
            }
        }
        //public override MetaStatements GenTemplateClassStatement(MetaGenTemplateClass mgt, MetaBlockStatements parentMs)
        //{
        //    MetaDefineVarStatements mns = new MetaDefineVarStatements(parentMs);
        //    mns.m_FileMetaDefineVariableSyntax = m_FileMetaDefineVariableSyntax;
        //    mns.m_FileMetaOpAssignSyntax = m_FileMetaOpAssignSyntax;
        //    mns.m_FileMetaCallSyntax = m_FileMetaCallSyntax;
        //    mns.m_IsNeedCastStatements = m_IsNeedCastStatements;
        //    mns.m_DefineVarMetaVariable = new MetaVariable(m_DefineVarMetaVariable);
        //    mns.m_ExpressNode = m_ExpressNode;
        //    mns.m_DefineVarMetaVariable.GenTemplateMetaVaraible( mgt, parentMs );
        //    if (m_NextMetaStatements != null)
        //    {
        //        m_NextMetaStatements.GenTemplateClassStatement(mgt, parentMs);
        //    }
        //    return mns;
        //}
        public override void SetDeep(int dp)
        {
            base.SetDeep(dp);
            if (m_ExpressNode is MetaExecuteStatementsNode)
            {
                (m_ExpressNode as MetaExecuteStatementsNode).SetDeep(dp);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_DefineVarMetaVariable.ToFormatString());
            sb.Append(" = ");
            if (m_DefineVarMetaVariable.defineMetaType.isData)
            {
                sb.Append(m_ExpressNode.ToFormatString());
            }
            else if (m_DefineVarMetaVariable.defineMetaType.isEnum)
            {
            }
            else
            {
                if (m_IsNeedCastState)
                {
                    sb.Append("(");
                }
                sb.Append(m_ExpressNode?.ToFormatString());
                if (m_IsNeedCastState)
                {
                    sb.Append(").cast<" + m_DefineVarMetaVariable.defineMetaType.metaClass.allName + ">()");
                }
                sb.Append(";");
            }

            if (nextMetaStatements != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();

        }
    }
}
