//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRNew : IRBase
    {
        private MetaType m_MetaDefineType = null;
        public IRNew( IRMethod irMethod, MetaType mdt):base( irMethod )
        {
            m_MetaDefineType = mdt;

            Parse();
        }
        void Parse()
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewObject;
            data.opValue = m_MetaDefineType;
            m_IRDataList.Add(data);
        }
    }
}
