//****************************************************************************
//  File:      RuntimeType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using SimpleLanguage.VM.MemoryManagement;
using SimpleLanguage.Parse;
using System.Text;
using SimpleLanguage.Logging;
using System.Reflection.Metadata.Ecma335;

namespace SimpleLanguage.VM
{
    public class RuntimeType
    {
        // Static field batch init can be re-entered through LoadStaticField while the same
        // runtime class (or template specialization) is still being resolved/constructed.
        // Use a shared (class-keyed) guard to prevent cross-instance infinite recursion.
        private static readonly Dictionary<string, bool> s_StaticExprAppliedByKey = new Dictionary<string, bool>();
        private static readonly HashSet<string> s_StaticExprApplyingByKey = new HashSet<string>();

        public int id => m_Id;
        public RuntimeClass runtimeClass => m_RuntimeClass;
        public List<RuntimeType> runtimeTemplateList => m_RuntimeTemplateList;
        /// <summary>本类型静态成员的紧凑字节块（与实例 <see cref="ClassObject"/> 上逻辑一致）。</summary>
        public byte[]? memberData => m_MemberData;

        private RuntimeClass m_RuntimeClass = null;
        private List<RuntimeType> m_RuntimeTemplateList = new List<RuntimeType>();
        private RuntimeObject[] m_StaticMemberRuntimeObjectArray = null;
        /// <summary>静态字段紧凑布局缓冲区，与 <see cref="m_StaticMemberRuntimeObjectArray"/> 下标一一对应（空槽不占字节）。</summary>
        private byte[] m_MemberData = null;
        //private Dictionary<int, int> m_StaticFieldIndexToSlot = new Dictionary<int, int>();
        //private bool m_IsStaticMemInitializing = false;
        //private bool m_IsStaticExprBatchApplied = false;
        private bool m_IsStaticExprBatchApplying = false;
        private int m_Id = 0;
        public EVMType eType { get; protected set; } = EVMType.Void;

        public RuntimeType( RuntimeClass rc, List<RuntimeType> rtList)
        {
            ++m_Id;
            m_RuntimeClass = rc;
            if (rtList != null)
            {
                m_RuntimeTemplateList = rtList;
            }
            if (Enum.TryParse<EVMType>(m_RuntimeClass.name, true, out var eoutType))
            {
                eType = eoutType;
            }
            else
            {
                eType = EVMType.Class;
            }
            //eType = GetVMType(irClass.irName);
        }
        public void SetEVMType( EVMType evmtype )
        {
            eType = evmtype;
        }
        //public static EVMType GetVMType(string irName)
        //{
        //    // Minimal mapping by known IR names used by ObjectManager
        //    if (string.IsNullOrEmpty(irName)) return EVMType.Class;
        //    if (irName.EndsWith("Int32") || irName.EndsWith("Int16") || irName.EndsWith("Int64") || irName.EndsWith("UInt32") || irName.EndsWith("UInt16") || irName.EndsWith("UInt64") || irName.EndsWith("Byte") || irName.EndsWith("SByte"))
        //        return EVMType.Num;
        //    if (irName.EndsWith("Float32") || irName.EndsWith("Float64"))
        //        return EVMType.Num;
        //    if (irName.EndsWith("String"))
        //        return EVMType.String;
        //    if (irName.EndsWith("Boolean"))
        //        return EVMType.Boolean;
        //    return EVMType.Class;
        //}

