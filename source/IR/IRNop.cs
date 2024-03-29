﻿//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRNop : IRBase
    {
        public IRData data = new IRData();
        public IRNop( IRMethod irMethod ):base( irMethod )
        {
            data.opCode = EIROpCode.Nop;
            m_IRDataList.Add(data);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            for( int i = 0; i < m_IRDataList.Count; i++ )
            {
                sb.AppendLine(m_IRDataList[i].ToString());
            }
            return sb.ToString();
        }
    }
}
