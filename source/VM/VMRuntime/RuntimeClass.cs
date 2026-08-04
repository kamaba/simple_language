using SimpleLanguage.Logging;

namespace SimpleLanguage.VM
{
    public class RuntimeClass
    {
        public int id { get; set; } = 0;
        public string name { get; set; } = "";
        /// <summary>Name of the module this class belongs to (set during SLRuntimeModuleRegistry registration).</summary>
        public string moduleName { get; private set; } = "";
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

        internal void AddNonStaticMethod(RuntimeMethod m)
        {
            if (m == null) return;
            m_NotStaticMethodList.Add(m);
        }

        internal void SetModuleName(string name)
        {
            moduleName = name ?? string.Empty;
        }

        internal void ClearBoundMethods()
        {
            m_NotStaticMethodList.Clear();
            m_OperatorMethodList.Clear();
        }

        internal void ClearFieldRuntimeState()
        {
            m_NonStaticIRMetaVariableList.Clear();
            m_StaticIRMetaVariableList.Clear();
            m_NonStaticMemberVariableSetValueList.Clear();
            m_StaticMemberVariableSetValueList.Clear();
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
    }
    public class RuntimeClassManager
    {
        private static List<RuntimeClass> m_IRMetaClassList = new List<RuntimeClass>();
        public static void RegisterDymnicClass()
        {
        }
        public static RuntimeClass GetRuntimeClassById( int id )
        {
            return m_IRMetaClassList.Find(a => a.id == id);
        }
        public static RuntimeClass GetRuntimeClassByName(string allname)
        {
            return m_IRMetaClassList.Find(a => a.name == allname);
        }
        public static RuntimeClass AddRuntimeClass( RuntimeClass rc )
        {
            m_IRMetaClassList.Add(rc);
            return rc;
        }
    }
}