        public RuntimeType GetExtendsTemplateRuntimeType( RuntimeDefType irmt, List<RuntimeType> _runtimeTemplateList)
        {
            if (_runtimeTemplateList?.Count > 0)
            {
                return _runtimeTemplateList[irmt.templateIndex];
            }
            return null;
        }
        public RuntimeType GetClassRuntimeType( RuntimeDefType rdt, bool isAdd = false)
        {
            var irmc = this.m_RuntimeClass;
            if (rdt.templateIndex != -1)
            {
                if (rdt.ownerRuntimeClass == this.m_RuntimeClass)
                {
                    return m_RuntimeTemplateList[rdt.templateIndex];
                }
                else
                {
                    var mt = m_RuntimeClass.GetRuntimeDefTypeByTemplateAndClassRelation(rdt.ownerRuntimeClass, rdt.templateIndex);
                    if (mt == null) return null;

                    return GetClassRuntimeType(mt, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (rdt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < rdt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(rdt.runtimeDefTypeList[i], isAdd);
                        rtList.Add(crt);
                    }
                }
                var rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(rdt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(rdt.runtimeClass, rtList);
                }
                return rt;
            }
        }
        public void GetStaticMemberVariableSValue(int index, ref SValue svalue)
        {
            if (m_StaticMemberRuntimeObjectArray == null)
            {
                svalue.SetNull();
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object list is not initialized for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            //var slotIndex = ResolveStaticSlotIndex(index);
            //if (slotIndex < 0 || slotIndex >= m_StaticMemObjectList.Length)
            //{
            //    svalue.SetNull();
            //    return;
            //}
            //EnsureStaticMemberObjectAt(index);
            var sobj = m_StaticMemberRuntimeObjectArray[index];
            if (sobj == null || sobj.isNull )
            {
                svalue.SetNull();
                return;
            }
            svalue.SetSObject(sobj.sobject);
        }
        public void SetStaticMemberVariableSValue(int index, SValue svalue)
        {
            if (m_StaticMemberRuntimeObjectArray == null) return;
            //var slotIndex = ResolveStaticSlotIndex(index);
            //if (slotIndex < 0 || slotIndex >= m_StaticMemObjectList.Length) return;
            var target = m_StaticMemberRuntimeObjectArray[index];
            //if (target == null)
            //{
            //    m_StaticMemberRuntimeObjectArray[index].SetSObject( svalue.GetSObject() );
            //    return;
            //}
            if (svalue.isNull)
            {
                target.SetNull();
                return;
            }
            // attempt to set by type-aware method on SObject
            //target.SetValueByType(svalue.eType == EVMType.Class ? EVMType.Class : svalue.eType, svalue.eType == EVMType.Class ? (object)svalue.sobject : svalue.GetValueObject());
            m_StaticMemberRuntimeObjectArray[index].SetSObjectBySValue(ref svalue);
        }

        /// <summary>按静态成员下标从 <see cref="memberData"/> 解析（引用槽为 HashCode，与 <see cref="RuntimeObject"/> 约定一致）。</summary>
        public bool TryReadStaticMemberDataAsSValue(int staticMemberIndex, ref SValue svalue)
        {
            if (m_StaticMemberRuntimeObjectArray == null
                || staticMemberIndex < 0
                || staticMemberIndex >= m_StaticMemberRuntimeObjectArray.Length)
                return false;
            var ro = m_StaticMemberRuntimeObjectArray[staticMemberIndex];
            if (ro == null)
                return false;
            return ro.TryReadMemberDataToSValue(ref svalue);
        }

        public void EnsureStaticMemberObjectsInitialized()
        {
            //if (m_IsStaticMemInitializing) return;
            if (m_RuntimeClass?.staticIRMetaVariableList == null) return;

            //m_IsStaticMemInitializing = true;
            try
            {
                if (m_StaticMemberRuntimeObjectArray == null && m_RuntimeClass.staticIRMetaVariableList.Count > 0)
                {
                    m_StaticMemberRuntimeObjectArray = new RuntimeObject[m_RuntimeClass.staticIRMetaVariableList.Count];
                    for (int i = 0; i < m_RuntimeClass.staticIRMetaVariableList.Count; i++)
                    {
                        var field = m_RuntimeClass.staticIRMetaVariableList[i];
                        if (field == null) continue;
                        var rt = GetClassRuntimeType(field.runtimeDefType, true);
                        if (rt == null) return;

                        m_StaticMemberRuntimeObjectArray[i] = new RuntimeObject(rt, field, null);

                        //if (!m_StaticFieldIndexToSlot.ContainsKey(field.index))
                        //{
                        //    m_StaticFieldIndexToSlot[field.index] = i;
                        //}
                    }
                }

                BuildStaticMemberDataLayout();
                ApplyStaticMemberExpressionsBatch();
            }
            catch (Exception e) { }
        }

        /// <summary>为 <see cref="m_StaticMemberRuntimeObjectArray"/> 分配 <see cref="m_MemberData"/> 并绑定各 <see cref="RuntimeObject"/> 切片（仅首次分配，避免覆盖已写入的静态初值）。</summary>
        private void BuildStaticMemberDataLayout()
        {
            if (m_StaticMemberRuntimeObjectArray == null || m_StaticMemberRuntimeObjectArray.Length == 0)
            {
                m_MemberData = null;
                return;
            }

            if (m_MemberData != null)
                return;

            int n = m_StaticMemberRuntimeObjectArray.Length;
            int totalBytes = 0;
            for (int i = 0; i < n; i++)
            {
                var ro = m_StaticMemberRuntimeObjectArray[i];
                if (ro == null)
                    continue;
                totalBytes += MemberDataLayout.GetSlotByteLength(ro.runtimeType);
            }

            m_MemberData = totalBytes > 0 ? new byte[totalBytes] : null;
            int offset = 0;
            for (int i = 0; i < n; i++)
            {
                var ro = m_StaticMemberRuntimeObjectArray[i];
                if (ro == null)
                    continue;
                int len = MemberDataLayout.GetSlotByteLength(ro.runtimeType);
                ro.AttachMemberDataSlice(m_MemberData, offset, len, i);
                offset += len;
            }
        }
        private void ApplyStaticMemberExpressionsBatch()
        {
            //if (m_IsStaticExprBatchApplied) return;
            if (m_IsStaticExprBatchApplying) return;

            var key = BuildStaticExprInitKey();
            if (s_StaticExprAppliedByKey.TryGetValue(key, out var applied) && applied)
            {
                //m_IsStaticExprBatchApplied = true;
                return;
            }
            if (s_StaticExprApplyingByKey.Contains(key))
            {
                // Another RuntimeType instance is currently applying the same batch.
                return;
            }

            s_StaticExprApplyingByKey.Add(key);
            m_IsStaticExprBatchApplying = true;
            try
            {
                // Collect all static field initializer instructions in class order.
                List<Instruction> initIR = new List<Instruction>();
                for (int i = 0; i < m_RuntimeClass.staticIRMetaVariableList.Count; i++)
                {
                    var field = m_RuntimeClass.staticIRMetaVariableList[i];
                    if (field == null) continue;
                    var fieldExpr = SLRuntimeModuleRegistry.GetStaticFieldInitializerExpressions(m_RuntimeClass.id, field.index);
                    if (fieldExpr == null || fieldExpr.Count == 0) continue;
                    initIR.AddRange(fieldExpr);
                }

                if (initIR.Count == 0)
                {
                    //m_IsStaticExprBatchApplied = true;
                    s_StaticExprAppliedByKey[key] = true;
                    return;
                }

                // Mark as applied BEFORE executing batch to break any recursive re-entry
                // through LoadStaticField/StoreStaticField paths while vm.Run is active.
                // If execution fails, static slots still keep default objects and we avoid
                // infinite initialization loops.
                //m_IsStaticExprBatchApplied = true;
                s_StaticExprAppliedByKey[key] = true;

                bool pushedRoot = false;
                if (CLRVM.clrRuntimeStack.Count == 0)
                {
                    var root = new RuntimeVM(new List<Instruction>());
                    root.id = "__static_field_init_root__";
                    CLRVM.PushCLRRuntime(root);
                    pushedRoot = true;
                }

                try
                {
                    var vm = CLRVM.CreateExeSplite(new List<RuntimeType>(), initIR);
                    vm.id = $"__static_field_init__{m_RuntimeClass.name}";
                    vm.isPersistent = true;
                    vm.Run(true);
                    CLRVM.PopCLRRuntime();
                }
                finally
                {
                    if (pushedRoot && CLRVM.clrRuntimeStack.Count > 0)
                    {
                        CLRVM.PopCLRRuntime();
                    }
                }
            }
            finally
            {
                m_IsStaticExprBatchApplying = false;
                s_StaticExprApplyingByKey.Remove(key);
            }
        }
        private string BuildStaticExprInitKey()
        {
            // runtime class id + template runtime class ids.
            // This keeps the guard correct for generic specializations too.
            var sb = new StringBuilder();
            sb.Append(m_RuntimeClass?.id ?? 0);
            sb.Append(':');
            if (m_RuntimeTemplateList != null && m_RuntimeTemplateList.Count > 0)
            {
                for (int i = 0; i < m_RuntimeTemplateList.Count; i++)
                {
                    var t = m_RuntimeTemplateList[i];
                    sb.Append(t?.m_RuntimeClass?.id ?? 0);
                    if (i + 1 < m_RuntimeTemplateList.Count) sb.Append(',');
                }
            }
            return sb.ToString();
        }
        public static bool SameRuntimeType(RuntimeType rt1, RuntimeType rt2)
        {
            if (rt1 == null || rt2 == null) return false;
            return rt1.m_RuntimeClass.id == rt2.m_RuntimeClass.id;
        }
        public bool IsExtendsRelation(RuntimeType rt)
        {
            if (rt == null) return false;
            return m_RuntimeClass.IsExtendsRelation(rt.m_RuntimeClass);
        }
        public static bool IsNumericEType(EVMType t)
        {
            return t == EVMType.Num;
        }
        public bool IsExtendsRelationWithPrimitiveSupport(RuntimeType rt)
        {
            return IsExtendsRelation(rt);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var name = m_RuntimeClass?.name;
            if (string.IsNullOrEmpty(name))
                sb.Append('<').Append(m_RuntimeClass?.id ?? 0).Append('>');
            else
                sb.Append(name);
            if( m_RuntimeTemplateList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_RuntimeTemplateList.Count; i++ )
                {
                    sb.Append(m_RuntimeTemplateList[i].ToString());
                }
                sb.Append(">");
            }
            return sb.ToString();
        }
    }

