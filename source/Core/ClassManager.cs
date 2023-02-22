using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
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
        public List<MetaClass> dynamicClassList => m_DynamicClassList;


        private Dictionary<string, MetaClass> m_AllClassDict = new Dictionary<string, MetaClass>();
        private List<MetaClass> m_DynamicClassList = new List<MetaClass>();

        private Dictionary<string, MetaData> m_AllDataDict = new Dictionary<string, MetaData>();

        public MetaClass GetClassByName(string name)
        {
            if (m_AllClassDict.ContainsKey(name))
                return m_AllClassDict[name];
            return null;
        }
        public MetaClass GetMetaClassByCSharpType(System.Type type)
        {
            string typeName = type.Name;
            switch (typeName)
            {
                case "Int16":
                    return CoreMetaClassManager.int16MetaClass;
                case "Int32":
                    return CoreMetaClassManager.int32MetaClass;
                case "Int64":
                    return CoreMetaClassManager.int64MetaClass;
                case "Float":
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
            AddDictMetaClass(mc);
            MetaBase topLevelNamespace = mm;
            if (topLevelNamespace == null)
            {
                topLevelNamespace = ModuleManager.instance.selfModule;
            }
            return topLevelNamespace.AddMetaBase(mc.name, mc) ;
        }
        public MetaClass FindDynamicClass( MetaClass dc )
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
        public bool AddDynamicClass( MetaClass dc )
        {
            m_DynamicClassList.Add(dc);

            m_AllClassDict.Add(dc.allName, dc);
            return true;
        }
        public bool CompareMetaClassMemberVariable( MetaClass curClass, MetaClass cpClass )
        {
            var curClassList = curClass.allMetaMemberVariableList;
            var cpClassList = cpClass.allMetaMemberVariableList;

            if(curClassList.Count == cpClassList.Count )
            {
                for( int i = 0; i < curClassList.Count; i++ )
                {
                    var curMV = curClassList[i];
                    var cpMV = cpClassList[i];
                    if( curMV.isConst == cpMV.isConst 
                        || curMV.isStatic == cpMV.isStatic 
                        || curMV.name == cpMV.name
                        || curMV.metaDefineType == cpMV.metaDefineType )
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
        public MetaClass FindDynamicClassByMetaType( MetaClass dc )
        {
            return null;
        }
        public MetaClass AddClass( FileMetaClass fmc )
        {
            bool isCanAddBind = false;
            MetaNamespace tmetaNamespace = null;
            MetaModule tmetaModule = null;
            MetaClass tmetaClass = null;
            FileMetaClass topLevelClass = fmc.topLevelFileMetaClass;
            if ( topLevelClass != null )
            {
                if(fmc.isPartial )
                {
                    Console.WriteLine("类:" + fmc.name + "在: " + fmc.token.ToAllString() + "不支持内部嵌套类定义并行!!");
                    return null;
                }
                
                if( topLevelClass.metaClass == null )
                {
                    Console.WriteLine("Error 上级类中的MetaClass没有绑定!!");
                    return null;
                }

                var findmc = topLevelClass.GetChildrenMetaBaseByName(fmc.name);
                if (findmc != null)
                {
                    Console.WriteLine("Error 查到内部不是内部内，可能有相同成员");
                    return null;
                }
                else
                {
                    tmetaClass = topLevelClass.metaClass;
                    isCanAddBind = true;
                }
            }
            else
            {
                MetaBase topLevelNamespace = ModuleManager.instance.selfModule;
                var lastMetaNamespace = fmc.GetLasatMetaNamespace();
                if( lastMetaNamespace != null )
                {
                    topLevelNamespace = lastMetaNamespace;
                }
                if (fmc.namespaceBlock != null )
                {
                    var nlist = fmc.namespaceBlock.namespaceList;
                    for (int i = 0; i < nlist.Count; i++)
                    {
                        topLevelNamespace = topLevelNamespace.GetChildrenMetaBaseByName(nlist[i]);
                        if (topLevelNamespace == null)
                        {
                            Console.WriteLine("Error 没有找到相当的命名空间!!!");
                            return null;
                        }
                    }
                }
                tmetaNamespace = topLevelNamespace as MetaNamespace;
                tmetaModule = topLevelNamespace as MetaModule;
                if (tmetaNamespace == null && tmetaModule == null )
                {
                    Console.WriteLine("命名空间中，已定义其它非命名空间的类型 !!");
                    return null;
                }
                var mbb = topLevelNamespace.GetChildrenMetaBaseByName(fmc.name);
                var amc = mbb as MetaClass;
                var amn = mbb as MetaNamespace;
                if( amn != null )
                {
                    Console.WriteLine("已有命名空间的定义: ");
                    return null;
                }
                else if (amc != null)
                {
                    if (!fmc.isPartial)
                    {
                        Console.WriteLine("类:" + fmc.name + "在: " + fmc.token.ToAllString() + "不支持文件并行 定义类");
                        return null;
                    }
                    bool isPartial = true;
                    foreach (var v in amc.fileMetaClassDict)
                    {
                        if (v.Value.isPartial == false)
                        {
                            isPartial = false;
                            Console.WriteLine("类:" + amc.name + "在: " + v.Value.token.ToAllString() + "不支持文件并行 定义类");
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
                else
                {
                    isCanAddBind = true;
                }
            }


            if( isCanAddBind ) 
            {
                MetaClass newmc = null;
                if (fmc.isEnum)
                {
                    MetaEnum newme = new MetaEnum(fmc.name, fmc.isConst);
                    newme.BindFileMetaClass(fmc);
                    newme.ParseFileMetaClassTemplate(fmc);
                    newme.ParseFileMetaEnumMemeberData(fmc);
                    newmc = newme;
                }
                else if (fmc.isData)
                {
                    var newmd = new MetaData(fmc.name, fmc.isConst);
                    newmc = newmd;
                    m_AllDataDict.Add(fmc.name, newmd);
                    newmd.BindFileMetaClass(fmc);
                    newmd.ParseFileMetaDataMemeberData(fmc);
                }
                else
                {
                    if (fmc.isConst)
                    {
                        Console.WriteLine("Class 中，使用关键字，不允许使用Const");
                        return null;
                    }
                    bool isCreateClass = true;
                    if (m_AllClassDict.ContainsKey(fmc.name))
                    {
                        var newmc2 = m_AllClassDict[fmc.name];
                        if (newmc2.isInnerDefineInCompile)
                        {
                            newmc = newmc2;
                            isCreateClass = false;
                        }
                    }

                    if (isCreateClass)
                    {
                        newmc = new MetaClass(fmc.name);
                        m_AllClassDict.Add(newmc.allName, newmc);

                        newmc.BindFileMetaClass(fmc);
                        newmc.ParseFileMetaClassTemplate(fmc);
                        newmc.ParseFileMetaClassMemeberVarAndFunc(fmc);
                    }
                    else
                    {
                        newmc.ParseFileMetaClassRewrite(fmc);
                    }
                }

                if(tmetaClass != null )
                {
                    tmetaClass.AddChildrenMetaClass(newmc);
                }
                else if (tmetaNamespace != null)
                    tmetaNamespace.AddMetaClass(newmc);
                else
                    tmetaModule.AddMetaClass(newmc);

                return newmc;
            }
            else
            {
                return null;
            }
        }       
        public void AddDictMetaClass( MetaClass mc )
        {
            if(m_AllClassDict.ContainsKey( mc.allName ) )
            {
                Console.WriteLine("Add Failed!!");
                return;
            }
            m_AllClassDict.Add(mc.allName, mc);
        }
        public void CheckExtendAndInterface( FileMetaClass mc )
        {
            if (mc.metaClass == null) return;

            if (mc.metaClass.extendClass != null)
            {
                Console.WriteLine("已绑定过了继承类 : " + mc.metaClass.extendClass.name);
                return;
            }
            MetaClass getmc = mc.GetExtendMetaClass();
            if (getmc != null)
            {
                mc.metaClass.SetExtendClass(getmc);
            }
            else
            {
                mc.metaClass.SetExtendClass(CoreMetaClassManager.objectMetaClass);
            }
            var ifmc = mc.GetInterfaceMetaClass();
            if (ifmc.Count > 0 )
            {
                for( int i = 0; i < ifmc.Count; i++ )
                {
                    mc.metaClass.AddInterfaceClass(ifmc[i]);
                }
            }
        }
        public void ParseFileMetaClass()
        {

        }
        public void Parse()
        {
            foreach( var it in m_AllClassDict )
            {
                it.Value.Parse();
            }
            foreach (var it in m_AllClassDict)
            {
                it.Value.ParseDefineComplete();
            }
        }

        public static bool IsNumberMetaClass( MetaClass mc )
        {
            switch( mc.eType )
            {
                case EType.Char:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                case EType.Int64:
                case EType.UInt64:
                case EType.Float:
                case EType.Double:
                    return true;
            }

            return false;
        }
        public static EClassRelation ValidateClassRelation( string curName, string compareName )
        {
            MetaClass currentClass = instance.GetClassByName(curName);
            if (currentClass == null)
            {
                return EClassRelation.CurClassError;
            }
            MetaClass compareClass = instance.GetClassByName(compareName);
            if (compareClass == null)
            {
                return EClassRelation.CompareClassError;
            }
            return ValidateClassRelationByMetaClass(currentClass, compareClass);
        }
        public static EClassRelation ValidateClassRelationByMetaClass( MetaClass curClass, MetaClass compareClass )
        {
            if( curClass == CoreMetaClassManager.objectMetaClass )
            {
                return EClassRelation.Child;
            }
            if (curClass.Equals(compareClass))
            {
                return EClassRelation.Same;
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
        public void HandleExtendContentAndInterfaceContent( FileMetaClass mc )
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
                        Console.WriteLine("查找接口类中的要实现的函数，实现失败函数名称" + func.name + " Token位置: " );
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
                            Console.WriteLine("Error 在类的值: " + v.Key + "  有重复定义: " + textendClass.allName + "中，值: [" + v.Key + "] Token1位置: "
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
                //Console.WriteLine("");
            }
        }
        public MetaClass GetMetaClassByName(string inputname, MetaClass ownerClass = null, FileMeta fm = null )
        {
            MetaClass fmc = CoreMetaClassManager.GetSelfMetaClass(inputname);
            if( fmc != null )
            {
                return fmc;
            }
            fmc = ClassManager.instance.GetClassByName(inputname);
            if (fmc != null)
            {
                return fmc;
            }

            if( ownerClass != null )
            {
                //子类
                MetaBase tmb = ownerClass.GetChildrenMetaBaseByName(inputname);
                if (tmb != null && tmb is MetaClass)
                {
                    return tmb as MetaClass;
                }
                //上级节点类或者是命名空间
                fmc = ownerClass.GetMetaBaseInParentNodeContainByName(inputname) as MetaClass;
                if( fmc != null )
                {
                    return fmc;
                }

            }
            //引入文件的类或者是命名空间
            if (fm == null)
            {
                fmc = fm.GetMetaBaseByName(inputname) as MetaClass;
            }

            return fmc;
        }
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
            FileMeta fm = fmcv.fileMeta;
            MetaClass mb = GetMetaClassByClassDefine(mc, fmcv);
            if (mb != null)
                return mb;
            
            var mb2 = fm.GetMetaBaseByFileMetaClassRef(fmcv);
            if (mb2 != null)
            {
                if (mb2 is MetaNamespace)
                {
                    return null;
                }
                else if (mb2 is MetaClass)
                {
                    return mb2 as MetaClass;
                }
            }
            else
            {
                mb2 = ClassManager.instance.GetClassByName(fmcv.allName);
                if (mb2 != null && mb2 is MetaClass)
                    return mb2 as MetaClass;
            }            
            return null;
        }
        public MetaClass GetMetaClassByClassDefine( MetaClass ownerClass, FileMetaClassDefine fmcd)
        {
            return GetMetaClassByListString(ownerClass, fmcd.stringList);
        }
        // 在ownerClass类中，通过当前的ownerClass的父节点逐查，直到没有父节点，如果找到了当前的节点后，开始往stringList下边找
        public MetaClass GetMetaClassByListString( MetaClass ownerClass, List<string> stringList)
        {
            if (stringList.Count == 0)
                return null;
            if (ownerClass == null)
                return null;

            MetaBase findMB = CoreMetaClassManager.GetSelfMetaClass(stringList[0]);
            if(findMB is MetaClass )
            {
                return findMB as MetaClass;
            }
            findMB = ClassManager.instance.GetClassByName(stringList[0]);
            if(findMB != null )
            {
                return findMB as MetaClass;
            }
            MetaBase mb = ownerClass;
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
        public MetaClass GetMetaClassByNameAndFileMeta(MetaClass ownerClass, string name, FileMeta fm)
        {
            MetaBase mb = ownerClass;
            var selfClass = CoreMetaClassManager.GetSelfMetaClass(name);
            if (selfClass != null)
                return selfClass;

            while (true)
            {
                MetaBase parentMB = mb;
                MetaBase rmb = null;
                if (parentMB != null)
                {
                    if (rmb == null)
                    {
                        if (parentMB is MetaNamespace)
                        {
                            rmb = (parentMB as MetaNamespace).GetChildrenMetaBaseByName(name);
                        }
                        else if (parentMB is MetaClass)
                        {
                            rmb = (parentMB as MetaClass).GetChildrenMetaClassByName(name); ;

                        }
                        if (rmb == null)
                        {
                            parentMB = null;
                            break;
                        }
                    }
                    parentMB = rmb;
                }

                if (parentMB != null)
                {
                    if (parentMB is MetaClass)
                        return parentMB as MetaClass;
                    else
                    {
                        Console.WriteLine(" Error 查找到元素，但不是类!!!");
                    }
                }
                mb = mb.parentNode;
                if (mb == null)
                    break;
            }

            MetaClass mb2 = fm.GetMetaBaseTByName<MetaClass>(name);


            return mb2;
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
                        Console.WriteLine("找到了已有命名空间而不是要继承的类!!");
                        return null;
                    }
                    else if (mb is MetaClass)
                    {
                        return mb as MetaClass;
                    }
                }
                else
                {
                    mb = ClassManager.instance.GetClassByName(fmcd.allName);
                    if (mb != null && mb is MetaClass)
                        return mb as MetaClass;
                }
            }
            return mc;
        }
        public MetaType GetMetaDefineTypeByInputTemplateAndFileMeta( MetaClass ownerClass, FileInputTemplateNode fmcd )
        {
            var nlist = fmcd.nameList;
            if( nlist.Count == 1 )
            {
                var retTemplate = ownerClass.GetTemplateMetaClassByName(nlist[0]);
                if( retTemplate != null )
                {
                    return new MetaType(retTemplate);
                }
            }

            FileMeta fm = fmcd.fileMeta;
            MetaClass mc = GetMetaClassByListString( ownerClass, nlist);
            if (mc == null)
            {
                var mb = fm.GetMetaBaseFileMetaClass(nlist);
                if (mb != null)
                {
                    if (mb is MetaNamespace)
                    {
                        Console.WriteLine("找到了已有命名空间而不是要继承的类!!");
                        return null;
                    }
                    else if (mb is MetaClass)
                    {
                        new MetaType( mb as MetaClass );
                    }
                }
            }
            return new MetaType(mc);
        }
        public void PrintAllClassName()
        {
            Console.Write("---------------ClassBegin-----------" + Environment.NewLine);
            Console.Write(ToAllClassName());
            Console.Write("--------------ClassEnd-------------" + Environment.NewLine);
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
            Console.Write("---------------ClassBegin-----------" + Environment.NewLine);
            Console.Write(ToAllClassContent());
            Console.Write("--------------ClassEnd-------------" + Environment.NewLine);
        }
        public string ToAllClassContent()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in m_AllClassDict)
            {
                var c = v.Value as MetaClass;
                if( c == null )
                {
                    Console.WriteLine("Errrorrrrrrr!!!");
                    continue;
                }
                c.SetAnchorDeep(c.deep);
                sb.Append(v.Value.GetFormatString( true ));
            }
            return sb.ToString();
        }
    }
}
