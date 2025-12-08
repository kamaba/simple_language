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
            IRMetaType irmt = null;
            IRMetaClass owirmc = null;
            if (mfc.loadMetaVariable != null)
            {
                owirmc = IRManager.instance.GetIRMetaClassById(mfc.loadMetaVariable.GetOwnerClassTemplateClass().GetHashCode());
                irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.metaDefineType, owirmc);
                IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable );
                AddIRRangeData(irload.IRDataList);
            }

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                IRExpress irexpress = new IRExpress(m_IRMethod, mfc.metaInputParamList[j] );
                AddIRRangeData(irexpress.IRDataList);
            }
            MetaFunction mf = mfc.GetTemplateMemberFunction();
            MetaMemberFunctionCSharp mmfcsharp = mf as MetaMemberFunctionCSharp;
            if (mmfcsharp != null)
            {
                m_MethodInfo = mmfcsharp.methodInfo;
                IRData data = new IRData();
                data.opCode = EIROpCode.CallCSharpMethod;
                data.opValue = this;
                data.SetDebugInfoByToken(mmfcsharp.GetToken());
                AddIRData(data);
                return;
            }


            int callMethodIndex = -1;


            string fname = "";
            IRMetaClass irmc = null;
            if ( mf.isStatic )
            {
                var scmc = mfc.staticCallerMetaClass;
                if( scmc != null && scmc is MetaGenTemplateClass mgtc )
                {
                    scmc = mgtc.metaTemplateClass;
                }
                irmc = IRManager.instance.GetIRMetaClassById(scmc.GetHashCode());

                if ( mf is MetaGenTemplateFunction mgtf )
                {
                    fname = mgtf.sourceMetaMemberFunction.functionAllName;
                    owirmc = IRManager.instance.GetIRMetaClassById(mgtf.sourceMetaMemberFunction.ownerMetaClass.GetHashCode());
                }
                else if( mf is MetaMemberFunction mmf22 )
                {
                    if(mmf22.sourceMetaMemberFunction != null )
                    {
                        fname = mmf22.sourceMetaMemberFunction.functionAllName;
                        owirmc = IRManager.instance.GetIRMetaClassById(mmf22.sourceMetaMemberFunction.ownerMetaClass.GetHashCode());
                    }
                    else
                    {
                        fname = mmf22.functionAllName;
                        owirmc = IRManager.instance.GetIRMetaClassById(mmf22.ownerMetaClass.GetHashCode());
                    }
                }
                else
                {
                    fname = mf.functionAllName;
                    owirmc = IRManager.instance.GetIRMetaClassById(mf.ownerMetaClass.GetHashCode());
                }

                m_IRRuntimeMethod = m_IRMethod.irManager.GetIRMethod(fname);
            }
            else
            {
                MetaClass mc2 = null;
                var mmf2 = (mf as MetaMemberFunction);
                if ( mmf2 != null )
                {
                    if (mmf2.sourceMetaMemberFunction != null)
                        mc2 = mmf2.sourceMetaMemberFunction.ownerMetaClass;
                    else
                        mc2 = mmf2.ownerMetaClass;
                }
                else
                {
                    mc2 = mf.ownerMetaClass;
                }
                fname = mf.virtualFunctionName;
                irmc = IRManager.instance.GetIRMetaClassById(mc2.GetHashCode());


                m_IRRuntimeMethod = irmc.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
            }
            List<IRMetaType> types = new List<IRMetaType>();
            for (int i = 0; i < mfc.staticMetaClassInputTemplateList.Count; i++)
            {
                types.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.staticMetaClassInputTemplateList[i], owirmc));
            }
            irmt = new IRMetaType(irmc, types);
            List<IRMetaType> functionMtList = new List<IRMetaType>();
            for( int i = 0; i < mfc.metaFunctionInputTemplateList.Count; i++ )
            {
                functionMtList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.metaFunctionInputTemplateList[i], owirmc));
            }
           var irmethodcall = new IRMethodCall(irmt, functionMtList, m_IRRuntimeMethod, paramCount );
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
                if(m_IRRuntimeMethod.interfaceMethod )
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallDynamic;
                    datacall.opValue = irmethodcall;
                    datacall.index = paramCount + 1;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
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

            }

            if( mfc.isRecieveReturnValue == false )
            {
                for (int i = 0; i < m_IRRuntimeMethod.methodReturnVariableList.Count; i++ )
                {
                    var mrv = m_IRRuntimeMethod.methodReturnVariableList[i];
                    if( mrv.irMetaType != null )
                    {
                        if( mrv.irMetaType.templateIndex > -1 )
                        {

                        }
                        else
                        {
                            IRPop irpop = new IRPop(m_IRMethod);
                            //AddIRData(irpop.data);
                        }
                    }
                }
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
