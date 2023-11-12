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

            public List<IRBase> conditionStatList = new List<IRBase>();
            public List<IRBase> thenStatList = new List<IRBase>();

            public IRBranch brBrach = null;
            public IRBranch ifBreach = null;
            public IRNop startNop = null;

            private IRExpress m_IrExpress = null;
            public void ParseIRStatements( IRMethod _irMethod )
            {
                startNop = new IRNop( _irMethod );
                conditionStatList.Add(startNop);

                if (m_IfElseState == IfElseState.If || m_IfElseState == IfElseState.ElseIf )
                {
                    startNop.nopData.line = m_FinalExpress.GetCodeFileLine();
                    
                    m_IrExpress = new IRExpress(_irMethod, m_FinalExpress);
                    conditionStatList.Add(m_IrExpress);

                    if (m_MetaAssignManager?.isNeedSetMetaVariable == true)
                    {
                        IRStoreVariable storeLocal = new IRStoreVariable(_irMethod, m_BoolConditionVariable);
                        storeLocal.irStoreData.line = m_BoolConditionVariable.GetCodeFileLine();
                        conditionStatList.Add(storeLocal);

                        IRLoadVariable loadLocal = new IRLoadVariable(_irMethod, m_BoolConditionVariable);
                        loadLocal.irLoadData.line = m_BoolConditionVariable.GetCodeFileLine();
                        conditionStatList.Add(loadLocal);
                    }

                    ifBreach = new IRBranch( _irMethod, EIROpCode.BrFalse, null );
                    ifBreach.SetCodeFileLine( m_IfOrElseIfKeySyntax.token.sourceBeginLine );
                    conditionStatList.Add(ifBreach);

                    _irMethod.AddLabelDict(ifBreach.brData);
                }
                m_ThenMetaStatements.ParseAllIRStatements();
                //thenStatList.AddRange(m_ThenMetaStatements.irDataList);

                brBrach = new IRBranch(_irMethod, EIROpCode.Br, null ); 
                _irMethod.AddLabelDict(brBrach.brData);

                thenStatList.Add(brBrach);

            }

            public string ToIRString()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("{");
                sb.Append("#if ");
                sb.AppendLine(m_FinalExpress.ToFormatString() + "#");

                for (int i = 0; i < conditionStatList.Count; i++)
                {
                    sb.AppendLine(conditionStatList[i].ToString());
                }
                sb.AppendLine("}");

                if( thenMetaStatements != null )
                {
                    sb.AppendLine(thenMetaStatements.ToIRString());
                }

                return sb.ToString();
            }
        }
        public override void ParseIRStatements()
        {
            for( int i = 0; i < m_MetaElseIfStatements.Count; i++ )
            {
                var meis = m_MetaElseIfStatements[i];

                meis.ParseIRStatements( irMethod );
                m_IRStatements.AddRange(meis.conditionStatList);
                m_IRStatements.AddRange(meis.thenStatList);               
            }

            IRNop nopIR = new IRNop( irMethod );
            m_IRStatements.Add(nopIR);

            for ( int i = 0; i < m_MetaElseIfStatements.Count; i++ )
            {
                var meis = m_MetaElseIfStatements[i];
                meis.brBrach.brData.opValue = nopIR;

                if (meis.ifBreach != null)
                {
                    if( i < m_MetaElseIfStatements.Count - 1 )
                    {
                        meis.ifBreach.brData.opValue = m_MetaElseIfStatements[i + 1].startNop.nopData;
                    }
                    else if( i == m_MetaElseIfStatements.Count - 1 )
                    {
                        meis.ifBreach.brData.opValue = nopIR;
                    }
                }
            }
        }
    }
}
