//****************************************************************************
//  File:      IRVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRLoadVariable : IRBase
    {
        public IRData data = new IRData();     
        public IRLoadVariable(IRManager _irManager, MetaMemberVariable mmv)
        {
            if (mmv.isStatic)
            {
                data.opCode = EIROpCode.LoadStaticField;
                data.index = _irManager.GetStaticVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
            else
            {
                data.opCode = EIROpCode.LoadNotStaticField;
                data.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
        }
        public IRLoadVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                data.opCode = EIROpCode.LoadArgument;
                data.SetDebugInfoByToken( mv.GetToken() );
                data.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(data);
            }
            else
            {
                data.opCode = EIROpCode.LoadLocal;
                data.SetDebugInfoByToken( mv.GetToken() );
                data.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(data);
            }
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
    public class IRStoreVariable : IRBase
    {
        public IRData data = new IRData();
        public IRStoreVariable( IRMethod _irMethod, MetaMemberVariable mmv)
        {
            IRExpress irexp = new IRExpress(_irMethod, mmv.express);
            m_IRDataList.AddRange(irexp.IRDataList);

            data.opCode = EIROpCode.StoreNotStaticField;
            data.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
            m_IRDataList.Add(data);
        }
        public IRStoreVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                data.opCode = EIROpCode.LoadArgument;
                data.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(data);
            }
            else
            {
                data.opCode = EIROpCode.StoreLocal;
                data.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(data);
            }
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