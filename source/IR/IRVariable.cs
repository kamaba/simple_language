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
        public IRData irLoadData = new IRData();
        MetaVariable m_MetaVariable = null;
        protected IRLoadVariable(IRMethod _irMethod) : base(_irMethod)
        { }
        public IRLoadVariable(IRManager _irManager, MetaMemberVariable mmv)
        {
            m_MetaVariable = mmv;
            if (mmv.isStatic)
            {
                irLoadData.opCode = EIROpCode.LoadStaticField;
                irLoadData.index = _irManager.GetStaticVariableIndex(mmv);
                m_IRDataList.Add(irLoadData);
            }
            else
            {
                irLoadData.opCode = EIROpCode.LoadNotStaticField;
                irLoadData.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
                m_IRDataList.Add(irLoadData);
            }
        }
        public IRLoadVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                irLoadData.opCode = EIROpCode.LoadArgument;
                irLoadData.line = mv.GetCodeFileLine();
                irLoadData.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(irLoadData);
            }
            else
            {
                irLoadData.opCode = EIROpCode.LoadLocal;
                irLoadData.line = mv.GetCodeFileLine();
                irLoadData.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(irLoadData);
            }
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
    public class IRStoreVariable : IRBase
    {
        public IRData irStoreData = new IRData();
        MetaVariable m_MetaVariable = null;
        public IRStoreVariable( IRMethod _irMethod, MetaMemberVariable mmv)
        {
            m_MetaVariable = mmv;

            IRExpress irexp = new IRExpress(_irMethod, mmv.express);
            m_IRDataList.AddRange(irexp.IRDataList);

            irStoreData.opCode = EIROpCode.StoreNotStaticField;
            irStoreData.index = mmv.ownerMetaClass.metaClassData.GetMemberVariableIndex(mmv);
            m_IRDataList.Add(irStoreData);
        }
        public IRStoreVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if (mv.isArgument)
            {
                irStoreData.opCode = EIROpCode.LoadArgument;
                irStoreData.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(irStoreData);
            }
            else
            {
                irStoreData.opCode = EIROpCode.StoreLocal;
                irStoreData.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(irStoreData);
            }
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
}