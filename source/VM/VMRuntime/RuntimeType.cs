//****************************************************************************
//  File:      RuntimeType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using SimpleLanguage.Parse;
using System.Runtime.CompilerServices;
using System.Text;
using SimpleLanguage.Logging;
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
            return GetClassRuntimeTypeCore(rdt, isAdd,
                new HashSet<RuntimeDefType>(RuntimeDefTypeReferenceComparer.Instance));
        }

        private sealed class RuntimeDefTypeReferenceComparer : IEqualityComparer<RuntimeDefType>
        {
            public static readonly RuntimeDefTypeReferenceComparer Instance = new();

            public bool Equals(RuntimeDefType? x, RuntimeDefType? y) => ReferenceEquals(x, y);
            public int GetHashCode(RuntimeDefType obj) => RuntimeHelpers.GetHashCode(obj);
        }

        private RuntimeType? GetClassRuntimeTypeCore(RuntimeDefType rdt, bool isAdd, HashSet<RuntimeDefType> visiting)
        {
            if (rdt == null)
            {
                return null;
            }

            if (!visiting.Add(rdt))
            {
                // Break cyclic template relation recursion; fallback to raw runtime class type.
                var raw = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(rdt.runtimeClass, new List<RuntimeType>());
                if (raw == null && isAdd)
                {
                    raw = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(rdt.runtimeClass, new List<RuntimeType>());
                }
                return raw;
            }

            try
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

                    return GetClassRuntimeTypeCore(mt, isAdd, visiting);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (rdt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < rdt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeTypeCore(rdt.runtimeDefTypeList[i], isAdd, visiting);
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
            finally
            {
                visiting.Remove(rdt);
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
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            if ( index >= m_StaticMemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                svalue.SetNull();
                return;
            }
            var ro = m_StaticMemberRuntimeObjectArray[index];
            if (ro == null)
            {
                svalue.SetNull();
                return;
            }
            // 与 ClassObject.GetMemberVariableSValue 一致：优先 m_MemberData 紧凑布局
            ro.SetSValueByRuntimeObjct(ref svalue);
        }
        public void SetStaticMemberVariableSValue(int index, SValue svalue)
        {
            if (m_StaticMemberRuntimeObjectArray == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            if (index >= m_StaticMemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            var target = m_StaticMemberRuntimeObjectArray[index];
            if (target == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Static member object at index {index} is null for runtime type {this}. EnsureStaticMemberObjectsInitialized should have been called.");
                return;
            }
            if (svalue.isNull)
            {
                target.SetNull();
                return;
            }
            target.SetSObjectBySValue(ref svalue);
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
            this.m_IsStaticExprBatchApplying = true;
            try
            {
                // 按 order（依赖解析次序）收集静态字段初始化指令，而不是按声明顺序。
                // order 来自 Front 的 MetaMemberVariable.parseOrder：被依赖的成员先获得较小 order，
                // 必须先执行其初始化。例如 x1 = x2 * 1 + -2、x2 = x3 + 4、x3 = 13 会按 x3 -> x2 -> x1 执行。
                List<Instruction> initIR = SLRuntimeModuleRegistry.GetStaticFieldInitializerExpressionsInOrder(m_RuntimeClass.id);

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

                //bool pushedRoot = false;
                //if (CLRVM.clrRuntimeStack.Count == 0)
                //{
                //    var root = new RuntimeVM("__static_field_init_root__", new List<Instruction>());
                //    CLRVM.PushCLRRuntime(root);
                //    pushedRoot = true;
                //}

                try
                {
                    var vm = CLRVM.CreateExeSplite($"__static_field_init__{m_RuntimeClass.name}", this.runtimeTemplateList, initIR);
                    //vm.isPersistent = true;
                    vm.Run(true);
                    CLRVM.PopCLRRuntime();
                }
                catch (Exception e)
                {
                    if (CLRVM.clrRuntimeStack.Count > 0)
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
        public bool IsExtendsRelation(RuntimeType rt)
        {
            if (rt == null) return false;
            if (!m_RuntimeClass.IsExtendsRelation(rt.m_RuntimeClass))
                return false;

            if (rt.m_RuntimeTemplateList == null || rt.m_RuntimeTemplateList.Count == 0)
                return true;

            RuntimeType selfAsTarget = TryBuildRelationRuntimeType(rt.m_RuntimeClass);
            if (selfAsTarget == null)
                return false;

            if (selfAsTarget.m_RuntimeTemplateList.Count != rt.m_RuntimeTemplateList.Count)
                return false;

            for (int i = 0; i < rt.m_RuntimeTemplateList.Count; i++)
            {
                if (!IsExactRuntimeType(selfAsTarget.m_RuntimeTemplateList[i], rt.m_RuntimeTemplateList[i]))
                    return false;
            }
            return true;
        }

        private RuntimeType TryBuildRelationRuntimeType(RuntimeClass targetClass)
        {
            if (targetClass == null)
                return null;

            if (m_RuntimeClass == targetClass)
                return this;

            var targetTemplateDefList = targetClass.templateDefTypeList;
            if (targetTemplateDefList == null || targetTemplateDefList.Count == 0)
            {
                return RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(targetClass, new List<RuntimeType>());
            }

            List<RuntimeType> relationTemplateList = new List<RuntimeType>();
            for (int i = 0; i < targetTemplateDefList.Count; i++)
            {
                var relationDef = m_RuntimeClass.GetRuntimeDefTypeByTemplateAndClassRelation(targetClass, i);
                if (relationDef == null)
                    return null;

                var relationType = RuntimeVM.GetRuntimeTypeByDefType(relationDef, m_RuntimeClass, m_RuntimeTemplateList, false);
                if (relationType == null)
                    return null;

                relationTemplateList.Add(relationType);
            }

            return RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(targetClass, relationTemplateList);
        }

        private static bool IsExactRuntimeType(RuntimeType rt1, RuntimeType rt2)
        {
            if (rt1 == null || rt2 == null)
                return false;
            if (rt1.m_RuntimeClass != rt2.m_RuntimeClass)
                return false;

            if (rt1.m_RuntimeTemplateList.Count != rt2.m_RuntimeTemplateList.Count)
                return false;

            for (int i = 0; i < rt1.m_RuntimeTemplateList.Count; i++)
            {
                if (!IsExactRuntimeType(rt1.m_RuntimeTemplateList[i], rt2.m_RuntimeTemplateList[i]))
                    return false;
            }
            return true;
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
}
