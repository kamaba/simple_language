//****************************************************************************
//  File:      MetaExpressAsOrIs.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/11/12 12:00:00
//  Description:  
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaAsIsExpressNode : MetaExpressNode
    {
        public MetaCallLink currentVariableLink => m_CurrentVariableLink;
        public MetaVariable convertTargetMetaVariable => m_ConvertTargetMetaVariable;
        public MetaType convertTargetMetaType => m_ConvertTargetMetaType;
        public bool isAs => m_IsAs;
        public bool isIsNot => m_IsIsNot;
        public FileMetaAsOrIsTerm fileMetaKeyAsIsSyntax => m_FileMetaKeyAsIsSyntax;

        private FileMetaAsOrIsTerm m_FileMetaKeyAsIsSyntax = null;
        private MetaVariable m_ReturnMetaVariable = null;
        //private MetaVariable m_CurrentVariable = null;
        private MetaType m_ConvertTargetMetaType = null;
        private MetaVariable m_ConvertTargetMetaVariable = null;

        private MetaCallLink m_CurrentVariableLink = null;
        private FileMetaSymbolTerm m_FileMetaBaseTerm = null;
        private bool m_IsAs = false;
        private bool m_IsIsNot = false;

        public static MetaAsIsExpressNode CreateMetaExecuteStatementsNode(MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, FileMetaAsOrIsTerm asisTerm, MetaVariable retMv )
        {
            MetaAsIsExpressNode maien = new MetaAsIsExpressNode(ownerMC, mbs, asisTerm, retMv );

            return maien;
        }
        public MetaAsIsExpressNode(MetaClass ownerMC, MetaBlockStatements mbs, FileMetaAsOrIsTerm fm, MetaVariable mv = null) 
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaKeyAsIsSyntax = fm;
            m_ReturnMetaVariable = mv;
            Parse();
        }
        private void Parse()
        {
            if (m_FileMetaKeyAsIsSyntax == null)
            {
                Debug.Write("Error 没有As语句!!");
            }
            m_IsAs = m_FileMetaKeyAsIsSyntax.isAsTerm;
            m_IsIsNot = m_FileMetaKeyAsIsSyntax.isIsNotTerm;

            if (m_FileMetaKeyAsIsSyntax.variableCallLink == null)
            {
                Log.AddInStructMeta(EError.None, "定义当前变量错误");
                return;
            }
            m_CurrentVariableLink = new MetaCallLink(m_FileMetaKeyAsIsSyntax.variableCallLink, m_OwnerMetaClass, m_OwnerMetaBlockStatements, null, null);

            if ( m_FileMetaKeyAsIsSyntax.defineType == null )
            {
                Debug.Assert(false, "没有定义转换的类型");
                Log.AddInStructMeta(EError.None, "定义的类型不正确");
                return;
            }
            m_ConvertTargetMetaType = null;
        }
        public override void Parse(AllowUseSettings auc)
        {
            if(m_CurrentVariableLink != null )
            {
                m_CurrentVariableLink.Parse(auc);
                //m_CurrentVariable = m_CurrentVariableLink.ExecuteGetMetaVariable();
                //if( m_CurrentVariable == null )
                //{
                //    Log.AddInStructMeta(EError.None, "没有找到相关的转化对象");
                //    return;
                //}
            }

            if(m_FileMetaKeyAsIsSyntax.defineType != null )
            {
                m_ConvertTargetMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, m_FileMetaKeyAsIsSyntax.defineType );

                if (m_ConvertTargetMetaVariable == null && isAs == false )                    
                {
                    string nametoken = this.GetHashCode() + "_auto_cast_target_mv";
                    if(m_FileMetaKeyAsIsSyntax.convertIsTypeNameToken != null)
                    {
                        nametoken = m_FileMetaKeyAsIsSyntax.convertIsTypeNameToken.lexeme.ToString();
                    }
                    m_ConvertTargetMetaVariable = new MetaVariable(nametoken, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, m_ConvertTargetMetaType);
                    m_ConvertTargetMetaVariable.SetIsDefineMetaType(true);
                    m_OwnerMetaBlockStatements.AddMetaVariable(m_ConvertTargetMetaVariable);
                }
            }
        }
        public override int CalcParseLevel(int level)
        {
            return 0;
        }
        public override void CalcReturnType()
        {
            m_MetaType = m_ConvertTargetMetaType;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_CurrentVariableLink.ToFormatString() + " ");
            sb.Append(m_FileMetaKeyAsIsSyntax.asOrIsToken.lexeme.ToString() + " ");
            sb.Append(m_ConvertTargetMetaType.ToFormatString() + " ");
            if(m_ConvertTargetMetaVariable != null )
            {
                sb.Append(m_ConvertTargetMetaVariable.name + " ");
            };

            return sb.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(m_Left?.ToTokenString());
            //if (m_SignToken != null)
            //{
            //    sb.Append(m_SignToken.lexeme?.ToString());
            //}
            //else
            //{
            ////    sb.Append(GetSignString(m_OpLevelSign));
            //}
            //sb.Append(m_Right?.ToTokenString());
            return sb.ToString();
        }
    }
}
