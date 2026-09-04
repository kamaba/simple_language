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

            // Not-null path: receiver is still on the stack (Cne consumed the dup'd copy).
            // Do NOT pop — the subsequent method call / field access needs the receiver.
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
            else if (cnode.visitType == MetaVisitNode.EVisitType.ClosureCall)
            {
                // 闭包调用: 压闭包变量 -> 依序压实参 -> CallClosure
                var mcc = cnode.closureCall;
                if (mcc == null)
                {
                    Log.AddIRLog(LID.MetaCoreAssertShowMessage, cnode.token, "closure call node is null");
                    return irList;
                }

                // 1. 压闭包变量 (宿主上下文: 局部加载; 闭包嵌套上下文: 经捕获代理 LoadArgument0+LoadArrayIndex)
                var loadMv = mcc.loadMetaVariable ?? (MetaVariable)mcc.closureVariable;
                if (loadMv != null)
                {
                    var irVar = IRLoadVariable.CreateLoadVariable(null, null, _irMethod, loadMv);
                    if (irVar == null)
                    {
                        Log.AddIRLog(LID.IRMethodNotFoundVariable, cnode.token, "load closure variable failed (null IR)", loadMv?.name);
                        return irList;
                    }
                    irList.Add(irVar);
                    for (int i = 0; i < irVar.IRDataList.Count; i++)
                    {
                        irVar.IRDataList[i].SetDebugInfoByToken(cnode.token);
                    }
                }

                // 2. 依序压实参表达式
                var plist = mcc.inputParamExpressList;
                for (int i = 0; i < plist.Count; i++)
                {
                    if (plist[i] == null) continue;
                    IRExpressBase irexpress = IRExpressManager.CreateExpress(_irMethod, plist[i]);
                    irList.Add(irexpress);
                }

                // 3. CallClosure: 弹 [closure, arg...] -> 压返回值
                IRMethod closureIRM = null;
                if ( mcc.closureFunction != null )
                {
                    closureIRM = IRClosureDefineStatements.ResolveClosureIRMethod( mcc.closureFunction, cnode.token );
                    if ( closureIRM == null )
                    {
                        return irList;
                    }
                }
                var imc = new IRMethodCall( null, null, closureIRM, plist.Count );
                IRData dataCall = new IRData();
                dataCall.opCode = EIROpCode.CallClosure;
                dataCall.SetOpValue( imc );
                dataCall.index = plist.Count;
                dataCall.SetDebugInfoByToken( cnode.token, "CallClosure " + ( mcc.closureFunction?.name ?? "indirect" ) + " params:" + plist.Count );
                irList.Add( new IRBase( dataCall ) );
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
                ParseNewInCallLink(_irMethod, cnode, irList);
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
        /// <summary>
        /// 链中 New 节点（如 FFI.Library(path).getFunction(...) 的 receiver，或 List<int>().add(...)）：
        /// 参照 IRNewExpress —— NewObject/NewTemplateObject 压新对象，Dup 复制引用，
        /// 压实参后 CallVirt 构造函数；栈上保留的新对象供链中后续节点作为 receiver 使用。
        /// </summary>
        private static void ParseNewInCallLink(IRMethod _irMethod, MetaVisitNode cnode, List<IRBase> irList)
        {
            MetaType newMt = cnode.callMetaType;
            if (newMt == null || newMt.IsArray())
            {
                Log.AddIRLog(LID.IRMethodNotSupportNew, cnode.token,
                    $"New expression in MetaCallLink is not supported for {(newMt == null ? "null type" : "array type")}");
                return;
            }

            IRMetaClass irmc = null;
            IRMetaType newObjectIRMT = null;
            if (newMt.eMetaTypeType == EMetaTypeType.MetaClass
                || newMt.eMetaTypeType == EMetaTypeType.MetaData
                || newMt.eMetaTypeType == EMetaTypeType.MetaEnum)
            {
                irmc = IRManager.GetIRMetaClassByMetaType(newMt)
                    ?? IRManager.instance.GetIRMetaClassById(newMt.metaClass?.classId ?? 0);
                if (irmc == null)
                {
                    Log.AddIRLog(LID.MetaCoreAssertShowMessage, cnode.token,
                        $"New-in-call-link: IRMetaClass not found for {newMt.metaClass?.allName}");
                    return;
                }
                newObjectIRMT = new IRMetaType(irmc);
                irList.Add(new IRNew(_irMethod, irmc));
            }
            else
            {
                // 泛型模板类（MetaGenClass，如 List<int>()）走 NewTemplateObject
                IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaType(newMt)
                    ?? IRManager.instance.GetIRMetaClassById(newMt.GetTemplateMetaClass()?.classId ?? 0)
                    ?? IRManager.instance.GetIRMetaClassById(newMt.metaClass?.classId ?? 0);
                newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(newMt, owirmc);
                irList.Add(new IRNew(_irMethod, newObjectIRMT));
            }

            var mmf = cnode.methodCall?.metaMemberFunction;
            if (mmf == null)
            {
                // 无构造调用：新对象已在栈上
                return;
            }

            // Dup 复制引用：CallVirt 构造消费一份，栈上保留一份作为链中后续 receiver
            irList.Add(new IRDup(_irMethod));

            var paramList = cnode.methodCall.metaInputParamList;
            var paramCount = paramList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                IRExpressBase irexpress = IRExpressManager.CreateExpress(_irMethod, paramList[j]);
                irList.Add(irexpress);
            }

            int callMethodIndex = -1;
            string fname = mmf.virtualFunctionName;
            irmc = IRManager.GetIRMetaClassByMetaType(newMt);
            if (irmc == null)
            {
                MetaClass mc2 = newMt.GetTemplateMetaClass();
                if (mc2 == null)
                    mc2 = mmf.ownerMetaClass;
                if (mc2 == null)
                    mc2 = mmf.sourceMetaMemberFunction?.ownerMetaClass;
                irmc = mc2 != null ? IRManager.instance.GetIRMetaClassById(mc2.classId) : null;
            }
            var runtimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
            if (callMethodIndex == -1 && mmf.sourceMetaMemberFunction != null)
            {
                var sourceMc = mmf.sourceMetaMemberFunction.ownerMetaClass;
                var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.classId) : null;
                if (sourceIrmc != null)
                {
                    var sourceMethod = sourceIrmc.GetIRNonStaticMethodIndexByMethod(fname, out var sourceIndex);
                    if (sourceIndex >= 0)
                    {
                        runtimeMethod = sourceMethod;
                        callMethodIndex = sourceIndex;
                    }
                }
            }
            if (callMethodIndex == -1)
            {
                Log.AddIRLog(LID.MetaCoreAssertShowMessage, cnode.token,
                    $"New-in-call-link: constructor not found for {newMt.metaClass?.allName}");
                return;
            }

            List<IRMetaType> functionMtList = new List<IRMetaType>();
            var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
            IRData datacall = new IRData();
            datacall.opCode = EIROpCode.CallVirt;
            datacall.index = callMethodIndex;
            datacall.opValue = irmethodcall;
            datacall.SetDebugInfoByToken(cnode.token, "CallVirt ctor (new-in-call-link)");
            irList.Add(new IRBase(datacall));
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
