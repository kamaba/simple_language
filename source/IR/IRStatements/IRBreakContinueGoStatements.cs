//****************************************************************************
//  File:      IRBreakContinueGoStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBreakStatements
    {
        public IRData brData = null;
        public override void ParseIRStatements()
        {
            brData = new IRData();
            brData.opCode = EIROpCode.Br;
            irMethod.AddLabelDict(brData);
            m_IRDataList.Add(brData);
            if (m_ForStatements != null )
            {
                brData.opValue = m_ForStatements.endData;
            }
            else if( m_WhileStatements != null )
            {
                brData.opValue = m_WhileStatements.endData;
            }

        }
    }
    public partial class MetaContinueStatements
    {
        public IRData brData = null;
        public override void ParseIRStatements()
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            m_IRDataList.Add(insNode);

            brData = new IRData();
            brData.opCode = EIROpCode.Br;
            irMethod.AddLabelDict(brData);
            m_IRDataList.Add(brData);
            if (m_ForStatements != null)
            {
                brData.opValue = m_ForStatements.forStartIRData;
            }
            else if (m_WhileStatements != null)
            {
                brData.opValue = m_WhileStatements.endData;
            }
        }
    }
    public partial class MetaGotoLabelStatements
    {
        public IRData labelIRData = null;
        public IRData brIRData = null;
        public override void ParseIRStatements()
        {
            if( isLabel )
            {
                labelIRData = new IRData();
                labelIRData.opCode = EIROpCode.Label;
                labelIRData.opValue = labelData.label;
                m_IRDataList.Add(labelIRData);
                irMethod.AddLabelDict(labelIRData);
            }
            else
            {
                brIRData = new IRData();
                brIRData.opCode = EIROpCode.BrLabel;
                brIRData.opValue = labelData.label;
                irMethod.AddLabelDict(labelIRData);
                m_IRDataList.Add(brIRData);
            }
        }
    }
}