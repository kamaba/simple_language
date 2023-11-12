//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBlockStatements
    {
        public override void ParseIRStatements()
        {
            IRNop insNode = new IRNop(irMethod);
            insNode.nopData.line = m_FileMetaBlockSyntax.token.sourceBeginLine;
            m_IRStatements.Add(insNode);
        }
        public void ParseAllIRStatements()
        {
            IRNop insNode = new IRNop(this.m_OwnerMetaFunction.irMethod);
            if (m_FileMetaBlockSyntax?.token  != null)
            {
                insNode.nopData.line = m_FileMetaBlockSyntax.token.sourceBeginLine;
            }
            m_IRStatements.Add(insNode);

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                mbs.ParseIRStatements();
                mbs = mbs.nextMetaStatements;
            }
        }
        public List<IRData> GetIRDataList()
        {
            List<IRData> irDataList = new List<IRData>();

            foreach( var v in m_IRStatements )
            {
                irDataList.AddRange(v.IRDataList);
            }
            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                foreach (var v in mbs.irStatements)
                {
                    irDataList.AddRange(v.IRDataList);
                }
                mbs = mbs.nextMetaStatements;
            }
            return irDataList;
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#{}#");
            if (nextMetaStatements != null)
            {
                sb.AppendLine( nextMetaStatements.ToIRString() );
            }

            return sb.ToString();
        }
    }
}
