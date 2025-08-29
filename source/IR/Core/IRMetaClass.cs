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

namespace SimpleLanguage.IR
{
    public class IRMetaClass
    {
        public int id { get; set; } = 0;
        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<IRMetaVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;
        public Dictionary<string, IRMetaClass> genTemplateIRMetaClassDict => m_GenTemplateIRMetaClassDict;
        public string irName => m_IRName;
        public bool isTemplate { get; private set; } = false;
        public bool genClass { get; private set; } = false;



        public int allocSize = 0;
        public List<EType> m_MetaTypeList = new List<EType>();
        public int byteCount = 0;

        private Dictionary<int, int> m_MetaMemberVariableHashCodeDict = new Dictionary<int, int>();
        private List<MetaMemberVariable> m_LocalMetaMemberVariables = new List<MetaMemberVariable>();
        private List<MetaMemberData> m_LocalMetaMemberDatas = new List<MetaMemberData>();
        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_StaticIRMetaVariableList = new List<IRMetaVariable>();
        private Dictionary<string, IRMetaClass> m_GenTemplateIRMetaClassDict = new Dictionary<string, IRMetaClass>();
        private Dictionary<int, IRCallFunction> m_LocalIRInitDict = new Dictionary<int, IRCallFunction>();
        private List<IRMethod> m_IRNotStaticMethodList = new List<IRMethod>();
        private IRMetaClass m_TemplateIRMetaClass;
        private string m_IRName = "";
        private IRManager m_IRManager = null;

        static int s_TypeLength = 1000;
        public IRMetaClass(IRManager manager)
        {
            m_IRManager = manager;
            id = s_TypeLength++;
        }
        public IRMetaClass(IRManager manager, string templateName )
        {
            m_IRManager = manager;
            m_IRName = templateName;
            this.isTemplate = true;
            id = s_TypeLength++;
        }
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
        public void SetTemplateIRMetaClass( IRMetaClass IRMetaClass)
        {
            m_TemplateIRMetaClass = IRMetaClass;
        }
        public IRMethod GetIRNonStaticMethodByIndex( int index )
        {
            if(m_TemplateIRMetaClass != null )
            {
                return m_TemplateIRMetaClass.GetIRNonStaticMethodByIndex(index);
            }
            if( index >= m_IRNotStaticMethodList.Count || index < 0 )
            {
                Log.AddVM(EError.None, "GetIRMethodByIndex is null");
                return null;
            }
            return m_IRNotStaticMethodList[index];
        }
        public int GetIRNonStaticMethodIndexByMethod( string name )
        {
            if (m_TemplateIRMetaClass != null)
            {
                return m_TemplateIRMetaClass.GetIRNonStaticMethodIndexByMethod(name);
            }
            for ( int i = 0; i < m_IRNotStaticMethodList.Count; i++ )
            {
                if(m_IRNotStaticMethodList[i].virtualFunctionName == name)
                {
                    return i;
                }
            }
            return -1;
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
            m_IRName = IRManager.GetIRNameByMetaClass(mc);
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
                    IRMetaVariable irmv = new IRMetaVariable(this, v, i);
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
                IRMetaVariable irmv = new IRMetaVariable(this, v, i);
                m_StaticIRMetaVariableList.Add(irmv);
                AddMetaMemberVariableIndexBindHashCode(v.GetHashCode(), i);
                if( v.sourceMetaMemberVariable != null )
                {
                    AddMetaMemberVariableIndexBindHashCode(v.sourceMetaMemberVariable.GetHashCode(), i);
                }
            }

            if( mc is MetaGenTemplateClass mgtc )
            {
                genClass = true;
                foreach ( var v in mgtc.metaGenTemplateList )
                {
                    var irmc = IRManager.instance.GetIRMetaClassByName( IRManager.GetIRNameByMetaType(v.metaType) );
                    m_GenTemplateIRMetaClassDict.Add( v.name, irmc );
                }
            }
            else
            {
                genClass = false;
            }
            CalcAllocSize();

            HandleMemberFunction(mc);
        }
        public void HandleMemberFunction( MetaClass mc )
        {
            if( mc is MetaGenTemplateClass mgtc )
            {
                return;
            }

            var smflist = mc.staticMetaMemberFunctionList;
            //int index = 0;
            for (int i = 0; i < smflist.Count; i++)
            {
                var mf = smflist[i];
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                IRManager.instance.AddIRMethod(gmf);
                //m_IRNotStaticMethodList.Add(gmf);
            }

            var nonsmflist = mc.nonStaticVirtualMetaMemberFunctionList;
            //int index = 0;
            for (int i = 0; i < nonsmflist.Count; i++)
            {
                var mf = nonsmflist[i];
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                IRManager.instance.AddIRMethod(gmf);
                m_IRNotStaticMethodList.Add(gmf);
            }
        }
        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            List<IRData> list = new List<IRData>();

            return list;
        }
        public bool IsCoreMetaClass()
        {
            if (this.m_IRName == "Int32"
                || this.m_IRName == "String"
                || this.m_IRName == "Float32"
                || this.m_IRName == "Float64")
            {
                return true;
            }
            return false;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.m_IRName);

            return sb.ToString();
        }
    }
}
