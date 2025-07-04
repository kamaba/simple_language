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
using System.Diagnostics;
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
            for( int i = 0; i < m_FileImportSyntax.Count; i++ )
            {
                MetaNode mn = NamespaceManager.instance.FindImportNamespace( m_FileImportSyntax[i], fmcv.name );
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
        public MetaNode GetMetaNodeFileMetaClass( List<string> classList )
        {
            if (classList.Count == 0) return null;
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            MetaNode mb = null;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                MetaNode findMN = NamespaceManager.instance.FindImportNamespace(m_FileImportSyntax[i], classList[0]);
                if (findMN == null)
                    continue;
                for ( int j = 1; j < classList.Count; j++ )
                {
                    findMN = findMN.GetChildrenMetaNodeByName(classList[i]);
                    if (findMN == null)
                        continue;
                }
                if (findMN != null)
                    return findMN;

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
        /*
        public T GetMetaBaseTByName<T>(string name) where T : MetaBase
        {
#pragma warning disable CS0162 // 检测到无法访问的代码
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                MetaBase mn = NamespaceManager.instance.FindImportNamespace(m_FileImportSyntax[i], name);
                while (true)
                {
                    //var fmn = mn.GetChildrenMetaBaseByName(name);
                    //if (fmn != null && fmn.GetType() == typeof(T) )
                    //{
                    //    return fmn as T;
                    //}
                    //mn = mn.parentNode;
                    //if (mn == null)
                    //    continue;
                }
            }
#pragma warning restore CS0162 // 检测到无法访问的代码
            return default(T);
        }
        */
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
                        Debug.Write("Error 暂不允许使用namespace 定义命名空间!!!" + fmn.ToFormatString() + " 位置: " + fmn.token.ToLexemeAllString());
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
        public void HandleExtendData()
        {
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];

                ClassManager.instance.HandleExtendContent(fns);
            }
        }
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
