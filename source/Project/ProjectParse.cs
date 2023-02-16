//****************************************************************************
//  File:      ProjectParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/4 12:00:00
//  Description:  Project's Configuration and ConfigData Parse to a Project Format Data!
//****************************************************************************


using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Compile;
using SimpleLanguage.Core;

namespace SimpleLanguage.Project
{
    public class CompileFileData
    {
        public enum ECompileState
        {
            Default,
            Ignore,
        }
        public class CompileFileDataUnit
        {
            public string path { get; set; }
            public string group { get; set; }
            public string tag { get; set; }
            public ECompileState compileState { get; set; }
            public int priority { get; set; }

            public CompileFileDataUnit(MetaMemberData mmd)
            {
                var findPath = mmd.GetString("path", true);
                if (findPath == null)
                {
                    Console.Write("在ProjectConfig 中的CompileFileData必须有path节点!!");
                    return;
                }
                path = findPath;
                //var findOption = mmd.GetFileMetaMemberDataByName("option");
                //var findGroup = mmd.GetFileMetaMemberDataByName("group");
                //group = findGroup?.fileMetaConstValue?.token?.lexeme.ToString();
                //var findTag = fmmd.GetFileMetaMemberDataByName("tag");
                //tag = findTag?.fileMetaConstValue?.token?.lexeme.ToString();
                //var findIgnore = fmmd.GetFileMetaMemberDataByName("ignore");

                //object obj = findIgnore?.fileMetaConstValue?.token?.lexeme;
                //if (obj != null && (bool)obj == true)
                //{
                //    compileState = ECompileState.Ignore;
                //}
                //else
                //{
                //    compileState = ECompileState.Default;
                //}
            }
        }

        public List<CompileFileDataUnit> compileFileDataUnitList = new List<CompileFileDataUnit>();

        public void Parse(MetaMemberData mmd)
        {
            foreach (var v in mmd.metaMemberDataDict)
            {
                compileFileDataUnitList.Add(new CompileFileDataUnit(v.Value));
            }
        }
    }
    public class CompileOptionData
    {
        public bool isForceUseClassKey { get; set; }
        public bool isSupportDoublePlus { get; set; }

        public void Parse(MetaMemberData mmd)
        {
            //var findIsForceUseClassKey = fmmd.GetFileMetaMemberDataByName("isForceUseClassKey");
            //var findIsSupportDoublePlus = fmmd.GetFileMetaMemberDataByName("isSupportDoublePlus");

            //object obj1 = findIsForceUseClassKey?.fileMetaConstValue?.token?.lexeme;
            //object obj2 = findIsSupportDoublePlus?.fileMetaConstValue?.token?.lexeme;

            //isForceUseClassKey = obj1 != null ? bool.Parse(obj1.ToString()) : false;
            //isSupportDoublePlus = obj2 != null ? bool.Parse(obj2.ToString()) : false;
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

            if (groupList.Count == 0) return true;

            if (groupList != null && groupList.Contains(group)) return true;

            return false;
        }
        public bool IsIncludeInTag(string tag)
        {
            if (isAllTag) return true;

            if (tagList.Count == 0) return true;

            if (tagList != null && tagList.Contains(tag)) return true;

            return false;
        }

        public bool Parse(MetaMemberData mmd)
        {
            if (mmd == null)
            {
                return false;
            }
            /*
            var findGroup = mmd.GetFileMetaMemberDataByName("group");
            if (findGroup != null)
            {
                for (int i = 0; i < findGroup.fileMetaMemberData.Count; i++)
                {
                    var mcv = findGroup.fileMetaMemberData[i].fileMetaConstValue;
                    if (mcv != null)
                    {
                        if (mcv.token?.lexeme.ToString() == "all")
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
            */

            return true;
        }
    }
    public class ImportModuleData
    {
        public class ImportModuleDataUnit
        {
            public string moduleName { get; set; } = "";
            public string modulePath { get; set; } = "";
        }

        List<ImportModuleDataUnit> importModuleDataUnitList = new List<ImportModuleDataUnit>();
        public bool Parse( MetaMemberData mmd )
        {
            /*
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
            */
            return true;
        }
    }
    public class CompileModuleData
    {
        public class CompileModuleDataUnit
        {
            public string name { get; set; }
            public string path { get; set; }
        }

        public void Parse(MetaMemberData mmd)
        {
            //var findOption = fmmd.GetFileMetaMemberDataByName("option");
        }
    }
    public class DefineNamespace
    {
        public string spaceName { get; set; } = "";
        public List<DefineNamespace> childDefineNamespace = new List<DefineNamespace>();


