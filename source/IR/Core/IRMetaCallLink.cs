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
using System.Collections;
using System.Collections.Generic;
using System.Text;


namespace SimpleLanguage.Core.IR
{
    public class IRMetaCallLink
    {
        private IRMethod m_IRMethod = null;
        public List<IRBase> irList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod, List<MetaVisitNode> cnlist, bool isSave = false)
        {
            m_IRMethod = _irMethod;

            for (int i = 0; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];
                if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue)
                {
                    IRExpress ire = new IRExpress(_irMethod, cnode.constValueExpress);
                    irList.Add(ire);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.Variable)
                {
                    MetaVariable mv = cnode.variable;
                    if (mv.variableFrom == MetaVariable.EVariableFrom.Static
                        || mv.variableFrom == MetaVariable.EVariableFrom.Global )
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod.irManager, mv.GetHashCode());
                        irList.Add(irVar);
                    }
                    else
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv.GetHashCode());
                        irList.Add(irVar);
                    }
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
                {
                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(cnode.methodCall);
                    irList.Add(irCallFun);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.NewClassMethodCall)
                {
                    var irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.callerMetaClass.allName);
                    IRNew irnew = new IRNew(m_IRMethod, irmc);
                    irList.Add(irnew);

                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(cnode.methodCall);
                    irList.Add(irCallFun);
                }
            }
        }
        public void ParseToIRDataListByIRManager( IRManager _irManager, List<MetaVisitNode> cnlist)
        {
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
                    MetaVariable mv = cnode.variable;
                    if (mv.variableFrom == MetaVariable.EVariableFrom.Static
                        || mv.variableFrom == MetaVariable.EVariableFrom.Global)
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod.irManager, mv.GetHashCode());
                        irList.Add(irVar);
                    }
                    else
                    {
                        Console.WriteLine("Error VM IRMetaCall 该位置不应该有非静态变量");
                    }
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
                {
                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(cnode.methodCall);
                    irList.Add(irCallFun);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.NewClassMethodCall )
                {
                    var irmc = _irManager.GetIRMetaClassByName(cnode.callerMetaClass.allName);
                    IRNew irnew = new IRNew( m_IRMethod, irmc);
                    irList.Add(irnew);

                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(cnode.methodCall);
                    irList.Add(irCallFun);
                }
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
