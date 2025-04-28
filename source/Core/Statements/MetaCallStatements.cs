//****************************************************************************
//  File:      MetaCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
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
        private AllowUseSettings m_AllowUseSettings = new AllowUseSettings();
        public MetaCallStatements(MetaBlockStatements mbs, FileMetaCallSyntax fmcl) : base(mbs)
        {
            m_FileMetaCallSyntax = fmcl;

            m_AllowUseSettings.useNotStatic = false;
            m_AllowUseSettings.useNotConst = false;
            m_AllowUseSettings.callConstructFunction = true;
            m_AllowUseSettings.callFunction = true;

            m_MetaCallLink = new MetaCallLink(fmcl.variableRef, mbs.ownerMetaClass, mbs);
            m_MetaCallLink.Parse(m_AllowUseSettings);
            m_MetaCallLink.CalcReturnType();
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
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(m_FileMetaCallLink.ToTokenString());
            return sb.ToString();

        }
    }
}