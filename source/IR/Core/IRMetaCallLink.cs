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


namespace SimpleLanguage.Core.IR
{
    public class IRMetaCallLink
    {
        private IRMethod m_IRMethod = null;
        public List<IRBase> irList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod, List<MetaVisitNode> callNodeList, bool isSave = false)
        {
            m_IRMethod = _irMethod;

            //for (int i = 0; i < callNodeList.Count; i++)
            //{
            //    var cnode = callNodeList[i];
            //    if (cnode.visitType == MetaVisitNode.EVisitType.Variable)
            //    {
            //        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, cnode.variable);
            //        irList.Add(irVar);
            //    }
            //    else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
            //    {
            //        IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, cnode.methodCall);
            //        irList.Add(irCallFun);
            //    }
            //    else if (cnode.visitType == MetaVisitNode.EVisitType.VisitVariable)
            //    {
            //    }
            //    else if (cnode.visitType == MetaVisitNode.EVisitType.NewMethodCall)
            //    {
            //        var irnew = new IRNew(m_IRMethod, cnode.GetMetaDefineType());
            //        irList.Add(irnew);

            //        IRCallFunction irCallFun = new IRCallFunction(m_IRMethod, cnode.methodCall);
            //        irList.Add(irCallFun);

            //        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, cnode.variable);
            //        irList.Add(irVar);
            //    }
            //    else
            //    {
            //        Console.WriteLine("Error 不允许有其它形式在CallLink的首位的形式" + cnode.visitType.ToString());
            //    }
            //}
        }
        public void ParseToIRDataListByIRManager( IRManager _irManager, List<MetaVisitNode> callNodeList )
        {
            var cnlist = callNodeList;
            for (int i = 0; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];
                if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue )
                {
                    IRExpress ire = new IRExpress(_irManager, cnode.constValueExpress);
                    irList.Add(ire);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.Variable )
                {
                    MetaVariable mv = cnode.visitVariable;
                    IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv);
                    irList.Add(irVar);
                }
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
            for (int i = 0; i < irList.Count; i++)
            {
                sb.AppendLine(irList[i].ToIRString());

            };
            return sb.ToString();
        }
    }
}
