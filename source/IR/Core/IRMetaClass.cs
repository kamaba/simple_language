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
        public string irName => m_IRName;
        public int byteCount => m_ByteCount;
        public bool needCallInitMethod => m_NeedCallInitMethod;

        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<IRMetaVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;


        private Dictionary<int, int> m_MetaMemberVariableHashCodeDict = new Dictionary<int, int>();
        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_StaticIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMethod> m_IRNotStaticMethodList = new List<IRMethod>();
        private string m_IRName = "";
        private MetaClass m_MetaClass = null;

        private int allocSize = 0;
        private List<EType> m_MetaTypeList = new List<EType>();
        private int m_ByteCount = 0;
        private bool m_NeedCallInitMethod = false;

        static int s_TypeLength = 1000;
        public IRMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
            id = mc.GetHashCode();
            m_IRName = IRManager.GetIRNameByMetaClass(mc);
        }        
        public IRMethod GetIRNonStaticMethodByIndex( int index )
        {
            if( index >= m_IRNotStaticMethodList.Count || index < 0 )
            {
                Log.AddVM(EError.None, "GetIRMethodByIndex is null");
                return null;
            }
            return m_IRNotStaticMethodList[index];
        }
        public IRMethod GetIRNonStaticMethodIndexByMethod(string name, out int index )
        {
            index = -1;
            for (int i = 0; i < m_IRNotStaticMethodList.Count; i++)
            {
                if (m_IRNotStaticMethodList[i].virtualFunctionName == name)
                {
                    index = i;
                    return m_IRNotStaticMethodList[i];
                }
            }
            return null;
        }
        public int GetIRNonStaticMethodIndexByMethod( string name )
        {
            for ( int i = 0; i < m_IRNotStaticMethodList.Count; i++ )
            {
                if(m_IRNotStaticMethodList[i].virtualFunctionName == name)
                {
                    return i;
                }
            }
            return -1;
        }
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
        public void CreateMemberData()
        {
            m_MetaTypeList.Clear();

            if (m_MetaClass is MetaEnum me)
            {
            }
            else if (m_MetaClass is MetaData md)
            {
               var localMetaMemberDatas = md.GetMetaMemberDataList();
            }
            else
            {
                var localMetaMemberVariables = m_MetaClass.GetMetaMemberVariableListByFlag(false);
                for (int i = 0; i < localMetaMemberVariables.Count; i++)
                {
                    var v = localMetaMemberVariables[i];
                    IRMetaVariable irmv = new IRMetaVariable(this, v, i);
                    m_LocalIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, i);
                    if (v.isInnerDefine == false)
                    {
                        //if (v.metaDefineType.metaClass != null)
                        //    m_MetaTypeList.Add(v.metaDefineType.metaClass.eType);
                    }
                }
            }
            int count = 0;
            int ssize = 0;
            for (int i = 0; i < m_MetaTypeList.Count; i++)
            {
                ssize = IR.IRUtil.GetTypeSize(m_MetaTypeList[i]);
                count += ssize;
                m_ByteCount += ssize;
            }

           var staticMetaMemberVariables = m_MetaClass.GetMetaMemberVariableListByFlag(true);
            for (int i = 0; i < staticMetaMemberVariables.Count; i++)
            {
                var v = staticMetaMemberVariables[i];
                IRMetaVariable irmv = new IRMetaVariable(this, v, i);
                m_StaticIRMetaVariableList.Add(irmv);
                AddMetaMemberVariableIndexBindHashCode(v.GetHashCode(), i);
            }
        }
        public void CreateMemberMethod()
        {
            var smflist = m_MetaClass.staticMetaMemberFunctionList;
            //int index = 0;
            for (int i = 0; i < smflist.Count; i++)
            {
                var mf = smflist[i];
                mf.UpdateFunctionName();
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                
                IRManager.instance.AddIRMethod(gmf);
            }

            var nonsmflist = m_MetaClass.nonStaticVirtualMetaMemberFunctionList;
            //int index = 0;
            for (int i = 0; i < nonsmflist.Count; i++)
            {
                var mf = nonsmflist[i];
                mf.UpdateVritualFunctionName();
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                m_IRNotStaticMethodList.Add(gmf);
                IRManager.instance.AddIRMethod(gmf);
            }
        }
        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            List<IRData> list = new List<IRData>();

            return list;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.m_IRName);

            return sb.ToString();
        }
    }
}
