//****************************************************************************
//  File:      MetaNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Net;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaNewStatements : MetaStatements
    {
        public MetaVariable metaVariable => m_MetaVariable;

        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;

        private MetaVariable m_MetaVariable = null;
        private MetaExpressNode m_ExpressNode = null;
        private bool m_IsNeedCastStatements = false;
        public MetaNewStatements( MetaBlockStatements mbs ) : base(mbs)
        {
        }
        public MetaNewStatements(MetaBlockStatements mbs, FileMetaDefineVariableSyntax fmdvs ) : base( mbs )
        {
            m_FileMetaDefineVariableSyntax = fmdvs;
            m_Name = fmdvs.name;            
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaNewStatements(MetaBlockStatements mbs, FileMetaOpAssignSyntax fmoas ): base( mbs )
        {
            m_FileMetaOpAssignSyntax = fmoas;
            m_Name = m_FileMetaOpAssignSyntax.variableRef.name;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaNewStatements( MetaBlockStatements mbs, FileMetaCallSyntax callSyntax ):base( mbs )
        {
            m_FileMetaCallSyntax = callSyntax;
            m_Name = callSyntax.variableRef.name;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);
            Parse();
        }
        public Compile.Token GetToken()
        {
            if (m_FileMetaDefineVariableSyntax != null)
            {
                return m_FileMetaDefineVariableSyntax.token;
            }
            if (m_FileMetaOpAssignSyntax != null)
            {
                return m_FileMetaOpAssignSyntax.assignToken;
            }
            if (m_FileMetaCallSyntax != null)
            {
                return m_FileMetaCallSyntax.token;
            }
            return null;
        }
        private void Parse()
        {
            string defineName = m_Name;
            MetaType mdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;

            FileMetaBaseTerm fileExpress = null;
            if ( m_FileMetaDefineVariableSyntax != null )
            {
                var fmcd = m_FileMetaDefineVariableSyntax.fileMetaClassDefine;
                mdt = new MetaType(fmcd, ownerMetaClass);

                m_MetaVariable = new MetaVariable(m_Name, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                m_MetaVariable.SetFromMetaNewStatementsCreate(this);
                m_OwnerMetaBlockStatements.UpdateMetaVariable(m_MetaVariable);

                fileExpress = m_FileMetaDefineVariableSyntax.express;
            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                m_MetaVariable = new MetaVariable(m_Name, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                m_MetaVariable.SetFromMetaNewStatementsCreate(this);
                m_OwnerMetaBlockStatements.UpdateMetaVariable(m_MetaVariable);

                fileExpress = m_FileMetaOpAssignSyntax.express;
            }
            else if (m_FileMetaCallSyntax!= null )
            {
                m_MetaVariable = new MetaVariable(m_Name, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, mdt );
                m_MetaVariable.SetFromMetaNewStatementsCreate(this);
                m_OwnerMetaBlockStatements.UpdateMetaVariable(m_MetaVariable);
            }
            if(m_MetaVariable == null )
            {
                Console.WriteLine("Error {0} MetaVariable is Null", defineName);
                return;
            }

            MetaType expressRetMetaDefineType = null;
            if (fileExpress != null)
            {
                m_ExpressNode = ExpressManager.CreateExpressNodeInMetaFunctionNewStatementsWithIfOrSwitch(fileExpress, m_OwnerMetaBlockStatements, mdt);
                m_ExpressNode.CalcReturnType();
                expressRetMetaDefineType = m_ExpressNode.GetReturnMetaDefineType();
                if (m_ExpressNode == null)
                {
                    Console.WriteLine("Error 解析新建变量语句时，表达式解析为空!!__1");
                    return;
                }
                if (expressRetMetaDefineType == null)
                {
                    Console.WriteLine("Error 解析新建变量语句时，表达式返回类型为空!!__2");
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
                    m_MetaVariable.SetMetaDefineType(expressRetMetaDefineType);
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
                        MetaNewObjectExpressNode mne = m_ExpressNode as MetaNewObjectExpressNode;
                        if( mne != null )
                        {
                            var expressMDT = mne.GetReturnMetaDefineType();
                            bool isSame = expressMDT.isEnum && expressMDT.metaClass == mdt.metaClass;
                            if( !isSame )
                            {
                                Console.WriteLine("Error Enum与值不相等!!");
                            }
                            else
                            {
                                m_IsNeedCastState = false;
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
                        sb.Append("Warning 在类: " + metaFunction?.ownerMetaClass.allName + " 函数: " + metaFunction?.name + "中  ");
                        if (curClass != null)
                        {
                            sb.Append(" 定义类 : " + curClass.allName);
                        }
                        if (defineName != null)
                        {
                            sb.Append(" 名称为: " + defineName?.ToString());
                        }
                        sb.Append("与后边赋值语句中 ");
                        if (compareClass != null)
                            sb.Append("表达式类为: " + compareClass.allName);
                        if (relation == ClassManager.EClassRelation.No)
                        {
                            sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                            Console.WriteLine(sb.ToString());
                            m_IsNeedCastState = true;
                        }
                        else if (relation == ClassManager.EClassRelation.Same)
                        {
                            m_MetaVariable.SetMetaDefineType(expressRetMetaDefineType);
                        }
                        else if (relation == ClassManager.EClassRelation.Parent)
                        {
                            sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                            Console.WriteLine(sb.ToString());
                            m_IsNeedCastState = true;
                        }
                        else if (relation == ClassManager.EClassRelation.Child)
                        {
                            if (compareClass != null)
                            {
                                m_MetaVariable.SetMetaDefineType(expressRetMetaDefineType);
                            }
                        }
                        else
                        {
                            sb.Append("表达式错误，或者是定义类型错误");
                            Console.WriteLine(sb.ToString());
                        }
                    }
                }
            }
            if (m_ExpressNode == null)
            {
                m_ExpressNode = m_MetaVariable.metaDefineType.GetDefaultExpressNode();
            }
            SetTRMetaVariable(m_MetaVariable);
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
        public override MetaStatements GenTemplateClassStatement(MetaGenTemplateClass mgt, MetaBlockStatements parentMs)
        {
            MetaNewStatements mns = new MetaNewStatements(parentMs);
            mns.m_FileMetaDefineVariableSyntax = m_FileMetaDefineVariableSyntax;
            mns.m_FileMetaOpAssignSyntax = m_FileMetaOpAssignSyntax;
            mns.m_FileMetaCallSyntax = m_FileMetaCallSyntax;
            mns.m_IsNeedCastStatements = m_IsNeedCastStatements;
            mns.m_MetaVariable = new MetaVariable(m_MetaVariable);
            mns.m_ExpressNode = m_ExpressNode;
            mns.m_MetaVariable.GenTemplateMetaVaraible( mgt, parentMs );
            if (m_NextMetaStatements != null)
            {
                m_NextMetaStatements.GenTemplateClassStatement(mgt, parentMs);
            }
            return mns;
        }
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
            sb.Append(m_MetaVariable.ToFormatString());
            sb.Append(" = ");
            if(m_IsNeedCastState )
            {
                sb.Append("(");
            }
            sb.Append(m_ExpressNode.ToFormatString());
            if(m_IsNeedCastState)
            {
                sb.Append(").Cast<" + m_MetaVariable.metaDefineType.metaClass.allName + ">()");
            }
            sb.Append(";");

            if ( nextMetaStatements != null )
            {
                sb.Append(Environment.NewLine);
                sb.Append(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();

        }
    }
}
