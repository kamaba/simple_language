//****************************************************************************
//  File:      ClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta Class's manager center 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Linq;
using SimpleLanguage.Project;
using System;

namespace SimpleLanguage.Core
{
    public class ClassManager
    {
        public static ClassManager s_Instance = null;
        public static ClassManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ClassManager();
                }
                return s_Instance;
            }
        }
        public List<MetaGenTemplateClass> genTemplateMetaClassList => m_GenTemplateMetaClassList;
        public List<MetaClass> exportMetaClassList => m_ExportMetaClassList;
        public List<MetaData> exportMetaDataList => m_ExportMetaDataList;
        public List<MetaEnum> exportMetaEnumList => m_ExportMetaEnumList;


        private readonly List<MetaClass> m_ExportMetaClassList = new List<MetaClass>();
        private readonly List<MetaData> m_ExportMetaDataList = new List<MetaData>();
        private readonly List<MetaEnum> m_ExportMetaEnumList = new List<MetaEnum>();
        private Dictionary<string, MetaClass> m_AllClassDict = new Dictionary<string, MetaClass>();
        //private List<MetaDynamicClass> m_DynamicClassList = new List<MetaDynamicClass>();         
        private Dictionary<string, MetaData> m_DefineDataDict = new Dictionary<string, MetaData>();
        private Dictionary<string, MetaData> m_AnonymousDataDict = new Dictionary<string, MetaData>();

        private List<MetaGenTemplateClass> m_GenTemplateMetaClassList = new List<MetaGenTemplateClass>();
        //private List<MetaGenTemplateClass> m_NeedHandleTemplateMetaClassList = new List<MetaGenTemplateClass>();
        private List<MetaClass> m_InitHandleMetaClassList = new List<MetaClass>();
        private List<MetaEnum> m_InitHandleMetaEnumList = new List<MetaEnum>();
        private List<MetaData> m_InitHandleMetaDataList = new List<MetaData>();

        public MetaClass GetClassByName(string name, int templateCount = 0 )
        {
            string nname = name + "_" + templateCount;
            if (m_AllClassDict.ContainsKey(nname))
                return m_AllClassDict[nname];
            return null;
        }

        /// <summary>
        /// ???? <c>Project</c> ??????????? <see cref="MetaNode.GetAllName"/> ????? <c>S.Core.Project</c> ????
        /// ????? <c>GetClassByName(&quot;Core.Project&quot;, 0)</c> ???????
        /// </summary>
        public MetaClass TryGetProjectMetaClass()
        {
            var mc = GetClassByName("S.Project", 0)
                ?? GetClassByName("Core.Project", 0)
                ?? GetClassByName("Project", 0);
            if (mc != null)
            {
                return mc;
            }
            return FindFirstMetaClassByShortName("Project", 0);
        }

        MetaClass FindFirstMetaClassByShortName(string shortName, int templateCount)
        {
            if (string.IsNullOrEmpty(shortName))
            {
                return null;
            }
            foreach (var kv in m_AllClassDict)
            {
                var c = kv.Value;
                if (c == null) continue;
                if (c.metaTemplateList.Count != templateCount) continue;
                if (string.Equals(c.name, shortName, StringComparison.Ordinal))
                {
                    return c;
                }
            }
            return null;
        }
        public bool AddMetaClass( MetaClass mc, MetaModule mm = null )
        {
            MetaNode topLevelNamespace = mm?.metaNode;
            if (topLevelNamespace == null)
            {
                topLevelNamespace = ModuleManager.instance.selfModule.metaNode;
            }
            topLevelNamespace.AddMetaClass(mc);
            return true;
        }
        public bool AddMetaData(MetaData md, MetaModule mm = null)
        {
            MetaNode topLevelNamespace = mm?.metaNode;
            if (topLevelNamespace == null)
            {
                topLevelNamespace = ModuleManager.instance.selfModule.metaNode;
            }
            topLevelNamespace.AddMetaData(md);
            return true;
        }
        public void AddGenTemplateClass(MetaGenTemplateClass mc)
        {
            if( !m_GenTemplateMetaClassList.Contains(mc ) )
            {
                m_GenTemplateMetaClassList.Add(mc);
            }
            if( !m_AllClassDict.ContainsKey(mc.allName) )
            {
                m_AllClassDict.Add(mc.allName, mc);
            }
        }
        //public MetaDynamicClass FindDynamicClass( MetaClass dc )
        //{
        //    foreach( var v in m_DynamicClassList )
        //    {
        //        if( CompareMetaClassMemberVariable( dc, v ) )
        //        {
        //            return v;
        //        }
        //    }
        //    return null;
        //}
        //public bool AddDynamicClass(MetaDynamicClass dc )
        //{
        //    m_DynamicClassList.Add(dc);

        //    m_AllClassDict.Add(dc.allClassName, dc);
        //    return true;
        //}
        //public void AddMetaGenTemplateClassList(MetaGenTemplateClass mc)
        //{
        //    if (m_GenTemplateMetaClassList.IndexOf(mc) == -1)
        //    {
        //        m_GenTemplateMetaClassList.Add(mc);
        //    }
        //}
        //public void AddNeedHandleTemplateMetaClassList(MetaGenTemplateClass mc)
        //{
        //    if (m_NeedHandleTemplateMetaClassList.IndexOf(mc) == -1)
        //    {
        //        m_NeedHandleTemplateMetaClassList.Add(mc);
        //    }

        //}
        //public bool IsMetaGenTemplateClass(MetaGenTemplateClass mc)
        //{
        //    if (m_GenTemplateMetaClassList.IndexOf(mc) != -1)
        //        return true;

        //    return false;
        //}
        public void AddInitHandleMetaClassList(MetaClass mc)
        {
            if (m_InitHandleMetaClassList.IndexOf(mc) == -1)
            {
                m_InitHandleMetaClassList.Add(mc);
            }
        }
        public void AddInitHandleMetaDataList(MetaData md)
        {
            if (md != null && m_InitHandleMetaDataList.IndexOf(md) == -1)
            {
                m_InitHandleMetaDataList.Add(md);
                AddExportMetaData(md);
            }
        }
        public void AddInitHandleMetaEnumList(MetaEnum me)
        {
            if (me != null && m_InitHandleMetaEnumList.IndexOf(me) == -1)
            {
                m_InitHandleMetaEnumList.Add(me);
                AddExportMetaEnum(me);
            }
        }
        /// <summary>
        /// ????????? data ??????????????????????????????????data ?????????????????????????
        /// </summary>
        public MetaData FindMetaDataByNameAndFormat( MetaData md )
        {
            var findmd = FindDeclareMetaData(md);
            if (findmd != null) return findmd;
            return FindAnonymousMetaData(md);
        }
        public MetaData FindDeclareMetaData(MetaData md)
        {
            if (md == null)
            {
                return null;
            }
            foreach (var v in m_DefineDataDict)
            {
                if (CompareMetaDataMember(v.Value, md))
                {
                    return v.Value;
                }
            }
            return null;
        }
        public MetaData FindAnonymousMetaData(MetaData md)
        {
            if (md == null)
            {
                return null;
            }
            foreach (var v in m_AnonymousDataDict)
            {
                if(CompareMetaDataMember( v.Value, md ) )
                {
                    return v.Value;
                }
            }
            return null;
        }
        public MetaData FindMetaDataByName( string name )
        {
            if (m_DefineDataDict.ContainsKey(name))
            {
                return m_DefineDataDict[name];
            }
            if (m_AnonymousDataDict.ContainsKey(name))
            {
                return m_AnonymousDataDict[name];
            }
            return null;
        }

        /// <summary>
        /// ?????????? data??define ????????
        /// </summary>
        public IEnumerable<MetaData> EnumerateDefineMetaData()
        {
            foreach (var kv in m_DefineDataDict)
            {
                yield return kv.Value;
            }
        }
        public bool AddDefineMetaData(MetaData dc)
        {
            if (dc == null)
            {
                return false;
            }
            if (m_DefineDataDict.ContainsKey(dc.name))
            {
                return false;
            }
            m_DefineDataDict.Add(dc.name, dc);
            m_ExportMetaDataList.Add(dc);
            return true;
        }
        public bool AddAnonymousMetaData(MetaData dc)
        {
            if (dc == null)
            {
                return false;
            }
            if (m_AnonymousDataDict.ContainsKey(dc.name))
            {
                return false;
            }
            m_AnonymousDataDict.Add(dc.name, dc);
            m_ExportMetaDataList.Add(dc);
            return true;
        }
        public bool CompareMetaClassMemberVariable(MetaClass curClass, MetaClass cpClass)
        {
            var curClassList = curClass.allMetaMemberVariableList;
            var cpClassList = cpClass.allMetaMemberVariableList;

            if (curClassList.Count == cpClassList.Count)
            {
                for (int i = 0; i < curClassList.Count; i++)
                {
                    var curMV = curClassList[i];
                    var cpMV = cpClassList[i];
                    if (curMV.isConst == cpMV.isConst
                        || curMV.isStatic == cpMV.isStatic
                        || curMV.name == cpMV.name
                        || curMV.defineMetaType == cpMV.defineMetaType)
                    {

                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// ???????? MetaData ??????????????/?????????????????????????????????
        /// ??????????????????????????????????????????????????????????????????????????????? MetaData ??????????????
        /// </summary>
        public bool CompareMetaDataMember(MetaData curClass, MetaData cpClass)
        {
            if (ReferenceEquals(curClass, cpClass))
            {
                return true;
            }
            if (curClass == null || cpClass == null)
            {
                return false;
            }

            var curClassList = curClass.metaMemberDataDict;
            var cpClassList = cpClass.metaMemberDataDict;

            if (curClassList.Count != cpClassList.Count)
            {
                return false;
            }

            // ?????????????? data ????????????????????????????????? class ????
            if (curClassList.Count == 0 && cpClassList.Count == 0)
            {
                return false;
            }

            // ?????????? data ????????????????????????? + ?????????????????????????????
            if (curClass.isDynamic && cpClass.isDynamic)
            {
                return CompareDynamicAnonymousMetaDataShape(curClass, cpClass);
            }

            foreach( var v in curClassList )
            {
                if( !cpClassList.ContainsKey(v.Key ) )
                {
                    return false;
                }
                var vval = v.Value;
                var val2 = cpClassList[v.Key];
                if (vval.defineMetaType == null || val2.defineMetaType == null)
                {
                    return false;
                }
                if( vval.defineMetaType.metaClass != val2.defineMetaType.metaClass )
                {
                    return false;
                }
            }

            return true;
        }
        private static List<MetaMemberData> OrderMetaMemberDataList(MetaData md)
        {
            return md.GetMetaMemberDataList()
                .OrderBy(m => m.dataFieldOrderIndex)
                .ThenBy(m => m.name, System.StringComparer.Ordinal)
                .ToList();
        }
        private static MetaType GetStructuralMetaTypeForCompare(MetaMemberData mmd)
        {
            if (mmd.isDefineMetaType && mmd.defineMetaType != null)
            {
                return mmd.defineMetaType;
            }
            if (mmd.realMetaType != null)
            {
                return mmd.realMetaType;
            }
            if (mmd.defineMetaType != null)
            {
                return mmd.defineMetaType;
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        private static bool FieldMetaTypesShapeEqual(MetaType ta, MetaType tb)
        {
            if (ta == null || tb == null)
            {
                return ta == tb;
            }
            var mdA = ta.metaData;
            var mdB = tb.metaData;
            if (mdA != null && mdB != null
                && mdA.isDynamic && mdB.isDynamic)
            {
                return CompareDynamicAnonymousMetaDataShape(mdA, mdB);
            }
            return TypeManager.CompareMetaType(ta, tb);
        }

        private static bool CompareDynamicAnonymousMetaDataShape(MetaData a, MetaData b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (a == null || b == null || !a.isDynamic || !b.isDynamic)
            {
                return false;
            }

            var listA = OrderMetaMemberDataList(a);
            var listB = OrderMetaMemberDataList(b);
            if (listA.Count != listB.Count)
            {
                return false;
            }

            for (int i = 0; i < listA.Count; i++)
            {
                var ma = listA[i];
                var mb = listB[i];
                if (ma.name != mb.name)
                {
                    return false;
                }
                var ta = GetStructuralMetaTypeForCompare(ma);
                var tb = GetStructuralMetaTypeForCompare(mb);
                if (!FieldMetaTypesShapeEqual(ta, tb))
                {
                    return false;
                }
            }

            return true;
        }
        public MetaClass FindDynamicClassByMetaType( MetaClass dc )
        {
            return null;
        }
        public MetaBase AddClass( FileMetaClass fmc )
        {
            bool isCanAddBind = false;
            Token token = fmc.token;
            MetaNode finalTopMetaNode = ModuleManager.instance.selfModule.metaNode;
            if( ProjectManager.config?.Project?.Name == "Core" )
            {                
                finalTopMetaNode = ModuleManager.instance.coreModule.metaNode;
            }
            FileMetaClass topLevelClass = fmc.topLevelFileMetaClass;
            if ( topLevelClass != null )
            {                
                if( topLevelClass?.metaClass?.metaNode == null )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, " ??????????????????????????");
                    return null;
                }

                var findmc = topLevelClass.metaClass.metaNode.GetChildrenMetaNodeByName( fmc.name );
                if (findmc != null)
                {
                    if(findmc.isMetaNamespace || findmc.isMetaData || findmc.isMetaEnum )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Namespace/data/enum node already exists, duplicate class node is not allowed.");
                        return null;
                    }

                    MetaClass findmc2 = findmc.GetMetaClassByTemplateCount(fmc.templateDefineList.Count);
                    if ( findmc2 != null )
                    {
                        if( findmc2.classDefineType == EClassDefineType.StructDefine )
                        {
                            findmc2.BindFileMetaClass(fmc);
                            findmc2.ParseFileMetaClassTemplate(fmc);
                            findmc2.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            findmc2.SetClassDefineType(EClassDefineType.CodeDefine);
                            findmc2.UpdateClassAllName();
                            AddExportMetaClass(findmc2);
                            return findmc2;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Found existing class node with incompatible define type.");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Found existing class node with incompatible define type.");
                        return null;
                    }
                }
                else
                {
                    finalTopMetaNode = topLevelClass.metaClass.metaNode;
                    isCanAddBind = true;
                }
            }
            else
            {
                if(fmc.topLevelFileMetaNamespace != null )
                {
                    finalTopMetaNode = ModuleManager.instance.GetChildrenMetaNodeByName(fmc.topLevelFileMetaNamespace.name);

                    if(finalTopMetaNode == null )
                    {
                        finalTopMetaNode = NamespaceManager.instance.SearchFinalNamespace(fmc.topLevelFileMetaNamespace);

                        if (fmc.namespaceBlock?.namespaceList.Count > 0)
                        {
                            finalTopMetaNode = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock, finalTopMetaNode);
                        }
                    }
                }

                if (finalTopMetaNode == null && fmc.namespaceBlock?.namespaceList?.Count > 0 )
                {
                    finalTopMetaNode = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock);
                   
                    if (finalTopMetaNode == null )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "???????????????????????????????????????!!");
                        return null;
                    }
                }
                if( finalTopMetaNode.isMetaModule && fmc.namespaceBlock?.namespaceList?.Count > 0)
                {
                    finalTopMetaNode = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock, finalTopMetaNode );
                }
                if( finalTopMetaNode == null )
                {
                    return null;
                }
                if(finalTopMetaNode.isMetaModule ||finalTopMetaNode.isMetaNamespace )
                {
                    var findamc = finalTopMetaNode.GetChildrenMetaNodeByName(fmc.name);
                    if (findamc != null && findamc.IsMetaClass() )
                    {
                        MetaClass ffmc = findamc.GetMetaClassByTemplateCount(fmc.templateDefineList.Count);
                        if( ffmc != null )
                        {
                            if (ProjectManager.useDefineNamespaceType == EUseDefineType.LimitUseProjectConfigNamespaceAndClass)
                            {

                            }
                            fmc.SetMetaClass(ffmc);
                            ffmc.BindFileMetaClass(fmc);
                            ffmc.SetClassDefineType(EClassDefineType.CodeDefine);
                            ffmc.metaTemplateList.Clear();
                            ffmc.ParseFileMetaClassTemplate(fmc);
                            ffmc.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            ffmc.UpdateClassAllName();
                            AddInitHandleMetaClassList(ffmc);
                            AddExportMetaClass(ffmc);
                            return ffmc;
                        }
                        else
                        {
                            isCanAddBind = true;
                        }
                        //if (!fmc.isPartial)
                        //{
                        //    Log.AddMetaCoreLog(LID.ShowExtendMessage, "??" + fmc.name + "?? " + fmc.token.ToAllString() + "?????????????????????????????");
                        //    return null;
                        //}
                        //bool isPartial = true;
                        //foreach (var v in ffmc.fileMetaClassDict)
                        //{
                        //    if (v.Value.isPartial == false)
                        //    {
                        //        isPartial = false;
                        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "??" + findamc.name + "?? " + v.Value.token.ToAllString() + "?????????????????????????????");
                        //        break;
                        //    }
                        //}
                        //if (isPartial == false)
                        //{
                        //    return null;
                        //}
                        //ffmc.BindFileMetaClass(fmc);
                        //return ffmc;
                    }
                    else
                    {
                        isCanAddBind = true;
                    }
                }
                else if ( finalTopMetaNode.IsMetaClass() )
                {
                    var findamc = finalTopMetaNode.GetMetaClassByTemplateCount(fmc.templateDefineList.Count);
                    if ( findamc == null )
                    {
                        isCanAddBind = true;
                    }
                    else
                    {
                        if (ProjectManager.useDefineNamespaceType == EUseDefineType.LimitUseProjectConfigNamespaceAndClass)
                        {
                            fmc.SetMetaClass(findamc);
                            findamc.BindFileMetaClass(fmc);
                            findamc.SetClassDefineType(EClassDefineType.CodeDefine);
                            findamc.ParseFileMetaClassTemplate(fmc);
                            findamc.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            findamc.UpdateClassAllName();
                            AddExportMetaClass(findamc);
                            return findamc;
                        }
                        if (!fmc.isPartial)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Class " + fmc.name + " at " + fmc.token.ToAllString() + " does not support parallel file definitions.");
                            return null;
                        }
                        bool isPartial = true;
                        foreach (var v in findamc.fileMetaClassDict)
                        {
                            if (v.Value.isPartial == false)
                            {
                                isPartial = false;
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Class " + findamc.name + " at " + v.Value.token.ToAllString() + " does not support parallel file definitions.");
                                break;
                            }
                        }
                        if (isPartial == false)
                        {
                            return null;
                        }
                        findamc.BindFileMetaClass(fmc);
                        findamc.UpdateClassAllName();
                        AddExportMetaClass(findamc);
                        return findamc;
                    }                    
                }
                else
                {
                    isCanAddBind = true;
                }
            }

            if( isCanAddBind )
            {
                if (ProjectManager.useDefineNamespaceType == EUseDefineType.LimitUseProjectConfigNamespaceAndClass)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, " ");
                }
                if (fmc.isEnum)
                {
                    MetaEnum newme = new MetaEnum(fmc.name);
                    finalTopMetaNode.AddMetaEnum(newme);
                    newme.UpdateAllName();
                    fmc.SetMetaEnum(newme);
                    newme.SetClassDefineType(EClassDefineType.CodeDefine);
                    newme.ParseFileMetaEnumMemeberEnum(fmc);
                    
                    AddInitHandleMetaEnumList(newme);

                    return newme;
                }
                else if (fmc.isData)
                {
                    var newmd = new MetaData(fmc);
                    newmd.SetClassDefineType(EClassDefineType.CodeDefine);
                    finalTopMetaNode.AddMetaData(newmd);
                    newmd.UpdateAllName();
                    newmd.ParseFileMetaDataMemeberData(fmc);
                    AddInitHandleMetaDataList(newmd);

                    return newmd;
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Class ??????? const ????");
                        return null;
                    }
                    var newmc = new MetaClass(fmc.name);
                    newmc.BindFileMetaClass(fmc);
                    newmc.SetClassDefineType(EClassDefineType.CodeDefine);
                    newmc.ParseFileMetaClassTemplate(fmc);
                    finalTopMetaNode.AddMetaClass(newmc);
                    newmc.UpdateClassAllName();
                    newmc.ParseFileMetaClassMemeberVarAndFunc(fmc);

                    AddInitHandleMetaClassList(newmc);
                    AddExportMetaClass(newmc);

                    return newmc;
                }
            }
            else
            {
                return null;
            }
        }
        public void AddExportMetaClass( MetaClass mc )
        {
            var find1 = m_ExportMetaClassList.Find(a => a == mc);
            if( find1  == null )
            {
                m_ExportMetaClassList.Add(mc);

                AddDictMetaClass(mc);
            }
        }
        public void AddExportMetaData(MetaData md)
        {
            if (md == null || m_ExportMetaDataList.Contains(md))
            {
                return;
            }
            m_ExportMetaDataList.Add(md);
        }

        void AddExportMetaEnum(MetaEnum me)
        {
            if (me == null || m_ExportMetaEnumList.Contains(me))
            {
                return;
            }
            m_ExportMetaEnumList.Add(me);
        }
        void AddDictMetaClass( MetaClass mc )
        {
            string acn = mc.allName + "_" + mc.metaTemplateList.Count;
            foreach( var v in m_AllClassDict )
            {
                if( v.Value == mc )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"???????????:{mc.allName} ????????????????????!");
                    return;
                }
            }
            m_AllClassDict.Add(acn, mc);

        }
        /// <summary>
        /// ????????extends/implements????????????????????????????????????????????????????????????????????
        /// </summary>
        public void ParseInitMetaClassListThroughInheritance()
        {
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseMetaTemplateInConstraint();
            }
            TypeManager.instance.EnsureBuiltinGlobalTypeAliases();

            foreach ( var it in m_InitHandleMetaClassList )
            {
                it.ParseExtendsRelation();
                it.ParseInterfaceRelation();
            }
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.CalcExtendLevel();
            }
            m_InitHandleMetaClassList.Sort((x, y) => x.extendLevel - y.extendLevel);
            foreach ( var it in m_InitHandleMetaEnumList )
            {
                it.ParseExtendsRelation();
            }
        }

        /// <summary>??? typealias ??????????????????????????????????/???????????????????????????????????????????</summary>
        public void ParseInitMetaListCollectMemberDefineMetaTypes()
        {
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseFileCollectMemberVariableDefineMetaType();
                it.ParseFileCollectMemberFunctionDefineMetaType();
                it.HandleExtendAndInterfaceMetaTypeInstnace();
                it.EnsureParsedGenTemplateMetaClasses();
            }
            EnsureAllGenTemplateClassesParsed();
        }

        public void EnsureAllGenTemplateClassesParsed()
        {
            var list = new List<MetaGenTemplateClass>(m_GenTemplateMetaClassList);
            foreach (var mgtc in list)
            {
                if (mgtc == null)
                {
                    continue;
                }
                mgtc.ParseGenTemplateClass(mgtc);
                mgtc.ParseGenMemberVarible();
            }
        }
        public void UpdateMetaGenTemplateClassHandle()
        {
            var list = new List<MetaGenTemplateClass>(m_GenTemplateMetaClassList);
            foreach( var v in list)
            {
                v.UpdateRegsterGenMetaClass();
            }
        }
        //public void ParseDefineMetaTypeGenTemplateMetaClassList()
        //{
        //    var list = new List<MetaGenTemplateClass>(m_NeedHandleTemplateMetaClassList);
        //    m_NeedHandleTemplateMetaClassList.Clear();
        //    foreach (var it in list)
        //    {
        //        it.ParseMemberVariableDefineMetaType();
        //        it.ParseMemberFunctionDefineMetaType();
        //        AddMetaGenTemplateClassList(it);
        //    }
        //}
        //public void ParseGenTemplateMetaClassList()
        //{
        //    if (m_NeedHandleTemplateMetaClassList.Count == 0) return;

        //    var list = new List<MetaGenTemplateClass>(m_NeedHandleTemplateMetaClassList);
        //    m_NeedHandleTemplateMetaClassList.Clear();
        //    foreach (var it in list)
        //    {
        //        it.Parse();
        //        AddMetaGenTemplateClassList(it);
        //    }
        //}
        public void CheckInterfaces()
        {
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.CheckInterface();
            }
        }
        public void ParseDefineComplete()
        {
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.ParseDefineComplete();
            }
            foreach (var md in m_InitHandleMetaDataList)
            {
                md.ParseDefineComplete();
            }
            foreach (var md in m_InitHandleMetaEnumList)
            {
                md.ParseDefineComplete();
            }
        }
        //public static EClassRelation ValidateClassRelation( string curName, string compareName )
        //{
        //    MetaClass currentClass = instance.GetClassByName(curName);
        //    if (currentClass == null)
        //    {
        //        return EClassRelation.CurClassError;
        //    }
        //    MetaClass compareClass = instance.GetClassByName(compareName);
        //    if (compareClass == null)
        //    {
        //        return EClassRelation.CompareClassError;
        //    }
        //    return ValidateClassRelationByMetaClass(currentClass, compareClass);
        //}
        public static bool IsNumberClass( MetaClass curClass )
        {
            if (curClass == null)
            {
                return false;
            }

            if ( curClass == CoreMetaClassManager.numMetaClass
                || curClass == CoreMetaClassManager.uint8MetaClass
                || curClass == CoreMetaClassManager.int8MetaClass
                || curClass == CoreMetaClassManager.int16MetaClass
                || curClass == CoreMetaClassManager.uint16MetaClass
                || curClass == CoreMetaClassManager.int32MetaClass
                || curClass == CoreMetaClassManager.uint32MetaClass
                || curClass == CoreMetaClassManager.int64MetaClass
                || curClass == CoreMetaClassManager.uint64MetaClass
                || curClass == CoreMetaClassManager.float32MetaClass
                || curClass == CoreMetaClassManager.float64MetaClass )
            {
                return true;
            }

            // ?????????????????????????????????????????????????????Num????????????????????????????
            if (curClass.IsParseMetaClass(CoreMetaClassManager.numMetaClass))
            {
                return true;
            }

            return false;
        }

        /// <summary> ????????????? Array&lt;T&gt;???Iterator&lt;T&gt; ?????????????????? </summary>
        public static MetaType GetSingleTemplateArgMetaType(MetaType mt)
        {
            if (mt == null) return null;
            var gen = mt.GetGenTemplateMetaTypeList();
            if (gen != null && gen.Count == 1)
                return gen[0];
            if (mt.defineTemplateMetaTypeList != null && mt.defineTemplateMetaTypeList.Count == 1)
                return mt.defineTemplateMetaTypeList[0];
            return mt.GetMetaTypeByIndex(0);
        }

        public static bool IsAbstractNumberMetaType(MetaType mt)
        {
            return mt != null && mt.metaClass == CoreMetaClassManager.numMetaClass;
        }

        /// <summary> ?????????????????Int32???Float32 ???????????? Num ??????? </summary>
        public static bool IsConcreteNumericElementType(MetaType elem)
        {
            if (elem?.metaClass == null) return false;
            if (elem.metaClass == CoreMetaClassManager.numMetaClass) return false;
            return IsNumberClass(elem.metaClass);
        }

        /// <summary>
        /// ?? Int8???Float64 ??????????????????????????????????????????? false???
        /// </summary>
        public static bool TryGetCorePrimitiveScalarStorage(MetaClass mc, out int widthBytes, out bool isFloat)
        {
            widthBytes = 0;
            isFloat = false;
            if (mc == null) return false;
            if (mc == CoreMetaClassManager.int8MetaClass || mc == CoreMetaClassManager.uint8MetaClass)
            {
                widthBytes = 1;
                return true;
            }
            if (mc == CoreMetaClassManager.int16MetaClass || mc == CoreMetaClassManager.uint16MetaClass)
            {
                widthBytes = 2;
                return true;
            }
            if (mc == CoreMetaClassManager.int32MetaClass || mc == CoreMetaClassManager.uint32MetaClass)
            {
                widthBytes = 4;
                return true;
            }
            if (mc == CoreMetaClassManager.float32MetaClass)
            {
                widthBytes = 4;
                isFloat = true;
                return true;
            }
            if (mc == CoreMetaClassManager.int64MetaClass || mc == CoreMetaClassManager.uint64MetaClass)
            {
                widthBytes = 8;
                return true;
            }
            if (mc == CoreMetaClassManager.float64MetaClass)
            {
                widthBytes = 8;
                isFloat = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// ?????????????????????????? <see cref="ValidateClassRelationByMetaClass"/> ?????? <see cref="EClassRelation.Num"/> ?????
        /// ?????????????????????????????????????????????????????????? Int8???UInt32???Float32???Float64????
        /// ?????????????? Int32 ?? UInt32????? int ?? float ??????? false???????????????????? true????????? Num ??????????
        /// </summary>
        public static bool IsNarrowerCorePrimitiveWideningOkForCallSite(MetaClass argClass, MetaClass paramClass)
        {
            if (!TryGetCorePrimitiveScalarStorage(argClass, out int aw, out bool af))
                return true;
            if (!TryGetCorePrimitiveScalarStorage(paramClass, out int pw, out bool pf))
                return true;
            if (af != pf)
                return false;
            return aw < pw;
        }

        /// <summary> Iterator&lt;Number&gt; &lt;- ??????????????????????? Array?????????????????????????? </summary>
        public static bool TryIteratorNumberFromConcreteNumericArray(MetaType targetIterator, MetaType exprArray)
        {
            if (targetIterator == null || exprArray == null) return false;
            if (!targetIterator.IsIterator() || !exprArray.IsArray()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetIterator);
            var eArg = GetSingleTemplateArgMetaType(exprArray);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary> Iterator&lt;Number&gt; &lt;- Iterator&lt;???????????&gt;????????????????? Number ?????????? </summary>
        public static bool TryIteratorNumberFromConcreteNumericIterator(MetaType targetIterator, MetaType exprIterator)
        {
            if (targetIterator == null || exprIterator == null) return false;
            if (!targetIterator.IsIterator() || !exprIterator.IsIterator()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetIterator);
            var eArg = GetSingleTemplateArgMetaType(exprIterator);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary>
        /// Iterator&lt;Number&gt; <- arr.iterator ?????????????
        /// ????????????????iterator ????????????????????????????????????????????????
        /// ???????????????????????????????? Array ??????????????? Number ??????
        /// </summary>
        public static bool TryIteratorNumberFromArrayIteratorSource(MetaType targetIterator, MetaExpressNodeBase expressNode)
        {
            if (targetIterator == null || expressNode == null) return false;
            if (!targetIterator.IsIterator()) return false;

            var mcle = expressNode as MetaCallLinkExpressNode;
            var list = mcle?.metaCallLink?.callNodeList;
            if (list == null || list.Count == 0) return false;

            var sourceVar = list[0]?.metaVariable;
            var sourceArrayMt = sourceVar?.GetFinalMetaType();
            return TryIteratorNumberFromConcreteNumericArray(targetIterator, sourceArrayMt);
        }

        /// <summary>
        /// IIterable&lt;TTarget&gt; &lt;- Array&lt;TExpr&gt;??????????????????????????????????????????????????????????????
        /// ??????????????IIterable&lt;Object&gt; &lt;- Int32[]???
        /// </summary>
        public static bool TryIterableFromArrayElementAssignable(MetaType targetIterable, MetaType exprArray)
        {
            if (targetIterable == null || exprArray == null) return false;
            if (!targetIterable.IsIterable() || !exprArray.IsArray()) return false;

            var tArg = GetSingleTemplateArgMetaType(targetIterable);
            var eArg = GetSingleTemplateArgMetaType(exprArray);
            if (tArg == null || eArg == null) return false;
            if (TypeManager.CompareMetaType(tArg, eArg)) return true;

            var tClass = tArg.GetTemplateMetaClass();
            var eClass = eArg.GetTemplateMetaClass();
            if (tClass == null || eClass == null) return false;

            var relation = ValidateClassTypeRelation(tClass, eClass);
            return relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Interface
                || relation == ETypeRelation.Num;
        }

        /// <summary> const Array&lt;Number&gt; &lt;- Array&lt;???????????&gt;???? const ??????????????????????? </summary>
        public static bool TryConstArrayNumberFromConcreteNumericArray(MetaType targetArray, MetaType exprArray, MetaVariable targetVar)
        {
            if (targetVar == null || !targetVar.isConst) return false;
            if (targetArray == null || exprArray == null) return false;
            if (!targetArray.IsArray() || !exprArray.IsArray()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetArray);
            var eArg = GetSingleTemplateArgMetaType(exprArray);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary> Iterator&lt;Num&gt; ??? const Array&lt;Num&gt; ????????????? Array ?????????????????????/??????????????????? </summary>
        public static bool TryNumberArrayCovarianceAllow(MetaType target, MetaType expr, MetaVariable targetVar)
        {
            if (target == null || expr == null) return false;
            if (TryIteratorNumberFromConcreteNumericArray(target, expr)) return true;
            if (TryConstArrayNumberFromConcreteNumericArray(target, expr, targetVar)) return true;
            return false;
        }

        /// <summary>
        /// Array ?????????????????? Array ?????????????????????????? <see cref="TypeManager.CompareMetaType"/> ???????
        /// ???????????? Array ?????????????????????????????????<see cref="EClassRelation.Interface"/>?????
        /// </summary>
        public static bool TryArrayElementInterfaceAssignable(MetaType targetArray, MetaType exprArray)
        {
            if (targetArray == null || exprArray == null) return false;
            if (!targetArray.IsArray() || !exprArray.IsArray()) return false;
            if (targetArray.GetTemplateMetaClass() != exprArray.GetTemplateMetaClass()) return false;
            var tl = targetArray.GetGenTemplateMetaTypeList();
            var tr = exprArray.GetGenTemplateMetaTypeList();
            if (tl == null || tr == null || tl.Count != tr.Count || tl.Count == 0) return false;
            for (int i = 0; i < tl.Count; i++)
            {
                var tArg = tl[i];
                var eArg = tr[i];
                if (TypeManager.CompareMetaType(tArg, eArg)) continue;
                if (tArg.IsArray() && eArg.IsArray())
                {
                    if (TryArrayElementInterfaceAssignable(tArg, eArg)) continue;
                    return false;
                }
                MetaClass cur = tArg.GetTemplateMetaClass();
                MetaClass cmp = eArg.GetTemplateMetaClass();
                if (cur == null || cmp == null) return false;
                if (ValidateClassTypeRelation(cur, cmp) == ETypeRelation.Interface) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// ??? <see cref="TypeManager.CompareMetaType"/> ?? false ??????????????????????????????????????? Array ????????????
        /// </summary>
        public static bool TryMetaTypeAssignableByInterfaceAfterCompareFails(MetaType target, MetaType expr)
        {
            if (target == null || expr == null) return false;
            if (target.IsArray() && expr.IsArray()
                && target.GetTemplateMetaClass() == expr.GetTemplateMetaClass())
                return TryArrayElementInterfaceAssignable(target, expr);
            MetaClass cur = target.GetTemplateMetaClass();
            MetaClass cmp = expr.GetTemplateMetaClass();
            if (cur == null || cmp == null) return false;
            return ValidateClassTypeRelation(cur, cmp) == ETypeRelation.Interface;
        }

        public static ETypeRelation ValidateClassTypeRelation(MetaClass curClass, MetaClass compareClass)
        {
            if (compareClass == CoreMetaClassManager.nullMetaClass)
            {
                if (IsNumberClass(curClass) || curClass == CoreMetaClassManager.booleanMetaClass)
                    return ETypeRelation.No;

                if (curClass == CoreMetaClassManager.objectMetaClass)
                    return ETypeRelation.Same;
                return ETypeRelation.Parent;
            }
            if ( curClass == CoreMetaClassManager.objectMetaClass )
            {
                if (curClass == compareClass)
                    return ETypeRelation.Same;
                return ETypeRelation.Child;
            }
            if (curClass.Equals(compareClass))
            {
                return ETypeRelation.Same;
            }

            if( curClass == CoreMetaClassManager.numMetaClass )
            {
                if (IsNumberClass(compareClass))
                    return ETypeRelation.Num;
                return ETypeRelation.No;
            }
            if(IsNumberClass(curClass) && IsNumberClass(compareClass ) )
            {
                return ETypeRelation.Num;
            }

            if(compareClass.IsInterfaceByMetaClass( curClass ) )
                return ETypeRelation.Interface;
            if (curClass.IsParseMetaClass(compareClass))
                return ETypeRelation.Parent;
            if (compareClass.IsParseMetaClass(curClass))
                return ETypeRelation.Child;
            return ETypeRelation.No;
        }

        [Obsolete("Use ValidateClassTypeRelation")]
        public static ETypeRelation ValidateClassRelationByMetaClass(MetaClass curClass, MetaClass compareClass)
            => ValidateClassTypeRelation(curClass, compareClass);
        //public void HandleExtendContent( FileMetaClass mc )
        //{
        //    if (mc.metaClass == null) return;

        //    bool isSuccess = true;
        //    for (int i = 0; i < mc.metaClass.interfaceClass.Count; i++ )
        //    {
        //        var interfaceClass = mc.metaClass.interfaceClass[i];

        //        List<MetaMemberFunction> interfaceFunctionList = interfaceClass.GetMemberInterfaceFunction();

        //        for( int j = 0; j < interfaceFunctionList.Count; j++ )
        //        {
        //            var func = interfaceFunctionList[j];

        //            if( !mc.metaClass.GetMemberInterfaceFunctionByFunc(func) )
        //            {
        //                Log.AddMetaCoreLog(LID.ShowExtendMessage, "??????????????????????????????????????????????????????????????????????" + func.name + " Token??????: " );
        //                //func.fileMetaMemberFunction.token.sourceBeginLine.ToString()
        //                isSuccess = false;
        //                break;
        //            }
        //        }
        //    }
        //    var list = mc.metaClass.allMetaMemberFunctionList;
        //    for ( int i = 0; i < list.Count; i++ )
        //    {
        //        var func = list[i];
        //        if( func.isOverrideFunction )
        //        {

        //        }
        //    }
        //    if( !isSuccess )
        //    {
        //        return;
        //    }

        //    Stack<MetaClass> metaClassStack = new Stack<MetaClass>();

        //    var textendClass = mc.metaClass;
        //    while( true )
        //    {
        //        if (textendClass != null)
        //        {
        //            metaClassStack.Push(mc.metaClass);
        //            textendClass = textendClass.extendClass;
        //        }
        //        else
        //            break;
        //    }
        //    bool isFailed = false;
        //    while ( true )
        //    {
        //        textendClass = metaClassStack.Pop();
        //        if (metaClassStack.Count <= 0)
        //            break;
        //        if( !textendClass.isHandleExtendVariableDirty )
        //        {
        //            textendClass.HandleExtendClassVariable();
        //        }

        //        isFailed = false;
        //        if( textendClass != null && textendClass.extendClass != null )
        //        {
        //            foreach( var v in textendClass.metaExtendMemeberVariableDict )
        //            {
        //                if( textendClass.metaMemberVariableDict.ContainsKey( v.Key ) )
        //                {
        //                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error ????????????? " + v.Key + "  ??????????????? " + textendClass.allClassName + "????????? [" + v.Key + "] Token1??????: "
        //                        + textendClass.metaMemberVariableDict[v.Key].ToTokenString());
        //                    isFailed = true;
        //                    break;
        //                }
        //            }
        //        }
        //        if (isFailed) break;
               
        //    }
        //    if( !isFailed )
        //    {
        //        //Debug.Write("");
        //    }
        //}
        public MetaNode GetMetaClassByRef( MetaClass mc, FileMetaClassDefine fmcv )
        {
            if (fmcv == null) return null;

            MetaNode mb = GetMetaClassByClassDefine(mc, fmcv);
            if (mb != null)
                return mb;

            var mb2 = fmcv.fileMeta.GetMetaBaseByFileMetaClassRef(fmcv);
            
            return mb2;
        }
        public MetaNode GetMetaClassByClassDefine( MetaBase ownerBase, FileMetaClassDefine fmcd)
        {
            return GetMetaClassByNameAndFileMeta(ownerBase, fmcd.fileMeta, fmcd.stringList );
        }
        // ????wnerClass?????????????????????????wnerClass???????????????????????????????????????????????????????????????????????????????????????????????stringList?????????
        private MetaNode GetMetaNodeByListString( MetaBase ownerBase, List<string> stringList )
        {
            if (stringList.Count == 0)
                return null;

            string firstName = "";
            if ( stringList.Count == 1 )
            {
                firstName = stringList[0];
            }
            MetaNode findMB = CoreMetaClassManager.GetCoreMetaClass(firstName);
            if (findMB?.IsMetaClass() == true || findMB?.isMetaEnum == true  || findMB?.isMetaData == true)
            {
                return findMB;
            }
            findMB = null;

            MetaNode mb = ModuleManager.instance.selfModule.metaNode;
            if( ownerBase != null && ownerBase.metaNode != null )
            {
                mb = ownerBase.metaNode;
            }
            while (true)
            {
                MetaNode parentMB = mb;
                for (int i = 0; i < stringList.Count; i++)
                {
                    string name = stringList[i];
                    if (parentMB != null)
                    {
                        if (findMB == null)
                        {
                            findMB = parentMB.GetChildrenMetaNodeByName(name);
                            if (findMB == null)
                            {
                                parentMB = null;
                                break;
                            }
                            parentMB = findMB;
                        }
                        else
                        {
                            parentMB = parentMB.GetChildrenMetaNodeByName(name);
                        }
                    }
                }
                if (parentMB != null)
                {
                    if (parentMB.IsMetaClass() || parentMB.isMetaData || parentMB.isMetaEnum)
                        return parentMB;
                }
                mb = mb.parentNode;
                if (mb == null)
                    break;
            }
            return null;
        }
        public MetaNode GetMetaClassByNameAndFileMeta(MetaBase ownerBase, FileMeta fm, List<string> stringList )
        {
            MetaNode mn = GetMetaNodeByListString(ownerBase, stringList);
            if(mn == null )
            {
                var mb = fm.GetMetaNodeFileMetaClass(stringList);

                if( mb != null )
                {
                    return mb;
                }
            }
            return mn;
        }
        public MetaNode GetMetaClassByClassDefineAndFileMeta( MetaBase ownerBase, FileMetaClassDefine fmcd )
        {
            FileMeta fm = fmcd.fileMeta;
            MetaNode mc = GetMetaClassByClassDefine(ownerBase, fmcd);
            if( mc == null )
            {
                var mb = fm.GetMetaBaseByFileMetaClassRef(fmcd);
                if (mb != null)
                {
                    if (mb.isMetaNamespace )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "????????????????????????????????????????????????????????????!!");
                        return null;
                    }
                    else if (mb.IsMetaClass() || mb.isMetaData || mb.isMetaEnum)
                    {
                        return mb;
                    }
                }
            }
            return mc;
        }
        //??????FileInputTemplateNode ??????MetaType ???List< List< List<int> > > ???????????????????????????????????
        public MetaClass GetMetaClassByInputTemplateAndFileMeta( MetaBase ownerBase, FileInputTemplateNode fitn )
        {
            if (fitn == null)
            {
                return null;
            }
            var nlist = fitn.nameList;
            MetaNode mn = GetMetaClassByNameAndFileMeta(ownerBase, fitn.fileMeta, nlist);
            if (mn != null)
            {
                var mb = mn.GetMetaClassByTemplateCount(fitn.inputTemplateCount);
                if (mb != null)
                {
                    return mb;
                }
            }
            return null;
        }
        
        #region ????????????????? ??????????????????????????????????????????????????????????????????????????????????????
        public MetaClass GetMetaClassAndRegisterExptendTemplateClassInstance( MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaNode getmc = GetMetaClassByRef(curMc, fmcd );
            if (getmc == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "CheckExtendAndInterface failed, class not found: " + fmcd.allName);
                //    + "????????? " + m_ExtendClass.token.sourceBeginLine.ToString() );

            }
            else
            {
                //getmc = GetMetaClassAndRegisterExpendTemplateClassInstanceByTemplateList(curMc, getmc, fmcd.inputTemplateNodeList);
            }
            //return getmc;
            return null;
        }
        #endregion
        
    }
}
