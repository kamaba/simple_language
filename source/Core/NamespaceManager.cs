//****************************************************************************
//  File:      NamespaceManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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

        //type = 0 all namespace/class/data/enum   1 namespace  2class/data
        public MetaBase FindImportNamespace(FileMetaImportSyntax fmis, string name )
        {
            MetaBase parentNode = ModuleManager.instance.selfModule;

            MetaBase resultMB = null;
            for( int i = 0; i < fmis.namespaceStatement.namespaceList.Count; i++ )
            {
                resultMB = parentNode.GetChildrenMetaBaseByName(fmis.namespaceStatement.namespaceList[i]);
                if( resultMB != null )
                {
                    if( resultMB.name == name )
                    {
                        return resultMB;
                    }
                    parentNode = resultMB;
                } 
            }

            return null;
        }
        public MetaBase SearchTopLevelFileMetaNamespace(FileMetaNamespace fns, MetaBase parentNode = null)
        {
            MetaBase findNode = parentNode;
            if ( fns.topLevelFileMetaNamespace != null )
            {
                findNode = SearchTopLevelFileMetaNamespace(fns.topLevelFileMetaNamespace, findNode);
                for (int i = 0; i < fns.topLevelFileMetaNamespace.namespaceStatementBlock.tokenList.Count; i++)
                {
                    string name = fns.topLevelFileMetaNamespace.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                    var findNode2 = findNode.GetChildrenMetaBaseByName(name);
                    if( findNode2 == null )
                    {
                        break;
                    }
                    findNode = findNode2;
                }
            }
            return findNode;
        }
        public MetaNamespace GetParentChildrenNode(FileMetaNamespace fns, MetaBase parentNode )
        {
            var findNode = parentNode;
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++)
            {
                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                var findNode2 = findNode.GetChildrenMetaBaseByName(name);
                if (findNode2 == null)
                {
                    break;
                }
                findNode = findNode2;
            }
            return findNode as MetaNamespace;
        }
        public MetaNamespace SearchFinalNamespace(FileMetaNamespace fns )
        {
            MetaBase findNode = ModuleManager.instance.selfModule;


            List<FileMetaNamespace> list = new List<FileMetaNamespace>();

            FileMetaNamespace PS_fmn = fns;
            list.Add(PS_fmn);
            while (true)
            {
                if(PS_fmn.topLevelFileMetaNamespace != null )
                {
                    PS_fmn = PS_fmn.topLevelFileMetaNamespace;
                    list.Add(PS_fmn);
                }
                else
                {
                    break;
                }
            }

            FileMetaNamespace fmn = null;
            for( int i = list.Count -1; i >= 0 ; i-- )
            {
                fmn = list[i];
                findNode = GetParentChildrenNode(fmn, findNode);          
                if(findNode == null )
                {
                    break;
                }
            }

            return findNode as MetaNamespace;
        }
        public void CreateMetaNamespaceByFineDefineNamespace( FileMetaNamespace fns, MetaBase parentNode = null )
        {
            FileMetaNamespace fnsc = fns;
            if ( parentNode == null )
            {
                parentNode = ModuleManager.instance.selfModule;
            }
            parentNode = SearchTopLevelFileMetaNamespace(fns, parentNode);

            CreateMetaNamespaceHandle(fnsc, parentNode);
        }
        void CreateMetaNamespaceHandle(FileMetaNamespace fns, MetaBase parentNode = null)
        {
            MetaBase mb = parentNode;
            if (parentNode == null)
            {
                parentNode = ModuleManager.instance.selfModule;
            }
            //fns.metaNamespaceList.Clear();
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++)
            {
                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                mb = parentNode.GetChildrenMetaBaseByName(name);
                if (mb == null)
                {
                    mb = new MetaNamespace(name);
                    if (ProjectManager.useDefineNamespaceType != EUseDefineType.NoUseProjectConfigNamespace)
                    {
                        (mb as MetaNamespace).isNotAllowCreateName = true;
                        Debug.Write("Error 在使用namespace 时，在项目定义中，没有找到相关的定义!!  位置:" + fns.namespaceStatementBlock.tokenList[i].ToLexemeAllString());
                    }
                    parentNode.AddMetaBase(name, mb);
                    metaNamespaceDict.Add((mb as MetaNamespace).namespaceName, mb as MetaNamespace);
                    parentNode = mb;
                }
                else
                {
                    parentNode = mb;
                    //fns.metaNamespaceList.Add(mb as MetaNamespace);
                }
            }
        }
        public void CreateMetaNamespaceByFileMetaNamespace( FileMetaNamespace fmn )
        {
            MetaBase mn = null;
            if( fmn.topLevelFileMetaNamespace != null )
            {
                //mn = fmn.topLevelFileMetaNamespace.namespaceStatementBlock;
            }
            CreateMetaNamespaceByFineDefineNamespace(fmn, mn);
        }
        public MetaNamespace FindFinalMetaNamespaceByNSBlock( NamespaceStatementBlock nsb, MetaBase root = null )
        {
            if( nsb.namespaceList.Count == 0 )
            {
                return null;
            }

            if( root == null )
            {
                root = ModuleManager.instance.selfModule;
            }
            for (int i = 0; i < nsb.tokenList.Count; i++)
            {
                string name = nsb.tokenList[i].lexeme.ToString();
                var findNode2 = root.GetChildrenMetaBaseByName(name);
                if (findNode2 == null)
                {
                    break;
                }
                root = findNode2;
                if ( i == nsb.tokenList.Count - 1 )
                {
                    return findNode2 as MetaNamespace;
                }
            }
            return null;
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
                                Debug.Write("已有类: " + tempname + "与添加的命名空间冲突!!");
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
                                Debug.Write("已有类: " + tempname + "与添加的命名空间冲突!!");
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
                Debug.Write("NamespaceManager::AddNamespaceString 命名空间:" + nsString + "解析错误!!");
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
            Debug.Write("---------------NamespaceBegin-----------" + Environment.NewLine);
            Debug.Write(ToAllNamespace());
            Debug.Write("--------------NamespaceEnd-------------" + Environment.NewLine);
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
