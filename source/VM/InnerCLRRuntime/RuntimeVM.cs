//****************************************************************************
//  File:      RuntimeMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: master use .net clr system. new create method instance than running code virtual machine 
//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.VM.Runtime
{
    //前置类型
    public enum EVMType : Byte
    {
        None,
        Null,
        Void,
        Object,
        Class,
        Enum,
        Data,
        Array,

        RawBoolean,
        RawByte,
        RawSByte,
        RawInt16,
        RawUInt16,
        RawInt32,
        RawUInt32,
        RawFloat16,
        RawFloat32,
        RawInt64,
        RawUInt64,
        RawFloat64,
        RawInt128,
        RawUInt128,
        RawString,

        Boolean,
        Byte,
        SByte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float16,
        Float32,
        Int64,
        UInt64,
        Float64,
        Int128,
        UInt128,
        String,
    }
    public class RuntimeVM
    {
        public string id { get; set; } = "";
        public int level { get; set; } = 0;
        public bool isPersistent { get; set; } = false;
        public SObject[] returnObjectArray => m_ReturnObjectArray;

        public SValue[] m_ValueStack = null;
        public ushort m_ValueIndex = 0;

        private List<RuntimeType> m_InputTemplateRuntimeTypeList = new List<RuntimeType>();
        private SObject[] m_LocalVariableObjectArray = null;
        private SObject[] m_ArgumentObjectArray = null;
        private SObject[] m_ReturnObjectArray = null;


        private IRMethod m_IRMethod = null;
        private IRData[] m_IRDataList = null;
        private ushort m_ExecuteIndex = 0;
        private ushort m_ExecuteCount = 0;
        private IRMetaClass m_IRMetaClass = null;
        //private Stack<List<RuntimeType>> m_NewObjectRuntimeTypeStack = new Stack<List<RuntimeType>>();
        public RuntimeVM( List<RuntimeType> inputTemplateTypeList, List<IRData> irlist )
        {
            if (inputTemplateTypeList != null)
            {
                m_InputTemplateRuntimeTypeList = inputTemplateTypeList;
            }
            m_IRMethod = null;
            id = "create_new_splite";
            m_IRDataList = irlist.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            Init();
        }
        public RuntimeVM( List<RuntimeType> inputTemplateTypeList, IRMethod mmf )
        {
            if(inputTemplateTypeList != null )
            {
                m_InputTemplateRuntimeTypeList = inputTemplateTypeList;
            }
            m_IRMethod = mmf;
            m_IRDataList = mmf.IRDataList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;

            id = mmf.id;

            Init();
        }
        public RuntimeVM( List<IRData> irList )
        {
            m_IRDataList = irList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            Init();
        }
        void Init()
        {
            //参数列表 argument variable table
            if(m_IRMethod != null )
            {
                m_ReturnObjectArray = new SObject[m_IRMethod.methodReturnVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodReturnVariableList.Count; i++)
                {
                    IRMetaType imt = m_IRMethod.methodReturnVariableList[i].irMetaType;
                    SObject sobj = CreateObjectByIRMetaType(imt, imt.irOwnerMetaClass, true);
                    m_ReturnObjectArray[i] = sobj;
                }

                m_ArgumentObjectArray = new SObject[m_IRMethod.methodArgumentList.Count];
                for (int i = 0; i < m_IRMethod.methodArgumentList.Count; i++)
                {
                    IRMetaType imt = m_IRMethod.methodArgumentList[i].irMetaType;
                    SObject sobj = CreateObjectByIRMetaType(imt, imt.irOwnerMetaClass, true);
                    m_ArgumentObjectArray[i] = sobj;
                }
                for( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
                {
                    Log.AddVM(EError.None, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentObjectArray[i].ToString() + "]" );
                }

                //局部变量列表 local variable table
                m_LocalVariableObjectArray = new SObject[m_IRMethod.methodLocalVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodLocalVariableList.Count; i++)
                {
                    var mev = m_IRMethod.methodLocalVariableList[i];
                    IRMetaType imt = mev.irMetaType;
                    SObject sobj = CreateObjectByIRMetaType(imt, m_IRMethod.irOwnerMetaClass, true);
                    m_LocalVariableObjectArray[i] = sobj;
                }
                for (int i = 0; i < m_LocalVariableObjectArray.Length; i++)
                {
                    Log.AddVM(EError.None, "Variable_" + i.ToString() + m_LocalVariableObjectArray[i].ToString());
                }
            }
            else
            {
                m_ReturnObjectArray = new SObject[0];
                m_ArgumentObjectArray = new SObject[0];
                m_LocalVariableObjectArray = new SObject[0];
            }
            var count = m_IRDataList.Length;
            if (count < 48)
            {
                m_ValueStack = new SValue[128];
            }
            else if (count >= 48 && count < 150)
            {
                m_ValueStack = new SValue[160];
            }
            else if (count >= 150 && count < 300)
            {
                m_ValueStack = new SValue[200];
            }
            else if (count >= 300 && count < 500)
            {
                m_ValueStack = new SValue[300];
            }
            else if (count >= 500 && count < 800)
            {
                m_ValueStack = new SValue[400];
            }
            else
            {
                m_ValueStack = new SValue[500];
            }            
        }
        SObject CreateObjectByIRMetaType(IRMetaType irmt, IRMetaClass curIrMc, bool isAdd = false )
        {
            if( irmt.templateIndex != -1 )
            {
                return new TemplateObject();
            }
            else
            {
                var rt = GetClassRuntimeType(irmt, curIrMc, m_InputTemplateRuntimeTypeList, isAdd);
                return ObjectManager.CreateObjectByRuntimeType(rt);
            }
        }
        public void AddReturnObjectArray( SObject[] sobjs )
        {
            for( int i = 0; i < sobjs.Length; i++ )
            {
                if( sobjs[i].runtimeType != RuntimeTypeManager.voidRuntimeType )
                {                    
                    //GetObjectByValue(4, i, sobjs, ref m_ValueStack[m_ValueIndex++] );
                    var obj = sobjs[i];
                    Debug.Assert(obj != null);
                    if (obj.isNull)
                    {
                        m_ValueStack[m_ValueIndex++].SetNull();
                        return;
                    }
                    SetSValue(obj, obj.eType, ref m_ValueStack[m_ValueIndex] );
                    m_ValueIndex++;
                }
            }
        }
        public void GetArgumentValue( int index, ref SValue svalue )
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, $"SVM Error FunctionName:{this.id} 执行的参数超出范围!!");
                return;
            }
            GetObjectByValue( 0, index, ref svalue);
        }
        public void SetArgumentValue( int index, SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            SetObjectByValue( 0, index, ref svalue);
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            SetObjectByValue(1, index, ref svalue);
        }
        public void GetLocalVariableSValue(int index, ref SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            GetObjectByValue(1, index, ref svalue);
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (index > m_ReturnObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            SetObjectByValue(2, index, ref svalue);
        }
        public SValue GetCurrentIndexValue( int index  )
        {
            return m_ValueStack[index];
        }
        public static RuntimeType GetClassRuntimeType(IRMetaType irmt, IRMetaClass curIRMc, List<RuntimeType> __rtList, bool isAdd = false )
        {
            if (irmt.templateIndex != -1)
            {
                if (irmt.irOwnerMetaClass == curIRMc || curIRMc.irName == "Object" )
                {
                    return __rtList[irmt.templateIndex];
                }
                else
                {
                    var mt = curIRMc.GetIRMetaTypeByTemplateAndClassRelation(irmt.irOwnerMetaClass, irmt.templateIndex);

                    return GetClassRuntimeType(mt, curIRMc, __rtList, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.irMetaTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], curIRMc, __rtList, isAdd);
                        rtList.Add(crt);
                    }
                }
                RuntimeType rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
                if( rt == null && isAdd )
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(irmt.irMetaClass, rtList);
                }
                return rt;
            }
        }
        public RuntimeType GetMethodRuntimeType(IRMetaType irmt)
        {
            if (irmt.templateIndex != -1)
            {
                return m_InputTemplateRuntimeTypeList[irmt.templateIndex];
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.irMetaTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                    {
                        var crt = GetMethodRuntimeType(irmt.irMetaTypeList[i]);
                        rtList.Add(crt);
                    }
                }
                return RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
            }
        }
        public void SetNewObject()
        {
            SValue sval = InnerCLRRuntimeVM.topCLRRuntime.GetCurrentIndexValue(InnerCLRRuntimeVM.topCLRRuntime.m_ValueIndex - 1);
            m_ValueStack[m_ValueIndex++] = sval;
            if( sval.eType == EVMType.Class )
            {
                m_IRMetaClass = (sval.sobject as ClassObject).irMetaClass;
            }
        }
        public void ClearNewObject()
        {
            m_IRMetaClass = null;
        }
        public void Run(bool disStackCount)
        {
            string funName = id;

            string pushChar = "";
            for( int i = 0; i < level; i++ )
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Push] Method: [" + funName +"]" );
            level++;

            var topClrRuntime = InnerCLRRuntimeVM.topCLRRuntime;
            for ( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
            {
                SValue sval;
                if ( disStackCount )
                {
                    topClrRuntime.m_ValueIndex--;
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex);
                }
                else
                {
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex - 1 - i);
                }
                SetArgumentValue(m_ArgumentObjectArray.Length - i - 1, sval);
            }

            while (true)
            {
                if (m_ExecuteIndex >= m_ExecuteCount)
                {                    
                    break;
                }
                RunInstruction(m_IRDataList[m_ExecuteIndex]);
                m_ExecuteIndex++;
            }
            level--;
            pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM( EError.None, pushChar  + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        public static void SetValue( ref SValue sValue, ref SValue sStore, IRData iri )
        {
            switch (sStore.eType)
            {
                case EVMType.Boolean:
                case EVMType.Byte: sStore.SetInt8Value(sValue.int8Value); break;
                case EVMType.SByte: sStore.SetSInt8Value(sValue.sint8Value); break;
                case EVMType.Int16: sStore.SetInt16Value(sValue.int16Value); break;
                case EVMType.UInt16: sStore.SetUInt16Value(sValue.uint16Value); break;
                case EVMType.Int32: sStore.SetInt32Value(sValue.int32Value); break;
                case EVMType.UInt32: sStore.SetUInt32Value(sValue.uint32Value); break;
                case EVMType.Int64: sStore.SetInt64Value(sValue.int64Value); break;
                case EVMType.UInt64: sStore.SetUInt64Value(sValue.uint64Value); break;
                case EVMType.Float32: sStore.SetFloatValue(sValue.floatValue); break;
                case EVMType.Float64: sStore.SetDoubleValue(sValue.doubleValue); break;
                case EVMType.String: sStore.SetStringValue(sValue.stringValue); break;
                case EVMType.Null:
                    {
                        sStore.SetNull();
                    }
                    break;
                case EVMType.Class:
                    {
                        (sStore.sobject as ClassObject).SetMemberVariableSValue(iri.index, sValue);
                    }
                    break;
                case EVMType.Array:
                    {
                        (sStore.sobject as ArrayObject).SetMemberVariableSValue(iri.index, sValue);
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, "Error StoreNotStaticField Path:" + iri.debugInfo.path + " Line: " + iri.debugInfo.beginLine);
                    }
                    break;
            }
        }
        public void RunInstruction( IRData iri )
        {
            //栈位的移动的规则，使用当前位为空的概念，只要栈被使用掉，索引则加1，所以索引最少为0
            switch ( iri.opCode )
            {
                case EIROpCode.Nop:
                    break;
                case EIROpCode.LoadConstNull:
                    {
                        m_ValueStack[m_ValueIndex++].SetNullValueType();
                    }
                    break; 
                case EIROpCode.LoadConstByte:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt8Value((Byte)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstSByte:
                    {
                        m_ValueStack[m_ValueIndex++].SetSInt8Value((SByte)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        m_ValueStack[m_ValueIndex++].SetBoolValue((bool)iri.opValue);
                    }
                    break;
                //case EIROpCode.LoadConstChar:
                //    {
                //        m_ValueStack[m_ValueIndex++].SetCharValue((Char)iri.opValue);
                //    }
                //    break;
                case EIROpCode.LoadConstInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt16Value((Int16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt16Value((UInt16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt32Value((Int32)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt32Value((UInt32)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt64Value((Int64)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt64Value((UInt64)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstFloat:
                    {
                        m_ValueStack[m_ValueIndex++].SetFloatValue((Single)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstDouble:
                    {
                        m_ValueStack[m_ValueIndex++].SetDoubleValue((Double)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        m_ValueStack[m_ValueIndex++].SetStringValue( (String)iri.opValue );
                    }
                    break;
                case EIROpCode.Convert_I8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Byte);
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.SByte);
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int16);
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt16);
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int32);
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt32);
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int64);
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt64);
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        m_ValueStack[m_ValueIndex-1].ConvertByEType(EVMType.Float32);
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float64);
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        GetArgumentValue(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        GetLocalVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.StoreLocal:
                    {
                        SetLocalVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                    }
                    break;
                case EIROpCode.StoreReturn:
                    {
                        SetReturnVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                        m_ExecuteIndex = m_ExecuteCount;
                    }
                    break; 
                case EIROpCode.LoadNotStaticField:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];

                        if (v.eType == EVMType.Class || v.eType == EVMType.Array )
                        {
                            var co = (v.sobject as ClassObject);
                            co.GetMemberVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
                        }
                        //else if( v.eType == EVMType.Int32 
                        //    )
                        //{

                        //}
                        else
                        {
                            //Debug.Assert(false, "还未确定其它类型可以拿值 ，如果拿成员变量，应该也是固定的几个变量! 比如value一类的");
                        }
                        //栈位不变，因为当前对象位的被通过索引取出来的成员变量值，覆盖掉， 所以栈位不会发生变化
                    }
                    break;
                case EIROpCode.StoreNotStaticField2:
                    {
                        // -2在存储的值 -1表示要存储的对象 存储完成，直接变成位置0
                        SValue sStore = m_ValueStack[m_ValueIndex - 2];
                        SValue sValue = m_ValueStack[m_ValueIndex - 1];
                        SetValue(ref sValue, ref sStore, iri);
                        m_ValueIndex -= 2;
                    }
                    break;
                case EIROpCode.StoreNotStaticField1:
                    {
                        // -2在存储的值 -1表示要存储的对象 存储完成，直接变成位置0
                        SValue sStore = m_ValueStack[m_ValueIndex - 2];
                        SValue sValue = m_ValueStack[m_ValueIndex - 1];
                        SetValue(ref sValue, ref sStore, iri);
                        m_ValueIndex -= 1;
                    }
                    break;
                case EIROpCode.LocalGlobal:
                    {
                        InnerCLRRuntimeVM.LoadGlobalVariable(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.LoadStaticField:
                    {
                        var irmt = iri.opValue as IRMetaType;

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                        {
                            var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], irmt.irMetaTypeList[i].irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                            classRTList.Add(crt);
                        }
                        RuntimeType rt = GetClassRuntimeType(irmt, irmt.irOwnerMetaClass, classRTList, true);

                        rt.GetMemberVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.StoreStaticField:
                    {
                        var irmt = iri.opValue as IRMetaType;

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                        {
                            var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], irmt.irMetaTypeList[i].irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                            classRTList.Add(crt);
                        }

                        RuntimeType rt = GetClassRuntimeType(irmt, irmt.irOwnerMetaClass, classRTList, true);
                        rt.SetMemberVariableSValue(iri.index, m_ValueStack[--m_ValueIndex] );
                    }
                    break;
                case EIROpCode.StoreGlobal:
                    {
                        var sval = m_ValueStack[--m_ValueIndex];
                        InnerCLRRuntimeVM.StoreGlobalVariable( iri.index, ref sval );
                    }
                    break;
                case EIROpCode.LoadArrayIndex:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];
                        if (v.eType == EVMType.Array)
                        {
                            (v.sobject as ArrayObject).LoadValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
                        }
                        else
                        {
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndex:
                    {
                        int int1 = 1, int2 = 2;
                        if( iri.opValue is Boolean flag )
                        {
                            if( flag )
                            {
                                int1 = 2;
                                int2 = 1;
                            }
                        }
                        SValue sStore = m_ValueStack[m_ValueIndex - int1];
                        SValue sValue = m_ValueStack[m_ValueIndex - int2];

                        if (sStore.eType == EVMType.Array)
                        {
                            (sStore.sobject as ArrayObject).StoreValue(iri.index, sValue);
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 2;
                    }
                    break;
                case EIROpCode.LoadArrayIndexField:
                    {
                        SValue arrayref = m_ValueStack[m_ValueIndex - 2];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EVMType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            (arrayref.sobject as ArrayObject).LoadValue(index, ref m_ValueStack[m_ValueIndex - 2]);
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 1;
                    }
                    break;
                case EIROpCode.StoreArrayIndexField:
                    {
                        SValue arrayref = m_ValueStack[m_ValueIndex - 3];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 2];
                        SValue storevalue = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EVMType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            (arrayref.sobject as ArrayObject).StoreValue(index, storevalue );
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 3;
                    }
                    break;
                case EIROpCode.CallStatic:
                    {
                        var mfc = iri.opValue as IRMethodCall;

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < mfc.metaType.irMetaTypeList.Count; i++)
                        {
                            var crt = GetClassRuntimeType(mfc.metaType.irMetaTypeList[i], mfc.metaType.irMetaTypeList[i].irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                            classRTList.Add(crt);
                        }
                        var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(mfc.metaType.irMetaClass, classRTList);
                        if (rt == null )
                        {
                            rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(mfc.metaType.irMetaClass, classRTList);
                        }
                        
                        if( mfc.irMethod.id == "type" )
                        {
                            var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                            m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                        }
                        else
                        {
                            for (int i = 0; i < mfc.irTemplateMetaType.Count; i++)
                            {
                                var crt = GetMethodRuntimeType(mfc.irTemplateMetaType[i]);
                                classRTList.Add(crt);
                            }
                            InnerCLRRuntimeVM.RunIRMethod(classRTList, mfc.irMethod);
                        }
                    }
                    break;
                case EIROpCode.CallDynamic:
                    {
                        var mfc = iri.opValue as IRMethodCall;

                        RuntimeType rt = null;
                        IRMetaClass irc = null;
                        if (iri.index > -1)
                        {
                            int stackIndex = m_ValueIndex - iri.index;
                            if (stackIndex < 0)
                            {
                                Log.AddVM(EError.None, "StackIndex 是负数!");
                                return;
                            }
                            var v = m_ValueStack[stackIndex];
                            if (v.eType == EVMType.Class
                                || v.eType == EVMType.Array )
                            {
                                var co = (v.sobject as ClassObject);
                                irc = co.irMetaClass;
                                rt = v.sobject.runtimeType;
                            }
                            else
                            {
                                irc = IRManager.instance.GetIRMetaClassByName(v.eType.ToString());
                                rt = RuntimeTypeManager.GetRuntimeTypeByMT(irc);
                            }
                            if (irc == null)
                            {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                                return;
                            }
                            if (mfc.irMethod == null)
                            {
                                Debug.Assert(false, "没有找到合适的调用方式");
                                return;
                            }
                            if (mfc.irMethod.id == "type")
                            {
                                var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                                m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                            }
                            else
                            {
                                List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                                for (int i = 0; i < mfc.irTemplateMetaType.Count; i++)
                                {
                                    var crt = GetClassRuntimeType(mfc.irTemplateMetaType[i], irc, rt.runtimeTemplateList, true);
                                    rtList.Add(crt);
                                }
                                if( mfc.irMethod.interfaceMethod )
                                {
                                    var irmethod = irc.GetIRNonStaticMethodIndexByName(mfc.methodName, out int index);
                                    if (irmethod != null)
                                    {
                                        InnerCLRRuntimeVM.RunIRMethod(rtList, irmethod);
                                    }
                                    else
                                    {
                                        Debug.Assert(false, "没有找到合适的调用方式");
                                    }
                                }
                                else
                                {
                                    InnerCLRRuntimeVM.RunIRMethod(rtList, mfc.irMethod);
                                }
                            }
                        }
                        else
                        {
                            Log.AddVM(EError.None, "调用栈上动态函数");
                        }
                    }
                    break;
                case EIROpCode.CallVirt:
                    {
                        var mfc = iri.opValue as IRMethodCall;

                        int stackFrontIndex = (int)mfc.paramCount + 1;
                        int stackIndex = m_ValueIndex - stackFrontIndex;
                        if( stackIndex < 0 )
                        {
                            Log.AddVM(EError.None, "StackIndex 是负数!");
                            return;
                        }
                        var v = m_ValueStack[stackIndex];

                        if( v.isNull )
                        {
                            Debug.Assert( false, "当前值为空!!" );
                            return;
                        }

                        RuntimeType rt = null;
                        IRMetaClass irc = null;
                        if (v.eType == EVMType.Class || v.eType == EVMType.Array )
                        {
                            var co = (v.sobject as ClassObject);
                            irc = co.irMetaClass;
                            rt = co.runtimeType;
                        }
                        else if( v.eType == EVMType.Object )
                        {
                            SObject co = (v.sobject) as SObject;
                            m_ValueStack[stackIndex].SetValue(co);
                            var nco = m_ValueStack[stackIndex].GetSObject();
                            Debug.Assert(nco != null);
                            irc = nco.irMetaClass;
                            rt = nco.runtimeType;
                        }
                        //else if( v.eType == EVMType.Array )
                        //{
                        //    irc = IRManager.instance.GetIRMetaClassByName("Array");
                        //    rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irc);
                        //}
                        else
                        {
                            irc = IRManager.instance.GetIRMetaClassByName(v.eType.ToString());
                            rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irc);
                        }
                        if( irc == null )
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            return;
                        }
                        IRMethod cfc = irc.GetIRNonStaticMethodByIndex(iri.index);


                        if (cfc == null)
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            Debug.Assert(false, "没有找到索引是" + iri.index  + "的函数!");
                            return;
                        }
                        List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                        for ( int i = 0; i < mfc.irTemplateMetaType.Count; i++ )
                        {
                            var crt = GetClassRuntimeType(mfc.irTemplateMetaType[i], irc, rt.runtimeTemplateList, true );
                            rtList.Add(crt);
                        }
                        InnerCLRRuntimeVM.RunIRMethod( rtList, cfc);

                        var a = ObjectManager.classObjectDict;
                    }
                    break;
                case EIROpCode.CallCSharpMethod:
                    {
                        var mfc = iri.opValue as IRCallFunction;                       
                        mfc.InvokeCSharp( this );
                    }
                    break;
                case EIROpCode.NewObject:
                    {
                        //前期先和newtemplateclass一样处理，等以后确定后，要精简单这个，使用无模板方法，省去查找的过程，直接
                        //创建已注册的runtimeType 当前runtimeType在生成类的时候，就已经注册过来了,加快了查找方法
                        IRMetaClass mdt = iri.opValue as IRMetaClass;
                        var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(mdt);
                        SObject sob = ObjectManager.CreateObjectByRuntimeType(rt, true);
                        if ( sob is ClassObject co )
                        {
                            ObjectManager.AddClassObject(co);
                        }
                        m_ValueStack[m_ValueIndex++].SetSObject(sob);

                        var irList = rt.irClass.CreateStaticMetaMetaVariableIRList();
                        if (irList.Count > 0)
                        {
                            InnerCLRRuntimeVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
                        }
                    }
                    break;
                case EIROpCode.NewTemplateObject:
                    {
                        IRMetaType mdt = iri.opValue as IRMetaType;

                        var rt = GetClassRuntimeType(mdt, m_IRMetaClass != null ? m_IRMetaClass : mdt.irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                        SObject sobj = ObjectManager.CreateObjectByRuntimeType( rt, true );
                        if (sobj is ClassObject co)
                        {
                            ObjectManager.AddClassObject(co);
                        }
                        m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                        var irc = rt.irClass;


                        var irList = rt.irClass.CreateStaticMetaMetaVariableIRList();
                        if( irList.Count > 0 )
                        {
                            InnerCLRRuntimeVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
                        }
                    }
                    break;
                case EIROpCode.NewArray:
                    {
                        var sval = m_ValueStack[m_ValueIndex - 1];
                        if( sval.eType != EVMType.Int32)
                        {
                            Log.AddVM(EError.None, "创建数组长度不是Int32类型!!");
                            break;
                        }

                        IRMetaType mdt = iri.opValue as IRMetaType;
                        var rt = GetClassRuntimeType(mdt, m_IRMetaClass != null ? m_IRMetaClass : mdt.irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                        ArrayObject sob = new ArrayObject(rt, sval.int32Value );
                        ObjectManager.AddClassObject(sob);
                        m_ValueStack[m_ValueIndex-1].SetSObject(sob);

                        //var irList = rt.irClass.CreateStaticMetaMetaVariableIRList();
                        //if (irList.Count > 0)
                        //{
                        //    InnerCLRRuntimeVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
                        //}
                    }
                    break;
                case EIROpCode.Dup:
                    {
                        if(iri.opValue == null )
                        {
                            var sval = m_ValueStack[m_ValueIndex - 1];
                            m_ValueStack[m_ValueIndex++] = sval;
                        }
                        else
                        {
                            int count = (int)iri.opValue;
                            int curVIndex = m_ValueIndex;
                            for( int i = count-1; i >= 0; i-- )
                            {
                                var sval = m_ValueStack[curVIndex - i - 1];
                                m_ValueStack[m_ValueIndex++] = sval;
                            }
                        }
                    }
                    break;
                case EIROpCode.Pop:
                    {
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Label:
                    {
                    }
                    break;
                case EIROpCode.Br:
                case EIROpCode.BrLabel:
                    {
                        m_ExecuteIndex = (ushort)iri.index;
                    }
                    break;
                case EIROpCode.BrFalse:
                    {
                        var v = m_ValueStack[--m_ValueIndex];
                        if (v.eType == EVMType.Boolean)
                        {
                            if (v.int8Value == 0)
                            {
                                m_ExecuteIndex = (ushort)iri.index;
                            }
                        }
                        //else if( v.eType == EVMType.RawBoolean )
                        //{
                        //    if (v.int8Value == 0)
                        //    {
                        //        m_ExecuteIndex = (ushort)iri.index;
                        //    }
                        //}
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        var v = m_ValueStack[--m_ValueIndex];
                        if (v.eType == EVMType.Boolean)
                           // || v.eType == EVMType.RawBoolean 
                        {
                            if (v.int8Value == 1)
                            {
                                m_ExecuteIndex = (ushort)iri.index;
                            }
                        }
                    }
                    break;
                case EIROpCode.Add:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 加法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].AddSValue(ref m_ValueStack[m_ValueIndex - 1], false, out bool isMethod);
                        if (isMethod)
                        {
                            m_ValueStack[m_ValueIndex - 3] = m_ValueStack[m_ValueIndex - 1];
                            m_ValueIndex -= 2;
                        }
                        else
                        {
                            m_ValueIndex--;
                        }
                    }
                    break;
                case EIROpCode.Minus:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 减法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-2].ComputeSVAlue(1, ref m_ValueStack[m_ValueIndex - 1], false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Multiply:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 乘法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(2, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Divide:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 除法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].DivSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(3, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Modulo:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 余法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].ModuloSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(4, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Combine:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 合并运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].CombineSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(5, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.InclusiveOr:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 包括运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].InclusiveOrSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(6, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.XOR:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 或运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].XORSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(7, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shr:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 右移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShrSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(8, ref m_ValueStack[m_ValueIndex], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shi:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 左移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShiSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(9, ref m_ValueStack[m_ValueIndex], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Not运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-1].NotSValue();
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Neg运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-1].NegSValue(false);
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareSValue1AndValue2( ref m_ValueStack[m_ValueIndex - 2],ref  m_ValueStack[m_ValueIndex - 1], 4 );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Or:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], 6 );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Ceq:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareEuqalSValue1AndValue2( ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], true, out bool isMethod );
                        if (isMethod)
                        {
                            m_ValueStack[m_ValueIndex - 3] = m_ValueStack[m_ValueIndex - 1];
                            m_ValueIndex -= 2;
                        }
                        else
                        {
                            m_ValueIndex--;
                        }
                    }
                    break;
                case EIROpCode.Cne:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareEuqalSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], false, out bool isMethod);
                        if (isMethod)
                        {
                            m_ValueStack[m_ValueIndex - 3] = m_ValueStack[m_ValueIndex - 1];
                            m_ValueIndex -= 2;
                        }
                        else
                        {
                            m_ValueIndex--;
                        }
                    }
                    break;
                case EIROpCode.Cgt:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }

                        SValue.CompareSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], 0 );
                        m_ValueIndex--;

                        /*
                        SValue.CompareEuqalSValue1AndValue2( ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], false, out bool isMethod);
                        if (isMethod)
                        {
                            m_ValueStack[m_ValueIndex - 3] = m_ValueStack[m_ValueIndex - 1];
                            m_ValueIndex -= 2;
                        }
                        else
                        {
                            m_ValueIndex--;
                        }
                        */
                    }
                    break;
                case EIROpCode.Cge:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], 1 );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Clt:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], 2 );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cle:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        SValue.CompareSValue1AndValue2(ref m_ValueStack[m_ValueIndex - 2], ref m_ValueStack[m_ValueIndex - 1], 3 );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.CastClass:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        IRMetaType mdt = iri.opValue as IRMetaType;
                        var rt = GetClassRuntimeType(mdt, m_IRMetaClass != null ? m_IRMetaClass : mdt.irOwnerMetaClass, m_InputTemplateRuntimeTypeList, true);
                        if( rt.eType == EVMType.Object )
                        {
                            break;
                        }

                        var v1 = m_ValueStack[m_ValueIndex-1];

                        if( v1.isNull )
                        {
                            m_ValueStack[m_ValueIndex - 1].SetNull();
                        }
                        else
                        {
                            if (v1.eType == EVMType.Class)
                            {
                                if (!v1.sobject.runtimeType.IsExtendsRelation(rt))
                                {
                                    m_ValueStack[m_ValueIndex - 1].SetNull();
                                }
                            }
                            else if( v1.eType == EVMType.Array )
                            {
                                if( rt.eType == EVMType.Array || rt.eType == EVMType.Class )
                                {

                                }
                                else if( rt.eType == EVMType.Object )
                                {
                                }
                                else
                                {
                                    m_ValueStack[m_ValueIndex - 1].SetNull();
                                }
                            }
                            else
                            {
                                if (v1.eType != rt.eType)
                                {
                                    m_ValueStack[m_ValueIndex - 1].SetNull();
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, "Error 暂不支持" + iri.opCode.ToString() + "的处理!!");
                    }
                    break;
            }
        }

        public void SetObjectByValue(int type, int index, ref SValue svalue)
        {
            SObject obj = null;
            if (type == 0)
            {
                obj = m_ArgumentObjectArray[index];
            }
            else if (type == 1)
            {
                obj = m_LocalVariableObjectArray[index];
            }
            else if( type == 2)
            {
                obj = m_ReturnObjectArray[index];
            }
            Debug.Assert(obj != null);
            if (svalue.isNull)
            {
                obj.SetNull();
                return;
            }
            bool anyObj = obj.eType == EVMType.Object;
            switch (svalue.eType)
            {
                case EVMType.Null:
                    {
                        obj.SetNull();
                    }
                    break;
                //case EVMType.RawBoolean:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Boolean, svalue.int8Value);
                //        }
                //        BoolObject boolObj = obj as BoolObject;
                //        if (boolObj == null)
                //        {
                //            Debug.Write("该类型不是Boolean类型!!");
                //            return;
                //        }
                //        boolObj.SetValue(svalue.int8Value == 1);
                //    }
                //    break;
                case EVMType.Boolean:
                    {
                        if( anyObj )
                        {
                            obj.SetValueByType(  EVMType.Boolean, svalue.int8Value == 1 );
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Boolean, svalue.int8Value);
                        }
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value == 1);
                    }
                    break;
                //case EVMType.RawByte:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Byte, svalue.int8Value);
                //            return;
                //        }
                //        Int8Object byteObj = obj as Int8Object;
                //        if (byteObj == null)
                //        {
                //            Debug.Write("该类型不是Byte类型!!");
                //            return;
                //        }
                //        byteObj.SetValue(svalue.int8Value);
                //    }
                //    break;
                case EVMType.Byte:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Byte, svalue.int8Value );
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Byte, svalue.int8Value);
                            return;
                        }
                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                //case EVMType.RawSByte:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.SByte, svalue.sint8Value);
                //        }
                //        SInt8Object byteObj = obj as SInt8Object;
                //        if (byteObj == null)
                //        {
                //            Debug.Write("该类型不是SByte类型!!");
                //            return;
                //        }
                //        byteObj.SetValue(svalue.sint8Value);
                //    }
                //    break;
                case EVMType.SByte:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.SByte, svalue.sint8Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.SByte, svalue.sint8Value);
                        }
                        SInt8Object byteObj = obj as SInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                //case EVMType.RawInt16:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int16, svalue.int16Value);
                //        }
                //        Int16Object int16Obj = obj as Int16Object;
                //        if (int16Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int16Obj.SetValue(svalue.int16Value);
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int16, svalue.int16Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int16, svalue.int16Value);
                        }
                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int16Obj.SetValue(svalue.int16Value);
                    }
                    break;
                //case EVMType.RawUInt16:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt16, svalue.uint16Value);
                //        }
                //        UInt16Object uint16Obj = obj as UInt16Object;
                //        if (uint16Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint16Obj.SetValue(svalue.uint16Value);
                //    }
                //    break;
                case EVMType.UInt16:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt16, svalue.int16Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt16, svalue.uint16Value);
                        }
                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                //case EVMType.RawInt32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int32, svalue.int32Value);
                //            return;
                //        }
                //        Int32Object int32Obj = obj as Int32Object;
                //        if (int32Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int32Obj.SetValue(svalue.int32Value);
                //    }
                //    break;
                case EVMType.Int32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                //case EVMType.RawUInt32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt32, svalue.uint32Value);
                //        }
                //        UInt32Object uint32Obj = obj as UInt32Object;
                //        if (uint32Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint32Obj.SetValue(svalue.uint32Value);
                //    }
                //    break;
                case EVMType.UInt32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt32, svalue.uint32Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt32, svalue.uint32Value);
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                //case EVMType.RawInt64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int64, svalue.int64Value);
                //        }
                //        Int64Object int64Obj = obj as Int64Object;
                //        if (int64Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int64Obj.SetValue(svalue.int64Value);
                //    }
                //    break;
                case EVMType.Int64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int64, svalue.int64Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int64, svalue.int64Value);
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                //case EVMType.RawUInt64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt64, svalue.uint64Value);
                //        }
                //        UInt64Object uint64Obj = obj as UInt64Object;
                //        if (uint64Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint64Obj.SetValue(svalue.uint64Value);
                //    }
                //    break;
                case EVMType.UInt64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt64, svalue.uint64Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt64, svalue.uint64Value);
                        }
                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                //case EVMType.RawFloat32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Float32, svalue.floatValue);
                //        }
                //        Float32Object floatObj = obj as Float32Object;
                //        if (floatObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        floatObj.SetValue(svalue.floatValue);
                //    }
                //    break;
                case EVMType.Float32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Float32, svalue.floatValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Float32, svalue.floatValue);
                        }
                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                //case EVMType.RawFloat64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Float64, svalue.doubleValue);
                //        }
                //        Float64Object doubleObj = obj as Float64Object;
                //        if (doubleObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        doubleObj.SetValue(svalue.doubleValue);
                //    }
                //    break;
                case EVMType.Float64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Float64, svalue.doubleValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Float64, svalue.doubleValue);
                        }
                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                //case EVMType.RawString:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.String, svalue.stringValue);
                //            return;
                //        }
                //        StringObject stringObj = obj as StringObject;
                //        if (stringObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        stringObj.SetValue(svalue.stringValue);
                //    }
                //    break;
                case EVMType.String:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.String, svalue.stringValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.String, svalue.stringValue);
                            return;
                        }
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Class, svalue.sobject );
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        if (obj is ClassObject co)
                        {
                            var ao = svalue.sobject as ClassObject;
                            Debug.Assert(ao != null);
                            //co.SetClassObject(ao);                            
                            (obj as ClassObject).SetClassObject(ao);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        if (svalue.eType == EVMType.Object)
                        {
                            obj.SetValueByType(EVMType.Object, svalue.sobject);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Class:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}

                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Class, svalue.sobject);
                            return;
                        }
                        ClassObject classObj = obj as ClassObject;
                        if (classObj == null)
                        {
                            Debug.Assert(false);
                            Debug.Write("该类型不是Class类型!!");
                            return;
                        }
                        classObj.SetClassObject(svalue.sobject as ClassObject);
                        /*
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(svalue.int8Value == 1 ? true : false);
                            return;
                        }
                        */
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
        public void GetObjectByValue(int type, int index, ref SValue svalue)
        {
            SObject obj = null;
            if (type == 0)
            {
                obj = m_ArgumentObjectArray[index];
            }
            else if (type == 1)
            {
                obj = m_LocalVariableObjectArray[index];
            }
            else if (type == 2)
            {
                obj = m_ReturnObjectArray[index];
            }
            Debug.Assert(obj != null);
            if( obj.isNull )
            {
                svalue.SetNull();
                return;
            }
            SetSValue(obj, obj.eType, ref svalue);
        }
        public void SetSValue( SObject obj, EVMType etype, ref SValue svalue )
        {
            bool anyObj = svalue.eType == EVMType.Object;
            switch (etype)
            {
                case EVMType.Null:
                    {
                        svalue.SetNull();
                    }
                    break;
                case EVMType.Boolean:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Boolean)
                            {
                                svalue.SetBoolValue((bool)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetBoolValue((bool)to.value);
                            return;
                        }

                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        svalue.SetBoolValue(boolObj.value);
                    }
                    break;
                case EVMType.Byte:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Byte)
                            {
                                svalue.SetInt8Value((byte)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt8Value((Byte)to.value);
                            to.SetValue(EVMType.Byte, obj.value);
                            return;
                        }

                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        svalue.SetInt8Value(byteObj.value);
                    }
                    break;
                case EVMType.SByte:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.SByte)
                            {
                                svalue.SetSInt8Value((sbyte)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetSInt8Value((SByte)to.value);
                            return;
                        }

                        SInt8Object byteObj = obj as SInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        svalue.SetSInt8Value(byteObj.value);
                    }
                    break;
                case EVMType.Int16:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int16)
                            {
                                svalue.SetInt16Value((short)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt16Value((Int16)to.value);
                            return;
                        }

                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt16Value(int16Obj.value);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt16)
                            {
                                svalue.SetUInt16Value((ushort)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt16Value((UInt16)to.value);
                            return;
                        }

                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt16Value(uint16Obj.value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int32)
                            {
                                svalue.SetInt32Value((int)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if( to != null )
                        {
                            svalue.SetInt32Value((int)to.value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt32Value(int32Obj.value);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt32)
                            {
                                svalue.SetUInt32Value((uint)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt32Value((UInt32)to.value);
                            return;
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt32Value(uint32Obj.value);
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int64)
                            {
                                svalue.SetInt64Value((long)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt64Value((Int64)to.value);
                            return;
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt64Value(int64Obj.value);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt64)
                            {
                                svalue.SetUInt64Value((ulong)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt64Value((UInt64)to.value);
                            return;
                        }

                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt64Value(uint64Obj.value);
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Float32)
                            {
                                svalue.SetFloatValue((float)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetFloatValue((Single)to.value);
                            return;
                        }

                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetFloatValue(floatObj.value);
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Float64)
                            {
                                svalue.SetDoubleValue((double)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetDoubleValue((Double)to.value);
                            return;
                        }

                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetDoubleValue(doubleObj.value);
                    }
                    break;
                case EVMType.String:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.String)
                            {
                                svalue.SetStringValue((string)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetStringValue((String)to.value);
                            return;
                        }

                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetStringValue(stringObj.value);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (obj is ClassObject co)
                        {
                            svalue.SetSObject(co.value);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        switch( obj.eAnyType )
                        {
                            case EVMType.Boolean:
                                {
                                    svalue.SetBoolValue((bool)obj.value);
                                }
                                break;
                            case EVMType.Byte:
                                {
                                    svalue.SetInt8Value((byte)obj.value);
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    svalue.SetSInt8Value((sbyte)obj.value);
                                }
                                break;
                            case EVMType.Int16:
                                {
                                    svalue.SetInt16Value((Int16)obj.value);
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    svalue.SetUInt16Value((UInt16)obj.value);
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    svalue.SetInt32Value((Int32)obj.value);
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    svalue.SetUInt32Value((UInt32)obj.value);
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    svalue.SetInt64Value((Int64)obj.value);
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    svalue.SetUInt64Value((UInt64)obj.value);
                                }
                                break;
                            case EVMType.Float32:
                                {
                                    svalue.SetFloatValue((float)obj.value);
                                }
                                break;
                            case EVMType.Float64:
                                {
                                    svalue.SetDoubleValue((double)obj.value);
                                }
                                break;
                            case EVMType.String:
                                {
                                    svalue.SetStringValue((string)obj.value);
                                }
                                break;
                            default:
                                {
                                    svalue.SetSObject(obj.value as SObject);
                                }
                                break;
                        }
                    }
                    break;
                case EVMType.Class:
                    {
                        if (anyObj)
                        {
                            svalue.SetSObject(obj.value as SObject);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetSObject(to.value as SObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}
                        //4代表的是直接传值 ，所以，原来值啥样就传进去
                        //if (type == 4)
                        //{
                        //    if (obj is ClassObject co)
                        //    {
                        //        if (co.value != null)
                        //        {
                        //            svalue.SetSObject(co.value as SObject);
                        //        }
                        //        else
                        //        {
                        //            svalue.SetSObject(co);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        Debug.Assert(false);
                        //    }
                        //}
                        //else
                        //{
                            if (obj is ClassObject co)
                            {
                                svalue.SetSObject(co.value as SObject);
                            }
                            else
                            {
                                Debug.Assert(false);
                            }
                        //}
                        /*
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(svalue.int8Value == 1 ? true : false);
                            return;
                        }
                        */
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
    }
}
