//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using SimpleLanguage.VM.MemoryManagement;
using SimpleLanuageVM.Load;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleLanguage.VM.Runtime
{
    public class RuntimeVM
    {
        public RuntimeObject[] returnRuntimeObjectArray { get => m_ReturnRuntimeObjectArray; }
#if DEBUG
        public ushort valueIndex => m_ValueIndex;
#else
        public ushort valueIndex => (ushort)ByteStackSlotDepthCount;
#endif
        public string id => m_Id;
        public int level => m_Level;


        private string m_Id = "";
        private int m_Level = 0;
        private RuntimeObject[] m_LocalVariableRuntimeObjectArray;
        private RuntimeObject[] m_ArgumentRuntimeObjectArray;
        private RuntimeObject[] m_ReturnRuntimeObjectArray;

        private RuntimeMethod m_Method;
        private Instruction[] m_InstructionList;
        private ushort m_ExecuteIndex;
        private ushort m_ExecuteCount;
        private RuntimeType m_CurrentRuntimeType;
        private List<RuntimeType> m_RuntimeTemplateRuntimeTypeList;
        private List<RuntimeType> m_RuntimeTypeList = new List<RuntimeType>();
        // RuntimeValue[] stack - debug mirror of the byte stack.
        // Operations on this array are guarded by #if DEBUG; the byte-level
        // m_ByteStack below is the primary eval stack (mirrors cvm VM.stack).
#if DEBUG
        private RuntimeValue[] m_ValueStack;
        private ushort m_ValueIndex;
#endif

        // ---- Byte-level eval stack (mirrors cvm VM.stack / VM.sp) ----
        private const int VM_STACK_SIZE = 8192;

        // VM_PTR_SIZE – byte width of pointer/handle values (PTR/STRING slots).
        // Must match cvm vm_runtime.h  #define VM_PTR_SIZE
        // and Frontend Define.cs  VM_PTR_SIZE.
        // Options: 2 (short), 4 (int), 8 (long).
        private const int VM_PTR_SIZE = 4;
        private byte[] m_ByteStack = new byte[VM_STACK_SIZE];
        private int m_ByteSp;                              // stack pointer (byte offset)
        // ---- Slot kind tracking (mirrors cvm VM.stack_slot_kind / VM.stack_slot_depth) ----
        private byte[] m_StackSlotKind = new byte[VM_STACK_SIZE];
        private int m_StackSlotDepth;
        // ---- Object pool for PTR/STRING references (mirrors cvm VM.obj_pool) ----
        private List<SObject?> m_ObjPool = new List<SObject?>();

        /** Slot kind enum — mirrors cvm VMStackSlotKind (vm_runtime.h). */
        private enum VMStackSlotKind : byte
        {
            INT32 = 1,
            UINT32 = 2,
            INT64 = 3,
            UINT64 = 4,
            INT8 = 5,
            UINT8 = 6,
            INT16 = 7,
            UINT16 = 8,
            FLOAT32 = 9,
            FLOAT64 = 10,
            PTR = 11,
            STRING = 12,
        }


        public RuntimeVM(string id)
        {
            m_Method = null;
            m_Id = id;
#if DEBUG
            m_ValueStack = new RuntimeValue[1024];
            m_ValueIndex = 0;
#endif
            ResetByteStack();
            m_RuntimeTemplateRuntimeTypeList = new List<RuntimeType>();
            m_RuntimeTypeList = new List<RuntimeType>(0);
            //m_RawCapacity = 1024;
            m_InstructionList = new Instruction[0];
            m_CurrentRuntimeType = null;
            Init();
        }
        public RuntimeVM(string id, RuntimeType rt, List<RuntimeType> irmtList, List<Instruction> irlist)
        {
            m_Method = null;
            m_Id = id;
#if DEBUG
            m_ValueStack = new RuntimeValue[1024];
            m_ValueIndex = 0;
#endif
            ResetByteStack();
            m_RuntimeTemplateRuntimeTypeList = irmtList;
            m_RuntimeTypeList = new List<RuntimeType>(rt.runtimeTemplateList.Count + irmtList.Count);
            m_RuntimeTypeList.AddRange(rt.runtimeTemplateList);
            m_RuntimeTypeList.AddRange(irmtList);
            //m_RawCapacity = 1024;
            m_InstructionList = irlist?.ToArray();
            m_CurrentRuntimeType = rt;
            Init();
        }
        public RuntimeVM(RuntimeType rt, List<RuntimeType> irmtList, RuntimeMethod rm)
        {
            m_Method = rm;
            m_Id = rm.id;
#if DEBUG
            m_ValueStack = new RuntimeValue[1024];
            m_ValueIndex = 0;
#endif
            ResetByteStack();
            m_RuntimeTemplateRuntimeTypeList = irmtList;
            int count = irmtList.Count;
            if (rt?.runtimeTemplateList != null)
            {
                count += rt.runtimeTemplateList.Count;
            }
            m_RuntimeTypeList = new List<RuntimeType>(count);
            if (rt?.runtimeTemplateList != null)
            {
                m_RuntimeTypeList.AddRange(rt.runtimeTemplateList);
            }
            m_RuntimeTypeList.AddRange(irmtList);
            m_InstructionList = rm.InstructionList.ToArray();
            m_CurrentRuntimeType = rt;
            Init();
        }
        public void Init()
        {
            //argument variable table
            if (this.m_Method != null)
            {
                m_ReturnRuntimeObjectArray = new RuntimeObject[m_Method.methodReturnVariableList.Count];
                for (int i = 0; i < m_Method.methodReturnVariableList.Count; i++)
                {
                    m_ReturnRuntimeObjectArray[i] = CreateRuntimeObject(m_Method.methodReturnVariableList[i], null);
#if DEBUG
                    Log.AddRuntimeLog(LID.ShowMessageInfo, "Ret_" + i.ToString() + "_Value: [" + m_ReturnRuntimeObjectArray[i]?.ToString() + "]");
#endif
                }

                m_ArgumentRuntimeObjectArray = new RuntimeObject[m_Method.methodArgumentList.Count];
                for (int i = 0; i < m_Method.methodArgumentList.Count; i++)
                {
                    m_ArgumentRuntimeObjectArray[i] = CreateRuntimeObject(m_Method.methodArgumentList[i], null);
#if DEBUG
                    Log.AddRuntimeLog(LID.ShowMessageInfo, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentRuntimeObjectArray[i]?.ToString() + "]");
#endif
                }
                //local variable table
                m_LocalVariableRuntimeObjectArray = new RuntimeObject[m_Method.methodLocalVariableList.Count];
                for (int i = 0; i < m_Method.methodLocalVariableList.Count; i++)
                {
                    m_LocalVariableRuntimeObjectArray[i] = CreateRuntimeObject(m_Method.methodLocalVariableList[i], null);
#if DEBUG
                    Log.AddRuntimeLog(LID.ShowMessageInfo, "Variable_" + i.ToString() + m_LocalVariableRuntimeObjectArray[i].ToString());
#endif
                }
            }

            else
            {
                m_ReturnRuntimeObjectArray = new RuntimeObject[0];
                m_ArgumentRuntimeObjectArray = new RuntimeObject[0];
                m_LocalVariableRuntimeObjectArray = new RuntimeObject[0];
            }
            var count = m_InstructionList.Length;
#if DEBUG
            if (count < 48)
            {
                m_ValueStack = new RuntimeValue[128];
            }
            else if (count >= 48 && count < 150)
            {
                m_ValueStack = new RuntimeValue[160];
            }
            else if (count >= 150 && count < 300)
            {
                m_ValueStack = new RuntimeValue[200];
            }
            else if (count >= 300 && count < 500)
            {
                m_ValueStack = new RuntimeValue[300];
            }
            else if (count >= 500 && count < 800)
            {
                m_ValueStack = new RuntimeValue[400];
            }
            else
            {
                m_ValueStack = new RuntimeValue[500];
            }
#endif

            SlMemoryManager.Instance.RegisterVmForRootCollection(this);
        }
        public RuntimeObject CreateRuntimeObject(RuntimeVariable rv, SObject sobj)
        {
            RuntimeType rt = null;

            var ownerClass = m_Method?.ownerMetaClass ?? rv.runtimeDefType.ownerRuntimeClass;
            if (ownerClass != null && m_RuntimeTypeList.Count > 0)
            {
                rt = GetRuntimeTypeByDefType(rv.runtimeDefType, ownerClass, m_RuntimeTypeList, true);
            }
            else
            {
                rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(rv.runtimeDefType);
            }
            return new RuntimeObject(rt, rv, sobj);
        }
        public void SetValueIndex(int vindex)
        {
#if DEBUG
            m_ValueIndex = (ushort)vindex;
#endif
        }
        /// <summary>GC roots: byte-stack obj pool, debug value stack, and argument/local/return runtime object slots.</summary>
        internal void AppendSlMemoryRoots(HashSet<SObject> roots)
        {
            if (roots == null) return;
            // Byte-stack object pool (PTR/STRING references)
            if (m_ObjPool != null)
            {
                foreach (var obj in m_ObjPool)
                {
                    if (obj != null)
                        roots.Add(obj);
                }
            }
#if DEBUG
            if (m_ValueStack != null)
            {
                int n = m_ValueIndex;
                for (int i = 0; i < n; i++)
                {
                    var v = m_ValueStack[i];
                    if (!v.isNull && v.sobject != null)
                        roots.Add(v.sobject);
                }
            }
#endif
            AppendRuntimeObjectRoots(roots, m_ArgumentRuntimeObjectArray);
            AppendRuntimeObjectRoots(roots, m_LocalVariableRuntimeObjectArray);
            AppendRuntimeObjectRoots(roots, m_ReturnRuntimeObjectArray);
        }

        private static void AppendRuntimeObjectRoots(HashSet<SObject> roots, RuntimeObject[]? arr)
        {
            if (arr == null) return;
            foreach (var ro in arr)
            {
                if (ro?.sobject != null)
                    roots.Add(ro.sobject);
            }
        }

        // =========================================================================
        // Byte-level eval stack (mirrors cvm vm_runtime.c)
        // All primitive values are encoded as raw bytes (1/2/4/8) just like the
        // C VM.  A parallel slot-kind array tracks the type of each logical
        // operand so that pops can widen / dispatch correctly.
        // =========================================================================

        private void ResetByteStack()
        {
            m_ByteSp = 0;
            m_StackSlotDepth = 0;
            m_ObjPool.Clear();
        }

        /** Mirror cvm vm_stack_slot_byte_len: byte size for a slot kind. */
        private static int ByteStackSlotByteLen(VMStackSlotKind kind)
        {
            switch (kind)
            {
                case VMStackSlotKind.INT8:
                case VMStackSlotKind.UINT8:
                    return 1;
                case VMStackSlotKind.INT16:
                case VMStackSlotKind.UINT16:
                    return 2;
                case VMStackSlotKind.INT32:
                case VMStackSlotKind.UINT32:
                case VMStackSlotKind.FLOAT32:
                    return 4;
                case VMStackSlotKind.INT64:
                case VMStackSlotKind.UINT64:
                case VMStackSlotKind.FLOAT64:
                    return 8;
                case VMStackSlotKind.PTR:
                case VMStackSlotKind.STRING:
                    return VM_PTR_SIZE;
                default:
                    return 0;
            }
        }

        /** Mirror cvm vm_stack_slot_try_push: push a slot-kind tag. */
        private bool ByteStackSlotTryPush(VMStackSlotKind kind)
        {
            if (m_StackSlotDepth >= m_StackSlotKind.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageError, "eval stack slot tag overflow depth={0}", m_StackSlotDepth);
                return false;
            }
            m_StackSlotKind[m_StackSlotDepth++] = (byte)kind;
            return true;
        }

        /** Mirror cvm vm_stack_pop_tags_for_bytes: pop slot tags for byte_count raw bytes. */
        private void ByteStackPopTagsForBytes(int byteCount)
        {
            int left = byteCount;
            while (left > 0 && m_StackSlotDepth > 0)
            {
                byte k = m_StackSlotKind[m_StackSlotDepth - 1];
                int bl = ByteStackSlotByteLen((VMStackSlotKind)k);
                if (bl == 0 || bl > left)
                {
                    m_StackSlotDepth = 0;
                    return;
                }
                m_StackSlotDepth--;
                left -= bl;
            }
            if (left != 0)
                m_StackSlotDepth = 0;
        }

        /** Mirror cvm vm_stack_top_n_slots_byte_sum: sum of byte lengths for top n slots. */
        private int ByteStackTopNSlotsByteSum(int nSlots)
        {
            if (nSlots == 0 || m_StackSlotDepth < nSlots)
                return 0;
            int sum = 0;
            for (int i = 0; i < nSlots; i++)
            {
                int bl = ByteStackSlotByteLen((VMStackSlotKind)m_StackSlotKind[m_StackSlotDepth - 1 - i]);
                if (bl == 0) return 0;
                sum += bl;
            }
            return sum;
        }

        // ---- Typed push functions (mirror cvm vm_stack_push_i32_slot / vm_eval_push_*) ----
        // Each writes raw bytes to m_ByteStack, advances m_ByteSp, and pushes a
        // slot-kind tag — exactly like the C VM.

        private unsafe void ByteStackPushI8(sbyte v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(sbyte*)p = v;
            m_ByteSp += 1;
            ByteStackSlotTryPush(VMStackSlotKind.INT8);
        }
        private unsafe void ByteStackPushU8(byte v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(byte*)p = v;
            m_ByteSp += 1;
            ByteStackSlotTryPush(VMStackSlotKind.UINT8);
        }
        private unsafe void ByteStackPushI16(short v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(short*)p = v;
            m_ByteSp += 2;
            ByteStackSlotTryPush(VMStackSlotKind.INT16);
        }
        private unsafe void ByteStackPushU16(ushort v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(ushort*)p = v;
            m_ByteSp += 2;
            ByteStackSlotTryPush(VMStackSlotKind.UINT16);
        }
        private unsafe void ByteStackPushI32(int v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(int*)p = v;
            m_ByteSp += 4;
            ByteStackSlotTryPush(VMStackSlotKind.INT32);
        }
        private unsafe void ByteStackPushU32(uint v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(uint*)p = v;
            m_ByteSp += 4;
            ByteStackSlotTryPush(VMStackSlotKind.UINT32);
        }
        private unsafe void ByteStackPushI64(long v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(long*)p = v;
            m_ByteSp += 8;
            ByteStackSlotTryPush(VMStackSlotKind.INT64);
        }
        private unsafe void ByteStackPushU64(ulong v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(ulong*)p = v;
            m_ByteSp += 8;
            ByteStackSlotTryPush(VMStackSlotKind.UINT64);
        }
        private unsafe void ByteStackPushF32(float v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(float*)p = v;
            m_ByteSp += 4;
            ByteStackSlotTryPush(VMStackSlotKind.FLOAT32);
        }
        private unsafe void ByteStackPushF64(double v)
        {
            fixed (byte* p = &m_ByteStack[m_ByteSp]) *(double*)p = v;
            m_ByteSp += 8;
            ByteStackSlotTryPush(VMStackSlotKind.FLOAT64);
        }
        private unsafe void ByteStackPushPtr(SObject? obj)
        {
            // Store the object in the pool and push the pool index as VM_PTR_SIZE bytes,
            // mirroring cvm vm_eval_push_ptr which stores a void* pointer.
            int idx = m_ObjPool.Count;
            m_ObjPool.Add(obj);
            fixed (byte* p = &m_ByteStack[m_ByteSp])
            {
                if (VM_PTR_SIZE == 8) *(long*)p = idx;
                else if (VM_PTR_SIZE == 4) *(int*)p = idx;
                else *(short*)p = (short)idx;
            }
            m_ByteSp += VM_PTR_SIZE;
            ByteStackSlotTryPush(VMStackSlotKind.PTR);
        }
        private void ByteStackPushString(string s)
        {
            // Mirror cvm vm_eval_push_string: wrap in an object and push as PTR.
            var so = new StringObject(s);
            ByteStackPushPtr(so);
        }
        private void ByteStackPushNull()
        {
            // Mirror cvm LoadConstNull: push NULL as a PTR slot.
            ByteStackPushPtr(null);
        }

        // ---- Typed pop functions (mirror cvm vm_try_pop_i32 / vm_pop_stack_top_to_vmvalue) ----

        /** Mirror cvm vm_try_pop_i32: pop with widening to int32. */
        private unsafe bool ByteStackTryPopI32(out int outValue)
        {
            outValue = 0;
            if (m_StackSlotDepth > 0)
            {
                byte k = m_StackSlotKind[m_StackSlotDepth - 1];
                var kind = (VMStackSlotKind)k;
                if (kind == VMStackSlotKind.PTR || kind == VMStackSlotKind.STRING)
                    return false;
                int bl = ByteStackSlotByteLen(kind);
                if (bl == 0 || m_ByteSp < bl)
                    return false;
                m_ByteSp -= bl;
                m_StackSlotDepth--;
                fixed (byte* p = &m_ByteStack[m_ByteSp])
                {
                    switch (kind)
                    {
                        case VMStackSlotKind.INT32:
                        case VMStackSlotKind.UINT32:
                            outValue = *(int*)p;
                            return true;
                        case VMStackSlotKind.INT8:
                            outValue = *(sbyte*)p;
                            return true;
                        case VMStackSlotKind.UINT8:
                            outValue = *(byte*)p;
                            return true;
                        case VMStackSlotKind.INT16:
                            outValue = *(short*)p;
                            return true;
                        case VMStackSlotKind.UINT16:
                            outValue = *(ushort*)p;
                            return true;
                        case VMStackSlotKind.INT64:
                            outValue = (int)*(long*)p;
                            return true;
                        case VMStackSlotKind.UINT64:
                            outValue = (int)*(ulong*)p;
                            return true;
                        case VMStackSlotKind.FLOAT32:
                            outValue = (int)*(float*)p;
                            return true;
                        case VMStackSlotKind.FLOAT64:
                            outValue = (int)*(double*)p;
                            return true;
                        default:
                            m_StackSlotDepth++;
                            m_ByteSp += bl;
                            return false;
                    }
                }
            }
            // Legacy 4-byte pop (no slot tracking)
            if (m_ByteSp < 4)
                return false;
            m_ByteSp -= 4;
            fixed (byte* p = &m_ByteStack[m_ByteSp])
                outValue = *(int*)p;
            return true;
        }

        /** Mirror cvm vm_try_pop_object: pop a PTR slot as SObject. */
        private unsafe bool ByteStackTryPopObject(out SObject? outObject)
        {
            outObject = null;
            if (m_StackSlotDepth > 0)
            {
                byte k = m_StackSlotKind[m_StackSlotDepth - 1];
                var kind = (VMStackSlotKind)k;
                if (kind != VMStackSlotKind.PTR && kind != VMStackSlotKind.STRING)
                    return false;
                if (m_ByteSp < VM_PTR_SIZE)
                    return false;
                m_ByteSp -= VM_PTR_SIZE;
                m_StackSlotDepth--;
                long idx;
                fixed (byte* p = &m_ByteStack[m_ByteSp])
                {
                    if (VM_PTR_SIZE == 8) idx = *(long*)p;
                    else if (VM_PTR_SIZE == 4) idx = *(int*)p;
                    else idx = *(short*)p;
                }
                if (idx >= 0 && idx < m_ObjPool.Count)
                    outObject = m_ObjPool[(int)idx];
                return true;
            }
            return false;
        }

        /** Mirror cvm vm_pop_stack_top_to_vmvalue: pop with full type dispatch. */
        private unsafe bool ByteStackPopToRuntimeValue(out RuntimeValue outVal)
        {
            outVal = default;
            if (m_StackSlotDepth > 0)
            {
                byte k = m_StackSlotKind[m_StackSlotDepth - 1];
                var kind = (VMStackSlotKind)k;
                switch (kind)
                {
                    case VMStackSlotKind.PTR:
                    case VMStackSlotKind.STRING:
                        if (ByteStackTryPopObject(out var obj))
                        {
                            if (obj == null)
                                outVal.SetNull();
                            else
                                outVal.SetValueBySObject(obj);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.INT8:
                        if (ByteStackTryPopI32(out int i8v))
                        {
                            outVal.SetInt8Value((sbyte)i8v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.UINT8:
                        if (ByteStackTryPopI32(out int u8v))
                        {
                            outVal.SetUInt8Value((byte)u8v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.INT16:
                        if (ByteStackTryPopI32(out int i16v))
                        {
                            outVal.SetInt16Value((short)i16v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.UINT16:
                        if (ByteStackTryPopI32(out int u16v))
                        {
                            outVal.SetUInt16Value((ushort)u16v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.INT32:
                        if (ByteStackTryPopI32(out int i32v))
                        {
                            outVal.SetInt32Value(i32v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.UINT32:
                        if (ByteStackTryPopI32(out int ui32v))
                        {
                            outVal.SetUInt32Value((uint)ui32v);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.INT64:
                        if (m_ByteSp >= 8)
                        {
                            m_ByteSp -= 8;
                            m_StackSlotDepth--;
                            fixed (byte* p = &m_ByteStack[m_ByteSp])
                                outVal.SetInt64Value(*(long*)p);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.UINT64:
                        if (m_ByteSp >= 8)
                        {
                            m_ByteSp -= 8;
                            m_StackSlotDepth--;
                            fixed (byte* p = &m_ByteStack[m_ByteSp])
                                outVal.SetUInt64Value(*(ulong*)p);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.FLOAT32:
                        if (m_ByteSp >= 4)
                        {
                            m_ByteSp -= 4;
                            m_StackSlotDepth--;
                            fixed (byte* p = &m_ByteStack[m_ByteSp])
                                outVal.SetFloatValue(*(float*)p);
                            return true;
                        }
                        return false;
                    case VMStackSlotKind.FLOAT64:
                        if (m_ByteSp >= 8)
                        {
                            m_ByteSp -= 8;
                            m_StackSlotDepth--;
                            fixed (byte* p = &m_ByteStack[m_ByteSp])
                                outVal.SetDoubleValue(*(double*)p);
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
            // Legacy 4-byte pop
            if (ByteStackTryPopI32(out int legacy))
            {
                outVal.SetInt32Value(legacy);
                return true;
            }
            return false;
        }

        /** Peek at the top slot as RuntimeValue WITHOUT popping. slotFromTop=1 means top. */
        private unsafe bool ByteStackTryPeekRuntimeValue(int slotFromTop, out RuntimeValue outVal)
        {
            outVal = default;
            if (slotFromTop <= 0 || m_StackSlotDepth < slotFromTop)
                return false;
            byte k = m_StackSlotKind[m_StackSlotDepth - slotFromTop];
            var kind = (VMStackSlotKind)k;
            // Compute byte offset of this slot's data
            int byteOff = m_ByteSp;
            for (int i = 1; i < slotFromTop; i++)
                byteOff -= ByteStackSlotByteLen((VMStackSlotKind)m_StackSlotKind[m_StackSlotDepth - i]);
            int bl = ByteStackSlotByteLen(kind);
            byteOff -= bl;
            if (byteOff < 0)
                return false;
            fixed (byte* p = &m_ByteStack[byteOff])
            {
                switch (kind)
                {
                    case VMStackSlotKind.PTR:
                    case VMStackSlotKind.STRING:
                        {
                            long idx;
                            if (VM_PTR_SIZE == 8) idx = *(long*)p;
                            else if (VM_PTR_SIZE == 4) idx = *(int*)p;
                            else idx = *(short*)p;
                            if (idx >= 0 && idx < m_ObjPool.Count)
                            {
                                var obj = m_ObjPool[(int)idx];
                                if (obj == null)
                                    outVal.SetNull();
                                else
                                    outVal.SetValueBySObject(obj);
                            }
                            else
                                outVal.SetNull();
                            return true;
                        }
                    case VMStackSlotKind.INT8:   outVal.SetInt8Value(*(sbyte*)p); return true;
                    case VMStackSlotKind.UINT8:  outVal.SetUInt8Value(*(byte*)p); return true;
                    case VMStackSlotKind.INT16:  outVal.SetInt16Value(*(short*)p); return true;
                    case VMStackSlotKind.UINT16: outVal.SetUInt16Value(*(ushort*)p); return true;
                    case VMStackSlotKind.INT32:  outVal.SetInt32Value(*(int*)p); return true;
                    case VMStackSlotKind.UINT32: outVal.SetUInt32Value(*(uint*)p); return true;
                    case VMStackSlotKind.INT64:  outVal.SetInt64Value(*(long*)p); return true;
                    case VMStackSlotKind.UINT64: outVal.SetUInt64Value(*(ulong*)p); return true;
                    case VMStackSlotKind.FLOAT32: outVal.SetFloatValue(*(float*)p); return true;
                    case VMStackSlotKind.FLOAT64: outVal.SetDoubleValue(*(double*)p); return true;
                    default: return false;
                }
            }
        }

        /** Replace the top slot's value in-place (for Convert / Neg / Not etc.). */
        private unsafe bool ByteStackReplaceTop(RuntimeValue v)
        {
            if (m_StackSlotDepth <= 0)
                return false;
            // Pop old value, push new value
            ByteStackPopToRuntimeValue(out _);
            // Push new value by eType
            if (v.isNull || v.eType == EVMType.Null) { ByteStackPushNull(); return true; }
            switch (v.eType)
            {
                case EVMType.Boolean: ByteStackPushI32(v.uint8Value != 0 ? 1 : 0); return true;
                case EVMType.UInt8:   ByteStackPushU8(v.uint8Value); return true;
                case EVMType.Int8:    ByteStackPushI8(v.int8Value); return true;
                case EVMType.Int16:   ByteStackPushI16(v.int16Value); return true;
                case EVMType.UInt16:  ByteStackPushU16(v.uint16Value); return true;
                case EVMType.Int32:   ByteStackPushI32(v.int32Value); return true;
                case EVMType.UInt32:  ByteStackPushU32(v.uint32Value); return true;
                case EVMType.Int64:   ByteStackPushI64(v.int64Value); return true;
                case EVMType.UInt64:  ByteStackPushU64(v.uint64Value); return true;
                case EVMType.Float32: ByteStackPushF32(v.float32Value); return true;
                case EVMType.Float64: ByteStackPushF64(v.float64Value); return true;
                case EVMType.String:  ByteStackPushPtr(v.sobject); return true;
                default:              ByteStackPushPtr(v.sobject); return true;
            }
        }

        /** Discard n slots from the top of the byte stack. */
        private void ByteStackDiscardN(int nSlots)
        {
            for (int i = 0; i < nSlots && m_StackSlotDepth > 0; i++)
            {
                byte k = m_StackSlotKind[m_StackSlotDepth - 1];
                int bl = ByteStackSlotByteLen((VMStackSlotKind)k);
                m_ByteSp -= bl;
                m_StackSlotDepth--;
            }
        }

        /** Push a RuntimeValue to the byte stack, dispatching by eType. */
        private void ByteStackPushByEType(ref RuntimeValue v)
        {
            if (v.isNull || v.eType == EVMType.Null) { ByteStackPushNull(); return; }
            switch (v.eType)
            {
                case EVMType.Boolean: ByteStackPushI32(v.uint8Value != 0 ? 1 : 0); break;
                case EVMType.UInt8:   ByteStackPushU8(v.uint8Value); break;
                case EVMType.Int8:    ByteStackPushI8(v.int8Value); break;
                case EVMType.Int16:   ByteStackPushI16(v.int16Value); break;
                case EVMType.UInt16:  ByteStackPushU16(v.uint16Value); break;
                case EVMType.Int32:   ByteStackPushI32(v.int32Value); break;
                case EVMType.UInt32:  ByteStackPushU32(v.uint32Value); break;
                case EVMType.Int64:   ByteStackPushI64(v.int64Value); break;
                case EVMType.UInt64:  ByteStackPushU64(v.uint64Value); break;
                case EVMType.Float32: ByteStackPushF32(v.float32Value); break;
                case EVMType.Float64: ByteStackPushF64(v.float64Value); break;
                case EVMType.String:  ByteStackPushPtr(v.sobject); break;
                default:              ByteStackPushPtr(v.sobject); break;
            }
        }

        /** Current logical slot depth (number of operands on the eval stack). */
        private int ByteStackSlotDepthCount => m_StackSlotDepth;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushSValueSynced(in RuntimeValue v)
        {
            // Push to byte stack (primary)
            if (v.isNull || v.eType == EVMType.Null) { ByteStackPushNull(); }
            else
            {
                switch (v.eType)
                {
                    case EVMType.Boolean: ByteStackPushI32(v.uint8Value != 0 ? 1 : 0); break;
                    case EVMType.UInt8:   ByteStackPushU8(v.uint8Value); break;
                    case EVMType.Int8:    ByteStackPushI8(v.int8Value); break;
                    case EVMType.Int16:   ByteStackPushI16(v.int16Value); break;
                    case EVMType.UInt16:  ByteStackPushU16(v.uint16Value); break;
                    case EVMType.Int32:   ByteStackPushI32(v.int32Value); break;
                    case EVMType.UInt32:  ByteStackPushU32(v.uint32Value); break;
                    case EVMType.Int64:   ByteStackPushI64(v.int64Value); break;
                    case EVMType.UInt64:  ByteStackPushU64(v.uint64Value); break;
                    case EVMType.Float32: ByteStackPushF32(v.float32Value); break;
                    case EVMType.Float64: ByteStackPushF64(v.float64Value); break;
                    case EVMType.String:  ByteStackPushPtr(v.sobject); break;
                    default:              ByteStackPushPtr(v.sobject); break;
                }
            }
#if DEBUG
            // Debug mirror
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++] = v;
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue " + v.ToString());
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue " + v.ToString());
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPushStackSlot(out int slotIndex)
        {
#if DEBUG
            if (m_ValueIndex >= m_ValueStack.Length)
            {
                slotIndex = -1;
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"SVM Error: Value stack overflow, current index={m_ValueIndex}, stack length={m_ValueStack.Length}");
                return false;
            }
            slotIndex = m_ValueIndex++;
            return true;
#else
            slotIndex = -1;
            return false;
#endif
        }

        // ---- Typed stack push helpers ----
        // Mirror cvm (vm_runtime.c) vm_stack_push_i32_slot / vm_eval_push_*.
        // Primary: write raw bytes to m_ByteStack + slot-kind tag.
        // Debug mirror (#if DEBUG): also write to m_ValueStack for inspection.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushNullSlot()
        {
            ByteStackPushNull();
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetNull();
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <null>");
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <null>");
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushBoolSlot(bool v)
        {
            ByteStackPushI32(v ? 1 : 0);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetBoolValue(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <bool> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <bool> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushI8Slot(sbyte v)
        {
            ByteStackPushI8(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetInt8Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <i8> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <i8> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushU8Slot(byte v)
        {
            ByteStackPushU8(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetUInt8Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <u8> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <u8> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushI16Slot(short v)
        {
            ByteStackPushI16(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetInt16Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <i16> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <i16> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushU16Slot(ushort v)
        {
            ByteStackPushU16(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetUInt16Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <u16> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <u16> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushI32Slot(int v)
        {
            ByteStackPushI32(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetInt32Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <i32> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <i32> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushU32Slot(uint v)
        {
            ByteStackPushU32(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetUInt32Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <u32> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <u32> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushI64Slot(long v)
        {
            ByteStackPushI64(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetInt64Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <i64> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <i64> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushU64Slot(ulong v)
        {
            ByteStackPushU64(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetUInt64Value(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <u64> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <u64> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushF32Slot(float v)
        {
            ByteStackPushF32(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetFloatValue(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <f32> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <f32> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushF64Slot(double v)
        {
            ByteStackPushF64(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetDoubleValue(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <f64> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <f64> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushStringSlot(string v)
        {
            ByteStackPushString(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                m_ValueStack[m_ValueIndex++].SetStringValue(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <string> " + v);
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <string> " + v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushPtrSlot(SObject? v)
        {
            ByteStackPushPtr(v);
#if DEBUG
            if (m_ValueIndex < m_ValueStack.Length)
            {
                ref var slot = ref m_ValueStack[m_ValueIndex++];
                if (v == null)
                    slot.SetNull();
                else
                    slot.SetValueBySObject(v);
                Log.AddVM(LID.ShowMessageInfo, "push RuntimeValue <ptr> " + (v == null ? "<null>" : v.ToString()));
            }
            else
                Log.AddVM(LID.ShowMessageAssert, "push override RuntimeValue <ptr> " + (v == null ? "<null>" : v.ToString()));
#endif
        }
        public static RuntimeType GetRuntimeTypeByDefType(RuntimeDefType irmt, RuntimeClass curIRMc, List<RuntimeType> __rtList, bool isAdd = false)
        {
            if (irmt.templateIndex != -1)
            {
                if (irmt.ownerRuntimeClass == curIRMc || curIRMc.name == "Core.Object")
                {
                    if (__rtList.Count <= irmt.templateIndex)
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "template index is out of range");
                        return null;
                    }
                    return __rtList[irmt.templateIndex];
                }
                else
                {
                    var mt = curIRMc.GetRuntimeDefTypeByTemplateAndClassRelation(irmt.ownerRuntimeClass, irmt.templateIndex);
                    if (mt == null) return null;

                    return GetRuntimeTypeByDefType(mt, curIRMc, __rtList, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetRuntimeTypeByDefType(irmt.runtimeDefTypeList[i], curIRMc, __rtList, isAdd);
                        rtList.Add(crt);
                    }
                }
                RuntimeType rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
                }
                return rt;
            }
        }
        public void AddReturnObjectArray(RuntimeObject[] sobjs)
        {
            for (int i = 0; i < sobjs.Length; i++)
            {
                if (sobjs[i].runtimeType != RuntimeTypeManager.voidRuntimeType)
                {
                    var obj = sobjs[i];
                    if (obj == null)
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "object is null");
                        return;
                    }
                    if (obj.eType == EVMType.Null)
                    {
                        ByteStackPushNull();
#if DEBUG
                        if (m_ValueIndex < m_ValueStack.Length)
                            m_ValueStack[m_ValueIndex++].SetNull();
#endif
                        continue;
                    }
                    // Read the RuntimeValue from the runtime object, then push to
                    // byte stack by eType (handles primitives AND objects correctly).
                    var sval = default(RuntimeValue);
                    obj.SetSValueByRuntimeObjct(ref sval);
                    ByteStackPushByEType(ref sval);
#if DEBUG
                    if (m_ValueIndex < m_ValueStack.Length)
                    {
                        m_ValueStack[m_ValueIndex++] = sval;
                    }
#endif
                }
            }
        }
        public RuntimeValue GetCurrentIndexValue(int index)
        {
#if DEBUG
            return m_ValueStack[index];
#else
            return default;
#endif
        }
        public void SetCurrentRuntimeType(RuntimeType rt)
        {
            m_CurrentRuntimeType = rt;
        }
        public void SetNewObject()
        {
            var topRt = CLRVM.topCLRRuntime;
            if (topRt.ByteStackSlotDepthCount > 0)
            {
                topRt.ByteStackTryPeekRuntimeValue(1, out var sval);
                ByteStackPushByEType(ref sval);
                if (sval.sobject != null)
                {
                    m_CurrentRuntimeType = sval.sobject.runtimeType;
                }
#if DEBUG
                m_ValueStack[m_ValueIndex++] = sval;
#endif
            }
        }
        public void ClearNewObject()
        {
            m_CurrentRuntimeType = null;
        }
        public void Run(bool disStackCount)
        {
            if (m_InstructionList == null || m_InstructionList.Length == 0) return;

            string funName = id;

            string pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddProjectLog(LID.ShowMessageInfo, pushChar + "[VMRuntime] [Push] Method: [" + funName + "]");
            m_Level++;

            var topClrRuntime = CLRVM.topCLRRuntime;
            for (int i = 0; i < m_ArgumentRuntimeObjectArray.Length; i++)
            {
                RuntimeValue sval;
                if (disStackCount)
                {
                    topClrRuntime.ByteStackPopToRuntimeValue(out sval);
#if DEBUG
                    topClrRuntime.m_ValueIndex--;
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex);
#endif
                }
                else
                {
                    topClrRuntime.ByteStackTryPeekRuntimeValue(i + 1, out sval);
#if DEBUG
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex - 1 - i);
#endif
                }
                uint index = (uint)(m_ArgumentRuntimeObjectArray.Length - i - 1);
                m_ArgumentRuntimeObjectArray[index].SetSObjectBySValue(ref sval);
            }

            m_ExecuteIndex = 0;
            m_ExecuteCount = (ushort)m_InstructionList.Length;
            while (m_ExecuteIndex < m_ExecuteCount)
            {
                var iri = m_InstructionList[m_ExecuteIndex];
                try
                {
                    RunInstruction(iri);
                }
                catch (Exception ex)
                {
                    // SvmNullNumericArithmeticException：LID.VMOperatorNotShouldHaveNull 已在 SValue（比�?算术）中输出，这里不再打日志
                    if (ex is SvmNullNumericArithmeticException) throw;
                    // CompilationAbortException：由 Log 系统统一决定“阻�?取消执行”，此处不重复包装日志�?
                    if (ex is CompilationAbortException) throw;
                    var loc2 = iri?.debugInfo?.FormatDiagnosticLine();
                    var detail = string.IsNullOrEmpty(loc2)
                        ? $"VM instruction fault: op={iri?.opCode} ip={m_ExecuteIndex} id={iri?.id} index={iri?.index}"
                        : $"VM instruction fault: op={iri?.opCode} ip={m_ExecuteIndex} id={iri?.id} index={iri?.index} at {loc2}";
                    if (iri?.debugInfo != null)
                        Log.AddRuntimeLog(LID.ShowMessageError, iri.debugInfo, detail + " �?" + ex.Message);
                    else
                        Log.AddRuntimeLog(LID.ShowMessageError, detail + " �?" + ex);
                    throw;
                }
                m_ExecuteIndex++;
            }


            m_Level--;
            pushChar = "";
            for (int i = 0; i < m_Level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddRuntimeLog(LID.ShowMessageInfo, pushChar + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        private static object? ConvertInvokeArg(object? source, Type targetType)
        {
            if (source == null) return null;

            // BridgeObject 鍙傛暟钀藉湴锛坙egacy bridge 璺緞涔熼渶瑕侊�?
            if (source is ClassObject co && IsBridgeObjectRuntime(co.runtimeClass))
            {
                if (TryExtractBridgeObjectPayload(co, out var payloadObj))
                {
                    if (payloadObj == null) return null;
                    if (targetType == typeof(object)) return payloadObj;
                    if (targetType == typeof(string))
                        return payloadObj is string s ? s : payloadObj.ToString();
                    if (targetType.IsInstanceOfType(payloadObj)) return payloadObj;

                    if (payloadObj is string payloadStr)
                    {
                        if (targetType == typeof(int) && int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return i;
                        if (targetType == typeof(long) && long.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                            return l;
                        if (targetType == typeof(float) && float.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f))
                            return f;
                        if (targetType == typeof(double) && double.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                            return d;
                        if (targetType == typeof(bool))
                        {
                            if (bool.TryParse(payloadStr, out var b)) return b;
                            if (int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bi)) return bi != 0;
                        }
                    }

                    if (targetType.IsEnum)
                    {
                        try { return Enum.ToObject(targetType, payloadObj); } catch { /* ignore */ }
                    }
                    try { return Convert.ChangeType(payloadObj, targetType); } catch { /* ignore */ }
                }
            }

            if (targetType == typeof(object) || targetType.IsInstanceOfType(source)) return source;
            if (targetType.IsEnum) return Enum.ToObject(targetType, source);
            return Convert.ChangeType(source, targetType);
        }

        private static bool IsBridgeObjectRuntime(RuntimeClass? runtimeClass)
        {
            if (runtimeClass == null) return false;
            var n = runtimeClass.name ?? string.Empty;
            return n.EndsWith("BridgeObject", StringComparison.Ordinal) || n.Contains(".BridgeObject", StringComparison.Ordinal);
        }

        private static bool TryExtractBridgeObjectPayload(ClassObject co, out object? payloadObj)
        {
            payloadObj = null;
            var rc = co.runtimeClass;
            if (rc == null) return false;

            var vars = rc.nonStaticIRMetaVariableList;
            if (vars == null || vars.Count == 0) return false;

            int index = -1;
            for (int i = 0; i < vars.Count; i++)
            {
                var vn = vars[i]?.name ?? string.Empty;
                if (string.Equals(vn, "valuetype", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(vn, "_valuetype", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) index = 0;

            var sv = default(RuntimeValue);
            co.GetMemberVariableSValue(index, ref sv);
            if (sv.int32Value >= 0 && sv.int32Value <= 5)
            {
                var realval = default(RuntimeValue);
                co.GetMemberVariableSValue(sv.int32Value, ref realval);
                payloadObj = realval.GetValueObject();
                return true;
            }
            else
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "array is index < 5");
                return false;
            }
        }

        private static object? NormalizeLegacyBridgeArg(ref RuntimeValue sv)
        {
            var raw = sv.GetValueObject();
            // Use object-target conversion so BridgeObject payload is extracted to CLR-friendly value.
            return ConvertInvokeArg(raw, typeof(object));
        }

        private static int FindBridgeMemberIndexByName(RuntimeClass? rc, string memberName)
        {
            var list = rc?.nonStaticIRMetaVariableList;
            if (list == null) return -1;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i]?.name ?? string.Empty;
                if (string.Equals(n, memberName, StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith("." + memberName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool TryStoreLegacyReturnToBridgeObject(RuntimeValue[] values, object? retObjValue)
        {
            if (values == null || values.Length < 4 || retObjValue == null) return false;

            var retObjSv = values[3];
            if (retObjSv.eType != EVMType.Class || retObjSv.sobject is not ClassObject retBridge) return false;
            if (!IsBridgeObjectRuntime(retBridge.runtimeClass)) return false;

            int idxBool = FindBridgeMemberIndexByName(retBridge.runtimeClass, "boolvalue");
            int idxI32 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "int32value");
            int idxI64 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "int64value");
            int idxF64 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "float64value");
            int idxStr = FindBridgeMemberIndexByName(retBridge.runtimeClass, "stringvalue");
            int idxType = FindBridgeMemberIndexByName(retBridge.runtimeClass, "valuetype");

            var outSv = default(RuntimeValue);
            int outTypeCode = -1;
            switch (retObjValue)
            {
                case bool b:
                    outSv.SetBoolValue(b);
                    outTypeCode = 0;
                    if (idxBool >= 0) retBridge.SetMemberVariableSValue(idxBool, outSv);
                    break;
                case byte v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case sbyte v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case short v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case ushort v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case int v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case uint v:
                    outSv.SetInt64Value(v);
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case long v:
                    outSv.SetInt64Value(v);
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case ulong v:
                    outSv.SetInt64Value(unchecked((long)v));
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case float v:
                    outSv.SetDoubleValue(v);
                    outTypeCode = 2;
                    if (idxF64 >= 0) retBridge.SetMemberVariableSValue(idxF64, outSv);
                    break;
                case double v:
                    outSv.SetDoubleValue(v);
                    outTypeCode = 2;
                    if (idxF64 >= 0) retBridge.SetMemberVariableSValue(idxF64, outSv);
                    break;
                case string s:
                    outSv.SetStringValue(s);
                    outTypeCode = 3;
                    if (idxStr >= 0) retBridge.SetMemberVariableSValue(idxStr, outSv);
                    break;
                default:
                    outSv.SetStringValue(retObjValue.ToString() ?? string.Empty);
                    outTypeCode = 3;
                    if (idxStr >= 0) retBridge.SetMemberVariableSValue(idxStr, outSv);
                    break;
            }

            if (idxType >= 0 && outTypeCode >= 0)
            {
                var typeSv = default(RuntimeValue);
                typeSv.SetInt32Value(outTypeCode);
                retBridge.SetMemberVariableSValue(idxType, typeSv);
            }

            return true;
        }

        private static bool TryBuildInvokeArgsForMethod(MethodInfo mi, object[] argsClr, out object?[] invokeArgs, out int score)
        {
            score = 0;
            var pars = mi.GetParameters();
            invokeArgs = new object?[pars.Length];
            if (pars.Length != argsClr.Length) return false;

            for (int i = 0; i < pars.Length; i++)
            {
                var pType = pars[i].ParameterType;
                var raw = argsClr[i];

                if (raw == null)
                {
                    bool canNull = !pType.IsValueType || Nullable.GetUnderlyingType(pType) != null;
                    if (!canNull) return false;
                    invokeArgs[i] = null;
                    score += 1;
                    continue;
                }

                var rawType = raw.GetType();
                if (rawType == pType)
                {
                    invokeArgs[i] = raw;
                    score += 4;
                    continue;
                }
                if (pType.IsAssignableFrom(rawType))
                {
                    invokeArgs[i] = raw;
                    score += 3;
                    continue;
                }

                try
                {
                    var converted = ConvertInvokeArg(raw, pType);
                    if (converted == null)
                    {
                        bool canNull = !pType.IsValueType || Nullable.GetUnderlyingType(pType) != null;
                        if (!canNull) return false;
                        invokeArgs[i] = null;
                        score += 1;
                        continue;
                    }

                    if (pType.IsInstanceOfType(converted) || converted.GetType() == pType)
                    {
                        invokeArgs[i] = converted;
                        score += 2;
                        continue;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
        RuntimeType? GetRuntimeTypeByInstruction(Instruction iri)
        {
            if (iri.Payload == null)
            {
                return null;
            }

            string payload = Encoding.UTF8.GetString(iri.Payload);
            if (payload == "self")
            {
                return m_CurrentRuntimeType;
            }
            var mt = TryGetInstructionRuntimeDefType(iri);

            if (mt != null)
            {
                RuntimeClass rc = mt.ownerRuntimeClass;
                if (m_CurrentRuntimeType != null)
                {
                    rc = m_CurrentRuntimeType.runtimeClass;
                }
                return GetRuntimeTypeByDefType(mt, rc, m_RuntimeTypeList, true);
            }
            return null;
        }

        private static RuntimeDefType? TryGetInstructionRuntimeDefType(Instruction iri)
        {
            if (iri == null) return null;
            //if (iri.opValue is RuntimeDefType direct) return direct;

            var resolved = SLRuntimeModuleRegistry.TryResolveRuntimeDefTypeFromInstruction(iri.opValue, iri.Payload);
            if (resolved != null)
            {
                iri.opValue = resolved;
            }
            return resolved;
        }

        internal bool TrySystemCallPopArgs(int paramCount, out RuntimeValue[] args)
        {
            args = null!;
            if (paramCount < 0) return false;
            if (ByteStackSlotDepthCount < paramCount) return false;
            args = new RuntimeValue[paramCount];
            for (int pi = paramCount - 1; pi >= 0; pi--)
            {
                ByteStackPopToRuntimeValue(out args[pi]);
#if DEBUG
                args[pi] = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
            }
            return true;
        }

        internal bool TrySystemCallPopDiscard(int discardCount)
        {
            if (discardCount < 0) return false;
            if (ByteStackSlotDepthCount < discardCount) return false;
            ByteStackDiscardN(discardCount);
#if DEBUG
            for (int pi = discardCount - 1; pi >= 0; pi--)
                _ = m_ValueStack[--m_ValueIndex];
#endif
            return true;
        }


        internal bool TryInvokeRegisteredBridgeByIndex(Instruction iri)
        {
            int bridgeIndex;
            if (iri.TryGetInt32(out var payloadIndex)) bridgeIndex = payloadIndex;
            else bridgeIndex = iri.index;

            if (!CSharpBridgeRegistry.TryResolve(bridgeIndex, out var model)) return false;
            if (!CSharpBridgeRegistry.TryBindMethod(model, out var methodInfo))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Bridge method bind failed, index={bridgeIndex}");
                return true;
            }

            var pars = methodInfo.GetParameters();
            if (ByteStackSlotDepthCount < pars.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Bridge stack underflow, need={pars.Length}, has={ByteStackSlotDepthCount}");
                return true;
            }

            var invokeArgs = new object?[pars.Length];
            for (int i = pars.Length - 1; i >= 0; i--)
            {
                ByteStackPopToRuntimeValue(out var sv);
#if DEBUG
                sv = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
                var raw = sv.ToClrObject(pars[i].ParameterType);
                invokeArgs[i] = ConvertInvokeArg(raw, pars[i].ParameterType);
            }

            if (!methodInfo.IsStatic)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Bridge instance methods are not supported");
                return true;
            }

            var ret = methodInfo.Invoke(null, invokeArgs);
            if (methodInfo.ReturnType != typeof(void))
            {
                var sv = RuntimeValue.FromClrObject(ret);
                PushSValueSynced(sv);
            }

            return true;
        }

        internal bool TryInvokeLegacyBridgeSignature(Instruction iri, string callName)
        {
            int paramCountLocal = iri.index;
            if (paramCountLocal <= 0) return false;

            var values = new RuntimeValue[paramCountLocal];
            for (int i = paramCountLocal - 1; i >= 0; i--)
            {
                if (ByteStackSlotDepthCount == 0)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName} stack underflow");
                    return true;
                }
                ByteStackPopToRuntimeValue(out values[i]);
#if DEBUG
                values[i] = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
            }

            string namespaceName = values.Length > 1 ? values[0].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string className = values.Length > 2 ? values[1].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string methodName = values.Length > 3 ? values[2].GetValueObject()?.ToString() ?? string.Empty : string.Empty;

            object[] argsClr = Array.Empty<object>();
            RuntimeValue paramArr;
            if (values.Length >= 5)
            {
                var arr = values[4];
                if (arr.eType == EVMType.Array && arr.sobject is ArrayObject aobj)
                {
                    int len = aobj.length;
                    argsClr = new object[len];
                    for (int j = 0; j < len; j++)
                    {
                        var temp = default(RuntimeValue);
                        aobj.LoadValue(j, ref temp);
                        argsClr[j] = NormalizeLegacyBridgeArg(ref temp);
                    }
                }
                else
                {
                    var single = values[4];
                    argsClr = new object[] { NormalizeLegacyBridgeArg(ref single) };
                }
            }

            MethodInfo? miFound = null;
            var methodId = CSharpBridgeRegistry.BuildMethodId(namespaceName, className, methodName);
            if (CSharpBridgeRegistry.TryResolve(methodId, out var model)
                && CSharpBridgeRegistry.TryBindMethod(model, out var regMi))
            {
                miFound = regMi;
            }

            if (miFound == null)
            {
                string typeFull = string.IsNullOrEmpty(namespaceName) ? className : (namespaceName + "." + className);
                Type t = Type.GetType(typeFull, throwOnError: false);
                if (t == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = a.GetType(typeFull, throwOnError: false);
                        if (t != null) break;
                    }
                }
                if (t == null)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: type not found " + typeFull);
                    return true;
                }

                int bestScore = int.MinValue;
                object?[]? bestInvokeArgs = null;
                foreach (var mi2 in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (mi2.Name != methodName) continue;
                    if (TryBuildInvokeArgsForMethod(mi2, argsClr, out var candidateArgs, out var score))
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            miFound = mi2;
                            bestInvokeArgs = candidateArgs;
                        }
                    }
                }

                if (miFound != null && bestInvokeArgs != null)
                {
                    if (!miFound.IsStatic)
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: instance methods not supported in bridge");
                        return true;
                    }
                    var ret2 = miFound.Invoke(null, bestInvokeArgs);
                    _ = TryStoreLegacyReturnToBridgeObject(values, ret2);
                    if (miFound.ReturnType != typeof(void))
                    {
                        var sv2 = RuntimeValue.FromClrObject(ret2);
                        //PushSValueSynced(sv2);
                    }
                    return true;
                }
            }

            if (miFound == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: method not found " + methodName);
                return true;
            }

            if (!TryBuildInvokeArgsForMethod(miFound, argsClr, out var invokeArgs, out _))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: method signature mismatch " + methodName);
                return true;
            }

            if (!miFound.IsStatic)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: instance methods not supported in bridge");
                return true;
            }

            var ret = miFound.Invoke(null, invokeArgs);
            _ = TryStoreLegacyReturnToBridgeObject(values, ret);
            if (miFound.ReturnType != typeof(void))
            {
                var sv = RuntimeValue.FromClrObject(ret);
                PushSValueSynced(sv);
            }

            return true;
        }

        public void RunInstruction(Instruction iri)
        {
            if (iri == null) return;

            int a2 = 20;

#if DEBUG
            int opcode = (int)iri.opCode;
            var idd = this.id;
#endif
            switch (iri.opCode)
            {
                case EIROpCode.Nop: break;
                case EIROpCode.LoadConstNull:
                    {
                        PushNullSlot();
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        if (iri.TryGetBoolean(out bool b))
                            PushBoolSlot(b);
                    }
                    break;
                case EIROpCode.LoadConstUInt8:
                    {
                        if (iri.TryGetUInt8(out byte cb))
                            PushU8Slot(cb);
                    }
                    break;
                case EIROpCode.LoadConstInt8:
                    {
                        if (iri.TryGetInt8(out sbyte sb))
                            PushI8Slot(sb);
                    }
                    break;
                case EIROpCode.LoadConstInt16:
                    {
                        if (iri.TryGetInt16(out short sv))
                            PushI16Slot(sv);
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        if (iri.TryGetUInt16(out ushort usv))
                            PushU16Slot(usv);
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        if (iri.TryGetInt32(out int i32))
                            PushI32Slot(i32);
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        if (iri.TryGetUInt32(out uint ui32))
                            PushU32Slot(ui32);
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        if (iri.TryGetInt64(out long l))
                            PushI64Slot(l);
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        if (iri.TryGetUInt64(out ulong ul))
                            PushU64Slot(ul);
                    }
                    break;
                case EIROpCode.LoadConstFloat32:
                    {
                        if (iri.TryGetFloat32(out float f))
                            PushF32Slot(f);
                    }
                    break;
                case EIROpCode.LoadConstFloat64:
                    {
                        if (iri.TryGetFloat64(out double d))
                            PushF64Slot(d);
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        var resolved = SLAssembly.TryGetConstString(iri.index) ?? string.Empty;
                        PushStringSlot(resolved);
                    }
                    break;
                case EIROpCode.LoadConstType:
                    {
                        var mdt = TryGetInstructionRuntimeDefType(iri);
                        if (mdt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, $"SVM Error: Value stack overflow, current index={ByteStackSlotDepthCount}, stack length={VM_STACK_SIZE}");
                            break;
                        }
                        var rt = GetRuntimeTypeByDefType(mdt, m_CurrentRuntimeType != null ? m_CurrentRuntimeType.runtimeClass : mdt.ownerRuntimeClass, m_CurrentRuntimeType.runtimeTemplateList, true);
                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, $"SVM Error: Value stack overflow, current index={ByteStackSlotDepthCount}, stack length={VM_STACK_SIZE}");
                            break;
                        }
                        var sobj = new TypeObject(rt);
                        sobj.CreateObject();
                        PushPtrSlot(sobj);
                    }
                    break;
                case EIROpCode.Convert_I8:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.UInt8);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.UInt8);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert i8.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt8");
#endif
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Int8);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Int8);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert si8.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int8");
#endif
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Int16);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Int16);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert i16.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int16");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.UInt16);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.UInt16);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert ui16.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt16");
#endif
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Int32);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Int32);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert i32.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int32");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.UInt32);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.UInt32);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert ui32.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt32");
#endif
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Int64);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Int64);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert i64.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int64");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.UInt64);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.UInt64);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert ui64.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt64");
#endif
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Float32);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Float32);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert r4.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Float32");
#endif
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.Float64);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.Float64);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert r8.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Float64");
#endif
                    }
                    break;
                case EIROpCode.Convert_ToString:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var top);
                            RuntimeValueMethod.ConvertByEType(ref top, EVMType.String);
                            ByteStackReplaceTop(top);
#if DEBUG
                            RuntimeValueMethod.ConvertByEType(ref m_ValueStack[m_ValueIndex - 1], EVMType.String);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "convert string.", 1, ByteStackSlotDepthCount);
                        }
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to String");
#endif
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        if ((uint)iri.index > m_ArgumentRuntimeObjectArray.Length)
                        {
                            Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "LoadArgument", iri.index);
                            return;
                        }
                        var temp = default(RuntimeValue);
                        m_ArgumentRuntimeObjectArray[(uint)iri.index].SetSValueByRuntimeObjct(ref temp);
                        PushSValueSynced(temp);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArgument: index=" + iri.index);
#endif
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        if ((uint)iri.index > m_LocalVariableRuntimeObjectArray.Length)
                        {
                            Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "LoadLocal", iri.index);
                            return;
                        }
                        var temp = default(RuntimeValue);
                        m_LocalVariableRuntimeObjectArray[(uint)iri.index].SetSValueByRuntimeObjct(ref temp);
                        PushSValueSynced(temp);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadLocal: index=" + iri.index);
#endif
                    }
                    break;
                case EIROpCode.LoadGlobal:
                    {
                        var temp = default(RuntimeValue);
                        CLRVM.LoadGlobalVariable((uint)iri.index, ref temp);
                        PushSValueSynced(temp);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadGlobal: index=" + iri.index);
#endif
                    }
                    break;
                case EIROpCode.StoreLocal:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            if ((uint)iri.index > m_LocalVariableRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "MethodId:" + id.ToString() + "SetLocalVariableSValue", (uint)iri.index);
                                return;
                            }
                            ByteStackPopToRuntimeValue(out var sv);
#if DEBUG
                            sv = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
                            m_LocalVariableRuntimeObjectArray[(uint)iri.index].SetSObjectBySValue(ref sv);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreLocal: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "StoreLocal stack underflow at index {iri.index}", 1, ByteStackSlotDepthCount);
                        }
                    }
                    break;
                case EIROpCode.StoreGlobal:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackPopToRuntimeValue(out var sv);
#if DEBUG
                            sv = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
                            CLRVM.StoreGlobalVariable((uint)iri.index, ref sv);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreGlobal: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "RuntimeVM LoadArrayIndex", 1, ByteStackSlotDepthCount);
                        }
                    }
                    break;
                case EIROpCode.StoreReturn:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            if ((uint)iri.index > m_ReturnRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "MethodId:" + id.ToString() + "StoreReturn", (uint)iri.index);
                                return;
                            }
                            ByteStackPopToRuntimeValue(out var sv);
#if DEBUG
                            sv = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
                            m_ReturnRuntimeObjectArray[(uint)iri.index].SetSObjectBySValue(ref sv);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreReturn: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + $"StoreReturn stack underflow at index {iri.index}");
                        }
                        m_ExecuteIndex = m_ExecuteCount;
                    }
                    break;
                case EIROpCode.LoadArrayIndex:
                    {
                        if (ByteStackSlotDepthCount > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var v);
                            if (v.sobject is ArrayObject ao)
                            {
                                ao.LoadValue(iri.index, ref v);
                                ByteStackReplaceTop(v);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArrayIndex: runtimeclass=" + ao.runtimeClass?.name
                                                + " objectId=" + ao.id + "index=" + iri.index);
                                ao.LoadValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
#endif
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", v.eType.ToString());
                            }
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndex:
                    {
                        int int1 = 1, int2 = 2;
                        byte flagByte = (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
                        if (iri.TryGetUInt8(out var bflag))
                        {
                            flagByte = bflag;
                        }
                        else if (iri.TryGetBoolean(out var oldFlag))
                        {
                            // backward-compat with old Front emitter: true means swapped order.
                            flagByte = oldFlag
                                ? (byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1
                                : (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
                        }

                        if (flagByte == (byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1)
                        {
                            int1 = 2;
                            int2 = 1;
                        }

                        if (m_StackSlotDepth >= Math.Max(int1, int2))
                        {
                            ByteStackTryPeekRuntimeValue(int1, out var sStore);
                            ByteStackTryPeekRuntimeValue(int2, out var RuntimeValue);
                            if (sStore.sobject is ArrayObject ao)
                            {
                                ao.StoreValue(iri.index, RuntimeValue);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreArrayIndex: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + iri.index);
#endif
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndex", sStore.eType.ToString());
                            }
                            ByteStackDiscardN(2);
#if DEBUG
                            m_ValueIndex -= 2;
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.LoadArrayIndexField:
                    {
                        if (m_StackSlotDepth > 1)
                        {
                            ByteStackPopToRuntimeValue(out var loadindex);
                            ByteStackPopToRuntimeValue(out var arrayref);

                            if (arrayref.sobject is ArrayObject ao)
                            {
                                if (RuntimeValueMethod.TryGetInt32FromRuntimeValue(loadindex, out var idx))
                                {
                                    ao.LoadValue(idx, ref arrayref);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArrayIndexField: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + idx);
#endif
                                }
                                else
                                {
                                    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndexField", loadindex.eType.ToString());
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndexField", arrayref.eType.ToString());
                            }
                            ByteStackPushByEType(ref arrayref);
#if DEBUG
                            m_ValueStack[m_ValueIndex - 2] = arrayref;
                            m_ValueIndex -= 1;
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndexField:
                    {
                        if (m_StackSlotDepth > 2)
                        {
                            ByteStackPopToRuntimeValue(out var storevalue);
                            ByteStackPopToRuntimeValue(out var loadindex);
                            ByteStackPopToRuntimeValue(out var arrayref);

                            if (arrayref.sobject is ArrayObject ao)
                            {
                                if (RuntimeValueMethod.TryGetInt32FromRuntimeValue(loadindex, out var idx))
                                {
                                    ao.StoreValue(idx, storevalue);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreArrayIndexField: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + idx);
#endif
                                }
                                else
                                {
                                    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndexField", loadindex.eType.ToString());
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndexField", arrayref.eType.ToString());
                            }
#if DEBUG
                            m_ValueIndex -= 3;
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.Dup:
                    {
                        int dupCount = 1;
                        if (iri.Payload != null && iri.Payload.Length >= 4)
                        {
                            dupCount = BitConverter.ToInt32(iri.Payload, 0);
                        }

                        if (m_StackSlotDepth >= dupCount)
                        {
                            var dups = new RuntimeValue[dupCount];
                            for (int i = 0; i < dupCount; i++)
                                ByteStackTryPeekRuntimeValue(dupCount - i, out dups[i]);
                            for (int i = 0; i < dupCount; i++)
                                PushSValueSynced(dups[i]);
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.Pop:
                    {
                        ushort dupCount = 1;
                        if (iri.Payload != null && iri.Payload.Length >= 4)
                        {
                            dupCount = (ushort)BitConverter.ToInt32(iri.Payload, 0);
                        }
                        if (m_StackSlotDepth >= dupCount)
                        {
                            ByteStackDiscardN(dupCount);
#if DEBUG
                            m_ValueIndex -= dupCount;
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.LoadStaticField:
                    {
                        var rt = GetRuntimeTypeByInstruction(iri);
                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }
                        var tempVal = default(RuntimeValue);
                        rt.GetStaticMemberVariableSValue(iri.index, ref tempVal);
                        ByteStackPushByEType(ref tempVal);
#if DEBUG
                        if (TryPushStackSlot(out int slot))
                            rt.GetStaticMemberVariableSValue(iri.index, ref m_ValueStack[slot]);
#endif
                    }
                    break;
                case EIROpCode.StoreStaticField:
                    {
                        var rt = GetRuntimeTypeByInstruction(iri);
                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }

                        if (m_StackSlotDepth > 0)
                        {
                            ByteStackPopToRuntimeValue(out var sv);
#if DEBUG
                            sv = m_ValueStack[--m_ValueIndex];
#endif
                            rt.SetStaticMemberVariableSValue(iri.index, ref sv);
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.LoadNotStaticField:
                    {
                        // expects instance on stack
                        if (m_StackSlotDepth > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var inst);
                            if (inst.eType == EVMType.Array
                                || inst.eType == EVMType.Class
                                || inst.eType == EVMType.Type
                                || inst.eType == EVMType.Object
                                || inst.eType == EVMType.Member)
                            {
                                ByteStackDiscardN(1);
#if DEBUG
                                --m_ValueIndex;
#endif
                                if (inst.sobject is ClassObject co)
                                {
                                    var tempVal = default(RuntimeValue);
                                    co.GetMemberVariableSValue(iri.index, ref tempVal);
                                    ByteStackPushByEType(ref tempVal);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadNotStaticField: runtimeclass=" + co.runtimeClass?.name
                                        + " objectId=" + co.id + "index=" + iri.index);
                                    if (TryPushStackSlot(out int slot))
                                        co.GetMemberVariableSValue(iri.index, ref m_ValueStack[slot]);
#endif
                                }
                                else
                                {
                                    PushNullSlot();
                                }
                            }
                            //else
                            //{
                            //    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndex", inst.eType.ToString());
                            //}
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.StoreNotStaticField2:
                    {
                        // expect value then instance on stack (value pushed last)
                        if (m_StackSlotDepth >= 2)
                        {
                            ByteStackPopToRuntimeValue(out var val);
                            ByteStackPopToRuntimeValue(out var inst);
#if DEBUG
                            val = m_ValueStack[--m_ValueIndex];
                            inst = m_ValueStack[--m_ValueIndex];
#endif
                            if (inst.sobject is ClassObject co)
                            {
                                co.SetMemberVariableSValue(iri.index, val);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField2: runtimeclass=" + co.runtimeClass?.name
                                            + " objectId=" + co.id + "index=" + iri.index);
#endif
                            }

                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;

                case EIROpCode.StoreNotStaticField1:
                    {
                        if (m_StackSlotDepth >= 2)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var val);
                            ByteStackTryPeekRuntimeValue(2, out var inst);
                            if (inst.sobject is ClassObject co)
                            {
                                co.SetMemberVariableSValue(iri.index, val);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + co.runtimeClass?.name
                                            + " objectId=" + co.id + "index=" + iri.index);
#endif
                            }
                            //else
                            //{
                            //    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "RuntimeVM StoreArrayIndex", inst.eType.ToString());
                            //}
                            ByteStackDiscardN(1);
#if DEBUG
                            m_ValueIndex -= 1;
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                //case EIROpCode.ClassInit:
                //    {
                //        var mdt = TryGetInstructionRuntimeDefType(iri);
                //        if (mdt != null)
                //        {
                //            var rt = GetRuntimeTypeByDefType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                //        }
                //    }
                //    break;
                case EIROpCode.NewObject:
                    {
                        if (iri.TryGetInt32(out int i32))
                        {
                            var rt = RuntimeTypeManager.GetRuntimeTypeById(i32);
                            // If runtime type not yet created, try to find corresponding RuntimeClass
                            // (possibly registered from package metadata) and dynamically register a RuntimeType for it.
                            if (rt == null)
                            {
                                // first try existing runtime class
                                var rc = RuntimeClassManager.GetRuntimeClassById(i32);
                                // if not found, attempt to resolve/create from loaded package metadata
                                if (rc == null)
                                {
                                    rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassById(i32);
                                }
                                if (rc != null)
                                {
                                    rt = RuntimeTypeManager.AddRuntimeTypeByClass(rc);
                                }
                            }
                            SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);


#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + sobj.id);
#endif

                            ObjectManager.RegisterObject(sobj);
                            ByteStackPushPtr(sobj);
#if DEBUG
                            m_ValueStack[m_ValueIndex++].SetRawSObject(sobj);
#endif

                            if (rt.runtimeClass.metaClassKind == 0)
                            {
                                var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                                if (irList.Count > 0)
                                {
                                    CLRVM.RunIRNewMethod($"__new_object__{rt.runtimeClass.name}", rt, irList, true);
                                }
                            }
                        }
                    }
                    break;
                case EIROpCode.NewTemplateObject:
                    {
                        var rt = GetRuntimeTypeByInstruction(iri);

                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }

                        SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                        ObjectManager.RegisterObject(sobj);
                        ByteStackPushPtr(sobj);
#if DEBUG
                        m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
#endif
                        var irc = rt.runtimeClass;

#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + sobj.id);
#endif

                        var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                        if (irList.Count > 0)
                        {
                            CLRVM.RunIRNewMethod($"__new_object__{rt.runtimeClass.name}", rt, irList, true);
                        }
                    }
                    break;
                case EIROpCode.NewArray:
                    {
                        // expects length on stack
                        var rt = GetRuntimeTypeByInstruction(iri);
                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }

                        if (m_StackSlotDepth > 0)
                        {
                            ByteStackTryPeekRuntimeValue(1, out var sval);
                            if (!RuntimeValueMethod.TryGetInt32FromRuntimeValue(sval, out var arrLength))
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "new array get RuntimeValue");
                                break;
                            }
                            if (arrLength < 0)
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() +
                                    $"不能将负值写入无符号类型: target= int32, source={sval.eType}");
                                return;
                            }

                            ArrayObject arr = new ArrayObject(rt, arrLength);
                            // NewArray opcode path should initialize only the backing storage.
                            // Full CreateObject() may require runtime member types that are not guaranteed ready.
                            arr.CreateObject();
                            ObjectManager.AddClassObject(arr);
                            var arrVal = default(RuntimeValue);
                            arrVal.SetArrayObject(arr);
                            ByteStackReplaceTop(arrVal);
#if DEBUG
                            m_ValueStack[m_ValueIndex - 1].SetArrayObject(arr);
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + arr.id);
#endif


                            //var sv = default(RuntimeValue);
                            //sv.SetSObject(arr);
                            //PushSValueSynced(sv);
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.Br:
                case EIROpCode.BrLabel:
                case EIROpCode.Break:
                case EIROpCode.Jmp:
                    {
                        m_ExecuteIndex = (ushort)iri.index;
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "jumpto->" + m_ExecuteIndex);
#endif
                    }
                    break;
                case EIROpCode.Label:
                    {
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "label");
#endif
                    }
                    break;
                case EIROpCode.BrFalse:
                    {
                        if (m_StackSlotDepth > 0)
                        {
                            ByteStackPopToRuntimeValue(out var cond);
#if DEBUG
                            cond = m_ValueStack[--m_ValueIndex];
#endif
                            if (cond.eType == EVMType.Boolean)
                            {
                                if (cond.int8Value != 1)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse nojump ");
#endif
                                }
                            }
                            else if (cond.sobject is BoolObject bl)
                            {
                                if (!bl.value)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse nojump ");
#endif
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "BrFalse");
                                break;
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "BrFalse", "");
                        }
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        if (m_StackSlotDepth > 0)
                        {
                            ByteStackPopToRuntimeValue(out var cond);
#if DEBUG
                            cond = m_ValueStack[--m_ValueIndex];
#endif
                            if (cond.eType == EVMType.Boolean)
                            {
                                if (cond.int8Value != 1)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue nojump ");
#endif
                                }
                            }
                            else if (cond.sobject is BoolObject bl)
                            {
                                if (bl.value)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue nojump ");
#endif
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "BrTrue");
                                break;
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "new array get RuntimeValue", "");
                        }
                    }
                    break;
                case EIROpCode.Switch:
                    {
                        if (m_StackSlotDepth >= 2)
                        {
                            ByteStackPopToRuntimeValue(out var right);
                            var left = right;
#if DEBUG
                            right = m_ValueStack[--m_ValueIndex];
                            left = m_ValueStack[m_ValueIndex];
#endif
                            //bool methodCall = false;
                            //RuntimeValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            //PushSValueSynced(left);
                            if (RuntimeValueMethod.TryGetInt32FromRuntimeValue(left, out var switchValue) && RuntimeValueMethod.TryGetInt32FromRuntimeValue(right, out _))
                            {
                                int caseCount = iri.opValue is int[] arr ? arr.Length : 0;
                                bool matched = false;
                                for (int i = 0; i < caseCount; i++)
                                {
                                    if (switchValue == (iri.opValue as int[])[i])
                                    {
                                        m_ExecuteIndex = (ushort)(iri.index + i - 1);
                                        matched = true;
                                        break;
                                    }
                                }
                                if (!matched)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index + caseCount - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "  switch to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "switch nojump ");
#endif
                                }
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "new array get RuntimeValue", "");
                            break;
                        }
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_StackSlotDepth >= 2)
                        {
                            ByteStackPopToRuntimeValue(out var right);
                            ByteStackPopToRuntimeValue(out var left);
#if DEBUG
                            right = m_ValueStack[--m_ValueIndex];
                            left = m_ValueStack[--m_ValueIndex];
#endif
                            bool methodCall = false;
                            RuntimeValueMethod.LogicalAnd(ref left, ref right, out methodCall);
                            if (methodCall)
                            {
                                if (m_StackSlotDepth > 0)
                                {
                                    ByteStackTryPeekRuntimeValue(1, out var top);
                                    if (top.eType == EVMType.Boolean)
                                    {
                                        PushSValueSynced(top);
                                    }
                                    else
                                    {
                                        bool b = RuntimeValueMethod.IsTruthy(ref top);
                                        top.SetBoolValue(b);
                                        PushSValueSynced(top);
                                    }
                                }
                                else
                                {
                                    PushSValueSynced(left);
                                }
                            }
                            else
                            {
                                PushSValueSynced(left);
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "new array get RuntimeValue", "");
                        }
                    }
                    break;
                case EIROpCode.Or:
                    {
                        if (m_StackSlotDepth >= 2)
                        {
                            ByteStackPopToRuntimeValue(out var right);
                            ByteStackPopToRuntimeValue(out var left);
#if DEBUG
                            right = m_ValueStack[--m_ValueIndex];
                            left = m_ValueStack[--m_ValueIndex];
#endif
                            bool methodCall = false;
                            RuntimeValueMethod.LogicalOr(ref left, ref right, out methodCall);
                            if (methodCall)
                            {
                                if (m_StackSlotDepth > 0)
                                {
                                    ByteStackTryPeekRuntimeValue(1, out var top);
                                    if (top.eType == EVMType.Boolean)
                                    {
                                        PushSValueSynced(top);
                                    }
                                    else
                                    {
                                        bool b = RuntimeValueMethod.IsTruthy(ref top);
                                        top.SetBoolValue(b);
                                        PushSValueSynced(top);
                                    }
                                }
                                else
                                {
                                    PushSValueSynced(left);
                                }
                            }
                            else
                            {
                                PushSValueSynced(left);
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "new array get RuntimeValue", "");
                        }
                    }
                    break;
                case EIROpCode.Ceq:
                case EIROpCode.Ceq_Un:
                    {
                        ExecuteEqualityOperation(iri, true, false);
                    }
                    break;
                case EIROpCode.Cne:
                case EIROpCode.Cne_Un:
                    {
                        ExecuteEqualityOperation(iri, false, false);
                    }
                    break;
                case EIROpCode.Beq:
                case EIROpCode.Beq_Un:
                    {
                        ExecuteEqualityOperation(iri, true, true);
                    }
                    break;
                case EIROpCode.Bne:
                case EIROpCode.Bne_Un:
                    {
                        ExecuteEqualityOperation(iri, false, true);
                    }
                    break;
                case EIROpCode.Clt:
                case EIROpCode.Clt_Un:
                    {
                        ExecuteRelationalOperation(iri, 2, false);
                    }
                    break;
                case EIROpCode.Cgt:
                case EIROpCode.Cgt_Un:
                    {
                        ExecuteRelationalOperation(iri, 0, false);
                    }
                    break;
                case EIROpCode.Cge:
                case EIROpCode.Cge_Un:
                    {
                        ExecuteRelationalOperation(iri, 1, false);
                    }
                    break;
                case EIROpCode.Cle:
                case EIROpCode.Cle_Un:
                    {
                        ExecuteRelationalOperation(iri, 3, false);
                    }
                    break;
                case EIROpCode.Bge:
                case EIROpCode.Bge_un:
                    {
                        ExecuteRelationalOperation(iri, 1, true);
                    }
                    break;
                case EIROpCode.Bgt:
                case EIROpCode.Bgt_Un:
                    {
                        ExecuteRelationalOperation(iri, 0, true);
                    }
                    break;
                case EIROpCode.Ble:
                case EIROpCode.Ble_Un:
                    {
                        ExecuteRelationalOperation(iri, 3, true);
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_StackSlotDepth < 1)
                        {
#if DEBUG
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "EIROpCode.Neg", 1, m_ValueIndex);
#endif
                            break;
                        }
                        ByteStackTryPeekRuntimeValue(1, out var negVal);
                        RuntimeValueMethod.NegSValue(ref negVal);
                        ByteStackReplaceTop(negVal);
