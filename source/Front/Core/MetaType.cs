//****************************************************************************
//  File:      MetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EMetaTypeType
    {
        None,
        MetaClass,
        MetaData,
        MetaEnum,
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
                if (m_MetaData != null) return m_MetaData.allClassName;
                if (m_MetaEnum != null) return m_MetaEnum.allClassName;
                return m_MetaClass?.allClassName;
            }
        }
        public bool isNullable => m_IsNullable;
        public bool isEnum => m_MetaEnum != null || m_EMetaTypeType == EMetaTypeType.MetaEnum;
        public bool isData => m_MetaData != null || m_EMetaTypeType == EMetaTypeType.MetaData;
        public bool isClass => m_MetaData != null || m_EMetaTypeType == EMetaTypeType.MetaClass;
        public bool isNull => m_MetaClass == CoreMetaClassManager.nullMetaClass;
        public bool isMap => m_MetaClass == CoreMetaClassManager.mapMetaClass;
        public bool isTemplate => m_EMetaTypeType == EMetaTypeType.Template;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData
            || (m_MetaData != null && m_MetaData.isDynamic);
        public int arrayLength => m_ArrayLength;
        public EMetaTypeType eMetaTypeType => m_EMetaTypeType;
        public MetaClass metaClass => m_MetaClass;
        public MetaEnum metaEnum => m_MetaEnum;
        public MetaData metaData => m_MetaData;
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaMemberEnum enumValue => m_EnumValue;
        public List<MetaType> defineTemplateMetaTypeList => m_DefineTemplateMetaTypeList;
        //public List<MetaType> genTemplateMetaTypeList => m_GenTemplateMetaTypeList;

        private EMetaTypeType m_EMetaTypeType = EMetaTypeType.None;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaEnum m_MetaEnum = null;
        private MetaData m_MetaData = null;
        private MetaType m_ParentMetaType = null;
        private MetaTemplate m_MetaTemplate = null;
        //private MetaType m_SourceMetaType = null;                         //生成类的 对应来源类
        //private MetaGenTemplate m_MetaGenTemplate = null;
        private MetaMemberEnum m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private List<MetaType> m_DefineTemplateMetaTypeList = new List<MetaType>();     //  Map<T1,T2> 一般用在返回值类型定义中
        //private List<MetaType> m_GenTemplateMetaTypeList = new List<MetaType>();     //  Map<T1,T2> 一般用在返回值类型定义中  //慢慢的移除 直接使用gen class中的数据
        private int m_ArrayLength = -1;       
        private bool m_IsNullable = false;   // 新增：可空标记

        public MetaType()
        {
        }
        public MetaType( EType etype )
        {
            m_EMetaTypeType = EMetaTypeType.MetaClass;
            m_MetaClass = CoreMetaClassManager.GetMetaClassByEType(etype);
            SyncSpecialMetaTypeByMetaClass();
        }
        public MetaType(MetaTemplate mt, string fromName = "" )
        {
            m_EMetaTypeType = EMetaTypeType.Template;
            m_MetaTemplate = mt;
            m_MetaClass = mt.extendsMetaClass;
            SyncSpecialMetaTypeByMetaClass();
        }
        public MetaType( MetaGenTemplateClass mgtc, List<MetaType> defineMTList )
        {
            m_EMetaTypeType = EMetaTypeType.MetaGenClass;
            m_MetaClass = mgtc;
            m_DefineTemplateMetaTypeList = defineMTList;
            SyncSpecialMetaTypeByMetaClass();
            //m_GenTemplateMetaTypeList = genMTList;
        }
        public MetaType( MetaClass mc )
        {
            if (mc == null)
            {
                Log.AddMetaCoreLog(LID.AutoMetaTypeL87, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_MetaClass = mc;
            m_EMetaTypeType = EMetaTypeType.MetaClass;
            SyncSpecialMetaTypeByMetaClass();
        }
        public MetaType( MetaData md )
        {
            if (md == null)
            {
                Log.AddMetaCoreLog(LID.AutoMetaTypeL87, "Error MetaDefineType RetMetaData is Null");
            }
            m_MetaData = md;
            m_EMetaTypeType = EMetaTypeType.MetaData;
        }
        public MetaType( MetaEnum me )
        {
            if (me == null)
            {
                Log.AddMetaCoreLog(LID.AutoMetaTypeL87, "Error MetaDefineType RetMetaEnum is Null");
            }
            m_MetaEnum = me;
            m_EMetaTypeType = EMetaTypeType.MetaEnum;
        }
        public MetaType( MetaClass mc, List<MetaType> mtList )
        {
            if (mc == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            //m_TemplateMetaClass = templatemc;
            m_MetaClass = mc;
            m_DefineTemplateMetaTypeList = mtList;
            //m_GenTemplateMetaTypeList = mtList;
            m_EMetaTypeType = EMetaTypeType.TemplateClassWithTemplate;
            SyncSpecialMetaTypeByMetaClass();
        }
        public MetaType( MetaClass mc, MetaClass templatemc, MetaInputTemplateCollection mitc )
        {
            if (mc == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
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
            SyncSpecialMetaTypeByMetaClass();
        }
        public MetaType(MetaType mt) : base(mt)
        {
            this.m_MetaClass = mt.m_MetaClass;
            this.m_MetaEnum = mt.m_MetaEnum;
            this.m_MetaData = mt.m_MetaData;
            //this.m_TemplateMetaClass = mt.m_TemplateMetaClass;
            this.m_ParentMetaType = mt.m_ParentMetaType;
            this.m_MetaTemplate = mt.m_MetaTemplate;
            this.m_EnumValue = mt.m_EnumValue;
            //this.m_FromName = mt.m_FromName;
            this.m_EMetaTypeType = mt.m_EMetaTypeType;
            this.m_ArrayLength = mt.m_ArrayLength;
            // 复制 nullable 标记
            this.m_IsNullable = mt.m_IsNullable;
            for (int i = 0; i < mt.m_DefineTemplateMetaTypeList.Count; i++)
            {
                MetaType mtc = new MetaType(mt.m_DefineTemplateMetaTypeList[i]);
                m_DefineTemplateMetaTypeList.Add(mtc);
            }
            //for (int i = 0; i < mt.m_GenTemplateMetaTypeList.Count; i++)
            //{
            //    MetaType mtc = new MetaType(mt.m_GenTemplateMetaTypeList[i]);
            //    m_GenTemplateMetaTypeList.Add(mtc);
            //}
        }
        public bool IsArray()
        {
            if(m_EMetaTypeType == EMetaTypeType.MetaGenClass )
            {
                if( m_MetaClass is MetaGenTemplateClass mgtc )
                {
                    if(mgtc.metaTemplateClass.ExtendClassContainMetaClass( CoreMetaClassManager.arrayMetaClass ) )
                    {
                        return true;
                    }
                }
            }
            else if(m_EMetaTypeType == EMetaTypeType.MetaClass )
            {
                MetaClass cmc = m_MetaClass;
                while ( true )
                {
                    if(cmc is MetaGenTemplateClass mgtc )
                    {
                        if( mgtc.metaTemplateClass.ExtendClassContainMetaClass(CoreMetaClassManager.arrayMetaClass) )
                        {
                            return true;
                        }
                    }
                    if( cmc == CoreMetaClassManager.arrayMetaClass )
                    {
                        return true;
                    }
                    cmc = cmc.extendClass;
                    if( cmc == null || cmc == CoreMetaClassManager.objectMetaClass )
                    {
                        break;
                    }
                    int a = 10;
                }
            }
            else if(m_EMetaTypeType == EMetaTypeType.TemplateClassWithTemplate )
            {
                if (m_MetaClass == CoreMetaClassManager.arrayMetaClass)
                {
                    return true;
                }
            }
                return false;
        }
        private static bool IsSameTemplateClassByIdentityOrName(MetaClass candidate, MetaClass expected, string expectedClassName)
        {
            if (candidate == null) return false;
            if (expected != null && candidate == expected) return true;
            var n = candidate.allClassName;
            return n == expectedClassName;
        }

        private bool IsTemplateTypeByNameOrIdentity(MetaClass expectedMetaClass, string expectedClassName)
        {
            if (m_MetaClass == null) return false;

            if (m_EMetaTypeType == EMetaTypeType.MetaGenClass || m_EMetaTypeType == EMetaTypeType.MetaClass)
            {
                if (m_MetaClass is MetaGenTemplateClass mgtc)
                {
                    return IsSameTemplateClassByIdentityOrName(mgtc.metaTemplateClass, expectedMetaClass, expectedClassName);
                }
            }

            if (m_EMetaTypeType == EMetaTypeType.TemplateClassWithTemplate)
            {
                return IsSameTemplateClassByIdentityOrName(m_MetaClass, expectedMetaClass, expectedClassName);
            }

            return false;
        }

        /// <summary> 是否为 Core.IIterator&lt;T&gt; 的实例类型（与 <see cref="IsArray"/> 对称）。 </summary>
        public bool IsIterator()
        {
            return IsTemplateTypeByNameOrIdentity(CoreMetaClassManager.iteratorMetaClass, "Core.IIterator<T>");
        }
        /// <summary> 是否为 Core.IIterable&lt;T&gt; 的实例类型（与 <see cref="IsArray"/> 对称）。 </summary>
        public bool IsIterable()
        {
            return IsTemplateTypeByNameOrIdentity(CoreMetaClassManager.iterableMetaClass, "Core.IIterable<T>");
        }
        public void SetNullable(bool v) { m_IsNullable = v; }
        public bool IsNum()
        {
            return ClassManager.IsNumberClass(this.m_MetaClass);
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
                    var list = curmt.GetGenTemplateMetaTypeList();
                    if(list.Count == 1 )
                    {
                        curmt = list[0];
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
                    var gmlist = curmt.GetGenTemplateMetaTypeList();
                    if (gmlist.Count == 1)
                    {
                        curmt = gmlist[0];
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
            this.m_MetaEnum = mt.m_MetaEnum;
            this.m_MetaData = mt.m_MetaData;
            //this.m_TemplateMetaClass = mt.m_TemplateMetaClass;
            this.m_ParentMetaType = mt.m_ParentMetaType;
            this.m_MetaTemplate = mt.m_MetaTemplate;
            this.m_EnumValue = mt.m_EnumValue;
            //this.m_FromName = mt.m_FromName;
            this.m_EMetaTypeType = mt.m_EMetaTypeType;
            this.m_DefineTemplateMetaTypeList = mt.m_DefineTemplateMetaTypeList;
            //this.m_GenTemplateMetaTypeList = mt.m_GenTemplateMetaTypeList;
            this.m_IsNullable = mt.m_IsNullable;
        }
        public List<MetaType> GetGenTemplateMetaTypeList()
        {
            if( eMetaTypeType == EMetaTypeType.MetaClass || eMetaTypeType == EMetaTypeType.MetaGenClass )
            {
                return this.m_MetaClass.genMetaTypeTemplateList;
            }
            return this.m_DefineTemplateMetaTypeList;
        }
        public MetaType GetMetaTypeByIndex( int index = 0)
        {
            //Debug.Assert(false, "");
            if (this.eMetaTypeType == EMetaTypeType.MetaClass
                || this.eMetaTypeType == EMetaTypeType.MetaGenClass )
            {
                return this.metaClass.GetGenMetaTypeTemplateByIndex(index);
            }
            else if (this.eMetaTypeType == EMetaTypeType.TemplateClassWithTemplate )
            {
                return this.m_DefineTemplateMetaTypeList[index];
            }
            return null;
        }
        public MetaClass GetTemplateMetaClass()
        {
            if (this.m_MetaClass is MetaGenTemplateClass mgtc)
            {
                return mgtc.metaTemplateClass;
            }
            if( isTemplate )
            {
                return m_MetaTemplate.extendsMetaClass;
            }
            return m_MetaClass;
        }
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
            //Debug.Assert(false, "");
            for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
            {
                var tmt = m_DefineTemplateMetaTypeList[i];
                if (tmt.GenTemplateIsIncludeTemplate())
                {
                    return true;
                }
            }
            return m_MetaTemplate != null;
        }
        public bool IsIncludeClassTemplate(MetaBase ownerBase)
        {
            if (ownerBase is not MetaClass ownerClass)
                return false;
            if (m_MetaTemplate != null && ownerClass.isTemplateClass)
            {
                return ownerClass.metaTemplateList.IndexOf(m_MetaTemplate) != -1;
            }
            for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
            {
                var tmt = m_DefineTemplateMetaTypeList[i];
                if (tmt.IsIncludeClassTemplate(ownerBase))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsIncludeFunctionTemplate( MetaMemberFunction mmf )
        {
            if( eMetaTypeType == EMetaTypeType.Template )
            {
                if (m_MetaTemplate != null && mmf.isTemplateFunction)
                {
                    return mmf.metaMemberTemplateCollection.metaTemplateList.IndexOf(m_MetaTemplate) != -1;
                }
            }
            else if( eMetaTypeType == EMetaTypeType .TemplateClassWithTemplate )
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
            if( eMetaTypeType == EMetaTypeType.TemplateClassWithTemplate )
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
        public MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection input = null)
        {
            return m_MetaClass?.GetMetaMemberConstructFunction(input);
        }
        public static bool ExtendRelateionMetaType( MetaType mdtL, MetaType mdtR )
        {
            if (mdtL == null || mdtR == null)
                return false;

            if (mdtL.eMetaTypeType != mdtR.eMetaTypeType)
            {
                if( mdtR.metaClass is MetaGenTemplateClass mgtc )
                {
                    if( mdtL.metaClass.ExtendClassContainMetaClass( mgtc.metaTemplateClass ) )
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mdtL.eMetaTypeType == EMetaTypeType.Template)
            {
                if (mdtL.metaTemplate == mdtR.metaTemplate)
                {
                    return true;
                }
            }
            else if (mdtL.eMetaTypeType == EMetaTypeType.MetaClass)
            {
                if (mdtR.metaClass.IsContainMetaClass( mdtL.metaClass ) )
                {
                    return true;
                }
            }
            else if( mdtL.eMetaTypeType == EMetaTypeType.MetaGenClass )
            {
                if( mdtL.metaClass is MetaGenTemplateClass mgtc )
                {
                    if( mdtR.eMetaTypeType == EMetaTypeType.MetaGenClass )
                    {
                        if( mgtc.metaTemplateClass == (mdtR.metaClass as MetaGenTemplateClass).metaTemplateClass )
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if( mgtc.metaTemplateClass == mdtR.metaClass )
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (mdtL.m_MetaClass != mdtR.m_MetaClass)
                {
                    return false;
                }
                if (mdtL.m_DefineTemplateMetaTypeList.Count != mdtR.m_DefineTemplateMetaTypeList.Count)
                {
                    return false;
                }
                for (int i = 0; i < mdtL.m_DefineTemplateMetaTypeList.Count; i++)
                {
                    var lv = mdtL.m_DefineTemplateMetaTypeList[i];
                    var rv = mdtR.m_DefineTemplateMetaTypeList[i];
                    if (TypeManager.CompareMetaType(lv, rv) == false)
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
            m_MetaEnum = null;
            m_MetaData = null;
            SyncSpecialMetaTypeByMetaClass();
            m_EMetaTypeType = EMetaTypeType.MetaClass;
        }
        public void SetMetaData(MetaData md)
        {
            m_MetaData = md;
            m_MetaEnum = null;
            m_MetaClass = null;
            m_EMetaTypeType = EMetaTypeType.MetaData;
        }
        public void SetMetaEnum(MetaEnum me)
        {
            m_MetaEnum = me;
            m_MetaData = null;
            m_MetaClass = null;
            m_EMetaTypeType = EMetaTypeType.MetaEnum;
        }
        public void SetGenMetaClass( MetaGenTemplateClass mgtc )
        {
            m_MetaClass = mgtc;
            m_MetaEnum = null;
            m_MetaData = null;
            SyncSpecialMetaTypeByMetaClass();
            m_EMetaTypeType = EMetaTypeType.MetaGenClass;
        }
        public void SetMetaTemplate(MetaTemplate mt)
        {
            m_MetaTemplate = mt;
            if (mt != null)
            {
                m_EMetaTypeType = EMetaTypeType.Template;
            }
        }
        public void SetTemplateMetaClass( MetaClass mc )
        {
            //m_TemplateMetaClass = mc;
            m_MetaClass = mc;
            m_MetaEnum = null;
            m_MetaData = null;
            SyncSpecialMetaTypeByMetaClass();
            m_EMetaTypeType = EMetaTypeType.TemplateClassWithTemplate;
        }

        private void SyncSpecialMetaTypeByMetaClass()
        {
            // MetaData / MetaEnum 不再继承自 MetaClass，
            // 二者作为独立的元数据节点存在于 MetaType 内部字段，
            // 仅在通过 MetaData / MetaEnum 构造或 SetMetaData / SetMetaEnum 时显式设置。
        }
        //生成注册后的 模板类的实例类
        public MetaClass UpdateMetaGenTemplate( List<MetaGenTemplate> metaGenTemplateList)
        {
            if( eMetaTypeType == EMetaTypeType.Template )
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
            else if( eMetaTypeType == EMetaTypeType.TemplateClassWithTemplate )
            {
                List<MetaClass> mcList = new List<MetaClass>();
                for (int i = 0; i < m_DefineTemplateMetaTypeList.Count; i++)
                {
                    var mgt = m_DefineTemplateMetaTypeList[i];
                    if (mgt.eMetaTypeType == EMetaTypeType.MetaClass)
                    {
                        mcList.Add(mgt.metaClass);
                    }
                    else
                    {
                        var mc = mgt.UpdateMetaGenTemplate(metaGenTemplateList);
                        if( mc == null )
                        {
                            Log.AddMetaCoreLog(LID.AutoMetaTypeL573, "注册生成类是空!");
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

            if (m_MetaData != null)
            {
                sb.Append(m_MetaData.allClassName);
                return sb.ToString();
            }
            if (m_MetaEnum != null)
            {
                sb.Append(m_MetaEnum.allClassName);
                return sb.ToString();
            }
            if (m_MetaClass != null)
            {
                sb.Append(this.m_MetaClass.allClassName);
            }
            for( int i = 0; i < this.m_DefineTemplateMetaTypeList.Count; i++ )
            {
                sb.AppendLine(this.m_DefineTemplateMetaTypeList[i].ToString());
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (eMetaTypeType == EMetaTypeType.MetaData)
            {
                if (m_MetaData != null)
                {
                    sb.Append(m_MetaData.allClassName);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "meta type is m_MetaData is null");
                }
                return sb.ToString();
            }
            if (eMetaTypeType == EMetaTypeType.MetaEnum)
            {
                if (m_MetaEnum != null)
                {
                    sb.Append(m_MetaEnum.allClassName);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "meta type is m_MetaEnum is null");
                }
                return sb.ToString();
            }

            if( eMetaTypeType == EMetaTypeType.Template )
            {
                if (m_MetaTemplate != null)
                {
                    sb.Append(m_MetaTemplate.name);
                }
            }
            else if( eMetaTypeType == EMetaTypeType.TemplateClassWithTemplate )
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
            else if (eMetaTypeType == EMetaTypeType.MetaClass)
            {
                if (m_MetaClass != null)
                {
                    sb.Append(m_MetaClass.allClassName);
                    if (m_MetaClass.genMetaClassTemplateList.Count > 0)
                    {
                        sb.Append("<");

                        for (int i = 0; i < m_MetaClass.genMetaClassTemplateList.Count; i++)
                        {
                            sb.Append(m_MetaClass.genMetaClassTemplateList[i].ToString());
                            if (i < m_MetaClass.genMetaClassTemplateList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append(">");
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "meta type is m_MetaClass is null");
                }
            }
            else if (eMetaTypeType == EMetaTypeType.MetaGenClass )
            {
                if (m_MetaClass is MetaGenTemplateClass mgtc)
                {
                    sb.Append(mgtc.metaTemplateClass.metaNode.allName);

                    if (mgtc.genMetaClassTemplateList.Count > 0)
                    {
                        sb.Append("<");

                        for (int i = 0; i < mgtc.genMetaClassTemplateList.Count; i++)
                        {
                            sb.Append(mgtc.genMetaClassTemplateList[i].ToString());
                            if (i < mgtc.genMetaClassTemplateList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append(">");
                    }
                    if (mgtc.metaTemplateClass == CoreMetaClassManager.arrayMetaClass)
                    {
                        sb.Append("[" + this.m_ArrayLength + "]");
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "eMetaTypeType is null ");
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
                }
            }

            return sb.ToString();
        }
    }
}
