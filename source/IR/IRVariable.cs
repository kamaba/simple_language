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
        MetaVariable m_MetaVariable = null;
        protected IRLoadVariable(IRMethod _irMethod) : base(_irMethod)
        { }
        IRLoadVariable(MetaMemberVariable mmv)
        {
            m_MetaVariable = mmv;
        }
        public IRLoadVariable(IRManager _irManager, MetaMemberVariable mmv)
        {
            m_MetaVariable = mmv;
            if (mmv.isStatic)
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadStaticField;
                data.index = _irManager.GetStaticVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
            else
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadNotStaticField;
                data.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
        }
        public IRLoadVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadArgument;
                data.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(data);
            }
            else
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadLocal;
                data.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(data);
            }
        }
    }
    public class IRStoreVariable : IRBase
    {
        MetaVariable m_MetaVariable = null;
        public IRStoreVariable( IRMethod _irMethod, MetaMemberVariable mmv)
        {
            m_MetaVariable = mmv;

            IRExpress irexp = new IRExpress(_irMethod, mmv.express);
            m_IRDataList.AddRange(irexp.IRDataList);

            IRData data = new IRData();
            data.opCode = EIROpCode.StoreNotStaticField;
            data.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
            m_IRDataList.Add(data);
        }
        public IRStoreVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadArgument;
                data.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(data);
            }
            else
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.LoadLocal;
                data.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(data);
            }
        }
    }
}