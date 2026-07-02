//****************************************************************************
//  File:      IRCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage;
using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleLanguage.IR
{
    public class IRCallFunction : IRBase
    {
        public int paramCount { get; set; } = 0;
        public bool target { get; set; } = false;

        private MethodInfo m_MethodInfo = null;
        private IRMethod m_IRRuntimeMethod = null;

        public IRCallFunction(IRMethod _irMethod) : base(_irMethod)
        {
        }

        /// <summary>Prefer member <see cref="MetaFunction.token"/>; fallback to call site from <see cref="MetaMethodCall.token"/>.</summary>
        private void ApplyCallInstructionDebug(IRData datacall, MetaFunction mf, MetaMethodCall mfc)
        {
            var site = mf?.token ?? mfc?.metaMemberFunction?.token;
            string name = m_IRRuntimeMethod?.onlyFunctionName ?? mf?.name ?? "";
            string detail = string.IsNullOrEmpty(name)
                ? datacall.opCode.ToString()
                : $"{datacall.opCode} {name}";
            datacall.SetDebugInfoByToken(site, detail);
        }

        public void ParseSystemCall(MetaMethodCall mfc)
        {
            // Keep the same argument emission pipeline as regular calls.
            IRMetaType irmt = null;
            IRMetaClass owirmc = null;
            //if (mfc.loadMetaVariable != null)
            //{
            //    owirmc = IRManager.GetIRMetaClassByMetaVariable(mfc.loadMetaVariable);
            //    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.defineMetaType, owirmc);
            //    IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable);
            //    AddIRRangeData(irload.IRDataList);
            //}

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                var argNode = mfc.metaInputParamList[j];
                IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, argNode);
                AddIRRangeData(irexpress.IRDataList);
                TryAddDataTypeLiteralFallback(argNode, irexpress);
            }

            var mf = mfc.GetTemplateMemberFunction();
            string systemName = mf?.name ?? string.Empty;
            int systemKind = -1;
            if (!string.IsNullOrEmpty(systemName)
                && SystemMethodCallDeclarationRegistry.TryResolveName(systemName, out var sysEnum))
            {
                systemKind = (int)sysEnum;
            }

            var sysPkg = new SLSystemMethodCallPackage
            {
                name = systemName,
                paramCount = paramCount,
                systemMethodKind = systemKind,
            };

            IRData datacall2 = new IRData();
            datacall2.opCode = EIROpCode.CallSystemMethod;
            // Legacy bridge pops this many stack slots (args only); payload carries full metadata.
            datacall2.index = paramCount;
            datacall2.SetOpValue(sysPkg);
            datacall2.SetDebugInfoByToken(mf?.token ?? mfc?.metaMemberFunction?.token,
                string.IsNullOrEmpty(systemName) ? "CallSystemMethod" : $"CallSystemMethod {systemName}");
            AddIRData(datacall2);
        }
        public void Parse(MetaMethodCall mfc)
        {
            IRMetaType irmt = null;
            IRMetaClass owirmc = null;
            //if (mfc.loadMetaVariable != null)
            //{
            //    owirmc = IRManager.GetIRMetaClassByMetaVariable(mfc.loadMetaVariable);
            //    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.defineMetaType, owirmc);
            //    IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable);
            //    AddIRRangeData(irload.IRDataList);
            //}

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                var argNode = mfc.metaInputParamList[j];
                IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, argNode);
                AddIRRangeData(irexpress.IRDataList);
                TryAddDataTypeLiteralFallback(argNode, irexpress);
            }
            MetaFunction mf = mfc.GetTemplateMemberFunction();
            MetaMemberFunction mmf = mf as MetaMemberFunction;
            
            //MetaMemberFunctionCSharp mmfcsharp = mf as MetaMemberFunctionCSharp;
            //if (mmfcsharp != null)
            //{
            //    m_MethodInfo = mmfcsharp.methodInfo;
            //    IRData data = new IRData();
            //    data.opCode = EIROpCode.Ca;
            //    data.opValue = this;
            //    data.SetDebugInfoByToken(mmfcsharp.GetToken());
            //    AddIRData(data);
            //    return;
            //}


            int callMethodIndex = -1;
            string fname = "";
            IRMetaClass irmc = null;
            List<IRMetaType> types = new List<IRMetaType>();

            int callType = -1;// 0->static call 1->virtual call  2->dynamic call
            if( mfc.staticCallMetaType != null || mmf.isStatic || mmf?.isFinal == true )
            {
                MetaClass scmc = null;
                MetaType staticMt = mfc.staticCallMetaType;
                if (staticMt == null )
                {
                    staticMt = new MetaType(mf.ownerMetaClass);
                }
                if (staticMt.metaClass != null )
                {
                    scmc = staticMt.metaClass;
                    if (scmc != null && scmc is MetaGenTemplateClass mgtc)
                    {
                        scmc = mgtc.metaTemplateClass;
                    }
                    irmc = IRManager.instance.GetIRMetaClassById(scmc.GetHashCode());
                }
                else if(staticMt.metaData != null )
                {
                    irmc = IRManager.instance.GetIRMetaClassById(CoreMetaClassManager.dataMetaClass.GetHashCode());
                }

                if (mf is MetaGenTemplateFunction mgtf)
                {
                    fname = mgtf.sourceMetaMemberFunction.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mgtf.sourceMetaMemberFunction.ownerMetaBase);
                }
                else if (mf is MetaMemberFunction mmf22)
                {
                    if (mmf22.sourceMetaMemberFunction != null)
                    {
                        fname = mmf22.sourceMetaMemberFunction.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.sourceMetaMemberFunction.ownerMetaBase);
                    }
                    else
                    {
                        fname = mmf22.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.ownerMetaBase);
                    }
                }
                else
                {
                    fname = mf.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mf.ownerMetaBase);
                }

                m_IRRuntimeMethod = m_IRMethod.irManager.GetIRMethod(fname);

                var list = staticMt.GetGenTemplateMetaTypeList();
                for (int i = 0; i < list.Count; i++)
                {
                    types.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(list[i], owirmc));
                }
                callType = 0;
            }
            else
            {
                MetaBase ownerBase = null;
                var mmf2 = mf as MetaMemberFunction;
                if (mmf2 != null)
                {
                    ownerBase = mmf2.sourceMetaMemberFunction?.ownerMetaBase ?? mmf2.ownerMetaBase;
                }
                else
                {
                    ownerBase = mf.ownerMetaBase;
                }
                fname = mf.virtualFunctionName;
                irmc = IRManager.GetIRMetaClassByMetaOwner(ownerBase);


                if (mf is MetaGenTemplateFunction mgtf)
                {
                    //fname = mgtf.sourceMetaMemberFunction.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mgtf.sourceMetaMemberFunction.ownerMetaBase);
                }
                else if (mf is MetaMemberFunction mmf22)
                {
                    if (mmf22.sourceMetaMemberFunction != null)
                    {
                        //fname = mmf22.sourceMetaMemberFunction.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.sourceMetaMemberFunction.ownerMetaBase);
                    }
                    else
                    {
                        //fname = mmf22.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.ownerMetaBase);
                    }
                }
                else
                {
                    //fname = mf.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mf.ownerMetaBase);
                }


                m_IRRuntimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                callType = 1;
                if (m_IRRuntimeMethod?.interfaceMethod == true)
                {
                    callType = 2;
                }
            }
            if (m_IRRuntimeMethod == null)
            {
                Log.AddIRLog(LID.MetaCoreAssertShowMessage, mfc.token, $"ir runtime[{fname}] method not found!!");
                return;
            }
            irmt = new IRMetaType(irmc, types);
            List<IRMetaType> functionMtList = new List<IRMetaType>();
            for( int i = 0; i < mfc.metaFunctionInputTemplateList.Count; i++ )
            {
                functionMtList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.metaFunctionInputTemplateList[i], owirmc));
            }
           var irmethodcall = new IRMethodCall(irmt, functionMtList, m_IRRuntimeMethod, paramCount );
            if(callType == 0 )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallStatic;
                datacall.SetOpValue(irmethodcall);
                datacall.index = paramCount;
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else if( callType == 1 )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallVirt;
                datacall.index = callMethodIndex;
                datacall.SetOpValue(irmethodcall);
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else if( callType == 2  )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallDynamic;
                datacall.SetOpValue(irmethodcall);
                datacall.index = paramCount + 1;
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else
            {
                Log.AddIRLog(LID.MetaCoreAssertShowMessage, mfc.token, "aaaa");
            }
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }

        private void TryAddDataTypeLiteralFallback(MetaExpressNodeBase? argNode, IRExpressBase? irexpress)
        {
            if (argNode == null || irexpress == null)
                return;

            if (irexpress.IRDataList.Count > 0)
                return;

            var metaType = argNode.expressReturnMetaType;
            if (argNode is MetaCallLinkExpressNode callLinkNode)
            {
                var metaCallLink = callLinkNode.metaCallLink;
                metaType = metaCallLink?.finalCallNode?.GetMetaType() ?? metaType;
                if (metaCallLink?.visitNodeList != null)
                {
                    for (int i = metaCallLink.visitNodeList.Count - 1; i >= 0; i--)
                    {
                        var visitMetaType = metaCallLink.visitNodeList[i]?.GetMetaType();
                        if (visitMetaType?.metaData != null)
                        {
                            metaType = visitMetaType;
                            break;
                        }
                    }
                }
            }

            var targetMetaData = metaType?.metaData;
            if (targetMetaData == null)
                return;

            int ownerHashCode = argNode.ownerMetaBase != null
                ? argNode.ownerMetaBase.GetHashCode()
                : targetMetaData.GetHashCode();
            var ownerIrMetaClass = IRManager.instance.GetIRMetaClassById(ownerHashCode);
            if (ownerIrMetaClass == null)
                return;

            var irMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(metaType, ownerIrMetaClass);
            var irdata = new IRData();
            irdata.opCode = EIROpCode.LoadConstType;
            irdata.SetOpValue(irMetaType);
            irdata.SetDebugInfoByToken(argNode.token, "LoadConstType data literal fallback");
            AddIRData(irdata);
        }
    }
}
