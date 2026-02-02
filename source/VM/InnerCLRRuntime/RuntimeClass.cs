using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    public class RuntimeClass
    {
        public int id { get; set; } = 0;
        public string name { get; set; } = "";
        public List<RuntimeVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<RuntimeVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;
        public List<RuntimeDefType> runtimeDefTypeList => m_RuntimeDefTypeList;
        public List<Instruction> memberVariableSetValueList=> m_MemberVariableSetValueList;

        private List<RuntimeVariable> m_LocalIRMetaVariableList = new List<RuntimeVariable>();
        private List<RuntimeVariable> m_StaticIRMetaVariableList = new List<RuntimeVariable>();
        private List<RuntimeDefType> m_RuntimeDefTypeList = new List<RuntimeDefType>();
        private List<Instruction> m_MemberVariableSetValueList = new List<Instruction>();


        private List<RuntimeMethod> m_NotStaticMethodList = new List<RuntimeMethod>();
        private List<RuntimeMethod> m_OperatorMethodList = new List<RuntimeMethod>();

        Dictionary<int, Dictionary<int, RuntimeDefType>> m_IRMetaClassMapTemplateDict = new Dictionary<int, Dictionary<int, RuntimeDefType>>();


        public RuntimeClass( IRMetaClass irmc )
        {
            //irmc.CreateStaticMetaMetaVariableIRList
        }
        public ClassObject GetStaticMetaMemberVaraible( int index )
        {
            //if( index < 0 || index >= m_StaticMetaMemberVariableArray.Length )
            //{
            //    return null;
            //}
            //return m_StaticMetaMemberVariableArray[index];
            return null;
        }
        public RuntimeMethod GetNonStaticMethodByIndex(int index)
        {
            if (index >= m_NotStaticMethodList.Count || index < 0)
            {
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
        public RuntimeDefType GetRuntimeDefTypeByTemplateAndClassRelation( RuntimeClass irmc, int index)
        {
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
        public static RuntimeClassManager s_Instance = null;
        public static RuntimeClassManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new RuntimeClassManager();
                }
                return s_Instance;
            }
        }
        Dictionary<int, RuntimeClass> m_RuntimeClassDict = new Dictionary<int, RuntimeClass>();

        public List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();


        public void RegisterDymnicClass()
        {

        }
        public RuntimeClass GetRuntimeClassById( int id )
        {
            if (m_RuntimeClassDict.ContainsKey(id))
            {
                return m_RuntimeClassDict[id];
            }
            return null;

        }
        public RuntimeClass GetRuntimeClassByName(string allname)
        {
            //return m_RuntimeClassDict.Find(a => a.name == allname);
            return null;
        }

        //public IRMetaType GetIRMetaClass( IRMetaClass metaclass, List<IRMetaClass> templateList, bool isNonIncludeAndRegister = false )
        //{
        //    IRMetaClass irmc = null;

        //    foreach( var v in m_IRMetaClassList )
        //    {
        //        if( v.irmeta)
        //    }
        //}

        public ClassObject GetStaticMetaMemberVariable( int classid, int index )
        {
            if(m_RuntimeClassDict.ContainsKey(classid) )
            {
                //m_RuntimeClassDict[classid].GetStaticMetaMemberVaraible(index);
            }

            return null;
        }
    }
}
