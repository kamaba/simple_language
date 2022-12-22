//****************************************************************************
//  File:      IRIfStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml.Linq;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaIfStatements
    {
        public partial class MetaElseIfStatements
        {
            public IRExpress irExpress => m_IrExpress;

            public List<IRData> conditionStatList = new List<IRData>();
            public List<IRData> thenStatList = new List<IRData>();

            public IRData brData = null;
            public IRData ifData = null;
            public IRData startData = null;

            private IRExpress m_IrExpress = null;
            public void ParseIRStatements( IRMethod _irMethod )
            {
                startData = new IRData();
                startData.opCode = EIROpCode.Nop;
                conditionStatList.Add(startData);

                if (m_IfElseState == IfElseState.If || m_IfElseState == IfElseState.ElseIf )
                {
                    m_IrExpress = new IRExpress(_irMethod, m_FinalExpress);
                    conditionStatList.AddRange(m_IrExpress.IRDataList);

                    if (m_MetaAssignManager?.isNeedSetMetaVariable == true)
                    {
                        IRData irData = new IRData();
                        irData.opCode = EIROpCode.StoreLocal;
                        irData.index = _irMethod.GetLocalVariableIndex(m_BoolConditionVariable);
                        conditionStatList.Add(irData);

                        IRData irData2 = new IRData();
                        irData2.opCode = EIROpCode.LoadLocal;
                        irData2.index = _irMethod.GetLocalVariableIndex(m_BoolConditionVariable);
                        conditionStatList.Add(irData2);
                    }

                    ifData = new IRData();
                    ifData.opCode = EIROpCode.BrFalse;
                    conditionStatList.Add(ifData);

                    _irMethod.AddLabelDict(ifData);
                }
                m_ThenMetaStatements.ParseAllIRStatements();                
                thenStatList.AddRange(m_ThenMetaStatements.irDataList);

                brData = new IRData();
                brData.opCode = EIROpCode.Br;
                brData.index = -1;
                _irMethod.AddLabelDict(brData);

                thenStatList.Add(brData);

            }
        }
        public override void ParseIRStatements()
        {
            for( int i = 0; i < m_MetaElseIfStatements.Count; i++ )
            {
                var meis = m_MetaElseIfStatements[i];

                meis.ParseIRStatements( irMethod );
                m_IRDataList.AddRange(meis.conditionStatList);
                m_IRDataList.AddRange(meis.thenStatList);               
            }

            IRData nopIR = new IRData();
            nopIR.opCode = EIROpCode.Nop;
            m_IRDataList.Add(nopIR);

            for ( int i = 0; i < m_MetaElseIfStatements.Count; i++ )
            {
                var meis = m_MetaElseIfStatements[i];
                meis.brData.opValue = nopIR;

                if (meis.ifData != null)
                {
                    if( i < m_MetaElseIfStatements.Count - 1 )
                    {
                        meis.ifData.opValue = m_MetaElseIfStatements[i + 1].startData;
                    }
                    else if( i == m_MetaElseIfStatements.Count - 1 )
                    {
                        meis.ifData.opValue = nopIR;
                    }
                }
            }
        }
    }
}