    public static class RuntimeTypeManager
    {
        public static List<RuntimeType> runtimeTypeList => s_RuntimeTypeList;
        public static RuntimeType voidRuntimeType { get => m_VoidRuntimeType; }
        public static RuntimeType objectRuntimeType { get => m_ObjectRuntimeType; }
        public static RuntimeType boolRuntimeType { get => m_BoolRuntimeType; }
        public static RuntimeType uint8RuntimeType { get => m_UInt8RuntimeType; }
        public static RuntimeType int8RuntimeType { get => m_Int8RuntimeType; }
        public static RuntimeType int16RuntimeType { get => m_Int16RuntimeType; }
        public static RuntimeType uint16RuntimeType { get => m_UInt16RuntimeType; }
        public static RuntimeType int32RuntimeType { get => m_Int32RuntimeType; }
        public static RuntimeType uint32RuntimeType { get => m_UInt32RuntimeType; }
        public static RuntimeType int64RuntimeType { get => m_Int64RuntimeType; }
        public static RuntimeType uint64RuntimeType { get => m_UInt64RuntimeType; }
        public static RuntimeType float32RuntimeType { get => m_Float32RuntimeType; }
        public static RuntimeType float64RuntimeType { get => m_Float64RuntimeType; }
        public static RuntimeType stringRuntimeType { get => m_StringRuntimeType; }
        public static RuntimeType numRuntimeType { get => m_NumRuntimeType; }
        public static RuntimeType typeRuntimeType { get => m_TypeRuntimeType; }
        public static RuntimeType memberRuntimeType { get => m_MemberRuntimeType; }

