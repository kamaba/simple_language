//****************************************************************************
//  File:      IRMetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;
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
                irList.AddRange(ExecOnceCnode(_irMethod, cnode));
            }
        }
        public static List<IRBase> ExecOnceCnode( IRMethod _irMethod, MetaVisitNode cnode )
        {
            List<IRBase> irList = new List<IRBase>();
            if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue)
            {
                IRExpress ire = new IRExpress(_irMethod, cnode.constValueExpress);
                irList.Add(ire);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.Variable)
            {
                MetaVariable mv = cnode.variable;


                bool isAddTemplate = false;
                IRMetaClass irmc = null;
                if( mv.metaDefineType.isTemplate )
                {
                    irmc = _irMethod.irManager.GetIRMetaClassByName(mv.metaDefineType.metaTemplate.ownerClass.allClassName);
                }
                else
                {
                    irmc = _irMethod.irManager.GetIRMetaClassByName(mv.metaDefineType.metaClass.allClassName);
                }
                if (cnode.callerMetaClass != null)
                {
                    if (cnode.genTemplateMetaClass != null)
                    {
                        IRData sc2 = new IRData();
                        sc2.opCode = EIROpCode.SetCallClass;
                        irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.genTemplateMetaClass.allClassName);
                        sc2.opValue = irmc;
                        IRBase irbase22 = new IRBase(sc2);
                        irList.Add(irbase22);
                    }
                    else if (cnode.callerMetaClass?.isTemplateClass == true)
                    {
                        IRData sc2 = new IRData();
                        sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                        IRBase irbase22 = new IRBase(sc2);
                        irList.Add(irbase22);
                    }
                    isAddTemplate = true;
                }
                IRLoadVariable irVar = IRLoadVariable.NewLoadVariable(_irMethod, irmc, mv);
                irList.Add(irVar);

                if(isAddTemplate)
                {
                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.UnSetCallClass;
                    IRBase irbase = new IRBase(sc2);
                    irList.Add(irbase);
                }
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var mfc = cnode.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                irCallFun.Parse(mfc);
                irList.Add(irCallFun);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.NewTemplate)
            {
                IRBase irbase = new IRBase();
                var mv = cnode.GetRetMetaVariable();
                IRMetaClass irmc = null;
                if ( mv != null )
                {
                    if( cnode.callerMetaTemplate != null )
                    {
                        irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.callerMetaTemplate.ownerClass.allClassName);
                    }
                    else if (cnode.genTemplateMetaClass != null)
                    {
                        irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.genTemplateMetaClass.allClassName);
                    }
                    IRLoadVariable irVar = IRLoadVariable.NewLoadVariable(_irMethod, irmc, mv);
                    irList.Add(irVar);

                    //IRData pop1 = new IRData();
                    //pop1.opCode = EIROpCode.Pop;
                    //irbase.AddIRData(pop1);
                    //irList.Add(irbase);
                }
                IRNew irnew = IRNew.CreateNew(_irMethod, irmc, true );
                irList.Add(irnew);

                if (cnode.methodCall != null)
                {
                    //System.Type t = typeof(IRDup);

                    //IRDup irdup = new IRDup(_irMethod);
                    //irList.Add(irdup);
                    //IRBase irbase2 = new IRBase();
                    //IRData sc2 = new IRData();
                    //sc2.opCode = EIROpCode.SetCallClass;
                    ////sc2.opValue = irmc;
                    //irbase2.AddIRData(sc2);

                    //var mfc = cnode.methodCall;
                    //var paramCount = mfc.metaInputParamCollection.count;
                    //for (int j = 0; j < paramCount; j++)
                    //{
                    //    MetaInputParam mip = mfc.metaInputParamCollection.metaInputParamList[j];
                    //    IRExpress irexpress = new IRExpress(_irMethod, mip.express);
                    //    irList.Add(irexpress);
                    //}
                    //MetaFunction mf = mfc.function;

                    //var rmr = _irMethod.irManager.GetIRMethod(mf.functionAllName);

                    //IRData datacall = new IRData();
                    //datacall.opCode = EIROpCode.Call;
                    //datacall.opValue = rmr;
                    //datacall.SetDebugInfoByToken(mf.pingToken);
                    //irbase.AddIRData(datacall);


                    //IRData sc3 = new IRData();
                    //sc3.opCode = EIROpCode.UnSetCallClass;
                    //irbase2.AddIRData(sc3);

                    //irList.Add(irbase2);
                }
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.NewClass)
            {
                var irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.callerMetaClass.allClassName);
                IRNew irnew = IRNew.CreateNew(_irMethod, irmc, false);
                irList.Add(irnew);

                if (irmc.IsCoreMetaClass() == false)
                {
                    if (irmc.localIRMetaVariableList.Count > 0)
                    {
                        bool isUseAssign = false;
                        for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                        {
                            var lirmv = irmc.localIRMetaVariableList[x];
                            if (cnode.metaBraceStatementsContent?.assignStatementsList?.Count > 0)
                            {
                                //var irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.variable.metaDefineType.metaClass.allClassName);
                                IRLoadVariable irlv = IRLoadVariable.NewLoadVariable(_irMethod, irmc, cnode.variable );
                                irList.Add(irlv);
                                for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                                {
                                    var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                                    if (asl.metaMemberVariable.name == lirmv.name)
                                    {
                                        IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                                        irList.Add(irexp);

                                        IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(_irMethod, lirmv.index, IRMetaVariableFrom.Member);
                                        irList.Add(irStoreNodeVar3);
                                        isUseAssign = true;
                                        break;
                                    }
                                }

                                if (isUseAssign == false)
                                {
                                    IRExpress irexp = new IRExpress(_irMethod, lirmv.express);
                                    irList.Add(irexp);

                                    IRStoreVariable irStoreVar2 = new IRStoreVariable(_irMethod, lirmv.index, IRMetaVariableFrom.Member);
                                    irList.Add(irStoreVar2);

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (cnode.metaBraceStatementsContent != null && cnode.metaBraceStatementsContent.assignStatementsList.Count > 0)
                    {
                        IRLoadVariable irlv = IRLoadVariable.NewLoadVariable(_irMethod, irmc, cnode.variable );
                        irList.Add(irlv);
                        for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                        {
                            var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                            IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                            irList.Add(irexp);

                            IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(_irMethod, cnode.variable.GetHashCode(), IRMetaVariableFrom.LocalStatement);
                            irList.Add(irStoreNodeVar3);
                        }
                    }
                }

                if (cnode.methodCall != null)
                {
                    IRDup irdup = new IRDup(_irMethod);
                    irList.Add(irdup);
                    IRBase irbase = new IRBase();
                    IRData sc1 = new IRData();
                    sc1.opCode = EIROpCode.SetCallClass;
                    sc1.opValue = irmc;
                    irbase.AddIRData(sc1);

                    var mfc = cnode.methodCall;
                    var paramCount = mfc.metaInputParamCollection.count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        MetaInputParam mip = mfc.metaInputParamCollection.metaInputParamList[j];
                        IRExpress irexpress = new IRExpress(_irMethod, mip.express);
                        irList.Add(irexpress);
                    }
                    MetaFunction mf = mfc.function;

                    var rmr = _irMethod.irManager.GetIRMethod(mf.functionAllName);


                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.Call;
                    datacall.opValue = rmr;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    irbase.AddIRData(datacall);


                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.UnSetCallClass;
                    irbase.AddIRData(sc2);

                    irList.Add(irbase);
                }
            }

            return irList;
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
                        var irmc = _irManager.GetIRMetaClassByName(mv.metaDefineType.metaClass.allClassName);
                        IRLoadVariable irVar = IRLoadVariable.NewLoadVariable(m_IRMethod, irmc, mv);
                        irList.Add(irVar);
                    }
                    else
                    {
                        Log.AddGenIR( EError.None, "Error VM IRMetaCall 该位置不应该有非静态变量");
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
                    var irmc = _irManager.GetIRMetaClassByName(cnode.callerMetaClass.allClassName);
                    IRNew irnew = IRNew.CreateNew(m_IRMethod, irmc, false);
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
