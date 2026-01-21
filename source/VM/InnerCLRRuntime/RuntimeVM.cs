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
        public SValue[] m_ValueStack;
        public IntPtr m_RawBuffer;
        public RawSValue* m_RawPtr;
        public int m_RawCapacity;
        public ushort m_ValueIndex;
        private List<RuntimeType> m_InputTemplateRuntimeTypeList;
        private SObject[] m_LocalVariableObjectArray;
        private SObject[] m_ArgumentObjectArray;
        private SObject[] m_ReturnObjectArray;
        private IRMethod m_IRMethod;
        private IRData[] m_IRDataList;
        private ushort m_ExecuteIndex;
        private ushort m_ExecuteCount;
        private IRMetaClass m_IRMetaClass;
        public string id { get; set; }
        public int level { get; set; }
        public bool isPersistent { get; set; }

        public SObject[] returnObjectArray { get => m_ReturnObjectArray; }
        // Basic constructors used by other code paths
        private SValue[] m_LocalValueArray;
        private SValue[] m_ArgumentValueArray;

        public RuntimeVM(List<IRData> irlist)
        {
            m_IRDataList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_LocalValueArray = null;
            m_ArgumentValueArray = null;
        }
        public RuntimeVM(List<RuntimeType> rtList, IRMethod irMethod)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_IRMethod = irMethod;
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            if (m_IRMethod != null)
            {
                m_ArgumentValueArray = new SValue[m_IRMethod.methodArgumentList.Count];
                m_LocalValueArray = new SValue[m_IRMethod.methodLocalVariableList.Count];
            }
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
        }

        public void Init()
        {
            // minimal initialization
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
            // placeholder
        }
        public void ClearNewObject()
        {
            // placeholder
        }
        public void Run(bool disStackCount)
        {
            if (m_IRDataList == null || m_IRDataList.Length == 0) return;
            m_ExecuteIndex = 0;
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            while (m_ExecuteIndex < m_ExecuteCount)
            {
                var iri = m_IRDataList[m_ExecuteIndex];
                RunInstruction(iri);
                m_ExecuteIndex++;
            }
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
                case EIROpCode.LoadConstInt32:
                    if (iri.TryGetInt32(out int vi)) { var v = default(SValue); v.SetInt32Value(vi); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadConstInt64:
                    if (iri.TryGetInt64(out long l)) { var v = default(SValue); v.SetInt64Value(l); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadConstFloat:
                    if (iri.TryGetSingle(out float f)) { var v = default(SValue); v.SetFloatValue(f); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadConstDouble:
                    if (iri.TryGetDouble(out double d)) { var v = default(SValue); v.SetDoubleValue(d); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadConstBoolean:
                    if (iri.TryGetBoolean(out bool b)) { var v = default(SValue); v.SetBoolValue(b); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadConstString:
                    if (iri.TryGetString(out string s)) { var v = default(SValue); v.SetStringValue(s); PushSValueSynced(v); }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        var data = iri.index;
                        var v = default(SValue);
                        GetLocalVariableSValue(data, ref v);
                        PushSValueSynced(v);
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        var data = iri.index;
                        var v = default(SValue);
                        GetArgumentValue(data, ref v);
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
                    {
                        // unconditional jump to index
                        m_ExecuteIndex = (ushort)(iri.index - 1);
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
                case EIROpCode.CallDynamic:
                case EIROpCode.CallVirt:
                    {
                        if (iri.opValue is IRMethodCall imc)
                        {
                            InnerCLRRuntimeVM.RunIRMethod(m_InputTemplateRuntimeTypeList, imc.irMethod, true);
                        }
                    }
                    break;
                case EIROpCode.Label:
                    // noop
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
                    break;
            }
        }
        public void SetObjectByValue(int type, int index, ref SValue svalue)
        {
            // minimal: not implemented
        }
    }
}
