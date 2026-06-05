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
                AddDefineMetaData(md);
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
        public MetaData FindMetaDataByNameAndType( MetaData md )
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
                if (MetaData.CompareMetaDataMember(v.Value, md))
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
                if(MetaData.CompareMetaDataMember( v.Value, md ) )
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
                        if( findmc2.structDefine )
                        {
                            findmc2.BindFileMetaClass(fmc);
                            findmc2.ParseFileMetaClassTemplate(fmc);
                            findmc2.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            findmc2.UpdateClassAllName();
                            AddExportMetaClass(findmc2);
                            return findmc2;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage,fmc.token,  "Found existing class node with incompatible define type.");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, fmc.token, "Found existing class node with incompatible define type2.");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, token, " useDefineNamespaceType not allow");
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
                    newmd.ParseFileMetaDataMemeberData(fmc);
                    AddInitHandleMetaDataList(newmd);
                    newmd.UpdateAllName();

                    return newmd;
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, token, "Class ??????? const ????");
                        return null;
                    }
                    var newmc = new MetaClass(fmc.name);
                    newmc.BindFileMetaClass(fmc);
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

            foreach( var it in m_InitHandleMetaDataList )
            {
                it.HandleExtendContent();
            }

            foreach( var it in m_InitHandleMetaEnumList )
            {
                it.ParseFileCollectMemberVariableDefineMetaType();
            }
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
    }
}
