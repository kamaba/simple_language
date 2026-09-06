//****************************************************************************
//  File:      MetaBreakContinueGoStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

using System;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaBreakStatements : MetaStatements
    {
        private FileMetaKeyOnlySyntax m_FileMetaKeyOnlySyntax;

        private MetaForStatements m_ForStatements = null;
        private MetaWhileDoWhileStatements m_WhileStatements = null;

        public MetaBreakStatements(MetaBlockStatements mbs, FileMetaKeyOnlySyntax fmkos) : base(mbs)
        {
            m_FileMetaKeyOnlySyntax = fmkos;
            AddPingToken(fmkos?.token);

            var fwd = mbs.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            if (fwd is MetaForStatements)
            {
                m_ForStatements = fwd as MetaForStatements;
            }
            else if (fwd is MetaWhileDoWhileStatements)
            {
                m_WhileStatements = fwd as MetaWhileDoWhileStatements;
            }

            if (m_ForStatements == null && m_WhileStatements == null)
            {
                // switch case 体内的 break: 跳出 switch（IR 层由 PushBreakTarget 提供目标）
                if (!mbs.IsInSwitchCaseBody())
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, fmkos?.token, "Error break 只能出现在 for/while/dowhile 循环体内或 switch case 体内");
                }
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
    public partial class MetaNextStatements : MetaStatements
    {
        private FileMetaKeyOnlySyntax m_FileMetaKeyOnlySyntax;

        private MetaForStatements m_ForStatements = null;
        private MetaWhileDoWhileStatements m_WhileStatements = null;

        public MetaNextStatements(MetaBlockStatements mbs, FileMetaKeyOnlySyntax fmkos) : base(mbs)
        {
            m_FileMetaKeyOnlySyntax = fmkos;
            AddPingToken(fmkos?.token);

            var fwd = mbs.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            if (fwd is MetaForStatements)
            {
                m_ForStatements = fwd as MetaForStatements;
            }
            else if (fwd is MetaWhileDoWhileStatements)
            {
                m_WhileStatements = fwd as MetaWhileDoWhileStatements;
            }

            if (m_ForStatements == null && m_WhileStatements == null)
            {
                // switch case 体内的 next: fall-through 语义，本 case 体执行完后继续匹配后续 case
                // (循环优先: case 体内嵌套 for/while 时，next 绑定到最近的循环)
                if (!mbs.IsInSwitchCaseBody())
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, fmkos?.token, "Error next 只能出现在 for/while/dowhile 循环体内或 switch case 体内");
                }
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
            {
                sb.Append(Global.tabChar);
            }
            sb.Append("next;");
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
            AddPingToken(fmkos?.token);

            var fwd = mbs.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            if (fwd is MetaForStatements)
            {
                m_ForStatements = fwd as MetaForStatements;
            }
            else if (fwd is MetaWhileDoWhileStatements)
            {
                m_WhileStatements = fwd as MetaWhileDoWhileStatements;
            }

            if (m_ForStatements == null && m_WhileStatements == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, fmkos?.token, "Error continue 只能出现在 for/while/dowhile 循环体内");
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
            if (mf != null && labelToken != null)
            {
                string labelName = labelToken.lexeme?.ToString();
                labelData = mf.GetLabelDataById(labelName);
                if (labelData == null)
                {
                    // label 语句定义标签; 前向 goto(label 语句尚未出现)也先创建占位引用
                    labelData = mf.AddLabelData(labelName, nextMetaStatements);
                }
                if (isLabel)
                {
                    if (labelData.isDefined)
                    {
                        // 同一函数内 label 重复定义，编译不通过
                        Log.AddMetaCoreLog(LID.LabelRepeatDefine, labelToken,
                            "重复定义位置: [" + labelToken.path + ":" + labelToken.sourceBeginLine + "]", labelName);
                        return;
                    }
                    labelData.isDefined = true;
                }
                // goto 引用了函数中不存在的标签时, 占位 LabelData 不会被 label 语句标记 isDefined,
                // 由 IRMethod.Parse() 回填阶段检测并报编译错误。
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
