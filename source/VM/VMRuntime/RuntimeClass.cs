using SimpleLanguage.Logging;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeClass
    {
        public int id { get; set; } = 0;
        public string name { get; set; } = "";
        /// <summary>Name of the module this class belongs to (set during SLRuntimeModuleRegistry registration).</summary>
        public string moduleName { get; private set; } = "";
        /// <summary>完整名称（fullName），包含命名空间路径，用于 GetRuntimeClassByName 唯一匹配。</summary>
        public string allName { get; set; } = "";
        /// <summary>0=Class, 1=Enum, 2=Data 3=Interface from exported SLIR class metadata.</summary>
        public int metaClassKind { get; set; }
        /// <summary>True when this runtime class comes from anonymous/dynamic data export.</summary>
        public bool isDynamicData { get; set; }
        /// <summary>IR class id of the base/extend class; 0 if none. Matches Front export.</summary>
        public int baseClassId { get; set; }
        /// <summary>IR generated template meta type count exported from Front <c>IRMetaClass.templateCount</c>.</summary>
        public int templateCount { get; set; }
        /// <summary>Declared template parameter count in source (e.g. <c>Foo&lt;T,U&gt;</c> => 2).</summary>
        public int templateParameterCount { get; set; }
        /// <summary>Set after <c>fieldList</c> from SLIR package is applied to this class (see SLRuntimeModuleRegistry).</summary>
        internal bool fieldsFromPackageApplied { get; set; }
        public List<RuntimeVariable> nonStaticIRMetaVariableList => m_NonStaticIRMetaVariableList;
        public List<RuntimeVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;
        /// <summary>Exported class-level generated template type list from SLIR <c>templateTypeList</c>.</summary>
        public List<RuntimeDefType> templateDefTypeList => m_TemplateDefTypeList;
        //public List<RuntimeDefType> runtimeDefTypeList => m_RuntimeDefTypeList;
        public List<Instruction> nonStaticMemberVariableSetValueList => m_NonStaticMemberVariableSetValueList;
        public List<Instruction> staticMemberVariableSetValueList => m_StaticMemberVariableSetValueList; 

        private List<RuntimeVariable> m_NonStaticIRMetaVariableList = new List<RuntimeVariable>();
        private List<RuntimeVariable> m_StaticIRMetaVariableList = new List<RuntimeVariable>();
        private List<RuntimeDefType> m_TemplateDefTypeList = new List<RuntimeDefType>();
        //private List<RuntimeDefType> m_RuntimeDefTypeList = new List<RuntimeDefType>();
        private List<Instruction> m_NonStaticMemberVariableSetValueList = new List<Instruction>();
        private List<Instruction> m_StaticMemberVariableSetValueList = new List<Instruction>();

        private List<RuntimeMethod> m_NotStaticMethodList = new List<RuntimeMethod>();
        private List<RuntimeMethod> m_OperatorMethodList = new List<RuntimeMethod>();
        private Dictionary<int, Dictionary<int, RuntimeDefType>> m_IRMetaClassMapTemplateDict = new Dictionary<int, Dictionary<int, RuntimeDefType>>();
        private readonly List<int> m_ImplementsInterfaceIdList = new List<int>();
        /// <summary>字段别名（@Nickname），alias name -> RuntimeVariable。</summary>
        private Dictionary<string, RuntimeVariable> m_FieldAliasDict = new Dictionary<string, RuntimeVariable>(System.StringComparer.Ordinal);
        /// <summary>方法别名（@Nickname），alias name -> RuntimeMethod。</summary>
        private Dictionary<string, RuntimeMethod> m_MethodAliasDict = new Dictionary<string, RuntimeMethod>(System.StringComparer.Ordinal);

        internal void AddNonStaticMethod(RuntimeMethod m)
        {
            if (m == null) return;
            m_NotStaticMethodList.Add(m);
        }

        internal void SetModuleName(string moduleName)
        {
            this.moduleName = moduleName ?? string.Empty;
            UpdateAllName();
        }

        /// <summary>根据 moduleName 和 name 重新计算 allName（回退方案，优先用 SetAllName）。</summary>
        internal void UpdateAllName()
        {
            if (string.IsNullOrEmpty(allName))
                allName = string.IsNullOrEmpty(moduleName) ? name : moduleName + "." + name;
        }

        /// <summary>直接设置 allName（优先使用 pkg.fullName，包含完整命名空间路径）。</summary>
        internal void SetAllName(string fullName)
        {
            allName = fullName ?? string.Empty;
        }

        internal void ClearBoundMethods()
        {
            m_NotStaticMethodList.Clear();
            m_OperatorMethodList.Clear();
            m_MethodAliasDict.Clear();
        }

        internal void ClearFieldRuntimeState()
        {
            m_NonStaticIRMetaVariableList.Clear();
            m_StaticIRMetaVariableList.Clear();
            m_NonStaticMemberVariableSetValueList.Clear();
            m_StaticMemberVariableSetValueList.Clear();
            m_FieldAliasDict.Clear();
        }
        internal void AddNonStaticMemberVariableSetValueList(Instruction item)
        {
            m_NonStaticMemberVariableSetValueList.Add(item);
        }
        internal void AddNonStaticIRMetaVariableList(RuntimeVariable item)
        {
            m_NonStaticIRMetaVariableList.Add(item);
        }
        internal void AddStaticIRMetaVariableList(RuntimeVariable item)
        {
            m_StaticIRMetaVariableList.Add(item);
        }
        
        internal void AddOperatorMethod(RuntimeMethod m)
        {
            if (m == null) return;
            m_OperatorMethodList.Add(m);
        }

        /// <summary>注册字段别名（@Nickname），使通过别名也能查到同一个 RuntimeVariable。</summary>
        internal void RegisterFieldAlias(string aliasName, RuntimeVariable rv)
        {
            if (string.IsNullOrEmpty(aliasName) || rv == null) return;
            if (!m_FieldAliasDict.ContainsKey(aliasName))
                m_FieldAliasDict[aliasName] = rv;
        }

        /// <summary>注册方法别名（@Nickname），使通过别名也能查到同一个 RuntimeMethod。</summary>
        internal void RegisterMethodAlias(string aliasName, RuntimeMethod rm)
        {
            if (string.IsNullOrEmpty(aliasName) || rm == null) return;
            if (!m_MethodAliasDict.ContainsKey(aliasName))
                m_MethodAliasDict[aliasName] = rm;
        }

        public RuntimeMethod GetNonStaticMethodByIndex(int index)
        {
            if (index >= m_NotStaticMethodList.Count || index < 0)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert,"GetIRMethodByIndex is null");
                return null;
            }
            return m_NotStaticMethodList[index];
        }
        public RuntimeMethod GetOperatorMethodIndexByMethod(string name, out int index)
        {
            index = -1;
            for (int i = 0; i < m_OperatorMethodList.Count; i++)
            {
                if (m_OperatorMethodList[i].onlyFunctionName == name)
                {
                    index = i;
                    return m_OperatorMethodList[i];
                }
            }
            return null;
        }
        public RuntimeMethod GetNonStaticMethodIndexByName(string name, out int index)
        {
            index = -1;
            for (int i = 0; i < m_NotStaticMethodList.Count; i++)
            {
                if (m_NotStaticMethodList[i].onlyFunctionName == name)
                {
                    index = i;
                    return m_NotStaticMethodList[i];
                }
            }
            // Check method aliases (@Nickname)
            if (m_MethodAliasDict.TryGetValue(name, out var aliased) && aliased != null)
            {
                for (int i = 0; i < m_NotStaticMethodList.Count; i++)
                {
                    if (ReferenceEquals(m_NotStaticMethodList[i], aliased))
                    {
                        index = i;
                        return aliased;
                    }
                }
            }
            return null;
        }
        public RuntimeDefType? GetRuntimeDefTypeByTemplateAndClassRelation( RuntimeClass? irmc, int index)
        {
            if (irmc == null)
                return null;
            if (m_IRMetaClassMapTemplateDict.ContainsKey(irmc.id))
            {
                var irmcmap = m_IRMetaClassMapTemplateDict[irmc.id];
                if (irmcmap != null)
                {
                    if (irmcmap.ContainsKey(index))
                    {
                        return irmcmap[index];
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Installs template bindings from exported <c>templateRelationList</c> (related class 鈫?template index 鈫?bound type).
        /// </summary>
        public void EnsureTemplateRelationClass(int relatedClassId)
        {
            if (relatedClassId == 0) return;
            if (!m_IRMetaClassMapTemplateDict.ContainsKey(relatedClassId))
            {
                m_IRMetaClassMapTemplateDict[relatedClassId] = new Dictionary<int, RuntimeDefType>();
            }
        }

        public void SetTemplateRelation(int relatedClassId, int templateIndex, RuntimeDefType? binding)
        {
            if (binding == null) return;
            if (!m_IRMetaClassMapTemplateDict.TryGetValue(relatedClassId, out var map) || map == null)
            {
                map = new Dictionary<int, RuntimeDefType>();
                m_IRMetaClassMapTemplateDict[relatedClassId] = map;
            }
            map[templateIndex] = binding;
        }

        /// <summary>True when <see cref="metaClassKind"/> is <c>3</c> (interface) from the SLIR package.</summary>
        public bool isInterfaceClass => metaClassKind == 3;

        public bool ImplementsInterfaceId(int interfaceClassId)
        {
            if (interfaceClassId == 0) return false;
            for (int i = 0; i < m_ImplementsInterfaceIdList.Count; i++)
            {
                if (m_ImplementsInterfaceIdList[i] == interfaceClassId) return true;
            }
            return false;
        }

        public bool ImplementsInterface(RuntimeClass interfaceClass)
        {
            if (interfaceClass == null || !interfaceClass.isInterfaceClass)
                return false;

            if (ImplementsInterfaceId(interfaceClass.id))
                return true;

            string targetName = GetGenericDefinitionName(interfaceClass.name);
            for (int i = 0; i < m_ImplementsInterfaceIdList.Count; i++)
            {
                var implClass = RuntimeClassManager.GetRuntimeClassById(m_ImplementsInterfaceIdList[i]);
                if (implClass == null) continue;
                if (!implClass.isInterfaceClass) continue;

                string implName = GetGenericDefinitionName(implClass.name);
                if (string.Equals(implName, targetName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string GetGenericDefinitionName(string className)
        {
            if (string.IsNullOrEmpty(className)) return string.Empty;
            int index = className.IndexOf('<');
            return index >= 0 ? className.Substring(0, index) : className;
        }

        internal void AddImplementsInterfaceId(int interfaceClassId)
        {
            if (interfaceClassId == 0) return;
            for (int i = 0; i < m_ImplementsInterfaceIdList.Count; i++)
            {
                if (m_ImplementsInterfaceIdList[i] == interfaceClassId) return;
            }
            m_ImplementsInterfaceIdList.Add(interfaceClassId);
        }

        public bool IsExtendsRelation(RuntimeClass rc)
        {
            if (rc == null)
                return false;

            var visited = new HashSet<int>();
            return IsExtendsRelationInternal(rc, visited);
        }

        private bool IsExtendsRelationInternal(RuntimeClass rc, HashSet<int> visited)
        {
            if (rc == null)
                return false;
            if (this == rc)
                return true;
            if (!visited.Add(this.id))
                return false;

            if (m_IRMetaClassMapTemplateDict.ContainsKey(rc.id))
                return true;

            if (rc.isInterfaceClass && ImplementsInterfaceId(rc.id))
                return true;

            foreach (var rel in m_IRMetaClassMapTemplateDict)
            {
                var directBase = RuntimeClassManager.GetRuntimeClassById(rel.Key);
                if (directBase == null || directBase == this)
                    continue;
                if (directBase.IsExtendsRelationInternal(rc, visited))
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(moduleName);
            sb.Append(":");
            sb.Append(name);

            if(templateCount > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < templateCount; i++ )
                {
                    sb.Append(m_TemplateDefTypeList[i].ToString());
                }
                sb.Append(">");
            }


            return sb.ToString();
        }
    }
    public class RuntimeClassManager
    {
        private static List<RuntimeClass> m_IRMetaClassList = new List<RuntimeClass>();
        /// <summary>别名到 RuntimeClass 的映射（@Nickname），同一个 RuntimeClass 可有多个名称。</summary>
        private static Dictionary<string, RuntimeClass> m_AliasDict = new Dictionary<string, RuntimeClass>(System.StringComparer.Ordinal);
        public static void RegisterDymnicClass()
        {
        }
        public static RuntimeClass GetRuntimeClassById( int id )
        {
            return m_IRMetaClassList.Find(a => a.id == id);
        }
        public static RuntimeClass GetRuntimeClassByName(string allname)
        {
            if (m_AliasDict.TryGetValue(allname, out var aliased))
                return aliased;
            return m_IRMetaClassList.Find(a => a.allName == allname);
        }

        /// <summary>按短名（name 字段）查找，可能匹配到多个，返回第一个。</summary>
        public static RuntimeClass? GetRuntimeClassByShortName(string shortName)
        {
            return m_IRMetaClassList.Find(a => a.name == shortName);
        }
        public static RuntimeClass AddRuntimeClass( RuntimeClass rc )
        {
            m_IRMetaClassList.Add(rc);
            return rc;
        }

        /// <summary>注册别名（@Nickname），使通过别名也能查到同一个 RuntimeClass。</summary>
        public static void RegisterClassAlias(string aliasFullName, RuntimeClass rc)
        {
            if (string.IsNullOrEmpty(aliasFullName) || rc == null) return;
            if (!m_AliasDict.ContainsKey(aliasFullName))
                m_AliasDict[aliasFullName] = rc;
        }
    }
}