        private static List<RuntimeType> s_RuntimeTypeList = new List<RuntimeType>();
        private static List<RuntimeType> s_CoreRuntimeTypeList = new List<RuntimeType>();
        private static RuntimeType m_ObjectRuntimeType = null;
        private static RuntimeType m_TypeRuntimeType = null;
        private static RuntimeType m_VoidRuntimeType = null;
        private static RuntimeType m_BoolRuntimeType = null;
        private static RuntimeType m_NumRuntimeType = null;
        private static RuntimeType m_UInt8RuntimeType = null;
        private static RuntimeType m_Int8RuntimeType = null;
        private static RuntimeType m_Int16RuntimeType = null;
        private static RuntimeType m_UInt16RuntimeType = null;
        private static RuntimeType m_Int32RuntimeType = null;
        private static RuntimeType m_UInt32RuntimeType = null;
        private static RuntimeType m_Int64RuntimeType = null;
        private static RuntimeType m_UInt64RuntimeType = null;
        private static RuntimeType m_Float32RuntimeType = null;
        private static RuntimeType m_Float64RuntimeType = null;
        private static RuntimeType m_StringRuntimeType = null;
        private static RuntimeType m_MemberRuntimeType = null;

        // Ensure core primitive runtime types are registered (and their static fields populated)
        // before VM global initialization and object creation.
        public static void EnsureCoreRuntimeTypesRegistered()
        {
            EnsureByClassName("Core.Object", ref m_ObjectRuntimeType, true );
            EnsureByClassName("Core.Void", ref m_VoidRuntimeType, true);
            EnsureByClassName("Core.Type", ref m_TypeRuntimeType, true);
            EnsureByClassName("Core.Boolean", ref m_BoolRuntimeType, true);
            EnsureByClassName("Core.Num", ref m_NumRuntimeType, true);
            EnsureByClassName("Core.UInt8", ref m_UInt8RuntimeType, true);
            EnsureByClassName("Core.Int8", ref m_Int8RuntimeType, true);
            EnsureByClassName("Core.Int16", ref m_Int16RuntimeType, true);
            EnsureByClassName("Core.UInt16", ref m_UInt16RuntimeType, true);
            EnsureByClassName("Core.Int32", ref m_Int32RuntimeType, true);
            EnsureByClassName("Core.UInt32", ref m_UInt32RuntimeType, true);
            EnsureByClassName("Core.Int64", ref m_Int64RuntimeType, true);
            EnsureByClassName("Core.UInt64", ref m_UInt64RuntimeType, true);
            EnsureByClassName("Core.String", ref m_StringRuntimeType, true);
            EnsureByClassName("Core.Float32", ref m_Float32RuntimeType, true);
            EnsureByClassName("Core.Float64", ref m_Float64RuntimeType, true);
            //EnsureRuntimeTypeRegisteredByClassName("Core.Array");
        }
        public static bool IsCoreRuntimeType(RuntimeType rt )
        {
            if(s_CoreRuntimeTypeList.Find( a=> a == rt  ) != null)
            {
                return true;
            }
            return false;
        }
        public static RuntimeType GetRuntimeTypeByEVMType(EVMType vmtype)
        {
            var rt = vmtype switch
            {
                EVMType.Object => m_ObjectRuntimeType,
                EVMType.Boolean => m_BoolRuntimeType,
                EVMType.UInt8 => m_UInt8RuntimeType,
                EVMType.Int8 => m_Int8RuntimeType,
                EVMType.Int16 => m_Int16RuntimeType,
                EVMType.UInt16 => m_UInt16RuntimeType,
                EVMType.Int32 => m_Int32RuntimeType,
                EVMType.UInt32 => m_UInt32RuntimeType,
                EVMType.Int64 => m_Int64RuntimeType,
                EVMType.UInt64 => m_UInt64RuntimeType,
                EVMType.Float32 => m_Float32RuntimeType,
                EVMType.Float64 => m_Float64RuntimeType,
                EVMType.String => m_StringRuntimeType,
                EVMType.Num => m_NumRuntimeType,
                EVMType.Void => m_VoidRuntimeType,
                EVMType.Type => m_TypeRuntimeType,
                _ => null
            };
            return rt;
        }
        private static void EnsureByClassName(string runtimeClassName, ref RuntimeType targetField, bool isCore = false )
        {
            if (targetField != null) return;

            var rc = RuntimeClassManager.GetRuntimeClassByName(runtimeClassName);
            if (rc == null)
            {
                // Prefer package-driven creation so RuntimeClass has full field/method/template data.
                rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassByName(runtimeClassName);
                if (rc == null)
                {
                    // Fallback: try by stable id (if exported ids match the stable hash).
                    rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassById(StableId32(runtimeClassName));
                }
                if (rc == null)
                {
                    // Last resort: create minimal RuntimeClass so ObjectManager can still build primitive objects.
                    rc = new RuntimeClass
                    {
                        id = StableId32(runtimeClassName),
                        name = runtimeClassName,
                    };
                    RuntimeClassManager.AddRuntimeClass(rc);
                }
            }

            // If a runtime type already exists (possibly created via template-based path),
            // reuse it to avoid duplicate RuntimeType instances.
            var existed = GetRuntimeTypeById(rc.id);
            if (existed != null)
            {
                targetField = existed;
                return;
            }

            if (isCore)
            {
                targetField = AddRuntimeTypeByCoreClass(rc);
            }
            else
            {
                targetField = AddRuntimeTypeByClass(rc);
            }
        }

