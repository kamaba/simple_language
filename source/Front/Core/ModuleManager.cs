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
            if( moduleName == "Core" )
            {
                m_CoreModule = m_SelfModule;
            }
            else
            {
                m_CoreModule = new MetaModule("Core");
            }
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
                Log.AddMetaCoreLog(LID.MetaCoreModuleIssue, "Error 严重错误，获取模式不传名称!!");
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

        /// <summary>
        /// 跨模块命名空间合并查找：同一逻辑命名空间可分布在多个模块
        /// （如 Std 模块贡献 SLang.Log，插件 refModule 贡献 SLang.Plugin.CSharpMono）。
        /// 从当前命中的命名空间节点出发，收集其不含模块名的逻辑路径，
        /// 再到 selfModule 与各引用模块的同名命名空间路径下查找 childName。
        /// 供调用链中段未命中时级联使用（如解析 SLang.Plugin.CSharpMono 的 Plugin 段）。
        /// </summary>
        public MetaNode FindChildrenInSameNamespaceAcrossModules( MetaNode fromNamespace, string childName )
        {
            if( fromNamespace == null || !fromNamespace.isMetaNamespace || string.IsNullOrEmpty(childName) )
                return null;

            /* 收集命名空间逻辑路径（不含模块名）：自身在最前，向外逐级追加 */
            var nsParts = new List<string>();
            MetaNode cur = fromNamespace;
            while( cur != null )
            {
                nsParts.Add(cur.name);
                var parent = cur.parentNode;
                if( parent == null || parent.isMetaModule )
                    break;
                cur = parent;
            }
            if( nsParts.Count == 0 )
                return null;

            MetaNode found = FindChildrenByNamespacePath(selfModule?.metaNode, nsParts, childName);
            if( found != null )
                return found;

            foreach( var v in m_ImportMetaModuleDict )
            {
                found = FindChildrenByNamespacePath(v.Value?.metaNode, nsParts, childName);
                if( found != null )
                    return found;
            }
            return null;
        }

        private static MetaNode FindChildrenByNamespacePath( MetaNode moduleRoot, List<string> nsParts, string childName )
        {
            if( moduleRoot == null )
                return null;

            /* nsParts[0] 是最内层命名空间名，从模块根按外→内顺序下钻 */
            MetaNode node = moduleRoot;
            for( int i = nsParts.Count - 1; i >= 0 && node != null; i-- )
            {
                node = node.GetChildrenMetaNodeByName(nsParts[i]);
            }
            return node?.GetChildrenMetaNodeByName(childName);
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
                Log.AddMetaCoreLog(LID.MetaCoreModuleModuleReplacedReference,
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
