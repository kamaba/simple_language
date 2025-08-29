//****************************************************************************
//  File:      IRData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using System;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRData
    {
        public int       id = 0;
        public EIROpCode opCode;                 //指令类型
        public object    opValue;                //指令值
        public int       index;                  //索引
        public DebugInfo debugInfo;              //调试信息

        public IRData()
        {

        }
        public void SetDebugInfoByValue( DebugInfo info )
        {
            debugInfo = info;
        }
        public void SetDebugInfoByToken( Token token )
        {
            if(token != null )
            {
                debugInfo.path = token.path;
                debugInfo.name = token.lexeme?.ToString();
                debugInfo.beginLine = token.sourceBeginLine;
                debugInfo.beginChar = token.sourceBeginChar;
                debugInfo.endLine = token.sourceEndLine;
                debugInfo.endChar = token.sourceEndChar;
            }
        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            m_StringBuilder.Append( id + "   [ " + debugInfo.path + ":" + debugInfo.beginLine.ToString() + "]" + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (opValue != null)
            {
                MetaType mt = opValue as MetaType;
                IRMethod irm = opValue as IRMethod;
                if ( opValue.GetType() == typeof( Int32 ) )
                {
                    m_StringBuilder.Append(" val: int32[" + opValue + "] ");
                }
                else
                {
                    if (mt != null)
                    {
                        m_StringBuilder.Append(" val mt:[" + mt.name + "] ");
                    }
                    else if( irm != null )
                    {
                        m_StringBuilder.Append(" val irm:[" + irm.id + "] ");
                    }
                    else
                    {
                        m_StringBuilder.Append(" val:[" + opValue.ToString() + "] ");
                    }
                }
            }
            return m_StringBuilder.ToString();
        }
    }
}
