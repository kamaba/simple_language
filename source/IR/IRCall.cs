//****************************************************************************
//  File:      IRCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;

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

            IRMetaClass curMc = null;
            if (mfc.loadMetaVariable != null)
            {
                curMc = m_IRMethod.irManager.GetIRMetaClassByName(mfc.loadMetaVariable.metaDefineType.metaClass.allClassName);
                IRLoadVariable irload = IRLoadVariable.NewLoadVariable(m_IRMethod, curMc, mfc.loadMetaVariable );
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


            int callMethodIndex = -1;

            IRBase irbase = null;
            if ( mf.isStatic )
            {
                irbase = IRUtil.GetSetCallClass(mfc.callerMetaType, mf.ownerMetaClass, out curMc);
                if (irbase != null)
                {
                    AddIRRangeData(irbase.IRDataList);
                }
            }
            else
            {
                IRUtil.GetSetCallClass(mfc.callerMetaType, mf.ownerMetaClass, out curMc);
            }

            m_IRRuntimeMethod = m_IRMethod.irManager.GetIRMethod(mf.functionAllName);
            if ( curMc != null )
            {
                callMethodIndex = curMc.GetIRNonStaticMethodIndexByMethod( mf.virtualFunctionName );
            }

            if( callMethodIndex == -1 )
            {
                if( m_IRRuntimeMethod == null )
                {
                    Log.AddVM(EError.None, "------------没有找到调用的方法体!!");
                    return;
                }

                if( mf.isStatic )
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallStatic;
                    datacall.opValue = m_IRRuntimeMethod;
                    datacall.index = 0;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
                else
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallDynamic;
                    datacall.opValue = m_IRRuntimeMethod;
                    datacall.index = paramCount + 1;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
            }
            else
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallVirt;
                datacall.index = callMethodIndex;
                datacall.opValue = paramCount + 1;
                datacall.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacall);
            }

            if( mfc.storeMetaVariable == null )
            {
                string voidn = IRManager.GetIRNameByMetaClass(Core.SelfMeta.CoreMetaClassManager.voidMetaClass);
                for (int i = 0; i < m_IRRuntimeMethod.methodReturnVariableList.Count; i++ )
                {
                    var mrv = m_IRRuntimeMethod.methodReturnVariableList[i];
                    if( mrv.irMetaClass != null )
                    {
                        if( mrv.irMetaClass.irName != voidn )
                        {
                            IRPop irpop = new IRPop(m_IRMethod);
                            AddIRData(irpop.data);
                        }
                    }
                }
            }

            if (irbase != null)
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
