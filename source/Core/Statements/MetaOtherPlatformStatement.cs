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
    public class MetaOtherPlatformStatements : MetaStatements
    {
        public MetaOtherPlatformStatements(MetaBlockStatements mbs) : base(mbs)
        {
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
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