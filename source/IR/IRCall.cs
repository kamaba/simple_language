//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRCallFunction : IRBase
    {
        public int paramCount { get; set; } = 0;
        public bool target { get; set; } = false;

        private MethodInfo m_MethodInfo = null;
        public IRCallFunction(IRMethod _irMethod) : base(_irMethod)
        {
        }
        void Parse(MetaMethodCall mfc)
        {
            if (mfc?.metaInputParamCollection == null)
            {
                return;
            }
            if( !mfc.isStaticCall )
            {
                if(mfc.callerMetaVariable!=null )
                {
                    IRLoadVariable irload = new IRLoadVariable(m_IRMethod, mfc.callerMetaVariable);
                    AddIRRangeData(irload.IRDataList);
                }
                else
                {
                    Console.WriteLine("Error 没有加载到调用者信息!");
                    return;
                }
            }
            paramCount = mfc.metaInputParamCollection.count;
            for (int j = 0; j < paramCount; j++)
            {
                MetaInputParam mip = mfc.metaInputParamCollection.metaParamList[j] as MetaInputParam;
                IRExpress irexpress = new IRExpress(m_IRMethod, mip.express);
                AddIRRangeData(irexpress.IRDataList);
            }
            MetaFunction mf = mfc.function;
            if (mf is MetaMemberFunction)
            {
                MetaMemberFunction mmf = mf as MetaMemberFunction;
                if (mmf.isCSharp)
                {
                    m_MethodInfo = mmf.methodInfo;

                    IRData data = new IRData();
                    data.opCode = EIROpCode.CallCSharpMethod;
                    data.opValue = this;
                    data.SetDebugInfoByToken( mmf.GetToken() );
                    AddIRData(data);
                    return;
                }
            }
            IRData datacall = new IRData();
            datacall.opCode = EIROpCode.Call;
            datacall.opValue = this;
            datacall.SetDebugInfoByToken( mf.pingToken );
            AddIRData(datacall);
        }
        public System.Object InvokeCSharp( Object target, Object[] csParamObjs)
        {
            if (m_MethodInfo == null)
            {
                Console.WriteLine("error 执行时发现系统空函数");
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
