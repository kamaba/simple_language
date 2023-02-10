//****************************************************************************
//  File:      ProjectConfigData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description:  project configuration file convert compile data struction!
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLanguage.Project
{
    public class CompileFileData
    {
        public enum ECompileState
        {
            Default,
            Ignore,
        }
        public string path { get; set; }
        public string group { get; set; }
        public string tag { get; set; }
        public ECompileState compileState { get; set; }
        public int priority { get; set; }

        public CompileFileData(FileMetaMemberData fmmd)
        {
            var findPath = fmmd.GetFileMetaMemberDataByName("path");
            if( findPath == null )
            {
                Console.Write("在ProjectConfig 中的CompileFileData必须有path节点!!");
                return;
            }
            if( findPath.fileMetaConstValue == null )
            {
                Console.Write("在ProjectConfig 中的CompileFileData必须有path节点!!");
                return;
            }
            if(findPath.fileMetaConstValue.token?.GetEType() != EType.String )
            {
                Console.Write("在ProjectConfig 中的CompileFileData必须有path节点必须是string!!");
                return;
            }
            path = findPath.fileMetaConstValue.token.lexeme.ToString();

            var findOption = fmmd.GetFileMetaMemberDataByName("option");
            var findGroup = fmmd.GetFileMetaMemberDataByName("group");
            group = findGroup?.fileMetaConstValue?.token?.lexeme.ToString();
            var findTag = fmmd.GetFileMetaMemberDataByName("tag");
            tag = findTag?.fileMetaConstValue?.token?.lexeme.ToString();
            var findIgnore = fmmd.GetFileMetaMemberDataByName("ignore");

            object obj = findIgnore?.fileMetaConstValue?.token?.lexeme;
            if (obj != null && (bool)obj == true )
            {
                compileState = ECompileState.Ignore;
            }
            else
            {
                compileState = ECompileState.Default;
            }
        }
    }
    public class CompileOptionData
    {
        public bool isForceUseClassKey { get; set; }
        public bool isSupportDoublePlus { get; set; }

        public CompileOptionData(FileMetaMemberData fmmd)
        {
            var findIsForceUseClassKey = fmmd.GetFileMetaMemberDataByName("isForceUseClassKey");
            var findIsSupportDoublePlus = fmmd.GetFileMetaMemberDataByName("isSupportDoublePlus");

            object obj1 = findIsForceUseClassKey?.fileMetaConstValue?.token?.lexeme;
            object obj2 = findIsSupportDoublePlus?.fileMetaConstValue?.token?.lexeme;

            isForceUseClassKey = obj1 != null ? bool.Parse(obj1.ToString()) : false;
            isSupportDoublePlus = obj2 != null ? bool.Parse(obj2.ToString()) : false;
        }
    }
    public class CompileFilterData
    {
        public List<string> groupList { get; set; } = new List<string>();
        public List<string> tagList { get; set; } = new List<string>();

        public bool isAllGroup { get; set; } = false;
        public bool isAllTag { get; set; } = false;

        public bool IsIncludeInGroup(string group)
        {
            if (isAllGroup) return true;

            if (groupList != null && groupList.Contains(group)) return true;

            return false;
        }
        public bool IsIncludeInTag(string tag)
        {
            if (isAllTag) return true;

            if (tagList != null && tagList.Contains(tag)) return true;

            return false;
        }

        FileMetaMemberData m_FileMetaMemberData = null;

        public CompileFilterData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData = fmmd;
        }
        public bool Parse()
        {
            if(m_FileMetaMemberData == null )
            {
                return false;
            }
            var findGroup = m_FileMetaMemberData.GetFileMetaMemberDataByName("group");
            if(findGroup != null )
            {
                for( int i = 0; i < findGroup.fileMetaMemberData.Count; i++ )
                {
                    var mcv = findGroup.fileMetaMemberData[i].fileMetaConstValue;
                    if( mcv != null )
                    {
                        if( mcv.token?.lexeme.ToString() == "all" )
                        {
                            isAllGroup = true;
                            break;
                        }
                        else
                        {
                            groupList.Add(mcv.token?.lexeme.ToString());
                        }
                    }
                }
            }

            var findTag = m_FileMetaMemberData.GetFileMetaMemberDataByName("tag");
            if (findTag != null)
            {
                for (int i = 0; i < findTag.fileMetaMemberData.Count; i++)
                {
                    var mcv = findTag.fileMetaMemberData[i].fileMetaConstValue;
                    if (mcv != null)
                    {
                        if (mcv.token?.lexeme.ToString() == "all")
                        {
                            isAllTag = true;
                            break;
                        }
                        else
                        {
                            tagList.Add(mcv.token?.lexeme.ToString());
                        }
                    }
                }
            }

            return true;
        }
    }
    public class ImportModuleData
    {
        public string moduleName { get; set; } = "";
        public string modulePath { get; set; } = "";


        FileMetaMemberData m_FileMetaMemberData = null;

        public ImportModuleData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData = fmmd;
        }
        public bool Parse()
        {
            if (m_FileMetaMemberData == null)
            {
                return false;
            }
            var findGroup = m_FileMetaMemberData.GetFileMetaMemberDataByName("group");
            if (findGroup != null)
            {
                for (int i = 0; i < findGroup.fileMetaMemberData.Count; i++)
                {
                    var mcv = findGroup.fileMetaMemberData[i].fileMetaConstValue;
                    if (mcv != null)
                    {
                        if (mcv.token?.lexeme.ToString() == "all")
                        {
                            break;
                        }
                        else
                        {
                        }
                    }
                }
            }

            var findTag = m_FileMetaMemberData.GetFileMetaMemberDataByName("tag");
            if (findTag != null)
            {
                for (int i = 0; i < findTag.fileMetaMemberData.Count; i++)
                {
                    var mcv = findTag.fileMetaMemberData[i].fileMetaConstValue;
                    if (mcv != null)
                    {
                        if (mcv.token?.lexeme.ToString() == "all")
                        {
                            break;
                        }
                        else
                        {
                        }
                    }
                }
            }

            return true;
        }
    }
    public class CompileModuleData
    {
        public string name { get; set; }
        public string path { get; set; }

        public CompileModuleData(FileMetaMemberData fmmd)
        {
            var findOption = fmmd.GetFileMetaMemberDataByName("option");
        }
    }
    public class DefineNamespace
    {
        public string spaceName { get; set; } = "";
        public List<DefineNamespace> childDefineNamespace = new List<DefineNamespace>();


        public DefineNamespace()
        {
        }
        public DefineNamespace Parse(FileMetaMemberData root)
        {
            if (root == null)
            {
                return null;
            }
            spaceName = root.name;

            for (int i = 0; i < root.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = root.fileMetaMemberData[i];

                var cDefineNamespce = new DefineNamespace();
                var newND = cDefineNamespce.Parse(cfmmd);
                if(newND != null )
                {
                    childDefineNamespace.Add(newND);
                }
            }
            return this;
        }
    }

    public class GlobalImportData
    {
        public List<string> globalImportList = new List<string>();

        FileMetaMemberData m_FileMetaMemberData = null;

        public GlobalImportData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData = fmmd;
        }
        public void Parse()
        {
            if (m_FileMetaMemberData == null)
            {
                return;
            }

            for (int i = 0; i < m_FileMetaMemberData.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = m_FileMetaMemberData.fileMetaMemberData[i];

                var mcv = cfmmd.fileMetaMemberData[i].fileMetaConstValue;
                if (mcv != null && mcv?.token?.GetEType() == EType.String )
                {
                    globalImportList.Add(mcv.token.lexeme.ToString());
                }
            }
        }
    }

    public class GlobalReplaceData
    {
        private Dictionary<string, string> repaceStringDict = new Dictionary<string, string>();

        FileMetaMemberData m_FileMetaMemberData = null;

        public GlobalReplaceData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData = fmmd;
        }
        public void Parse()
        {
            if (m_FileMetaMemberData == null)
            {
                return;
            }
            for (int i = 0; i < m_FileMetaMemberData.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = m_FileMetaMemberData.fileMetaMemberData[i];
                if( cfmmd.DataType == FileMetaMemberData.EMemberDataType.KeyValue )
                {
                    string orgString = cfmmd.token?.lexeme.ToString();
                    string replaceString = cfmmd?.fileMetaConstValue?.token?.lexeme.ToString();
                
                    if(repaceStringDict.ContainsKey( orgString ) )
                    {
                        Console.Write("Error GlobalReplace中，存在两个相同的关键字!!");
                        continue;
                    }
                    repaceStringDict.Add(orgString, replaceString);
                }
            }
        }
    }

    public class ExportDllData
    {
        public string spaceName { get; set; } = "";
        public List<DefineNamespace> childDefineNamespace = new List<DefineNamespace>();

        FileMetaMemberData m_FileMetaMemberData = null;

        public ExportDllData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData = fmmd;
        }
        public void Parse(FileMetaMemberData root)
        {
            if (root == null)
            {
                return;
            }
            spaceName = m_FileMetaMemberData.name;

            for (int i = 0; i < root.fileMetaMemberData.Count; i++)
            {
            }
            return;
        }
    }

    public class ProjectData
    {
        public string projectName { get; set; }
        public string projectDesc { get; set; }
        public int mainVersion { get; set; } = 0;
        public int subVersion { get; set; } = 0;
        public int buildVersion { get; set; } = 0;
        public int buildSubVersion { get; set; } = 0;
        public bool isUseForceSemiColonInLineEnd { get; set; } = true;
        public CompileOptionData compileOptionData { get; set; } = null;
        public CompileFilterData compileFilter { get; set; } = null;
        public DefineNamespace namespaceRoot { get; set; } = null;
        public GlobalImportData globalImportData { get; set; } = null;
        public GlobalReplaceData globalReplaceData { get; set; } = null;
        public ExportDllData exportDllData { get; set; } = null;
        public List<CompileModuleData> compileModuleList { get; set; } = new List<CompileModuleData>();
        public List<ImportModuleData> importModuleList { get; set; } = new List<ImportModuleData>();
        public List<CompileFileData> compileFileList { get; set; } = new List<CompileFileData>();
    }
}
