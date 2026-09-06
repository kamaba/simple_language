//****************************************************************************
//  File:      FileParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.Process;
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
        public FileCompileState compileState => m_FileCompileState;

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

        FileCompileState m_FileCompileState = new FileCompileState("");

        public FileParse( string path, ParseFileParam param )
        {
            m_FilePath = path;
            m_File = new FileMeta(m_FilePath);
            m_FileCompileState = new FileCompileState(m_FilePath);
        }
        public bool IsExists()
        {
            string realpath = Path.Combine(ProjectManager.projectPath, m_FilePath );
            return File.Exists(realpath);
        }
        public bool LoadFile()
        {
            Log.AddProcessLog( LID.ProcessCompileFileStart, "", m_FilePath );
            string realpath = Path.Combine(ProjectManager.projectPath, m_FilePath);
            using (var stream = File.OpenRead(realpath))
            {
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
            return true;
        }

        /// <summary>文件阶段小步骤 1：读取文件 + Lexer 解析为 Token。单文件错误只影响本文件。</summary>
        public bool ParseTokenStep()
        {
            if (!m_FileCompileState.CanEnterNextStep())
            {
                return false;
            }
            int errorBegin = Log.errorCount;
            try
            {
                if (!LoadFile())
                {
                    m_FileCompileState.MarkFailed("文件读取失败");
                    Log.AddProcessLog(LID.ProcessLoadFileFailed, "", m_FilePath);
                    return false;
                }
                m_FileCompileState.SetStep(FileCompileState.EFileStep.Token);

                // C# 风格 @DllImport 无体函数声明先改写为两段式
                //（隐藏 Func 字段 + 静态 wrapper），后续 Token~Meta 全管线自然处理
                m_ContentBuffer = DllImportSourceRewriter.Rewrite( m_ContentBuffer, m_FilePath );

                SaveCodeToFile();

                m_LexerParse = new LexerParse( m_FilePath, m_ContentBuffer );
                m_LexerParse.ParseToTokenList();
#if DEBUG
                m_LexerParse.DumpTokensToFile();
#endif
            }
            catch (Exception e)
            {
                m_FileCompileState.MarkFailed("Token 解析异常");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "Token 解析异常: " + e.Message);
                return false;
            }
            if (Log.errorCount > errorBegin)
            {
                m_FileCompileState.MarkFailed("Token 解析存在错误");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "Token 解析存在错误");
                return false;
            }
            return true;
        }

        /// <summary>文件阶段小步骤 2：Token 解析为 Node。</summary>
        public bool ParseNodeStep()
        {
            if (!m_FileCompileState.CanEnterNextStep())
            {
                return false;
            }
            int errorBegin = Log.errorCount;
            try
            {
                m_TokenParse = new TokenParse( m_File, m_LexerParse.listTokens );
                m_TokenParse.BuildStruct();
#if DEBUG
                m_TokenParse.WriteNodeString(true);
#endif
                m_FileCompileState.SetStep(FileCompileState.EFileStep.Node);
            }
            catch (Exception e)
            {
                m_FileCompileState.MarkFailed("Node 解析异常");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "Node 解析异常: " + e.Message);
                return false;
            }
            if (Log.errorCount > errorBegin)
            {
                m_FileCompileState.MarkFailed("Node 解析存在错误");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "Node 解析存在错误");
                return false;
            }
            return true;
        }

        /// <summary>文件阶段小步骤 3：Node 解析为 FileMeta。</summary>
        public bool ParseFileStep()
        {
            if (!m_FileCompileState.CanEnterNextStep())
            {
                return false;
            }
            int errorBegin = Log.errorCount;
            try
            {
                m_StructBuild = new StructParse(m_File, m_TokenParse.rootNode );
                m_StructBuild.ParseRootNodeToFileMeta();
                m_File.SetDeep(0);
#if DEBUG
                ExportFileMetaDebugData();
#endif
                m_FileCompileState.SetStep(FileCompileState.EFileStep.File);
            }
            catch (Exception e)
            {
                m_FileCompileState.MarkFailed("File 解析异常");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "File 解析异常: " + e.Message);
                return false;
            }
            if (Log.errorCount > errorBegin)
            {
                m_FileCompileState.MarkFailed("File 解析存在错误");
                Log.AddProcessLog(LID.ProcessCompileFileFailed, "", m_FilePath, "File 解析存在错误");
                return false;
            }
            m_FileCompileState.MarkCompleted();
            Log.AddProcessLog(LID.ProcessCompileFileCompleted, "", m_FilePath );

            if (structParseComplete != null )
            {
                structParseComplete();
            }
            return true;
        }

        /// <summary>一次完成 Token -> Node -> File 三个小步骤（兼容旧流程）</summary>
        public bool StructParse()
        {
            return ParseTokenStep() && ParseNodeStep() && ParseFileStep();
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
                        sb.AppendLine(classList[i].ToFormatString());
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