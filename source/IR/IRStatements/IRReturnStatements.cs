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

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaReturnStatements
    {
        private IRExpress m_ReturnValueExpress = null;
        public override void ParseIRStatements()
        {
            /*
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            m_IRDataList.Add(insNode);
            */
            if( m_Express != null )
            {
                m_ReturnValueExpress = new IRExpress( irMethod, m_Express );
                m_IRDataList.AddRange(m_ReturnValueExpress.IRDataList);

                IRData storeNode = new IRData();
                storeNode.opCode = EIROpCode.StoreReturn;
                storeNode.index = 0;
                m_IRDataList.Add(storeNode);
            }
        }
    }

    public partial class MetaTRStatements
    {
        public override void ParseIRStatements()
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            m_IRDataList.Add(insNode);
        }
    }
}
