//****************************************************************************
//  File:      ModuleManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class ModuleManager
    {
        public static ModuleManager instance = new ModuleManager();
        public MetaModule selfModule => m_SelfModule;
        public MetaModule coreModule => m_CoreModule;
        public Dictionary<string, MetaModule> importMetaModuleDict => m_ImportMetaModuleDict;
        public MetaModule csharpLangRegisterModule => m_CSharpLangRegisterModule;
        public MetaModule clangRegisterModule => m_CLangRegisterModule;

        private Dictionary<string, MetaModule> m_ImportMetaModuleDict = new Dictionary<string, MetaModule>();

        public Dictionary<string, MetaModule> m_AllMetaModuleDict = new Dictionary<string, MetaModule>();


        private MetaModule m_SelfModule = null;
        private MetaModule m_CoreModule = null;
        private MetaModule m_CSharpLangRegisterModule = null;
        private MetaModule m_CLangRegisterModule = null;
        private MetaModule m_JavaLangRegisterModule = null;
        public ModuleManager()
        {
        }
        public void InitSelfModuleManager( string moduleName )
        {
            m_SelfModule = new MetaModule(moduleName);
            m_CoreModule = m_SelfModule;
            m_CSharpLangRegisterModule = new MetaModule("CSharp");
            m_CLangRegisterModule = new MetaModule("CLang");
            m_JavaLangRegisterModule = new MetaModule("Java");
            m_AllMetaModuleDict.Add(moduleName, m_SelfModule);
            m_AllMetaModuleDict.Add("CSharp", m_CSharpLangRegisterModule);
            m_AllMetaModuleDict.Add("CLang", m_CLangRegisterModule);
            m_AllMetaModuleDict.Add("Java", m_JavaLangRegisterModule);
            selfModule.SetDeep(0);
            m_CoreModule.SetDeep(0);
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
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 严重错误，获取模式不传名称!!");
                return null;
            }
            if(m_AllMetaModuleDict.ContainsKey( name ) )
            {
                return m_AllMetaModuleDict[name];
            }
            return null;
        }
        public MetaNode GetChildrenMetaNodeByName( string name )
        {
            if( name == "Core" )
            {
                return coreModule.metaNode;
            }
            MetaNode m2 = selfModule.metaNode.GetChildrenMetaNodeByName(name);
            if (m2 != null)
            {
                return m2;
            }
            foreach( var v in m_ImportMetaModuleDict )
            {
                m2 = v.Value.metaNode.GetChildrenMetaNodeByName(name);
                if( m2 != null )
                {
                    return m2;
                }
            }
            return null;
        }
        public void AddMetaMdoule( MetaModule mm )
        {
            if( mm == null ) return;

            // Replace existing module with the same name (e.g. default Core
            // module created by InitSelfModuleManager gets replaced by the
            // real one loaded from references).
            if( m_ImportMetaModuleDict.ContainsKey( mm.name ) )
            {
                m_ImportMetaModuleDict[mm.name] = mm;
            }
            else
            {
                m_ImportMetaModuleDict.Add(mm.name, mm);
            }

            if( m_AllMetaModuleDict.ContainsKey( mm.name ) )
            {
                m_AllMetaModuleDict[mm.name] = mm;
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    $"Module '{mm.name}' replaced by reference loading.");
            }
            else
            {
                m_AllMetaModuleDict.Add(mm.name, mm);
            }

            // If a "Core" module is loaded from references, update coreModule
            // to point to the real one (instead of the self module stub).
            if( mm.name == "Core" && mm != m_SelfModule )
            {
                m_CoreModule = mm;
            }
        }

        public string ToFormatString()
        {
            return selfModule.ToFormatString();
        }
    }
}
