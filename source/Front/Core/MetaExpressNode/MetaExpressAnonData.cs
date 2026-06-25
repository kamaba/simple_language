//****************************************************************************
//  File:      MetaExpressAnonData.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/11/12 12:00:00
//  Description: 匿名 data 字面量：字段表达式 → 规范 MetaData → MetaNewObjectExpressNode
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaAnonDataExpressNode : MetaExpressNodeBase
    {
        public MetaData metaData => m_MetaData;

        private MetaVariable m_ReturnMetaVariable = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private FileMetaMemberData m_FileMetaMemberData = null;
        private MetaData m_MetaData = null;

        public MetaAnonDataExpressNode(FileMetaBraceTerm fmbt, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable mv = null)
        {
            m_FileMetaBraceTerm = fmbt;
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_ReturnMetaVariable = mv;
            m_Token = fmbt.token;

            m_MetaData = new MetaData("AnonData_" + m_Token?.ToLexemeAllString() + "_" + m_ReturnMetaVariable?.name, false, false, true);
            m_MetaData.AddPingToken(m_Token);

            for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
            {
                MetaMemberData mmd = new MetaMemberData(m_MetaData, m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i], ownerMC, mbs, i);
                mmd.CreateMetaExpress();
                m_MetaData.AddMetaMemberData(mmd);
            }
        }

        /// <summary>由 <see cref="FileMetaMemberData"/> 嵌套 data 块构造（data 成员初始化中的 <c>{ ... }</c>）。</summary>
        public MetaAnonDataExpressNode(FileMetaMemberData fmmd, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable mv = null)
        {
            m_FileMetaMemberData = fmmd;
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_ReturnMetaVariable = mv;
            m_Token = fmmd.nameToken ?? fmmd.token;

            m_MetaData = new MetaData("AnonData_" + m_Token?.ToLexemeAllString() + "_" + m_ReturnMetaVariable?.name, false, false, true);
            m_MetaData.AddPingToken(m_Token);

            for (int i = 0; i < fmmd.fileMetaMemberData.Count; i++)
            {
                var child = fmmd.fileMetaMemberData[i];
                MetaMemberData mmd = new MetaMemberData(m_MetaData, child, m_MetaData, i, false);
                mmd.CreateMetaExpress();
                m_MetaData.AddMetaMemberData(mmd);
            }
        }

        public override void Parse(AllowUseSettings auc)
        {
            if (m_Parsed) return;
            foreach (var v in m_MetaData.metaMemberDataDict)
            {
                v.Value.ParseMetaExpress();
            }
            m_Parsed = true;
        }

        //public override int CalcParseLevel(int level)
        //{
        //    return level;
        //}

        public override void CalcReturnType()
        {
            if (m_ExpressReturnMetaType != null) return;
            if (m_MetaData != null)
            {
                foreach (var v in m_MetaData.metaMemberDataDict)
                {
                    v.Value.ParseRealMetaType();
                }
                m_ExpressReturnMetaType = new MetaType(m_MetaData);
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "not found metadata");
            }
        }

        public override string ToFormatString()
        {
            if (m_MetaData != null)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var v in m_MetaData.metaMemberDataDict)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    sb.Append(v.Value.ToFormatString(true));
                }
                sb.Append("}");
                return "ExpressAnonData" + sb.ToString();
            }
            return "{}";
        }
        public override string ToString()
        {
            var sb = new StringBuilder();            
            sb.Append("{");
            bool first = true;
            foreach (var v in m_MetaData.metaMemberDataDict)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append(v.Value.ToFormatString(true));
            }
            sb.Append("}");
            return "ExpressAnonSchemaData" + m_MetaData.allName + sb.ToString();
        }
    }
}
