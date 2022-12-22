using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class NamespaceManager
    {
        public static NamespaceManager s_Instance = null;
        public static NamespaceManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new NamespaceManager();
                }
                return s_Instance;
            }
        }
        public Dictionary<string, MetaNamespace> metaNamespaceDict = new Dictionary<string, MetaNamespace>();
       
        public MetaNamespace GetNamespaceByString( string nsString )
        {
            if( string.IsNullOrEmpty( nsString ) )
            {
                return null;
            }
            if (metaNamespaceDict.ContainsKey(nsString))
            {
                return metaNamespaceDict[nsString];
            }
            return null;
        }
        public void CreateMetaNamespaceByFineDefineNamespace( FileDefineNamespace fns, MetaBase parentNode = null )
        {
            MetaBase mb = parentNode;
            if( parentNode == null )
            {
                parentNode = ModuleManager.instance.selfModule;
            }
            fns.metaNamespaceList.Clear();
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++ )
            {
                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                mb = parentNode.GetChildrenMetaBaseByName(name);
                if (mb == null)
                {
                    mb = new MetaNamespace(name);
                    if (ProjectManager.isUseDefineNamespace)
                    {
                        Console.WriteLine("Error 在使用namespace 时，在项目定义中，没有找到相关的定义!!  位置:" + fns.namespaceStatementBlock.tokenList[i].ToLexemeAllString());
                        (mb as MetaNamespace).isNotAllowCreateName = true;
                    }
                    parentNode.AddMetaBase(name, mb);

                    metaNamespaceDict.Add((mb as MetaNamespace).namespaceName, mb as MetaNamespace);
                }
               
                parentNode = mb;
                fns.metaNamespaceList.Add(mb as MetaNamespace);
            }
        }
        public void CreateMetaNamespaceByFileMetaNamespace( FileMetaNamespace fmn )
        {
            MetaBase mn = null;
            if( fmn.topLevelFileMetaNamespace != null )
            {
                mn = fmn.topLevelFileMetaNamespace.namespaceStatementBlock.lastMetaNamespace;
            }
            CreateMetaNamespaceByFineDefineNamespace(fmn, mn);
        }
        public void AddNamespaceString( string nsString )
        {
            if( metaNamespaceDict.ContainsKey( nsString ) )
            {
                return;
            }
            List<string> list = new List<string>();
            MetaModule selfModule = ModuleManager.instance.selfModule;
            string tempname = "";
            if ( CompilerUtil.CheckNameList(nsString, list))
            {
                MetaNamespace parentMetaNamespace = null;
                for ( int i = 0; i < list.Count; i++ )
                {
                    tempname = list[i];
                    if ( i == 0 )
                    {
                        var metabase = selfModule.GetChildrenMetaBaseByName(tempname);
                        if (metabase != null)
                        {
                            parentMetaNamespace = metabase as MetaNamespace;
                            if(parentMetaNamespace == null )
                            {
                                Console.WriteLine("已有类: " + tempname + "与添加的命名空间冲突!!");
                                return;
                            }
                        }
                        else
                        {
                            parentMetaNamespace = new MetaNamespace(tempname);
                            selfModule.AddMetaNamespace(parentMetaNamespace);
                        }                
                    }
                    else
                    {
                        var metabase = parentMetaNamespace.GetChildrenMetaBaseByName(tempname);
                        if( metabase != null )
                        {
                            parentMetaNamespace = metabase as MetaNamespace;
                            if (parentMetaNamespace == null)
                            {
                                Console.WriteLine("已有类: " + tempname + "与添加的命名空间冲突!!");
                                return;
                            }
                        }
                        else
                        {
                            var mn = new MetaNamespace(tempname);
                            parentMetaNamespace.AddMetaNamespace(mn);
                            parentMetaNamespace = mn;
                        }
                    }
                }
                metaNamespaceDict.Add(nsString, parentMetaNamespace);
            }
            else
            {
                Console.WriteLine("NamespaceManager::AddNamespaceString 命名空间:" + nsString + "解析错误!!");
                return;
            }
        }
        public static MetaBase FindMetaBaseByNamespaceToParentAndName( MetaNamespace mn, string nodeName )
        {
            MetaBase cur = mn;
            MetaBase childMB = null;
            while( cur != null )
            {
                childMB = cur.GetChildrenMetaBaseByName(nodeName);
                if (childMB != null)
                    return childMB;
                cur = cur.parentNode;
            }
            return childMB;
        }

        public void PrintAllNamespace()
        {
            Console.Write("---------------NamespaceBegin-----------" + Environment.NewLine);
            Console.Write(ToAllNamespace());
            Console.Write("--------------NamespaceEnd-------------" + Environment.NewLine);
        }
        public string ToAllNamespace()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in metaNamespaceDict )
            {
                sb.Append("namespace " + v.Key + Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
