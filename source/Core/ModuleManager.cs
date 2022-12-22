using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class ModuleManager
    {
        public static ModuleManager s_Instance = null;
        public static ModuleManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ModuleManager();
                }
                return s_Instance;
            }
        }

        public string moduleName = "S";
        public const string slCoreModuleName = "SLCore";
        public const string csharpModuleName = "CSharp";

        public MetaModule selfModule = null;
        public MetaModule coreModule = null;
        public MetaModule csharpModule = null;
        public Dictionary<string, MetaModule> outerMetaModuleDict = new Dictionary<string, MetaModule>();

        public Dictionary<string, MetaModule> m_AllMetaModuleDict = new Dictionary<string, MetaModule>();
        public ModuleManager()
        {
            selfModule = new MetaModule(moduleName);
            coreModule = new MetaModule(slCoreModuleName);
            csharpModule = new MetaModule(csharpModuleName);
            csharpModule.SetRefFromType(RefFromType.CSharp);
            m_AllMetaModuleDict.Add(moduleName, selfModule);
            m_AllMetaModuleDict.Add(slCoreModuleName, coreModule);
            m_AllMetaModuleDict.Add(csharpModuleName, csharpModule);
        }
        public MetaModule GetMetaModuleOrRetSelfModuleByName( string name )
        {
            MetaModule mm = GetMetaModuleByName(name);
            if (mm == null) return selfModule;
            else return mm;
        }
        public MetaModule GetMetaModuleByName( string name )
        {
            if( string.IsNullOrEmpty( name ) )
            {
                Console.WriteLine("Error 严重错误，获取模式不传名称!!");
                return null;
            }
            if(m_AllMetaModuleDict.ContainsKey( name ) )
            {
                return m_AllMetaModuleDict[name];
            }
            return null;
        }
        public MetaBase GetChildrenMetaBaseByName( string name )
        {
            MetaBase m2 = selfModule.GetChildrenMetaBaseByName(name);
            if (m2 != null)
            {
                return m2;
            }
            return coreModule.GetChildrenMetaBaseByName(name);
        }
        public void AddMetaMdoule( MetaModule mm )
        {
            if( outerMetaModuleDict.ContainsKey( mm.name ) )
            {
                return;
            }
            outerMetaModuleDict.Add(mm.name, mm);
            if( m_AllMetaModuleDict.ContainsKey( mm.name ) )
            {
                Console.WriteLine("Error 严重错误，模块有重名!!!");
                return;
            }
            m_AllMetaModuleDict.Add(mm.name, mm);
        }

        public string ToFormatString()
        {
            return selfModule.ToFormatString();
        }
    }
}
