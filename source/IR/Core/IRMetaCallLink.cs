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
                irList.AddRange(ExecOnceCnode(_irMethod, cnode));
            }
        }
        public static List<IRBase> ExecOnceCnode(IRMethod _irMethod, MetaVisitNode cnode)
        {
            var irmanager = IRManager.instance;
            List<IRBase> irList = new List<IRBase>();
            if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue)
            {
                IRExpress ire = null;
                if (_irMethod != null )
                {
                    ire = new IRExpress(_irMethod , cnode.constValueExpress);
                }
                else
                {
                    ire = new IRExpress(irmanager, cnode.constValueExpress);
                }
                irList.Add(ire);
            }
            else if( cnode.visitType == MetaVisitNode.EVisitType.Express )
            {
                IRExpress ire = null;
                if (_irMethod != null)
                {
                    ire = new IRExpress(_irMethod, cnode.express);
                }
                else
                {
                    ire = new IRExpress(irmanager, cnode.express );
                }
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
                        irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                    }
                    else
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.defineMetaType, owirmc);
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
            else if( cnode.visitType == MetaVisitNode.EVisitType.VisitVariable )
            {
                MetaVisitVariable mv = cnode.visitVariable;

                IRMetaClass irmc = IRManager.instance.GetIRMetaClassById(mv.sourceMetaVariable.GetOwnerClassTemplateClass().GetHashCode());
                IRMetaType irmt = new IRMetaType(irmc);
                /*
                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv.sourceMetaVariable);
                irList.Add(irVar);
                */

                if ( mv.fastVisit )
                {
                    IRData irdata = new IRData();
                    string val = mv.fastVisitConstExpressNode.value.ToString();
                    int.TryParse(val, out int index);
                    irdata.opValue = index;
                    irdata.index = index;
                    irdata.opCode = EIROpCode.LoadArrayIndex;
                    IRBase irbase = new IRBase();
                    irbase.AddIRData(irdata);
                    irList.Add(irbase);
                }
                else
                {
                    IRMetaCallLink irmcl = new IRMetaCallLink();
                    irmcl.ParseToIRDataList(_irMethod, mv.targetMetaVisitCallLink.visitNodeList);
                    irList.AddRange(irmcl.irList);

                    IRData irdata = new IRData();
                    irdata.opValue = 0;
                    irdata.opCode = EIROpCode.LoadArrayIndexField;
                    IRBase irbase = new IRBase();
                    irbase.AddIRData(irdata);
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
            else if (cnode.visitType == MetaVisitNode.EVisitType.New)
            {
                //ParseNew(cnode, _irMethod, irList );
                Debug.Assert(false, "New的方法，已经独立于表达式");
            }
            return irList;
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
