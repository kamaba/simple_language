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
            irList.AddRange(ProcessVisitNodeList(_irMethod, cnlist, 0));
        }

        private static List<IRBase> ProcessVisitNodeList(IRMethod _irMethod, List<MetaVisitNode> cnlist, int startIndex)
        {
            List<IRBase> irList = new List<IRBase>();

            for (int i = startIndex; i < cnlist.Count; i++)
            {
                var cnode = cnlist[i];

                if (cnode.isQuestionMarkDot && i > startIndex)
                {
                    var nullCheckIR = BuildNullConditionalCheck(_irMethod, cnode, cnlist, i);
                    irList.AddRange(nullCheckIR);
                    return irList;
                }

                // MetaData visit 仅在后面紧跟 MethodCall/SystemCall 时才生成 LoadConstType（作为 this 传入）。
                // 否则跳过（如 GlobalCounter.field 走 LoadStaticField，不需要 TypeObject 压栈，避免栈不平衡）。
                if (cnode.visitType == MetaVisitNode.EVisitType.MetaData)
                {
                    bool hasNextMethodCall = (i + 1 < cnlist.Count) &&
                        (cnlist[i + 1].visitType == MetaVisitNode.EVisitType.MethodCall ||
                         cnlist[i + 1].visitType == MetaVisitNode.EVisitType.SystemCall);
                    if (!hasNextMethodCall)
                    {
                        continue;
                    }
                }

                irList.AddRange(ExecOnceCnode(_irMethod, cnode));
            }

            return irList;
        }

        private static List<IRBase> BuildNullConditionalCheck(IRMethod _irMethod, MetaVisitNode qmdNode, List<MetaVisitNode> cnlist, int qmdIndex)
        {
            List<IRBase> irList = new List<IRBase>();

            IRData dupData = new IRData();
            dupData.opCode = EIROpCode.Dup;
            dupData.SetDebugInfoByToken(qmdNode.token, "?. dup receiver");
            irList.Add(new IRBase(dupData));

            IRData nullData = new IRData();
            nullData.opCode = EIROpCode.LoadConstNull;
            nullData.SetDebugInfoByToken(qmdNode.token, "?. load null");
            irList.Add(new IRBase(nullData));

            IRData cneData = new IRData();
            cneData.opCode = EIROpCode.Cne;
            cneData.SetDebugInfoByToken(qmdNode.token, "?. Cne (not null check)");
            irList.Add(new IRBase(cneData));

            IRData elseLabelData = new IRData();
            IRData endLabelData = new IRData();

            IRBranch ifBranch = new IRBranch(_irMethod, EIROpCode.BrFalse, elseLabelData);
            irList.Add(ifBranch);

            IRData popDupData = new IRData();
            popDupData.opCode = EIROpCode.Pop;
            popDupData.SetDebugInfoByToken(qmdNode.token, "?. pop dup, not null path");
            irList.Add(new IRBase(popDupData));

            irList.AddRange(ProcessVisitNodeList(_irMethod, cnlist, qmdIndex));

            IRBranch endBranch = new IRBranch(_irMethod, EIROpCode.Br, endLabelData);
            irList.Add(endBranch);

            irList.Add(new IRBase(elseLabelData));

            IRData popElseData = new IRData();
            popElseData.opCode = EIROpCode.Pop;
            popElseData.SetDebugInfoByToken(qmdNode.token, "?. pop receiver, null path");
            irList.Add(new IRBase(popElseData));

            IRData nullResultData = new IRData();
            nullResultData.opCode = EIROpCode.LoadConstNull;
            nullResultData.SetDebugInfoByToken(qmdNode.token, "?. result null");
            irList.Add(new IRBase(nullResultData));

            irList.Add(new IRBase(endLabelData));

            return irList;
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
                irdata.opCode = EIROpCode.LoadConstType;
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
                {
                    irList.Add(irVar);
                    for (int i = 0; i < irVar.IRDataList.Count; i++)
                    {
                        irVar.IRDataList[i].SetDebugInfoByToken(cnode.token);
                    }
                }
            }
            else if (cnode.visitType == MetaVisitNode.EVisitType.EnumMember)
            {
                // MetaMemberEnum → 静态字段为 Core.Member；values 等为合成成员 → 按其 define/real（如 Array<Member>）。
                // callMetaType 常为声明侧用户 enum，不能优先当作静态字段的运行时存储类型。
                MetaVariable mv = cnode.variable;
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);

                IRMetaType irLoadMt = null;
                if ( owirmc != null)
                {
                    irLoadMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(
                        new MetaType(CoreMetaClassManager.memberMetaClass), owirmc);
                }

                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irLoadMt, owirmc, _irMethod, mv);
                if (irVar == null)
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, $"load enum/variable failed (null IR): {mv?.name}");
                }
                else
                {
                    irList.Add(irVar);
                    for (int i = 0; i < irVar.IRDataList.Count; i++)
                    {
                        irVar.IRDataList[i].SetDebugInfoByToken(cnode.token);
                    }
                }
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
                {
                    irList.Add(irVar);
                    for (int i = 0; i < irVar.IRDataList.Count; i++)
                    {
                        irVar.IRDataList[i].SetDebugInfoByToken(cnode.token);
                    }
                }
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
                // 为 data 类型名直接调用（如 Student.toString()）生成 LoadConstType，
                // 将 data 类型作为 TypeObject 压栈，供后续 SystemBuildDataString 等识别并输出静态字段。
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaType(cnode.callMetaType)
                    ?? IRManager.instance.GetIRMetaClassByName("Core.Object");
                IRMetaType irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);

                IRData irdata = new IRData();
                irdata.opCode = EIROpCode.LoadConstType;
                irdata.SetOpValue(irmt);
                irdata.SetDebugInfoByToken(cnode.token, "Ldc data type literal");

                IRBase irbase = new IRBase(irdata);
                irList.Add(irbase);
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
