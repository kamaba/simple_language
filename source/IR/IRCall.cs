//****************************************************************************
//  File:      IRCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using System;
using System.Diagnostics;
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
        public void Parse(MetaMethodCall mfc)
        {
            if (mfc?.metaInputParamCollection == null)
            {
                return;
            }
            if (mfc.loadMetaVariable != null)
            {
                IRLoadVariable irload = IRLoadVariable.NewLoadVariable(m_IRMethod, mfc.loadMetaVariable );
                AddIRRangeData(irload.IRDataList);
            }
            paramCount = mfc.metaInputParamCollection.count;
            for (int j = 0; j < paramCount; j++)
            {
                MetaInputParam mip = mfc.metaInputParamCollection.metaInputParamList[j];
                IRExpress irexpress = new IRExpress(m_IRMethod, mip.express);
                AddIRRangeData(irexpress.IRDataList);
            }
            MetaFunction mf = mfc.function;
            MetaMemberFunctionCSharp mmf = mf as MetaMemberFunctionCSharp;
            if (mmf != null)
            {
                m_MethodInfo = mmf.methodInfo;
                IRData data = new IRData();
                data.opCode = EIROpCode.CallCSharpMethod;
                data.opValue = this;
                data.SetDebugInfoByToken(mmf.GetToken());
                AddIRData(data);
                return;
            }

            m_IRRuntimeMethod = m_IRMethod.irManager.GetIRMethod(mf.functionAllName);

            var cc = m_IRMethod.irManager.GetIRMetaClassByName(mfc.callerInstanceClass?.allClassName);

            int callMethodIndex = -1;

            if( cc != null )
            {
                IRData datacallsc = new IRData();
                datacallsc.opCode = EIROpCode.SetCallClass;
                datacallsc.opValue = cc;
                datacallsc.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacallsc);
                callMethodIndex = cc.GetIRNonStaticMethodIndexByMethod( mf.virtualFunctionName );
            }

            if( callMethodIndex == -1 )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.Call;
                datacall.opValue = m_IRRuntimeMethod;
                datacall.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacall);
            }
            else
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallVirt;
                datacall.index = callMethodIndex;
                datacall.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacall);
            }

            if( mfc.storeMetaVariable != null )
            {
                IRPop irpop = new IRPop(m_IRMethod);
                AddIRData(irpop.data);
            }

            if (cc != null)
            {
                IRData datacallunsc = new IRData();
                datacallunsc.opCode = EIROpCode.UnSetCallClass;
                datacallunsc.opValue = null;
                datacallunsc.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacallunsc);
            }
        }
        public System.Object InvokeCSharp( Object target, Object[] csParamObjs)
        {
            if (m_MethodInfo == null)
            {
                Debug.Write("error 执行时发现系统空函数");
                return null;
            }
            return m_MethodInfo.Invoke(target, csParamObjs);
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
}
