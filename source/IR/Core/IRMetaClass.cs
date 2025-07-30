//****************************************************************************
//  File:      IRMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************


using System.Collections.Generic;
using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public class IRMetaClass
    {
        static short s_TypeLength = 1000;
        public short id { get; set; } = 0;

        public bool IsCoreMetaClass()
        {
            if( this.allName == "Int32"
                || this.allName == "String"
                || this.allName == "Float")
            {
                return true;
            }
            return false;
        }
        public IRMetaClass( IRManager manager )
        {
            irManager = manager;
            id = s_TypeLength++;
        }
        public IRMetaClass( IRManager manager, MetaTemplate mt )
        {
            irManager = manager;
            id = s_TypeLength++;
            allName = mt.name;
            isTemplate = true;
        }
        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<IRMetaVariable> staticIRMetaVariableList => staticIRMetaVariableList;
        public Dictionary<string, IRMetaClass> genTemplateIRMetaClassDict => m_GenTemplateIRMetaClassDict;

        public int allocSize = 0;
        public List<EType> m_MetaTypeList = new List<EType>();
        public int byteCount = 0;
        public string allName { get; set; } = null;
        public bool isTemplate { get; set; } = false;
        public bool genClass { get; set; } = false;

        private Dictionary<string, IRMetaClass> m_GenTemplateIRMetaClassDict = new Dictionary<string, IRMetaClass>();

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
                    if(v.metaDefineType.metaClass != null )
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
            allName = mc.allClassName;
            if (mc is MetaEnum me)
            {
            }
            else if (mc is MetaData md)
            {
                m_LocalMetaMemberDatas = md.GetMetaMemberDataList();
            }
            else
            {
                m_LocalMetaMemberVariables = mc.GetMetaMemberVariableListByFlag(false, false);
            }
            if( mc is MetaGenTemplateClass mgtc )
            {
                genClass = true;
                foreach( var v in mgtc.metaGenTemplateList )
                {
                    var irmc = IRManager.instance.GetIRMetaClassByName(v.metaType.metaClass.allClassName);
                    m_GenTemplateIRMetaClassDict.Add( v.name, irmc );
                }
            }
            else
            {
                genClass = false;
            }
            CalcAllocSize();
        }
        public void CreateIRMetaMemberVariable()
        {
            foreach (var v in m_LocalMetaMemberVariables)
            {
                IRMetaVariable irmv = new IRMetaVariable( this, v);
                m_LocalIRMetaVariableList.Add(irmv);
            }
        }
        public override string ToString()
        {
            return allName;
        }
    }
}