#if DEBUG
                        RuntimeValueMethod.NegSValue(ref m_ValueStack[m_ValueIndex - 1]);
#endif
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_StackSlotDepth < 1)
                        {
#if DEBUG
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "EIROpCode.Not", 1, m_ValueIndex);
#endif
                            break;
                        }
                        ByteStackTryPeekRuntimeValue(1, out var notVal);
                        RuntimeValueMethod.NotSValue(ref notVal);
                        ByteStackReplaceTop(notVal);
#if DEBUG
                        RuntimeValueMethod.NotSValue(ref m_ValueStack[m_ValueIndex - 1]);
#endif
                    }
                    break;
                case EIROpCode.Add:
                case EIROpCode.Add_Un:
                case EIROpCode.Minus:
                case EIROpCode.Minus_Un:
                case EIROpCode.Multiply:
                case EIROpCode.Multiply_Un:
                case EIROpCode.Divide:
                case EIROpCode.Divide_Un:
                case EIROpCode.Modulo:
                case EIROpCode.Module_Un:
                case EIROpCode.Combine:
                case EIROpCode.Combine_Un:
                case EIROpCode.InclusiveOr:
                case EIROpCode.InclusiveOr_Un:
                case EIROpCode.XOR:
                case EIROpCode.XOR_Un:
                case EIROpCode.Shi:
                case EIROpCode.Shi_Un:
                case EIROpCode.Shr:
                case EIROpCode.Shr_Un:
                    {
                        if (m_StackSlotDepth < 2)
                        {
#if DEBUG
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, $"{opcode} ", 2, m_ValueIndex);
#endif
                            break;
                        }

                        ByteStackPopToRuntimeValue(out var right);
                        ByteStackPopToRuntimeValue(out var left);
