//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
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
    public class IRCallFunction : IRLoadVariable
    {
        MetaFunctionCall m_MetaFunctionCall = null;
        public int paramCount { get; set; } = 0;
        public bool target { get; set; } = false;
        public IRCallFunction(IRMethod _irMethod, MetaFunctionCall mfc) : base(_irMethod)
        {
            m_MetaFunctionCall = mfc;
            Parse();
        }
        void Parse()
        {
            paramCount = m_MetaFunctionCall.metaInputParamCollection.count;
            for (int j = 0; j < paramCount; j++)
            {
                MetaInputParam mip = m_MetaFunctionCall.metaInputParamCollection.metaParamList[j] as MetaInputParam;
                IRExpress irexpress = new IRExpress(m_IRMethod, mip.express);
                IRDataList.AddRange(irexpress.IRDataList);
            }
            MetaFunction mf = m_MetaFunctionCall.function;
            if (mf is MetaMemberFunction)
            {
                MetaMemberFunction mmf = mf as MetaMemberFunction;
                if (mmf.isCSharp)
                {
                    IRData data = new IRData();
                    data.opCode = EIROpCode.CallCSharpMethod;
                    data.opValue = this;
                    IRDataList.Add(data);
                    return;
                }
            }
            IRData datacall = new IRData();
            datacall.opCode = EIROpCode.Call;
            datacall.opValue = m_MetaFunctionCall;
            IRDataList.Add(datacall);
        }
        public System.Object InvokeCSharp( Object target, Object[] csParamObjs)
        {
            return m_MetaFunctionCall.methodInfo.Invoke(target, csParamObjs);
        }
    }
}
namespace SimpleLanguage.Core
{
    public partial class MetaCallLink
    {
        public List<IRData> irDataList => m_IRDataList;

        private List<IRData> m_IRDataList = new List<IRData>();
        private IRMethod m_IRMethod = null;
        private List<IRBase> m_IRList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod)
        {
            m_IRMethod = _irMethod;

            var cnlist = callNodeList;
            for (int i = 0; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];
                if (cnode.callNodeType == ECallNodeType.ConstValue)
                {
                    IRData data = IRExpress.CreateIRDataByConstExpress(cnode.constValue);
                    m_IRDataList.Add(data);
                }
                else if (cnode.callNodeType == ECallNodeType.VariableName)
                {
                    MetaVariable mv = cnode.GetMetaVariable();
                    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
                    m_IRList.Add(irVar);
                    m_IRDataList.AddRange(irVar.IRDataList);
                }
                else if( cnode.callNodeType == ECallNodeType.MemberVariableName )
                {
                    MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();

                    IRLoadVariable irVar = new IRLoadVariable(IRManager.instance, mmv);
                    m_IRList.Add(irVar);
                    m_IRDataList.AddRange(irVar.IRDataList);
                }
                else if (cnode.callNodeType == ECallNodeType.FunctionName)
                {
                    var mfc = cnode.GetMetaFunctionCall();
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, mfc);
                    m_IRList.Add(irCallFun);
                    m_IRDataList.AddRange(irCallFun.IRDataList);
                }
                else if (cnode.callNodeType == ECallNodeType.This)
                {
                    IRData data = new IRData();
                    data.opCode = EIROpCode.Call;
                    data.opValue = "";
                    m_IRDataList.Add(data);
                }
                else if (cnode.callNodeType == ECallNodeType.Base)
                {
                    IRData data = new IRData();
                    data.opCode = EIROpCode.Call;
                    data.opValue = "";
                    m_IRDataList.Add(data);
                }
            }
        }
        public void ParseToIRDataListByIRManager(IRManager _irManager)
        {
            var cnlist = callNodeList;
            for (int i = 0; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];
                if (cnode.callNodeType == ECallNodeType.ConstValue)
                {
                    IRData data = IRExpress.CreateIRDataByConstExpress(cnode.constValue);
                    m_IRDataList.Add(data);
                }
                else if (cnode.callNodeType == ECallNodeType.VariableName)
                {
                    MetaVariable mv = cnode.GetMetaVariable();
                    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
                    m_IRList.Add(irVar);
                    m_IRDataList.AddRange(irVar.IRDataList);
                }
                else if (cnode.callNodeType == ECallNodeType.MemberVariableName)
                {
                    MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();
                    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mmv);
                    m_IRList.Add(irVar);
                    m_IRDataList.AddRange(irVar.IRDataList);
                }
                else if (cnode.callNodeType == ECallNodeType.FunctionName)
                {
                    var mfc = cnode.GetMetaFunctionCall();
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, mfc);
                    m_IRList.Add(irCallFun);
                    m_IRDataList.AddRange(irCallFun.IRDataList);
                }
                else if (cnode.callNodeType == ECallNodeType.This)
                {
                    IRData data = new IRData();
                    data.opCode = EIROpCode.Call;
                    data.opValue = "";
                    m_IRDataList.Add(data);
                }
                else if (cnode.callNodeType == ECallNodeType.Base)
                {
                    IRData data = new IRData();
                    data.opCode = EIROpCode.Call;
                    data.opValue = "";
                    m_IRDataList.Add(data);
                }
            }
        }
    }
}
