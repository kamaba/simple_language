//****************************************************************************
//  File:      IRMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************


using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using SimpleLanguage.VM;

namespace SimpleLanguage.IR
{
    public class IRMetaClass
    {
        static int s_TypeLength = 1000;
        public int id { get; set; } = 0;

        public IRMetaClass(IRManager manager)
        {
            m_IRManager = manager;
            id = s_TypeLength++;
        }
        public IRMetaClass(IRManager manager, string templateName )
        {
            m_IRManager = manager;
            allName = templateName;
            this.isTemplate = true;
            id = s_TypeLength++;
        }
        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<IRMetaVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;
        public Dictionary<string, IRMetaClass> genTemplateIRMetaClassDict => m_GenTemplateIRMetaClassDict;
        private Dictionary<int, int> m_MetaMemberVariableHashCodeDict = new Dictionary<int, int>();

        public int allocSize = 0;
        public List<EType> m_MetaTypeList = new List<EType>();
        public int byteCount = 0;
        public string allName { get; private set; } = "";
        public bool isTemplate { get; private set; } = false;
        public bool genClass { get; private set; } = false;


        List<MetaMemberVariable> m_LocalMetaMemberVariables = new List<MetaMemberVariable>();
        List<MetaMemberData> m_LocalMetaMemberDatas = new List<MetaMemberData>();

        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_StaticIRMetaVariableList = new List<IRMetaVariable>();
        private Dictionary<string, IRMetaClass> m_GenTemplateIRMetaClassDict = new Dictionary<string, IRMetaClass>();
        private Dictionary<int, IRCallFunction> m_LocalIRInitDict = new Dictionary<int, IRCallFunction>();
        private List<IRMethod> m_IRMethodList = new List<IRMethod>();
        private IRManager m_IRManager = null;
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
        public IRMethod GetIRMethodByIndex( int index )
        {
            if( index >= m_IRMethodList.Count || index < 0 )
            {
                Log.AddVM(EError.None, "GetIRMethodByIndex is null");
                return null;
            }
            return m_IRMethodList[index];
        }
        public IRMetaVariable GetIRMetaVariable( int id )
        {
            return m_LocalIRMetaVariableList.Find( a=> a.id == id );
        }
        //public class Level<T>
        //{
        //    public static T static_t1 = default(T);

        //    public static T create( T t )
        //    {
        //        Level<int>.static_t1 = 20;
        //        Level<T>.static_t1 = t;
        //        return static_t1;
        //    }
        //}
        public int GetMetaMemberVariableIndexByHashCode( int id )
        {
            if(m_MetaMemberVariableHashCodeDict.ContainsKey(id ) )
            {
                return m_MetaMemberVariableHashCodeDict[id];
            }
            return -1;
        }
        public void AddMetaMemberVariableIndexBindHashCode( int id, int newid)
        {
            if( !m_MetaMemberVariableHashCodeDict.ContainsKey( id ) )
            {
                m_MetaMemberVariableHashCodeDict.Add(id, newid);
            }
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
                m_LocalMetaMemberVariables = mc.GetMetaMemberVariableListByFlag(false);
                for (int i = 0; i < m_LocalMetaMemberVariables.Count; i++)
                {
                    var v = m_LocalMetaMemberVariables[i];
                    IRMetaVariable irmv = new IRMetaVariable(this, v);
                    irmv.index = i;
                    m_LocalIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, i);
                    if (v.sourceMetaMemberVariable != null)
                    {
                        AddMetaMemberVariableIndexBindHashCode(v.sourceMetaMemberVariable.GetHashCode(), i);
                    }
                }
            }
            var staticMMVList = mc.GetMetaMemberVariableListByFlag(true);            
            for ( int i = 0; i < staticMMVList.Count; i++ )
            {
                var v = staticMMVList[i];
                IRMetaVariable irmv = new IRMetaVariable(this, v);
                irmv.index = i;
                m_StaticIRMetaVariableList.Add(irmv);
                AddMetaMemberVariableIndexBindHashCode(v.GetHashCode(), i);
                if( v.sourceMetaMemberVariable != null )
                {
                    AddMetaMemberVariableIndexBindHashCode(v.sourceMetaMemberVariable.GetHashCode(), i);
                }
            }

            var mflist = mc.GetVirtualMemberFunctionList();
            //int index = 0;
            for( int i = 0; i < mflist.Count; i++ )
            {
                var mf = mflist[i];
                var gmf = IRManager.instance.GetIRMethod(mf.functionAllName);
                m_IRMethodList.Add(gmf);
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
        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            List<IRData> list = new List<IRData>();

            return list;
        }
        public bool IsCoreMetaClass()
        {
            if (this.allName == "Int32"
                || this.allName == "String"
                || this.allName == "Float")
            {
                return true;
            }
            return false;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.allName);

            return sb.ToString();
        }
    }
}
