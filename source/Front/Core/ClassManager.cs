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
        public List<MetaClass> exportClassList => m_ExportClassList;


        private List<MetaClass> m_ExportClassList = new List<MetaClass>();
        private Dictionary<string, MetaClass> m_AllClassDict = new Dictionary<string, MetaClass>();
        //private List<MetaDynamicClass> m_DynamicClassList = new List<MetaDynamicClass>();         
        private Dictionary<string, MetaData> m_DefineDataDict = new Dictionary<string, MetaData>();
        private Dictionary<string, MetaData> m_AnonymousDataDict = new Dictionary<string, MetaData>();

        private List<MetaGenTemplateClass> m_GenTemplateMetaClassList = new List<MetaGenTemplateClass>();
        //private List<MetaGenTemplateClass> m_NeedHandleTemplateMetaClassList = new List<MetaGenTemplateClass>();
        private List<MetaClass> m_InitHandleMetaClassList = new List<MetaClass>();

        public MetaClass GetClassByName(string name, int templateCount = 0 )
        {
            string nname = name + "_" + templateCount;
            if (m_AllClassDict.ContainsKey(nname))
                return m_AllClassDict[nname];
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
        public void AddGenTemplateClass(MetaGenTemplateClass mc)
        {
            if( !m_GenTemplateMetaClassList.Contains(mc ) )
            {
                m_GenTemplateMetaClassList.Add(mc);
            }
            if( !m_AllClassDict.ContainsKey(mc.allClassName ) )
            {
                m_AllClassDict.Add(mc.allClassName, mc);
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
        /// <summary>
        /// 仅在匿名 data 池中按结构查找（用于语句字面量、data 内嵌匿名结构去重）。
        /// </summary>
        public MetaData FindMetaData( MetaData md )
        {
            return FindAnonymousMetaData(md);
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
        /// 遍历源码声明 data（define 区）。
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
            AddExportMetaClass(dc);
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
            AddExportMetaClass(dc);
            return true;
        }
        public bool AddMetaData(MetaData dc)
        {
            // 兼容旧调用：define data 进 define 区，dynamic/anonymous data 进 anonymous 区。
            return dc != null && (dc.isDynamic ? AddAnonymousMetaData(dc) : AddDefineMetaData(dc));
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
        /// 判断两个 MetaData 是否表示同一匿名/结构化类型，用于注册表去重。
        /// 动态匿名类型：成员个数相同、按声明顺序名称一致、字段类型形状一致（嵌套匿名 MetaData 递归比较）。
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

            // 避免「两个均无 data 成员」被误判为同一类型（如动态 class 占位）
            if (curClassList.Count == 0 && cpClassList.Count == 0)
            {
                return false;
            }

            // 动态匿名 data 字面量：按顺序做完整结构 + 类型形状比较（不依赖字典迭代顺序）
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
            if (ta.metaClass is MetaData mdA && tb.metaClass is MetaData mdB
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
        public MetaClass AddClass( FileMetaClass fmc )
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
                    Log.AddMetaCoreLog(LID.AutoClassManagerL229, "Error 涓婄骇绫讳腑鐨凪etaClass娌℃湁缁戝畾!!");
                    return null;
                }

                var findmc = topLevelClass.metaClass.metaNode.GetChildrenMetaNodeByName( fmc.name );
                if (findmc != null)
                {
                    if(findmc.isMetaNamespace || findmc.isMetaData || findmc.isMetaEnum )
                    {
                        Log.AddMetaCoreLog(LID.AutoClassManagerL238, "Namespace/data/enum node already exists, duplicate class node is not allowed.");
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
                            return findmc2;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.AutoClassManagerL255, "Found existing class node with incompatible define type.");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.AutoClassManagerL260, "Found existing class node with incompatible define type.");
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
                        Log.AddMetaCoreLog(LID.AutoClassManagerL293, "鍛藉悕绌洪棿涓紝宸插畾涔夊叾瀹冮潪鍛藉悕绌洪棿鐨勭被鍨?!!");
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
                            return ffmc;
                        }
                        else
                        {
                            isCanAddBind = true;
                        }
                        //if (!fmc.isPartial)
                        //{
                        //    Log.AddMetaCoreLog(LID.Unknown, "绫?" + fmc.name + "鍦? " + fmc.token.ToAllString() + "涓嶆敮鎸佹枃浠跺苟琛?瀹氫箟绫");
                        //    return null;
                        //}
                        //bool isPartial = true;
                        //foreach (var v in ffmc.fileMetaClassDict)
                        //{
                        //    if (v.Value.isPartial == false)
                        //    {
                        //        isPartial = false;
                        //        Log.AddMetaCoreLog(LID.Unknown, "绫?" + findamc.name + "鍦? " + v.Value.token.ToAllString() + "涓嶆敮鎸佹枃浠跺苟琛?瀹氫箟绫");
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
                            return findamc;
                        }
                        if (!fmc.isPartial)
                        {
                            Log.AddMetaCoreLog(LID.AutoClassManagerL378, "Class " + fmc.name + " at " + fmc.token.ToAllString() + " does not support parallel file definitions.");
                            return null;
                        }
                        bool isPartial = true;
                        foreach (var v in findamc.fileMetaClassDict)
                        {
                            if (v.Value.isPartial == false)
                            {
                                isPartial = false;
                                Log.AddMetaCoreLog(LID.AutoClassManagerL387, "Class " + findamc.name + " at " + v.Value.token.ToAllString() + " does not support parallel file definitions.");
                                break;
                            }
                        }
                        if (isPartial == false)
                        {
                            return null;
                        }
                        findamc.BindFileMetaClass(fmc);
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
                    Log.AddMetaCoreLog(LID.AutoClassManagerL409, "Error 浣跨敤鐨勫己瀹氬埗绫昏妭鐐圭殑鏂瑰紡涓紝娌℃湁鏌ユ壘鍒扮浉鍏崇殑绫伙紝鎵€浠ヤ笉鍏佽瀹氫箟璇ョ被锛岃鍏堝湪宸ョ▼涓畾涔夌被");
                }
                if (fmc.isEnum)
                {
                    MetaEnum newme = new MetaEnum(fmc.name);
                    finalTopMetaNode.AddMetaEnum(newme);
                    fmc.SetMetaClass(newme);
                    newme.SetClassDefineType(EClassDefineType.CodeDefine);
                    newme.BindFileMetaClass(fmc);
                    newme.ParseFileMetaEnumMemeberEnum(fmc);
                    newme.UpdateClassAllName();
                    
                    AddInitHandleMetaClassList(newme);

                    return newme;
                }
                else if (fmc.isData)
                {
                    var newmd = new MetaData(fmc);
                    newmd.BindFileMetaClass(fmc);
                    newmd.SetClassDefineType(EClassDefineType.CodeDefine);
                    finalTopMetaNode.AddMetaData(newmd);
                    newmd.UpdateClassAllName();
                    newmd.ParseFileMetaDataMemeberData(fmc);
                    AddDefineMetaData(newmd);
                    AddInitHandleMetaClassList(newmd);

                    return newmd;
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Log.AddMetaCoreLog(LID.AutoClassManagerL440, "Class 涓紝浣跨敤鍏抽敭瀛楋紝涓嶅厑璁镐娇鐢–onst");
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
            var find1 = m_ExportClassList.Find(a => a == mc);
            if( find1  == null )
            {
                m_ExportClassList.Add(mc);

                AddDictMetaClass(mc);
            }
        }
        void AddDictMetaClass( MetaClass mc )
        {
            string acn = mc.allClassName + "_" + mc.metaTemplateList.Count;
            foreach( var v in m_AllClassDict )
            {
                if( v.Value == mc )
                {
                    Log.AddMetaCoreLog(LID.AutoClassManagerL478, $"宸插寘鍚被:{mc.allClassName} 鍙堣繘琛屼簡閲嶈繘娣诲姞!");
                    return;
                }
            }
            m_AllClassDict.Add(acn, mc);

        }
        /// <summary>
        /// 模板约束、extends/implements、运行时注册，以及按继承深度排序（尚未收集成员上的定义类型）。
        /// 之后应调用 <see cref="TypeManager.ResolveAllDeclaredTypeAliases"/>，再调用 <see cref=""/>。
        /// </summary>
        public void ParseInitMetaClassListThroughInheritance()
        {
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseMetaTemplateInConstraint();
                it.ParseExtendsRelation();
                it.ParseInterfaceRelation();
            }

            foreach (var it in m_InitHandleMetaClassList)
            {
                it.CalcExtendLevel();
            }
            m_InitHandleMetaClassList.Sort((x, y) => x.extendLevel - y.extendLevel);
        }

        /// <summary>在 typealias 注册之后，从源文件收集成员变量/函数声明上的定义类型并处理继承侧实例化。</summary>
        public void ParseInitMetaClassListCollectMemberDefineMetaTypes()
        {
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseFileCollectMemberVariableDefineMetaType();
                it.ParseFileCollectMemberFunctionDefineMetaType();
                it.HandleExtendAndInterfaceMetaTypeInstnace();
                it.EnsureParsedGenTemplateMetaClasses();
            }
            EnsureAllGenTemplateClassesParsed();
            //foreach( var it in m_InitHandleMetaClassList )
            //{
            //    it.ParseGenTemplateClassMetaType();
            //}
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
        }
        public void ParseMemberEnumExpress()
        {
            foreach (var it in m_InitHandleMetaClassList )
            {
                if( it is MetaEnum me )
                {
                    me.ParseMemberMetaEnumExpress();
                }
            }
        }

        public void ParseMetaDataMemberAnonAndArray()
        {

            // 嵌套 const / 匿名 {} / 数组元素未进入 metaMemberDataVariableList，需在 ParseExpress 之后按树后序补全匿名 MetaData 与 NewObject。
            foreach (var md in EnumerateDefineMetaData())
            {
                var roots = new List<MetaMemberData>();
                foreach (var kv in md.metaMemberDataDict)
                {
                    roots.Add(kv.Value);
                }
                roots.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
                for (int i = 0; i < roots.Count; i++)
                {
                    MetaMemberData.ResolveAnonymousDataHierarchyPostOrder(roots[i]);
                }
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

            // 瀵规ā鏉跨害鏉?娉涘瀷瀹炰緥鍖栫被锛岃嫢缁ф壙閾惧寘鍚?Num锛屼篃鎸夋暟鍊肩被鍨嬪鐞?
            if (curClass.IsParseMetaClass(CoreMetaClassManager.numMetaClass))
            {
                return true;
            }

            return false;
        }

        /// <summary> 单模板参数，如 Array&lt;T&gt;、Iterator&lt;T&gt; 的第一维元素类型。 </summary>
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

        /// <summary> 具体数值类型（Int32、Float32 等），不含抽象 Num 本身。 </summary>
        public static bool IsConcreteNumericElementType(MetaType elem)
        {
            if (elem?.metaClass == null) return false;
            if (elem.metaClass == CoreMetaClassManager.numMetaClass) return false;
            return IsNumberClass(elem.metaClass);
        }

        /// <summary>
        /// 仅 Int8…Float64 等核心原语；用于传参阶比较。非核心原语返回 false。
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
        /// 调用点实参匹配形参：在 <see cref="ValidateClassRelationByMetaClass"/> 已给出 <see cref="EClassRelation.Num"/> 时，
        /// 对「双核心原语」收紧为仅允许更窄实参隐式拓宽到更宽形参（如 Int8→UInt32、Float32→Float64）；
        /// 同阶不同号类（如 Int32 与 UInt32）或 int 与 float 混用则 false；任一方非核心原语则 true（保持旧 Num 宽松语义）。
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

        /// <summary> Iterator&lt;Number&gt; &lt;- 元素为具体数值类型的 Array（只读遍历视角，允许协变）。 </summary>
        public static bool TryIteratorNumberFromConcreteNumericArray(MetaType targetIterator, MetaType exprArray)
        {
            if (targetIterator == null || exprArray == null) return false;
            if (!targetIterator.IsIterator() || !exprArray.IsArray()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetIterator);
            var eArg = GetSingleTemplateArgMetaType(exprArray);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary> Iterator&lt;Number&gt; &lt;- Iterator&lt;具体数值&gt;：仅遍历语义，允许 Number 抽象协变。 </summary>
        public static bool TryIteratorNumberFromConcreteNumericIterator(MetaType targetIterator, MetaType exprIterator)
        {
            if (targetIterator == null || exprIterator == null) return false;
            if (!targetIterator.IsIterator() || !exprIterator.IsIterator()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetIterator);
            var eArg = GetSingleTemplateArgMetaType(exprIterator);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary>
        /// Iterator&lt;Number&gt; <- arr.iterator 场景兜底：
        /// 某些链式表达式上，iterator 返回的接口模板参数在当前阶段可能未具体化；
        /// 此时改为从调用链首节点推断 Array 元素类型来判断 Number 协变。
        /// </summary>
        public static bool TryIteratorNumberFromArrayIteratorSource(MetaType targetIterator, MetaExpressNode expressNode)
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
        /// IIterable&lt;TTarget&gt; &lt;- Array&lt;TExpr&gt;：当元素可赋值（同型、子类、接口实现、数值族）时允许。
        /// 典型场景：IIterable&lt;Object&gt; &lt;- Int32[]。
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

            var relation = ValidateClassRelationByMetaClass(tClass, eClass);
            return relation == EClassRelation.Same
                || relation == EClassRelation.Child
                || relation == EClassRelation.Interface
                || relation == EClassRelation.Num;
        }

        /// <summary> const Array&lt;Number&gt; &lt;- Array&lt;具体数值&gt;：仅 const 目标允许元素抽象协变。 </summary>
        public static bool TryConstArrayNumberFromConcreteNumericArray(MetaType targetArray, MetaType exprArray, MetaVariable targetVar)
        {
            if (targetVar == null || !targetVar.isConst) return false;
            if (targetArray == null || exprArray == null) return false;
            if (!targetArray.IsArray() || !exprArray.IsArray()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetArray);
            var eArg = GetSingleTemplateArgMetaType(exprArray);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        /// <summary> Iterator&lt;Num&gt; 或 const Array&lt;Num&gt; 与具体数值 Array 的协变总开关（赋值/参数匹配用）。 </summary>
        public static bool TryNumberArrayCovarianceAllow(MetaType target, MetaType expr, MetaVariable targetVar)
        {
            if (target == null || expr == null) return false;
            if (TryIteratorNumberFromConcreteNumericArray(target, expr)) return true;
            if (TryConstArrayNumberFromConcreteNumericArray(target, expr, targetVar)) return true;
            return false;
        }

        /// <summary>
        /// Array 声明与右侧表达式：模板须与 Array 泛型结构一致；元素类型须 <see cref="TypeManager.CompareMetaType"/> 一致，
        /// 或（在仍同为 Array 时递归）右侧元素实现左侧元素接口（<see cref="EClassRelation.Interface"/>）。
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
                if (ValidateClassRelationByMetaClass(cur, cmp) == EClassRelation.Interface) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 在 <see cref="TypeManager.CompareMetaType"/> 已为 false 时，判断是否为「右侧实现左侧接口」等非 Array 元素赋值。
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
            return ValidateClassRelationByMetaClass(cur, cmp) == EClassRelation.Interface;
        }

        public static EClassRelation ValidateClassRelationByMetaClass( MetaClass curClass, MetaClass compareClass )
        {
            // null can be assigned to any non-primitive/reference type parameter.
            // This is required for call-site overload resolution, e.g.:
            // NativeBridge.Call(..., null, paramObjs)
            if (compareClass == CoreMetaClassManager.nullMetaClass)
            {
                // numeric primitives and bool are treated as value types and do not accept null
                if (IsNumberClass(curClass) || curClass == CoreMetaClassManager.booleanMetaClass)
                    return EClassRelation.No;

                // any other class (including Object/String/BridgeObject/arrays) accepts null
                if (curClass == CoreMetaClassManager.objectMetaClass)
                    return EClassRelation.Same;
                return EClassRelation.Parent;
            }
            if ( curClass == CoreMetaClassManager.objectMetaClass )
            {
                if (curClass == compareClass)
                {
                    return EClassRelation.Same;
                }
                    
                return EClassRelation.Child;
            }
            if (curClass.Equals(compareClass))
            {
                return EClassRelation.Same;
            }
            else
            {
                if( curClass == CoreMetaClassManager.numMetaClass )
                {
                    if (IsNumberClass(compareClass))
                    {
                        return EClassRelation.Num;
                    }
                    else return EClassRelation.No;
                }
                else if(IsNumberClass(curClass) && IsNumberClass(compareClass ) )
                {
                    //switch( curClass )
                    //{
                    //    case Int16MetaClass int16:
                    //        {

                    //        }
                    //        break;
                    //}
                    return EClassRelation.Num;
                }
                else
                {
                    if(compareClass.IsInterfaceByMetaClass( curClass ) )
                    {
                        return EClassRelation.Interface;
                    }
                    if (curClass.IsParseMetaClass(compareClass))
                    {
                        return EClassRelation.Parent;
                    }
                    if (compareClass.IsParseMetaClass(curClass))
                    {
                        return EClassRelation.Child;
                    }
                    return EClassRelation.No;
                }
            }
        }
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
        //                Log.AddMetaCoreLog(LID.Unknown, "鏌ユ壘鎺ュ彛绫讳腑鐨勮瀹炵幇鐨勫嚱鏁帮紝瀹炵幇澶辫触鍑芥暟鍚嶇О" + func.name + " Token浣嶇疆: " );
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
        //                    Log.AddMetaCoreLog(LID.Unknown, "Error 鍦ㄧ被鐨勫€? " + v.Key + "  鏈夐噸澶嶅畾涔? " + textendClass.allClassName + "涓紝鍊? [" + v.Key + "] Token1浣嶇疆: "
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
        public MetaNode GetMetaClassByClassDefine( MetaClass ownerClass, FileMetaClassDefine fmcd)
        {
            return GetMetaClassByNameAndFileMeta(ownerClass, fmcd.fileMeta, fmcd.stringList );
        }
        // 鍦╫wnerClass绫讳腑锛岄€氳繃褰撳墠鐨刼wnerClass鐨勭埗鑺傜偣閫愭煡锛岀洿鍒版病鏈夌埗鑺傜偣锛屽鏋滄壘鍒颁簡褰撳墠鐨勮妭鐐瑰悗锛屽紑濮嬪線stringList涓嬭竟鎵?
        private MetaNode GetMetaNodeByListString( MetaClass ownerClass, List<string> stringList )
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
            if( ownerClass != null )
            {
                mb = ownerClass.metaNode;
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
                    if (parentMB.IsMetaClass())
                        return parentMB;
                }
                mb = mb.parentNode;
                if (mb == null)
                    break;
            }
            return null;
        }
        public MetaNode GetMetaClassByNameAndFileMeta(MetaClass ownerClass, FileMeta fm, List<string> stringList )
        {
            MetaNode mn = GetMetaNodeByListString(ownerClass, stringList);
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
        public MetaNode GetMetaClassByClassDefineAndFileMeta( MetaClass ownerClass, FileMetaClassDefine fmcd )
        {
            FileMeta fm = fmcd.fileMeta;
            MetaNode mc = GetMetaClassByClassDefine(ownerClass, fmcd);
            if( mc == null )
            {
                var mb = fm.GetMetaBaseByFileMetaClassRef(fmcd);
                if (mb != null)
                {
                    if (mb.isMetaNamespace )
                    {
                        Log.AddMetaCoreLog(LID.AutoClassManagerL926, "鎵惧埌浜嗗凡鏈夊懡鍚嶇┖闂磋€屼笉鏄缁ф壙鐨勭被!!");
                        return null;
                    }
                    else if (mb.IsMetaClass())
                    {
                        return mb;
                    }
                }
            }
            return mc;
        }
        //閫氳繃FileInputTemplateNode 鑾峰彇MetaType 渚?List< List< List<int> > > 杩欑鐨勶紝闇€瑕佸祵濂楄幏鍙栧鐞?
        public MetaClass GetMetaClassByInputTemplateAndFileMeta( MetaClass ownerClass, FileInputTemplateNode fitn )
        {
            if (fitn == null)
            {
                return null;
            }
            var nlist = fitn.nameList;
            MetaNode mn = GetMetaClassByNameAndFileMeta(ownerClass, fitn.fileMeta, nlist);
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
        
        #region 妯℃澘绫诲鐞嗗尯 璇ュ尯鍏堣瘑鍒綋鍓嶇被锛?鍐嶈瘑鍒槸鍚﹀甫妯℃澘杈撳叆锛屽鏋滃甫鍒欐嬁妯℃澘绫?
        public MetaClass GetMetaClassAndRegisterExptendTemplateClassInstance( MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaNode getmc = GetMetaClassByRef(curMc, fmcd );
            if (getmc == null)
            {
                Log.AddMetaCoreLog(LID.AutoClassManagerL965, "CheckExtendAndInterface failed, class not found: " + fmcd.allName);
                //    + "浣嶇疆琛? " + m_ExtendClass.token.sourceBeginLine.ToString() );

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
