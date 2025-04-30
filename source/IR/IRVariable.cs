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
                data.index = mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
        }

        public IRLoadVariable(IRMethod _irManager, MetaMemberVariable mmv)
        {
            data.opCode = EIROpCode.LoadNotStaticField;
            data.index = mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
            m_IRDataList.Add(data);
        }

        public IRLoadVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            if ( mv.variableFrom == MetaVariable.EVariableFrom.Argument )
            {
                data.opCode = EIROpCode.LoadArgument;
                data.SetDebugInfoByToken( mv.pingToken );
                data.index = m_IRMethod.GetArgumentIndex(mv);
                m_IRDataList.Add(data);
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                MetaMemberVariable mmv = mv as MetaMemberVariable;
                data.opCode = EIROpCode.LoadNotStaticField;
                data.SetDebugInfoByToken(mmv.pingToken);
                data.index = mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement)
            {
                data.opCode = EIROpCode.LoadLocal;
                data.SetDebugInfoByToken(mv.pingToken);
                data.index = m_IRMethod.GetLocalVariableIndex(mv);
                m_IRDataList.Add(data);
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Global )
            {
                MetaMemberVariable mmv = mv as MetaMemberVariable;
                data.opCode = EIROpCode.LoadNotStaticField;
                data.SetDebugInfoByToken(mmv.pingToken);
                data.index = mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
            else
            {
                Console.WriteLine($"SVM Error 没有找到加载变量的来源类型！");
            }
        }
        public IRLoadVariable( IRMethod _irMethod, MetaVisitNode mvn ) : base( _irMethod )
        {
            //if( mvn.)
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#LoadVariable#");
            for (int i = 0; i < m_IRDataList.Count; i++)
            {
                sb.AppendLine(m_IRDataList[i].ToString());
            }
            return sb.ToString();
        }
    }
    public class IRStoreVariable : IRBase
    {
        public IRData data = new IRData();
        public IRStoreVariable(IRMethod _irMethod, MetaVariable mv) : base(_irMethod)
        {
            var vmv = mv as MetaVisitVariable;
            var mmv = mv as MetaMemberVariable;
            if (vmv != null)
            {
                var localVariable = vmv.sourceMetaVariable;
                if (localVariable is MetaVariable)
                {
                    if (localVariable is MetaVisitVariable)
                    {
                        IRStoreVariable parentIRStore = new IRStoreVariable(_irMethod, localVariable as MetaVisitVariable);
                        m_IRDataList.AddRange(parentIRStore.IRDataList);
                    }
                    else
                    {
                        IRLoadVariable irload = new IRLoadVariable(_irMethod, localVariable as MetaVariable);
                        m_IRDataList.AddRange(irload.IRDataList);
                    }
                }
                data.opCode = EIROpCode.StoreNotStaticField;
                data.index = vmv.GetIRMemberIndex();
                m_IRDataList.Add(data);
            }
            else if (mmv != null)
            {
                data.opCode = EIROpCode.StoreNotStaticField;
                data.index = mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
                m_IRDataList.Add(data);
            }
            else
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
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#StoreVariable#");
            for( int i = 0; i < m_IRDataList.Count; i++ )
            {
                sb.AppendLine(m_IRDataList[i].ToString());
            }
            return sb.ToString();
        }
    }
}