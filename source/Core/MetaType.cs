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
                return m_MetaClass?.allName;
            }
        }
        public MetaClass metaClass => m_MetaClass;
        public bool isEnum => m_MetaClass is MetaEnum;
        public bool isData => m_MetaClass is MetaData;
        public MetaMemberEnum enumValue => m_EnumValue;

        // 采用模板类表示法，仿C#做法，而不是C++，这种做法有效减少了，代码生成的数据，后续数据类型
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public bool isTemplate => m_MetaTemplate is MetaTemplate;
        private List<MetaTemplate> defineMetaTemplateList => m_DefineMetaTemplateList;
        public bool isGenTemplateClass => m_MetaClass is MetaGenTemplateClass;
        public bool isArray => m_MetaClass?.eType == EType.Array;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData;
        public bool isDefineMetaClass => m_IsDefineMetaClass;

        private MetaInputTemplateCollection m_InputTemplateCollection = null;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaClass m_RawMetaClass = null;                    // List<int> => list
        private MetaExpressNode m_DefaultExpressNode = null;        // int a => a = 0;
        private MetaMemberEnum m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private bool m_IsDefineMetaClass = false;

        private MetaTemplate m_MetaTemplate = null;                 // T t  => T
        private List<MetaTemplate> m_DefineMetaTemplateList = new List<MetaTemplate>();     //  Array<T1,T2> 一般用在返回值类型定义中

        public MetaType(MetaTemplate mt)
        { 

        }
        public MetaType( MetaType mt )
        {
            this.m_MetaClass = mt.m_MetaClass;
            this.m_RawMetaClass = mt.m_RawMetaClass;
        }
        public MetaType(FileInputTemplateNode fm, MetaClass mc)
        {
            m_RawMetaClass = ClassManager.instance.GetMetaClassByInputTemplateAndFileMeta(mc, fm);
            if (fm.defineClassCallLink?.callNodeList.Count > 0)
            {
                var finalNode = fm.defineClassCallLink?.callNodeList[fm.defineClassCallLink.callNodeList.Count - 1];

                if (finalNode.inputTemplateNodeList.Count > 0)
                {
                    m_IsDefineMetaClass = true;
                    m_InputTemplateCollection = new MetaInputTemplateCollection(finalNode.inputTemplateNodeList, mc);
                    m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
                }
            }
            if( m_MetaClass == null )
            {
                m_MetaClass = m_RawMetaClass;
            }
        }
        public MetaType( FileMetaClassDefine cmr, MetaClass mc )
        {
            if (cmr == null) return;

            string templateName = cmr.name;
            var metaTemplate = mc.GetTemplateMetaClassByName(templateName);
            if (metaTemplate != null)
            {
                m_MetaClass = null;
                m_IsDefineMetaClass = true;
            }
            else
            {
                if (cmr.isInputTemplateData)
                {
                    m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                    if(m_RawMetaClass == null )
                    {
                        Debug.WriteLine("Error 没有找到相当类: " + cmr.name);
                        return;
                    }
                    m_IsDefineMetaClass = true;

                    m_InputTemplateCollection = new MetaInputTemplateCollection(cmr.inputTemplateNodeList, m_RawMetaClass);

                    if( m_InputTemplateCollection.isTemplateName )
                    {
                        m_MetaClass = m_RawMetaClass;
                    }
                    else
                    {
                        m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
                    }
                }
                else
                {
                    if (cmr.isArray)
                    {
                        m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        List<int> arrayList = new List<int>();
                        for (int i = 0; i < cmr.arrayTokenList.Count; i++)
                        {
                            var token = cmr.arrayTokenList[i];
                            if (token.GetEType() == EType.UInt32 || token.GetEType() == EType.Int32)
                            {
                                arrayList.Add(int.Parse(token.lexeme.ToString()));
                            }
                            else
                            {
                                Debug.WriteLine("Error 解析数组，维度不允许有除Int之外的类型!!");
                            }
                        }
                        m_IsDefineMetaClass = true;
                        MetaType mitp = new MetaType(m_RawMetaClass);
                        m_InputTemplateCollection = new MetaInputTemplateCollection();
                        m_InputTemplateCollection.AddMetaTemplateParamsList(mitp);

                        m_MetaClass = CoreMetaClassManager.listMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
                    }

                    if (m_MetaClass == null)
                    {
                        m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        
                        if (m_RawMetaClass != null)
                        {
                            m_IsDefineMetaClass = true;
                        }
                        else
                        {
                            m_IsDefineMetaClass = false;
                        }
                        m_MetaClass = m_RawMetaClass;
                    }

                    if (m_MetaClass == null)
                    {
                        Debug.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable " + cmr?.ToTokenString());
                        m_MetaClass = CoreMetaClassManager.objectMetaClass;
                    }
                }
            }

        }       
        public MetaType(MetaClass mc, MetaInputTemplateCollection mitc = null )
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
                m_InputTemplateCollection = mitc;

                m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
            }

        }
        //public MetaType( MetaTemplate mt )
        //{
        //    m_MetaTemplate = mt;
        //}
        //public MetaType( MetaType mdt )
        //{
        //    m_RawMetaClass = mdt.m_RawMetaClass;
        //    m_MetaClass = mdt.m_MetaClass;
        //    m_MetaTemplate = mdt.m_MetaTemplate;
        //    m_InputTemplateCollection = mdt.m_InputTemplateCollection;
        //}

        public static MetaType NewMetaTypeByMemeberDefine(FileMetaClassDefine cmr, MetaClass mc)
        {
            MetaType mt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (cmr == null) return mt;

            string templateName = cmr.name;
            var metaTemplate = mc.GetTemplateMetaClassByName(templateName);
            if (metaTemplate != null)
            {
                mt.m_MetaClass = null;
                mt.m_MetaTemplate = metaTemplate;
                mt.m_IsDefineMetaClass = true;
            }
            else
            {
                if (cmr.isInputTemplateData)
                {
                    mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                    if (mt.m_RawMetaClass == null)
                    {
                        Debug.WriteLine("Error 没有找到相当类: " + cmr.name);
                        return mt;
                    }
                    mt.m_IsDefineMetaClass = true;

                    if (cmr.inputTemplateNodeList?.Count > 0)
                    {
                        mt.m_InputTemplateCollection = new MetaInputTemplateCollection(cmr.inputTemplateNodeList, mt.m_RawMetaClass);
                        mt.m_MetaClass = mt.m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(mt.m_InputTemplateCollection);
                    }
                }
                else
                {
                    if (cmr.isArray)
                    {
                        mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        List<int> arrayList = new List<int>();
                        for (int i = 0; i < cmr.arrayTokenList.Count; i++)
                        {
                            var token = cmr.arrayTokenList[i];
                            if (token.GetEType() == EType.UInt32 || token.GetEType() == EType.Int32)
                            {
                                arrayList.Add(int.Parse(token.lexeme.ToString()));
                            }
                            else
                            {
                                Debug.WriteLine("Error 解析数组，维度不允许有除Int之外的类型!!");
                            }
                        }
                        mt.m_IsDefineMetaClass = true;
                        MetaType mitp = new MetaType(mt.m_RawMetaClass);
                        mt.m_InputTemplateCollection = new MetaInputTemplateCollection();
                        mt.m_InputTemplateCollection.AddMetaTemplateParamsList(mitp);

                        mt.m_MetaClass = CoreMetaClassManager.listMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(mt.m_InputTemplateCollection);
                    }
                    else
                    {
                        mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);

                        if (mt.m_RawMetaClass != null)
                        {
                            mt.m_IsDefineMetaClass = true;
                        }
                        else
                        {
                            mt.m_IsDefineMetaClass = false;
                        }
                        mt.m_MetaClass = mt.m_RawMetaClass;
                    }

                    if (mt.m_MetaClass == null)
                    {
                        Debug.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable " + cmr?.ToTokenString());
                        mt.m_MetaClass = CoreMetaClassManager.objectMetaClass;
                    }
                }
            }
            return mt;
        }
        public static MetaType NewMetaTypeByStatement(FileMetaClassDefine cmr, MetaClass mc)
        {
            MetaType mt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (cmr == null) return mt;

            string templateName = cmr.name;
            var metaTemplate = mc.GetTemplateMetaClassByName(templateName);
            if (metaTemplate != null)
            {
                mt.m_MetaClass = null;
                mt.m_IsDefineMetaClass = true;
            }
            else
            {
                if (cmr.isInputTemplateData)
                {
                    mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                    if (mt.m_RawMetaClass == null)
                    {
                        Debug.WriteLine("Error 没有找到相当类: " + cmr.name);
                        return mt;
                    }
                    mt.m_IsDefineMetaClass = true;

                    if (cmr.inputTemplateNodeList?.Count > 0)
                    {
                        mt.m_InputTemplateCollection = new MetaInputTemplateCollection(cmr.inputTemplateNodeList, mt.m_RawMetaClass);
                        mt.m_MetaClass = mt.m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(mt.m_InputTemplateCollection);

                        var mgtc = (mt.m_MetaClass as MetaGenTemplateClass);
                        mgtc.UpdateGenMemberDefineMetaType();
                        mgtc.ParseTemplateClassMemberFunction();

                    }
                }
                else
                {
                    if (cmr.isArray)
                    {
                        mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        List<int> arrayList = new List<int>();
                        for (int i = 0; i < cmr.arrayTokenList.Count; i++)
                        {
                            var token = cmr.arrayTokenList[i];
                            if (token.GetEType() == EType.UInt32 || token.GetEType() == EType.Int32)
                            {
                                arrayList.Add(int.Parse(token.lexeme.ToString()));
                            }
                            else
                            {
                                Debug.WriteLine("Error 解析数组，维度不允许有除Int之外的类型!!");
                            }
                        }
                        mt.m_IsDefineMetaClass = true;
                        MetaType mitp = new MetaType(mt.m_RawMetaClass);
                        mt.m_InputTemplateCollection = new MetaInputTemplateCollection();
                        mt.m_InputTemplateCollection.AddMetaTemplateParamsList(mitp);

                        mt.m_MetaClass = CoreMetaClassManager.listMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(mt.m_InputTemplateCollection);
                    }
                    else
                    {
                        mt.m_RawMetaClass = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);

                        if (mt.m_RawMetaClass != null)
                        {
                            mt.m_IsDefineMetaClass = true;
                        }
                        else
                        {
                            mt.m_IsDefineMetaClass = false;
                        }
                        mt.m_MetaClass = mt.m_RawMetaClass;
                    }

                    if (mt.m_MetaClass == null)
                    {
                        Debug.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable " + cmr?.ToTokenString());
                        mt.m_MetaClass = CoreMetaClassManager.objectMetaClass;
                    }
                }
            }
            return mt;
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
                if( mdtL.m_InputTemplateCollection != null )
                {
                    if(mdtR.m_InputTemplateCollection != null )
                    {
                        if (mdtL.m_InputTemplateCollection.metaTemplateParamsList.Count
                            == mdtR.m_InputTemplateCollection?.metaTemplateParamsList.Count)
                        {
                            for (int i = 0; i < mdtL.m_InputTemplateCollection.metaTemplateParamsList.Count; i++)
                            {
                                var mtpl = mdtL.m_InputTemplateCollection.metaTemplateParamsList[i];
                                var mtpr = mdtR.m_InputTemplateCollection.metaTemplateParamsList[i];
                                if (EqualMetaDefineType(mtpl, mtpr))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
        public void SetRawMetaClass( MetaClass mc )
        {
            m_RawMetaClass = mc;
        }
        public void UpdateMetaClassByRawMetaClassAndInputTemplateCollection()
        {
            m_MetaClass = m_RawMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
        }
        public void SetMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
            m_IsDefineMetaClass = true;
        }
        public void SetMetaInputTemplateCollection( MetaInputTemplateCollection mitc )
        {
            m_InputTemplateCollection = mitc;
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
                sb.Append(m_MetaClass?.allName);
            }
            return sb.ToString();
        }
    }
}
