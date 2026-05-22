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
    public sealed class MetaAnonDataExpressNode : MetaExpressNodeBase
    {
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaBraceTerm;

        private MetaVariable m_ReturnMetaVariable = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private MetaData m_MetaData = null;
        public MetaAnonDataExpressNode(FileMetaBraceTerm fmbt, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable mv = null) 
        {
            m_FileMetaBraceTerm = fmbt;
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_ReturnMetaVariable = mv;
            m_Token = fmbt.token;

            m_MetaData = new MetaData("AnynData_" + fmbt.ToString() + "_" + m_ReturnMetaVariable?.name, false, false, true );

            for (int i = 0; i < fileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
            {
                MetaMemberData mmd = new MetaMemberData(m_MetaData, fileMetaBraceTerm.fileMetaAssignSyntaxList[i], ownerMC, mbs );
                m_MetaData.AddMetaMemberData(mmd);
            }
        }
        public override void Parse(AllowUseSettings auc)
        {
            foreach( var v in m_MetaData.metaMemberDataDict)
            {
                v.Value.CreateMetaExpress();
                v.Value.ParseMetaExpress();
                v.Value.ParseRealMetaType();
            }
        }
        public override int CalcParseLevel(int level)
        {
            return 0;
        }
        public override void CalcReturnType()
        {
            m_MetaData.ParseDefineComplete();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(m_CurrentVariableLink.ToFormatString() + " ");
            //sb.Append(m_FileMetaKeyAsIsSyntax.asOrIsToken.lexeme.ToString() + " ");
            //sb.Append(m_ConvertTargetMetaType.ToFormatString() + " ");
            //if(m_ConvertTargetMetaVariable != null )
            //{
            //    sb.Append(m_ConvertTargetMetaVariable.name + " ");
            //};

            return sb.ToString();
        }
    }
}
