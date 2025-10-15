//****************************************************************************
//  File:      MetaNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using System;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile;
using System.Diagnostics;

namespace SimpleLanguage.Core.Statements
{
    public class MetaDefineVarStatements : MetaStatements
    {
        public MetaExpressNode expressNode => m_ExpressNode;
        public MetaVariable defineVarMetaVariable => m_DefineVarMetaVariable;

        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;

        private MetaVariable m_DefineVarMetaVariable = null;
        private MetaExpressNode m_ExpressNode = null;
        private bool m_IsNeedCastStatements = false;
        public MetaDefineVarStatements( MetaBlockStatements mbs ) : base(mbs)
        {
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaDefineVariableSyntax fmdvs ) : base( mbs )
        {
            m_FileMetaDefineVariableSyntax = fmdvs;
            m_Name = fmdvs.name;            
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaOpAssignSyntax fmoas ): base( mbs )
        {
            m_FileMetaOpAssignSyntax = fmoas;
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
            MetaType mdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;

#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isDynamicClass = false;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            FileMetaBaseTerm fileExpress = null;
            if ( m_FileMetaDefineVariableSyntax != null )
            {
                var fmcd = m_FileMetaDefineVariableSyntax.fileMetaClassDefine;
                mdt = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, fmcd);
                if( mdt.metaClass is MetaGenTemplateClass mgtc )
                {
                    mgtc.Parse();
                }

                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaDefineVariableSyntax.token);
                fileExpress = m_FileMetaDefineVariableSyntax.express;
            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                Token token = m_FileMetaOpAssignSyntax.assignToken;
                if( m_FileMetaOpAssignSyntax.dynamicToken != null )
                {
                    isDynamicClass = true;  
                }
                if(m_FileMetaOpAssignSyntax.variableRef != null )
                {
                    foreach( var v in  m_FileMetaOpAssignSyntax.variableRef.callNodeList)
                    {
                        m_DefineVarMetaVariable.AddPingToken(v.token);
                    }
                }
                m_DefineVarMetaVariable.AddPingToken(token);

                fileExpress = m_FileMetaOpAssignSyntax.express;
            }
            else if (m_FileMetaCallSyntax!= null )
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaCallSyntax.token);
            }
            if(m_DefineVarMetaVariable == null )
            {
                Debug.Write("Error {0} MetaVariable is Null", defineName);
                return;
            }
            m_OwnerMetaBlockStatements.UpdateMetaVariableDict(m_DefineVarMetaVariable);

            MetaType expressRetMetaDefineType = null;
            if (fileExpress != null)
            {
                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fileExpress;
                cep.equalMetaVariable = m_DefineVarMetaVariable;
                cep.metaType = mdt;
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass;

                m_ExpressNode = ExpressManager.CreateExpressNodeByCEP(cep);
                if (m_ExpressNode == null)
                {
                    Debug.WriteLine("Error 解析新建变量语句时，表达式解析为空!!__1");
                    return;
                }
                m_ExpressNode.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_ExpressNode.CalcReturnType();
                expressRetMetaDefineType = m_ExpressNode.GetReturnMetaDefineType();               
                if (expressRetMetaDefineType == null)
                {
                    Debug.WriteLine("Error 解析新建变量语句时，表达式返回类型为空!!__2", defineName);
                    return;
                }
            }

            if (!mdt.isDefineMetaClass)
            {
                MetaConstExpressNode constExpressNode = m_ExpressNode as MetaConstExpressNode;
                bool isCheckReturnType = true;
                if (constExpressNode != null)
                {
                    if (constExpressNode.eType == EType.Null)
                    {
                        isCheckReturnType = false;
                    }
                }
                if (isCheckReturnType)
                {
                    m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
                }
            }
            else
            {
                if (m_ExpressNode != null)
                {
                    ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                    MetaConstExpressNode constExpressNode = m_ExpressNode as MetaConstExpressNode;
                    MetaClass curClass = mdt.metaClass;
                    if( mdt.isEnum )
                    {
                        if(m_ExpressNode != null )
                        {
                            MetaCallLinkExpressNode expressMDT = m_ExpressNode as MetaCallLinkExpressNode;
                            if (expressMDT == null )
                            {
                                Debug.WriteLine("Error Enum模式，只允许是调用模式[CallLinkExpress]");
                            }
                            else
                            {
                                var varableEnum = expressMDT.metaCallLink.finalCallNode.variable.ownerMetaClass;
                                if( mdt.metaClass != varableEnum )
                                {
                                    Debug.Write("Error Enum与值不相等!!");
                                }
                                else
                                {
                                    m_IsNeedCastState = false;
                                }
                            }
                        }
                    }
                    //else if( mdt.isData )
                    //{

                    //}
                    else
                    {
                        MetaClass compareClass = null;
                        if (constExpressNode != null && constExpressNode.eType == EType.Null)
                        {
                            relation = ClassManager.EClassRelation.Same;
                        }
                        else
                        {
                            compareClass = expressRetMetaDefineType.metaClass;
                            relation = ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
                        }

                        StringBuilder sb = new StringBuilder();
                        sb.Append("Warning 在类: " + metaFunction?.ownerMetaClass.allClassName + " 函数: " + metaFunction?.name + "中  ");
                        if (curClass != null)
                        {
                            sb.Append(" 定义类 : " + curClass.allClassName );
                        }
                        if (defineName != null)
                        {
                            sb.Append(" 名称为: " + defineName?.ToString());
                        }
                        sb.Append("与后边赋值语句中 ");
                        if (compareClass != null)
                            sb.Append("表达式类为: " + compareClass.allClassName );
                        if (relation == ClassManager.EClassRelation.No)
                        {
                            sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                            Debug.Write(sb.ToString());
                            m_IsNeedCastState = true;
                        }
                        else if (relation == ClassManager.EClassRelation.Same)
                        {
                            m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
                        }
                        else if (relation == ClassManager.EClassRelation.Parent)
                        {
                            sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                            Debug.Write(sb.ToString());
                            m_IsNeedCastState = true;
                        }
                        else if (relation == ClassManager.EClassRelation.Child)
                        {
                            if (compareClass != null)
                            {
                                m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
                            }
                        }
                        else
                        {
                            sb.Append("表达式错误，或者是定义类型错误");
                            Debug.Write(sb.ToString());
                        }
                    }
                }
            }
            if (m_ExpressNode == null)
            {
                m_ExpressNode = m_DefineVarMetaVariable.metaDefineType.GetDefaultExpressNode();
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
            if (m_DefineVarMetaVariable.metaDefineType.isData)
            {
                sb.Append(m_ExpressNode.ToFormatString());
            }
            else if (m_DefineVarMetaVariable.metaDefineType.isEnum)
            {
            }
            else
            {
                if (m_IsNeedCastState)
                {
                    sb.Append("(");
                }
                sb.Append(m_ExpressNode.ToFormatString());
                if (m_IsNeedCastState)
                {
                    sb.Append(").cast<" + m_DefineVarMetaVariable.metaDefineType.metaClass.allClassName + ">()");
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
