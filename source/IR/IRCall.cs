//****************************************************************************
//  File:      IRCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using SimpleLanguage.VM;
using SimpleLanguage.VM.Runtime;
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
                irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.defineMetaType, owirmc);
                IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable);
                AddIRRangeData(irload.IRDataList);
            }

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, mfc.metaInputParamList[j]);
                AddIRRangeData(irexpress.IRDataList);
            }
            MetaFunction mf = mfc.GetTemplateMemberFunction();
            // Special-case: calls that request a Type object for a variable (e.g. `c.type()`)
            // are represented as a MetaMethodCall where the target function's owner meta-class
            // is the Type meta-class. In that case call the runtime helper to obtain a TypeObject.
            if (mf != null && mf.ownerMetaClass == SimpleLanguage.Core.CoreMetaClassManager.typeMetaClass)
            {
                // expect the instance (SObject) to be already on the value stack via IRLoadVariable emitted above
                m_MethodInfo = typeof(SimpleLanguage.Lib.ObjectClass).GetMethod("GetObjectType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (m_MethodInfo != null)
                {
                    // ensure paramCount reflects the single parameter expected by GetObjectType
                    paramCount = 1;

                    IRData data = new IRData();
                    data.opCode = EIROpCode.CallCSharpMethod;
                    data.SetOpValue(this);
                    data.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(data);
                    return;
                }
            }
            MetaMemberFunctionCSharp mmfcsharp = mf as MetaMemberFunctionCSharp;
            if (mmfcsharp != null)
            {
                m_MethodInfo = mmfcsharp.methodInfo;
                IRData data = new IRData();
                data.opCode = EIROpCode.CallCSharpMethod;
                    data.SetOpValue(this);
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
                    datacall.SetOpValue(irmethodcall);
                    datacall.index = 0;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
                else
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallDynamic;
                    datacall.SetOpValue(irmethodcall);
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
                    datacall.SetOpValue(irmethodcall);
                    datacall.index = paramCount + 1;
                    datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
                else
                {
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.SetOpValue(irmethodcall);
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
        public void InvokeCSharp( RuntimeVM rvm )
        {
            if (m_MethodInfo == null)
            {
                Debug.Write("error 执行时发现系统空函数");
                return;
            }

            Object[] paramsObj = new Object[paramCount];
            ParameterInfo[] pis = m_MethodInfo.GetParameters();
            for (int i = 0; i < pis.Length; i++)
            {
                paramsObj[i] = GetObjectByValue(rvm.m_ValueStack[rvm.m_ValueIndex - paramsObj.Length + i], pis[i].ParameterType );
            }
            rvm.m_ValueIndex -= (ushort)(paramsObj.Length - 1);

            var retobj = m_MethodInfo.Invoke(target, paramsObj );
            if (retobj != null)
            {
                CreateSObjectByCSharpObject( ref rvm.m_ValueStack[rvm.m_ValueIndex++], retobj );
            }
        }
        public System.Object GetObjectByValue( SValue sval, Type type )
        {
           
            if( type == typeof(bool) )
            {
                return sval.int8Value == 1;
            }
            else if (type == typeof(Byte))
            {
                return sval.int8Value;
            }
            else if (type == typeof(SByte))
            {
                return sval.sint8Value;
            }
            else if (type == typeof(Int16))
            {
                return sval.int16Value;
            }
            else if (type == typeof(UInt16))
            {
                return sval.uint16Value;
            }
            else if (type == typeof(Int32))
            {
                return sval.int32Value;
            }
            else if (type == typeof(UInt32))
            {
                return sval.uint32Value;
            }
            else if (type == typeof(Int64))
            {
                return sval.int64Value;
            }
            else if (type == typeof(UInt64))
            {
                return sval.uint64Value;
            }
            else if (type == typeof(Single))
            {
                return sval.floatValue;
            }
            else if (type == typeof(Double))
            {
                return sval.doubleValue;
            }
            else if (type == typeof(String))
            {
                return sval.stringValue;
            }

            switch (sval.eType)
                {
                    //case EVMType.RawBoolean:
                    //    {
                    //        return sval.int8Value == 1;
                    //        //return new BoolObject(sval.int8Value == 1);
                    //    }
                    //case EVMType.RawByte:
                    //    {
                    //        return sval.int8Value;
                    //        //return new Int8Object(sval.int8Value);
                    //    }
                    //case EVMType.RawSByte:
                    //    {
                    //        return sval.sint8Value;
                    //        //return new SInt8Object(sval.sint8Value);
                    //    }
                    ////case EVMType.Char:
                    ////    {
                    ////        return charValue;
                    ////    }
                    //case EVMType.RawInt16:
                    //    {
                    //        return sval.int16Value;
                    //        //return new Int16Object(sval.int16Value);
                    //    }
                    //case EVMType.RawUInt16:
                    //    {
                    //        //return new UInt16Object(sval.uint16Value);
                    //        return sval.uint16Value;
                    //    }
                    //case EVMType.RawInt32:
                    //    {
                    //        //return new Int32Object(sval.int32Value);
                    //        return sval.int32Value;
                    //    }
                    //case EVMType.RawUInt32:
                    //    {
                    //        //return new UInt32Object(sval.uint32Value);
                    //        return sval.uint32Value;
                    //    }
                    //case EVMType.RawInt64:
                    //    {
                    //        //return new Int64Object(sval.int64Value);
                    //        return sval.int64Value;
                    //    }
                    //case EVMType.RawUInt64:
                    //    {
                    //        //return new UInt64Object(sval.uint64Value);
                    //        return sval.uint64Value;
                    //    }
                    //case EVMType.RawFloat32:
                    //    {
                    //        //return new Float32Object(sval.floatValue);
                    //        return sval.floatValue;
                    //    }
                    //case EVMType.RawFloat64:
                    //    {
                    //        //return new Float64Object(sval.doubleValue);
                    //        return sval.doubleValue;
                    //    }
                    //case EVMType.RawString:
                    //    {
                    //        //return new StringObject(sval.stringValue);
                    //        return sval.stringValue;
                    //    }
                    case EVMType.Boolean:
                        {
                            return new BoolObject(sval.int8Value == 1);
                        }
                    case EVMType.Byte:
                        {
                            return new Int8Object(sval.int8Value);
                        }
                    case EVMType.SByte:
                        {
                            return new SInt8Object(sval.sint8Value);
                        }
                    //case EVMType.Char:
                    //    {
                    //        return charValue;
                    //    }
                    case EVMType.Int16:
                        {
                            return new Int16Object(sval.int16Value);
                        }
                    case EVMType.UInt16:
                        {
                            return new UInt16Object(sval.uint16Value);
                        }
                    case EVMType.Int32:
                        {
                            return new Int32Object(sval.int32Value);
                        }
                    case EVMType.UInt32:
                        {
                            return new UInt32Object(sval.uint32Value);
                        }
                    case EVMType.Int64:
                        {
                            return new Int64Object(sval.int64Value);
                        }
                    case EVMType.UInt64:
                        {
                            return new UInt64Object(sval.uint64Value);
                        }
                    case EVMType.Float32:
                        {
                            return new Float32Object(sval.floatValue);
                        }
                    case EVMType.Float64:
                        {
                            return new Float64Object(sval.doubleValue);
                        }
                    case EVMType.String:
                        {
                            return new StringObject(sval.stringValue);
                        }
                case EVMType.Array:
                    {
                        return sval.sobject as ArrayObject;
                    }
                }
            return sval.sobject;
        }
        public void CreateSObjectByCSharpObject( ref SValue sva, System.Object obj)
        {
            switch (obj)
            {
                case bool boo:
                    {
                        sva.SetBoolValue((bool)obj ==true );
                    }
                    break;
                case Byte b:
                    {
                        sva.SetInt8Value((Byte)obj);
                    }
                    break;
                case SByte sb:
                    {
                        sva.SetSInt8Value((SByte)obj);
                    }
                    break;
                case Char ch:
                    {
                        sva.SetStringValue((String)obj);
                    }
                    break;
                case Int16 int16:
                    {
                        sva.SetInt16Value((Int16)obj);
                    }
                    break;
                case UInt16 int16:
                    {
                        sva.SetUInt16Value((UInt16)obj);
                    }
                    break;
                case Int32 int32:
                    {
                        sva.SetInt32Value((Int32)obj);
                    }
                    break;
                case UInt32 int32:
                    {
                        sva.SetUInt32Value((uint)obj);
                    }
                    break;
                case Int64 int64:
                    {
                        sva.SetInt64Value((long)obj);
                    }
                    break;
                case UInt64 uint64:
                    {
                        sva.SetUInt64Value((ulong)obj);
                    }
                    break;
                case Single f:
                    {
                        sva.SetFloatValue((Single)obj);
                    }
                    break;
                case Double d:
                    {
                        sva.SetDoubleValue((double)obj);
                    }
                    break;
                case String str:
                    {
                        sva.SetStringValue((String)obj);
                    }
                    break;
                default:
                    {
                        sva.SetSObject(obj as SObject);
                    }
                    break;
            }
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
}
