//****************************************************************************
//  File:      ClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta Class's manager center 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.Core.AOT
{
    public class MetaGenManager
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
            SameClassNotSameInputTemplate,
            SameClassAndSameInputTemplate,
        }
        public static MetaGenManager s_Instance = null;
        public static MetaGenManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new MetaGenManager();
                }
                return s_Instance;
            }
        }
        public Dictionary<string, MetaClass> allClassDict => m_AllClassDict;
        public Dictionary<string, MetaData> allDataDict => m_AllDataDict;
        public List<MetaDynamicClass> dynamicClassList => m_DynamicClassList;
        public List<MetaGenTemplateClass> needHandleTemplateMetaClassList => m_NeedHandleTemplateMetaClassList;
        public List<MetaClass> preInitHandleMetaClassList => m_InitHandleMetaClassList;


        private Dictionary<string, MetaClass> m_AllClassDict = new Dictionary<string, MetaClass>();
        private List<MetaDynamicClass> m_DynamicClassList = new List<MetaDynamicClass>();         
        private Dictionary<string, MetaData> m_AllDataDict = new Dictionary<string, MetaData>();

        private List<MetaGenTemplateClass> m_GenTemplateMetaClassList = new List<MetaGenTemplateClass>();
        private List<MetaGenTemplateClass> m_NeedHandleTemplateMetaClassList = new List<MetaGenTemplateClass>();
        private List<MetaClass> m_InitHandleMetaClassList = new List<MetaClass>();

        public MetaClass GetClassByName(string name, int templateCount = 0 )
        {
            string nname = name + "_" + templateCount;
            if (m_AllClassDict.ContainsKey(nname))
                return m_AllClassDict[nname];
            return null;
        }
        public MetaClass GetMetaClassByCSharpType(System.Type type)
        {
            string typeName = type.Name;
            switch (typeName)
            {
                case "Byte":
                    return CoreMetaClassManager.byteMetaClass;
                case "SByte":
                    return CoreMetaClassManager.sbyteMetaClass;
                case "Int16":
                    return CoreMetaClassManager.int16MetaClass;
                case "UInt16":
                    return CoreMetaClassManager.uint16MetaClass;
                case "Int32":
                    return CoreMetaClassManager.int32MetaClass;
                case "UInt32":
                    return CoreMetaClassManager.uint32MetaClass;
                case "Int64":
                    return CoreMetaClassManager.int64MetaClass;
                case "UInt64":
                    return CoreMetaClassManager.uint64MetaClass;
                case "Float32":
                    return CoreMetaClassManager.float32MetaClass;
                case "Float64":
                    return CoreMetaClassManager.float64MetaClass;
                case "String":
                    return CoreMetaClassManager.stringMetaClass;
                case "Object":
                    return CoreMetaClassManager.objectMetaClass;
                case "Void":
                    return CoreMetaClassManager.voidMetaClass;
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
        public void AddGenTemplateClass(MetaGenTemplateClass mc )
        {
            //var find1 = mc.metaGenTemplateClassList.Find(a => a == mc);
            //if( find1 == null )
            //{
            //    mc.metaGenTemplateClassList.Add(mc);
            //    //m_GenTemplateClassList.Add(mc);
            //}
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
        public void AddMetaGenTemplateClassList(MetaGenTemplateClass mc)
        {
            if (m_GenTemplateMetaClassList.IndexOf(mc) == -1)
            {
                m_GenTemplateMetaClassList.Add(mc);
            }
        }
        public void AddNeedHandleTemplateMetaClassList(MetaGenTemplateClass mc)
        {
            if (m_NeedHandleTemplateMetaClassList.IndexOf(mc) == -1)
            {
                m_NeedHandleTemplateMetaClassList.Add(mc);
            }
            
        }
        public bool IsMetaGenTemplateClass(MetaGenTemplateClass mc )
        {
            if (m_GenTemplateMetaClassList.IndexOf(mc) != -1)
                return true;

            return false;
        }
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
                        || curMV.metaDefineType == cpMV.metaDefineType)
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

                if( vval.metaDefineType.metaClass != val2.metaDefineType.metaClass )
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
            MetaNode finalTopMetaNode = ModuleManager.instance.selfModule.metaNode;
            FileMetaClass topLevelClass = fmc.topLevelFileMetaClass;
            if ( topLevelClass != null )
            {                
                if( topLevelClass?.metaClass?.metaNode == null )
                {
                    Log.AddInStructMeta(EError.None, "Error 上级类中的MetaClass没有绑定!!");
                    return null;
                }

                var findmc = topLevelClass.metaClass.metaNode.GetChildrenMetaNodeByName( fmc.name );
                if (findmc != null)
                {
                    if(findmc.isMetaNamespace || findmc.isMetaData || findmc.isMetaEnum )
                    {
                        Log.AddInStructMeta(EError.None, "已添加了空间命名节点，不允许有重复名称的类节点再次添加");
                        return null;
                    }

                    MetaClass findmc2 = findmc.GetMetaClassByTemplateCount(fmc.templateDefineList.Count);
                    if ( findmc2 != null )
                    {
                        if( findmc2.classDefineType == EClassDefineType.StructDefine )
                        {
                            findmc2.BindFileMetaClass(fmc);
                            findmc2.SetClassDefineType(EClassDefineType.CodeDefine);
                            findmc2.ParseFileMetaClassTemplate(fmc);
                            findmc2.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            return findmc2;
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "Error 查到内部不是内部内，可能有相同成员");
                        }
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error 查到内部不是内部内，可能有相同成员");
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
                    finalTopMetaNode = NamespaceManager.instance.SearchFinalNamespace(fmc.topLevelFileMetaNamespace);

                    if( fmc.namespaceBlock?.namespaceList.Count > 0 )
                    {
                        finalTopMetaNode = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock, finalTopMetaNode);
                    }
                }

                if (finalTopMetaNode == null && fmc.namespaceBlock?.namespaceList?.Count > 0 )
                {
                    finalTopMetaNode = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock);
                   
                    if (finalTopMetaNode == null )
                    {
                        Log.AddInStructMeta(EError.None, "命名空间中，已定义其它非命名空间的类型 !!");
                        return null;
                    }
                }
                if ( finalTopMetaNode.IsMetaClass() )
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
                            Log.AddInStructMeta(EError.None, "类:" + fmc.name + "在: " + fmc.token.ToAllString() + "不支持文件并行 定义类");
                            return null;
                        }
                        bool isPartial = true;
                        foreach (var v in findamc.fileMetaClassDict)
                        {
                            if (v.Value.isPartial == false)
                            {
                                isPartial = false;
                                Log.AddInStructMeta(EError.None, "类:" + findamc.name + "在: " + v.Value.token.ToAllString() + "不支持文件并行 定义类");
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
                    Log.AddInStructMeta(EError.None, "Error 使用的强定制类节点的方式中，没有查找到相关的类，所以不允许定义该类，请先在工程中定义类");
                }
                if (fmc.isEnum)
                {
                    MetaEnum newme = new MetaEnum(fmc.name);
                    finalTopMetaNode.AddMetaEnum(newme);
                    newme.BindFileMetaClass(fmc);
                    newme.ParseFileMetaEnumMemeberEnum(fmc);
                   

                    AddInitHandleMetaClassList(newme);

                    return newme;
                }
                else if (fmc.isData)
                {
                    var newmd = new MetaData( fmc );
                    finalTopMetaNode.AddMetaData(newmd);
                    AddInitHandleMetaClassList(newmd);
                    newmd.BindFileMetaClass(fmc);
                    newmd.ParseFileMetaDataMemeberData(fmc);

                    return newmd;
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Log.AddInStructMeta(EError.None, "Class 中，使用关键字，不允许使用Const");
                        return null;
                    }
                    var newmc = new MetaClass(fmc.name);
                    newmc.BindFileMetaClass(fmc);
                    newmc.SetClassDefineType(EClassDefineType.CodeDefine);
                    newmc.ParseFileMetaClassTemplate(fmc);
                    finalTopMetaNode.AddMetaClass(newmc);
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
        public void AddDictMetaClass( MetaClass mc )
        {
            string acn = mc.allClassName + "_" + mc.metaTemplateList.Count;
            if (m_AllClassDict.ContainsKey(acn) )
            {
                Log.AddInStructMeta(EError.AddClassNameSame, $"已包含类:{mc.allClassName} 又进行了重进添加!");
                return;
            }
            m_AllClassDict.Add(acn, mc);
        }
        public void HandleExtendMember()
        {
            m_InitHandleMetaClassList.Sort((x, y) => x.extendLevel - y.extendLevel);
            
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.HandleExtendMemberVariable();
                it.HandleExtendMemberFunction();
            }
        }
        public void ParseInitMetaClassList()
        {
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.ParseExtendsRelation();
                it.ParseInterfaceRelation();
                it.ParseMemberVariableDefineMetaType();
                it.ParseMemberFunctionDefineMetaType();
                it.ParseMetaInConstraint();
                AddDictMetaClass(it);
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
        //    if (m_NeedHandleTemplateMetaClassList.Count==0) return;

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
            if (curClass == CoreMetaClassManager.byteMetaClass
                || curClass == CoreMetaClassManager.sbyteMetaClass
                //|| curClass == CoreMetaClassManager.charMetaClass
                || curClass == CoreMetaClassManager.int16MetaClass
                || curClass == CoreMetaClassManager.uint16MetaClass
                || curClass == CoreMetaClassManager.int32MetaClass
                || curClass == CoreMetaClassManager.uint32MetaClass
                || curClass == CoreMetaClassManager.int64MetaClass
                || curClass == CoreMetaClassManager.uint64MetaClass)
            {
                return true;
            }
            return false;
        }
        public static EClassRelation ValidateClassRelationByMetaClass( MetaClass curClass, MetaClass compareClass )
        {
            if ( curClass == CoreMetaClassManager.objectMetaClass )
            {
                return EClassRelation.Child;
            }
            if (curClass.Equals(compareClass))
            {
                return EClassRelation.Same;
            }
            else
            {
                if(IsNumberClass(curClass) && IsNumberClass(compareClass ) )
                {
                    //switch( curClass )
                    //{
                    //    case Int16MetaClass int16:
                    //        {

                    //        }
                    //        break;
                    //}
                    return EClassRelation.Similar;
                }
                else
                {
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
        //                Log.AddInStructMeta(EError.None, "查找接口类中的要实现的函数，实现失败函数名称" + func.name + " Token位置: " );
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
        //                    Log.AddInStructMeta(EError.None, "Error 在类的值: " + v.Key + "  有重复定义: " + textendClass.allClassName + "中，值: [" + v.Key + "] Token1位置: "
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
        //public MetaClass GetMetaClassByName(string inputname, MetaClass ownerClass = null, FileMeta fm = null )
        //{
        //    MetaClass fmc = CoreMetaClassManager.GetCoreMetaClass(inputname);
        //    if( fmc != null )
        //    {
        //        return fmc;
        //    }
        //    fmc = ClassManager.instance.GetClassByName(inputname);
        //    if (fmc != null)
        //    {
        //        return fmc;
        //    }

        //    if( ownerClass != null )
        //    {
        //        //子类
        //        MetaBase tmb = ownerClass.GetChildrenMetaBaseByName(inputname);
        //        if (tmb != null && tmb is MetaClass)
        //        {
        //            return tmb as MetaClass;
        //        }
        //    }
        //    //引入文件的类或者是命名空间
        //    if (fm == null)
        //    {
        //        fmc = fm.GetMetaBaseByName(inputname) as MetaClass;
        //    }

        //    return fmc;
        //}
        //public MetaBase GetMetaBaseByName(MetaClass ownerClass, string name)
        //{
        //    MetaBase mb = ownerClass;
        //    while ( mb != null )
        //    {
        //        MetaBase mb2 = mb.GetMetaBaseByName(name);

        //        if (mb2 != null)
        //        {
        //            return mb2;
        //        }
        //        mb = mb.parentNode;
        //        if (mb == null)
        //            break;
        //    }
        //    return null;
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
        // 在ownerClass类中，通过当前的ownerClass的父节点逐查，直到没有父节点，如果找到了当前的节点后，开始往stringList下边找
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
            if (findMB?.IsMetaClass() == true )
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
                        Log.AddInStructMeta(EError.None, "找到了已有命名空间而不是要继承的类!!");
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
        //通过FileInputTemplateNode 获取MetaType 例 List< List< List<int> > > 这种的，需要嵌套获取处理
        public MetaClass GetMetaClassByInputTemplateAndFileMeta( MetaClass ownerClass, FileInputTemplateNode fitn )
        {
            var nlist = fitn.nameList;
            FileMeta fm = fitn.fileMeta;
            MetaNode mn = GetMetaClassByNameAndFileMeta(ownerClass, fitn.fileMeta, nlist);
            if (mn == null)
            {
                var mb = mn.GetMetaClassByTemplateCount(fitn.inputTemplateCount);
                if (mb != null)
                {
                    return mb;
                }
            }
            return null;
        }
        
        #region 模板类处理区 该区先识别当前类， 再识别是否带模板输入，如果带则拿模板类
        public MetaClass GetMetaClassAndRegisterExptendTemplateClassInstance( MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaNode getmc = GetMetaClassByRef(curMc, fmcd );
            if (getmc == null)
            {
                Log.AddInStructMeta(EError.StructMetaStart, " CheckExtendAndInterface 在判断继承的时候，发没的:" + fmcd.allName + "  类");
                //    + "位置行: " + m_ExtendClass.token.sourceBeginLine.ToString() );

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
