//****************************************************************************
//  File:      MetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


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
        public bool isEnum => m_MetaClass is MetaEnum;
        public bool isData => m_MetaClass is MetaData;
        public bool isNull => m_MetaClass == CoreMetaClassManager.nullMetaClass;
        public bool isMap => m_MetaClass == CoreMetaClassManager.mapMetaClass;
        public bool isTemplate => m_EType == EMetaTypeType.Template;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData;
        public int arrayLength => m_ArrayLength;
        public EMetaTypeType eType => m_EType;
        public MetaClass metaClass => m_MetaClass;
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaMemberEnum enumValue => m_EnumValue;
        public List<MetaType> defineTemplateMetaTypeList => m_DefineTemplateMetaTypeList;
        public List<MetaType> genTemplateMetaTypeList => m_GenTemplateMetaTypeList;

        private EMetaTypeType m_EType = EMetaTypeType.None;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaType m_ParentMetaType = null;
        private MetaTemplate m_MetaTemplate = null;
        //private MetaType m_SourceMetaType = null;                         //生成类的 对应来源类
        //private MetaGenTemplate m_MetaGenTemplate = null;
        private MetaMemberEnum m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private List<MetaType> m_DefineTemplateMetaTypeList = new List<MetaType>();     //  Map<T1,T2> 一般用在返回值类型定义中
        private List<MetaType> m_GenTemplateMetaTypeList = new List<MetaType>();     //  Map<T1,T2> 一般用在返回值类型定义中
        private int m_ArrayLength = -1;
        public MetaType()
        {
        }
        public MetaType( EType etype )
        {
            m_EType = EMetaTypeType.MetaClass;
            m_MetaClass = CoreMetaClassManager.GetMetaClassByEType(etype);
        }
        public MetaType(MetaTemplate mt, string fromName = "" )
        {
            m_EType = EMetaTypeType.Template;
            m_MetaTemplate = mt;
            m_MetaClass = mt.extendsMetaClass;
        }
        public MetaType( MetaGenTemplateClass mgtc, List<MetaType> defineMTList, List<MetaType> genMTList )
        {
            m_EType = EMetaTypeType.MetaGenClass;
            m_MetaClass = mgtc;
            m_DefineTemplateMetaTypeList = defineMTList;
            m_GenTemplateMetaTypeList = genMTList;
        }
        public MetaType( MetaClass mc )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_MetaClass = mc;
            m_EType = EMetaTypeType.MetaClass;
        }
        public MetaType( MetaClass mc, List<MetaType> mtList )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            //m_TemplateMetaClass = templatemc;
            m_MetaClass = mc;
            m_DefineTemplateMetaTypeList = mtList;
            m_GenTemplateMetaTypeList = mtList;
            m_EType = EMetaTypeType.TemplateClassWithTemplate;
        }
        public MetaType( MetaClass mc, MetaClass templatemc, MetaInputTemplateCollection mitc )
        {
            if (mc == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
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
                m_DefineTemplateMetaTypeList = mitc.metaTemplateParamsList;
            }
        }
        public MetaType(MetaType mt) : base(mt)
        {
            this.m_MetaClass = mt.m_MetaClass;
            //this.m_TemplateMetaClass = mt.m_TemplateMetaClass;
            this.m_ParentMetaType = mt.m_ParentMetaType;
            this.m_MetaTemplate = mt.m_MetaTemplate;
            this.m_EnumValue = mt.m_EnumValue;
            //this.m_FromName = mt.m_FromName;
            this.m_EType = mt.m_EType;
            this.m_ArrayLength = mt.m_ArrayLength;
            for (int i = 0; i < mt.m_DefineTemplateMetaTypeList.Count; i++)
            {
                MetaType mtc = new MetaType(mt.m_DefineTemplateMetaTypeList[i]);
                m_DefineTemplateMetaTypeList.Add(mtc);
            }
            for (int i = 0; i < mt.m_GenTemplateMetaTypeList.Count; i++)
            {
                MetaType mtc = new MetaType(mt.m_GenTemplateMetaTypeList[i]);
                m_GenTemplateMetaTypeList.Add(mtc);
            }
        }
        public bool IsArray()
        {
            if( m_EType == EMetaTypeType.MetaGenClass )
            {
                if( m_MetaClass is MetaGenTemplateClass mgtc )
                {
                    if( mgtc.metaTemplateClass == CoreMetaClassManager.arrayMetaClass )
                    {
                        return true;
                    }
                }
            }
            else if( m_EType == EMetaTypeType.MetaClass )
            {
                if (m_MetaClass == CoreMetaClassManager.arrayMetaClass)
                {
                    return true;
                }
            }
            else if( m_EType == EMetaTypeType.TemplateClassWithTemplate )
            {
                if (m_MetaClass == CoreMetaClassManager.arrayMetaClass)
                {
                    return true;
                }
            }
                return false;
        }
        public int ArrayDimension()
        {
            MetaType curmt = this;
            int dismesion = 0;
            while(true)
            {
                if(curmt.IsArray())
                {
                    dismesion++;
                    if(curmt.m_GenTemplateMetaTypeList.Count == 1 )
                    {
                        curmt = curmt.m_GenTemplateMetaTypeList[0];
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            return dismesion;
        }
        public List<int> ArrayDimensionLengthList()
        {
            List<int> list = new List<int>();

            MetaType curmt = this;
            while (true)
            {
                if (curmt.IsArray())
                {
                    list.Add(curmt.arrayLength);
                    if (curmt.m_GenTemplateMetaTypeList.Count == 1)
                    {
                        curmt = curmt.m_GenTemplateMetaTypeList[0];
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return list;
        }
        public void SetArrayLength( int len )
        {
            this.m_ArrayLength = len;
        }
        public void SetMetaType( MetaType mt )
        {
            this.m_MetaClass = mt.m_MetaClass;
            //this.m_TemplateMetaClass = mt.m_TemplateMetaClass;
            this.m_ParentMetaType = mt.m_ParentMetaType;
            this.m_MetaTemplate = mt.m_MetaTemplate;
            this.m_EnumValue = mt.m_EnumValue;
            //this.m_FromName = mt.m_FromName;
            this.m_EType = mt.m_EType;
            this.m_DefineTemplateMetaTypeList = mt.m_DefineTemplateMetaTypeList;
            this.m_GenTemplateMetaTypeList = mt.m_GenTemplateMetaTypeList;
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
        public bool DefineTemplateIsIncludeTemplate()
        {
            for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
            {
                var tmt = m_DefineTemplateMetaTypeList[i];
                if (tmt.DefineTemplateIsIncludeTemplate())
                {
                    return true;
                }
            }
            return m_MetaTemplate != null;
        }
        public bool GenTemplateIsIncludeTemplate()
        {
            for (int i = 0; i < m_GenTemplateMetaTypeList.Count; i++)
            {
                var tmt = m_GenTemplateMetaTypeList[i];
                if (tmt.GenTemplateIsIncludeTemplate())
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
            for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
            {
                var tmt = m_DefineTemplateMetaTypeList[i];
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
                for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
                {
                    var tmt = m_DefineTemplateMetaTypeList[i];
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
                if (m_DefineTemplateMetaTypeList.Count == 0) return null;
                for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
                {
                    var tmt = m_DefineTemplateMetaTypeList[i];
                    if ( tmt.IsIncludeFunctionTemplate(mmf))
                    {
                        return mmf;
                    }
                }
                return null;
            }
            return null;
        }
        public void AddDefineTemplateMetaType(MetaType mt)
        {
            mt.m_ParentMetaType = this;
            m_DefineTemplateMetaTypeList.Add(mt);
        }
        public void AddGenTemplateMetaType(MetaType mt)
        {
            m_GenTemplateMetaTypeList.Add(mt);
        }
        //public void AddArrayMetaType( MetaType mt )
        //{
        //    m_ArrayMetaTypeList.Add(mt);
        //}
        //public void SetArrayMetaType( List<MetaType> list )
        //{
        //    m_ArrayMetaTypeList = list;
        //    m_ArrayDimensionLengthList.Clear();
        //    m_ArrayDimensionLengthList.Add(list.Count);
        //    m_EType = EMetaTypeType.Array;
        //}
        //public void SetSourceMetaType( MetaType sourceMt )
        //{
        //    this.m_SourceMetaType = sourceMt;
        //}
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
                if( mdtL.m_DefineTemplateMetaTypeList.Count != mdtR.m_DefineTemplateMetaTypeList.Count )
                {
                    return false;
                }
                for( int i = 0; i <  mdtL.m_DefineTemplateMetaTypeList.Count; i++ )
                {
                    var lv = mdtL.m_DefineTemplateMetaTypeList[i];
                    var rv = mdtR.m_DefineTemplateMetaTypeList[i];
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
            m_EType = EMetaTypeType.MetaClass;
        }
        public void SetGenMetaClass( MetaGenTemplateClass mgtc )
        {
            m_MetaClass = mgtc;
            m_EType = EMetaTypeType.MetaGenClass;
        }
        public void SetMetaTemplate(MetaTemplate mt)
        {
            m_MetaTemplate = mt;
            if (mt != null)
            {
                m_EType = EMetaTypeType.Template;
            }
        }
        //public void SetGenMetaTemplate(MetaGenTemplate mt)
        //{
        //    //this.m_MetaGenTemplate = mt;
        //}        
        public void SetTemplateMetaClass( MetaClass mc )
        {
            //m_TemplateMetaClass = mc;
            m_MetaClass = mc;
            m_EType = EMetaTypeType.TemplateClassWithTemplate;
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
                for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
                {
                    var mgt = m_DefineTemplateMetaTypeList[i];
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
            if (index < 0 || index >= m_DefineTemplateMetaTypeList.Count) return null;

            return m_DefineTemplateMetaTypeList[index];
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
                sb.Append(this.m_MetaClass.allClassName);
            }
            for( int i = 0; i < this.genTemplateMetaTypeList.Count; i++ )
            {
                sb.AppendLine(this.genTemplateMetaTypeList[i].ToString());
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if( eType == EMetaTypeType.Template )
            {
                if (m_MetaTemplate != null)
                {
                    sb.Append(m_MetaTemplate.name);
                }
            }
            else if( eType == EMetaTypeType.TemplateClassWithTemplate )
            {
                if (m_MetaClass != null)
                {
                    sb.Append(m_MetaClass.allClassName);
                }
                if (m_DefineTemplateMetaTypeList.Count > 0)
                {
                    sb.Append("<");

                    for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
                    {
                        sb.Append(m_DefineTemplateMetaTypeList[i].ToString());
                        if (i < m_DefineTemplateMetaTypeList.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append(">");
                }
                if(m_MetaClass == CoreMetaClassManager.arrayMetaClass )
                    sb.Append("[" + this.m_ArrayLength + "]");
            }
            else if (eType == EMetaTypeType.MetaClass)
            {
                if (m_MetaClass != null)
                {
                    sb.Append(m_MetaClass.allClassName);
                }
            }
            else if (eType == EMetaTypeType.MetaGenClass )
            {
                if (m_MetaClass != null)
                {
                    if(m_MetaClass is MetaGenTemplateClass mgtc )
                    {
                        sb.Append(mgtc.metaTemplateClass.metaNode.allName);
                        if (m_GenTemplateMetaTypeList.Count > 0)
                        {
                            sb.Append("<");

                            for (int i = 0; i < m_GenTemplateMetaTypeList.Count; i++)
                            {
                                sb.Append(m_GenTemplateMetaTypeList[i].ToString());
                                if (i < m_GenTemplateMetaTypeList.Count - 1)
                                {
                                    sb.Append(",");
                                }
                            }
                            sb.Append(">");
                        }
                        if ( mgtc.metaTemplateClass == CoreMetaClassManager.arrayMetaClass )
                        {
                            sb.Append("[" + this.m_ArrayLength + "]");
                        }
                    }
                }
            }
            else
            {
                if (m_MetaClass is MetaGenTemplateClass mgtc)
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
                }
            }

            return sb.ToString();
        }
    }
}
