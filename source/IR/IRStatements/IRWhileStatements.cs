//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  handle for loop statements or while/dowhile statements create instruction 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaForStatements
    {
        public IRNop startIRData = null;       //for开始执行起点
        public IRNop forStartIRData = null;    //for 循环返回起点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //for结束点返回起点语句
        public IRNop endIRData = null;           //for语句点

        private IRExpress m_IRConditionExpress = null;
        public override void ParseIRStatements()
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);

            m_IRStatements.Add(startIRData);

            if (m_IsForIn)
            {
                IRStoreVariable irStoreVar = new IRStoreVariable(irMethod, m_ForMetaVariable);
                m_IRStatements.Add(irStoreVar);
                //if (m_FileMetaOpAssignSyntax != null)
                //{
                //    irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
                //}

                m_ThenMetaStatements.ParseAllIRStatements();
                m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
            }
            else
            {
                if (m_NewStatements != null)
                {
                    m_NewStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_NewStatements.irStatements);
                }
                else if (m_AssignStatements != null)
                {
                    m_AssignStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_AssignStatements.irStatements);
                }
                forStartIRData = new IRNop( irMethod );
                m_IRStatements.Add(forStartIRData);

                if (m_StepStatements != null)
                {
                    m_StepStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_StepStatements.irStatements);
                }

                if (m_ConditionExpress != null)
                {
                    m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
                    m_IRStatements.Add(m_IRConditionExpress);

                    ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                    m_IRStatements.Add(ifIRData);
                }
                m_ThenMetaStatements.ParseAllIRStatements();
                m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
            }

            brIRData = new IRBranch(irMethod, EIROpCode.Br, forStartIRData.data );
            m_IRStatements.Add(brIRData);
            m_IRStatements.Add(endIRData);

            if (m_NextMetaStatements != null)
            {
                m_NextMetaStatements.ParseIRStatements();
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#for ");
            if (m_IsForIn)
            {
                sb.Append(m_ForMetaVariable.name);
                sb.Append(" in ");
                sb.Append(m_ForInContent.name);
            }
            sb.AppendLine("#");
            sb.Append("{");
            sb.Append(Environment.NewLine);

            if (!m_IsForIn)
            {
                if (m_NewStatements != null)
                {
                    sb.Append(m_NewStatements.ToIRString());
                }
                if (m_AssignStatements != null)
                {
                    sb.Append(m_AssignStatements.ToIRString());
                }

                if (m_ConditionExpress != null)
                {
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append("if ");
                    sb.Append(m_ConditionExpress.ToFormatString());
                    sb.Append("{break;}");
                    sb.Append(Environment.NewLine);
                }
                if (m_StepStatements != null)
                {
                    sb.Append(m_StepStatements.ToIRString());
                }
                sb.Append(m_ThenMetaStatements?.ToIRString());
            }
            else
            {
            }

            sb.Append("}");

            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }

    public partial class MetaWhileDoWhileStatements
    {
        public IRNop startIRData = null;       //while 开始执行起点
        public IRNop whileStartIRData = null;    //while 循环返回起点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //while结束点返回起点语句
        public IRNop endIRData = null;           //while语句点

        private IRExpress m_IRConditionExpress = null;
        public override void ParseIRStatements()
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);
            m_IRStatements.Add(startIRData);

            whileStartIRData = new IRNop(irMethod);
            m_IRStatements.Add(whileStartIRData);


            if (m_IsWhile)
            {
                if (m_ConditionExpress != null)
                {
                    m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
                    m_IRStatements.Add(m_IRConditionExpress);

                    ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                    m_IRStatements.Add(ifIRData);
                }
                m_ThenMetaStatements.ParseAllIRStatements();
                m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
            }
            else
            {
                m_ThenMetaStatements.ParseAllIRStatements();
                m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);

                if (m_ConditionExpress != null)
                {
                    m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
                    m_IRStatements.Add(m_IRConditionExpress);

                    ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                    m_IRStatements.Add(ifIRData);
                }
            }

            brIRData = new IRBranch(irMethod, EIROpCode.Br, whileStartIRData.data);
            m_IRStatements.Add(brIRData);
            m_IRStatements.Add(endIRData);

            if (m_ConditionExpress != null)
            {
                ifIRData.data.SetDebugInfoByToken(m_ConditionExpress.GetToken());
            }
            if (m_FileMetaKeyWhileSyntax != null)
            {
                brIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
                startIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.token);
                whileStartIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.beginBlock);
                endIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
            }

            if (m_NextMetaStatements != null)
            {
                m_NextMetaStatements.ParseIRStatements();
            }
        }

        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#" + (m_IsWhile ? "while" : "dowhile#   {") );
            sb.AppendLine( base.ToIRString() );
            sb.AppendLine("}");
            if (m_ConditionExpress != null)
            {
                sb.AppendLine("#condition" + m_ConditionExpress.ToFormatString() + "#");
                sb.AppendLine("{");
                sb.Append(m_IRConditionExpress.ToIRString());
                sb.AppendLine("}");
            }

            sb.AppendLine(m_ThenMetaStatements.ToIRString());

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
