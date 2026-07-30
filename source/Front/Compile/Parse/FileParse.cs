//****************************************************************************
//  File:      FileParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.IO;
using System.Text;

namespace SimpleLanguage.Compile
{
    public struct ParseFileParam
    {

    }
    public class FileParse
	{
		public FileMeta file => m_File;
        public string filePath => m_FilePath;

        private LexerParse m_LexerParse;
        private TokenParse m_TokenParse;
        private StructParse m_StructBuild;
        private FileMeta m_File = null;

        private string m_FilePath;
        private long m_FileSize;
        private char[] m_ContentBuffer = null;

        public Action structParseComplete { get; set; } = null;
        public Action buildParseComplete = null;
        public Action grammerParseComplete = null;

        FileCompileState m_FileCompileState = new FileCompileState();

        public FileParse( string path, ParseFileParam param )
        {
            m_FilePath = path;
            m_File = new FileMeta(m_FilePath);
        }
        public bool IsExists()
        {
            string realpath = Path.Combine(ProjectManager.projectPath, m_FilePath );
            return File.Exists(realpath);
        }
        public bool LoadFile()
        {
            Log.AddProcessLog( LID.ProcessCompileFileStart, "", m_FilePath );
            m_FileCompileState.SetLoadState( FileCompileState.ELoadState.LoadStart );
            string realpath = Path.Combine(ProjectManager.projectPath, m_FilePath);
            using (var stream = File.OpenRead(realpath))
            {
                m_FileCompileState.SetLoadState(FileCompileState.ELoadState.Loading );
                m_FileSize = stream.Length;
                int count = (int)m_FileSize;
                var buffer = new byte[m_FileSize];
                int numRead = 0;
                while (true)
                {
                    int n = stream.Read(buffer, numRead, count);
                    if (n == 0) break;
                    numRead += n;
                    count -= n;
                    if (count <= 0) break;
                }
                stream.Close();
                m_ContentBuffer = Encoding.UTF8.GetChars(buffer);                
            }
            m_FileCompileState.SetLoadState( FileCompileState.ELoadState.LoadEnd );
            return true;
        }
        public void StructParse()
        {
            if( LoadFile() )
            {
                SaveCodeToFile();

                m_LexerParse = new LexerParse( m_FilePath, m_ContentBuffer );

                m_LexerParse.ParseToTokenList();
#if DEBUG
                m_LexerParse.DumpTokensToFile();
#endif

                m_TokenParse = new TokenParse( m_File, m_LexerParse.listTokens );

                m_TokenParse.BuildStruct();
#if DEBUG
                m_TokenParse.WriteNodeString(true);
#endif

                m_StructBuild = new StructParse(m_File, m_TokenParse.rootNode );

                m_StructBuild.ParseRootNodeToFileMeta();

                m_File.SetDeep(0);
#if DEBUG
                ExportFileMetaDebugData();
#endif

                Log.AddProcessLog(LID.ProcessCompileFileCompleted, "", m_FilePath );

                if (structParseComplete != null )
                {
                    structParseComplete();
                }
            }
            else
            {
                Log.AddProcessLog( LID.ProcessLoadFileFailed, "", m_FilePath );
            }
        }
        public void CreateNamespace()
        {
            m_File.CreateNamespace();
        }
        public void CombineFileMeta()
        {
            m_File.CombineFileMeta();
        }
        public string ToFormatString()
        {
            return m_File.ToFormatString();
        }
        public void PrintFormatString()
        {
            Log.AddFileMetaLog(LID.ShowExtendMessage, m_File.ToFormatString());
        }
        public void SaveCodeToFile()
        {
            if (!Common.ShouldExportDebugText("Code.txt"))
            {
                return;
            }
            string outPath = Common.GetDebugCodeFilePath(m_FilePath, "Code.txt");
            File.WriteAllText(outPath, new string(m_ContentBuffer));
        }

        public void ExportFileMetaDebugData()
        {
            try
            {
                if (!Common.ShouldExportDebugText("File.txt"))
                {
                    return;
                }
                string outPath = Common.GetDebugCodeFilePath(m_FilePath, "File.txt");
                File.WriteAllText(outPath, m_File?.ToFormatString() ?? string.Empty);
            }
            catch (Exception e)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage,  "Export FileMeta debug data failed: " + e.Message);
            }
        }

        public override string ToString()
        {
            return filePath;
        }

        public void ExportMetaDebugData()
        {
            try
            {
                if (!Common.ShouldExportDebugText("Meta.txt"))
                {
                    return;
                }
                string outPath = Common.GetDebugCodeFilePath(m_FilePath, "Meta.txt");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("-------------------Meta 文件显示 开始 : Path: " + m_FilePath + "-----------------------");

                var classList = m_File?.fileMetaClassList;
                if (classList != null)
                {
                    for (int i = 0; i < classList.Count; i++)
                    {
                        var metaClass = classList[i]?.metaClass;
                        if (metaClass == null) continue;
                        sb.AppendLine(metaClass.ToFormatString());
                    }
                }

                sb.AppendLine("-------------------Meta 文件显示 结束 : -----------------------");
                File.WriteAllText(outPath, sb.ToString());
            }
            catch (Exception e)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Export Meta debug data failed: " + e.Message);
            }
        }
    }
}