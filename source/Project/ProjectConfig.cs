using System.Collections.Generic;

namespace SimpleLanguage.Project
{
    // Strongly-typed representation of project <ProjectName>.toml
    public class ProjectConfig
    {
        public ProjectSection Project { get; set; } = new ProjectSection();
        public SourceSection Source { get; set; } = new SourceSection();
        public CompileSection Compile { get; set; } = new CompileSection();
        public CompileFilesSection CompileFiles { get; set; } = new CompileFilesSection();
        public CompileFilterSection CompileFilter { get; set; } = new CompileFilterSection();
        public GlobalSection Global { get; set; } = new GlobalSection();
        public StructTreeNode StructTree { get; set; } = new StructTreeNode();
        public List<ReferenceSection> References { get; set; } = new List<ReferenceSection>();

        public class ProjectSection
        {
            public string Name { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public int MainVersion { get; set; } = 0;
            public int SubVersion { get; set; } = 0;
            public int BuildVersion { get; set; } = 0;
            public int BuildSubVersion { get; set; } = 0;
        }

        public class SourceSection
        {
            public string Root { get; set; } = "source";
            public string EntryFile { get; set; } = "Program.sl";
        }

        public class StructTreeNode
        {
            public enum NodeType
            {
                Root,
                Namespace,
                Class,
                Data,
                Interface,
                Enum,
                Method,
                Property,
                Field
            }
            public string Name { get; set; } = string.Empty;
            public NodeType Type { get; set; }  = NodeType.Root;
            public List<StructTreeNode> Children { get; set; } = new List<StructTreeNode>();
    
            // Build or extend a path under this node using a dotted name like "Std.Console".
            // The last segment gets the specified leafType; intermediate segments default to Namespace.
            public StructTreeNode EnsurePath(string dottedName, NodeType leafType)
            {
                if (string.IsNullOrEmpty(dottedName))
                {
                    return this;
                }

                var parts = dottedName.Split('.');
                var current = this;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool isLeaf = (i == parts.Length - 1);
                    var expectedType = isLeaf ? leafType : NodeType.Namespace;

                    // Try to find an existing child with same name and type
                    StructTreeNode child = null;
                    for (int j = 0; j < current.Children.Count; j++)
                    {
                        var c = current.Children[j];
                        if (c.Name == part && c.Type == expectedType)
                        {
                            child = c;
                            break;
                        }
                    }

                    if (child == null)
                    {
                        child = new StructTreeNode
                        {
                            Name = part,
                            Type = expectedType
                        };
                        current.Children.Add(child);
                    }

                    current = child;
                }

                return current;
            }
        }

        public class CompileSection
        {
            public bool Optimize { get; set; }
            public string Target { get; set; } = "AnyCPU";
            public bool Debug { get; set; } = true;
            public bool IsUseForceSemiColonInLineEnd { get; set; } = true;
            public bool IsForceUseClassKey { get; set; }
            public bool IsSupportDoublePlus { get; set; }
        }

        // mirror CompileFileData / CompileFileDataUnit
        public class CompileFilesSection
        {
            public List<CompileFileItem> Files { get; set; } = new List<CompileFileItem>();
        }

        public class CompileFileItem
        {
            public string Path { get; set; } = string.Empty;
            public string Group { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public bool Ignore { get; set; } = false;
            public int Priority { get; set; } = 0;
        }

        // mirror CompileFilterData
        public class CompileFilterSection
        {
            public List<string> Groups { get; set; } = new List<string>();
            public List<string> Tags { get; set; } = new List<string>();
            public bool IsAllGroup { get; set; } = false;
            public bool IsAllTag { get; set; } = false;

            public bool IsIncludeInGroup(string group)
            {
                if (IsAllGroup) return true;
                if (Groups == null || Groups.Count == 0) return true;
                return Groups.Contains(group);
            }

            public bool IsIncludeInTag(string tag)
            {
                if (IsAllTag) return true;
                if (Tags == null || Tags.Count == 0) return true;
                return Tags.Contains(tag);
            }
        }

        // merge several global-related pieces into one section
        public class GlobalSection
        {
            public List<string> Imports { get; set; } = new List<string>();
            public Dictionary<string, string> Replace { get; set; } = new Dictionary<string, string>();
        }

        public class ReferenceSection
        {
            public string Path { get; set; } = string.Empty;
        }
    }
}
