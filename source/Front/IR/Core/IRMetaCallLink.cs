//****************************************************************************
//  File:      IRMetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
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
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaOwner(cnode.ownerMetaBase)
                    ?? IRManager.GetIRMetaClassByMetaType(cnode.callMetaType)
                    ?? IRManager.instance.GetIRMetaClassByName("Core.Object");
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
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                IRLoadVariable irVar = null;
                if ( cnode.callMetaType != null )
                {
                    irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                    // 枚举常量成员运行时存 Core.Member，defineMetaType 为 extends；LoadStaticField 的 opValue 须为 Member 类型。
                    if (mv is MetaMemberEnum)
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(
                            new MetaType(CoreMetaClassManager.memberMetaClass), owirmc);
                    }
                    else if (cnode.callMetaType != null)
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);

                        int index = irmt.irMetaClass.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                        irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                    }
                    else
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.GetFinalMetaType(), owirmc);
                    }
                }
                else
                {
                    irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                }
                if( irVar == null )
                {
                    irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                }
                if (irVar == null)
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load variable failed (null IR): {mv?.name}");
                }
                else
                    irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.EnumMember)
            {
                // MetaMemberEnum → 静态字段为 Core.Member；values 等为合成成员 → 按其 define/real（如 Array<Member>）。
                // callMetaType 常为声明侧用户 enum，不能优先当作静态字段的运行时存储类型。
                MetaVariable mv = cnode.variable;
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);

                IRMetaType irLoadMt = null;
                if (mv is MetaMemberEnum && owirmc != null)
                {
                    irLoadMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(
                        new MetaType(CoreMetaClassManager.memberMetaClass), owirmc);
                }
                else if (owirmc != null)
                {
                    var srcMt = mv.GetFinalMetaType() ?? mv.defineMetaType ?? mv.realMetaType;
                    if (srcMt != null)
                    {
                        irLoadMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(srcMt, owirmc);
                    }
                }

                if (irLoadMt == null && cnode.callMetaType != null)
                {
                    irLoadMt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                }

                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irLoadMt, owirmc, _irMethod, mv);
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

                IRMetaClass irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
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
                Log.AddIRLog(LID.IRMethodNotSupportNew, cnode.token, $"New expression is not supported in MetaCallLink anymore");
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MetaClass)
            {
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.MetaData)
            {
                //MetaVariable mv = cnode.GetOrgTemplateMetaVariable();

                //IRMetaType irmt = null;
                //IRMetaClass irmc = null;
                //IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                //if (mv.isStatic || mv.isConst)
                //{
                //    irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                //    // 枚举常量成员运行时存 Core.Member，defineMetaType 为 extends；LoadStaticField 的 opValue 须为 Member 类型。
                //    if (mv is MetaMemberEnum)
                //    {
                //        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(
                //            new MetaType(CoreMetaClassManager.memberMetaClass), owirmc);
                //    }
                //    else if (cnode.callMetaType != null)
                //    {
                //        irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                //    }
                //    else
                //    {
                //        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.GetFinalMetaType(), owirmc);
                //    }
                //}
                //else
                //{
                //    irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                //}
                //IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                //if (irVar == null)
                //{
                //    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load variable failed (null IR): {mv?.name}");
                //}
                //else
                //    irList.Add(irVar);
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.Enum )
            {

            }
            else
            {
                Log.AddIRLog(LID.IRNotSupportVisitType, cnode.token, $"Visit type is not supported in MetaCallLink: {cnode.visitType}", cnode.visitType.ToString() );
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
