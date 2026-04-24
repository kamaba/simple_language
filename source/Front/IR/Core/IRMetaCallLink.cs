//****************************************************************************
//  File:      IRMetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
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
        public static List<IRBase> ExecOnceCnode(IRMethod _irMethod, MetaVisitNode cnode, int dupcount = 0 )
        {
            List<IRBase> irList = new List<IRBase>();
            if (cnode.visitType == MetaVisitNode.EVisitType.ConstValue)
            {
                IRExpress ire = new IRExpress(_irMethod , cnode.constValueExpress);
                irList.Add(ire);
            }
            else if( cnode.visitType == MetaVisitNode.EVisitType.Express )
            {
                IRExpressBase ire = IRExpressManager.CreateExpress(_irMethod, cnode.express);
                irList.Add(ire);
            }
            else if( cnode.visitType == MetaVisitNode.EVisitType.GetTypeValue )
            {
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(cnode.ownerMetaClass.GetHashCode());
                IRMetaType irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);

                IRData irdata = new IRData();
                irdata.opCode = EIROpCode.Ldc;
                irdata.SetOpValue(irmt);
                irdata.SetDebugInfoByToken(cnode.token, "Ldc type literal");

                IRBase irbase = new IRBase(irdata);
                irList.Add(irbase);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.Variable)
            {
                MetaVariable mv = cnode.GetOrgTemplateMetaVariable();

                IRMetaType irmt = null;
                IRMetaClass irmc = null;
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                if (mv.isStatic || mv.isConst )
                {
                    if (cnode.callMetaType != null)
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                    }
                    else
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.isDefineMetaType ? mv.defineMetaType : mv.realMetaType, owirmc);
                    }
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                else
                {
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                if (irVar == null)
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load variable failed (null IR): {mv?.name}");
                }
                else
                    irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.EnumMember)
            {
                // Enum member is represented as a static/global MetaMemberEnum,
                // but when it is passed into a function whose parameter type is `enum`,
                // the IR typing must use the *declared enum type* (cnode.callMetaType),
                // not the underlying primitive type of the member.
                MetaVariable mv = cnode.variable;

                IRMetaType irmt = null;
                IRMetaClass irmc = null;
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());

                if (mv.isStatic || mv.isConst)
                {
                    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.isDefineMetaType ? mv.defineMetaType : mv.realMetaType, owirmc);

                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                else
                {
                    Debug.Assert(false, "enum类型，不允许使用非const类型!");
                    //irmc = owirmc;
                    //if (cnode.callMetaType != null)
                    //{
                    //    irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                    //}
                    //else
                    //{
                    //    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.isDefineMetaType ? mv.defineMetaType : mv.realMetaType, owirmc);
                    //}
                }

                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                if (irVar == null)
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load enum/variable failed (null IR): {mv?.name}");
                }
                else
                    irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.VisitVariable)
            {
                MetaVisitVariable mv = cnode.visitVariable;

                IRMetaClass irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                IRMetaType irmt = new IRMetaType(irmc);

                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                if (irVar == null)
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load visit target failed (null IR): {mv?.name}");
                }
                else
                    irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var mfc = cnode.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                irCallFun.Parse(mfc);
                irList.Add(irCallFun);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.SystemCall)
            {
                var mfc = cnode.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                irCallFun.ParseSystemCall(mfc);
                irList.Add(irCallFun);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.NewConst)
            {
                IRNewExpress ire = new IRNewExpress(_irMethod, cnode.constValueExpress);
                irList.Add(ire);

                //var mfc = cnode.methodCall;
                //IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                //irCallFun.Parse(mfc);
                //irList.Add(irCallFun);


                //var owirmc = IRManager.instance.GetIRMetaClassById(cnode.variable.ownerMetaClass.GetHashCode());
                //var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(cnode.variable.defineMetaType, owirmc);

                //IRStoreVariable irStoreVar = IRStoreVariable.CreateIRStoreVariable(irmt, owirmc,_irMethod, cnode.variable);

                //irList.Add(irStoreVar);

            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.New)
            {
                //ParseNew(cnode, _irMethod, irList );
                Debug.Assert(false, "New的方法，已经独立于表达式");
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MetaClass)
            {

            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.Enum )
            {

            }
            else
            {
                Debug.Assert(false, $"没有找到:{cnode.visitType} ");
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