        public DefineNamespace()
        {
        }
        public DefineNamespace Parse( MetaMemberData mmd)
        {
            if (mmd == null)
            {
                return null;
            }
            spaceName = mmd.name;

            /*
            for (int i = 0; i < root.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = root.fileMetaMemberData[i];

                var cDefineNamespce = new DefineNamespace();
                var newND = cDefineNamespce.Parse(cfmmd);
                if (newND != null)
                {
                    childDefineNamespace.Add(newND);
                }
            }
            */
            return this;
        }
    }
    public class GlobalVariableData
    {
        public Dictionary<string, MetaVariable> m_VariableDict = new Dictionary<string, MetaVariable>();
        public void Parse( MetaMemberData mmd )
        {
            if (mmd == null)
            {
                return;
            }
            foreach ( var v in mmd.metaMemberDataDict )
            {
                ProjectManager.globalData.AddMetaMemberData(v.Value);
            }
        }
    }
    public class GlobalImportData
    {
        public List<string> globalImportList = new List<string>();

        public void Parse( MetaMemberData mmd )
        {
            if (mmd == null)
            {
                return;
            }
            /*
            for (int i = 0; i < m_FileMetaMemberData.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = m_FileMetaMemberData.fileMetaMemberData[i];

                var mcv = cfmmd.fileMetaMemberData[i].fileMetaConstValue;
                if (mcv != null && mcv?.token?.GetEType() == EType.String)
                {
                    globalImportList.Add(mcv.token.lexeme.ToString());
                }
            }
            */
        }
    }
    public class GlobalReplaceData
    {
        private Dictionary<string, string> repaceStringDict = new Dictionary<string, string>();

        public void Parse( MetaMemberData mmd )
        {
            if (mmd == null)
            {
                return;
            }
            /*
            for (int i = 0; i < m_FileMetaMemberData.fileMetaMemberData.Count; i++)
            {
                FileMetaMemberData cfmmd = m_FileMetaMemberData.fileMetaMemberData[i];
                if (cfmmd.DataType == FileMetaMemberData.EMemberDataType.KeyValue)
                {
                    string orgString = cfmmd.token?.lexeme.ToString();
                    string replaceString = cfmmd?.fileMetaConstValue?.token?.lexeme.ToString();

                    if (repaceStringDict.ContainsKey(orgString))
                    {
                        Console.Write("Error GlobalReplace中，存在两个相同的关键字!!");
                        continue;
                    }
                    repaceStringDict.Add(orgString, replaceString);
                }
            }
            */
        }
    }
    public class ExportDllData
    {
        public string spaceName { get; set; } = "";
        public List<DefineNamespace> childDefineNamespace = new List<DefineNamespace>();

        public void Parse(MetaMemberData mmd)
        {
            if (mmd == null)
            {
                return;
            }
            //spaceName = m_FileMetaMemberData.name;

            //for (int i = 0; i < root.fileMetaMemberData.Count; i++)
            //{
            //}
            return;
        }
    }
    public class MemberSetData
    {
        public void Parse(MetaMemberData mmd)
        {
            if (mmd == null)
            {
                return;
            }
            //spaceName = m_FileMetaMemberData.name;

            //for (int i = 0; i < root.fileMetaMemberData.Count; i++)
            //{
            //}
            return;
        }
    }

    public class ProjectData : MetaData
    {
        public ProjectData(string _name, bool isConst) : base(_name, isConst)
        {

        }
        public string projectName { get; set; }
        public string projectDesc { get; set; }
        public int mainVersion { get; set; } = 0;
        public int subVersion { get; set; } = 0;
        public int buildVersion { get; set; } = 0;
        public int buildSubVersion { get; set; } = 0;
        public bool isUseForceSemiColonInLineEnd { get; set; } = true;
        public CompileFileData compileFileData { get; set; } = new CompileFileData();
        public CompileOptionData compileOptionData { get; set; } = new CompileOptionData();
        public CompileFilterData compileFilterData { get; set; } = new CompileFilterData();
        public DefineNamespace namespaceRoot { get; set; } = new DefineNamespace();
        public GlobalImportData globalImportData { get; set; } = new GlobalImportData();
        public GlobalReplaceData globalReplaceData { get; set; } = new GlobalReplaceData();
        public GlobalVariableData globalVariableData { get; set; } = new GlobalVariableData();
        public ExportDllData exportDllData { get; set; } = new ExportDllData();
        public CompileModuleData compileModuleData { get; set; } = new CompileModuleData();
        public ImportModuleData importModuleData { get; set; } = new ImportModuleData();
        public MemberSetData memberSetData { get; set; } = new MemberSetData();

