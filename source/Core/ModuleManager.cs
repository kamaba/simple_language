//****************************************************************************
//  File:      ModuleManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Parse;
using System.Collections.Generic;

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
        public const string csharpModuleName = "CSharp";
        public const string coreModuleName = "Core";

        public MetaModule selfModule => m_SelfModule;
        public MetaModule coreModule => m_CoreModule;
        public MetaModule csharpModule => m_CSharpModule;
        private Dictionary<string, MetaModule> m_ImportMetaModuleDict = new Dictionary<string, MetaModule>();

        public Dictionary<string, MetaModule> m_AllMetaModuleDict = new Dictionary<string, MetaModule>();


        private MetaModule m_SelfModule = null;
        private MetaModule m_CoreModule = null;
        private MetaModule m_CSharpModule = null;
        public ModuleManager()
        {
            m_SelfModule = new MetaModule(moduleName);
            m_CoreModule = new MetaModule(coreModuleName);
            m_CSharpModule = new MetaModule(csharpModuleName);
            m_CSharpModule.SetRefFromType(RefFromType.CSharp);
            m_AllMetaModuleDict.Add(moduleName, m_SelfModule);
            m_AllMetaModuleDict.Add(coreModuleName, m_CoreModule);
            m_AllMetaModuleDict.Add(csharpModuleName, m_CSharpModule);
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
                Log.AddInStructMeta(EError.None, "Error 严重错误，获取模式不传名称!!");
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
            if(m_ImportMetaModuleDict.ContainsKey( mm.name ) )
            {
                return;
            }
            m_ImportMetaModuleDict.Add(mm.name, mm);
            if( m_AllMetaModuleDict.ContainsKey( mm.name ) )
            {
                Log.AddInStructMeta(EError.None, "Error 严重错误，模块有重名!!!");
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
