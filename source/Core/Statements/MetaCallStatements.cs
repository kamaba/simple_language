//****************************************************************************
//  File:      MetaCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaCallStatements : MetaStatements
    {
        public MetaCallLink metaCallLink => m_MetaCallLink;

        private MetaCallLink m_MetaCallLink = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;
        private AllowUseConst m_AllowUseConst = new AllowUseConst();
        public MetaCallStatements(MetaBlockStatements mbs, FileMetaCallSyntax fmcl) : base(mbs)
        {
            m_FileMetaCallSyntax = fmcl;

            m_AllowUseConst.useNotStatic = false;
            m_AllowUseConst.useNotConst = false;
            m_AllowUseConst.callConstructFunction = true;
            m_AllowUseConst.callFunction = true;

            m_MetaCallLink = new MetaCallLink( fmcl.variableRef, mbs.ownerMetaClass, mbs );
            m_MetaCallLink.Parse(m_AllowUseConst);
            m_MetaCallLink.CalcReturnType();
            var metaFunCall = m_MetaCallLink.metaFunctionCall;
            if (metaFunCall == null)
            {
                return;
            }
            if (!(metaFunCall.function is MetaMemberFunction))
            {
                return;

            }
            var conFun = metaFunCall.function as MetaMemberFunction;
            if (!conFun.isConstructInitFunction)
            {
                return;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_MetaCallLink?.ToFormatString());
            sb.Append(Environment.NewLine);
            if (nextMetaStatements != null)
            {
                sb.Append(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();
        }
    }
}
