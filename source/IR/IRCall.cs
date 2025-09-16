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
using System.Collections.Generic;
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
            IRMetaClass curMc = null;
            if (mfc.loadMetaVariable != null)
            {
                curMc = m_IRMethod.irManager.GetIRMetaClassByName(mfc.loadMetaVariable.metaDefineType.metaClass.allClassName);
                var irmt = new IRMetaType(mfc.loadMetaVariable.metaDefineType);
                IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, m_IRMethod, mfc.loadMetaVariable );
                AddIRRangeData(irload.IRDataList);
            }
            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                IRExpress irexpress = new IRExpress(m_IRMethod, mfc.metaInputParamList[j] );
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
            IRMetaType irmc = null;

            //IRBase irbase = null;
            if ( mf.isStatic )
            {
                var irname = IRManager.GetIRNameByMetaClass(mfc.staticCallerMetaClass);
                curMc = IRManager.instance.GetIRMetaClassByName(irname);
                //irbase = IRUtil.GetSetCallClassByMetaClass(mfc.staticCallerMetaClass, mfc.staticMetaClassInputTemplateList, out curMc);
                //if (irbase != null)
                //{
                //    AddIRRangeData(irbase.IRDataList);
                //}
            }
            else
            {
                var irname = IRManager.GetIRNameByMetaClass(mf.ownerMetaClass);
                curMc = IRManager.instance.GetIRMetaClassByName(irname);
            }

            m_IRRuntimeMethod = m_IRMethod.irManager.GetIRMethod(mf.functionAllName);
            if ( curMc != null )
            {
                callMethodIndex = curMc.GetIRNonStaticMethodIndexByMethod( mf.virtualFunctionName );

                List<IRMetaType> types = new List<IRMetaType>();
                for( int i = 0; i < mfc.staticMetaClassInputTemplateList.Count; i++ )
                {
                    types.Add(new IRMetaType(mfc.staticMetaClassInputTemplateList[i]));
                }
                irmc = new IRMetaType(curMc, types );
            }
            List<IRMetaType> functionMtList = new List<IRMetaType>();
            for( int i = 0; i < mfc.metaParamInputTemplateList.Count; i++ )
            {
                functionMtList.Add(new IRMetaType(mfc.metaParamInputTemplateList[i]));
            }
           var irmethodcall = new IRMethodCall(irmc, functionMtList, m_IRRuntimeMethod, paramCount );
            if ( callMethodIndex == -1 )
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
                    datacall.opValue = irmethodcall;
                    datacall.index = 0;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
                else
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallDynamic;
                    datacall.opValue = irmethodcall;
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
                datacall.opValue = irmethodcall;
                datacall.SetDebugInfoByToken(mf.pingToken);
                AddIRData(datacall);
            }

            if( mfc.storeMetaVariable == null )
            {
                string voidn = IRManager.GetIRNameByMetaClass(Core.SelfMeta.CoreMetaClassManager.voidMetaClass);
                for (int i = 0; i < m_IRRuntimeMethod.methodReturnVariableList.Count; i++ )
                {
                    var mrv = m_IRRuntimeMethod.methodReturnVariableList[i];
                    if( mrv.irMetaType != null )
                    {
                        if( mrv.irMetaType.irMetaClass.irName != "Void" )
                        {
                            IRPop irpop = new IRPop(m_IRMethod);
                            AddIRData(irpop.data);
                        }
                    }
                }
            }

            //if (irbase != null)
            //{
            //    IRData datacallunsc = new IRData();
            //    datacallunsc.opCode = EIROpCode.UnSetCallClass;
            //    datacallunsc.opValue = null;
            //    datacallunsc.SetDebugInfoByToken(mf.pingToken);
            //    AddIRData(datacallunsc);
            //}
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
