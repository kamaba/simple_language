//****************************************************************************
//  File:      MetaExpressAsOrIs.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/11/12 12:00:00
//  Description:  
//****************************************************************************

using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public sealed class MetaAsIsExpressNode : MetaExpressNode
    {
        public MetaVariable currentVariable => m_CurrentVariable;
        public MetaVariable convertTargetMetaVariable => m_ConvertTargetMetaVariable;
        public MetaType convertTargetMetaType => m_ConvertTargetMetaType;
        public bool isAs => m_IsAs;
        public FileMetaAsOrIsTerm fileMetaKeyAsIsSyntax => m_FileMetaKeyAsIsSyntax;

        private FileMetaAsOrIsTerm m_FileMetaKeyAsIsSyntax = null;
        private MetaVariable m_ReturnMetaVariable = null;
        private MetaVariable m_CurrentVariable = null;
        private MetaType m_ConvertTargetMetaType = null;
        private MetaVariable m_ConvertTargetMetaVariable = null;

        private MetaCallLink m_CurrentVariableLink = null;
        private MetaCallLink m_ConvertTargetMetaTypeLink = null;
        private FileMetaSymbolTerm m_FileMetaBaseTerm = null;
        private bool m_IsAs = false;

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

            if (m_FileMetaKeyAsIsSyntax.variableCallLink == null)
            {
                Log.AddInStructMeta(EError.None, "定义当前变量错误");
                return;
            }
            m_CurrentVariableLink = new MetaCallLink(m_FileMetaKeyAsIsSyntax.variableCallLink, m_OwnerMetaClass, m_OwnerMetaBlockStatements, null, null);

            if ( m_FileMetaKeyAsIsSyntax.defineTypeLink == null )
            {
                Log.AddInStructMeta(EError.None, "定义的类型不正确");
                return;
            }
            m_ConvertTargetMetaTypeLink = new MetaCallLink(m_FileMetaKeyAsIsSyntax.defineTypeLink, m_OwnerMetaClass, m_OwnerMetaBlockStatements, null, null);
        }
        public override void Parse(AllowUseSettings auc)
        {
            if(m_CurrentVariableLink != null )
            {
                m_CurrentVariableLink.Parse(auc);
                m_CurrentVariable = m_CurrentVariableLink.ExecuteGetMetaVariable();
                if( m_CurrentVariable == null )
                {
                    Log.AddInStructMeta(EError.None, "没有找到相关的转化对象");
                    return;
                }
            }

            if(m_ConvertTargetMetaTypeLink != null )
            {
                m_ConvertTargetMetaTypeLink.Parse(auc);
                m_ConvertTargetMetaType = m_ConvertTargetMetaTypeLink.GetMetaDefineType();

                if (m_ConvertTargetMetaVariable == null )                    
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
            m_MetaDefineType = m_ConvertTargetMetaType;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_CurrentVariable.name + " ");
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
