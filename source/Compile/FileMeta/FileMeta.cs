//****************************************************************************
//  File:      FileMeta.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMeta : FileMetaBase
    {
        public List<FileMetaClass> fileMetaClassList => m_FileMetaClassList;


        private string m_Path;
        // for example: import namespace1.namespace2;
        private List<FileMetaImportSyntax> m_FileImportSyntax = new List<FileMetaImportSyntax>();
        // for example: namespace a.b.c;
        private List<FileDefineNamespace> m_FileDefineNamespaceList = new List<FileDefineNamespace>();
        // for example: namespace a{ namespace b{}}
        private List<FileMetaNamespace> m_FileMetaNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_FileMetaClassList = new List<FileMetaClass>();

        private List<FileMetaNamespace> m_FileMetaAllNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_FileMetaAllClassList = new List<FileMetaClass>();

        public FileMeta( string p )
        {
            m_Path = p;
        }
        public void AddFileImportSyntax(FileMetaImportSyntax iss )
        {
            iss.SetFileMeta(this);
            m_FileImportSyntax.Add(iss);
        }
        public void AddFileDefineNamespace( FileDefineNamespace fdn )
        {
            fdn.SetFileMeta(this);
            m_FileDefineNamespaceList.Add(fdn);
        }
        public void AddFileMetaNamespace( FileMetaNamespace mn )
        {
            mn.SetFileMeta(this);
            m_FileMetaNamespaceList.Add(mn);
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
        public FileMetaClass GetFileMetaClassByName( string name )
        {
            var fmc = m_FileMetaAllClassList.Find(a => a.name == name);
           
            return fmc;
        }
        public void AddFileMetaClass( FileMetaClass mc )
        {
            m_FileMetaClassList.Add(mc);
        }
        public MetaBase GetMetaBaseByFileMetaClassRef( FileMetaClassDefine fmcv )
        {
            MetaBase mb = null;
            for( int i = 0; i < m_FileImportSyntax.Count; i++ )
            {
                MetaNamespace mn = m_FileImportSyntax[i].lastMetaNamespace;
                if( mn.refFromType == RefFromType.CSharp )
                {
                    Object obj = CSharpManager.GetObject(fmcv, mn);
                }
                else
                {
                    mb = fmcv.GetChildrenMetaBase( mn );
                    if (mb != null)
                        return mb;
                }
            }
            return null;
        }
        public MetaBase GetMetaBaseFileMetaClass( List<string> classList )
        {
            if (classList.Count == 0) return null;
            MetaBase mb = null;
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {                
                MetaNamespace mn = m_FileImportSyntax[i].lastMetaNamespace;
                MetaBase findMN = NamespaceManager.FindMetaBaseByNamespaceToParentAndName(mn, classList[0] );
                if (findMN == null)
                    continue;
                for ( int j = 1; j < classList.Count; j++ )
                {
                    findMN = findMN.GetChildrenMetaBaseByName(classList[i]);
                    if (findMN == null)
                        continue;
                }
                if (findMN != null)
                    return findMN;

            }
            return null;
        }
        public MetaBase GetMetaBaseByName( string name )
        {
            MetaModule mm = ModuleManager.instance.GetMetaModuleByName(name);
            if( mm != null )
            {
                return mm;
            }
            else
            {
                MetaBase m2 = ModuleManager.instance.GetChildrenMetaBaseByName(name);
                if (m2 != null)
                {
                    return m2;
                }
            }

            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                MetaBase mn = m_FileImportSyntax[i].lastMetaNamespace;
                MetaBase findMB = null;
                while (mn != null)
                {
                    var fmn = mn.GetChildrenMetaBaseByName(name);
                    if (fmn != null)
                    {
                        findMB = fmn;
                        break;
                    }
//#ifdef CSharp
                    else if( mn.refFromType == RefFromType.CSharp )
                    {
                        findMB = CSharpManager.FindAndCreateMetaBase(mn, name);
                        if (findMB != null)
                            return findMB;
                    }
//#endif
                    mn = mn.parentNode;
                }
                if (findMB != null) return findMB;
            }
            return null;
        }
        public T GetMetaBaseTByName<T>(string name) where T : MetaBase
        {
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                MetaBase mn = m_FileImportSyntax[i].lastMetaNamespace;
                while (true)
                {
                    var fmn = mn.GetChildrenMetaBaseByName(name);
                    if (fmn != null && fmn.GetType() == typeof(T) )
                    {
                        return fmn as T;
                    }
                    mn = mn.parentNode;
                    if (mn == null)
                        continue;
                }
            }
            return default(T);
        }
        public void CreateNamespace()
        {
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                NamespaceManager.instance.CreateMetaNamespaceByFineDefineNamespace(m_FileDefineNamespaceList[i]);
            }

            for (int i = 0; i < m_FileMetaAllNamespaceList.Count; i++)
            {
                NamespaceManager.instance.CreateMetaNamespaceByFileMetaNamespace(m_FileMetaAllNamespaceList[i]);
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

                ClassManager.instance.AddClass(fns);
            }
        }
        public void CheckExtendAndInterface()
        {
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];

                ClassManager.instance.CheckExtendAndInterface(fns);
            }
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];

                ClassManager.instance.HandleExtendContentAndInterfaceContent(fns);
            }
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];

                ClassManager.instance.HandleExtendContentAndInterfaceContent(fns);
            }
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
            for (int i = 0; i < m_FileMetaNamespaceList.Count; i++)
            {
                m_FileMetaNamespaceList[i].SetDeep(m_Deep);
            }
            for (int i = 0; i < m_FileMetaClassList.Count; i++)
            {
                m_FileMetaClassList[i].SetDeep(m_Deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Path: " + m_Path + Environment.NewLine );
            //sb.Append("ImportSyntax" + Environment.NewLine);
            for( int i = 0; i < m_FileImportSyntax.Count; i++ )
            {
                sb.Append(m_FileImportSyntax[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                sb.Append(m_FileDefineNamespaceList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileMetaNamespaceList.Count; i++)
            {
                sb.Append(m_FileMetaNamespaceList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_FileMetaClassList.Count; i++)
            {
                sb.Append(m_FileMetaClassList[i].ToFormatString() + Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
