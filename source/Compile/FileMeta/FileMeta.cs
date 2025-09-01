//****************************************************************************
//  File:      FileMeta.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMeta : FileMetaBase
    {
        public string path => m_Path;
        public List<FileMetaClass> fileMetaClassList => m_FileMetaClassList;


        private string m_Path;
        // for example: import namespace1.namespace2;
        private List<FileMetaImportSyntax> m_FileImportSyntax = new List<FileMetaImportSyntax>();
        // for example: namespace a.b.c;
        private List<FileMetaNamespace> m_FileDefineNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaNamespace> m_FileSearchNamespaceList = new List<FileMetaNamespace>();
        // for example: namespace a{ namespace b{}}
        //private List<FileMetaNamespace> m_FileMetaNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_FileMetaClassList = new List<FileMetaClass>();

        private List<FileMetaNamespace> m_FileMetaAllNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_FileMetaAllClassList = new List<FileMetaClass>();

        private List<MetaNamespace> m_ImportMetaNamespaceList = new List<MetaNamespace>();

        public FileMeta( string p )
        {
            m_Path = p;
        }
        public void AddFileImportSyntax(FileMetaImportSyntax iss )
        {
            iss.SetFileMeta(this);
            m_FileImportSyntax.Add(iss);
        }
        public void AddFileDefineNamespace(FileMetaNamespace fdn )
        {
            fdn.SetFileMeta(this);
            m_FileDefineNamespaceList.Add(fdn);
        }
        public void AddFileSearchNamespace(FileMetaNamespace fdn )
        {
            fdn.SetFileMeta(this);
            m_FileSearchNamespaceList.Add(fdn);
        }
        public void AddFileMetaAllNamespace( FileMetaNamespace fmn )
        {
            fmn.SetFileMeta(this);
            m_FileMetaAllNamespaceList.Add(fmn);
        }
        public void AddFileMetaAllClass( FileMetaClass fmc )
        {
            fmc.SetFileMeta(this);
            m_FileMetaAllClassList.Add(fmc);
        }
        public void AddImportMetaNamespace(MetaNamespace mn)
        {
            if (m_ImportMetaNamespaceList.IndexOf(mn) >= 0)
            {
                return;
            }
            m_ImportMetaNamespaceList.Add(mn);
        }
        public FileMetaClass GetFileMetaClassByName( string name )
        {
            var fmc = m_FileMetaAllClassList.Find(a => a.name == name);
           
            return fmc;
        }
        public void AddFileMetaClass( FileMetaClass mc )
        {
            m_FileMetaClassList.Add(mc);
        }
        public MetaNode GetMetaBaseByFileMetaClassRef( FileMetaClassDefine fmcv )
        {
            MetaNode mb = null;
            for( int i = 0; i < m_ImportMetaNamespaceList.Count; i++ )
            {
                MetaNode mn = GetMetaNodeFileMetaClass(fmcv.stringList);
                if (mn == null) continue;
                if (mn.isMetaNamespace == false ) { continue; }
                if (mn.metaNamespace.refFromType == RefFromType.CSharp)
                {
                    Object obj = CSharpManager.GetObject(fmcv, mn.metaNamespace);
                }
                else
                {
                    mb = fmcv.GetChildrenMetaNode(mn);
                    if (mb != null)
                        return mb;
                }
            }
            return null;
        }
        public MetaNode GetMetaNodeByNamespace( MetaNode namespaceMN, List<string> classList )
        {
            MetaNode mb = namespaceMN;
            for (int j = 0; j < classList.Count; j++)
            {
                string cname = classList[j];

                mb = mb.GetChildrenMetaNodeByName(cname);
                if( mb == null )
                {
                    if (namespaceMN.name == cname 
                        && (namespaceMN.isMetaModule||namespaceMN.isMetaNamespace ) )
                    {
                        mb = namespaceMN;
                        continue;
                    }
                    break;
                }
                //else
                //{
                //    if (imn.refFromType == RefFromType.CSharp)
                //    {
                //        mb = CSharpManager.FindCSharpClassOrNameSpace(imn.name, cname);
                //    }
                //    else
                //    {
                //        mb = imn.metaNode.GetChildrenMetaNodeByName(name);
                //    }
                //    if( mb == null )
                //    {
                //        break;
                //    }
                //}
            }
            return mb;
        }
        public MetaNode GetMetaNodeFileMetaClass( List<string> classList )
        {
            if (classList.Count == 0) return null;


            MetaNode mb = null;
            MetaNode searchMN = null;
            for (int i = 0; i < m_ImportMetaNamespaceList.Count; i++)
            {
                searchMN = m_ImportMetaNamespaceList[i].metaNode;

                while(searchMN != null )
                {
                    mb = GetMetaNodeByNamespace(searchMN, classList);

                    if (mb == null)
                    {
                        searchMN = searchMN.parentNode;
                    }
                    else
                    {
                        break;
                    }
                }

                if( mb != null )
                {
                    break;
                }
            }
            return mb;
        }
        public MetaNode GetMetaNodeByImportNamespace( string name )
        {
            for (int i = 0; i < m_ImportMetaNamespaceList.Count; i++)
            {
                var imn = m_ImportMetaNamespaceList[i];
                if (imn.refFromType == RefFromType.CSharp)
                {
                    MetaNode getmb = CSharpManager.FindCSharpClassOrNameSpace(imn.name, name);
                    if (getmb != null)
                        return getmb;
                }
                else
                {
                    return imn.metaNode.GetChildrenMetaNodeByName(name);
                }
            }
            return null;
        }
        //public MetaNode GetMetaBaseByName( string name )
        //{
        //    MetaModule mm = ModuleManager.instance.GetMetaModuleByName(name);
        //    if( mm != null )
        //    {
        //        return mm;
        //    }
        //    else
        //    {
        //        MetaNode m2 = ModuleManager.instance.GetChildrenMetaNodeByName(name);
        //        if (m2 != null)
        //        {
        //            return m2;
        //        }
        //    }

        //    for (int i = 0; i < m_ImportMetaNamespaceList.Count; i++)
        //    {
        //        var imn = m_ImportMetaNamespaceList[i];
        //        if( imn.refFromType == RefFromType.CSharp )
        //        {
        //            MetaNode getmb = CSharpManager.FindCSharpClassOrNameSpace(imn.name, name);
        //            if (getmb != null)
        //                return getmb;
        //        }
        //        else
        //        {
        //            //return imn.GetChildrenMetaBaseByName(name);
        //        }
        //    }
        //    return null;
        //}
        public void CreateNamespace()
        {
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                var fmn = m_FileDefineNamespaceList[i];
                NamespaceManager.instance.CreateMetaNamespaceByFineDefineNamespace(fmn);
            }
            for (int i = 0; i < m_FileSearchNamespaceList.Count; i++)
            {
                var fmn = m_FileSearchNamespaceList[i];
                if (ProjectManager.useDefineNamespaceType != EUseDefineType.NoUseProjectConfigNamespace)
                {
                    if (!ProjectManager.data.IsIncludeDefineStruct(fmn.namespaceStatementBlock.namespaceList))
                    {
                        Log.AddInStructFileMeta( EError.None, "Error 暂不允许使用namespace 定义命名空间!!!" + fmn.ToFormatString() + " 位置: " + fmn.token.ToLexemeAllString());
                    }
                }
                NamespaceManager.instance.CreateMetaNamespaceByFineDefineNamespace(fmn);
            }
        }
        public void CombineFileMeta()
        {
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                m_FileImportSyntax[i].Parse();
            }
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];

                for( int j = 0; j < m_FileSearchNamespaceList.Count; j++)
                {
                    fns.AddExtendMetaNamespace(m_FileSearchNamespaceList[j]);
                }

                ClassManager.instance.AddClass(fns);
            }
        }
        //public void HandleExtendData()
        //{
        //    for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
        //    {
        //        var fns = m_FileMetaAllClassList[i];

        //        ClassManager.instance.HandleExtendContent(fns);
        //    }
        //}
        public void ParseInface()
        {

        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                m_FileImportSyntax[i].SetDeep(m_Deep);
            }
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                m_FileDefineNamespaceList[i].SetDeep(m_Deep);
            }
            for (int i = 0; i < m_FileSearchNamespaceList.Count; i++)
            {
                m_FileSearchNamespaceList[i].SetDeep(m_Deep);
            }
            for (int i = 0; i < m_FileMetaClassList.Count; i++)
            {
                m_FileMetaClassList[i].SetDeep(m_Deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-------------------FileMeta 文件显示 开始 : Path: " + m_Path + "-----------------------" + Environment.NewLine );
            //sb.Append("ImportSyntax" + Environment.NewLine);
            for( int i = 0; i < m_FileImportSyntax.Count; i++ )
            {
                sb.Append(m_FileImportSyntax[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                sb.Append(m_FileDefineNamespaceList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileSearchNamespaceList.Count; i++)
            {
                sb.Append(m_FileSearchNamespaceList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileMetaClassList.Count; i++)
            {
                sb.Append(m_FileMetaClassList[i].ToFormatString() + Environment.NewLine);
            }
            sb.Append("-------------------FileMeta 文件显示 结束 : -----------------------" + Environment.NewLine);

            return sb.ToString();
        }
    }
}
