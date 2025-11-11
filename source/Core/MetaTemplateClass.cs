//****************************************************************************
//  File:      MetaTemplateClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/12/17 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class ClassLevelRelationData
    {
        public class BindData
        {
            public MetaTemplate sourceTemplate;
            public MetaType targetMetaType;
        }
        public List<BindData> metaTemplateBindDataList = new List<BindData>();

        public void AddBindData(MetaTemplate  m1, MetaType m2 )
        {
            var find1 = metaTemplateBindDataList.Find( a => a.sourceTemplate == m1 );
            if( find1 == null )
            {
                metaTemplateBindDataList.Add( new BindData() { sourceTemplate = m1, targetMetaType = m2 });
            }
        }
        public MetaType GetSrouceTemplateByTargetTemplate( MetaTemplate m2 )
        {
            var find1 = metaTemplateBindDataList.Find(a => a.sourceTemplate == m2);
            if( find1 != null )
            {
                return find1.targetMetaType;
            }
            return null;
        }
    }
    public partial class MetaClass
    {
        public virtual bool isGenTemplate { get { return false; } }
        public List<MetaGenTemplateClass> metaGenTemplateClassList => m_MetaGenTemplateClassList;
        public Dictionary<MetaClass, ClassLevelRelationData> metaTemplateMapDict => m_MetaTemplateMapDict;
        public bool isTemplateClass { get { return m_MetaTemplateList.Count > 0; } }        //是否是模版类
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();
        protected List<MetaGenTemplateClass> m_MetaGenTemplateClassList = new List<MetaGenTemplateClass>();
        protected ClassLevelRelationData m_ClassLevelRelationData = null;
        private Dictionary<MetaClass, ClassLevelRelationData> m_MetaTemplateMapDict = new Dictionary<MetaClass, ClassLevelRelationData>();
        //protected Dictionary<MetaTemplate, List<MetaType>> m_TemplateBindMetaTypeDict = new Dictionary<MetaTemplate, List<MetaType>>();

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
        public MetaClass ParseFileMetaClassTemplate(FileMetaClass fmc)
        {
            if(fmc.templateDefineList.Count > 0)
            {
                for (int i = 0; i < fmc.templateDefineList.Count; i++)
                {
                    string tTemplateName = fmc.templateDefineList[i].name;
                    if ( m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 定义模式名称重复!!");
                    }
                    else
                    {
                        var mt = new MetaTemplate(this, fmc.templateDefineList[i], i );
                        m_MetaTemplateList.Add(mt);
                    }
                }
            }
            return this;
        }
        void HandleParentClassTemplateMapRelation()
        {
            m_ClassLevelRelationData = new ClassLevelRelationData();

            if (this.m_ExtendClassMetaType.defineTemplateMetaTypeList.Count > 0 
                && m_ExtendClass.metaTemplateList.Count == m_ExtendClassMetaType.defineTemplateMetaTypeList.Count )
            {
                for (int i = 0; i < this.m_ExtendClassMetaType.defineTemplateMetaTypeList.Count; i++)
                {
                    var mapMetaType = m_ExtendClassMetaType.defineTemplateMetaTypeList[i];
                    var parentClassTemplate = m_ExtendClass.metaTemplateList[i];
                    if (mapMetaType != null)
                    {
                        m_ClassLevelRelationData.AddBindData(parentClassTemplate, mapMetaType);
                    }
                }
                if (!m_MetaTemplateMapDict.ContainsKey(m_ExtendClass))
                {
                    this.m_MetaTemplateMapDict[m_ExtendClass] = m_ClassLevelRelationData;
                }
            }
        }
        public void HandleExtendClassTemplateMapRelation()
        {
            if (m_ExtendClassMetaType != null)
            {
                MetaClass extendMC = m_ExtendClass;
                MetaClass currentMC = extendMC;
                while (currentMC != null)
                {
                    var parentMc = currentMC.extendClass;

                    if(parentMc == null )
                    {
                        break;
                    }
                    if (parentMc == CoreMetaClassManager.objectMetaClass)
                    {
                        break;
                    }                   
                    if (!m_MetaTemplateMapDict.ContainsKey(parentMc))
                    {
                        var list = currentMC.m_MetaTemplateMapDict[parentMc].metaTemplateBindDataList;
                        ClassLevelRelationData clrd = new ClassLevelRelationData();
                        foreach (var v in list)
                        {
                            if (m_MetaTemplateMapDict.ContainsKey(currentMC) && currentMC.m_MetaTemplateMapDict.ContainsKey(parentMc ) )
                            {
                                var t1 = m_MetaTemplateMapDict[currentMC];
                                var t2 = currentMC.m_MetaTemplateMapDict[parentMc];

                                MetaType mtfind2 = t2.GetSrouceTemplateByTargetTemplate(v.sourceTemplate);
                                if (mtfind2 != null)
                                {
                                    MetaType copymt = new MetaType(mtfind2);
                                    ReplaceMetaTypeTemplateMeta(copymt, t1 );
                                    clrd.AddBindData(v.sourceTemplate, copymt);
                                }
                                else
                                {
                                    Log.AddInStructMeta(EError.None, "没有找到父级别自己模板生成时的数据!!");
                                }
                            }
                            else
                            {
                                Log.AddInStructMeta(EError.None, "没有找到父级别自己模板生成时的数据!!");
                            }
                        }
                        this.m_MetaTemplateMapDict[parentMc] = clrd;
                    }

                    currentMC = currentMC.extendClass;
                }
            }
        }
        public void ReplaceMetaTypeTemplateMeta( MetaType mt, ClassLevelRelationData clrd )
        {
            if (mt.genTemplateMetaTypeList.Count > 0)
            {
                for (int i = 0; i < mt.genTemplateMetaTypeList.Count; i++)
                {
                    ReplaceMetaTypeTemplateMeta(mt.genTemplateMetaTypeList[i], clrd);
                }
            }
            if (mt.isTemplate)
            {
                MetaType mtfind = clrd.GetSrouceTemplateByTargetTemplate(mt.metaTemplate);
                if (mtfind != null)
                {
                    mt.SetMetaType(mtfind);
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
        //public void AddBindMetaType(MetaTemplate metaTemp, MetaType mt )
        //{
        //    List<MetaType> mtList = new List<MetaType>();
        //    if(m_TemplateBindMetaTypeDict.ContainsKey(metaTemp) )
        //    {
        //        mtList = m_TemplateBindMetaTypeDict[metaTemp];
        //    }
        //    else
        //    {
        //        m_TemplateBindMetaTypeDict.Add(metaTemp, mtList);
        //    }

        //    var find1 = mtList.Find(a => a == mt);
        //    if( find1 == null )
        //    {
        //        mtList.Add(mt);
        //    }
        //}
        public MetaTemplate GetMetaTemplateByName(string _name)
        {
            return m_MetaTemplateList.Find(a => a.name == _name);
        }
        public MetaTemplate GetMetaTemplateByIndex( int index )
        {
            if( index < 0 || index >= m_MetaTemplateList.Count )
            {
                return null;
            }
            return m_MetaTemplateList[index];
        }
        public bool IsTemplateMetaClassByName(string _name)
        {
            return m_MetaTemplateList.Exists(a => a.name == _name);
        }
        public void AddGenTemplateMetaClass(MetaGenTemplateClass mtc)
        {
            //mtc.SetDeep(this.m_Deep + 1);
            m_MetaGenTemplateClassList.Add(mtc);
            ClassManager.instance.AddGenTemplateClass(mtc);
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
        public MetaGenTemplateClass AddInstanceMetaClass( List<MetaType> inputlist )
        {
            List<MetaClass> list = new List<MetaClass>();
            foreach (var item in inputlist )
            {
                if (item.isTemplate == false)
                {
                    list.Add(item.metaClass);
                }
            }
            return AddInstanceMetaClass(list);
        }
        public MetaGenTemplateClass AddInstanceMetaClass(List<MetaClass> list, bool isParse = false )
        {
            if(list.Count == 0)
            {
                return null;
            }
            if (this.m_MetaTemplateList.Count == list.Count)
            {
                List<MetaGenTemplate> list2 = new List<MetaGenTemplate>();
                for (int i = 0; i < this.metaTemplateList.Count; i++)
                {
                    if (list[i].isTemplateClass)
                    {
                        if (list[i] is not MetaGenTemplateClass )
                        {
                            return null;
                        }
                    }


                    var classTemplate = this.metaTemplateList[i];

                    MetaGenTemplate mgt = new MetaGenTemplate(classTemplate, new MetaType(list[i]));
                    list2.Add(mgt);
                }

                MetaGenTemplateClass tmc = GetGenTemplateMetaClassByTemplateList(list2);
                if (tmc == null)
                {
                    tmc = new MetaGenTemplateClass(this, list2);                    
                    this.AddGenTemplateMetaClass(tmc);
                    if (isParse)
                    {
                        tmc.ParseGenTemplateClass(tmc);
                    }
                }
                return tmc;
            }
            return null;
        }
        public virtual void ParseGenTemplateClassMetaType()
        {
            var list = new List<MetaGenTemplateClass>(m_MetaGenTemplateClassList);
            foreach ( var v in list )
            {
                v.ParseGenTemplateClass(v);
            }
        }
        public MetaGenTemplateClass GetGenTemplateMetaClassByTemplateList(List<MetaGenTemplate> list)
        {
            foreach (var v in m_MetaGenTemplateClassList)
            {
                if (v.IsMatchByMetaTemplateClass(list))
                {
                    return v;
                }
            }
            return null;
        }
        //public MetaGenTemplateClass GetGenTemplateMetaClassIfNotThenGenTemplateClass(MetaInputTemplateCollection mtic)
        //{
        //    MetaGenTemplateClass mtc = GetGenTemplateMetaClass(mtic);
        //    if (mtc == null)
        //    {
        //        mtc = MetaGenTemplateClass.GenerateTemplateClass(this, mtic);
        //    }
        //    if (mtc == null)
        //    {
        //        Log.AddInStructMeta(EError.None, "Error 没有找到合适的Template");
        //    }
        //    return mtc;
        //}
    }
}
