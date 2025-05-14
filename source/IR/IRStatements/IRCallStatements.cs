//****************************************************************************
//  File:      IRCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public partial class MetaCallStatements
    {
        private IRMethod m_IRMethod = null;
        private List<IRBase> m_IRList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod)
        {
            m_IRMethod = _irMethod;

            //var cnlist = callNodeList;
            //for (int i = 0; i < cnlist.Count; i++)
            //{
            //    var cnode = cnlist[i];
            //    if (cnode.callNodeType == ECallNodeType.ConstValue)
            //    {
            //        IRExpress irExpress = new IRExpress( _irMethod, cnode.metaExpressValue );
            //        m_IRList.Add(irExpress);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.VariableName)
            //    {
            //        MetaVariable mv = cnode.GetMetaVariable();
            //        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
            //        m_IRList.Add(irVar);
            //    }
            //    else if( cnode.callNodeType == ECallNodeType.MemberVariableName )
            //    {
            //        MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();

            //        IRLoadVariable irVar = new IRLoadVariable(IRManager.instance, mmv);
            //        m_IRList.Add(irVar);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.FunctionName)
            //    {
            //        var mfc = cnode.GetMetaFunctionCall();
            //        IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, mfc);
            //        m_IRList.Add(irCallFun);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.This)
            //    {
            //        IRData data = new IRData();
            //        data.opCode = EIROpCode.Call;
            //        data.opValue = "";
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.Base)
            //    {
            //        IRData data = new IRData();
            //        data.opCode = EIROpCode.Call;
            //        data.opValue = "";
            //    }
            //}
        }
        public void ParseToIRDataListByIRManager(IRManager _irManager)
        {
            //var cnlist = callNodeList;
            //for (int i = 0; i < cnlist.Count; i++)
            //{
            //    var cnode = cnlist[i];
            //    if (cnode.callNodeType == ECallNodeType.ConstValue)
            //    {
            //        IRExpress data = new IRExpress( _irManager, cnode.constValue);
            //        m_IRList.Add(data);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.VariableName)
            //    {
            //        MetaVariable mv = cnode.GetMetaVariable();
            //        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
            //        m_IRList.Add(irVar);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.MemberVariableName)
            //    {
            //        MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();
            //        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mmv);
            //        m_IRList.Add(irVar);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.FunctionName)
            //    {
            //        var mfc = cnode.GetMetaFunctionCall();
            //        IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, mfc);
            //        m_IRList.Add(irCallFun);
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.This)
            //    {
            //        IRData data = new IRData();
            //        data.opCode = EIROpCode.Call;
            //        data.opValue = "";
            //    }
            //    else if (cnode.callNodeType == ECallNodeType.Base)
            //    {
            //        IRData data = new IRData();
            //        data.opCode = EIROpCode.Call;
            //        data.opValue = "";
            //    }
            //}
        }

        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("call");
            //sb.Append(base.ToIRString());
            return sb.ToString();
        }
    }
    public class MetaIRCallStatements : MetaIRStatements
    {
        public void ParseIRStatements(MetaCallStatements ms)
        {
            //m_MetaCallLink.ParseToIRDataList(irMethod);
            //m_IRStatements.AddRange(m_MetaCallLink.irList);
        }
        //public override string ToIRString()
        //{
        //    return m_MetaCallLink.ToIRString();
        //}
    }
}
