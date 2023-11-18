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
    public class IRCallFunction : IRBase
    {
        MetaMethodCall m_MetaFunctionCall = null;
        public int paramCount { get; set; } = 0;
        public bool target { get; set; } = false;
        public IRCallFunction(IRMethod _irMethod, MetaMethodCall mfc) : base(_irMethod)
        {
            m_MetaFunctionCall = mfc;
            Parse();
        }
        void Parse()
        {
            if (m_MetaFunctionCall?.metaInputParamCollection == null)
            {
                return;
            }
            if( !m_MetaFunctionCall.isStaticCall )
            {
                if( m_MetaFunctionCall.callerMetaVariable!=null )
                {
                    IRLoadVariable irload = new IRLoadVariable(m_IRMethod, m_MetaFunctionCall.callerMetaVariable);
                    AddIRRangeData(irload.IRDataList);
                }
                else
                {
                    Console.WriteLine("Error 没有加载到调用者信息!");
                    return;
                }
            }
            paramCount = m_MetaFunctionCall.metaInputParamCollection.count;
            for (int j = 0; j < paramCount; j++)
            {
                MetaInputParam mip = m_MetaFunctionCall.metaInputParamCollection.metaParamList[j] as MetaInputParam;
                IRExpress irexpress = new IRExpress(m_IRMethod, mip.express);
                AddIRRangeData(irexpress.IRDataList);
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
                    data.SetDebugInfoByToken( mmf.GetToken() );
                    AddIRData(data);
                    return;
                }
            }
            IRData datacall = new IRData();
            datacall.opCode = EIROpCode.Call;
            datacall.opValue = m_MetaFunctionCall.function.irMethod;
            datacall.SetDebugInfoByToken( mf.GetToken() );
            AddIRData(datacall);
        }
        public System.Object InvokeCSharp( Object target, Object[] csParamObjs)
        {
            return m_MetaFunctionCall.methodInfo.Invoke(target, csParamObjs);
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
    
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
}

namespace SimpleLanguage.Core
{
    public partial class MetaCallLink
    {
        private IRMethod m_IRMethod = null;
        public List<IRBase> irList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod)
        {
            m_IRMethod = _irMethod;

            for (int i = 0; i < callNodeList.Count; i++)
            {
                var cnode = callNodeList[i];
                //if (cnode.callNodeType == ECallNodeType.ConstValue)
                //{
                //    IRExpress data = new IRExpress( _irMethod, cnode.constValue);
                //    irList.Add(data);
                //}
                //else if (cnode.callNodeType == ECallNodeType.VariableName)
                //{
                //    MetaVariable mv = cnode.GetMetaVariable();
                //    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
                //    irList.Add(irVar);
                //}
                //else if (cnode.callNodeType == ECallNodeType.MemberVariableName)
                //{
                //    MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();

                    //    IRLoadVariable irVar = new IRLoadVariable(IRManager.instance, mmv);
                    //    irList.Add(irVar);
                    //}
                    //else if( cnode.callNodeType == ECallNodeType.VisitVariable )
                    //{
                    //    MetaVisitVariable vmv = cnode.GetMetaVariable() as MetaVisitVariable;
                    //    if( vmv != null )
                    //    {
                    //        IRLoadVariable irVar = new IRLoadVariable(IRManager.instance, vmv.visitMetaVariable as MetaMemberVariable );
                    //        irList.Add(irVar);
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Error convert VisitMetaVariable");
                    //    }
                    //}
                if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
                {
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, cnode.methodCall );
                    irList.Add(irCallFun);
                }
                //else if (cnode.callNodeType == ECallNodeType.This)
                //{
                //    IRCallFunction data = new IRCallFunction( _irMethod, cnode.GetMetaFunctionCall() );
                //    irList.Add(data);
                //}
                //else if (cnode.callNodeType == ECallNodeType.Base)
                //{
                //    IRCallFunction data = new IRCallFunction(_irMethod, cnode.GetMetaFunctionCall());
                //    irList.Add(data);
                //}
            }
        }
        public void ParseToIRDataListByIRManager(IRManager _irManager)
        {
            var cnlist = callNodeList;
            for (int i = 0; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];
                //if (cnode.callNodeType == ECallNodeType.ConstValue)
                //{
                //    IRExpress data = new IRExpress(_irManager, cnode.constValue);
                //    irList.Add(data);
                //}
                //else if (cnode.callNodeType == ECallNodeType.VariableName)
                //{
                //    MetaVariable mv = cnode.GetMetaVariable();
                //    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
                //    irList.Add(irVar);
                //}
                //else if (cnode.callNodeType == ECallNodeType.MemberVariableName)
                //{
                //    MetaMemberVariable mmv = cnode.GetMetaMemeberVariable();
                //    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mmv);
                //    irList.Add(irVar);
                //}
                //else if (cnode.callNodeType == ECallNodeType.FunctionName)
                //{
                //    var mfc = cnode.GetMetaFunctionCall();
                //    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, mfc);
                //    irList.Add(irCallFun);
                //}               
            }
        }

        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#Call#");
            for (int i = 0; i < irList.Count; i++ )
            {
                sb.AppendLine(irList[i].ToIRString());

            };
            return sb.ToString();
        }
    }
}
