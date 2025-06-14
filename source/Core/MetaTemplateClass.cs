//****************************************************************************
//  File:      MetaTemplateClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/12/17 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaClass
    {
        public virtual bool isGenTemplate { get { return false; } }
        public List<MetaGenTemplateClass> metaGenTemplateClassList => m_MetaGenTemplateClassList;
        public bool isTemplateClass { get { return m_MetaTemplateList.Count > 0; } }        //是否是模版类
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();
        protected List<MetaGenTemplateClass> m_MetaGenTemplateClassList = new List<MetaGenTemplateClass>();

        public bool isDefineTemplate(string name)
        {
            return m_MetaTemplateList.Find(a => a.name == name) != null;
        }
        public void ParseMetaInConstraint()
        {
            foreach (var it in m_MetaTemplateList)
            {
                it.ParseInConstraint();
            }
        }
        public void ParseFileMetaClassTemplate(FileMetaClass fmc)
        {
            for (int i = 0; i < fmc.templateDefineList.Count; i++)
            {
                string tTemplateName = fmc.templateDefineList[i].name;
                if (m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
                {
                    Debug.Write("Error 定义模式名称重复!!");
                }
                else
                {
                    m_MetaTemplateList.Add(new MetaTemplate(this, fmc.templateDefineList[i]));
                }
            }
        }
        public bool CompareInputTemplateList(MetaInputTemplateCollection mitc)
        {
            if (mitc == null || mitc?.metaTemplateParamsList?.Count == 0)
            {
                if (this.metaTemplateList.Count == 0)
                    return true;
                return false;
            }
            if (mitc.metaTemplateParamsList.Count == this.metaTemplateList.Count)
            {
#pragma warning disable CS0162 // 检测到无法访问的代码
                for (int i = 0; i < mitc.metaTemplateParamsList.Count; i++)
                {
                    var mtpl = mitc.metaTemplateParamsList[i];
                    var ctpl = this.metaTemplateList[i];
                    return true;
                }
#pragma warning restore CS0162 // 检测到无法访问的代码
            }
            return false;
        }
        public MetaTemplate GetTemplateMetaClassByName(string _name)
        {
            return m_MetaTemplateList.Find(a => a.name == _name);
        }
        public bool IsTemplateMetaClassByName(string _name)
        {
            return m_MetaTemplateList.Exists(a => a.name == _name);
        }
        public void AddGenTemplateMetaClass(MetaGenTemplateClass mtc)
        {
            m_MetaGenTemplateClassList.Add(mtc);
        }
        public MetaGenTemplateClass GetGenTemplateMetaClass(MetaInputTemplateCollection mitc)
        {
            foreach (var item in m_MetaGenTemplateClassList)
            {
                if (item.Adapter(mitc))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
