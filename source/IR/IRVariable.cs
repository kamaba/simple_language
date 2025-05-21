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
using System.Diagnostics;
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
        public IRLoadVariable(IRMethod _irMethod, int id, IRMetaVariableFrom irmvf ) : base(_irMethod)
        {
            IRMetaVariable irmv = null;
            if (irmvf == IRMetaVariableFrom.Argument )
            {
                irmv = _irMethod.GetIRArgumentById(id);
                data.opCode = EIROpCode.LoadArgument;
                data.index = irmv.index;
                //data.SetDebugInfoByToken( mv.pingToken );
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                //irmv = _irMethod.GetIRLocalVariableById(id);
                //data.SetDebugInfoByToken(mv.pingToken);
                data.index = id;
                data.opCode = EIROpCode.LoadNotStaticField;
            }
            else if (irmvf == IRMetaVariableFrom.LocalStatement)
            {
                irmv = _irMethod.GetIRLocalVariableById(id);
                data.opCode = EIROpCode.LoadLocal;
                data.index = irmv.index;
                //data.SetDebugInfoByToken(mv.pingToken);
            }
            else
            {
                Debug.Write($"SVM Error 没有找到加载变量的来源类型！");
            }
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
        public IRStoreVariable(IRMethod _irMethod, int id, IRMetaVariableFrom irmvf) : base(_irMethod)
        {
            IRMetaVariable irmv = null;
            if (irmvf == IRMetaVariableFrom.Member)
            {
                data.index = id;
                data.opCode = EIROpCode.StoreNotStaticField;
            }
            else if (irmvf == IRMetaVariableFrom.Member2)
            {
                data.index = id;
                data.opCode = EIROpCode.StoreNotStaticField_R1;
            }
            else if (irmvf == IRMetaVariableFrom.LocalStatement)
            {
                irmv = _irMethod.GetIRLocalVariableById(id);
                data.opCode = EIROpCode.StoreLocal;
                data.index = irmv.index;
            }
            else
            {
                Debug.Write($"SVM Error 没有找到加载变量的来源类型！");
            }
            m_IRDataList.Add(data);
        }

        public IRStoreVariable()
        {

        }
        public static IRStoreVariable CreateStaticReturnIRSV( )
        {
            IRStoreVariable irsv = new IRStoreVariable( );
            IRData storeNode = new IRData();
            storeNode.opCode = EIROpCode.StoreReturn;
            storeNode.index = 0;
            irsv.IRDataList.Add(storeNode);

            return irsv;
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