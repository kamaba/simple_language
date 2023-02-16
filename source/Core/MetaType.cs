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
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaMemberVariable enumValue => m_EnumValue;
        public bool isUseInputTemplate => m_IsUseInputTemplate;
        public bool isEnum { get { return m_MetaClass is MetaEnum; } }
        public bool isData { get { return m_MetaClass is MetaData; } }
        public bool isArray{ get { return m_MetaClass == CoreMetaClassManager.arrayMetaClass; }  }
        public bool isDynamicClass => m_IsDynamicClass;
        public bool isDefineMetaClass => m_IsDefineMetaClass;
        public MetaInputTemplateCollection inputTemplateCollection => m_InputTemplateCollection;

        private MetaInputTemplateCollection m_InputTemplateCollection = null;
        private List<int> m_ArrayList = new List<int>();
        private MetaClass m_MetaClass = null;
        private MetaExpressNode m_DefaultExpressNode = null;
        private MetaTemplate m_MetaTemplate = null;
        private bool m_IsDefineMetaClass = false;
        private bool m_IsUseInputTemplate = false;
        private bool m_IsDynamicClass = false;
        private MetaMemberVariable m_EnumValue = null;


        public MetaType(FileInputTemplateNode fm, MetaClass mc)
        {
            var m_MetaDefineType = ClassManager.instance.GetMetaDefineTypeByInputTemplateAndFileMeta(mc, fm);
            m_MetaClass = m_MetaDefineType.metaClass;
            if (fm.defineClassCallLink?.callNodeList.Count > 0)
            {
                var finalNode = fm.defineClassCallLink?.callNodeList[fm.defineClassCallLink.callNodeList.Count - 1];

                if (finalNode.inputTemplateNodeList.Count > 0)
                {
                    m_IsDefineMetaClass = true;
                    m_InputTemplateCollection = new MetaInputTemplateCollection(finalNode.inputTemplateNodeList, mc);
                }
            }
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
                    m_IsUseInputTemplate = true;
                    var dmt = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                    if( dmt == null )
                    {
                        Console.WriteLine("Error 没有找到相当类: " + cmr.name);
                        return;
                    }
                    m_IsDefineMetaClass = true;
                    m_MetaClass = dmt;

                    m_InputTemplateCollection = new MetaInputTemplateCollection(cmr.inputTemplateNodeList, mc );
                }
                else
                {
                    if (cmr.isArray)
                    {
                        m_MetaClass = CoreMetaClassManager.arrayMetaClass;
                        var dmt = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        for (int i = 0; i < cmr.arrayTokenList.Count; i++)
                        {
                            var token = cmr.arrayTokenList[i];
                            if (token.GetEType() == EType.UInt32 || token.GetEType() == EType.Int32)
                            {
                                m_ArrayList.Add(int.Parse(token.lexeme.ToString()));
                            }
                            else
                            {
                                Console.WriteLine("Error 解析数组，维度不允许有除Int之外的类型!!");
                            }
                        }
                        m_IsDefineMetaClass = true;
                        MetaType mitp = new MetaType(dmt);
                        m_InputTemplateCollection = new MetaInputTemplateCollection();
                        m_InputTemplateCollection.AddMetaTemplateParamsList(mitp);
                    }

                    if (m_MetaClass == null)
                    {
                        var dmt = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(mc, cmr);
                        if (dmt != null)
                        {
                            m_IsDefineMetaClass = true;
                        }
                        else
                        {
                            m_IsDefineMetaClass = false;
                        }
                        m_MetaClass = dmt;
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
            m_MetaClass = mc;
            if (m_MetaClass == null)
            {
                Console.WriteLine("Error MetaDefineType RetMetaClass is Null MetaMemberVariable Only MetaClass");
            }
            m_InputTemplateCollection = mitc;
        }
        public MetaType( MetaTemplate mt )
        {
            m_MetaTemplate = mt;
        }
        public MetaType( MetaType mdt )
        {
            m_MetaClass = mdt.m_MetaClass;
            m_MetaTemplate = mdt.m_MetaTemplate;
            m_InputTemplateCollection = mdt.m_InputTemplateCollection;
            m_IsDynamicClass = mdt.isDynamicClass;
        }
        public void SetDynamicClass( bool flag )
        {
            m_IsDynamicClass = flag;
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

            if (mdtL.metaClass == mdtR.metaClass)
            {
                if (mdtL.m_InputTemplateCollection.metaTemplateParamsList.Count == mdtR.m_InputTemplateCollection.metaTemplateParamsList.Count)
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
            else
            {
                if (mdtL.metaTemplate == mdtR.metaTemplate && mdtL.metaTemplate != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
            m_IsDefineMetaClass = true;
        }
        public void SetMetaInputTemplateCollection(MetaInputTemplateCollection mitc )
        {
            m_InputTemplateCollection = mitc;
        }
        public MetaType GetMetaInputTemplateByIndex( int index = 0 )
        {
            if (m_InputTemplateCollection == null) return null;

            if( index >= 0 && index < m_InputTemplateCollection.metaTemplateParamsList.Count )
            {
                var mtp = m_InputTemplateCollection.metaTemplateParamsList[index];
                return mtp;
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
            else
            {
                sb.Append(m_MetaClass?.allName);
            }
            if(m_ArrayList.Count > 0 )
            {
                sb.Append("[");
                for( int i = 0; i < m_ArrayList.Count; i++ )
                {
                    sb.Append(m_ArrayList[i]);
                    if (i < m_ArrayList.Count - 1)
                        sb.Append(",");
                }
                sb.Append("]");
            }
            if(m_InputTemplateCollection != null && m_InputTemplateCollection.metaTemplateParamsList.Count > 0 )
            {
                sb.Append("<");
                for (int i = 0; i < m_InputTemplateCollection.metaTemplateParamsList.Count; i++)
                {
                    var tpl = m_InputTemplateCollection.metaTemplateParamsList[i];
                    sb.Append( tpl.ToFormatString() );
                    if (i < m_InputTemplateCollection.metaTemplateParamsList.Count - 1)
                        sb.Append(",");

                }
                sb.Append(">");
            }

            return sb.ToString();
        }
    }
}
