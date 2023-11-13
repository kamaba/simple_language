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
            insNode.nopData.SetDebugInfoByToken( m_FileMetaBlockSyntax.token );
            m_IRStatements.Add(insNode);
        }
        public void ParseAllIRStatements()
        {
            IRNop insNode = new IRNop(this.m_OwnerMetaFunction.irMethod);
            if (m_FileMetaBlockSyntax?.token  != null)
            {
                insNode.nopData.SetDebugInfoByToken( m_FileMetaBlockSyntax.token );
            }
            m_IRStatements.Add(insNode);

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                mbs.ParseIRStatements();
                m_IRStatements.AddRange(mbs.irStatements);
                mbs = mbs.nextMetaStatements;
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#block {");

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                sb.AppendLine(mbs.ToIRString());
                mbs = mbs.nextMetaStatements;
            }
            sb.AppendLine("}#");
            return sb.ToString();
        }
    }
}
