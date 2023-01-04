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
        [JsonIgnore]
        public ECompileState compileState;
        public int priority { get; set; }

        public void Parse()
        {

        }
    }
    public class CompileModuleData
    {
        public string name { get; set; }
        public string path { get; set; }
    }
    public class CompileFilterData
    {
        [JsonPropertyName("group")]
        public List<string> groupList { get; set; }
        [JsonPropertyName("tag")]
        public List<string> tagList { get; set; }

        [JsonIgnore]
        public bool isAllGroup { get; set; } = false;
        [JsonIgnore]
        public bool isAllTag { get; set; } = false;
        public void Parse()
        {
            if(groupList != null )
            {
                for( int i = 0; i < groupList.Count; i++ )
                {
                    if(groupList.Contains("all" ) )
                    {
                        isAllGroup = true;
                        break;
                    }
                }
            }
            else
            {
                isAllGroup = true;
            }
            if(tagList != null )
            {
                for (int i = 0; i < tagList.Count; i++)
                {
                    if (tagList.Contains("all"))
                    {
                        isAllTag = true;
                        break;
                    }
                }
            }
            else
            {
                isAllTag = true;
            }
        }
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
    }
    public class CompileOptionData
    {
        public bool isForceUseClassKey { get;set; }
    }
    public class ProjectData
    {
        public string projectName { get; set; }
        public string projectDesc { get; set; }
        public List<CompileFileData> compileFileList { get; set; }
        public CompileOptionData compileOptionData { get; set; }
        public CompileFilterData filter { get; set; }
        public List<string> globalNamespaceList { get; set; }
        public List<CompileModuleData> compileModuleList { get; set; }
        public void Parse()
        {
            if(compileFileList != null )
            {
                for( int i = 0; i < compileFileList.Count; i++ )
                {
                    compileFileList[i].Parse();
                }
            }
            if (filter != null)
            {
                filter.Parse();
            }
        }
    }

    public class ProjectLoad
    {
        public ProjectData data => m_Data;

        private ProjectData m_Data = null;
        private string m_ProjectPath;
        private string m_FileContentString = null;
        private FileMeta m_File = null;
        private LexerParse m_LexerParse = null;
        private TokenParse m_TokenParse = null;
        private ProjectParse m_ProjectBuild = null;

        public ProjectLoad( string path )
        {
            m_ProjectPath = path;

            m_File = new FileMeta(m_ProjectPath);
        }
        public void Load()
        {
            if (!File.Exists(m_ProjectPath))
            {
                Console.WriteLine("Error 项目加载路径不正确!!");
                return;
            }

            byte[] buffer = File.ReadAllBytes(m_ProjectPath);
            m_FileContentString = System.Text.Encoding.UTF8.GetString(buffer);

            m_LexerParse = new LexerParse(m_ProjectPath, m_FileContentString);
            m_LexerParse.ParseToTokenList();

            m_TokenParse = new TokenParse(m_File, m_LexerParse.GetListTokensWidthEnd());

            m_TokenParse.BuildStruct();

            m_ProjectBuild = new ProjectParse(m_File, m_TokenParse.rootNode);

            m_ProjectBuild.ParseRootNodeToFileMeta();

            //ProjectCompile.compileFunction = new ProjectCompileFunction(projectPath, functionContent);
        }
    }
}
