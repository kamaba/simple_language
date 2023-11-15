//****************************************************************************
//  File:      IRMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Reflection;

namespace SimpleLanguage.Core
{
    public partial class MetaClass
    {
        public List<MetaMemberVariable> localMetaMemberVariables => m_LocalMetaMemberVariables;

        public int allocSize = 0;
        List<MetaMemberVariable> m_LocalMetaMemberVariables = new List<MetaMemberVariable>();
        public List<EType> m_MetaTypeList = new List<EType>();
        public int byteCount = 0;

        public int GetLocalMemberVariableIndex(MetaMemberVariable mmv)
        {
            for (int i = 0; i < m_LocalMetaMemberVariables.Count; i++)
            {
                if (m_LocalMetaMemberVariables[i] == mmv)
                    return i;
            }
            return -1;
        }
        public void CalcAllocSize()
        {
            m_MetaTypeList.Clear();
            foreach (var v in m_LocalMetaMemberVariables)
            {
                if (v.isInnerDefine == false)
                {
                    m_MetaTypeList.Add(v.metaDefineType.metaClass.eType);
                }
            }
            int count = 0;
            int ssize = 0;
            for (int i = 0; i < m_MetaTypeList.Count; i++)
            {
                ssize = IR.IRUtil.GetTypeSize(m_MetaTypeList[i]);
                count += ssize;
                byteCount += ssize;
            }
        }

        public int GetSize()
        {
            return 100;
        }
        public void CreateMetaClassData()
        {
            m_LocalMetaMemberVariables = GetMetaMemberVariableListByFlag(false, false);
            CalcAllocSize();
        }
    }
}