        private static int StableId32(string s)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }
                return (int)hash;
            }
        }

        /// <summary>
        /// Builds a <see cref="TypeObject"/> that <i>describes</i> <paramref name="rt"/> (for <c>GetType()</c> / <c>typeof</c>).
        /// Previous implementation always returned null, so <c>type.toString()</c> was empty and type handles were broken.
        /// </summary>
        public static ClassObject? CreateTypeObject(RuntimeType? rt)
        {
            if (rt == null) return null;
            try
            {
                EnsureCoreRuntimeTypesRegistered();
                var tobj = new TypeObject(rt);
                tobj.CreateObject();
                if (tobj.refCount == 0)
                    tobj.refCount = 1;
                SlMemoryManager.Instance.RegisterAllocation(tobj);
                return tobj;
            }
            catch
            {
                return null;
            }
        }

        public static RuntimeType GetRuntimeTypeByRuntimeClass(RuntimeClass rmc)
        {
            if (rmc == null) return null;
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == rmc.id);
        }
        public static RuntimeType GetRuntimeTypeById( int id )
        {
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == id );
        }
        public static RuntimeType GetRuntimeTypeByDefTypeAndAdd(RuntimeDefType irmt )
        {
            var rt = GetRuntimeTypeByDefType(irmt);
            if( rt == null )
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetRuntimeTypeByDefTypeAndAdd(irmt.runtimeDefTypeList[i]);
                        rtList.Add(crt);
                    }
                }
                rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
            }
            return rt;
        }
        public static RuntimeType GetRuntimeTypeByDefType(RuntimeDefType irmt )
        {
            if (irmt == null) return null;
            foreach (var v in s_RuntimeTypeList)
            {
                if (v.runtimeClass != irmt.runtimeClass)
                {
                    continue;
                }

                if (v.runtimeTemplateList.Count == irmt.runtimeDefTypeList.Count )
                {
                    if (v.runtimeTemplateList.Count == 0)
                    {
                        return v;
                    }
                    bool flag = true;
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var ft = GetRuntimeTypeByDefType(irmt.runtimeDefTypeList[i]);
                        if (!RuntimeType.SameRuntimeType(ft, v.runtimeTemplateList[i]))
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                        return v;
                }
            }
            return null;
        }
        public static RuntimeType GetRuntimeTypeByRuntimeClassAndRuntimeTypeList( RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            foreach (var v in s_RuntimeTypeList)
            {
                if (v.runtimeClass != rmc)
                {
                    continue;
                }

                if (v.runtimeTemplateList.Count == inputTemplateTypeList.Count)
                {
                    if (v.runtimeTemplateList.Count == 0)
                    {
                        return v;
                    }
                    bool flag = true;
                    for (int i = 0; i < inputTemplateTypeList.Count; i++)
                    {
                        if (!RuntimeType.SameRuntimeType(inputTemplateTypeList[i], v.runtimeTemplateList[i]))
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                        return v;
                }
            }
            return null;
        }
        public static RuntimeType AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            if (rmc == null) return null;
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);
            s_RuntimeTypeList.Add(rt);
            rt.EnsureStaticMemberObjectsInitialized();
            return rt;
        }
        public static RuntimeType AddRuntimeTypeByCoreClass(RuntimeClass rmc)
        {
            RuntimeType rt = new RuntimeType(rmc, null);

            string name = rmc.name;
            if( name == "Object" || name == "Core.Object" )
            {
                m_ObjectRuntimeType = rt;
                m_ObjectRuntimeType.SetEVMType(EVMType.Object);
            }
            if (name == "Void" || name == "Core.Void")
            {
                m_VoidRuntimeType = rt;
                m_VoidRuntimeType.SetEVMType(EVMType.Void);
            }
            else if (name == "Type" || name == "Core.Type")
            {
                m_TypeRuntimeType = rt;
                m_TypeRuntimeType.SetEVMType(EVMType.Type);
            }
            else if (name == "Boolean" || name == "Core.Boolean")
            {
                m_BoolRuntimeType = rt;
                m_BoolRuntimeType.SetEVMType(EVMType.Boolean);
            }
            else if (name == "Num" || name == "Core.Num")
            {
                m_NumRuntimeType = rt;
                m_NumRuntimeType.SetEVMType(EVMType.Num);
            }
            else if (name == "UInt8" || name == "Core.UInt8")
            {
                m_UInt8RuntimeType = rt;
                m_UInt8RuntimeType.SetEVMType(EVMType.UInt8);
            }
            else if (name == "Int8" || name == "Core.Int8")
            {
                m_Int8RuntimeType = rt;
                m_Int8RuntimeType.SetEVMType(EVMType.Int8);
            }
            else if (name == "Int16" || name == "Core.Int16")
            {
                m_Int16RuntimeType = rt;
                m_Int16RuntimeType.SetEVMType(EVMType.Int16);
            }
            else if (name == "UInt16" || name == "Core.UInt16")
            {
                m_UInt16RuntimeType = rt;
                m_UInt16RuntimeType.SetEVMType(EVMType.UInt16);
            }
            else if (name == "Int32" || name == "Core.Int32")
            {
                m_Int32RuntimeType = rt;
                m_Int32RuntimeType.SetEVMType(EVMType.Int32);
            }
            else if (name == "UInt32" || name == "Core.UInt32")
            {
                m_UInt32RuntimeType = rt;
                m_UInt32RuntimeType.SetEVMType(EVMType.UInt32);
            }
            else if (name == "Int64" || name == "Core.Int64")
            {
                m_Int64RuntimeType = rt;
                m_Int64RuntimeType.SetEVMType(EVMType.Int64);
            }
            else if (name == "UInt64" || name == "Core.UInt64")
            {
                m_UInt64RuntimeType = rt;
                m_UInt64RuntimeType.SetEVMType(EVMType.UInt64);
            }
            else if (name == "Float32" || name == "Core.Float32")
            {
                m_Float32RuntimeType = rt;
                m_Float32RuntimeType.SetEVMType(EVMType.Float32);
            }
            else if (name == "Float64" || name == "Core.Float64")
            {
                m_Float64RuntimeType = rt;
                m_Float64RuntimeType.SetEVMType(EVMType.Float64);
            }
            else if (name == "String" || name == "Core.String")
            {
                m_StringRuntimeType = rt;
                m_StringRuntimeType.SetEVMType(EVMType.String);
            }
            s_RuntimeTypeList.Add(rt);
            s_CoreRuntimeTypeList.Add(rt);
            rt.EnsureStaticMemberObjectsInitialized();

            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass(RuntimeClass rmc )
        {
            RuntimeType rt = new RuntimeType(rmc, null);
            rt.EnsureStaticMemberObjectsInitialized();

            s_RuntimeTypeList.Add(rt);

            return rt;
        }
    }
}
