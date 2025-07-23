//****************************************************************************
//  File:      FileMetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public class FileMetaMemberData : FileMetaBase
    {
        public enum EMemberDataType
        {
            Data,
            Array,
            ConstValue,
            Class,
            //KeyValueData,           // $name = {}$
            //KeyValueArray,          // $name = []$
            //KeyValueConstValue,          // $name = 1$
            //KeyValueClass,          // $name = Class1()$
            //NonNameData,            // [ ${}$ ]
            //NonNameArray,           // [ $[] ]
            //NonNameConstValue,           // [ $1$ ]
        }

        public Token nameToken => m_Token;
        public List<FileMetaMemberData> fileMetaMemberData => m_FileMetaMemberData;
        public FileMetaConstValueTerm fileMetaConstValue => m_FileMetaConstValue;
        public FileMetaCallTerm fileMetaCallTermValue => m_FileMetaCallTermValue;
        public bool isWithName => m_IsWithName;
        public EMemberDataType DataType => m_MemberDataType;

        private Token m_AssignToken = null;
        private List<Node> m_NodeList = new List<Node>();
        private List<FileMetaMemberData> m_FileMetaMemberData = new List<FileMetaMemberData>();
        private EMemberDataType m_MemberDataType = EMemberDataType.Data;
        private FileMetaConstValueTerm m_FileMetaConstValue = null;
        private FileMetaCallTerm m_FileMetaCallTermValue = null;
        private bool m_IsWithName = false;

        public FileMetaMemberData( FileMeta fm, Node node, bool isWithName, EMemberDataType dataType )
        {
            m_MemberDataType = dataType;
            m_FileMeta = fm;
            m_Token = node.token;
            m_IsWithName = isWithName;



            if (m_MemberDataType == EMemberDataType.ConstValue)
            {
                m_FileMetaConstValue = new FileMetaConstValueTerm(m_FileMeta, m_Token );

            }
            else if (dataType == EMemberDataType.Class)
            {
                m_FileMetaCallTermValue = new FileMetaCallTerm(m_FileMeta, node);
            }
        }
        public FileMetaMemberData(FileMeta fm, List<Node> frontList, Node assignNode, List<Node> backList, bool isWithName, EMemberDataType dataType )
        {
            m_FileMeta = fm;
            m_MemberDataType = dataType;
            m_IsWithName = isWithName;

            if(m_IsWithName )
            {
                if (frontList.Count > 0)
                {
                    m_Token = frontList[0].token;
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 带着名称FileMetaMemberData 但没有发现相关的token");
                }
            }
            if (dataType == EMemberDataType.Data)
            {
            }
            else if (m_MemberDataType == EMemberDataType.Array )
            {
            }
            else if(m_MemberDataType == EMemberDataType.ConstValue)
            {
                if( backList.Count > 0 )
                {
                    Token signalToken = null;
                    Token valueToken = null;
                    foreach( Node node in backList)
                    {
                        if (node.nodeType == ENodeType.Symbol)
                        {
                            signalToken = node.token;
                        }
                        else
                        {
                            valueToken = node.token;
                        }

                    }
                    m_FileMetaConstValue = new FileMetaConstValueTerm(m_FileMeta, valueToken, signalToken );
                }
            }
            else if (dataType == EMemberDataType.Class )
            {
                if( backList.Count > 0 )
                {
                    m_FileMetaCallTermValue = new FileMetaCallTerm(m_FileMeta, backList[backList.Count - 1]);
                }
            }
        }
        public void AddFileMemberData(FileMetaMemberData fmmd)
        {
            m_FileMetaMemberData.Add(fmmd);
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaMemberData.Count; i++)
            {
                m_FileMetaMemberData[i].SetDeep(m_Deep + 1);
            }
        }
        public void SetMetaMemberData(MetaMemberData mmd)
        {
        }
        public FileMetaMemberData GetFileMetaMemberDataByName(string name)
        {
            FileMetaMemberData fmmd = m_FileMetaMemberData.Find(a => a.name == name);
            return fmmd;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if ( m_IsWithName )
            {
                sb.Append(name + " = ");
            }
            if (m_MemberDataType == EMemberDataType.Data )
            {
                sb.AppendLine("{");
                for (int i = 0; i < m_FileMetaMemberData.Count; i++)
                {
                    sb.AppendLine(m_FileMetaMemberData[i].ToFormatString());
                }
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("}");
            }
            else if (m_MemberDataType == EMemberDataType.Array)
            {
                sb.Append("[");
                for (int i = 0; i < m_FileMetaMemberData.Count; i++)
                {
                    sb.Append(m_FileMetaMemberData[i].ToFormatString());
                    if (i < m_FileMetaMemberData.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
            }
            else if (m_MemberDataType == EMemberDataType.ConstValue )
            {
                sb.Append(this.m_FileMetaConstValue.ToFormatString());
            }
            else if (m_MemberDataType == EMemberDataType.Class)
            {
                sb.Append(m_FileMetaCallTermValue.ToFormatString());
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_IsWithName)
            {
                sb.AppendLine(name + " = ");
            }
            if (m_MemberDataType == EMemberDataType.Data)
            {
                sb.AppendLine("{");
                for (int i = 0; i < m_FileMetaMemberData.Count; i++)
                {
                    sb.Append(m_FileMetaMemberData[i].ToString());
                }
                sb.Append("}");
            }
            else if (m_MemberDataType == EMemberDataType.Array)
            {
                sb.Append("[");
                for (int i = 0; i < m_FileMetaMemberData.Count; i++)
                {
                    sb.Append(m_FileMetaMemberData[i].ToFormatString());
                    if (i < m_FileMetaMemberData.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
            }
            else if (m_MemberDataType == EMemberDataType.ConstValue)
            {
                sb.Append(m_FileMetaConstValue.ToFormatString());
            }
            else if (m_MemberDataType == EMemberDataType.Class)
            {
                sb.Append(m_FileMetaCallTermValue.ToFormatString());
            }            
            return sb.ToString();
        }
    }
}