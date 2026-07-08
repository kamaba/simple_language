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
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaAsIsExpressNode : MetaExpressNodeBase
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

        public static MetaAsIsExpressNode CreateMetaExecuteStatementsNode(MetaType mdt, MetaBase ownerMC, MetaBlockStatements mbs, FileMetaAsOrIsTerm asisTerm, MetaVariable retMv )
        {
            MetaAsIsExpressNode maien = new MetaAsIsExpressNode(ownerMC, mbs, asisTerm, retMv );

            return maien;
        }
        public MetaAsIsExpressNode(MetaBase ownerMC, MetaBlockStatements mbs, FileMetaAsOrIsTerm fm, MetaVariable mv = null) 
        {
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaKeyAsIsSyntax = fm;
            m_ReturnMetaVariable = mv;
            Parse();
        }
        private void Parse()
        {
            if (m_FileMetaKeyAsIsSyntax == null)
            {
                Debug.Write("Error ??As??!!");
            }
            m_IsAs = m_FileMetaKeyAsIsSyntax.isAsTerm;
            m_IsIsNot = m_FileMetaKeyAsIsSyntax.isIsNotTerm;

            if (m_FileMetaKeyAsIsSyntax.variableCallLink == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "????????");
                return;
            }
            m_CurrentVariableLink = new MetaCallLink(m_FileMetaKeyAsIsSyntax.variableCallLink, m_OwnerMetaBase, m_OwnerMetaBlockStatements, null, null);

            if ( m_FileMetaKeyAsIsSyntax.defineType == null )
            {
                Debug.Assert(false, "?????????");
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "????????");
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
                //    Log.AddMetaCoreLog(LID.ShowExtendMessage, "???????????");
                //    return;
                //}
            }

            if(m_FileMetaKeyAsIsSyntax.defineType != null )
            {
                m_ConvertTargetMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, m_FileMetaKeyAsIsSyntax.defineType );

                var sourceMetaType = m_CurrentVariableLink?.GetMetaType();
                if (sourceMetaType == null)
                {
                    var sourceMv = m_CurrentVariableLink?.GetReturnMetaVariable();
                    sourceMetaType = sourceMv?.GetFinalMetaType();
                }

                if (sourceMetaType != null && m_ConvertTargetMetaType != null)
                {
                    var forward = TypeManager.ResolveTypeRelation(m_ConvertTargetMetaType, sourceMetaType, out _, out _);
                    var backward = TypeManager.ResolveTypeRelation(sourceMetaType, m_ConvertTargetMetaType, out _, out _);
                    if (!forward.IsAcceptableForAsIs() && !backward.IsAcceptableForAsIs())
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaKeyAsIsSyntax.asOrIsToken,
                            $" {sourceMetaType.ToString() } as/is {m_ConvertTargetMetaType.ToString() } ");
                    }
                }

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
            m_ParsedState = EParseState.ParseSuccess;
        }

        //public override int CalcParseLevel(int level)
        //{
        //    return 0;
        //}
        public override void CalcReturnType()
        {
            m_ExpressReturnMetaType = m_ConvertTargetMetaType;
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
    }
}