        public override void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass(this);
            m_FileMetaClassDict.Add(fmc.token, fmc);

            bool isHave = false;
            for (int i = 0; i < fmc.memberDataList.Count; i++)
            {
                var v = fmc.memberDataList[i];
                MetaBase mb = GetChildrenMetaBaseByName(v.name);
                if (mb != null)
                {
                    Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                if (v.name == "globalVariable")
                    continue;

                MetaMemberData mmd = new MetaMemberData(this, v);
                if (isHave)
                {
                    mmd.SetName(mmd.name + "__repeat__");
                }
                AddMetaMemberData(mmd);

                mmd.ParseChildMemberData();

                ParseBlockNode(mmd);
            }
            if (fmc.memberVariableList.Count > 0 || fmc.memberFunctionList.Count > 0)
            {
                Console.WriteLine("Error Data中不允许有Variable 和 Function!!");
            }
        }

        public void ParseBlockNode(MetaMemberData mmd)
        {
            if (mmd == null) return;

            string _name = mmd.name;
            switch (_name)
            {
                case "name":
                case "desc":
                    {
                        projectName = mmd.GetString(_name);
                    }
                    break;
                case "mainVersion":
                case "subVersion":
                case "buildVersion":
                case "buildSubVersion":
                    {
                        buildSubVersion = mmd.GetInt(_name);
                    }
                    break;
                case "compileFileList":
                    {
                        compileFileData.Parse(mmd);
                    }
                    break;
                case "compileOption":
                    {
                        compileOptionData.Parse(mmd);
                    }
                    break;
                case "compileFilter":
                    {
                        compileFilterData.Parse(mmd);
                    }
                    break;
                case "importModule":
                    {
                        importModuleData.Parse(mmd);
                    }
                    break;
                case "globalVariable":
                    {
                        globalVariableData.Parse(mmd);
                    }
                    break;
                case "globalNamespace":
                    {
                        namespaceRoot.Parse(mmd);
                    }
                    break;
                case "globalImport":
                    {
                        globalImportData.Parse(mmd);
                    }
                    break;
                case "globalReplace":
                    {
                        globalReplaceData.Parse(mmd);
                    }
                    break;
                case "exportDll":
                    {
                        exportDllData.Parse(mmd);
                    }
                    break;
                case "memberSet":
                    {
                        memberSetData.Parse(mmd);
                    }
                    break;
            }
        }

        public void ParseGlobalVariable()
        {
            if (m_FileMetaClassDict.Count == 0)
                return;
            FileMetaMemberData fmd = null;
            foreach( var v in m_FileMetaClassDict )
            {
                fmd = v.Value.GetFileMemberData("globalVariable");
                if (fmd != null)
                    continue;
            }

            if(fmd == null )
            {
                return;
            }

            MetaBase mb = GetChildrenMetaBaseByName(fmd.name);
            if (mb != null)
            {
                Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + fmd.token?.ToLexemeAllString() + "的元素!!");
                return;
            }

            MetaMemberData mmd = new MetaMemberData(this, fmd );
            AddMetaMemberData(mmd);
            mmd.ParseChildMemberData();
            ParseBlockNode(mmd);
        }
    }

    public class ProjectParse
    {
        ProjectData m_ProjectData = null;

        FileMeta m_FileMetaData = null;
        public ProjectParse( FileMeta fm, ProjectData pd )
        {
            m_FileMetaData = fm;

            m_ProjectData = pd;
        }
        public void ParseProject()
        {
            if( m_FileMetaData == null )
            {
                Console.Write("Error 没有解析成功Config.sp文件!!");
                return;
            }
            FileMetaClass fmc = m_FileMetaData.GetFileMetaClassByName("ProjectConfig");
            if(fmc == null )
            {
                Console.Write("Error 解析工程文件，没有找到ProjectConfig!!");
                return;
            }
            m_ProjectData.BindFileMetaClass(fmc);
            m_ProjectData.Parse();

        }
        public void ParseInitClassAfter()
        {
            if(m_ProjectData == null)
            {
                return;
            }
            m_ProjectData.ParseGlobalVariable();

            m_ProjectData.ParseDefineComplete();

            m_ProjectData.metaVariable.isGlobal = true;
        }
    }
}