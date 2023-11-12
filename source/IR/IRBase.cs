//****************************************************************************
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
    public class IRBase
    {
        protected IRMethod m_IRMethod = null;
        public List<IRData> IRDataList => m_IRDataList;

        protected List<IRData> m_IRDataList = new List<IRData>();

        protected IRBase()
        {

        }
        protected IRBase( IRMethod irMethod )
        {
            m_IRMethod = irMethod;
        }
        public void AddIRData( IRData irData )
        {
            m_IRDataList.Add(irData);
        }
        public virtual string ToIRString()
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