#if DEBUG
                        right = m_ValueStack[--m_ValueIndex];
                        left = m_ValueStack[--m_ValueIndex];
#endif
                        int sign = 0;
                        bool isUn = iri.opCode == EIROpCode.Add_Un
                            || iri.opCode == EIROpCode.Minus_Un
                            || iri.opCode == EIROpCode.Multiply_Un
                            || iri.opCode == EIROpCode.Divide_Un
                            || iri.opCode == EIROpCode.Module_Un
                            || iri.opCode == EIROpCode.Combine_Un
                            || iri.opCode == EIROpCode.InclusiveOr_Un
                            || iri.opCode == EIROpCode.XOR_Un
                            || iri.opCode == EIROpCode.Shi_Un
                            || iri.opCode == EIROpCode.Shr_Un;
                        switch (iri.opCode)
                        {
                            case EIROpCode.Add: sign = 0; break;
                            case EIROpCode.Add_Un: sign = 0; break;
                            case EIROpCode.Minus: sign = 1; break;
                            case EIROpCode.Minus_Un: sign = 1; break;
                            case EIROpCode.Multiply: sign = 2; break;
                            case EIROpCode.Multiply_Un: sign = 2; break;
                            case EIROpCode.Divide: sign = 3; break;
                            case EIROpCode.Divide_Un: sign = 3; break;
                            case EIROpCode.Modulo: sign = 4; break;
                            case EIROpCode.Module_Un: sign = 4; break;
                            case EIROpCode.Combine: sign = 5; break;
                            case EIROpCode.Combine_Un: sign = 5; break;
                            case EIROpCode.InclusiveOr: sign = 6; break;
                            case EIROpCode.InclusiveOr_Un: sign = 6; break;
                            case EIROpCode.XOR: sign = 7; break;
                            case EIROpCode.XOR_Un: sign = 7; break;
                            case EIROpCode.Shi: sign = 8; break;
                            case EIROpCode.Shi_Un: sign = 8; break;
                            case EIROpCode.Shr: sign = 9; break;
                            case EIROpCode.Shr_Un: sign = 9; break;
                        }
                        RuntimeValueMethod.ComputeValueInline(ref left, sign, ref right, isUn);
                        PushSValueSynced(left);
                    }
                    break;
                case EIROpCode.CallSystemMethod:
                    {
                        if (!iri.TryGetSystemMethodCallPackage(out var sysPkg) || sysPkg == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "CallSystemMethod: expected JSON payload with name/paramCount/systemMethodKind");
                            break;
                        }

                        int kind = sysPkg.systemMethodKind;
                        switch (kind)
                        {
                            case (int)ESystemMethodCall.SystemPrint:
                                ConsoleSystemMethodCall.ExecuteSystemPrint(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemPrintln:
                                ConsoleSystemMethodCall.ExecuteSystemPrintln(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemCallCLRMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallCLRMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemCallNativeMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallNativeMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemCallJVMMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallJVMMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemReadLine:
                                ConsoleSystemMethodCall.ExecuteSystemReadLine(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemReadKey:
                                ConsoleSystemMethodCall.ExecuteSystemReadKey(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemConvertInt8:
                            case (int)ESystemMethodCall.SystemConvertUInt8:
                            case (int)ESystemMethodCall.SystemConvertBool:
                            case (int)ESystemMethodCall.SystemConvertSInt8:
                            case (int)ESystemMethodCall.SystemConvertInt16:
                            case (int)ESystemMethodCall.SystemConvertUInt16:
                            case (int)ESystemMethodCall.SystemConvertInt32:
                            case (int)ESystemMethodCall.SystemConvertUInt32:
                            case (int)ESystemMethodCall.SystemConvertInt64:
                            case (int)ESystemMethodCall.SystemConvertUInt64:
                            case (int)ESystemMethodCall.SystemConvertFloat32:
                            case (int)ESystemMethodCall.SystemConvertFloat64:
                                NumSystemMethodCall.ExecuteNumericConvert(this, sysPkg, (ESystemMethodCall)kind);
                                break;
                            case (int)ESystemMethodCall.SystemInt32Parse:
                                NumSystemMethodCall.ExecuteSystemInt32Parse(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemNumAbs:
                                NumSystemMethodCall.ExecuteSystemNumAbs(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemNumFloor:
                                NumSystemMethodCall.ExecuteSystemNumFloor(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemConvertString:
                                StringSystemMethodCall.ExecuteStringConvert(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringFormat:
                                StringSystemMethodCall.ExecuteStringFormat(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringFront:
                                StringSystemMethodCall.ExecuteStringFront(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringEnd:
                                StringSystemMethodCall.ExecuteStringEnd(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringRange:
                                StringSystemMethodCall.ExecuteStringRange(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringToByteArray:
                                StringSystemMethodCall.ExecuteStringToByteArray(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemEqualObject:
                                ObjectSystemMethodCall.ExecuteSystemEqualObject(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataAllEqual:
                                DataSystemMethodCall.ExecuteDataAllEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataTypeEqual:
                                DataSystemMethodCall.ExecuteDataTypeEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataNameAndTypeEqual:
                                DataSystemMethodCall.ExecuteDataNameAndTypeEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataDataEqual:
                                DataSystemMethodCall.ExecuteDataDataEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemBuildDataString:
                                DataSystemMethodCall.ExecuteBuildDataString(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectGetType:
                                ObjectSystemMethodCall.ExecuteSystemObjectGetType(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectGetHashCode:
                                ObjectSystemMethodCall.ExecuteSystemObjectGetHashCode(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRef:
                                ObjectSystemMethodCall.ExecuteSystemObjectRef(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRefWeak:
                                ObjectSystemMethodCall.ExecuteSystemObjectRefWeak(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRefCount:
                                ObjectSystemMethodCall.ExecuteSystemObjectRefCount(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectFree:
                                ObjectSystemMethodCall.ExecuteSystemObjectFree(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRelease:
                                ObjectSystemMethodCall.ExecuteSystemObjectRelease(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemArrayGetValueThis:
                                ObjectSystemMethodCall.ExecuteSystemArrayGetValueThis(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemArraySetValueThis:
                                ObjectSystemMethodCall.ExecuteSystemArraySetValueThis(this, sysPkg);
                                break;

                            #region Math
                            case (int)ESystemMethodCall.SystemMathSin:
                                MathSystemMethodCall.ExecuteMathSin(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathCos:
                                MathSystemMethodCall.ExecuteMathCos(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathTan:
                                MathSystemMethodCall.ExecuteMathTan(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathAsin:
                                MathSystemMethodCall.ExecuteMathAsin(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathAcos:
                                MathSystemMethodCall.ExecuteMathAcos(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathAtan:
                                MathSystemMethodCall.ExecuteMathAtan(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathAtan2:
                                MathSystemMethodCall.ExecuteMathAtan2(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathSinh:
                                MathSystemMethodCall.ExecuteMathSinh(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathCosh:
                                MathSystemMethodCall.ExecuteMathCosh(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathTanh:
                                MathSystemMethodCall.ExecuteMathTanh(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathPow:
                                MathSystemMethodCall.ExecuteMathPow(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathSqrt:
                                MathSystemMethodCall.ExecuteMathSqrt(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathExp:
                                MathSystemMethodCall.ExecuteMathExp(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathLog:
                                MathSystemMethodCall.ExecuteMathLog(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathLog10:
                                MathSystemMethodCall.ExecuteMathLog10(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathCeil:
                                MathSystemMethodCall.ExecuteMathCeil(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathFloor:
                                MathSystemMethodCall.ExecuteMathFloor(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathRound:
                                MathSystemMethodCall.ExecuteMathRound(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemMathTruncate:
                                MathSystemMethodCall.ExecuteMathTruncate(this, sysPkg);
                                break;
                            #endregion

                            default:
                                Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "CallSystemMethod: unknown systemMethodKind " + kind + " name=" + sysPkg.name);
                                break;
                        }
                    }
                    break;
                case EIROpCode.CallStatic:
                    {
                        // try to create runtime call on demand from instruction payload
                        SLRuntimeCallPackage? callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        RuntimeCall? runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);
                        if (runtimeCall == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "鎵ц闈欐€佸嚱鏁帮紝娌℃湁鍙戠幇鐩稿叧鍑芥暟�?");
                            return;
                        }

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < runtimeCall.runtimeTypeDefType.runtimeDefTypeList.Count; i++)
                        {
                            var crt = GetRuntimeTypeByDefType(runtimeCall.runtimeTypeDefType.runtimeDefTypeList[i],
                                runtimeCall.runtimeTypeDefType.runtimeDefTypeList[i].ownerRuntimeClass,
                                m_RuntimeTypeList, true);
                            classRTList.Add(crt);
                        }
                        var rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(runtimeCall.runtimeTypeDefType.runtimeClass, classRTList);
                        if (rt == null)
                        {
                            rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(runtimeCall.runtimeTypeDefType.runtimeClass, classRTList);
                        }

                        if (runtimeCall.method.onlyFunctionName == "type")
                        {
                            var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                            ByteStackPushPtr(sobj);
#if DEBUG
                            this.m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
#endif
                        }
                        else
                        {
                            List<RuntimeType> classRTList2 = new List<RuntimeType>();
                            for (int i = 0; i < runtimeCall.runtimeMethodTemplateRuntimeDefTypeList.Count; i++)
                            {
                                var crt = GetRuntimeTypeByDefType(runtimeCall.runtimeMethodTemplateRuntimeDefTypeList[i], runtimeCall.runtimeMethodTemplateRuntimeDefTypeList[i].ownerRuntimeClass,
                                    m_RuntimeTypeList, true);
                                classRTList2.Add(crt);
                            }
                            CLRVM.RunIRMethodByRuntimeType(rt, classRTList2, runtimeCall.method);
                        }
                    }
                    break;
                case EIROpCode.CallDynamic:
                    {
                        SLRuntimeCallPackage callPkg = null;
                        if (!iri.TryGetRuntimeCallPackage(out callPkg))
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "");
                            return;
                        }
                        RuntimeCall? mfc = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);
                        if (mfc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "mfc is null?");
                            return;
                        }

                        RuntimeType rt = null;
                        RuntimeClass irc = null;
                        if (iri.index > -1)
                        {
                            if (m_StackSlotDepth < iri.index)
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                                return;
                            }
                            ByteStackTryPeekRuntimeValue(iri.index, out var v);
                            if (v.sobject != null)
                            {
                                rt = v.sobject.runtimeType;
                                irc = rt.runtimeClass;
                            }
                            else
                            {
                                irc = RuntimeClassManager.GetRuntimeClassByName(v.eType.ToString());
                                if (irc != null)
                                {
                                    rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClass(irc);
                                }
                            }
                            if (irc == null)
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeClass, " irc is null");
                                return;
                            }
                            if (mfc.method == null)
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeMethod, "mfc.method is null");
                                return;
                            }
                            if (mfc.method.id == "type")
                            {
                                //var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                                //this.m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "type ��Ϊ��final ����������Ӧ��ʹ��callStatic�ķ�ʽ");
                                break;
                            }
                            else
                            {
                                // attribute hooks are handled in Front/Core; VM does�?reference Front.
                                List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                                for (int i = 0; i < mfc.runtimeMethodTemplateRuntimeDefTypeList.Count; i++)
                                {
                                    var crt = GetRuntimeTypeByDefType(mfc.runtimeMethodTemplateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
                                    rtList.Add(crt);
                                }
                                if (mfc.method.interfaceMethod)
                                {
                                    var irmethod = irc.GetNonStaticMethodIndexByName(mfc.methodName, out int index);
                                    if (irmethod != null)
                                    {
                                        CLRVM.RunIRMethodByRuntimeType(rt, rtList, irmethod);
                                    }
                                    else
                                    {
                                        Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeMethod, $"interfaceMethod:{mfc.methodName}");
                                    }
                                }
                                else
                                {
                                    CLRVM.RunIRMethodByRuntimeType(rt, rtList, mfc.method);
                                }
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundCurrentValue, "Dynamic function call from stack failed.", iri.index);
                        }
                    }
                    break;
                case EIROpCode.CallVirt:
                    {
                        SLRuntimeCallPackage? callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        RuntimeCall? runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, 0);
                        if (runtimeCall == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "Virtual call failed: runtime call metadata not found.");
                            return;
                        }
                        // attribute hooks are handled in Front/Core; VM does�?reference Front�?

                        int stackFrontIndex = (int)runtimeCall.paramCount + 1;
                        if (m_StackSlotDepth < stackFrontIndex)
                        {
#if DEBUG
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "Stack index is negative.", stackFrontIndex, m_ValueIndex);
#endif
                            return;
                        }
                        ByteStackTryPeekRuntimeValue(stackFrontIndex, out var v);

                        if (v.isNull || v.eType == EVMType.Null)
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotShouldIsNull, iri.debugInfo, "Current stack value is null.");
                            return;
                        }

                        RuntimeType? rt = null;
                        RuntimeClass? irc = null;
                        if (v.eType == EVMType.Class
                            || v.eType == EVMType.Object
                            || v.eType == EVMType.Array)
                        {
                            irc = v.sobject.runtimeClass;
                            rt = v.sobject.runtimeType;
                        }
                        else
                        {
                            irc = RuntimeClassManager.GetRuntimeClassByName("Core." + v.eType.ToString());
                            rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClass(irc);
                            if (rt == null)
                            {
                                rt = RuntimeTypeManager.AddRuntimeTypeByClass(irc);
                            }
                        }
                        if (irc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageError, "Virtual call failed: runtime class is null.");
                            return;
                        }
                        RuntimeMethod cfc = irc.GetNonStaticMethodByIndex(iri.index);


                        if (cfc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "Method index not found: " + iri.index);
                            return;
                        }
                        List<RuntimeType> rtList = new List<RuntimeType>();
                        for (int i = 0; i < runtimeCall.runtimeMethodTemplateRuntimeDefTypeList.Count; i++)
                        {
                            var crt = GetRuntimeTypeByDefType(runtimeCall.runtimeMethodTemplateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
                            rtList.Add(crt);
                        }
                        CLRVM.RunIRMethodByRuntimeType(rt, rtList, cfc);

                        var a = ObjectManager.classObjectDict;
                    }
                    break;
                case EIROpCode.Ret:
                    // stop execution early
                    m_ExecuteIndex = m_ExecuteCount;
                    break;

                case EIROpCode.CastClass:
                    {
                        if (m_StackSlotDepth < 1)
                        {
#if DEBUG
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri.debugInfo, "Stack index is negative.", 1, m_ValueIndex);
#endif
                            break;
                        }
                        var mt = TryGetInstructionRuntimeDefType(iri);
                        if (mt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }
                        var rt = GetRuntimeTypeByDefType(mt, mt.ownerRuntimeClass, m_CurrentRuntimeType?.runtimeTemplateList, true);
                        if (rt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "CastClass failed to get runtime type for metadata type: ");
                            break;
                        }
                        if (rt.eType == EVMType.Object)
                        {
                            break;
                        }
                        ByteStackTryPeekRuntimeValue(1, out var v1);
                        if (v1.isNull)
                        {
                            break;
                        }

                        // For primitive/builtin types, "as" should behave as numeric/string conversion,
                        // not strict runtime-class identity check.
                        bool targetIsPrimitiveLike =
                            rt.eType == EVMType.Boolean
                            || rt.eType == EVMType.UInt8
                            || rt.eType == EVMType.Int8
                            || rt.eType == EVMType.Int16
                            || rt.eType == EVMType.UInt16
                            || rt.eType == EVMType.Int32
                            || rt.eType == EVMType.UInt32
                            || rt.eType == EVMType.Int64
                            || rt.eType == EVMType.UInt64
                            || rt.eType == EVMType.Float32
                            || rt.eType == EVMType.Float64
                            || rt.eType == EVMType.Num;
                        if (targetIsPrimitiveLike)
                        {
                            try
                            {
                                var etype = v1.eType;
                                var rso = v1.GetReferenceSObject();
                                if (rso != null && v1.eType == EVMType.Object)
                                {
                                    etype = rso.eType;
                                }
                                if (rt.eType == EVMType.Num)
                                {

                                }
                                else if (rt.eType != etype)
                                {
                                    v1.SetNull();
                                }
                                else
                                {
                                }
                                //v1.ConvertByEType(rt.eType);
                            }
                            catch
                            {
                                v1.SetNull();
                            }
                            ByteStackReplaceTop(v1);
#if DEBUG
                            m_ValueStack[m_ValueIndex - 1] = v1;
#endif
                            break;
                        }
                        else
                        {
                            if (v1.sobject == null)
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "CastClass failed to get runtime type for metadata type: ");
                            }
                            else
                            {
                                if (!v1.sobject.runtimeType.IsExtendsRelation(rt))
                                {
                                    bool interfaceMatched = false;
                                    var targetClass = rt.runtimeClass;
                                    var sourceClass = v1.sobject.runtimeType?.runtimeClass;
                                    if (targetClass != null && targetClass.isInterfaceClass && sourceClass != null)
                                    {
                                        interfaceMatched = sourceClass.ImplementsInterface(targetClass);
                                    }

                                    if (!interfaceMatched)
                                    {
                                        v1.SetNull();
                                    }
                                }
                            }
                        }
                        ByteStackReplaceTop(v1);
#if DEBUG
                        m_ValueStack[m_ValueIndex - 1] = v1;
#endif
                    }
                    break;
                default:
                    // unhandled op
                    Log.AddRuntimeLog(LID.ShowMessageAssert, iri.debugInfo, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                    break;
            }
        }

        private void ExecuteEqualityOperation(Instruction iri, bool equalCompare, bool isBranch)
        {
            if (!TryPopBranchOperands(out var left, out var right, iri, true))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                return;
            }

            bool methodCall = false;
            RuntimeValueMethod.CompareEuqalSValue1AndValue2(ref left, ref right, equalCompare, out methodCall);

            RuntimeValue result = left;
            if (methodCall)
            {
                if (ByteStackSlotDepthCount == 0)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode + " equality operator has no return value");
                    result.SetBoolValue(false);
                }
                else
                {
                    ByteStackPopToRuntimeValue(out result);
#if DEBUG
                    result = m_ValueStack[--m_ValueIndex]; // debug mirror
#endif
                }
            }

            bool isTrue = RuntimeValueMethod.IsTruthy(ref result);
            result.SetBoolValue(isTrue);

            if (isBranch)
            {
                if (isTrue)
                {
                    m_ExecuteIndex = (ushort)(iri.index - 1);
                }
            }
            else
            {
                PushSValueSynced(result);
            }
        }

        private void ExecuteRelationalOperation(Instruction iri, int compareSign, bool isBranch)
        {
            if (!TryPopBranchOperands(out var left, out var right, iri, true))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                return;
            }

            RuntimeValueMethod.CompareSValue1AndValue2(ref left, ref right, compareSign);

            bool isTrue = RuntimeValueMethod.IsTruthy(ref left);
            left.SetBoolValue(isTrue);

            if (isBranch)
            {
                if (isTrue)
                {
                    m_ExecuteIndex = (ushort)(iri.index - 1);
                }
            }
            else
            {
                PushSValueSynced(left);
            }
        }

        private bool TryPopBranchOperands(out RuntimeValue left, out RuntimeValue right, Instruction iri, bool logStackNotEnough)
        {
            left = default;
            right = default;
            if (ByteStackSlotDepthCount < 2)
            {
                if (logStackNotEnough)
                {
                    Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri?.opCode.ToString() ?? "Branch", 2, ByteStackSlotDepthCount);
                }
                return false;
            }

            ByteStackPopToRuntimeValue(out right);
            ByteStackPopToRuntimeValue(out left);
#if DEBUG
            right = m_ValueStack[--m_ValueIndex]; // debug mirror
            left = m_ValueStack[--m_ValueIndex];  // debug mirror
#endif
            return true;
        }
    }
}
