//****************************************************************************
//  File:      ClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
                case "Single":
                    return CoreMetaClassManager.floatMetaClass;
                case "Double":
                    return CoreMetaClassManager.doubleMetaClass;
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
            MetaBase topLevelNamespace = mm;
            if (topLevelNamespace == null)
            {
                topLevelNamespace = ModuleManager.instance.selfModule;
            }
            topLevelNamespace.AddMetaBase(mc.name, mc);
            return true;
        }
        public void AddGenTemplateClass(MetaGenTemplateClass mc )
        {
            var find1 = mc.metaGenTemplateClassList.Find(a => a == mc);
            if( find1 == null )
            {
                mc.metaGenTemplateClassList.Add(mc);
                //m_GenTemplateClassList.Add(mc);
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

            m_AllClassDict.Add(dc.allName, dc);
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
        public MetaClass FindMetaClass( List<MetaClass> mcList, string name)
        {
            var find1 = mcList.Find(x => x.name == name );

            return find1;
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
            MetaNamespace finalTopMetaNamespace = null;
            MetaModule finalTopMetaModule = ModuleManager.instance.selfModule;
            MetaClass finalTopMetaClass = null;
            FileMetaClass topLevelClass = fmc.topLevelFileMetaClass;
            MetaClass findTemplateParentMetaClass = null;
            bool isCreateTemplateClass = false;
            if ( topLevelClass != null )
            {
                if(fmc.isPartial )
                {
                    Debug.Write("类:" + fmc.name + "在: " + fmc.token.ToAllString() + "不支持内部嵌套类定义并行!!");
                    return null;
                }
                
                if( topLevelClass.metaClass == null )
                {
                    Debug.Write("Error 上级类中的MetaClass没有绑定!!");
                    return null;
                }

                var findmc = FindMetaClass(topLevelClass.metaClass.metaClassList, fmc.name );
                if (findmc != null)
                {
                    MetaClass findmc2 = findmc as MetaClass;
                    if( findmc2 != null )
                    {
                        if( findmc2.classDefineType == EClassDefineType.StructDefine )
                        {
                            findmc2.BindFileMetaClass(fmc);
                            findmc2.SetClassDefineType(EClassDefineType.CodeDefine);
                            findmc2.ParseFileMetaClassTemplate(fmc);
                            findmc2.ParseFileMetaClassMemeberVarAndFunc(fmc);
                            return findmc2;
                        }
                    }
                    else
                    {
                        Debug.Write("Error 查到内部不是内部内，可能有相同成员");
                        return null;
                    }
                }
                else
                {
                    finalTopMetaClass = topLevelClass.metaClass;
                    isCanAddBind = true;
                }
            }
            else
            {
                if(fmc.topLevelFileMetaNamespace != null )
                {
                    finalTopMetaNamespace = NamespaceManager.instance.SearchFinalNamespace(fmc.topLevelFileMetaNamespace);

                    if( fmc.namespaceBlock?.namespaceList.Count > 0 )
                    {
                        finalTopMetaNamespace = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock, finalTopMetaNamespace);
                    }
                }

                if (finalTopMetaNamespace == null && fmc.namespaceBlock?.namespaceList?.Count > 0 )
                {
                    finalTopMetaNamespace = NamespaceManager.instance.FindFinalMetaNamespaceByNSBlock(fmc.namespaceBlock);
                   
                    if (finalTopMetaNamespace == null )
                    {
                        Debug.Write("命名空间中，已定义其它非命名空间的类型 !!");
                        return null;
                    }
                }
                MetaBase mbb = null;
                if (finalTopMetaNamespace != null)
                {
                    mbb = FindMetaClass(finalTopMetaNamespace.metaClassList, fmc.name );
                }
                else
                if( finalTopMetaModule != null)
                {
                    mbb = FindMetaClass(finalTopMetaModule.metaClassList, fmc.name );
                }
                var amc = mbb as MetaClass;
                var amn = mbb as MetaNamespace;
                if( amn != null )
                {
                    Debug.Write("已有命名空间的定义: ");
                    return null;
                }
                else if (amc != null)
                {
                    if (ProjectManager.useDefineNamespaceType == EUseDefineType.LimitUseProjectConfigNamespaceAndClass)
                    {
                        amc.BindFileMetaClass(fmc);
                        amc.SetClassDefineType(EClassDefineType.CodeDefine);
                        amc.ParseFileMetaClassTemplate(fmc);
                        var newmc2 = amc.ParseFileMetaClassTemplate(fmc);
                        amc.ParseFileMetaClassMemeberVarAndFunc(fmc);
                        return amc;
                    }
                    else
                    {
                        var findamc = amc.GetTemplateMetaClassByTemplateCount(fmc.templateDefineList.Count);
                        if ( findamc == null )
                        {
                            isCanAddBind = true;
                            findTemplateParentMetaClass = amc;
                            isCreateTemplateClass = true;
                        }
                        else
                        {
                            if (!fmc.isPartial)
                            {
                                Debug.Write("类:" + fmc.name + "在: " + fmc.token.ToAllString() + "不支持文件并行 定义类");
                                return null;
                            }
                            bool isPartial = true;
                            foreach (var v in amc.fileMetaClassDict)
                            {
                                if (v.Value.isPartial == false)
                                {
                                    isPartial = false;
                                    Debug.Write("类:" + amc.name + "在: " + v.Value.token.ToAllString() + "不支持文件并行 定义类");
                                    break;
                                }
                            }
                            if (isPartial == false)
                            {
                                return null;
                            }
                            amc.BindFileMetaClass(fmc);
                            return amc;
                        }
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
                    Debug.Write("Error 使用的强定制类节点的方式中，没有查找到相关的类，所以不允许定义该类，请先在工程中定义类");
                }
                MetaClass newmc = null;
                if (fmc.isEnum)
                {
                    MetaEnum newme = new MetaEnum(fmc.name);
                    AddInitHandleMetaClassList(newme);
                    newme.BindFileMetaClass(fmc);
                    newme.ParseFileMetaEnumMemeberEnum(fmc);
                    newmc = newme;


                    if ( finalTopMetaClass != null)
                    {
                        finalTopMetaClass.AddChildrenMetaClass(newme);
                    }
                    else if (finalTopMetaNamespace != null)
                        finalTopMetaNamespace.AddMetaClass(newme);
                    else
                    {
                        finalTopMetaModule.AddMetaClass(newme);
                    }
                }
                else if (fmc.isData)
                {
                    var newmd = new MetaData( fmc );
                    AddInitHandleMetaClassList(newmd);
                    newmc = newmd;
                    newmd.BindFileMetaClass(fmc);
                    newmd.ParseFileMetaDataMemeberData(fmc);


                    if (finalTopMetaClass != null)
                    {
                        finalTopMetaClass.AddChildrenMetaClass(newmd);
                    }
                    else if (finalTopMetaNamespace != null)
                        finalTopMetaNamespace.AddMetaClass(newmd);
                    else
                    {
                        finalTopMetaModule.AddMetaClass(newmd);
                    }
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Debug.Write("Class 中，使用关键字，不允许使用Const");
                        return null;
                    }
                    if (isCreateTemplateClass)
                    {
                        newmc = findTemplateParentMetaClass;
                        newmc = newmc.ParseFileMetaClassTemplate(fmc);
                        newmc.BindFileMetaClass(fmc);
                        newmc.ParseFileMetaClassMemeberVarAndFunc(fmc);
                    }
                    else
                    {
                        newmc = new MetaClass(fmc.name);
                        newmc.BindFileMetaClass(fmc);
                        newmc.SetClassDefineType(EClassDefineType.CodeDefine);
                        var newmc2 = newmc.ParseFileMetaClassTemplate(fmc);
                        AddInitHandleMetaClassList(newmc2);
                        newmc2.ParseFileMetaClassMemeberVarAndFunc(fmc);
                        if (finalTopMetaClass != null)
                        {
                            finalTopMetaClass.AddChildrenMetaClass(newmc);
                        }
                        else if (finalTopMetaNamespace != null)
                            finalTopMetaNamespace.AddMetaClass(newmc);
                        else
                        {
                            finalTopMetaModule.AddMetaClass(newmc);
                        }
                    }
                }

                return newmc;
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
                Log.AddInStructMeta(EError.AddClassNameSame, $"已包含类:{mc.allName} 又进行了重进添加!");
                return;
            }
            m_AllClassDict.Add(acn, mc);
        }
        public void HandleExtendData()
        {
            m_InitHandleMetaClassList.Sort((x, y) => x.extendLevel - y.extendLevel);
            
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.HandleExtendData();
            }
        }
        public void ParseInitMetaClassList()
        {
            foreach (var it in m_InitHandleMetaClassList )
            {
                it.ParseExtendsRelation();
                it.UpdateInterfaceMetaClass();
                it.ParseMemberVariableDefineMetaType();
                it.ParseMemberFunctionDefineMetaType();
                AddDictMetaClass(it);
            }
        }
        public void ParseDefineMetaTypeGenTemplateMetaClassList()
        {
            var list = new List<MetaGenTemplateClass>(m_NeedHandleTemplateMetaClassList);
            m_NeedHandleTemplateMetaClassList.Clear();
            foreach (var it in list)
            {
                it.ParseMemberVariableDefineMetaType();
                it.ParseMemberFunctionDefineMetaType();
                AddMetaGenTemplateClassList(it);
            }
        }
        public void ParseGenTemplateMetaClassList()
        {
            if (m_NeedHandleTemplateMetaClassList.Count==0) return;

            var list = new List<MetaGenTemplateClass>(m_NeedHandleTemplateMetaClassList);
            m_NeedHandleTemplateMetaClassList.Clear();
            foreach (var it in list)
            {
                it.Parse();
                AddMetaGenTemplateClassList(it);
            }
        }
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
        public void HandleExtendContent( FileMetaClass mc )
        {
            if (mc.metaClass == null) return;

            bool isSuccess = true;
            for (int i = 0; i < mc.metaClass.interfaceClass.Count; i++ )
            {
                var interfaceClass = mc.metaClass.interfaceClass[i];

                List<MetaMemberFunction> interfaceFunctionList = interfaceClass.GetMemberInterfaceFunction();

                for( int j = 0; j < interfaceFunctionList.Count; j++ )
                {
                    var func = interfaceFunctionList[j];

                    if( !mc.metaClass.GetMemberInterfaceFunctionByFunc(func) )
                    {
                        Debug.Write("查找接口类中的要实现的函数，实现失败函数名称" + func.name + " Token位置: " );
                        //func.fileMetaMemberFunction.token.sourceBeginLine.ToString()
                        isSuccess = false;
                        break;
                    }
                }
            }
            var list = mc.metaClass.GetMemberFunctionList();
            for ( int i = 0; i < list.Count; i++ )
            {
                var func = list[i];
                if( func.isOverrideFunction )
                {

                }
            }
            if( !isSuccess )
            {
                return;
            }

            Stack<MetaClass> metaClassStack = new Stack<MetaClass>();

            var textendClass = mc.metaClass;
            while( true )
            {
                if (textendClass != null)
                {
                    metaClassStack.Push(mc.metaClass);
                    textendClass = textendClass.extendClass;
                }
                else
                    break;
            }
            bool isFailed = false;
            while ( true )
            {
                textendClass = metaClassStack.Pop();
                if (metaClassStack.Count <= 0)
                    break;
                if( !textendClass.isHandleExtendVariableDirty )
                {
                    textendClass.HandleExtendClassVariable();
                }

                isFailed = false;
                if( textendClass != null && textendClass.extendClass != null )
                {
                    foreach( var v in textendClass.metaExtendMemeberVariableDict )
                    {
                        if( textendClass.metaMemberVariableDict.ContainsKey( v.Key ) )
                        {
                            Debug.Write("Error 在类的值: " + v.Key + "  有重复定义: " + textendClass.allName + "中，值: [" + v.Key + "] Token1位置: "
                                + textendClass.metaMemberVariableDict[v.Key].ToTokenString());
                            isFailed = true;
                            break;
                        }
                    }
                }
                if (isFailed) break;
               
            }
            if( !isFailed )
            {
                //Debug.Write("");
            }
        }
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
        public MetaClass GetMetaClassByRef( MetaClass mc, FileMetaClassDefine fmcv )
        {
            if (fmcv == null) return null;

            MetaClass mc2 = mc.GetTreeStructNode();

            MetaClass mb = GetMetaClassByClassDefine(mc2, fmcv);
            if (mb != null)
                return mb;

            var mb2 = fmcv.fileMeta.GetMetaBaseByFileMetaClassRef(fmcv);
            if (mb2 is MetaClass mb22 )
            {
                return mb22;
            }  
            return null;
        }
        public MetaClass GetMetaClassByClassDefine( MetaClass ownerClass, FileMetaClassDefine fmcd)
        {
            return GetMetaClassByNameAndFileMeta(ownerClass, fmcd.fileMeta, fmcd.stringList );
        }
        // 在ownerClass类中，通过当前的ownerClass的父节点逐查，直到没有父节点，如果找到了当前的节点后，开始往stringList下边找
        private MetaClass GetMetaClassByListString( MetaClass ownerClass, List<string> stringList )
        {
            if (stringList.Count == 0)
                return null;

            string firstName = "";
            if ( stringList.Count == 1 )
            {
                firstName = stringList[0];
            }
            MetaBase findMB = CoreMetaClassManager.GetCoreMetaClass(firstName);
            if (findMB is MetaClass mc )
            {
                return mc;
            }

            MetaBase mb = ModuleManager.instance.selfModule;
            if( ownerClass != null )
            {
                mb = ownerClass;
            }
            while (true)
            {
                MetaBase parentMB = mb;
                for (int i = 0; i < stringList.Count; i++)
                {
                    string name = stringList[i];
                    if (parentMB != null)
                    {
                        if (findMB == null)
                        {
                            if (parentMB is MetaNamespace)
                            {
                                findMB = (parentMB as MetaNamespace).GetChildrenMetaBaseByName(name);
                            }
                            else if (parentMB is MetaClass)
                            {
                                findMB = (parentMB as MetaClass).GetChildrenMetaBaseByName(name);
                            }
                            else if( parentMB is MetaModule )
                            {
                                findMB = (parentMB as MetaModule).GetChildrenMetaBaseByName(name);
                            }
                            if (findMB == null)
                            {
                                parentMB = null;
                                break;
                            }
                            parentMB = findMB;
                        }
                        else
                        {
                            parentMB = parentMB.GetChildrenMetaBaseByName(name);
                        }
                    }
                }
                if (parentMB != null)
                {
                    if (parentMB is MetaClass)
                        return parentMB as MetaClass;
                }
                mb = mb.parentNode;
                if (mb == null)
                    break;
            }
            return null;
        }
        public MetaClass GetMetaClassByNameAndFileMeta(MetaClass ownerClass, FileMeta fm, List<string> stringList )
        {
            var newownerclass = ownerClass;
            if (ownerClass.isTemplateClass) 
            { 
                newownerclass = ownerClass.templateParentClass; 
            }
            else if (ownerClass is MetaGenTemplateClass mgtc)
            {
                newownerclass = mgtc.metaTemplateClass.templateParentClass;
            }

            MetaClass mc = GetMetaClassByListString(newownerclass, stringList);

            if( mc == null )
            {
                var mb = fm.GetMetaBaseFileMetaClass(stringList);

                if( mb is MetaClass mc2 )
                {
                    return mc2;
                }
            }
            return mc;
        }
        public MetaClass GetMetaClassByClassDefineAndFileMeta( MetaClass ownerClass, FileMetaClassDefine fmcd )
        {
            FileMeta fm = fmcd.fileMeta;
            MetaClass mc = GetMetaClassByClassDefine(ownerClass, fmcd);
            if( mc == null )
            {
                var mb = fm.GetMetaBaseByFileMetaClassRef(fmcd);
                if (mb != null)
                {
                    if (mb is MetaNamespace)
                    {
                        Debug.Write("找到了已有命名空间而不是要继承的类!!");
                        return null;
                    }
                    else if (mb is MetaClass)
                    {
                        return mb as MetaClass;
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
            MetaClass mc = GetMetaClassByNameAndFileMeta( ownerClass, fitn.fileMeta, nlist );
            if (mc == null)
            {
                var mb = fm.GetMetaBaseFileMetaClass(nlist);
                if (mb != null)
                {
                    if (mb is MetaNamespace)
                    {
                        Debug.Write("找到了已有命名空间而不是要继承的类!!");
                        return null;
                    }
                    else if (mb is MetaClass)
                    {
                        mc = mb as MetaClass;
                    }
                }
            }
            return mc;
        }
        //public MetaType GetMetaTemplateClassAndRegisterExptendTemplateFunction(MetaFunction mf, FileMetaClassDefine fmcd)
        //{
        //    if (fmcd == null) return null;

        //    MetaClass getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
        //    if (getmc == null)
        //    {
        //        Log.AddInStructMeta(EError.StructMetaStart, " CheckExtendAndInterface 在判断继承的时候，发没的:" + fmcd.allName + "  类");
        //        //    + "位置行: " + m_ExtendClass.token.sourceBeginLine.ToString() );

        //    }
        //    else
        //    {
        //        return GetMetaTemplateClassByTemplateList(curMc, getmc, fmcd.inputTemplateNodeList);
        //    }
        //    return null;
        //}
        
        #region 模板类处理区 该区先识别当前类， 再识别是否带模板输入，如果带则拿模板类
        public MetaClass GetMetaClassAndRegisterExptendTemplateClassInstance( MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaClass getmc = GetMetaClassByRef(curMc, fmcd );
            if (getmc == null)
            {
                Log.AddInStructMeta(EError.StructMetaStart, " CheckExtendAndInterface 在判断继承的时候，发没的:" + fmcd.allName + "  类");
                //    + "位置行: " + m_ExtendClass.token.sourceBeginLine.ToString() );

            }
            else
            {
                getmc = GetMetaClassAndRegisterExpendTemplateClassInstanceByTemplateList(curMc, getmc, fmcd.inputTemplateNodeList);
            }
            return getmc;
        }
        public MetaClass GetMetaClassAndRegisterExpendTemplateClassInstanceByTemplateList( MetaClass curMc, MetaClass getmc, List<FileInputTemplateNode> inputTemplateNodeList )
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return getmc;
            }
            var curMc2 = curMc.GetTreeStructNode();

            var findfn = getmc.GetTemplateMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if( findfn == null )
            {
                Log.AddInStructMeta(EError.None, $"在查找{getmc.name}的模板类{inputTemplateNodeList.Count} 时没有发现相对应的模板类!");
                return getmc;
            }
            getmc = findfn;
            List<MetaClass> regMCList = new List<MetaClass>();
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateClass(curMc2, inputTemplateNodeList[i]);
                regMCList.Add(t);
            }
            if (findfn != null)
            {
                bool isNeedReg = true;
                for (int i = 0; i < regMCList.Count; i++)
                {
                    if (regMCList[i] == null)
                    {
                        isNeedReg = false;
                        break;
                    }
                }
                if (isNeedReg)
                {
                    getmc = findfn.AddInstanceMetaClass(regMCList);
                }
            }
            return getmc;
        }
        public MetaClass RegisterTemplateDefineMetaTemplateClass( MetaClass ownerMc, FileInputTemplateNode fmtd)
        {
            var ownerMc2 = ownerMc.GetTreeStructNode();

            var newmc = GetMetaClassByNameAndFileMeta(ownerMc2, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                if (fmtd.inputTemplateCount == 0)
                {
                    return newmc;
                }
                var findfn = newmc.GetTemplateMetaClassByTemplateCount(fmtd.inputTemplateCount);
                if (findfn == null)
                {
                    Log.AddInStructMeta(EError.None, $"在查找{newmc.name}的模板类{fmtd.inputTemplateCount} 时没有发现相对应的模板类!");
                    return null;
                }
                newmc = findfn;
                List<MetaClass> regMCList = new List<MetaClass>();
                //这里，要注册实体模板类
                for (int i = 0; i < fmtd.defineClassCallLink.callNodeList.Count; i++)
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[i];

                    for (int j = 0; j < dcc.inputTemplateNodeList.Count; j++)
                    {
                        var itn = dcc.inputTemplateNodeList[j];
                        var t = RegisterTemplateDefineMetaTemplateClass(ownerMc, itn);
                        regMCList.Add(t);
                    }
                }
                if (findfn != null)
                {
                    bool isNeedReg = true;
                    for (int i = 0; i < regMCList.Count; i++)
                    {
                        if (regMCList[i] == null)
                        {
                            isNeedReg = false;
                            break;
                        }
                    }
                    if (isNeedReg)
                    {
                        newmc = findfn.AddInstanceMetaClass(regMCList);
                    }
                }
                if (newmc != null)
                {
                    return newmc;
                }
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    if(ownerMc is MetaGenTemplateClass mgtc )
                    {
                        var mgtc2 = mgtc.GetMetaGenTemplate(fmtd.nameList[0]);
                        if (mgtc2 != null)
                        {
                            return mgtc2.metaType.metaClass;
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, $"没有找到相对应的nameList[0]的名称{fmtd.nameList[0]}");
                        }
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "在找类定义时发生错误，nameList>1");
                }
            }
            return null;
        }
        #endregion
        #region 模板函数处理区
        public MetaClass GetMetaClassAndRegisterExptendTemplateFunctionClassInstance(MetaClass curMc, MetaGenTempalteFunction mgtf, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaClass getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var mgtc = (curMc as MetaGenTemplateClass).GetMetaGenTemplate(fmcd.stringList[0]);
                if( mgtc != null)
                {
                    return mgtc.metaType.metaClass;
                }
                else
                {
                    var gmgt2 = mgtf.GetMetaGenTemplate(fmcd.stringList[0]);
                    if ( gmgt2 != null )
                    {
                        return gmgt2.metaType.metaClass;
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "没有找到相关的模板或者是定义!");
                    }
                }

            }
            else
            {
                getmc = GetMetaClassAndRegisterExpendTemplateFunctionInstanceByTemplateList(curMc, getmc, mgtf, fmcd.inputTemplateNodeList);
            }
            return getmc;
        }
        public MetaClass GetMetaClassAndRegisterExpendTemplateFunctionInstanceByTemplateList(MetaClass curMc, MetaClass getmc, MetaGenTempalteFunction mgtf, List<FileInputTemplateNode> inputTemplateNodeList)
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return getmc;
            }
            var findfn = getmc.GetTemplateMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn != null)
            {
                getmc = findfn;
            }
            List<MetaClass> regMCList = new List<MetaClass>();
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateFunction(curMc, mgtf, inputTemplateNodeList[i]);
                regMCList.Add(t);
            }
            if (findfn != null)
            {
                bool isNeedReg = true;
                for (int i = 0; i < regMCList.Count; i++)
                {
                    if (regMCList[i] == null)
                    {
                        isNeedReg = false;
                        break;
                    }
                }
                if (isNeedReg)
                {
                    getmc = findfn.AddInstanceMetaClass(regMCList);
                }
            }
            return getmc;
        }
        public MetaClass RegisterTemplateDefineMetaTemplateFunction(MetaClass ownerMc, MetaGenTempalteFunction mgtf, FileInputTemplateNode fmtd)
        {
            var newmc = GetMetaClassByNameAndFileMeta(ownerMc, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                if (fmtd.inputTemplateCount == 0)
                {
                    return newmc;
                }
                var findfn = newmc.GetTemplateMetaClassByTemplateCount(fmtd.inputTemplateCount);

                List<MetaClass> regMCList = new List<MetaClass>();
                //这里，要注册实体模板类
                for (int i = 0; i < fmtd.defineClassCallLink.callNodeList.Count; i++)
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[i];

                    for (int j = 0; j < dcc.inputTemplateNodeList.Count; j++)
                    {
                        var itn = dcc.inputTemplateNodeList[j];
                        var t = RegisterTemplateDefineMetaTemplateFunction(ownerMc, mgtf, itn);
                        regMCList.Add(t);
                    }
                }
                if (findfn != null)
                {
                    bool isNeedReg = true;
                    for (int i = 0; i < regMCList.Count; i++)
                    {
                        if (regMCList[i] == null)
                        {
                            isNeedReg = false;
                            break;
                        }
                    }
                    if (isNeedReg)
                    {
                        newmc = findfn.AddInstanceMetaClass(regMCList);
                    }
                }               
                return newmc;
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    var mgtc = (ownerMc as MetaGenTemplateClass).GetMetaGenTemplate(fmtd.nameList[0]);
                    if (mgtc != null)
                    {
                        return mgtc.metaType.metaClass;
                    }
                    else
                    {
                        var gmgt2 = mgtf.GetMetaGenTemplate(fmtd.nameList[0]);
                        if (gmgt2 != null)
                        {
                            return gmgt2.metaType.metaClass;
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "没有找到相关的模板或者是定义!1324");
                        }
                    }
                }
            }
            return null;
        }

        #endregion
        public void PrintAllClassName()
        {
            Debug.Write("---------------ClassBegin-----------" + Environment.NewLine);
            Debug.Write(ToAllClassName());
            Debug.Write("--------------ClassEnd-------------" + Environment.NewLine);
        }
        public string ToAllClassName()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in m_AllClassDict )
            {
                sb.Append("class " + v.Key + Environment.NewLine);
            }
            return sb.ToString();
        }
        public void PrintAlllClassContent()
        {
            Debug.Write("---------------ClassBegin-----------" + Environment.NewLine);
            Debug.Write(ToAllClassContent());
            Debug.Write("--------------ClassEnd-------------" + Environment.NewLine);
        }
        public string ToAllClassContent()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in m_AllClassDict)
            {
                var c = v.Value as MetaClass;
                if( c == null )
                {
                    Debug.Write("Errrorrrrrrr!!!");
                    continue;
                }
                c.SetAnchorDeep(c.deep);

                if( c.classDefineType == EClassDefineType.CodeDefine )
                {
                    sb.Append(v.Value.GetFormatString(true));
                }

            }
            return sb.ToString();
        }
    }
}
