//****************************************************************************
//  File:      NamespaceManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Project;
using SimpleLanguage.Logging;
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

        public MetaNode GetParentChildrenNode(FileMetaNamespace fns, MetaNode parentNode )
        {
            var findNode = parentNode;
            for (int i = 0; i < fns.namespaceStatementBlock.tokenList.Count; i++)
            {
                if (findNode == null)
                {
                    return null;
                }

                string name = fns.namespaceStatementBlock.tokenList[i].lexeme.ToString();
                var findNode2 = findNode.GetChildrenMetaNodeByName(name);
                if (findNode2 == null)
                {
                    return null;
                }
                if (!findNode2.isMetaNamespace)
                {
                    return null;
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
                    return null;
                }
            }

            return findNode;
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
    }
}
