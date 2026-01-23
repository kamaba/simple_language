//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.VM;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.VM.Runtime
{
    public unsafe class RuntimeVM
    {
        public SObject[] returnObjectArray { get => m_ReturnObjectArray; }


        public SValue[] m_ValueStack;
        public IntPtr m_RawBuffer;
        public RawSValue* m_RawPtr;
        public int m_RawCapacity;
        public ushort m_ValueIndex;


        private List<RuntimeType> m_InputTemplateRuntimeTypeList;

        private SObject[] m_LocalVariableObjectArray;
        private SValue[] m_LocalValueArray;

        private SObject[] m_ArgumentObjectArray;
        private SValue[] m_ArgumentValueArray;

        private SObject[] m_ReturnObjectArray;

        private IRMethod m_IRMethod;
        private IRData[] m_IRDataList;
        private ushort m_ExecuteIndex;
        private ushort m_ExecuteCount;
        private IRMetaClass m_IRMetaClass;
        public string id { get; set; }
        public int level { get; set; }
        public bool isPersistent { get; set; }

        public RuntimeVM(List<IRData> irlist)
        {
            m_IRDataList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_LocalValueArray = null;
            m_ArgumentValueArray = null;

            Init();
        }
        public RuntimeVM(List<RuntimeType> rtList, IRMethod irMethod)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_IRMethod = irMethod;
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_IRDataList = irMethod.IRDataList.ToArray();
            if (m_IRMethod != null)
            {
                m_ArgumentValueArray = new SValue[m_IRMethod.methodArgumentList.Count];
                m_LocalValueArray = new SValue[m_IRMethod.methodLocalVariableList.Count];
            }
            Init();
        }
        public RuntimeVM(List<RuntimeType> rtList, List<IRData> irlist)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_IRDataList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_LocalValueArray = null;
            m_ArgumentValueArray = null;

            Init();
        }

        public void Init()
        {
            //参数列表 argument variable table
            if (m_IRMethod != null)
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
                for (int i = 0; i < m_ArgumentObjectArray.Length; i++)
                {
                    Log.AddVM(EError.None, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentObjectArray[i].ToString() + "]");
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
        public bool IsNumericTypeLocal(EVMType t)
        {
            return t == EVMType.Num || t == EVMType.Int32 || t == EVMType.Int64 || t == EVMType.Float32 || t == EVMType.Float64;
        }
        public void SyncRawFromSValue(int index)
        {
            // minimal noop for now
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncRawAtIndex(int index)
        {
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushSValueSynced(in SValue v)
        {
            if (m_ValueStack == null) m_ValueStack = new SValue[1024];
            if (m_ValueIndex >= m_ValueStack.Length) return;
            m_ValueStack[m_ValueIndex++] = v;
        }
        public SObject CreateObjectByIRMetaType(IRMetaType irmt, IRMetaClass curIrMc, bool isAdd = false)
        {
            return ObjectManager.CreateObjectByRuntimeType(RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt), false);
        }
        public void AddReturnObjectArray(SObject[] sobjs)
        {
            m_ReturnObjectArray = sobjs;
        }
        public void GetArgumentValue(int index, ref SValue svalue)
        {
            if (m_ArgumentValueArray == null || index < 0 || index >= m_ArgumentValueArray.Length)
            {
                svalue.SetNull();
                return;
            }
            svalue = m_ArgumentValueArray[index];
        }
        public void SetArgumentValue(int index, SValue svalue)
        {
            if (m_ArgumentValueArray == null || index < 0 || index >= m_ArgumentValueArray.Length) return;
            m_ArgumentValueArray[index] = svalue;
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (m_LocalValueArray == null || index < 0 || index >= m_LocalValueArray.Length) return;
            m_LocalValueArray[index] = svalue;
        }
        public void GetLocalVariableSValue(int index, ref SValue svalue)
        {
            if (m_LocalValueArray == null || index < 0 || index >= m_LocalValueArray.Length)
            {
                svalue.SetNull();
                return;
            }
            svalue = m_LocalValueArray[index];
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (m_ReturnObjectArray == null)
            {
                if (m_IRMethod != null)
                {
                    m_ReturnObjectArray = new SObject[m_IRMethod.methodReturnVariableList.Count];
                }
            }
            if (m_ReturnObjectArray != null && index >= 0 && index < m_ReturnObjectArray.Length)
            {
                m_ReturnObjectArray[index] = svalue.CreateSObject();
            }
        }
        public SValue GetCurrentIndexValue(int index)
        {
            if (m_ValueStack == null || m_ValueIndex == 0) return default;
            if (index < 0 || index >= m_ValueIndex) return default;
            return m_ValueStack[index];
        }
        public RuntimeType GetClassRuntimeType(IRMetaType irmt, IRMetaClass curIRMc, List<RuntimeType> __rtList, bool isAdd = false)
        {
            return RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
        }
        public RuntimeType GetMethodRuntimeType(IRMetaType irmt)
        {
            return RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
        }
        public void SetNewObject()
        {
            SValue sval = InnerCLRRuntimeVM.topCLRRuntime.GetCurrentIndexValue(InnerCLRRuntimeVM.topCLRRuntime.m_ValueIndex - 1);
            m_ValueStack[m_ValueIndex++] = sval;
            m_IRMetaClass = sval.sobject?.irMetaClass;
        }
        public void ClearNewObject()
        {
            m_IRMetaClass = null;
        }
        public void Run(bool disStackCount)
        {
            if (m_IRDataList == null || m_IRDataList.Length == 0) return;

            string funName = id;

            string pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Push] Method: [" + funName + "]");
            level++;

            var topClrRuntime = InnerCLRRuntimeVM.topCLRRuntime;
            for (int i = 0; i < m_ArgumentObjectArray.Length; i++)
            {
                SValue sval;
                if (disStackCount)
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

            m_ExecuteIndex = 0;
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            while (m_ExecuteIndex < m_ExecuteCount)
            {
                var iri = m_IRDataList[m_ExecuteIndex];
                RunInstruction(iri);
                m_ExecuteIndex++;
            }


            level--;
            pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string MakeIndent(int count) { return new string(' ', count); }
        public static void SetValue(ref SValue sValue, ref SValue sStore, IRData iri)
        {
            sStore = sValue;
        }
        public void RunInstruction(IRData iri)
        {
            if (iri == null) return;
            switch (iri.opCode)
            {
                case EIROpCode.Nop:break;
                case EIROpCode.LoadConstNull:
                    {
                        m_ValueStack[m_ValueIndex].SetNull();
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        if (iri.TryGetBoolean(out bool b)) { var v = default(SValue); v.SetBoolValue(b); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstByte:
                    {
                        if (iri.TryGetByte(out byte cb)) { var v = default(SValue); v.SetInt8Value(cb); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstSByte:
                    {
                        if (iri.TryGetSByte(out sbyte sb)) { var v = default(SValue); v.SetSInt8Value(sb); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt16:
                    {
                        if (iri.TryGetInt16(out short sv)) { var v = default(SValue); v.SetInt16Value(sv); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        if (iri.TryGetUInt16(out ushort usv)) { var v = default(SValue); v.SetUInt16Value(usv); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        if (iri.TryGetInt32(out int i32)) { var v = default(SValue); v.SetInt32Value(i32); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        if (iri.TryGetUInt32(out uint ui32)) { var v = default(SValue); v.SetUInt32Value(ui32); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        if (iri.TryGetInt64(out long l)) { var v = default(SValue); v.SetInt64Value(l); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        if (iri.TryGetUInt64(out ulong ul)) { var v = default(SValue); v.SetUInt64Value(ul); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstFloat:
                    {
                        if (iri.TryGetSingle(out float f)) { var v = default(SValue); v.SetFloatValue(f); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstDouble:
                    {
                        if (iri.TryGetDouble(out double d)) { var v = default(SValue); v.SetDoubleValue(d); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        if (iri.TryGetString(out string s)) { var v = default(SValue); v.SetStringValue(s); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstType:
                    {

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
                            m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float32);
                        }
                        break;
                    case EIROpCode.Convert_R8:
                        {
                            m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float64);
                        }
                        break;
                    //case EIROpCode.LoadArgument:
                    //    {
                    //        GetArgumentValue(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    //    }
                    //    break;
                    case EIROpCode.LoadArgument:
                        {
                            var data = iri.index;
                            var v = default(SValue);
                            GetArgumentValue(data, ref v);
                            PushSValueSynced(v);
                        }
                        break;
                    case EIROpCode.LoadLocal:
                        {
                            var data = iri.index;
                            var v = default(SValue);
                            GetLocalVariableSValue(data, ref v);
                            PushSValueSynced(v);
                        }
                        break;
                    case EIROpCode.LocalGlobal:
                        {
                            var data = iri.index;
                            var v = default(SValue);
                            InnerCLRRuntimeVM.LoadGlobalVariable(data, ref v);
                            PushSValueSynced(v);
                        }
                        break;
                    case EIROpCode.StoreLocal:
                        {
                            var idx = iri.index;
                            if (m_ValueIndex > 0)
                            {
                                var val = m_ValueStack[--m_ValueIndex];
                                SetLocalVariableSValue(idx, val);
                            }
                        }
                        break;
                    case EIROpCode.StoreReturn:
                        {
                            SetReturnVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                            m_ExecuteIndex = m_ExecuteCount;
                        }
                        break;
                    case EIROpCode.StoreGlobal:
                        {
                            var id = iri.index;
                            if (m_ValueIndex > 0)
                            {
                                var val = m_ValueStack[--m_ValueIndex];
                                InnerCLRRuntimeVM.StoreGlobalVariable(id, ref val);
                            }
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
                            if (iri.opValue is Boolean flag)
                            {
                                if (flag)
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
                                (arrayref.sobject as ArrayObject).StoreValue(index, storevalue);
                            }
                            else
                            {
                                Debug.Assert(false, "不是数组类型!!");
                                Log.AddVM(EError.None, "不是数组类型!!");
                            }
                            m_ValueIndex -= 3;
                        }
                        break;
                    case EIROpCode.Dup:
                        if (m_ValueIndex > 0)
                        {
                            var v = m_ValueStack[m_ValueIndex - 1];
                            PushSValueSynced(v);
                        }
                        break;
                    case EIROpCode.Pop:
                        if (m_ValueIndex > 0) m_ValueIndex--;
                        break;
                    case EIROpCode.LoadNotStaticField:
                        {
                            // expects instance on stack
                            if (m_ValueIndex > 0)
                            {
                                var inst = m_ValueStack[--m_ValueIndex];
                                if (inst.eType == EVMType.Class || inst.eType == EVMType.Object)
                                {
                                    var v = default(SValue);
                                    if (inst.sobject is ClassObject co)
                                    {
                                        co.GetMemberVariableSValue(iri.index, ref v);
                                        PushSValueSynced(v);
                                    }
                                    else
                                    {
                                        PushSValueSynced(v);
                                    }
                                }
                            }
                        }
                        break;
                    case EIROpCode.StoreNotStaticField2:
                        {
                            // expect value then instance on stack (value pushed last)
                            if (m_ValueIndex >= 2)
                            {
                                var val = m_ValueStack[--m_ValueIndex];
                                var inst = m_ValueStack[--m_ValueIndex];
                                if (inst.eType == EVMType.Class || inst.eType == EVMType.Object)
                                {
                                    if (inst.sobject is ClassObject co)
                                    {
                                        co.SetMemberVariableSValue(iri.index, val);
                                    }
                                }
                            }
                        }
                        break;
                    case EIROpCode.NewObject:
                        {
                            if (iri.opValue is IRMetaClass irmc)
                            {
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irmc);
                                if (rt == null) rt = RuntimeTypeManager.AddRuntimeTypeByClass(irmc);
                                var sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                                var sv = default(SValue);
                                sv.SetSObject(sobj);
                                PushSValueSynced(sv);
                            }
                        }
                        break;
                    case EIROpCode.NewTemplateObject:
                        {
                            if (iri.opValue is IRMetaType irmt)
                            {
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
                                if (rt == null && irmt?.m_IRMetaClass != null) rt = RuntimeTypeManager.AddRuntimeTypeByClass(irmt.m_IRMetaClass);
                                var sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                                var sv = default(SValue);
                                sv.SetSObject(sobj);
                                PushSValueSynced(sv);
                            }
                        }
                        break;
                    case EIROpCode.NewArray:
                        {
                            // expects length on stack
                            if (m_ValueIndex > 0 && iri.opValue is IRMetaType irmt)
                            {
                                var lenVal = m_ValueStack[--m_ValueIndex];
                                int len = 0;
                                if (lenVal.eType == EVMType.Int32) len = lenVal.int32Value;
                                else if (lenVal.eType == EVMType.Int64) len = (int)lenVal.int64Value;
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
                                var arr = new ArrayObject(rt, len);
                                var sv = default(SValue);
                                sv.SetSObject(arr);
                                PushSValueSynced(sv);
                            }
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
                            if (m_ValueIndex > 0)
                            {
                                var cond = m_ValueStack[--m_ValueIndex];
                                bool isTrue = cond.eType == EVMType.Boolean ? cond.int8Value == 1 : cond.GetValueObject() != null;
                                if (!isTrue)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
                                }
                            }
                        }
                        break;
                    case EIROpCode.BrTrue:
                        {
                            if (m_ValueIndex > 0)
                            {
                                var cond = m_ValueStack[--m_ValueIndex];
                                bool isTrue = cond.eType == EVMType.Boolean ? cond.int8Value == 1 : cond.GetValueObject() != null;
                                if (isTrue)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
                                }
                            }
                        }
                        break;
                    case EIROpCode.Ceq:
                        {
                            if (m_ValueIndex >= 2)
                            {
                                var right = m_ValueStack[--m_ValueIndex];
                                var left = m_ValueStack[--m_ValueIndex];
                                bool methodCall = false;
                                SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                                PushSValueSynced(left);
                            }
                        }
                        break;
                    case EIROpCode.Cne:
                        {
                            if (m_ValueIndex >= 2)
                            {
                                var right = m_ValueStack[--m_ValueIndex];
                                var left = m_ValueStack[--m_ValueIndex];
                                bool methodCall = false;
                                SValue.CompareEuqalSValue1AndValue2(ref left, ref right, false, out methodCall);
                                PushSValueSynced(left);
                            }
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
                    case EIROpCode.Add:
                    case EIROpCode.Minus:
                    case EIROpCode.Multiply:
                    case EIROpCode.Divide:
                    case EIROpCode.Modulo:
                        {
                            if (m_ValueIndex >= 2)
                            {
                                var right = m_ValueStack[--m_ValueIndex];
                                var left = m_ValueStack[--m_ValueIndex];
                                int sign = 0;
                                bool isUn = false;
                                switch (iri.opCode)
                                {
                                    case EIROpCode.Add: sign = 0; break;
                                    case EIROpCode.Minus: sign = 1; break;
                                    case EIROpCode.Multiply: sign = 2; break;
                                    case EIROpCode.Divide: sign = 3; break;
                                    case EIROpCode.Modulo: sign = 4; break;
                                }
                                SValue.ComputeValueInline(ref left, sign, ref right, isUn);
                                PushSValueSynced(left);
                            }
                        }
                        break;
                    case EIROpCode.CallCSharpMethod:
                        {
                            if (iri.opValue is IRCallFunction ircf)
                            {
                                ircf.InvokeCSharp(this);
                            }
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
                            if (rt == null)
                            {
                                rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(mfc.metaType.irMetaClass, classRTList);
                            }

                            if (mfc.irMethod.id == "type")
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
                                    || v.eType == EVMType.Array)
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
                                    if (mfc.irMethod.interfaceMethod)
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
                            if (stackIndex < 0)
                            {
                                Log.AddVM(EError.None, "StackIndex 是负数!");
                                return;
                            }
                            var v = m_ValueStack[stackIndex];

                            if (v.isNull)
                            {
                                Debug.Assert(false, "当前值为空!!");
                                return;
                            }

                            RuntimeType rt = null;
                            IRMetaClass irc = null;
                            if (v.eType == EVMType.Class || v.eType == EVMType.Array)
                            {
                                var co = (v.sobject as ClassObject);
                                irc = co.irMetaClass;
                                rt = co.runtimeType;
                            }
                            else if (v.eType == EVMType.Object)
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
                            if (irc == null)
                            {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                                return;
                            }
                            IRMethod cfc = irc.GetIRNonStaticMethodByIndex(iri.index);


                            if (cfc == null)
                            {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                                Debug.Assert(false, "没有找到索引是" + iri.index + "的函数!");
                                return;
                            }
                            List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                            for (int i = 0; i < mfc.irTemplateMetaType.Count; i++)
                            {
                                var crt = GetClassRuntimeType(mfc.irTemplateMetaType[i], irc, rt.runtimeTemplateList, true);
                                rtList.Add(crt);
                            }
                            InnerCLRRuntimeVM.RunIRMethod(rtList, cfc);

                            var a = ObjectManager.classObjectDict;
                        }
                        break;
                    case EIROpCode.Ldc:
                        {
                            if (iri.opValue is IRMetaType irmt)
                            {
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
                                if (rt == null && irmt?.m_IRMetaClass != null)
                                {
                                    rt = RuntimeTypeManager.AddRuntimeTypeByClass(irmt.m_IRMetaClass);
                                }

                                var sobj = new TypeObject(rt);
                                var sv = default(SValue);
                                sv.SetSObject(sobj);
                                PushSValueSynced(sv);
                            }
                        }
                        break;
                    case EIROpCode.Ret:
                        // stop execution early
                        m_ExecuteIndex = m_ExecuteCount;
                        break;
                    case EIROpCode.LoadStaticField:
                        {
                            if (iri.opValue is IRMetaType mt)
                            {
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(mt);
                                var v = default(SValue);
                                if (rt != null)
                                {
                                    rt.GetMemberVariableSValue(iri.index, ref v);
                                    PushSValueSynced(v);
                                }
                            }
                        }
                        break;
                    case EIROpCode.StoreStaticField:
                        {
                            if (iri.opValue is IRMetaType mt)
                            {
                                if (m_ValueIndex > 0)
                                {
                                    var val = m_ValueStack[--m_ValueIndex];
                                    var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(mt);
                                    rt?.SetMemberVariableSValue(iri.index, val);
                                }
                            }
                        }
                        break;
                    default:
                        // unhandled op
                        Debug.Assert(false);
                        break;
                    }
        }
        public void SetObjectByValue(int type, int index, ref SValue svalue)
        {
            // minimal: not implemented
        }
    }
}
