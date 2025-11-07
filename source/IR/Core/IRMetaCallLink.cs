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
        public static List<IRBase> ExecOnceCnode(IRMethod _irMethod, MetaVisitNode cnode)
        {
            List<IRBase> irList = new List<IRBase>();
            if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue)
            {
                IRExpress ire = new IRExpress(_irMethod, cnode.constValueExpress);
                irList.Add(ire);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.Variable)
            {
                MetaVariable mv = cnode.GetOrgTemplateMetaVariable();

                IRMetaType irmt = null;
                IRMetaClass irmc = null;
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                if ( mv.isStatic )
                {
                    if( cnode.callMetaType != null )
                    {
                        irmt = new IRMetaType(cnode.callMetaType, owirmc);
                    }
                    else
                    {
                        irmt = new IRMetaType(mv.metaDefineType, owirmc);
                    }
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                else
                {
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var mfc = cnode.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                irCallFun.Parse(mfc);
                irList.Add(irCallFun);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.New)
            {
                ParseNew(cnode, _irMethod, irList );
            }

            return irList;
        }
        public static void ParseNew(MetaVisitNode cnode, IRMethod _irMethod, List<IRBase> irList )
        {
            IRMetaClass irmc = IRManager.instance.GetIRMetaClassById(cnode.callMetaType.GetTemplateMetaClass().GetHashCode());
            IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(cnode.variable.GetOwnerClassTemplateClass().GetHashCode());
            if (cnode.callMetaType.eType == EMetaTypeType.TemplateClassWithTemplate
                || cnode.callMetaType.eType == EMetaTypeType.Template)
            {
                var irnew = new IRNew(_irMethod, new IRMetaType(cnode.callMetaType, owirmc ));
                irList.Add(irnew);
            }
            else if (cnode.callMetaType.eType == EMetaTypeType.MetaClass)
            {
                IRNew irnew = new IRNew(_irMethod, irmc);
                irList.Add(irnew);

                if (irmc.needCallInitMethod == false)
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
                                IRLoadVariable irlv = IRLoadVariable.CreateLoadVariable(null, irmc, _irMethod, cnode.GetOrgTemplateMetaVariable());
                                irList.Add(irlv);
                                for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                                {
                                    var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                                    if (asl.metaMemberVariable.name == lirmv.name)
                                    {
                                        IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                                        irList.Add(irexp);

                                        IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(null, _irMethod, lirmv.index, IRMetaVariableFrom.Member);
                                        irList.Add(irStoreNodeVar3);
                                        isUseAssign = true;
                                        break;
                                    }
                                }

                                if (isUseAssign == false)
                                {
                                    IRExpress irexp = new IRExpress(_irMethod, lirmv.express);
                                    irList.Add(irexp);

                                    IRStoreVariable irStoreVar2 = new IRStoreVariable(null, _irMethod, lirmv.index, IRMetaVariableFrom.Member);
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
                        IRLoadVariable irlv = IRLoadVariable.CreateLoadVariable(null, irmc, _irMethod, cnode.GetOrgTemplateMetaVariable());
                        irList.Add(irlv);
                        for (int y = 0; y < cnode.metaBraceStatementsContent.assignStatementsList.Count; y++)
                        {
                            var asl = cnode.metaBraceStatementsContent.assignStatementsList[y];

                            IRExpress irexp = new IRExpress(_irMethod, asl.expressNode);
                            irList.Add(irexp);

                            IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(null, _irMethod, cnode.GetOrgTemplateMetaVariable().GetHashCode(), IRMetaVariableFrom.LocalStatement);
                            irList.Add(irStoreNodeVar3);
                        }
                    }
                }
            }

            if (cnode.methodCall != null  )
            {
                IRDup irdup = new IRDup(null);
                irList.Add(irdup);

                IRCallFunction ircf = new IRCallFunction(null);
                ircf.Parse(cnode.methodCall);
                irList.Add(ircf);
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
                    MetaVariable mv = cnode.GetOrgTemplateMetaVariable();
                    if (mv.variableFrom == MetaVariable.EVariableFrom.Static
                        || mv.variableFrom == MetaVariable.EVariableFrom.Global)
                    {
                        IRMetaClass irmc = IRManager.instance.GetIRMetaClassById(mv.ownerMetaClass.GetHashCode());
                        var irmt = new IRMetaType(mv.metaDefineType, irmc);
                        IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, m_IRMethod, mv);
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
                else if (cnode.visitType == MetaVisitNode.EVisitType.New )
                {
                    ParseNew(cnode, null, irList);
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
