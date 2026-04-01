using SimpleLanguage.Logging;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public class RuntimeClass
    {
        public int id { get; set; } = 0;
        public string name { get; set; } = "";
        /// <summary>0=Class, 1=Enum, 2=Data — from exported SLIR class metadata.</summary>
        public int metaClassKind { get; set; }
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

        internal void AddNonStaticMethod(RuntimeMethod m)
        {
            if (m == null) return;
            m_NotStaticMethodList.Add(m);
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
                Debug.Assert(false);
                Log.AddVM(EError.None, "GetIRMethodByIndex is null");
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
        /// Installs template bindings from exported <c>templateRelationList</c> (related class → template index → bound type).
        /// </summary>
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

        public bool IsExtendsRelation(RuntimeClass rc)
        {
            if (this == rc )
            {
                return true;
            }
            if (m_IRMetaClassMapTemplateDict.ContainsKey(rc.id))
            {
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
