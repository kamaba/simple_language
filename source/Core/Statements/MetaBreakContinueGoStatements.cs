//****************************************************************************
//  File:      MetaBreakContinueGoStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBreakStatements : MetaStatements
    {
        private FileMetaKeyOnlySyntax m_FileMetaKeyOnlySyntax;

        private MetaForStatements m_ForStatements = null;
        private MetaWhileDoWhileStatements m_WhileStatements = null;

        public MetaBreakStatements( MetaBlockStatements mbs, FileMetaKeyOnlySyntax fmkos) : base(mbs)
        {
            m_FileMetaKeyOnlySyntax = fmkos;

            var fwd = mbs.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            if( fwd is MetaForStatements )
            {
                m_ForStatements = fwd as MetaForStatements;
            }
            else if( fwd is MetaWhileDoWhileStatements )
            {
                m_WhileStatements = fwd as MetaWhileDoWhileStatements;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("break;");
            return sb.ToString();
        }
    }
    public partial class MetaContinueStatements : MetaStatements
    {
        private FileMetaKeyOnlySyntax m_FileMetaKeyOnlySyntax = null;

        private MetaForStatements m_ForStatements = null;
        private MetaWhileDoWhileStatements m_WhileStatements = null;
        public MetaContinueStatements(MetaBlockStatements mbs, FileMetaKeyOnlySyntax fmkos) : base(mbs)
        {
            m_FileMetaKeyOnlySyntax = fmkos;

            var fwd = mbs.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            if (fwd is MetaForStatements)
            {
                m_ForStatements = fwd as MetaForStatements;
            }
            else if (fwd is MetaWhileDoWhileStatements)
            {
                m_WhileStatements = fwd as MetaWhileDoWhileStatements;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("continue;");
            return sb.ToString();
        }
    }
    public partial class MetaGotoLabelStatements : MetaStatements
    {
        public bool isLabel = true;

        public LabelData labelData;

        public Token labelToken;

        public FileMetaKeyGotoLabelSyntax m_FileMetaKeyGotoLabelSyntax;
        public MetaGotoLabelStatements(MetaBlockStatements mbs, FileMetaKeyGotoLabelSyntax labelSyntax ) : base(mbs)
        {
            m_FileMetaKeyGotoLabelSyntax = labelSyntax;

            labelToken = labelSyntax.labelToken;

            if(m_FileMetaKeyGotoLabelSyntax.token.type == ETokenType.Goto )
            {
                isLabel = false;
            }
            MetaFunction mf = m_OwnerMetaBlockStatements.ownerMetaFunction;
            if (mf != null)
            {
                string labelName = labelToken.lexeme?.ToString();
                labelData = mf.GetLabelDataById(labelName);
                if (labelData == null)
                {
                    if (isLabel)
                    {
                        labelData = mf.AddLabelData(labelName, nextMetaStatements);
                    }
                    else
                    {
                        Console.WriteLine("Error 使用goto跳转，必须在本函数中已有定义!!");
                        return;
                    }
                }
            }
        }
        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            nextMetaStatements?.SetDeep(dp);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append( isLabel ? "label " :  "goto " );
            if(labelData != null )
            {
                sb.Append(labelData.label.ToString() + ";");
            }
            sb.Append(Environment.NewLine);

            sb.Append(nextMetaStatements?.ToFormatString());

            return sb.ToString();
        }
    }
}
