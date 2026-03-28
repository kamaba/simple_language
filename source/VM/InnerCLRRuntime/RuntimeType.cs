//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using SimpleLanguage.Parse;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeType
    {
        // Static field batch init can be re-entered through LoadStaticField while the same
        // runtime class (or template specialization) is still being resolved/constructed.
        // Use a shared (class-keyed) guard to prevent cross-instance infinite recursion.
        private static readonly Dictionary<string, bool> s_StaticExprAppliedByKey = new Dictionary<string, bool>();
        private static readonly HashSet<string> s_StaticExprApplyingByKey = new HashSet<string>();

        public RuntimeClass runtimeClass => m_RuntimeClass;
        public List<RuntimeType> runtimeTemplateList => m_RuntimeTemplateList;

        private RuntimeClass m_RuntimeClass = null;
        private List<RuntimeType> m_RuntimeTemplateList = new List<RuntimeType>();
        private SObject[] m_StaticMemObjectList;
        private Dictionary<int, int> m_StaticFieldIndexToSlot = new Dictionary<int, int>();
        //private bool m_IsStaticMemInitializing = false;
        //private bool m_IsStaticExprBatchApplied = false;
        private bool m_IsStaticExprBatchApplying = false;
        public EVMType eType { get; set; }

        public RuntimeType( RuntimeClass rc, List<RuntimeType> rtList)
        {
            m_RuntimeClass = rc;
            if (rtList != null)
            {
                m_RuntimeTemplateList = rtList;
            }

            // Delay static member object creation to first access.
            // This avoids recursive RuntimeType construction when static fields
            // reference types that are still being initialized.
            m_StaticMemObjectList = null;

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
        public static EVMType GetVMType(string irName)
        {
            // Minimal mapping by known IR names used by ObjectManager
            if (string.IsNullOrEmpty(irName)) return EVMType.Class;
            if (irName.EndsWith("Int32") || irName.EndsWith("Int16") || irName.EndsWith("Int64") || irName.EndsWith("UInt32") || irName.EndsWith("UInt16") || irName.EndsWith("UInt64") || irName.EndsWith("Byte") || irName.EndsWith("SByte"))
                return EVMType.Num;
            if (irName.EndsWith("Float32") || irName.EndsWith("Float64"))
                return EVMType.Num;
            if (irName.EndsWith("String"))
                return EVMType.String;
            if (irName.EndsWith("Boolean"))
                return EVMType.Boolean;
            return EVMType.Class;
        }

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
                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(rdt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(rdt.runtimeClass, rtList);

                    EnsureStaticMemberObjectsInitialized();
                }
                return rt;
            }
        }
        public void GetMemberVariableSValue(int index, ref SValue svalue)
        {
            if (m_StaticMemObjectList == null)
            {
                svalue.SetNull();
                return;
            }
            var slotIndex = ResolveStaticSlotIndex(index);
            if (slotIndex < 0 || slotIndex >= m_StaticMemObjectList.Length)
            {
                svalue.SetNull();
                return;
            }
            EnsureStaticMemberObjectAt(slotIndex);
            var sobj = m_StaticMemObjectList[slotIndex];
            if (sobj == null || sobj.isNull)
            {
                svalue.SetNull();
                return;
            }
            svalue.SetSObject(sobj);
        }
        public void SetMemberVariableSValue(int index, SValue svalue)
        {
            if (m_StaticMemObjectList == null) return;
            var slotIndex = ResolveStaticSlotIndex(index);
            if (slotIndex < 0 || slotIndex >= m_StaticMemObjectList.Length) return;
            EnsureStaticMemberObjectAt(slotIndex);
            var target = m_StaticMemObjectList[slotIndex];
            if (target == null) return;
            if (svalue.isNull)
            {
                target.SetNull();
                return;
            }
            // attempt to set by type-aware method on SObject
            target.SetValueByType(svalue.eType == EVMType.Class ? EVMType.Class : svalue.eType, svalue.eType == EVMType.Class ? (object)svalue.sobject : svalue.GetValueObject());
        }
        public List<Instruction> CreateStaticMetaMetaVariableIRList()
        {
            return new List<Instruction>();
        }
        public void EnsureStaticMemberObjectsInitialized()
        {
            //if (m_IsStaticMemInitializing) return;
            if (m_RuntimeClass?.staticIRMetaVariableList == null) return;

            //m_IsStaticMemInitializing = true;
            try
            {
                if (m_StaticMemObjectList == null)
                {
                    m_StaticMemObjectList = new SObject[m_RuntimeClass.staticIRMetaVariableList.Count];
                    m_StaticFieldIndexToSlot.Clear();
                    for (int i = 0; i < m_RuntimeClass.staticIRMetaVariableList.Count; i++)
                    {
                        var field = m_RuntimeClass.staticIRMetaVariableList[i];
                        if (field == null) continue;
                        if (!m_StaticFieldIndexToSlot.ContainsKey(field.index))
                        {
                            m_StaticFieldIndexToSlot[field.index] = i;
                        }
                    }
                }
                for (int i = 0; i < m_StaticMemObjectList.Length; i++)
                {
                    EnsureStaticMemberObjectAt(i);
                }

                // After all static member objects are created, run class-level
                // static expressions once (batch). This avoids recursive init
                // when static fields reference each other.
                //if (!m_IsStaticExprBatchApplied)
                {
                    ApplyStaticMemberExpressionsBatch();
                }
            }
            catch (Exception e) { }
        }
        private void EnsureStaticMemberObjectAt(int index)
        {
            if (m_StaticMemObjectList == null) return;
            if (index < 0 || index >= m_StaticMemObjectList.Length) return;

            // Already initialized: keep existing value/object, do not overwrite.
            if (m_StaticMemObjectList[index] != null) return;

            if (m_RuntimeClass?.staticIRMetaVariableList == null) return;
            if (index >= m_RuntimeClass.staticIRMetaVariableList.Count) return;

            var irmv = m_RuntimeClass.staticIRMetaVariableList[index];
            var rt = GetClassRuntimeType(irmv.runtimeDefType, true);
            if (rt == null) return;

            // Not initialized yet: prioritize initializing the missing slot.
            m_StaticMemObjectList[index] = ObjectManager.CreateObjectByRuntimeType(rt, true);
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

            if (m_StaticMemObjectList == null) return;
            if (m_RuntimeClass?.staticIRMetaVariableList == null) return;

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
                    vm.id = $"__static_field_init__{m_RuntimeClass.id}";
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
        private int ResolveStaticSlotIndex(int fieldIndex)
        {
            if (fieldIndex < 0) return -1;
            if (m_RuntimeClass?.staticIRMetaVariableList == null) return -1;
            if (m_StaticFieldIndexToSlot != null && m_StaticFieldIndexToSlot.TryGetValue(fieldIndex, out var slot))
            {
                return slot;
            }
            // Fallback for old data where field index is already compact slot index.
            if (fieldIndex < m_RuntimeClass.staticIRMetaVariableList.Count) return fieldIndex;
            return -1;
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
            sb.Append(m_RuntimeClass.name );
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
        public static RuntimeType boolRuntimeType { get => m_BoolRuntimeType; }
        public static RuntimeType byteRuntimeType { get => m_ByteRuntimeType; }
        public static RuntimeType sbyteRuntimeType { get => m_SByteRuntimeType; }
        public static RuntimeType int16RuntimeType { get => m_Int16RuntimeType; }
        public static RuntimeType uint16RuntimeType { get => m_UInt16RuntimeType; }
        public static RuntimeType int32RuntimeType { get => m_Int32RuntimeType; }
        public static RuntimeType uint32RuntimeType { get => m_UInt32RuntimeType; }
        public static RuntimeType int64RuntimeType { get => m_Int64RuntimeType; }
        public static RuntimeType uint64RuntimeType { get => m_UInt64RuntimeType; }
        public static RuntimeType float32RuntimeType { get => m_Float32RuntimeType; }
        public static RuntimeType float64runtimeType { get => m_Float64RuntimeType; }
        public static RuntimeType stringRuntimeType { get => m_StringRuntimeType; }
        public static RuntimeType numRuntimeType { get => m_NumRuntimeType; }
        public static RuntimeType typeRuntimeType { get => m_TypeRuntimeType; }

        private static List<RuntimeType> s_RuntimeTypeList = new List<RuntimeType>();
        private static RuntimeType m_TypeRuntimeType = null;
        private static RuntimeType m_VoidRuntimeType = null;
        private static RuntimeType m_BoolRuntimeType = null;
        private static RuntimeType m_NumRuntimeType = null;
        private static RuntimeType m_ByteRuntimeType = null;
        private static RuntimeType m_SByteRuntimeType = null;
        private static RuntimeType m_Int16RuntimeType = null;
        private static RuntimeType m_UInt16RuntimeType = null;
        private static RuntimeType m_Int32RuntimeType = null;
        private static RuntimeType m_UInt32RuntimeType = null;
        private static RuntimeType m_Int64RuntimeType = null;
        private static RuntimeType m_UInt64RuntimeType = null;
        private static RuntimeType m_Float32RuntimeType = null;
        private static RuntimeType m_Float64RuntimeType = null;
        private static RuntimeType m_StringRuntimeType = null;

        // Ensure core primitive runtime types are registered (and their static fields populated)
        // before VM global initialization and object creation.
        public static void EnsureCoreRuntimeTypesRegistered()
        {
            EnsureByClassName("Void", ref m_VoidRuntimeType);
            EnsureByClassName("Type", ref m_TypeRuntimeType);
            EnsureByClassName("Bool", ref m_BoolRuntimeType);
            EnsureByClassName("Num", ref m_NumRuntimeType);
            EnsureByClassName("Byte", ref m_ByteRuntimeType);
            EnsureByClassName("SByte", ref m_SByteRuntimeType);
            EnsureByClassName("Int16", ref m_Int16RuntimeType);
            EnsureByClassName("UInt16", ref m_UInt16RuntimeType);
            EnsureByClassName("Int32", ref m_Int32RuntimeType);
            EnsureByClassName("UInt32", ref m_UInt32RuntimeType);
            EnsureByClassName("Int64", ref m_Int64RuntimeType);
            EnsureByClassName("UInt64", ref m_UInt64RuntimeType);
            EnsureByClassName("String", ref m_StringRuntimeType);
            EnsureByClassName("Float32", ref m_Float32RuntimeType);
            EnsureByClassName("Float64", ref m_Float64RuntimeType);

            // Ensure non-primitive system core runtime types are also registered.
            // VM may create them during global init / allocation (e.g., ArrayObject).
            EnsureRuntimeTypeRegisteredByClassName("Core.Object");
            EnsureRuntimeTypeRegisteredByClassName("Object");
            EnsureRuntimeTypeRegisteredByClassName("Core.Array");
            EnsureRuntimeTypeRegisteredByClassName("Array");
        }

        private static void EnsureRuntimeTypeRegisteredByClassName(string runtimeClassName)
        {
            if (string.IsNullOrWhiteSpace(runtimeClassName)) return;

            var rc = RuntimeClassManager.instance.GetRuntimeClassByName(runtimeClassName);
            if (rc == null)
            {
                // Prefer package-driven creation so RuntimeClass contains the full metadata.
                rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassByName(runtimeClassName);
            }

            if (rc == null)
            {
                // Fallback to minimal RuntimeClass while keeping id stable to reduce duplicates.
                var stableId = StableId32(runtimeClassName);
                rc = RuntimeClassManager.instance.GetRuntimeClassById(stableId)
                     ?? new RuntimeClass { id = stableId, name = runtimeClassName };
                if (RuntimeClassManager.instance.GetRuntimeClassById(stableId) == null)
                {
                    RuntimeClassManager.instance.m_IRMetaClassList.Add(rc);
                }
            }

            if (rc == null) return;
            if (GetRuntimeTypeByClassId(rc.id) == null)
            {
                AddRuntimeTypeByClass(rc);
            }
        }

        private static void EnsureByClassName(string runtimeClassName, ref RuntimeType targetField)
        {
            if (targetField != null) return;

            var rc = RuntimeClassManager.instance.GetRuntimeClassByName(runtimeClassName);
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
                    RuntimeClassManager.instance.m_IRMetaClassList.Add(rc);
                }
            }

            // If a runtime type already exists (possibly created via template-based path),
            // reuse it to avoid duplicate RuntimeType instances.
            var existed = GetRuntimeTypeByClassId(rc.id);
            if (existed != null)
            {
                targetField = existed;
                return;
            }

            targetField = AddRuntimeTypeByClass(rc);
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

        public static ClassObject CreateTypeObject(RuntimeType rt)
        {
            if (rt == null) return null;
            // Use the ObjectClass.GetObjectType to obtain/cached TypeObject
            try
            {
                // create a temporary object instance for this runtime type (no member init)
                SObject obj = ObjectManager.CreateObjectByRuntimeType(rt, false);
                if (obj == null) return null;
                // ObjectClass moved under SimpleLanguage.Lib; use that implementation
                var typeObj = SimpleLanguage.Lib.ObjectClass.GetObjectType(obj);
                return typeObj as ClassObject;
            }
            catch
            {
                return null;
            }
        }

        public static RuntimeType GetRuntimeTypeByMT(RuntimeClass rmc)
        {
            if (rmc == null) return null;
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == rmc.id);
        }
        public static RuntimeType GetRuntimeTypeByClassId( int id )
        {
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == id );
        }
        public static RuntimeType GetRuntimeTypeByMIRMetaType(RuntimeDefType irmt, bool isAdd = true )
        {
            if (irmt == null) return null;
            RuntimeType t =  GetRuntimeTypeByMT(irmt.runtimeClass);
            if( t == null && isAdd )
            {
                List<RuntimeType> rtlist = new List<RuntimeType>();
                for( int i = 0; i < irmt.runtimeDefTypeList.Count; i++ )
                {
                    var tc = GetRuntimeTypeByMIRMetaType(irmt.runtimeDefTypeList[i], isAdd);
                }

                t = AddRuntimeTypeByClassAndTemplate(irmt.runtimeClass, rtlist );
                t.EnsureStaticMemberObjectsInitialized();
            }
            return t;
        }
        public static RuntimeType GetRuntimeTypeByMTAndTemplateMT( RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
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
        public static RuntimeType GetRuntimeTypeByMTAndIRMetaClass(RuntimeClass rmc)
        {
            return GetRuntimeTypeByMT(rmc);
        }
        public static RuntimeType AddRuntimeTypeByClassAndTemplate(RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            if (rmc == null) return null;
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);
            s_RuntimeTypeList.Add(rt);
            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass(RuntimeClass rmc )
        {
            RuntimeType rt = new RuntimeType(rmc, null);

            string name = rmc.name;
            if (name == "Void" || name == "Core.Void" )
            {
                m_VoidRuntimeType = rt;
            }
            else if(name == "Type" || name == "Core.Type")
            {
                m_TypeRuntimeType = rt;
            }
            else if (name == "Bool" || name == "Core.Bool")
            {
                m_BoolRuntimeType = rt;
            }
            else if (name == "Num" || name == "Core.Num")
            {
                m_NumRuntimeType = rt;
            }
            else if (name == "Byte" || name == "Core.Byte" )
            {
                m_ByteRuntimeType = rt;
            }
            else if (name == "SByte" || name == "Core.SByte")
            {
                m_SByteRuntimeType = rt;
            }
            else if (name == "Int16" || name == "Core.Int16")
            {
                m_Int16RuntimeType = rt;
            }
            else if (name == "UInt16" || name == "Core.UInt16")
            {
                m_UInt16RuntimeType = rt;
            }
            else if (name == "Int32" || name == "Core.Int32")
            {
                m_Int32RuntimeType = rt;
            }
            else if (name == "UInt32" || name == "Core.UInt32")
            {
                m_UInt32RuntimeType = rt;
            }
            else if (name == "Int64" || name == "Core.Int64")
            {
                m_Int64RuntimeType = rt;
            }
            else if (name == "UInt64" || name == "Core.UInt64")
            {
                m_UInt64RuntimeType = rt;
            }
            else if (name == "String" || name == "Core.String")
            {
                m_StringRuntimeType = rt;
            }
            else if (name == "Float32" || name == "Core.Float32")
            {
                m_Float32RuntimeType = rt;
            }
            else if (name == "Float64" || name == "Core.Float64")
            {
                m_Float64RuntimeType = rt;
            }
            s_RuntimeTypeList.Add(rt);

            return rt;
        }
    }
}
