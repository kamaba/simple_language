//****************************************************************************
//  File:      IRVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: 
//****************************************************************************

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
        public IRLoadVariable(IRManager _irManager, int id)
        {
            var irmv = _irManager.staticVariableList.Find(a => a.id == id);
            data.opCode = EIROpCode.LoadStaticField;
            data.index = irmv.index;
            m_IRDataList.Add(data);
        }
        public IRLoadVariable(IRMethod _irMethod, int id) : base(_irMethod)
        {
            var irmv = _irMethod.GetIRLocalVariableById(id);
            if (irmv.irMetaVariableFrom == IRMetaVariableFrom.Argument )
            {
                data.opCode = EIROpCode.LoadArgument;
                //data.SetDebugInfoByToken( mv.pingToken );
            }
            else if (irmv.irMetaVariableFrom == IRMetaVariableFrom.Member)
            {
                //data.SetDebugInfoByToken(mv.pingToken);
                data.opCode = EIROpCode.LoadNotStaticField;
            }
            else if (irmv.irMetaVariableFrom == IRMetaVariableFrom.LocalStatement )
            {
                data.opCode = EIROpCode.LoadLocal;
                //data.SetDebugInfoByToken(mv.pingToken);
            }
            else
            {
                Console.WriteLine($"SVM Error 没有找到加载变量的来源类型！");
            }
            data.index = irmv.index;
            m_IRDataList.Add(data);
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
        public IRStoreVariable(IRMethod _irMethod, int id ) : base(_irMethod)
        {
            IRMetaVariable irmv = _irMethod.GetIRLocalVariableById(id);
            data.opCode = EIROpCode.LoadLocal;
            data.index = irmv.index; 
            m_IRDataList.Add(data);
            //var vmv = mv as MetaVisitVariable;
            //var mmv = mv as MetaMemberVariable;
            //if (vmv != null)
            //{
            //    var localVariable = vmv.sourceMetaVariable;
            //    if (localVariable is MetaVariable)
            //    {
            //        if (localVariable is MetaVisitVariable)
            //        {
            //            IRStoreVariable parentIRStore = new IRStoreVariable(_irMethod, localVariable as MetaVisitVariable);
            //            m_IRDataList.AddRange(parentIRStore.IRDataList);
            //        }
            //        else
            //        {
            //            IRLoadVariable irload = new IRLoadVariable(_irMethod, localVariable as MetaVariable);
            //            m_IRDataList.AddRange(irload.IRDataList);
            //        }
            //    }
            //    data.opCode = EIROpCode.StoreNotStaticField;
            //    data.index = vmv.GetIRMemberIndex();
            //    m_IRDataList.Add(data);
            //}
        }

        public IRStoreVariable( IRMetaVariable irmv )
        {

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