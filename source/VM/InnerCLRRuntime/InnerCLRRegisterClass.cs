using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM.InnerCLRRuntime
{
    public class RuntimeClass
    {
        public int id { get; set; } = 0;
        public IRMetaClass irMetaClass { get; set; } = null;
        public ClassObject[] m_StaticMetaMemberVariableArray = null;

        public RuntimeClass( IRMetaClass irmc )
        {
            //irmc.CreateStaticMetaMetaVariableIRList
        }

        public ClassObject GetStaticMetaMemberVaraible( int index )
        {
            if( index < 0 || index >= m_StaticMetaMemberVariableArray.Length )
            {
                return null;
            }
            return m_StaticMetaMemberVariableArray[index];
        }
    }
    public class InnerCLRRegisterClass
    {
        public static InnerCLRRegisterClass s_Instance = null;
        public static InnerCLRRegisterClass instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new InnerCLRRegisterClass();
                }
                return s_Instance;
            }
        }
        Dictionary<int, RuntimeClass> m_RuntimeClassDict = new Dictionary<int, RuntimeClass>();

        public List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();


        public void RegisterDymnicClass()
        {

        }

        //public IRMetaType GetIRMetaClass( IRMetaClass metaclass, List<IRMetaClass> templateList, bool isNonIncludeAndRegister = false )
        //{
        //    IRMetaClass irmc = null;

        //    foreach( var v in m_IRMetaClassList )
        //    {
        //        if( v.irmeta)
        //    }
        //}

        public ClassObject GetStaticMetaMemberVariable( int classid, int index )
        {
            if(m_RuntimeClassDict.ContainsKey(classid) )
            {
                m_RuntimeClassDict[classid].GetStaticMetaMemberVaraible(index);
            }

            return null;
        }
    }
}
