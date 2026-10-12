//****************************************************************************
//  File:      FileMeta.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMeta : FileMetaBase
    {
        public string path => m_Path;
        public List<FileMetaClass> fileMetaAllClassList => m_FileMetaAllClassList;


        private string m_Path;
        // for example: import namespace1.namespace2;
        private List<FileMetaImportSyntax> m_FileImportSyntax = new List<FileMetaImportSyntax>();
        private FileMetaLocalSyntax m_FileMetaLocalSyntax = null;
        // for example: namespace a.b.c;
        private List<FileMetaNamespace> m_FileDefineNamespaceList = new List<FileMetaNamespace>();

        private List<FileMetaNamespace> m_FileMetaAllNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_FileMetaAllClassList = new List<FileMetaClass>();

        private List<MetaNamespace> m_ImportMetaNamespaceList = new List<MetaNamespace>();
        private Dictionary<string, MetaNamespace> m_ImportAliasNamespaceDict = new Dictionary<string, MetaNamespace>();
        // 模块根节点列表（import Std; 注册的是模块根，不是 MetaNamespace）
        private List<MetaNode> m_ImportMetaNodeList = new List<MetaNode>();
        // import as 模块别名注册表：import Std as S → "S" 指向 Std 模块根 MetaNode（模块根不是 MetaNamespace，单独存）
        private readonly Dictionary<string, MetaNode> m_ImportAliasNodeDict = new Dictionary<string, MetaNode>();

        private readonly List<FileMetaTypeAliasDecl> m_TypeAliasDeclList = new List<FileMetaTypeAliasDecl>();
        private readonly Dictionary<string, MetaType> m_FileResolvedTypeAliasDict = new Dictionary<string, MetaType>();

        public IReadOnlyList<FileMetaTypeAliasDecl> typeAliasDeclList => m_TypeAliasDeclList;

        public void AddTypeAliasDecl(FileMetaTypeAliasDecl decl)
        {
            if (decl != null)
                m_TypeAliasDeclList.Add(decl);
        }

        public void ClearResolvedFileTypeAliases()
        {
            m_FileResolvedTypeAliasDict.Clear();
        }

        public bool TryGetFileTypeAlias(string name, out MetaType targetType)
        {
            return m_FileResolvedTypeAliasDict.TryGetValue(name, out targetType);
        }

        internal void InternalSetFileTypeAlias(string name, MetaType targetType)
        {
            if (string.IsNullOrEmpty(name) || targetType == null)
                return;
            m_FileResolvedTypeAliasDict[name] = targetType;
        }

        public FileMeta( string p )
        {
            m_Path = p;
        }
        public void AddFileImportSyntax(FileMetaImportSyntax iss )
        {
            iss.SetFileMeta(this);
            m_FileImportSyntax.Add(iss);
        }

        public void SetFileMetaLocalSyntax(FileMetaLocalSyntax local)
        {
            m_FileMetaLocalSyntax = local;
        }

        public FileMetaLocalSyntax GetFileMetaLocalSyntax()
        {
            return m_FileMetaLocalSyntax;
        }
        public FileMetaLocalSyntax GetFileMetaGlobalSyntax()
        {
            return null;
        }
        public void AddFileDefineNamespace(FileMetaNamespace fdn )
        {
            fdn.SetFileMeta(this);
            m_FileDefineNamespaceList.Add(fdn);
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
        public void AddImportMetaNode(MetaNode mn)
        {
            if (mn == null) return;
            if (m_ImportMetaNodeList.IndexOf(mn) >= 0) return;
            m_ImportMetaNodeList.Add(mn);
        }
        public void AddImportAliasMetaNamespace(string aliasName, MetaNamespace mn)
        {
            if (string.IsNullOrWhiteSpace(aliasName) || mn == null)
            {
                return;
            }

            m_ImportAliasNamespaceDict[aliasName] = mn;
        }
        /// <summary>
        /// 注册 import as 的模块别名（import Std as S）。
        /// 消费点：GetMetaNodeByImportAlias（类型位置与表达式位置的链首兜底解析）。
        /// </summary>
        public void AddImportAliasMetaNode(string aliasName, MetaNode mn)
        {
            if (string.IsNullOrWhiteSpace(aliasName) || mn == null)
            {
                return;
            }

            m_ImportAliasNodeDict[aliasName] = mn;
        }
        public FileMetaClass GetFileMetaClassByName( string name )
        {
            var fmc = m_FileMetaAllClassList.Find(a => a.name == name);
           
            return fmc;
        }
        // 文件顶级类视图：AllClassList 中不隶属任何命名空间、也不是内部类的类
        // （顶级类状态由解析期容器决定：进 namespace 会 SetMetaNamespace，进类会 SetFileMetaClass）
        public List<FileMetaClass> GetTopLevelFileMetaClassList()
        {
            var list = new List<FileMetaClass>();
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fmc = m_FileMetaAllClassList[i];
                if (fmc.topLevelFileMetaClass == null && fmc.topLevelFileMetaNamespace == null)
                {
                    list.Add(fmc);
                }
            }
            return list;
        }
        public void RemoveFileMetaClass( FileMetaClass mc )
        {
            m_FileMetaAllClassList.Remove(mc);
        }
        public MetaNode GetMetaBaseByFileMetaClassRef( FileMetaClassDefine fmcv )
        {
            MetaNode mb = null;
            for( int i = 0; i < m_ImportMetaNamespaceList.Count; i++ )
            {
                MetaNode mn = GetMetaNodeFileMetaClass(fmcv.stringList);
                if (mn == null) continue;
                if (mn.isMetaNamespace == false ) { continue; }
                mb = fmcv.GetChildrenMetaNode(mn);
                if (mb != null)
                    return mb;
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
            }
            return mb;
        }
        public MetaNode GetMetaNodeFileMetaClass( List<string> classList )
        {
            if (classList.Count == 0) return null;

            var aliasNode = GetMetaNodeByImportAlias(classList);
            if (aliasNode != null)
            {
                return aliasNode;
            }


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
            // 也搜索模块根节点（import Std; 注册的是模块根）
            if (mb == null)
            {
                for (int i = 0; i < m_ImportMetaNodeList.Count; i++)
                {
                    mb = GetMetaNodeByNamespace(m_ImportMetaNodeList[i], classList);
                    if (mb != null) break;
                }
            }
            return mb;
        }
        MetaNode GetMetaNodeByImportAlias(List<string> classList)
        {
            if (classList == null || classList.Count == 0)
            {
                return null;
            }
            MetaNode startNode = null;
            if (m_ImportAliasNodeDict.TryGetValue(classList[0], out var aliasNode))
            {
                // 模块别名：import Std as S → S 指向模块根 MetaNode，续解 S.X.Y 路径
                startNode = aliasNode;
            }
            else if (m_ImportAliasNamespaceDict.TryGetValue(classList[0], out var aliasNamespace))
            {
                // 命名空间别名：import NS.a as NSA → 指向目标命名空间节点
                startNode = aliasNamespace?.metaNode;
            }
            if (startNode == null)
            {
                return null;
            }

            MetaNode mb = startNode;
            for (int i = 1; i < classList.Count; i++)
            {
                mb = mb?.GetChildrenMetaNodeByName(classList[i]);
                if (mb == null)
                {
                    return null;
                }
            }
            return mb;
        }
        public void CreateNamespace()
        {
            MetaNode parentNode = ModuleManager.instance.selfModule.metaNode;
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                m_FileDefineNamespaceList[i].CreateNamespace(parentNode);
            }
        }
        public void CombineFileMeta()
        {
            for (int i = 0; i < this.m_FileImportSyntax.Count; i++)
            {
                m_FileImportSyntax[i].Parse();
            }
            for (int i = 0; i < m_FileMetaAllClassList.Count; i++)
            {
                var fns = m_FileMetaAllClassList[i];
                ClassManager.instance.AddClass(fns);
            }
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileImportSyntax.Count; i++)
            {
                m_FileImportSyntax[i].SetDeep(m_Deep);
            }
            if (m_FileMetaLocalSyntax != null)
            {
                m_FileMetaLocalSyntax.SetDeep(m_Deep);
            }
            for (int i = 0; i < m_FileDefineNamespaceList.Count; i++)
            {
                m_FileDefineNamespaceList[i].SetDeep(m_Deep);
            }
            // 只对顶级类 SetDeep，命名空间内/内部类由各自容器递归设置深度
            var topLevelClassList = GetTopLevelFileMetaClassList();
            for (int i = 0; i < topLevelClassList.Count; i++)
            {
                topLevelClassList[i].SetDeep(m_Deep);
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
            if(m_FileMetaLocalSyntax != null )
            {
                sb.Append(m_FileMetaLocalSyntax.ToFormatString() + Environment.NewLine);
            }
            //for (int i = 0; i < m_FileSearchNamespaceList.Count; i++)
            //{
            //    sb.Append(m_FileSearchNamespaceList[i].ToFormatString() + Environment.NewLine);
            //}
            //只打印顶级类，命名空间内/内部类由各自容器的 ToFormatString 递归输出
            var topLevelClassList = GetTopLevelFileMetaClassList();
            for (int i = 0; i < topLevelClassList.Count; i++)
            {
                sb.Append(topLevelClassList[i].ToFormatString() + Environment.NewLine);
            }
            sb.Append("-------------------FileMeta 文件显示 结束 : -----------------------" + Environment.NewLine);

            return sb.ToString();
        }

        public override string ToString()
        {
            return m_Path;
        }
    }
}
