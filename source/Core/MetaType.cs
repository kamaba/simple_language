//****************************************************************************
//  File:      MetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;

namespace SimpleLanguage.Core
{
    public sealed class MetaType : MetaBase
    {
        public override string name
        {
            get
            {
                return m_MetaClass?.allClassName;
            }
        }
        public MetaClass metaClass => m_MetaClass;
        public bool isEnum => m_MetaClass is MetaEnum;
        public bool isData => m_MetaClass is MetaData;
        public bool isTemplate => m_MetaTemplate != null;
        public MetaMemberEnum enumValue => m_EnumValue;
        public List<MetaType> templateMetaTypeList => m_TemplateMetaTypeList;
        public bool isGenTemplateClass => m_MetaClass is MetaGenTemplateClass;
        public bool isArray => m_MetaClass?.eType == EType.Array;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData;
        public bool isDefineMetaClass => m_IsDefineMetaClass;
        public MetaTemplate metaTemplate => m_MetaTemplate;


        //private MetaInputTemplateCollection m_InputTemplateCollection = null;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaClass m_RawMetaClass = null;                    // List<int> => list
        private MetaTemplate m_MetaTemplate = null;
        private MetaExpressNode m_DefaultExpressNode = null;        // int a => a = 0;
        private MetaMemberEnum m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private bool m_IsDefineMetaClass = false;

        private List<MetaType> m_TemplateMetaTypeList = new List<MetaType>();     //  Array<T1,T2> 一般用在返回值类型定义中

        public MetaType(MetaTemplate mt)
        {
            m_MetaTemplate = mt;
        }
        public MetaType( MetaType mt )
        {
            this.m_MetaClass = mt.m_MetaClass;
            this.m_RawMetaClass = mt.m_RawMetaClass;
            //m_MetaTemplate = mt.m_MetaTemplate;
            //m_InputTemplateCollection = mt.m_InputTemplateCollection;
        }
        public MetaType( MetaClass mc )
        {
            if (mc == null)
            {
                Debug.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_IsDefineMetaClass = false;
            m_RawMetaClass = mc;
            m_MetaClass = mc;
        }
        public MetaType( MetaClass mc, MetaInputTemplateCollection mitc )
        {
            if (mc == null)
            {
                Debug.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_IsDefineMetaClass = false;
            if ( mitc == null)
            {
                m_RawMetaClass = mc;
                m_MetaClass = mc;
            }
            else
            {
                m_RawMetaClass = mc;
                //m_InputTemplateCollection = mitc;

                //m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
            }

        }      
        public bool IsCanForIn()
        {
            if(m_MetaClass is MetaEnum )//m_MetaClass is MetaData ||  )
            { return true; }
            if( m_MetaClass.eType == EType.Array
                || m_MetaClass.eType == EType.Range )
            { return true; }

            return false;
        }
        //public void SetEnumValue( MetaMemberVariable mmv )
        //{
        //    m_EnumValue = mmv;
        //    m_MetaClass = mmv.ownerMetaClass;
        //}
        public bool IsIncludeTemplate()
        {
            for( int i = 0; i < m_TemplateMetaTypeList.Count; i++ )
            {
                var tmt = m_TemplateMetaTypeList[i];
                if( tmt.IsIncludeTemplate() == false )
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsIncludeClassTemplate(MetaClass ownerClass)
        {
            if (m_MetaTemplate != null && ownerClass.isTemplateClass)
            {
                return ownerClass.metaTemplateList.IndexOf(m_MetaTemplate) != -1;
            }
            for (int i = 0; i < m_TemplateMetaTypeList.Count; i++)
            {
                var tmt = m_TemplateMetaTypeList[i];
                if (tmt.IsIncludeClassTemplate(ownerClass))
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsIncludeFunctionTemplate( MetaMemberFunction mmf )
        {
            if (m_MetaTemplate != null && mmf.isTemplateFunction )
            {
                return mmf.metaMemberTemplateCollection.metaTemplateList.IndexOf(m_MetaTemplate) != -1;
            }
            for (int i = 0; i < m_TemplateMetaTypeList.Count; i++)
            {
                var tmt = m_TemplateMetaTypeList[i];
                if (tmt.IsIncludeFunctionTemplate(mmf))
                {
                    return false;
                }
            }
            return true;
        }
        public void AddTemplateMetaType( MetaType mt )
        {
            m_TemplateMetaTypeList.Add(mt);
        }
        public MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection input = null)
        {
            return m_MetaClass?.GetMetaMemberConstructFunction(input);
        }
        public static bool EqualMetaDefineType(MetaType mdtL, MetaType mdtR)
        {
            if (mdtL == null || mdtR == null)
                return false;

            //if( mdtL.isTemplate )
            //{
            //    if (mdtL.metaTemplate == mdtR.metaTemplate && mdtL.metaTemplate != null)
            //    {
            //        return true;
            //    }
            //}
            if (mdtL.metaClass == mdtR.metaClass && mdtL.metaClass != null )
            {
                //if( mdtL.m_InputTemplateCollection != null )
                //{
                //    if(mdtR.m_InputTemplateCollection != null )
                //    {
                //        if (mdtL.m_InputTemplateCollection.metaTemplateParamsList.Count
                //            == mdtR.m_InputTemplateCollection?.metaTemplateParamsList.Count)
                //        {
                //            for (int i = 0; i < mdtL.m_InputTemplateCollection.metaTemplateParamsList.Count; i++)
                //            {
                //                var mtpl = mdtL.m_InputTemplateCollection.metaTemplateParamsList[i];
                //                var mtpr = mdtR.m_InputTemplateCollection.metaTemplateParamsList[i];
                //                if (EqualMetaDefineType(mtpl, mtpr))
                //                {
                //                    return true;
                //                }
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    return true;
                //}
            }

            return false;
        }
        public void SetRawMetaClass( MetaClass mc )
        {
            m_RawMetaClass = mc;
        }
        public void UpdateMetaClassByRawMetaClassAndInputTemplateCollection()
        {
            //m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
        }
        //public void UpdateMetaTypeTemplateByGenMetaClass()
        //{
        //    if ( m_MetaTemplate != null )
        //    {
        //        if( m_RawMetaClass is MetaGenTemplateClass mgtc )
        //        {
        //            var gmgt = mgtc.GetMetaGenTemplate(m_MetaTemplate.name);
        //            if( gmgt != null )
        //            {
        //                m_MetaClass = gmgt.metaType.metaClass;
        //            }
        //        }
        //    }
        //}
        public void SetMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
            m_IsDefineMetaClass = true;
        }
        public void SetMetaTemplate( MetaTemplate mt )
        {
            m_MetaTemplate = mt;
        }
        public void SetMetaInputTemplateCollection( MetaInputTemplateCollection mitc )
        {
            //m_InputTemplateCollection = mitc;
        }
        public MetaType GetMetaInputTemplateByIndex( int index = 0 )
        {
            MetaGenTemplateClass mtc = m_MetaClass as MetaGenTemplateClass;
            if (mtc != null )
            {
                return mtc.GetGenTemplateByIndex(index);
            }
            return null;
        }
        public MetaExpressNode GetDefaultExpressNode()
        {
            if (m_DefaultExpressNode != null)
            {
                return m_DefaultExpressNode;
            }
            else
            {
                return m_MetaClass.defaultExpressNode;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_MetaClass is MetaGenTemplateClass)
            {
                sb.Append((m_MetaClass as MetaGenTemplateClass).ToDefineTypeString());
            }
            else
            {
                sb.Append(m_MetaClass?.allClassName);
            }
            return sb.ToString();
        }
    }
}
