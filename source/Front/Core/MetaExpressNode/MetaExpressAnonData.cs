//****************************************************************************
//  File:      MetaExpressAnonData.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/11/12 12:00:00
//  Description: 匿名 data 字面量：字段表达式 → 规范 MetaData → MetaNewObjectExpressNode
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaAnonDataExpressNode : MetaExpressNodeBase
    {
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaBraceTerm;
        public MetaData schemaMetaData => m_SchemaMetaData;
        public MetaData canonicalMetaData => m_CanonicalMetaData;

        private MetaVariable m_ReturnMetaVariable = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private FileMetaMemberData m_FileMetaMemberData = null;
        private MetaData m_SchemaMetaData = null;
        private MetaData m_CanonicalMetaData = null;

        public MetaAnonDataExpressNode(FileMetaBraceTerm fmbt, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable mv = null)
        {
            m_FileMetaBraceTerm = fmbt;
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_ReturnMetaVariable = mv;
            m_Token = fmbt.token;

            m_SchemaMetaData = new MetaData("AnonData_" + fmbt.ToString() + "_" + m_ReturnMetaVariable?.name, false, false, true);
            m_SchemaMetaData.AddPingToken(m_Token);

            for (int i = 0; i < fileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
            {
                MetaMemberData mmd = new MetaMemberData(m_SchemaMetaData, fileMetaBraceTerm.fileMetaAssignSyntaxList[i], ownerMC, mbs);
                m_SchemaMetaData.AddMetaMemberData(mmd);
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

            m_SchemaMetaData = new MetaData("AnonData_" + m_Token?.lexeme + "_" + GetHashCode(), false, false, true);
            m_SchemaMetaData.AddPingToken(m_Token);

            for (int i = 0; i < fmmd.fileMetaMemberData.Count; i++)
            {
                var child = fmmd.fileMetaMemberData[i];
                MetaMemberData mmd = new MetaMemberData(m_SchemaMetaData, child, i, false);
                m_SchemaMetaData.AddMetaMemberData(mmd);
            }
        }

        public override void Parse(AllowUseSettings auc)
        {
            foreach (var v in m_SchemaMetaData.metaMemberDataDict)
            {
                v.Value.CreateMetaExpress();
                v.Value.ParseMetaExpress();
                v.Value.ParseRealMetaType();
            }
        }

        public override int CalcParseLevel(int level)
        {
            return level;
        }

        public override void CalcReturnType()
        {
            m_CanonicalMetaData = MetaData.ResolveCanonicalAnonymousType(
                m_SchemaMetaData.GetMetaMemberDataList(),
                m_OwnerMetaBase,
                m_ReturnMetaVariable?.name);
            if (m_CanonicalMetaData != null)
            {
                m_CanonicalMetaData.SetToken(m_SchemaMetaData.token);
                m_ExpressReturnMetaType = new MetaType(m_CanonicalMetaData);
            }
            else
            {
                m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
        }

        public override string ToFormatString()
        {
            if (m_SchemaMetaData != null)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var v in m_SchemaMetaData.metaMemberDataDict)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    sb.Append(v.Value.ToFormatString(true));
                }
                sb.Append("}");
                return sb.ToString();
            }
            return "{}";
        }
    }
}
