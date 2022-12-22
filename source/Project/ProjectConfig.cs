using SimpleLanguage.Project;
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
        [JsonPropertyName("name")]
        public string projectName { get; set; }
        [JsonPropertyName("desc")]
        public string projectDesc { get; set; }
        [JsonPropertyName("compileFileList")]
        public List<CompileFileData> compileFileList { get; set; }
        [JsonPropertyName("option")]
        public CompileOptionData compileOptionData { get; set; }
        [JsonPropertyName("filter")]
        public CompileFilterData filter { get; set; }
        [JsonPropertyName("globalNamespace")]
        public List<string> globalNamespaceList { get; set; }
        [JsonPropertyName("module")]
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
    public class ProjectConfig
    {
        public string projectPath;
        public ProjectData data;
        public string fileContentString = null;
        public string projectContentString = null;

        public ProjectConfig( string path )
        {
            projectPath = path;
        }
        public void Load()
        {
            if (!File.Exists(projectPath))
            {
                return;
            }
            byte[] buffer = File.ReadAllBytes(projectPath);
            fileContentString = System.Text.Encoding.UTF8.GetString(buffer);


            int beginIndex = fileContentString.IndexOf("ProjectConfigBegin{");
            int endIndex = fileContentString.IndexOf("}ProjectConfigEnd");

            projectContentString = fileContentString.Substring(beginIndex + 18, endIndex - beginIndex - 17 );

            var options = new JsonSerializerOptions { 
                WriteIndented = true,
                PropertyNameCaseInsensitive = false
            };
            data = JsonSerializer.Deserialize<ProjectData>(projectContentString, options);

            data.Parse();

            string functionContent = fileContentString.Substring(0, beginIndex);

            ProjectCompile.compileFunction = new ProjectCompileFunction(projectPath, functionContent);
        }
    }
}
