//****************************************************************************
//  File:      ProjectParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/4 12:00:00
//  Description:  
//****************************************************************************


using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Compile;

namespace SimpleLanguage.Project
{
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

            for( int i = 0; i < fmc.memberDataList.Count; i++ )
            {
                ParseBlockNode(fmc.memberDataList[i]);
            }

        }
        public void ParseBlockNode( FileMetaMemberData fmmd )
        {
            if (fmmd == null) return;

            string _name = fmmd.name;
            switch ( _name )
            {
                case "name":
                    {
                        m_ProjectData.projectName = fmmd.fileMetaConstValue?.token?.lexeme.ToString();
                    }
                    break;
                case "desc":
                    {
                        m_ProjectData.projectDesc = fmmd.fileMetaConstValue?.token?.lexeme.ToString();
                    }
                    break;
                case "mainVersion":
                    {
                        m_ProjectData.mainVersion = int.Parse(fmmd.fileMetaConstValue?.token?.lexeme.ToString());
                    }
                    break;
                case "subVersion":
                    {
                        m_ProjectData.subVersion = int.Parse(fmmd.fileMetaConstValue?.token?.lexeme.ToString());
                    }
                    break;
                case "buildVersion":
                    {
                        m_ProjectData.buildSubVersion = int.Parse(fmmd.fileMetaConstValue?.token?.lexeme.ToString());
                    }
                    break;
                case "buildSubVersion":
                    {
                        m_ProjectData.buildSubVersion = int.Parse(fmmd.fileMetaConstValue?.token?.lexeme.ToString());
                    }
                    break;
                case "compileFileList":
                    {
                        for (int i = 0; i < fmmd.fileMetaMemberData.Count; i++)
                        {
                            m_ProjectData.compileFileList.Add(new CompileFileData(fmmd.fileMetaMemberData[i]));
                        }
                    }
                    break;
                case "compileOption":
                    {
                        m_ProjectData.compileOptionData = new CompileOptionData(fmmd);
                    }
                    break;
                case "compileFilter":
                    {
                        m_ProjectData.compileFilter = new CompileFilterData(fmmd);
                        m_ProjectData.compileFilter.Parse();
                    }
                    break;
                case "importModule":
                    {
                        for (int i = 0; i < fmmd.fileMetaMemberData.Count; i++)
                        {
                            var fmmdc = new ImportModuleData(fmmd.fileMetaMemberData[i]);
                            if( fmmdc.Parse() )
                            {
                                m_ProjectData.importModuleList.Add(fmmdc);
                            }
                        }
                    }
                    break;
                case "globalNamespace":
                    {
                        var dns = new DefineNamespace();
                        var nns = dns.Parse(fmmd);
                        m_ProjectData.namespaceRoot = nns;
                    }
                    break;
                case "globalImport":
                    {
                        m_ProjectData.globalImportData = new GlobalImportData(fmmd);
                    }
                    break;
                case "globalReplace":
                    {
                        m_ProjectData.globalReplaceData = new GlobalReplaceData(fmmd);
                    }
                    break;
                case "exportDll":
                    {
                        m_ProjectData.exportDllData = new ExportDllData(fmmd);
                    }
                    break;
                case "memberSet":
                    {

                    }
                    break;
            }
        }
    }
}