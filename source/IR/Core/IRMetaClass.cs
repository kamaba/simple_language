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
using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public class IRMetaClass
    {
        static short s_TypeLength = 1000;
        public short id { get; set; } = 0;
        public IRMetaClass( IRManager manager )
        {
            irManager = manager;
            id = s_TypeLength++;
        }
        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;

        public int allocSize = 0;
        public List<EType> m_MetaTypeList = new List<EType>();
        public int byteCount = 0;
        public string allName { get; set; } = null;


        List<MetaMemberVariable> m_LocalMetaMemberVariables = new List<MetaMemberVariable>();
        List<MetaMemberData> m_LocalMetaMemberDatas = new List<MetaMemberData>();

        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        private IRManager irManager = null;
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
        public IRMetaVariable GetIRMetaVariable( int id )
        {
            return m_LocalIRMetaVariableList.Find( a=> a.id == id );
        }
        public void CreateMetaClassData( MetaClass mc )
        {
            allName = mc.allName;
            if (mc is MetaEnum me)
            {
            }
            else if (mc is MetaData md)
            {
                m_LocalMetaMemberDatas = md.GetMetaMemberDataList();
                foreach( var v in m_LocalMetaMemberDatas )
                {
                    IRMetaVariable irmv = new IRMetaVariable(v);
                    m_LocalIRMetaVariableList.Add(irmv);
                    irManager.allVariableList.Add(irmv);
                }
            }
            else
            {
                m_LocalMetaMemberVariables = mc.GetMetaMemberVariableListByFlag(false, false);
                foreach (var v in m_LocalMetaMemberVariables)
                {
                    IRMetaVariable irmv = new IRMetaVariable(v);
                    m_LocalIRMetaVariableList.Add(irmv);
                    irManager.allVariableList.Add(irmv);
                }
            }
            CalcAllocSize();
        }
        public override string ToString()
        {
            return allName;
        }
    }
}
