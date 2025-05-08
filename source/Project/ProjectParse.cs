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

            public CompileFileDataUnit(MetaMemberData mmd )
            {
                var findPath = mmd.GetString("path", true);
                if (findPath == null)
                {
                    Console.Write("在ProjectConfig 中的CompileFileData必须有path节点!!");
                    return;
                }
                path = findPath;

                //var findPath = mdc.GetMetaMemberVariableByName("path");
                //if (findPath == null)
                //{
                //    Console.Write("在ProjectConfig 中的CompileFileData必须有path节点!!");
                //    return;
                //}
                //path = findPath.constExpressNode.ToTokenString();
                //var findOption = mdc.GetMetaMemberVariableByName("option");
                //var findGroup = mdc.GetMetaMemberVariableByName("group");
                ////group = findGroup?.fileMetaConstValue?.token?.lexeme.ToString();
                //var findTag = mdc.GetMetaMemberVariableByName("tag");
                ////tag = findTag?.fileMetaConstValue?.token?.lexeme.ToString();
                //var findIgnore = mdc.GetMetaMemberVariableByName("ignore");

                //object obj = findIgnore?.fileMetaConstValue?.token?.lexeme;
                //if (obj != null && (bool)obj == true)
                //{
                //    compileState = ECompileState.Ignore;
                //}
                //else
                //{
                    compileState = ECompileState.Default;
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
            //var findIsForceUseClassKey = mmd.GetFileMetaMemberDataByName("isForceUseClassKey");
            //var findIsSupportDoublePlus = mmd.GetFileMetaMemberDataByName("isSupportDoublePlus");

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
        public bool Parse(MetaMemberData mmd )
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

        public void Parse(MetaMemberVariable mmd)
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
        public DefineNamespace Parse(MetaMemberData mmd )
        {
            if (mmd == null)
            {
                return null;
            }
            spaceName = mmd.name;

            foreach (var ns in mmd.childrenNameNodeDict)
            {
                var dn = new DefineNamespace();
                MetaMemberData cmmd = ns.Value as MetaMemberData;
                if (cmmd != null)
                {
                    dn.Parse(cmmd);
                }
                childDefineNamespace.Add(dn);

            }
            return this;
        }
    }
    public class GlobalVariableData
    {
        public Dictionary<string, MetaVariable> m_VariableDict = new Dictionary<string, MetaVariable>();
        public void Parse(MetaMemberData mmd )
        {
            if (mmd == null)
            {
                return;
            }
            //foreach ( var v in mmd.metaMemberDataDict )
            //{
            //    ProjectManager.globalData.AddMetaMemberData(v.Value);
            //}
        }
    }
    public class GlobalImportData
    {
        public List<string> globalImportList = new List<string>();

        public void Parse(MetaMemberData mmd )
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

        public void Parse(MetaMemberData mmd )
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
    public class MemorySetData
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
        public ProjectData(string _name, bool isConst) : base(_name, isConst, false )
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
        public MemorySetData memorySetData { get; set; } = new MemorySetData();
        public override void ParseFileMetaDataMemeberData(FileMetaClass fmc)
        {
            m_Deep = 0;
            for (int i = 0; i < fmc.memberDataList.Count; i++)
            {
                var v = fmc.memberDataList[i];
                
                if (v.name == "globalVariable")
                    continue;

                MetaMemberData mmd = new MetaMemberData(this, v, i);
                mmd.ParseName();
                mmd.ParseDefineMetaType();
                mmd.ParseMetaExpress();
                AddMetaMemberData(mmd);
                mmd.ParseChildMemberData();

                ParseBlockNode(mmd);
            }
            int ct = this.metaMemberDataDict.Count;
            if ( fmc.memberVariableList.Count > 0 || fmc.memberFunctionList.Count > 0)
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
                case "memorySet":
                    {
                        memorySetData.Parse(mmd);
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
                    break;
            }

            if( fmd == null )
            {
                return;
            }

            MetaBase mb = ProjectManager.globalData.GetChildrenMetaBaseByName(fmd.name);
            if (mb != null)
            {
                Console.WriteLine("Error ProjectParse ParseGlobalVariable已有定义类: " + allName + "中 已有: " + fmd.token?.ToLexemeAllString() + "的元素!!");
                return;
            }
            //需要在做Data处理的时候 ，再处理该逻辑
            MetaMemberData mmd = new MetaMemberData(ProjectManager.globalData, fmd, ProjectManager.globalData.metaMemberDataDict.Count );
            mmd.ParseName();
            mmd.ParseDefineMetaType();
            mmd.ParseMetaExpress();
            ProjectManager.globalData.AddMetaMemberData(mmd);
        }
    }
    public class ProjectParse
    {
        ProjectData m_ProjectData = null;

        readonly FileMeta m_FileMetaData = null;
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
            m_ProjectData.ParseFileMetaDataMemeberData(fmc);
            m_ProjectData.ParseMetaMemberVariableName();
            m_ProjectData.ParseMetaMemberFunctionName();
            m_ProjectData.ParseMemberVariableDefineMetaType();
            m_ProjectData.ParseMemberFunctionDefineMetaType();
        }
        public void ParseGlobalVariable()
        {
            if(m_ProjectData == null)
            {
                return;
            }
            m_ProjectData.ParseGlobalVariable();
        }
        public void ParseDefineComplete()
        {

            if (m_ProjectData == null)
            {
                return;
            }
            m_ProjectData.ParseDefineComplete();

            //m_ProjectData.metaVariable.isGlobal = true;
        }
    }
}