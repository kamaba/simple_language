//****************************************************************************
//  File:      RuntimeMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: master use .net clr system. new create method instance than running code virtual machine 
//****************************************************************************
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace SimpleLanguage.VM.Runtime
{
    public class RuntimeVM
    {
        public string id { get; set; } = "";
        public int level { get; set; } = 0;
        public bool isPersistent { get; set; } = false;
        public SObject[] returnObjectArray => m_ReturnObjectArray;

        private SValue[] m_ValueStack = null;
        private ushort m_ValueIndex = 0;

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
                if( !sobjs[i].isVoid )
                {
                    m_ValueStack[m_ValueIndex++].SetSObject(sobjs[i]);
                }
            }
        }
        public SObject GetLocalVariableValue( int index )
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return null;
            }
            if(index < 0 )
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!-");
                return null;
            }

            return m_LocalVariableObjectArray[index];
        }
        public SObject GetArgumentValue( int index )
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, $"SVM Error FunctionName:{this.id} 执行的参数超出范围!!");
                return null;
            }
            return m_ArgumentObjectArray[index];
        }
        public void SetArgumentValue( int index, SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_ArgumentObjectArray[index], ref svalue);
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_LocalVariableObjectArray[index], ref svalue);
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (index > m_ReturnObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_ReturnObjectArray[index], ref svalue);
        }
        public SValue GetCurrentIndexValue( bool isRecude )
        {
            if(isRecude)
            {
                m_ValueIndex--;
                return m_ValueStack[m_ValueIndex];
            }
            else
            {
                return m_ValueStack[m_ValueIndex-1];
            }
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
                RuntimeType rt = irmt.isArray ? RuntimeTypeManager.arrayRuntimeType :
                    RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
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
            SValue sval = InnerCLRRuntimeVM.topCLRRuntime.GetCurrentIndexValue(false);
            m_ValueStack[m_ValueIndex++] = sval;
            m_IRMetaClass = sval.sobject?.irMetaClass;
        }
        public void ClearNewObject()
        {
            m_IRMetaClass = null;
        }
        public void Run()
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
                SValue sval = topClrRuntime.GetCurrentIndexValue(true);
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
                case EType.Boolean:
                case EType.Byte: sStore.SetInt8Value(sValue.int8Value); break;
                case EType.SByte: sStore.SetSInt8Value(sValue.sint8Value); break;
                case EType.Int16: sStore.SetInt16Value(sValue.int16Value); break;
                case EType.UInt16: sStore.SetUInt16Value(sValue.uint16Value); break;
                case EType.Int32: sStore.SetInt32Value(sValue.int32Value); break;
                case EType.UInt32: sStore.SetUInt32Value(sValue.uint32Value); break;
                case EType.Int64: sStore.SetInt64Value(sValue.int64Value); break;
                case EType.UInt64: sStore.SetUInt64Value(sValue.uint64Value); break;
                case EType.Float32: sStore.SetFloatValue(sValue.floatValue); break;
                case EType.Float64: sStore.SetDoubleValue(sValue.doubleValue); break;
                case EType.String: sStore.SetStringValue(sValue.stringValue); break;
                case EType.Null:
                    {
                        sStore.SetNull();
                    }
                    break;
                case EType.Class:
                    {
                        (sStore.sobject as ClassObject).SetMemberVariableSValue(iri.index, sValue);
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
                        m_ValueStack[m_ValueIndex++].SetNullValue();
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
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Byte);
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.SByte);
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int16);
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt16);
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int32);
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt32);
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int64);
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt64);
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        m_ValueStack[m_ValueIndex-1].ConvertByEType(EType.Float32);
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Float64);
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        var vval = GetArgumentValue(iri.index);
                        m_ValueStack[m_ValueIndex++].SetSObject(vval);
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        var vval = GetLocalVariableValue(iri.index);
                        m_ValueStack[m_ValueIndex++].SetSObject( vval );
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
                    }
                    break; 
                case EIROpCode.LoadNotStaticField:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];

                        if (v.eType == EType.Class)
                        {
                            var co = (v.sobject as ClassObject);
                            co.value.GetMemberVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
                        }
                        //栈位不变，因为当前对象位的被通过索引取出来的成员变量值，覆盖掉， 所以栈位不会发生变化
                    }
                    break;
                case EIROpCode.StoreNotStaticField2:
                    {
                        // -2在存储的值 -1表示要存储的对象 存储完成，直接变成位置0
                        SValue sValue = m_ValueStack[m_ValueIndex - 2];
                        SValue sStore = m_ValueStack[m_ValueIndex - 1];
                        SetValue(ref sValue, ref sStore, iri);
                        m_ValueIndex -= 2;
                    }
                    break;
                case EIROpCode.StoreNotStaticField1:
                    {
                        // -2在存储的值 -1表示要存储的对象 存储完成，直接变成位置0
                        SValue sValue = m_ValueStack[m_ValueIndex - 1];
                        SValue sStore = m_ValueStack[m_ValueIndex - 2];
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
                        if (v.eType == EType.Array)
                        {
                            v.arrayValue.LoadValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
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

                        if (sStore.eType == EType.Array)
                        {
                            sStore.arrayValue.StoreValue(iri.index, sValue);
                        }
                        else
                        {
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 2;
                    }
                    break;
                case EIROpCode.LoadArrayIndexField:
                    {
                        SValue arrayref = m_ValueStack[m_ValueIndex - 2];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            arrayref.arrayValue.LoadValue(index, ref m_ValueStack[m_ValueIndex - 2]);
                        }
                        else
                        {
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 1;
                    }
                    break;
                case EIROpCode.StoreArrayIndexField:
                    {
                        SValue storevalue = m_ValueStack[m_ValueIndex - 3];
                        SValue arrayref = m_ValueStack[m_ValueIndex - 2];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            arrayref.arrayValue.StoreValue(index, storevalue );
                        }
                        else
                        {
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

                        for ( int i = 0; i < mfc.irTemplateMetaType.Count; i++ )
                        {
                            var crt = GetMethodRuntimeType(mfc.irTemplateMetaType[i]);
                            classRTList.Add(crt);
                        }
                        InnerCLRRuntimeVM.RunIRMethod( classRTList, mfc.irMethod );
                    }
                    break;
                case EIROpCode.CallDynamic:
                    {
                        var mfc = iri.opValue as IRMethodCall;

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

                            if (v.eType == EType.Class)
                            {
                                var co = (v.sobject as ClassObject);
                                irc = co.value.irMetaClass;
                            }
                            else
                            {
                                irc = IRManager.instance.GetIRMetaClassByName(v.eType.ToString());
                            }
                            if (irc == null)
                            {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                                return;
                            }
                        }
                        else
                        {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                        }
                        //InnerCLRRuntimeVM.RunIRMethod(mfc.metaType, mfc.metaTypeList, mfc.irMethod);
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

                        RuntimeType rt = null;
                        IRMetaClass irc = null;
                        if (v.eType == EType.Class)
                        {
                            var co = (v.sobject as ClassObject);
                            irc = co.value.irMetaClass;
                            rt = co.value.runtimeType;
                        }
                        else if( v.eType == EType.Array )
                        {
                            irc = IRManager.instance.GetIRMetaClassByName("Array");
                            rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irc);
                        }
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

                        //Dictionary<int,int> mapTemplate = irc.GetIRMetaTypeByTemplateAndClassRelation(cfc.irOwnerMetaClass, 0 );

                        List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                        for ( int i = 0; i < mfc.irTemplateMetaType.Count; i++ )
                        {
                            var crt = GetClassRuntimeType(mfc.irTemplateMetaType[i], irc, rt.runtimeTemplateList, true );
                            rtList.Add(crt);
                        }
                        //if( irc.irName == "Int8"
                        //    || irc.irName == "SInt8"
                        //    || irc.irName == "Int16"
                        //    || irc.irName == "UInt16"
                        //    || irc.irName == "Int32"
                        //    || irc.irName == "UInt32"
                        //    || irc.irName == "Int64"
                        //    || irc.irName == "UInt64" )
                        //{
                        //    return;
                        //}

                        InnerCLRRuntimeVM.RunIRMethod( rtList, cfc);
                    }
                    break;
                case EIROpCode.CallCSharpMethod:
                    {
                        var mfc = iri.opValue as IRCallFunction;
                        Object targetObject = null;
                        Object[] paramsObj = new Object[mfc.paramCount];
                        if( mfc.target )
                        {
                            targetObject = m_ValueStack[--m_ValueIndex].CreateCSharpObject();
                        }
                        for (int i = 0; i < paramsObj.Length; i++)
                        {
                            paramsObj[i] = m_ValueStack[m_ValueIndex-paramsObj.Length+i].CreateCSharpObject();
                        }
                        m_ValueIndex -= (ushort)(paramsObj.Length-1);
                        Object obj = mfc.InvokeCSharp(targetObject, paramsObj);
                        if (obj != null)
                        {
                            m_ValueStack[m_ValueIndex++].CreateSObjectByCSharpObject(obj);
                        }
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
                    }
                    break;
                case EIROpCode.NewTemplateClass:
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
                        InnerCLRRuntimeVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
                    }
                    break;
                case EIROpCode.NewArray:
                    {
                        IRNewArray mdt = iri.opValue as IRNewArray;
                        //var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(mdt);
                        ArrayObject sob = new ArrayObject(mdt.eArrayType, mdt.length);
                        ObjectManager.AddArrayObject(sob);
                        m_ValueStack[m_ValueIndex++].SetSObject(sob);
                    }
                    break;
                case EIROpCode.Dup:
                    {
                        var sval = m_ValueStack[m_ValueIndex - 1];
                        m_ValueStack[m_ValueIndex++] = sval;
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
                        if (v.eType == EType.Boolean)
                        {
                            if (v.int8Value == 0)
                            {
                                m_ExecuteIndex = (ushort)iri.index;
                            }
                        }
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        var v = m_ValueStack[--m_ValueIndex];
                        if (v.eType == EType.Boolean)
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
                        if( m_ValueIndex - 2 < 0 )
                        {
                            Log.AddVM(EError.None, "Error 加法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-2].AddSValue(ref m_ValueStack[m_ValueIndex-1], false );
                        m_ValueIndex--;
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
                        m_ValueStack[m_ValueIndex].NotSValue();
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Neg运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex].NegSValue(false);
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 4, false);
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
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 5, false);
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
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 0, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cne:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 1, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cgt:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 2, false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cge:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 2, true);
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
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 3, false );
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
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 3, true);
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


                        var v1 = m_ValueStack[m_ValueIndex-1];

                        if( v1.eType == EType.Class )
                        {
                            if (!v1.sobject.runtimeType.IsExtendsRelation(rt))
                            {
                                m_ValueStack[m_ValueIndex - 1].SetNull();
                            }
                        }
                        else
                        {                            
                            if (v1.eType != rt.eType )
                            {
                                m_ValueStack[m_ValueIndex - 1].SetNull();
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
    }
}
