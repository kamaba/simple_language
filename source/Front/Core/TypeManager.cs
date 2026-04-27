//****************************************************************************
//  File:      TypeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.Core
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
    public class TypeManager
    {
        public static TypeManager instance = new TypeManager();

        private readonly Dictionary<string, MetaType> m_GlobalTypeAliasDict = new Dictionary<string, MetaType>();
        /// <summary>当前编译工程中由 .sp 的 Project 类体内 typealias 注册的别名（每次编译清空后重建）。</summary>
        private readonly Dictionary<string, MetaType> m_ProjectTypeAliasDict = new Dictionary<string, MetaType>();

        public bool AddGlobalTypeAlias(string aliasName, MetaType targetType)
        {
            if (string.IsNullOrEmpty(aliasName) || targetType == null)
                return false;

            if (m_GlobalTypeAliasDict.ContainsKey(aliasName))
                return false;

            m_GlobalTypeAliasDict.Add(aliasName, targetType);
            return true;
        }

        public bool TryGetGlobalTypeAlias(string aliasName, out MetaType targetType)
        {
            return m_GlobalTypeAliasDict.TryGetValue(aliasName, out targetType);
        }

        public void ClearProjectTypeAliases()
        {
            m_ProjectTypeAliasDict.Clear();
        }

        public bool AddProjectTypeAlias(string aliasName, MetaType targetType)
        {
            if (string.IsNullOrEmpty(aliasName) || targetType == null)
                return false;
            if (m_GlobalTypeAliasDict.ContainsKey(aliasName))
                return false;
            if (m_ProjectTypeAliasDict.ContainsKey(aliasName))
                return false;
            m_ProjectTypeAliasDict.Add(aliasName, targetType);
            return true;
        }

        public bool TryGetProjectTypeAlias(string aliasName, out MetaType targetType)
        {
            return m_ProjectTypeAliasDict.TryGetValue(aliasName, out targetType);
        }

        /// <summary>
        /// 解析简单类型名时的别名链：当前文件局部 typealias → 工程(.sp Project) typealias → 内置全局别名。
        /// </summary>
        public bool TryResolveTypeAlias(string aliasName, FileMeta fileMeta, out MetaType targetType)
        {
            targetType = null;
            if (!string.IsNullOrEmpty(aliasName) && fileMeta != null && fileMeta.TryGetFileTypeAlias(aliasName, out targetType))
                return true;
            if (TryGetProjectTypeAlias(aliasName, out targetType))
                return true;
            return TryGetGlobalTypeAlias(aliasName, out targetType);
        }

        /// <summary>
        /// 在所有源文件已通过 <see cref="FileMeta.CombineFileMeta"/> 创建类结构，且
        /// <see cref="ClassManager.ParseInitMetaClassListThroughInheritance"/> 已处理模板约束、extends/implements 与 extend 排序之后，
        /// 再解析并注册 typealias（工程级 + 文件级）；须在
        /// <see cref="ClassManager.ParseInitMetaClassListCollectMemberDefineMetaTypes"/> 之前调用。
        /// </summary>
        public void ResolveAllDeclaredTypeAliases(List<FileParse> fileParseList)
        {
            if (fileParseList == null) return;
            EnsureBuiltinGlobalTypeAliases();
            ClearProjectTypeAliases();

            // 工程级 typealias：多轮解析以支持别名之间的依赖
            for (int round = 0; round < 64; round++)
            {
                int added = 0;
                for (int fi = 0; fi < fileParseList.Count; fi++)
                {
                    var fm = fileParseList[fi]?.file;
                    if (fm == null) continue;
                    var list = fm.typeAliasDeclList;
                    for (int j = 0; j < list.Count; j++)
                    {
                        var decl = list[j];
                        if (!decl.IsProjectScope) continue;
                        if (TryGetProjectTypeAlias(decl.AliasName, out _))
                            continue;
                        var mt = GetMetaTypeByTemplateFunction(null, null, decl.TargetDefine);
                        if (mt == null)
                            continue;
                        if (AddProjectTypeAlias(decl.AliasName, mt))
                            added++;
                    }
                }
                if (added == 0)
                    break;
            }

            for (int fi = 0; fi < fileParseList.Count; fi++)
            {
                var fm = fileParseList[fi]?.file;
                if (fm == null) continue;
                fm.ClearResolvedFileTypeAliases();
                for (int round = 0; round < 64; round++)
                {
                    int added = 0;
                    var list = fm.typeAliasDeclList;
                    for (int j = 0; j < list.Count; j++)
                    {
                        var decl = list[j];
                        if (decl.IsProjectScope) continue;
                        if (fm.TryGetFileTypeAlias(decl.AliasName, out _))
                            continue;
                        var mt = GetMetaTypeByTemplateFunction(null, null, decl.TargetDefine);
                        if (mt == null)
                            continue;
                        fm.InternalSetFileTypeAlias(decl.AliasName, new MetaType(mt));
                        added++;
                    }
                    if (added == 0)
                        break;
                }
            }
        }

        /// <summary>
        /// 注册语言内置的全局类型别名（等价于在工程 Global 里写 typealias），任意编译单元可直接使用别名。
        /// 已存在同名别名时不覆盖。
        /// </summary>
        public void EnsureBuiltinGlobalTypeAliases()
        {
            AddGlobalTypeAlias("Byte", new MetaType(CoreMetaClassManager.uint8MetaClass));
            AddGlobalTypeAlias("SByte", new MetaType(CoreMetaClassManager.int8MetaClass));
            AddGlobalTypeAlias("UInt8Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.uint8MetaClass));
            AddGlobalTypeAlias("Int32Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.int32MetaClass));
            AddGlobalTypeAlias("UInt32Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.uint32MetaClass));
            AddGlobalTypeAlias("Int64Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.int64MetaClass));
            AddGlobalTypeAlias("Float32Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.float32MetaClass));
            AddGlobalTypeAlias("Float64Array", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.float64MetaClass));
            AddGlobalTypeAlias("StringArray", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.stringMetaClass));
            AddGlobalTypeAlias("ObjectArray", SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.objectMetaClass));
        }

        // 比较两个MetaType的内容， 主要通过 MetaClass 和里边的MetaType的遍历 都相同 

        public static bool IsCoreMetaType( MetaType mt )
        {
            if( mt.eMetaTypeType == EMetaTypeType.MetaClass )
            {
                var curClass = mt.metaClass;
                if (curClass == CoreMetaClassManager.uint8MetaClass
                    || curClass == CoreMetaClassManager.int8MetaClass
                    //|| curClass == CoreMetaClassManager.charMetaClass
                    || curClass == CoreMetaClassManager.int16MetaClass
                    || curClass == CoreMetaClassManager.uint16MetaClass
                    || curClass == CoreMetaClassManager.int32MetaClass
                    || curClass == CoreMetaClassManager.uint32MetaClass
                    || curClass == CoreMetaClassManager.int64MetaClass
                    || curClass == CoreMetaClassManager.uint64MetaClass
                    || curClass == CoreMetaClassManager.booleanMetaClass
                    || curClass == CoreMetaClassManager.stringMetaClass
                    || curClass == CoreMetaClassManager.arrayMetaClass )
                {
                    return true;
                }
            }
            return false;
        }
        public bool UpdateMetaTypeByGenClassAndFunction(MetaType mt, MetaGenTemplateClass mgtc, MetaGenTemplateFunction mgtf )
        {
            bool isNeedReg = false;
            MetaClass findfn = null;
            List<MetaClass> regMCList = new List<MetaClass>();
            if (mt.defineTemplateMetaTypeList.Count > 0)
            {
                //Debug.Assert(false, "");
                for (int i = 0; i < mt.defineTemplateMetaTypeList.Count; i++)
                {
                    MetaType regMt = new MetaType(mt.defineTemplateMetaTypeList[i]);
                    if (UpdateMetaTypeByGenClassAndFunction(regMt, mgtc, mgtf))
                    {
                        isNeedReg = true;
                    }
                    regMCList.Add(regMt.metaClass);
                }
            }
            if (isNeedReg)
            {
                var newmc = mt.metaClass.AddInstanceMetaClass(regMCList, true );
                if (newmc == null)
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL212, "MetaClass is Null");
                    return false;
                }
                mt.SetGenMetaClass(newmc);
                return true;
            }
            if (mt.isTemplate)
            {
                MetaGenTemplate gmgt = mgtc.GetMetaGenTemplate(mt.metaTemplate.name);
                if (gmgt != null)
                {

                    if (gmgt.metaType.metaClass == null)
                    {
                        Log.AddMetaCoreLog(LID.AutoTypeManagerL226, "MetaClass is Null");
                        return false;
                    }

                    mt.SetMetaClass(gmgt.metaType.metaClass);
                    //mt.SetGenMetaTemplate(gmgt);
                    findfn = gmgt.metaType.metaClass;
                }
                else
                {
                    gmgt = mgtf?.GetMetaGenTemplate(mt.metaTemplate.name);
                    if (gmgt != null)
                    {
                        if (gmgt.metaType.metaClass == null)
                        {
                            Log.AddMetaCoreLog(LID.AutoTypeManagerL241, "MetaClass is Null");
                            return false;
                        }
                        mt.SetMetaClass(gmgt.metaType.metaClass);
                        //mt.SetGenMetaTemplate(gmgt);
                        findfn = gmgt.metaType.metaClass;
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.AutoTypeManagerL250, "没有找到模板中定义的模板内容!" + mt.metaTemplate.name);
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }
        public static bool CompareMetaType(MetaType mdtL, MetaType mdtR)
        {
            if (mdtL == null || mdtR == null)
                return false;

            MetaClass leftBaseClass = mdtL.GetTemplateMetaClass();
            MetaClass rightBaseClass = mdtR.GetTemplateMetaClass();

            if (leftBaseClass != rightBaseClass)
            {
                return false;
            }

            List<MetaType> leftTemplateList = mdtL.GetGenTemplateMetaTypeList();
            List<MetaType> rightTemplateList = mdtR.GetGenTemplateMetaTypeList();

            if (leftTemplateList.Count != rightTemplateList.Count)
            {
                return false;
            }

            for (int i = 0; i < leftTemplateList.Count; i++)
            {
                var lv = leftTemplateList[i];
                var rv = rightTemplateList[i];
                if (!CompareMetaType(lv, rv))
                {
                    return false;
                }
            }

            return true;
        }

        public static EClassRelation ResolveAssignRelation(
            MetaType targetMetaType,
            MetaExpressNode expressNode,
            bool useTemplateExactMatch,
            bool allowEnumOwnerEqual,
            out MetaType expressRetMetaDefineType,
            out MetaClass curClass,
            out MetaClass compareClass,
            out bool isNullConstExpress,
            MetaVariable targetVariable = null)
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
                expressRetMetaDefineType = new MetaType(CoreMetaClassManager.nullMetaClass);
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

            // Iterator<Number> <- Array<具体数值>：仅遍历/访问语义，允许协变
            if (targetMetaType.IsIterator() && expressRetMetaDefineType.IsArray())
            {
                if (ClassManager.TryIteratorNumberFromConcreteNumericArray(targetMetaType, expressRetMetaDefineType))
                    return EClassRelation.Same;
            }

            // Iterator<Number> <- Iterator<具体数值>：仅遍历/访问语义，允许协变
            if (targetMetaType.IsIterator() && expressRetMetaDefineType.IsIterator())
            {
                if (ClassManager.TryIteratorNumberFromConcreteNumericIterator(targetMetaType, expressRetMetaDefineType))
                    return EClassRelation.Same;
            }

            if (targetMetaType.IsIterator())
            {
                if (ClassManager.TryIteratorNumberFromArrayIteratorSource(targetMetaType, expressNode))
                    return EClassRelation.Same;
            }

            // IIterable<Object> <- Int32[]：数组到可迭代接口按元素可赋值放宽
            if (targetMetaType.IsIterable() && expressRetMetaDefineType.IsArray())
            {
                if (ClassManager.TryIterableFromArrayElementAssignable(targetMetaType, expressRetMetaDefineType))
                    return EClassRelation.Same;
            }

            // 数组实体：不支持协变，必须完整类型一致（与 Dart 风格一致，仅接口侧放宽）。
            if (targetMetaType.IsArray() && expressRetMetaDefineType.IsArray())
            {
                if (TypeManager.CompareMetaType(targetMetaType, expressRetMetaDefineType))
                    return EClassRelation.Same;
                return EClassRelation.No;
            }

            if (TryGenericTemplateAssignRelation(targetMetaType, expressRetMetaDefineType, out var genericRelation))
            {
                return genericRelation;
            }

            return ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
        }

        private static bool HasTemplateArgs(MetaType mt)
        {
            if (mt == null) return false;
            var list = mt.GetGenTemplateMetaTypeList();
            return list != null && list.Count > 0;
        }

        private static bool IsCovariantTemplateArgAssignable(MetaType targetArg, MetaType exprArg)
        {
            if (targetArg == null || exprArg == null)
                return false;

            if (CompareMetaType(targetArg, exprArg))
                return true;

            var tClass = targetArg.GetTemplateMetaClass();
            var eClass = exprArg.GetTemplateMetaClass();
            if (tClass == null || eClass == null)
                return false;

            var relation = ClassManager.ValidateClassRelationByMetaClass(tClass, eClass);
            return relation == EClassRelation.Same
                || relation == EClassRelation.Child
                || relation == EClassRelation.Interface
                || relation == EClassRelation.Num;
        }

        private static bool IsSameTemplateBase(MetaType mt, MetaClass expected)
        {
            if (mt == null || expected == null)
                return false;

            var baseClass = mt.GetTemplateMetaClass();
            return baseClass == expected;
        }

        private static bool TryFindImplementedInterfaceMetaType(MetaType exprMetaType, MetaClass targetInterfaceClass, out MetaType implementedInterfaceMetaType)
        {
            implementedInterfaceMetaType = null;
            if (exprMetaType == null || targetInterfaceClass == null)
                return false;

            var queue = new Queue<MetaClass>();
            var visited = new HashSet<MetaClass>();
            if (exprMetaType.metaClass != null)
            {
                queue.Enqueue(exprMetaType.metaClass);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || !visited.Add(current))
                    continue;

                var interfaces = current.interfaceMetaType;
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.Count; i++)
                    {
                        var imt = interfaces[i];
                        if (IsSameTemplateBase(imt, targetInterfaceClass))
                        {
                            implementedInterfaceMetaType = imt;
                            return true;
                        }
                    }
                }

                if (current.extendClass != null)
                {
                    queue.Enqueue(current.extendClass);
                }
            }

            return false;
        }

        /// <summary>
        /// 泛型赋值规则（Front 侧）：
        /// 1) 非接口泛型：必须完整模板一致；
        /// 2) 接口泛型：允许按模板参数协变（targetArg 可接收 exprArg）。
        /// </summary>
        private static bool TryGenericTemplateAssignRelation(MetaType targetMetaType, MetaType exprMetaType, out EClassRelation relation)
        {
            relation = EClassRelation.None;
            if (targetMetaType == null || exprMetaType == null)
                return false;

            bool targetHasTemplate = HasTemplateArgs(targetMetaType);
            bool exprHasTemplate = HasTemplateArgs(exprMetaType);
            if (!targetHasTemplate && !exprHasTemplate)
                return false;

            if (CompareMetaType(targetMetaType, exprMetaType))
            {
                relation = EClassRelation.Same;
                return true;
            }

            var targetClass = targetMetaType.GetTemplateMetaClass();
            var exprClass = exprMetaType.GetTemplateMetaClass();
            if (targetClass == null || exprClass == null)
            {
                relation = EClassRelation.No;
                return true;
            }

            bool targetIsInterface = targetClass.isInterfaceClass;
            if (!targetIsInterface)
            {
                relation = EClassRelation.No;
                return true;
            }

            var targetArgs = targetMetaType.GetGenTemplateMetaTypeList();

            // 场景A：同一个接口定义（如 IIterator<Num> <- IIterator<Int32>）
            if (targetClass == exprClass)
            {
                var exprArgsSameInterface = exprMetaType.GetGenTemplateMetaTypeList();
                if (targetArgs.Count != exprArgsSameInterface.Count)
                {
                    relation = EClassRelation.No;
                    return true;
                }

                for (int i = 0; i < targetArgs.Count; i++)
                {
                    if (!IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsSameInterface[i]))
                    {
                        relation = EClassRelation.No;
                        return true;
                    }
                }

                relation = EClassRelation.Same;
                return true;
            }

            // 场景B：模板类实例实现了目标接口（如 IIterable<Object> <- Array<Int32>）
            if (TryFindImplementedInterfaceMetaType(exprMetaType, targetClass, out var implementedInterfaceMt))
            {
                var exprArgsFromInterface = implementedInterfaceMt.GetGenTemplateMetaTypeList();
                if (targetArgs.Count != exprArgsFromInterface.Count)
                {
                    relation = EClassRelation.No;
                    return true;
                }

                for (int i = 0; i < targetArgs.Count; i++)
                {
                    if (!IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsFromInterface[i]))
                    {
                        relation = EClassRelation.No;
                        return true;
                    }
                }

                relation = EClassRelation.Same;
                return true;
            }

            relation = EClassRelation.No;
            return true;
        }

        #region 模板类定义处理区
        public MetaType GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            if (fmcd.stringList != null && fmcd.stringList.Count == 1)
            {
                if (TryResolveTypeAlias(fmcd.stringList[0], fmcd.fileMeta, out MetaType aliasTarget) && aliasTarget != null)
                {
                    var retAlias = new MetaType(aliasTarget);
                    if (fmcd.isNullable)
                        retAlias.SetNullable(true);
                    if (fmcd.isArray)
                        retAlias = AddArrayTemplate(retAlias, fmcd.arrayDimsionLengthList);
                    return retAlias;
                }
            }

            MetaNode getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var mt = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (mt == null)
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL285, $"没有找到模板类中，对应的模板，名称为{fmcd.stringList[0]}请仔细检查模板的命名与使用模板命名是否对应");//, fmcd.classNameToken );
                }
                else
                {
                    var retmt = new MetaType(mt, fmcd.stringList[0] );
                    if (fmcd.isNullable)
                        retmt.SetNullable(true);
                    return retmt;
                }
            }
            else
            {
                var ret = GetMetaTypeByInputTemplateList(curMc, getmc, fmcd.inputTemplateNodeList);
                if (fmcd.isArray)
                {
                    var rarraymt = AddArrayTemplate(ret, fmcd.arrayDimsionLengthList);
                    if (fmcd.isNullable)
                        rarraymt.SetNullable(true);
                    return rarraymt;
                }
                if (fmcd.isNullable && ret != null)
                    ret.SetNullable(true);
                return ret;
            }
            return null;
        }
        public MetaType GetMetaTypeByInputTemplateList(MetaClass ownerMc, MetaNode getmc, List<FileInputTemplateNode> inputTemplateNodeList, List<MetaType> list = null )
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return new MetaType(getmc.GetMetaClassByTemplateCount(0));
            }
            var findfn = getmc.GetMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn == null)
            {
                return null;
            }
            if( inputTemplateNodeList.Count == 0 )
            {
                return new MetaType(findfn );
            }
            var mt = new MetaType();
            mt.SetTemplateMetaClass(findfn);
            //这里，要注册实体模板类            
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                MetaType mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(ownerMc, findfn, inputTemplateNodeList[i]);
                mt.AddDefineTemplateMetaType(new MetaType(mt2));
                //mt.AddGenTemplateMetaType(new MetaType(mt2));
            }
            
            return ownerMc.AddMetaPreTemplateClass(mt, false, out bool igmc);
        }
        MetaType GetAndRegisterTemplateDefineMetaTemplateClass(MetaClass ownerMc, MetaClass findMc, FileInputTemplateNode fmtd)
        {
            var newmn = ClassManager.instance.GetMetaClassByNameAndFileMeta(ownerMc, fmtd.fileMeta, fmtd.nameList);
            FileMetaCallNode cnode = null;
            if (newmn != null)
            {
                var findfn = newmn.GetMetaClassByTemplateCount(fmtd.inputTemplateCount);
                if(findfn == null )
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL347, $"没有发现{fmtd.nameList}找到的类!");
                    return null;
                }
                if( fmtd.inputTemplateCount == 0 )
                {
                    return new MetaType(findfn);
                }
                else
                {
                    var mt = new MetaType();
                    mt.SetTemplateMetaClass(findfn);
                    List<MetaGenTemplate> mgtList = new List<MetaGenTemplate>();
                    for (int i = 0; i < fmtd.defineClassCallLink.callNodeList.Count; i++)
                    {
                        var dcc = fmtd.defineClassCallLink.callNodeList[i];
                        for (int j = 0; j < dcc.inputTemplateNodeList.Count; j++)
                        {
                            var itn = dcc.inputTemplateNodeList[j];
                            var mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(ownerMc, findfn, itn);
                            mt.AddDefineTemplateMetaType(new MetaType(mt2));
                            //mt.AddGenTemplateMetaType(new MetaType(mt2) );
                            //if (mt2.isTemplate)
                            //{
                            //    isNeedReg = false;
                            //    MetaGenTemplate mgt = new MetaGenTemplate(mt2.metaTemplate);
                            //    mgtList.Add(mgt);
                            //}
                            //else if( mt2.metaClass != null )
                            //{
                            //    regMCList.Add(mt2.metaClass);
                            //}
                            //else
                            //{
                            //    isNeedReg = false;
                            //    var template = findfn.GetMetaTemplateByIndex(j);
                            //    MetaGenTemplate mgt = new MetaGenTemplate(template, mt2);
                            //    mgtList.Add(mgt);
                            //}
                        }
                    }

                    return ownerMc.AddMetaPreTemplateClass(mt, false, out bool igmc);
                }
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    var mt = ownerMc.GetMetaTemplateByName(fmtd.nameList[0]);
                    if (mt == null)
                    {
                        Log.AddMetaCoreLog(LID.AutoTypeManagerL398, "没有找到模板类中，对应的模板，请仔细检查模板的命名与使用模板命名是否对应");//, cnode?.token );
                    }
                    else
                    {
                        return new MetaType(mt, fmtd.nameList[0] );
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL407, "使用模板类中使用.连接符号，模板中不允许使用.");
                }
            }
            return null;
        }
        #endregion
        #region 模板函数处理区
        public MetaType GetMetaTypeByTemplateFunction(MetaClass curMc, MetaMemberFunction findFun, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            // typealias：文件局部 / 工程 / 内置
            if (fmcd.stringList != null && fmcd.stringList.Count == 1)
            {
                if (TryResolveTypeAlias(fmcd.stringList[0], fmcd.fileMeta, out MetaType aliasTarget) && aliasTarget != null)
                {
                    var retAlias = new MetaType(aliasTarget);
                    if (fmcd.isNullable)
                        retAlias.SetNullable(true);
                    if (fmcd.isArray)
                    {
                        var list = fmcd.arrayDimsionLengthList;
                        retAlias = AddArrayTemplate(retAlias, list);
                    }
                    return retAlias;
                }
            }

            MetaNode getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var gmtbn = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (gmtbn != null)
                {
                    var mt = new MetaType(gmtbn, fmcd.stringList[0] );
                    if (fmcd.isNullable) mt.SetNullable(true);
                    return mt;
                }
                else if (findFun != null)
                {
                    var mt = findFun.GetMetaDefineTemplateByName(fmcd.stringList[0]);
                    if( mt == null )
                    {
                        return null;
                    }
                    var ret = new MetaType(mt, fmcd.stringList[0]);
                    if (fmcd.isNullable) ret.SetNullable(true);
                    return ret;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL458, $"没有找到{fmcd.stringList[0]} 的相关类!");
                }

            }
            else
            {
                var ret =  GetMetaTypeByTemplateList(curMc, getmc, findFun, fmcd.inputTemplateNodeList);
                if (fmcd.isArray)
                {
                    var list = fmcd.arrayDimsionLengthList;
                    var rarraymt = AddArrayTemplate(ret, list);
                    return rarraymt;
                }
                return ret;
            }
            return null;
        }
        public MetaType AddArrayTemplate( MetaType arrayMt, List<int> list )
        {
            MetaType cmt = new MetaType(arrayMt.metaClass);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                MetaType mt = new MetaType();
                mt.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                MetaType dmt = new MetaType(cmt);
                mt.AddDefineTemplateMetaType(dmt);
                //mt.AddGenTemplateMetaType(dmt);

                cmt = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(mt, true, out bool igmc);
                cmt.SetArrayLength(list[i]);
            }
            return cmt;
        }
        public MetaType GetMetaTypeByTemplateList(MetaClass curMc, MetaNode getmc, MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList)
        {            
            var findfn = getmc.GetMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn != null)
            {
                if( inputTemplateNodeList.Count == 0 )
                {
                    return new MetaType(findfn);
                }

                var newmc = HandleInputTemplateNodeList(curMc, findfn, findFun, inputTemplateNodeList, false);
                if( newmc != null)
                {
                    return newmc;
                }
                else
                {
                    var mt = new MetaType();
                    mt.SetTemplateMetaClass(findfn);
                    return mt;
                }
            }

            return null;
        }
        public MetaType HandleInputTemplateNodeList(MetaClass findfn, MetaClass regMc,  MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList, bool isParse )
        {
            var getmc = findfn;
            MetaType mt = new MetaType();
            if( inputTemplateNodeList.Count == 0 )
            {
                mt.SetMetaClass(regMc);
                return mt;
            }
            mt.SetTemplateMetaClass(regMc);
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateFunction(findfn, findFun, inputTemplateNodeList[i], isParse );
                mt.AddDefineTemplateMetaType(t);
                //mt.AddGenTemplateMetaType(t);
            }
            mt = regMc.AddMetaPreTemplateClass(mt, isParse, out bool igmc);
            return mt;
        }
        public MetaType RegisterTemplateDefineMetaTemplateFunction(MetaClass findMc, MetaMemberFunction findFun, FileInputTemplateNode fmtd, bool isParse = false )
        {
            var newmc = ClassManager.instance.GetMetaClassByNameAndFileMeta(findMc, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                var findfn = newmc.GetMetaClassByTemplateCount(fmtd.inputTemplateCount);

                if (findfn == null)
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL545, "没有找到相对应的模板类!!");
                    return null;
                }
                if( fmtd.inputTemplateCount > 0 )
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[fmtd.defineClassCallLink.callNodeList.Count - 1];

                    var retmc = HandleInputTemplateNodeList(findMc, findfn, findFun, dcc.inputTemplateNodeList, isParse);

                    if( retmc != null )
                    {
                        return retmc;
                    }
                }
                else
                {
                    var mt = new MetaType(findfn);
                    return mt;
                }
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    if (findMc != null)
                    {
                        var mgtc2 = findMc.GetMetaTemplateByName(fmtd.nameList[0]);
                        if (mgtc2 != null)
                        {
                            return new MetaType(mgtc2, fmtd.nameList[0] );
                        }
                    }
                    if (findFun != null)
                    {
                        var mt = findFun.GetMetaDefineTemplateByName(fmtd.nameList[0]);
                        if (mt != null)
                        {
                            return new MetaType(mt, fmtd.nameList[0]);
                        }
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.AutoTypeManagerL588, "----fmtd.nameList.count > 1 ");
                }
            }
            return null;
        }
        #endregion
    }
}
