using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core;
using System.Security.Cryptography;

namespace SimpleLanguage.Core
{
    public partial class MetaType : MetaBase
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
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaMemberVariable enumValue => m_EnumValue;
        public bool isTemplate => m_MetaTemplate is MetaTemplate;
        public bool isGenTemplateClass => m_MetaClass is MetaGenTemplateClass;
        public bool isArray => m_MetaClass?.eType == EType.Array;
        public bool isDynamicClass => m_MetaClass == CoreMetaClassManager.dynamicMetaClass;
        public bool isDynamicData => m_MetaClass == CoreMetaClassManager.dynamicMetaData;
        public bool isDefineMetaClass => m_IsDefineMetaClass;
        private List<MetaTemplate> defineMetaTemplateList => m_DefineMetaTemplateList;

        private MetaInputTemplateCollection m_InputTemplateCollection = null;
        private MetaClass m_MetaClass = null;                       // int a = 0; => int  List<int> => List<int>
        private MetaClass m_RawMetaClass = null;                    // List<int> => list
        private MetaExpressNode m_DefaultExpressNode = null;        // int a => a = 0;
        private MetaTemplate m_MetaTemplate = null;                 // T t  => T
        private List<MetaTemplate> m_DefineMetaTemplateList = new List<MetaTemplate>();     //  Array<T1,T2> 一般用在返回值类型定义中
        private MetaMemberVariable m_EnumValue = null;              // Enum{ a = 1; } Enum e = Enum.a(20)=> Enum.a(20)
        private bool m_IsDefineMetaClass = false;

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
                else
                {
                    m_MetaTemplate = mc.GetTemplateMetaClassByName(finalNode.name );  //在语句中使用类中的T
                }
            }
            if( m_MetaClass == null )
            {
                m_MetaClass = m_RawMetaClass;
            }
        }
        public MetaType( MetaClass mc, List<MetaTemplate> defineMetaTemplateList )
        {
            m_RawMetaClass = m_MetaClass = mc;
            m_DefineMetaTemplateList = defineMetaTemplateList;
        }
        public MetaType( FileMetaClassDefine cmr, MetaClass mc )
        {
            if (cmr == null) return;

            string templateName = cmr.name;
            m_MetaTemplate = mc.GetTemplateMetaClassByName(templateName);
            if (m_MetaTemplate != null)
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
                        Console.WriteLine("Error 没有找到相当类: " + cmr.name);
                        return;
                    }
                    m_IsDefineMetaClass = true;

                    m_InputTemplateCollection = new MetaInputTemplateCollection(cmr.inputTemplateNodeList, mc );

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
                                Console.WriteLine("Error 解析数组，维度不允许有除Int之外的类型!!");
                            }
                        }
                        m_IsDefineMetaClass = true;
                        MetaType mitp = new MetaType(m_RawMetaClass);
                        m_InputTemplateCollection = new MetaInputTemplateCollection();
                        m_InputTemplateCollection.AddMetaTemplateParamsList(mitp);

                        m_MetaClass = CoreMetaClassManager.arrayMetaClass.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_InputTemplateCollection);
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
                        Console.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable " + cmr?.ToTokenString());
                        m_MetaClass = CoreMetaClassManager.objectMetaClass;
                    }
                }
            }

        }
       
        public MetaType(MetaClass mc, MetaInputTemplateCollection mitc = null )
        {
            if (mc == null)
            {
                Console.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
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
        public MetaType( MetaTemplate mt )
        {
            m_MetaTemplate = mt;
        }
        public MetaType( MetaType mdt )
        {
            m_RawMetaClass = mdt.m_RawMetaClass;
            m_MetaClass = mdt.m_MetaClass;
            m_MetaTemplate = mdt.m_MetaTemplate;
            m_InputTemplateCollection = mdt.m_InputTemplateCollection;
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
        public void SetEnumValue( MetaMemberVariable mmv )
        {
            m_EnumValue = mmv;
            m_MetaClass = mmv.ownerMetaClass;
        }
        public MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection input = null)
        {
            return m_MetaClass?.GetMetaMemberConstructFunction(input);
        }
        public static bool EqualMetaDefineType(MetaType mdtL, MetaType mdtR)
        {
            if (mdtL == null || mdtR == null)
                return false;

            if( mdtL.isTemplate )
            {
                if (mdtL.metaTemplate == mdtR.metaTemplate && mdtL.metaTemplate != null)
                {
                    return true;
                }
            }
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
        public void ClearMetaTemplate()
        {
            m_MetaTemplate = null;
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
            if( m_MetaTemplate == null )
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
            return null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_MetaTemplate != null )
            {
                sb.Append(m_MetaTemplate.allName);
            }
            else if (m_MetaClass is MetaGenTemplateClass)
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
