//****************************************************************************
//  File:      NamespaceManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;

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

        public MetaNode SearchTopLevelFileMetaNamespace(FileMetaNamespace fns, MetaNode parentNode = null)
        {
            MetaNode findNode = parentNode;
            if ( fns.topLevelFileMetaNamespace != null )
            {
                findNode = SearchTopLevelFileMetaNamespace(fns.topLevelFileMetaNamespace, findNode);
                for (int i = 0; i < fns.topLevelFileMetaNamespace.namespaceStatementBlock.tokenList.Count; i++)
                {
                    string name = fns.topLevelFileMetaNamespace.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                    var findNode2 = findNode.GetChildrenMetaNodeByName(name);
                    if( findNode2 == null )
                    {
                        break;
                    }
                    findNode = findNode2;
                }
            }
            return findNode;
        }
        public MetaNode GetParentChildrenNode(FileMetaNamespace fns, MetaNode parentNode )
        {
            var findNode = parentNode;
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++)
            {
                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                var findNode2 = findNode.GetChildrenMetaNodeByName(name);
                if (findNode2 == null)
                {
                    break;
                }
                findNode = findNode2;
            }
            return findNode;
        }
        public MetaNode SearchFinalNamespace(FileMetaNamespace fns )
        {
            MetaNode findNode = ModuleManager.instance.selfModule.metaNode;

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

            return findNode;
        }
        public void CreateMetaNamespaceByFineDefineNamespace( FileMetaNamespace fns, MetaNode parentNode = null )
        {
            FileMetaNamespace fnsc = fns;
            if ( parentNode == null )
            {
                parentNode = ModuleManager.instance.selfModule.metaNode;
            }
            parentNode = SearchTopLevelFileMetaNamespace(fns, parentNode);

            CreateMetaNamespaceHandle(fnsc, parentNode);
        }
        void CreateMetaNamespaceHandle(FileMetaNamespace fns, MetaNode parentNode = null)
        {
            MetaNode mnode = parentNode;
            if (parentNode == null)
            {
                parentNode = ModuleManager.instance.selfModule.metaNode;
            }
            //fns.metaNamespaceList.Clear();
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++)
            {
                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                parentNode = parentNode.GetChildrenMetaNodeByName(name);
                bool isCreate = true;
                if (parentNode != null)
                {
                    if (parentNode.metaNamespace == null)
                    {
                        isCreate = true;
                    }
                    else
                    {
                        isCreate = false;
                    }
                }
                else
                {
                    isCreate = true;
                }
                if( isCreate )
                {
                    var mn = new MetaNamespace(name);
                    if (ProjectManager.useDefineNamespaceType != EUseDefineType.NoUseProjectConfigNamespace)
                    {
                        mn.isNotAllowCreateName = true;
                        Log.AddInStructMeta(EError.None, "Error 在使用namespace 时，在项目定义中，没有找到相关的定义!!  位置:" + fns.namespaceStatementBlock.tokenList[i].ToLexemeAllString());
                    }
                    parentNode = parentNode.AddMetaNamespace(mn);
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
            //CreateMetaNamespaceByFineDefineNamespace(fmn, mn);
        }
        public MetaNode FindFinalMetaNamespaceByNSBlock( NamespaceStatementBlock nsb, MetaNode root = null )
        {
            if( nsb.namespaceList.Count == 0 )
            {
                return null;
            }

            if( root == null )
            {
                root = ModuleManager.instance.selfModule.metaNode;
            }
            for (int i = 0; i < nsb.tokenList.Count; i++)
            {
                string name = nsb.tokenList[i].lexeme.ToString();
                var findNode2 = root.GetChildrenMetaNodeByName(name);
                if (findNode2 == null)
                {
                    break;
                }
                root = findNode2;
                if ( i == nsb.tokenList.Count - 1 )
                {
                    if (findNode2.metaNamespace != null)
                    {
                        return findNode2;
                    }
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
                //MetaNamespace parentMetaNamespace = null;
                //for ( int i = 0; i < list.Count; i++ )
                //{
                //    tempname = list[i];
                //    if ( i == 0 )
                //    {
                //        var metabase = selfModule.GetChildrenMetaBaseByName(tempname);
                //        if (metabase != null)
                //        {
                //            parentMetaNamespace = metabase as MetaNamespace;
                //            if(parentMetaNamespace == null )
                //            {
                //                Debug.Write("已有类: " + tempname + "与添加的命名空间冲突!!");
                //                return;
                //            }
                //        }
                //        else
                //        {
                //            parentMetaNamespace = new MetaNamespace(tempname);
                //            selfModule.AddMetaNamespace(parentMetaNamespace);
                //        }                
                //    }
                //    else
                //    {
                //        var metabase = parentMetaNamespace.GetChildrenMetaBaseByName(tempname);
                //        if( metabase != null )
                //        {
                //            parentMetaNamespace = metabase as MetaNamespace;
                //            if (parentMetaNamespace == null)
                //            {
                //                Debug.Write("已有类: " + tempname + "与添加的命名空间冲突!!");
                //                return;
                //            }
                //        }
                //        else
                //        {
                //            var mn = new MetaNamespace(tempname);
                //            parentMetaNamespace.AddMetaNamespace(mn);
                //            parentMetaNamespace = mn;
                //        }
                //    }
                //}
                //metaNamespaceDict.Add(nsString, parentMetaNamespace);
            }
            else
            {
                Log.AddInStructMeta(EError.None, "NamespaceManager::AddNamespaceString 命名空间:" + nsString + "解析错误!!");
                return;
            }
        }
        public static MetaBase FindMetaBaseByNamespaceToParentAndName( MetaNamespace mn, string nodeName )
        {
            MetaBase cur = mn;
            MetaBase childMB = null;
            while( cur != null )
            {
                //childMB = cur.GetChildrenMetaBaseByName(nodeName);
                //if (childMB != null)
                //    return childMB;
                //cur = cur.parentNode;
            }
            return childMB;
        }
    }
}
