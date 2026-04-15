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
using SimpleLanguage.Project;

namespace SimpleLanguage.Core
{
    public class ClassManager
    {
        public enum EClassRelation
        {
            None,
            CurClassError,
            CompareClassError,
            No,
            Same,
            Child,
            Parent,
            Similar,
            Interface,
            Num,
            SameClassNotSameInputTemplate,
            SameClassAndSameInputTemplate,
        }
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
        public List<MetaClass> runtimeClassList => m_RuntimeClassList;


        private List<MetaClass> m_RuntimeClassList = new List<MetaClass>();
        private Dictionary<string, MetaClass> m_AllClassDict = new Dictionary<string, MetaClass>();
        private List<MetaDynamicClass> m_DynamicClassList = new List<MetaDynamicClass>();         
        private Dictionary<string, MetaData> m_AllDataDict = new Dictionary<string, MetaData>();

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
        public MetaDynamicClass FindDynamicClass( MetaClass dc )
        {
            foreach( var v in m_DynamicClassList )
            {
                if( CompareMetaClassMemberVariable( dc, v ) )
                {
                    return v;
                }
            }
            return null;
        }
        public bool AddDynamicClass(MetaDynamicClass dc )
        {
            m_DynamicClassList.Add(dc);

            m_AllClassDict.Add(dc.allClassName, dc);
            return true;
        }
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
        public MetaData FindMetaData( MetaData md )
        {
            foreach( var v in m_AllDataDict )
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
            if(m_AllDataDict.ContainsKey(name ) )
            {
                return m_AllDataDict[name];
            }
            return null;
        }
        public bool AddMetaData(MetaData dc)
        {
            m_AllDataDict.Add(dc.name, dc);
            AddRuntimeMetaClass(dc);
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
        public bool CompareMetaDataMember(MetaData curClass, MetaData cpClass)
        {
            var curClassList = curClass.metaMemberDataDict;
            var cpClassList = cpClass.metaMemberDataDict;

            if (curClassList.Count != cpClassList.Count)
            {
                return false;
            }
            foreach( var v in curClassList )
            {
                if( !cpClassList.ContainsKey(v.Key ) )
                {
                    return false;
                }
                var vval = v.Value;
                var val2 = cpClassList[v.Key];

                if( vval.defineMetaType.metaClass != val2.defineMetaType.metaClass )
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
                    Log.AddMetaCoreLog(LID.Unknown, "Error 涓婄骇绫讳腑鐨凪etaClass娌℃湁缁戝畾!!");
                    return null;
                }

                var findmc = topLevelClass.metaClass.metaNode.GetChildrenMetaNodeByName( fmc.name );
                if (findmc != null)
                {
                    if(findmc.isMetaNamespace || findmc.isMetaData || findmc.isMetaEnum )
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Namespace/data/enum node already exists, duplicate class node is not allowed.");
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
                            Log.AddMetaCoreLog(LID.Unknown, "Found existing class node with incompatible define type.");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Found existing class node with incompatible define type.");
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
                        Log.AddMetaCoreLog(LID.Unknown, "鍛藉悕绌洪棿涓紝宸插畾涔夊叾瀹冮潪鍛藉悕绌洪棿鐨勭被鍨?!!");
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
                            Log.AddMetaCoreLog(LID.Unknown, "Class " + fmc.name + " at " + fmc.token.ToAllString() + " does not support parallel file definitions.");
                            return null;
                        }
                        bool isPartial = true;
                        foreach (var v in findamc.fileMetaClassDict)
                        {
                            if (v.Value.isPartial == false)
                            {
                                isPartial = false;
                                Log.AddMetaCoreLog(LID.Unknown, "Class " + findamc.name + " at " + v.Value.token.ToAllString() + " does not support parallel file definitions.");
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
                    Log.AddMetaCoreLog(LID.Unknown, "Error 浣跨敤鐨勫己瀹氬埗绫昏妭鐐圭殑鏂瑰紡涓紝娌℃湁鏌ユ壘鍒扮浉鍏崇殑绫伙紝鎵€浠ヤ笉鍏佽瀹氫箟璇ョ被锛岃鍏堝湪宸ョ▼涓畾涔夌被");
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
                    AddInitHandleMetaClassList(newmd);

                    return newmd;
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Class 涓紝浣跨敤鍏抽敭瀛楋紝涓嶅厑璁镐娇鐢–onst");
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
        public void AddRuntimeMetaClass( MetaClass mc )
        {
            var find1 = m_RuntimeClassList.Find(a => a == mc);
            if( find1  == null )
            {
                m_RuntimeClassList.Add(mc);

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
                    Log.AddMetaCoreLog(LID.Unknown, $"宸插寘鍚被:{mc.allClassName} 鍙堣繘琛屼簡閲嶈繘娣诲姞!");
                    return;
                }
            }
            m_AllClassDict.Add(acn, mc);

        }
        public void ParseInitMetaClassList()
        {
            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseMetaTemplateInConstraint();
                it.ParseExtendsRelation();
                it.ParseInterfaceRelation();
                AddRuntimeMetaClass(it);
            }

            foreach (var it in m_InitHandleMetaClassList)
            {
                it.CalcExtendLevel();
            }
            m_InitHandleMetaClassList.Sort((x, y) => x.extendLevel - y.extendLevel);

            foreach (var it in m_InitHandleMetaClassList)
            {
                it.ParseFileCollectMemberVariableDefineMetaType();
                it.ParseFileCollectMemberFunctionDefineMetaType();
                it.HandleExtendAndInterfaceMetaTypeInstnace();
            }
            //foreach( var it in m_InitHandleMetaClassList )
            //{
            //    it.ParseGenTemplateClassMetaType();
            //}
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
        public static EClassRelation ResolveAssignRelation(
            MetaType targetMetaType,
            MetaExpressNode expressNode,
            bool useTemplateExactMatch,
            bool allowEnumOwnerEqual,
            out MetaType expressRetMetaDefineType,
            out MetaClass curClass,
            out MetaClass compareClass,
            out bool isNullConstExpress)
        {
            expressRetMetaDefineType = null;
            compareClass = null;
            isNullConstExpress = false;
            curClass = targetMetaType?.metaClass;

            if (curClass == null)
            {
                return EClassRelation.CurClassError;
            }
            if (expressNode == null)
            {
                return EClassRelation.CompareClassError;
            }

            if (expressNode is MetaConstExpressNode constExpressNode && constExpressNode.eType == EType.Null)
            {
                isNullConstExpress = true;
                return EClassRelation.Same;
            }

            expressRetMetaDefineType = expressNode.GetReturnMetaDefineType();
            compareClass = expressRetMetaDefineType?.metaClass;
            if (compareClass == null)
            {
                return EClassRelation.CompareClassError;
            }

            if (allowEnumOwnerEqual && curClass is MetaEnum me && expressNode is MetaCallLinkExpressNode mclen)
            {
                var mv = mclen.GetMetaVariable();
                if (mv?.ownerMetaClass == me)
                {
                    return EClassRelation.Same;
                }
            }

            return ValidateClassRelationByMetaClass(curClass, compareClass);
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
        public void HandleInterface( FileMetaClass mc )
        {
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
                        Log.AddMetaCoreLog(LID.Unknown, "鎵惧埌浜嗗凡鏈夊懡鍚嶇┖闂磋€屼笉鏄缁ф壙鐨勭被!!");
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
                Log.AddMetaCoreLog(LID.Unknown, "CheckExtendAndInterface failed, class not found: " + fmcd.allName);
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
        
        public void PrintAlllClassContent()
        {
            foreach( var v in m_AllClassDict )
            {

            }

            foreach( var v in m_RuntimeClassList )
            {

            }
        }
    }
}
