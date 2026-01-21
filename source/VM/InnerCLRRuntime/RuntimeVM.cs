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
        public RuntimeVM(List<IRData> irlist)
        {
            m_IRDataList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
        }
        public RuntimeVM(List<RuntimeType> rtList, IRMethod irMethod)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_IRMethod = irMethod;
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
        }
        public RuntimeVM(List<RuntimeType> rtList, List<IRData> irlist)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_IRDataList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
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
            // placeholder
        }
        public void SetArgumentValue(int index, SValue svalue)
        {
            // placeholder
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            // placeholder
        }
        public void GetLocalVariableSValue(int index, ref SValue svalue)
        {
            // placeholder
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            // placeholder
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
            // minimal executor: do nothing
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string MakeIndent(int count) { return new string(' ', count); }
        public static void SetValue(ref SValue sValue, ref SValue sStore, IRData iri)
        {
            // minimal: copy value
            sStore = sValue;
        }
        public void RunInstruction(IRData iri)
        {
            // minimal: no-op
        }
        public void SetObjectByValue(int type, int index, ref SValue svalue)
        {
            // placeholder
        }
    }
}
