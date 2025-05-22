//****************************************************************************
//  File:      IRMetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace SimpleLanguage.Core.IR
{
    public class IRMetaCallLink
    {
        private IRMethod m_IRMethod = null;
        public List<IRBase> irList = new List<IRBase>();

        public void ParseToIRDataList(IRMethod _irMethod, List<MetaVisitNode> cnlist)
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
                    else if( mv.variableFrom == MetaVariable.EVariableFrom.Member )
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, (mv as MetaMemberVariable).index, IRMetaVariableFrom.Member);
                        irList.Add(irVar);
                    }
                    else if( mv.variableFrom == MetaVariable.EVariableFrom.Argument )
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv.GetHashCode(), IRMetaVariableFrom.Argument );
                        irList.Add(irVar);
                    }
                    else
                    {
                        IRLoadVariable irVar = new IRLoadVariable(m_IRMethod, mv.GetHashCode(), IRMetaVariableFrom.LocalStatement );
                        irList.Add(irVar);
                    }
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
                {
                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(mfc);
                    irList.Add(irCallFun);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.NewClass)
                {
                    var irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.callerMetaClass.allName);
                    IRNew irnew = new IRNew(m_IRMethod, irmc);
                    irList.Add(irnew);

                    if( cnode.variable != null )
                    {
                        IRStoreVariable irsv = new IRStoreVariable(m_IRMethod, cnode.variable.GetHashCode(), IRMetaVariableFrom.LocalStatement);
                        irList.Add(irsv);

                        IRLoadVariable irlv = new IRLoadVariable(m_IRMethod, cnode.variable.GetHashCode(), IRMetaVariableFrom.LocalStatement);
                        irList.Add(irlv);
                    }

                    if( irmc.IsCoreMetaClass() == false )
                    {
                        bool isUseAssign = false;
                        for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                        {
                            var lirmv = irmc.localIRMetaVariableList[x];
                            if (cnode.metaBraceStatementsContent?.assignStatementsList?.Count > 0)
                            {
                                for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                                {
                                    var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                                    if (asl.metaMemberVariable.allName == lirmv.name)
                                    {
                                        IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                                        irList.Add(irexp);

                                        IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(_irMethod, lirmv.index, IRMetaVariableFrom.Member2);
                                        irList.Add(irStoreNodeVar3);
                                        isUseAssign = true;
                                        break;
                                    }
                                }

                                if (isUseAssign == false)
                                {
                                    IRExpress irexp = new IRExpress(_irMethod, lirmv.express);
                                    irList.Add(irexp);

                                    IRStoreVariable irStoreVar2 = new IRStoreVariable(_irMethod, lirmv.index, IRMetaVariableFrom.Member2);
                                    irList.Add(irStoreVar2);

                                }
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                        {
                            var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                            IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                            irList.Add(irexp);

                            IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(_irMethod, cnode.variable.GetHashCode(), IRMetaVariableFrom.LocalStatement );
                            irList.Add(irStoreNodeVar3);
                        }
                    }
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
                        Debug.Write("Error VM IRMetaCall 该位置不应该有非静态变量");
                    }
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
                {
                    var mfc = cnode.methodCall;
                    IRCallFunction irCallFun = new IRCallFunction(m_IRMethod);
                    irCallFun.Parse(cnode.methodCall);
                    irList.Add(irCallFun);
                }
                else if (cnode.visitType == MetaVisitNode.EVisitType.NewClass )
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
