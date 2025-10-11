//****************************************************************************
//  File:      MetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EMetaTypeType
    {
        None,
        MetaClass,
        MetaGenClass,
        Template,
        TemplateClassWithTemplate
    }
    public sealed class MetaType : MetaBase
    {
        public override string name
        {
            get
            {
                return m_MetaClass?.allClassName;
            }
        }
        public EMetaTypeType eType => m_EType;
        public MetaClass metaClass => m_MetaClass;
        public MetaClass typeInferenceClass => m_TypeInferenceClass;
        //public MetaClass templateMetaClass => m_TemplateMetaClass;
        public bool isEnum => m_MetaClass is MetaEnum;
        public bool isData => m_MetaClass is MetaData;
        public bool isTemplate => m_EType == EMetaTypeType.Template;
        public MetaMemberEnum enumValue => m_EnumValue;
        public List<MetaType> templateMetaTypeList => m_TemplateMetaTypeList;
        public bool isArray => m_MetaClass?.eType == EType.Array;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData;
        public bool isDefineMetaClass => m_IsDefineMetaClass;
        public MetaTemplate metaTemplate => m_MetaTemplate;

        //private MetaInputTemplateCollection m_InputTemplateCollection = null;
        private EMetaTypeType m_EType = EMetaTypeType.None;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaClass m_TypeInferenceClass = null;                  //推理类
        private MetaType m_ParentMetaType = null;
        private MetaTemplate m_MetaTemplate = null;
        private MetaExpressNode m_DefaultExpressNode = null;        // int a => a = 0;
        private MetaMemberEnum m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private bool m_IsDefineMetaClass = false;
        private List<MetaType> m_TemplateMetaTypeList = new List<MetaType>();     //  Map<T1,T2> 一般用在返回值类型定义中

        public MetaType()
        {
        }
        public MetaType(MetaTemplate mt)
        {
            m_EType = EMetaTypeType.Template;
            m_MetaTemplate = mt;
            m_MetaClass = mt.extendsMetaClass;
        }
        public MetaType( MetaGenTemplateClass mgtc, List<MetaType> mtList )
        {
            m_EType = EMetaTypeType.MetaGenClass;
            m_MetaClass = mgtc;
            m_TemplateMetaTypeList = mtList;
        }
        public MetaType( MetaClass mc )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_IsDefineMetaClass = false;
            m_MetaClass = mc;
            m_EType = EMetaTypeType.MetaClass;
        }
        public MetaType( MetaClass mc, List<MetaType> mtList )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_IsDefineMetaClass = false;
            //m_TemplateMetaClass = templatemc;
            m_MetaClass = mc;
            m_TemplateMetaTypeList = mtList;
            m_EType = EMetaTypeType.TemplateClassWithTemplate;
        }
        public MetaType( MetaClass mc, MetaClass templatemc, MetaInputTemplateCollection mitc )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_IsDefineMetaClass = false;
            if ( mitc == null)
            {
                //m_TemplateMetaClass = templatemc;
                m_MetaClass = mc;
            }
            else
            {
                m_MetaClass = templatemc;
                //m_TemplateMetaClass = mc;
                //m_InputTemplateCollection = mitc;
                //m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
                m_TemplateMetaTypeList = mitc.metaTemplateParamsList;
            }
        }
        public MetaType(MetaType mt) : base(mt)
        {
            this.m_MetaClass = mt.m_MetaClass;
            //this.m_TemplateMetaClass = mt.m_TemplateMetaClass;
            this.m_ParentMetaType = mt.m_ParentMetaType;
            this.m_MetaTemplate = mt.m_MetaTemplate;
            this.m_DefaultExpressNode = mt.m_DefaultExpressNode;
            this.m_EnumValue = mt.m_EnumValue;
            this.m_IsDefineMetaClass = mt.m_IsDefineMetaClass;
            this.m_EType = mt.m_EType;
            for (int i = 0; i < mt.m_TemplateMetaTypeList.Count; i++)
            {
                MetaType mtc = new MetaType(mt.m_TemplateMetaTypeList[i]);
                m_TemplateMetaTypeList.Add(mtc);
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
        public MetaClass GetTemplateMetaClass(out bool isGTC)
        {
            isGTC = false;
            if (m_MetaClass is MetaGenTemplateClass mgtc)
            {
                isGTC = true;
                return mgtc.metaTemplateClass;
            }
            return m_MetaClass;
        }
        public MetaClass GetTemplateMetaClass()
        {
            if (m_MetaClass is MetaGenTemplateClass mgtc)
            {
                return mgtc.metaTemplateClass;
            }
            return m_MetaClass;
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
                if( tmt.IsIncludeTemplate()  )
                {
                    return true;
                }
            }
            return m_MetaTemplate != null;
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
                    return true;
                }
            }
            return false;
        }
        public bool IsIncludeFunctionTemplate( MetaMemberFunction mmf )
        {
            if( eType == EMetaTypeType.Template )
            {
                if (m_MetaTemplate != null && mmf.isTemplateFunction)
                {
                    return mmf.metaMemberTemplateCollection.metaTemplateList.IndexOf(m_MetaTemplate) != -1;
                }
            }
            else if( eType == EMetaTypeType .TemplateClassWithTemplate )
            {
                for (int i = 0; i < m_TemplateMetaTypeList.Count; i++)
                {
                    var tmt = m_TemplateMetaTypeList[i];
                    if (tmt.IsIncludeFunctionTemplate(mmf))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
        //是否包含 模板函数模板  意思就是是否在 templateMetaTypeList 中，有模板函数定义的T
        public MetaMemberFunction FindTemplateFunctionTemplate( MetaMemberFunction mmf )
        {
            if( eType == EMetaTypeType.TemplateClassWithTemplate )
            {
                if (m_TemplateMetaTypeList.Count == 0) return null;
                for (int i = 0; i < m_TemplateMetaTypeList.Count; i++)
                {
                    var tmt = m_TemplateMetaTypeList[i];
                    if ( tmt.IsIncludeFunctionTemplate(mmf))
                    {
                        return mmf;
                    }
                }
                return null;
            }
            return null;
        }
        public void AddTemplateMetaType( MetaType mt )
        {
            mt.m_ParentMetaType = this;
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

            if( mdtL.eType != mdtR.eType )
            {
                return false;
            }

            if( mdtL.eType == EMetaTypeType.Template )
            {
                if( mdtL.metaTemplate ==  mdtR.metaTemplate )
                {
                    return true;
                }
            }
            else if( mdtL.eType == EMetaTypeType.MetaClass )
            {
                if( mdtL.metaClass == mdtR.metaClass )
                {
                    return true;
                }
            }
            else
            {
                //if( mdtL.templateMetaClass != mdtR.templateMetaClass )
                if (mdtL.m_MetaClass != mdtR.m_MetaClass)
                {
                    return false;
                }
                if( mdtL.templateMetaTypeList.Count != mdtR.templateMetaTypeList.Count )
                {
                    return false;
                }
                for( int i = 0; i <  mdtL.templateMetaTypeList.Count; i++ )
                {
                    var lv = mdtL.templateMetaTypeList[i];
                    var rv = mdtR.templateMetaTypeList[i];
                    if(EqualMetaDefineType(lv, rv ) == false )
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
        public void SetMetaClass(MetaClass mc)
        {
            m_MetaClass = mc;
            m_IsDefineMetaClass = true;
            m_EType = EMetaTypeType.MetaClass;
        }
        public void SetMetaTemplate(MetaTemplate mt)
        {
            m_MetaTemplate = mt;
            if (mt != null)
            {
                m_EType = EMetaTypeType.Template;
            }
        }
        public void SetTypeInferenceClass(MetaClass mc )
        {
            this.m_TypeInferenceClass = mc;
        }
        public void SetTemplateMetaClass( MetaClass mc )
        {
            //m_TemplateMetaClass = mc;
            m_MetaClass = mc;
            m_EType = EMetaTypeType.TemplateClassWithTemplate;
        }
        public void UpdateMetaClassByRawMetaClassAndInputTemplateCollection()
        {
            //m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
        }
        //生成注册后的 模板类的实例类
        public MetaClass UpdateMetaGenTemplate( List<MetaGenTemplate> metaGenTemplateList)
        {
            if( eType == EMetaTypeType.Template )
            {
                if (m_MetaTemplate != null)
                {
                    for (int i = 0; i < metaGenTemplateList.Count; i++)
                    {
                        var cmgt = metaGenTemplateList[i];
                        if (cmgt.metaTemplate == m_MetaTemplate)
                        {
                            return cmgt.metaType.metaClass;
                        }
                    }
                }
            }
            else if( eType == EMetaTypeType.TemplateClassWithTemplate )
            {
                List<MetaClass> mcList = new List<MetaClass>();
                for (int i = 0; i < templateMetaTypeList.Count; i++)
                {
                    var mgt = templateMetaTypeList[i];
                    if (mgt.eType == EMetaTypeType.MetaClass)
                    {
                        mcList.Add(mgt.metaClass);
                    }
                    else
                    {
                        var mc = mgt.UpdateMetaGenTemplate(metaGenTemplateList);
                        if( mc == null )
                        {
                            Log.AddInStructMeta(EError.None, "注册生成类是空!");
                            return null;
                        }
                        mcList.Add(mc);
                    }
                }
                return this.m_MetaClass.AddInstanceMetaClass(mcList);
            }
            return null;
        }
        public MetaType GetMetaInputTemplateByIndex( int index = 0 )
        {
            if (index < 0 || index >= m_TemplateMetaTypeList.Count) return null;

            return m_TemplateMetaTypeList[index];
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

            //if (m_TemplateMetaClass != null)
            //{
            //    sb.Append(m_TemplateMetaClass.allClassName);
            //}
            //else 
            if (m_MetaClass != null)
            {
                sb.Append(m_MetaClass.allClassName);
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_MetaTemplate != null )
            {
                sb.Append(m_MetaTemplate.name);
            }
            else
            {
                if( m_MetaClass is MetaGenTemplateClass mgtc )
                {
                    sb.Append(mgtc.allClassName);
                }
                else
                {
                    //if (m_TemplateMetaClass != null)
                    //{
                    //    sb.Append(m_TemplateMetaClass.metaNode.allName);                        
                    //}
                    //else 
                    if (m_MetaClass != null)
                    {
                        sb.Append(m_MetaClass.allClassName);
                    }

                    if( m_TemplateMetaTypeList.Count > 0 )
                    {
                        sb.Append("<");

                        for (int i = 0; i < m_TemplateMetaTypeList.Count; i++)
                        {
                            sb.Append(m_TemplateMetaTypeList[i].ToString());
                            if (i < m_TemplateMetaTypeList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append(">");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
