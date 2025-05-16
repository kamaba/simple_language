//****************************************************************************
//  File:      IRReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRReturnStatements : IRStatements
    {
        public IRReturnStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        private IRExpress m_ReturnValueExpress = null;
        public void ParseIRStatements(MetaReturnStatements ms)
        {
            //if( m_Express != null )
            //{
            //    m_ReturnValueExpress = new IRExpress( irMethod, m_Express );
            //    m_IRStatements.Add( m_ReturnValueExpress );

            //    //IRStoreVariable

            //    //IRData storeNode = new IRData();
            //    //storeNode.opCode = EIROpCode.StoreReturn;
            //    //storeNode.index = 0;
            //    //m_IRDataList.Add(storeNode);
            //}
        }
    }

    public class MetaIRTRStatements
    {
        public void ParseIRStatements()
        {
        }
    }
}
