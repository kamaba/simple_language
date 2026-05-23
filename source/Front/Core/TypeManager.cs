//****************************************************************************
//  File:      TypeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
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
        public static bool TryAdjustConstExpressByDefineMetaType(MetaConstExpressNode mcen, MetaType defineMetaType)
        {
            if (mcen == null || defineMetaType == null)
            {
                return false;
            }

            var curEType = CoreMetaClassManager.GetETypeByMetaClass(defineMetaType.metaClass);

            if (curEType == EType.Object)
            {
                curEType = mcen.eType;
            }

            if (mcen.eType == curEType)
            {
                return true;
            }

            return TryAdjustConstExpressByDefineEType(mcen, curEType);
        }
        public static bool TryAdjustConstExpressByDefineEType(MetaConstExpressNode mcen, EType defineEType)
        {
            if (mcen == null)
            {
                return false;
            }

            if (defineEType == EType.Object)
            {
                return true;
            }

            var curEType = defineEType;
            var expEType = mcen.eType;
            Token token = mcen.token;

            if (expEType == EType.Null)
            {
                return true;
            }

            if (NumberManager.IsNumericEType(curEType) && NumberManager.IsNumericEType(expEType))
            {
                return NumberManager.TryAdjustConstExpressToNumericTarget(mcen, curEType, expEType, token);
            }

            if (expEType != curEType)
            {
                if (NumberManager.TryConvertConstValueByEType(curEType, mcen.value, out var convertedValue))
                {
                    mcen.SetConstValue(curEType, convertedValue);
                    return true;
                }

                if (NumberManager.IsRadixNumberLiteral(mcen)
                    && NumberManager.TryConvertRadixUnsignedToSignedByEType(curEType, mcen.value, out var radixConvertedValue))
                {
                    mcen.SetConstValue(curEType, radixConvertedValue);
                    return true;
                }

                Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, token, (mcen.value?.ToString() ?? "null"), curEType.ToString(), expEType.ToString());
                return false;
            }

            return true;
        }

        public static MetaType GetMaxCompatibleMetaTypeFromList(IReadOnlyList<MetaType> mtList)
        {
            var objMt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (mtList == null || mtList.Count == 0)
            {
                return objMt;
            }

            if (mtList.Count == 1)
            {
                var only = mtList[0];
                if (only == null || only.isNull)
                {
                    return objMt;
                }
                return new MetaType(only);
            }

            bool allNumeric = true;
            for (int i = 0; i < mtList.Count; i++)
            {
                var t = mtList[i];
                if (t == null || t.isNull)
                {
                    return objMt;
                }
                if (!ClassManager.IsNumberClass(t.metaClass))
                {
                    allNumeric = false;
                }
            }

            if (allNumeric)
            {
                bool hasInt64 = false;
                bool hasUInt64 = false;
                int maxRank = int.MinValue;
                for (int i = 0; i < mtList.Count; i++)
                {
                    var t = mtList[i];
                    if (t.metaClass == CoreMetaClassManager.int64MetaClass) hasInt64 = true;
                    else if (t.metaClass == CoreMetaClassManager.uint64MetaClass) hasUInt64 = true;

                    if (!NumberManager.TryGetLiteralPromotionRank(t.metaClass, out int rank))
                    {
                        return objMt;
                    }
                    if (rank > maxRank) maxRank = rank;
                }

                if (hasInt64 && hasUInt64)
                {
                    return objMt;
                }

                var promotedMc = NumberManager.GetMetaClassForLiteralPromotionRank(maxRank);
                return promotedMc != null ? new MetaType(promotedMc) : objMt;
            }

            var merged = new MetaType(mtList[0]);
            for (int i = 1; i < mtList.Count; i++)
            {
                var next = mtList[i];
                if (next == null || next.isNull)
                {
                    return objMt;
                }

                if (CompareMetaType(merged, next))
                {
                    continue;
                }

                if (merged.IsArray() && next.IsArray()
                    && MetaBraceAssignStatements.TryGetCompatibleArrayMetaType(merged, next, out var compatibleArrayMetaType))
                {
                    merged = compatibleArrayMetaType;
                    continue;
                }

                if (MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(merged, next))
                {
                    continue;
                }
                if (MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(next, merged))
                {
                    merged = new MetaType(next);
                    continue;
                }

                var left = merged.metaClass;
                var right = next.metaClass;
                if (left != null && right != null)
                {
                    var relation = ClassManager.ValidateClassTypeRelation(left, right);
                    if (relation == ETypeRelation.Child || relation == ETypeRelation.Interface)
                    {
                        merged = new MetaType(next);
                        continue;
                    }
                    if (relation == ETypeRelation.Parent)
                    {
                        continue;
                    }
                }

                return objMt;
            }

            return merged ?? objMt;
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
        /// 再解析并注册 typealias（工程级 + 文件级）；须在
        /// <see cref="ClassManager.ParseInitMetaClassListCollectMemberDefineMetaTypes"/> 之前调用。
        /// </summary>
        public void ResolveAllDeclaredTypeAliases(List<FileParse> fileParseList)
        {
            if (fileParseList == null) return;

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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "MetaClass is Null");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "MetaClass is Null");
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "MetaClass is Null");
                            return false;
                        }
                        mt.SetMetaClass(gmgt.metaType.metaClass);
                        //mt.SetGenMetaTemplate(gmgt);
                        findfn = gmgt.metaType.metaClass;
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到模板中定义的模板内容!" + mt.metaTemplate.name);
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool HasTemplateArgs(MetaType mt)
        {
            if (mt == null) return false;
            var list = mt.GetGenTemplateMetaTypeList();
            return list != null && list.Count > 0;
        }

        private static bool MetaTypeContainsTemplate(MetaType mt, MetaTemplate template)
        {
            if (mt == null || template == null)
                return false;

            if (mt.isTemplate && mt.metaTemplate == template)
                return true;

            var childList = mt.GetGenTemplateMetaTypeList();
            if (childList == null || childList.Count == 0)
                return false;

            for (int i = 0; i < childList.Count; i++)
            {
                if (MetaTypeContainsTemplate(childList[i], template))
                    return true;
            }
            return false;
        }

        private static bool IsSameTemplateBase(MetaType mt, MetaClass expected)
        {
            if (mt == null || expected == null)
                return false;

            return mt.GetTemplateMetaClass() == expected;
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

            var relation = ClassManager.ValidateClassTypeRelation(tClass, eClass);
            return relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Interface
                || relation == ETypeRelation.Num;
        }

        private static void CollectInterfaceTemplateUsage(MetaClass interfaceClass, MetaTemplate template, HashSet<MetaClass> visited, ref bool usedInInput)
        {
            if (interfaceClass == null || template == null || visited == null)
                return;
            if (!visited.Add(interfaceClass))
                return;

            var methods = interfaceClass.fileCollectMetaMemberFunctionList;
            if (methods != null)
            {
                for (int i = 0; i < methods.Count; i++)
                {
                    var mmf = methods[i];
                    if (mmf == null)
                        continue;

                    var paramList = mmf.metaMemberParamCollection?.metaDefineParamList;
                    if (paramList == null)
                        continue;

                    for (int pi = 0; pi < paramList.Count; pi++)
                    {
                        var pm = paramList[pi];
                        var pmt = pm?.metaVariable?.defineMetaType;
                        if (MetaTypeContainsTemplate(pmt, template))
                        {
                            usedInInput = true;
                            return;
                        }
                    }
                }
            }

            var inheritedInterfaceList = interfaceClass.interfaceMetaType;
            if (inheritedInterfaceList == null)
                return;
            for (int i = 0; i < inheritedInterfaceList.Count; i++)
            {
                var inherited = inheritedInterfaceList[i]?.GetTemplateMetaClass();
                if (inherited != null && inherited.isInterfaceClass)
                {
                    CollectInterfaceTemplateUsage(inherited, template, visited, ref usedInInput);
                    if (usedInInput)
                        return;
                }
            }
        }

        private static bool IsInterfaceTemplateArgCovariant(MetaClass interfaceClass, int argIndex)
        {
            if (interfaceClass == null || !interfaceClass.isInterfaceClass)
                return false;
            if (argIndex < 0 || argIndex >= interfaceClass.metaTemplateList.Count)
                return false;

            var template = interfaceClass.metaTemplateList[argIndex];
            if (template == null)
                return false;

            bool usedInInput = false;
            CollectInterfaceTemplateUsage(interfaceClass, template, new HashSet<MetaClass>(), ref usedInInput);
            return !usedInInput;
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
        /// 仅处理「两侧均有模板实参且属同基类/接口实现」的情形；否则返回 false，交由 <see cref="ClassManager.ValidateClassTypeRelation"/> 解析继承（如 Object ← Array&lt;T&gt;）。
        /// </summary>
        private static bool TryGenericTemplateAssignRelation(MetaType targetMetaType, MetaType exprMetaType, out ETypeRelation relation)
        {
            relation = ETypeRelation.None;
            if (targetMetaType == null || exprMetaType == null)
                return false;

            bool targetHasTemplate = HasTemplateArgs(targetMetaType);
            bool exprHasTemplate = HasTemplateArgs(exprMetaType);
            if (!targetHasTemplate && !exprHasTemplate)
                return false;

            if (CompareMetaType(targetMetaType, exprMetaType))
            {
                relation = ETypeRelation.Same;
                return true;
            }

            // 仅一侧带模板（Object vs Array&lt;T&gt; 等）——不在此处理，走类继承链。
            if (targetHasTemplate != exprHasTemplate)
                return false;

            var targetClass = targetMetaType.GetTemplateMetaClass();
            var exprClass = exprMetaType.GetTemplateMetaClass();
            if (targetClass == null || exprClass == null)
                return false;

            var targetArgs = targetMetaType.GetGenTemplateMetaTypeList();

            // 场景A：同一模板基类（如 IIterator&lt;Num&gt; ← IIterator&lt;Int32&gt;，或 List&lt;T&gt; 严格同参）
            if (targetClass == exprClass)
            {
                var exprArgsSameInterface = exprMetaType.GetGenTemplateMetaTypeList();
                if (targetArgs.Count != exprArgsSameInterface.Count)
                {
                    relation = ETypeRelation.No;
                    return true;
                }

                for (int i = 0; i < targetArgs.Count; i++)
                {
                    bool allowCovariant = targetClass.isInterfaceClass
                        && IsInterfaceTemplateArgCovariant(targetClass, i);
                    bool ok = allowCovariant
                        ? IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsSameInterface[i])
                        : CompareMetaType(targetArgs[i], exprArgsSameInterface[i]);
                    if (!ok)
                    {
                        relation = ETypeRelation.No;
                        return true;
                    }
                }

                relation = ETypeRelation.Same;
                return true;
            }

            // 场景B：表达式类型实现了目标接口（如 IIterable&lt;Object&gt; ← Array&lt;Int32&gt;）
            if (targetClass.isInterfaceClass
                && TryFindImplementedInterfaceMetaType(exprMetaType, targetClass, out var implementedInterfaceMt))
            {
                var exprArgsFromInterface = implementedInterfaceMt.GetGenTemplateMetaTypeList();
                if (targetArgs.Count != exprArgsFromInterface.Count)
                {
                    relation = ETypeRelation.No;
                    return true;
                }

                for (int i = 0; i < targetArgs.Count; i++)
                {
                    bool allowCovariant = IsInterfaceTemplateArgCovariant(targetClass, i);
                    bool ok = allowCovariant
                        ? IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsFromInterface[i])
                        : CompareMetaType(targetArgs[i], exprArgsFromInterface[i]);
                    if (!ok)
                    {
                        relation = ETypeRelation.No;
                        return true;
                    }
                }

                relation = ETypeRelation.Interface;
                return true;
            }

            // 不同模板基类且非接口实现路径 — 交回继承解析（Object/Array、无关泛型对等）。
            return false;
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"没有找到模板类中，对应的模板，名称为{fmcd.stringList[0]}请仔细检查模板的命名与使用模板命名是否对应");//, fmcd.classNameToken );
                }
                else
                {
                    var retmt = new MetaType(mt, fmcd.stringList[0]);
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
        public MetaType GetMetaTypeByInputTemplateList(MetaClass ownerMc, MetaNode getmc, List<FileInputTemplateNode> inputTemplateNodeList, List<MetaType> list = null)
        {
            if (getmc == null)
            {
                return null;
            }
            int tplCount = inputTemplateNodeList != null ? inputTemplateNodeList.Count : 0;
            if (getmc.isMetaData)
            {
                if (tplCount > 0)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "data 类型不支持模板实参");
                    return null;
                }
                return getmc.metaData != null ? new MetaType(getmc.metaData) : null;
            }
            if (getmc.isMetaEnum)
            {
                if (tplCount > 0)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "enum 类型不支持模板实参");
                    return null;
                }
                return getmc.metaEnum != null ? new MetaType(getmc.metaEnum) : null;
            }
            if (tplCount == 0)
            {
                var mc0 = getmc.GetMetaClassByTemplateCount(0);
                return mc0 != null ? new MetaType(mc0) : null;
            }
            var findfn = getmc.GetMetaClassByTemplateCount(tplCount);
            if (findfn == null)
            {
                return null;
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
                if (findfn == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"没有发现{fmtd.nameList}找到的类!");
                    return null;
                }
                if (fmtd.inputTemplateCount == 0)
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到模板类中，对应的模板，请仔细检查模板的命名与使用模板命名是否对应");//, cnode?.token );
                    }
                    else
                    {
                        return new MetaType(mt, fmtd.nameList[0]);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "使用模板类中使用.连接符号，模板中不允许使用.");
                }
            }
            return null;
        }
        #endregion
        #region 模板函数处理区
        public MetaType GetMetaTypeByTemplateFunction(MetaClass curMc, MetaMemberFunction findFun, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaType ApplyFileMetaClassDefineDecorations(MetaType mt)
            {
                if (mt == null)
                {
                    return null;
                }

                var retMt = new MetaType(mt);
                if (fmcd.isNullable)
                {
                    retMt.SetNullable(true);
                }
                if (fmcd.isArray)
                {
                    var list = fmcd.arrayDimsionLengthList;
                    retMt = AddArrayTemplate(retMt, list);
                }
                return retMt;
            }

            // typealias：文件局部 / 工程 / 内置
            if (fmcd.stringList != null && fmcd.stringList.Count == 1)
            {
                if (TryResolveTypeAlias(fmcd.stringList[0], fmcd.fileMeta, out MetaType aliasTarget) && aliasTarget != null)
                {
                    return ApplyFileMetaClassDefineDecorations(aliasTarget);
                }
            }

            MetaNode getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var gmtbn = curMc?.GetMetaTemplateByName(fmcd.stringList[0]);
                if (gmtbn != null)
                {
                    var mt = new MetaType(gmtbn, fmcd.stringList[0]);
                    return ApplyFileMetaClassDefineDecorations(mt);
                }
                else if (findFun != null)
                {
                    var mt = findFun.GetMetaDefineTemplateByName(fmcd.stringList[0]);
                    if (mt == null)
                    {
                        return null;
                    }
                    var ret = new MetaType(mt, fmcd.stringList[0]);
                    return ApplyFileMetaClassDefineDecorations(ret);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"没有找到{fmcd.stringList[0]} 的相关类!");
                }

            }
            else
            {
                if (getmc.isMetaData)
                {
                    return new MetaType(getmc.metaData);
                }
                else if (getmc.isMetaEnum)
                {
                    return new MetaType(getmc.metaEnum);
                }
                var ret = GetMetaTypeByTemplateList(curMc, getmc, findFun, fmcd.inputTemplateNodeList);
                return ApplyFileMetaClassDefineDecorations(ret);
            }
            return null;
        }
        public MetaType AddArrayTemplate(MetaType arrayMt, List<int> list)
        {
            MetaType cmt = new MetaType(arrayMt.metaClass);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                MetaType mt = new MetaType();
                mt.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                MetaType dmt = new MetaType(cmt);
                mt.AddDefineTemplateMetaType(dmt);
                //mt.AddGenTemplateMetaType(dmt);

                cmt = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(mt, false, out bool igmc);
                cmt.SetArrayLength(list[i]);
            }
            return cmt;
        }
        public MetaType GetMetaTypeByTemplateList(MetaClass curMc, MetaNode getmc, MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList)
        {
            if (getmc == null)
            {
                return null;
            }
            int tplCount = inputTemplateNodeList != null ? inputTemplateNodeList.Count : 0;
            if (getmc.isMetaData)
            {
                if (tplCount > 0)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "data 类型不支持模板实参");
                    return null;
                }
                return getmc.metaData != null ? new MetaType(getmc.metaData) : null;
            }
            if (getmc.isMetaEnum)
            {
                if (tplCount > 0)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "enum 类型不支持模板实参");
                    return null;
                }
                return getmc.metaEnum != null ? new MetaType(getmc.metaEnum) : null;
            }

            var findfn = getmc.GetMetaClassByTemplateCount(tplCount);
            if (findfn != null)
            {
                if (tplCount == 0)
                {
                    return new MetaType(findfn);
                }

                var newmc = HandleInputTemplateNodeList(curMc, findfn, findFun, inputTemplateNodeList, false);
                if (newmc != null)
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
        public MetaType HandleInputTemplateNodeList(MetaClass findfn, MetaClass regMc, MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList, bool isParse)
        {
            var getmc = findfn;
            MetaType mt = new MetaType();
            if (inputTemplateNodeList.Count == 0)
            {
                mt.SetMetaClass(regMc);
                return mt;
            }
            mt.SetTemplateMetaClass(regMc);
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateFunction(findfn, findFun, inputTemplateNodeList[i], isParse);
                mt.AddDefineTemplateMetaType(t);
                //mt.AddGenTemplateMetaType(t);
            }
            mt = regMc.AddMetaPreTemplateClass(mt, isParse, out bool igmc);
            return mt;
        }
        public MetaType RegisterTemplateDefineMetaTemplateFunction(MetaClass findMc, MetaMemberFunction findFun, FileInputTemplateNode fmtd, bool isParse = false)
        {
            var newmc = ClassManager.instance.GetMetaClassByNameAndFileMeta(findMc, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                var findfn = newmc.GetMetaClassByTemplateCount(fmtd.inputTemplateCount);

                if (findfn == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到相对应的模板类!!");
                    return null;
                }
                if (fmtd.inputTemplateCount > 0)
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[fmtd.defineClassCallLink.callNodeList.Count - 1];

                    var retmc = HandleInputTemplateNodeList(findMc, findfn, findFun, dcc.inputTemplateNodeList, isParse);

                    if (retmc != null)
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
                            return new MetaType(mgtc2, fmtd.nameList[0]);
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "----fmtd.nameList.count > 1 ");
                }
            }
            return null;
        }
        #endregion


        #region MetaType 对比与赋值关系

        /// <summary>两个 <see cref="MetaType"/> 是否结构/类型等价（class / data / enum 分派）。</summary>
        public static bool CompareMetaType(MetaType mdtL, MetaType mdtR)
        {
            if (mdtL == null || mdtR == null)
                return false;

            if (mdtR.isNull || mdtR.metaClass == CoreMetaClassManager.nullMetaClass)
                return true;

            if (mdtL.isData || mdtR.isData)
            {
                if (!mdtL.isData || !mdtR.isData)
                    return false;

                var leftMd = mdtL.metaData;
                var rightMd = mdtR.metaData;
                if (ReferenceEquals(leftMd, rightMd))
                    return true;
                if (leftMd == null || rightMd == null)
                    return false;
                return ClassManager.instance.CompareMetaDataMember(leftMd, rightMd);
            }

            if (mdtL.isEnum || mdtR.isEnum)
            {
                if (!mdtL.isEnum || !mdtR.isEnum)
                    return false;

                var leftEnum = mdtL.metaEnum;
                var rightEnum = mdtR.metaEnum;
                if (leftEnum == null || rightEnum == null)
                    return false;
                if (ReferenceEquals(leftEnum, rightEnum))
                    return true;
                return string.Equals(leftEnum.allName, rightEnum.allName, StringComparison.Ordinal);
            }

            MetaClass leftBaseClass = mdtL.GetTemplateMetaClass();
            MetaClass rightBaseClass = mdtR.GetTemplateMetaClass();

            if( leftBaseClass == CoreMetaClassManager.objectMetaClass )
            {
                return true;
            }
            if (leftBaseClass != rightBaseClass)
                return false;

            List<MetaType> leftTemplateList = mdtL.GetGenTemplateMetaTypeList();
            List<MetaType> rightTemplateList = mdtR.GetGenTemplateMetaTypeList();
            if (leftTemplateList.Count != rightTemplateList.Count)
                return false;

            for (int i = 0; i < leftTemplateList.Count; i++)
            {
                if (!CompareMetaType(leftTemplateList[i], rightTemplateList[i]))
                    return false;
            }

            return true;
        }

        /// <summary>合并多维数组各维元素关系：全 Same 为 Same，否则各维须一致（Child/Parent/Interface/Num 等）。</summary>
        private static ETypeRelation CombineArrayElementRelations(ETypeRelation aggregate, ETypeRelation element)
        {
            if (element == ETypeRelation.No || element.IsError() || element == ETypeRelation.KindMismatch)
                return ETypeRelation.No;

            if (aggregate == ETypeRelation.Same)
                return element;

            if (element == ETypeRelation.Same)
                return aggregate;

            if (aggregate == element)
                return aggregate;

            return ETypeRelation.No;
        }

        /// <summary>同模板 Array 在元素类型上的递归关系（CompareMetaType 失败后的继承/接口/数值族解析）。</summary>
        private static ETypeRelation ResolveArrayTypeRelation(
            MetaType targetArray,
            MetaType expressArray,
            out MetaClass targetClass,
            out MetaClass expressClass,
            ETypeRelationResolveFlags flags)
        {
            targetClass = targetArray.GetTemplateMetaClass();
            expressClass = expressArray.GetTemplateMetaClass();

            if (targetClass == null || expressClass == null || targetClass != expressClass)
                return ETypeRelation.No;

            var targetArgs = targetArray.GetGenTemplateMetaTypeList();
            var expressArgs = expressArray.GetGenTemplateMetaTypeList();
            if (targetArgs == null || expressArgs == null
                || targetArgs.Count == 0 || targetArgs.Count != expressArgs.Count)
                return ETypeRelation.No;

            ETypeRelation aggregate = ETypeRelation.Same;
            for (int i = 0; i < targetArgs.Count; i++)
            {
                var elementRelation = ResolveTypeRelation(
                    targetArgs[i],
                    expressArgs[i],
                    out _,
                    out _,
                    flags);
                aggregate = CombineArrayElementRelations(aggregate, elementRelation);
                if (aggregate == ETypeRelation.No)
                    return ETypeRelation.No;
            }

            return aggregate;
        }

        /// <summary>两个 <see cref="MetaType"/> 之间的统一关系（class / data / enum）。</summary>
        public static ETypeRelation ResolveTypeRelation(
            MetaType targetMetaType,
            MetaType expressMetaType,
            out MetaClass targetClass,
            out MetaClass expressClass,
            ETypeRelationResolveFlags flags = ETypeRelationResolveFlags.None)
        {
            targetClass = null;
            expressClass = null;

            if (targetMetaType == null)
                return ETypeRelation.TargetTypeError;
            if (expressMetaType == null)
                return ETypeRelation.ExpressTypeError;

            bool isNullExpress = expressMetaType.isNull
                || expressMetaType.metaClass == CoreMetaClassManager.nullMetaClass;

            if (targetMetaType.isData)
            {
                if (isNullExpress)
                {
                    expressClass = CoreMetaClassManager.nullMetaClass;
                    return ETypeRelation.Same;
                }
                if (!expressMetaType.isData)
                    return ETypeRelation.KindMismatch;
                return CompareMetaType(targetMetaType, expressMetaType)
                    ? ETypeRelation.Same
                    : ETypeRelation.No;
            }

            if (targetMetaType.isEnum)
            {
                var targetEnum = targetMetaType.metaEnum;
                if (targetEnum == null)
                    return ETypeRelation.TargetTypeError;
                if (isNullExpress)
                    return ETypeRelation.No;
                if (expressMetaType.isEnum)
                {
                    return CompareMetaType(targetMetaType, expressMetaType)
                        ? ETypeRelation.Same
                        : ETypeRelation.No;
                }

                var extendClass = targetEnum.extendClass;
                if (extendClass != null && expressMetaType.isClass)
                {
                    expressClass = expressMetaType.metaClass;
                    targetClass = extendClass;
                    if (expressClass == null)
                        return ETypeRelation.ExpressTypeError;
                    return ClassManager.ValidateClassTypeRelation(extendClass, expressClass);
                }

                var extendMd = targetEnum.extendMetaData;
                if (extendMd != null && expressMetaType.isData)
                {
                    return CompareMetaType(new MetaType(extendMd), expressMetaType)
                        ? ETypeRelation.Same
                        : ETypeRelation.No;
                }

                return ETypeRelation.KindMismatch;
            }

            if (targetMetaType.isClass)
            {
                targetClass = targetMetaType.metaClass;
                if (targetClass == null)
                    return ETypeRelation.TargetTypeError;

                if (isNullExpress)
                {
                    expressClass = CoreMetaClassManager.nullMetaClass;
                    return ClassManager.ValidateClassTypeRelation(targetClass, expressClass);
                }

                expressClass = expressMetaType.metaClass;
                if (!expressMetaType.isClass)
                    return ETypeRelation.KindMismatch;
                if (expressClass == null)
                    return ETypeRelation.ExpressTypeError;

                if (targetMetaType.IsArray() && expressMetaType.IsArray())
                {
                    if (CompareMetaType(targetMetaType, expressMetaType))
                    {
                        targetClass = targetMetaType.GetTemplateMetaClass();
                        expressClass = expressMetaType.GetTemplateMetaClass();
                        return ETypeRelation.Same;
                    }

                    return ResolveArrayTypeRelation(
                        targetMetaType,
                        expressMetaType,
                        out targetClass,
                        out expressClass,
                        flags);
                }

                if (TryGenericTemplateAssignRelation(targetMetaType, expressMetaType, out var genericRelation))
                    return genericRelation;

                return ClassManager.ValidateClassTypeRelation(targetClass, expressClass);
            }

            return ETypeRelation.TargetTypeError;
        }

        /// <summary>从表达式节点解析右值类型，再调用 <see cref="ResolveTypeRelation"/>（含 Iterator/Iterable 协变与 enum 成员特例）。</summary>
        public static ETypeRelation ResolveAssignRelation(
            MetaType targetMetaType,
            MetaExpressNodeBase expressNode,
            bool useTemplateExactMatch,
            bool allowEnumOwnerEqual,
            out MetaType expressRetMetaDefineType,
            out MetaClass curClass,
            out MetaClass compareClass,
            out bool isNullConstExpress,
            MetaVariable targetVariable = null)
        {
            expressRetMetaDefineType = null;
            curClass = null;
            compareClass = null;
            isNullConstExpress = false;

            if (targetMetaType == null)
                return ETypeRelation.TargetTypeError;
            if (expressNode == null)
                return ETypeRelation.ExpressTypeError;

            if (expressNode is MetaConstExpressNode constExpressNode && constExpressNode.eType == EType.Null)
            {
                isNullConstExpress = true;
                expressRetMetaDefineType = new MetaType(CoreMetaClassManager.nullMetaClass);
                return ResolveTypeRelation(
                    targetMetaType,
                    expressRetMetaDefineType,
                    out curClass,
                    out compareClass,
                    allowEnumOwnerEqual ? ETypeRelationResolveFlags.AllowEnumStorageMember : ETypeRelationResolveFlags.None);
            }

            expressRetMetaDefineType = expressNode.GetReturnMetaType();
            if (expressRetMetaDefineType == null)
                return ETypeRelation.ExpressTypeError;

            if (allowEnumOwnerEqual
                && targetMetaType.isClass
                && targetMetaType.metaClass == CoreMetaClassManager.enumMetaData
                && expressNode is MetaCallLinkExpressNode mclen)
            {
                var mv = mclen.GetReturnMetaVariable();
                if (mv?.ownerMetaClass == CoreMetaClassManager.enumMetaData || mv is MetaMemberEnum)
                {
                    curClass = targetMetaType.metaClass;
                    compareClass = expressRetMetaDefineType.metaClass;
                    return ETypeRelation.Same;
                }
            }

            if (targetMetaType.isClass)
            {
                if (targetMetaType.IsIterator() && expressRetMetaDefineType.IsArray()
                    && ClassManager.TryIteratorNumberFromConcreteNumericArray(targetMetaType, expressRetMetaDefineType))
                    return ETypeRelation.Same;

                if (targetMetaType.IsIterator() && expressRetMetaDefineType.IsIterator()
                    && ClassManager.TryIteratorNumberFromConcreteNumericIterator(targetMetaType, expressRetMetaDefineType))
                    return ETypeRelation.Same;

                if (targetMetaType.IsIterator()
                    && ClassManager.TryIteratorNumberFromArrayIteratorSource(targetMetaType, expressNode))
                    return ETypeRelation.Same;

                if (targetMetaType.IsIterable() && expressRetMetaDefineType.IsArray()
                    && ClassManager.TryIterableFromArrayElementAssignable(targetMetaType, expressRetMetaDefineType))
                    return ETypeRelation.Same;
            }

            if (targetMetaType.isEnum)
            {
                var targetEnum = targetMetaType.metaEnum;
                if (targetEnum != null)
                {
                    if (expressNode is MetaCallLinkExpressNode enumCall
                        && enumCall.GetReturnMetaVariable() is MetaMemberEnum mme
                        && ReferenceEquals(mme.ownerMetaEnum, targetEnum))
                    {
                        return ETypeRelation.Same;
                    }

                    var enumValue = expressRetMetaDefineType.enumValue;
                    if (enumValue != null && ReferenceEquals(enumValue.ownerMetaEnum, targetEnum))
                        return ETypeRelation.Same;
                }
            }

            var relation = ResolveTypeRelation(targetMetaType, expressRetMetaDefineType, out curClass, out compareClass);
            // 数组实体赋值要求严格同型（见 md/syntax/array.md §5.1）；关系解析仍可在 as/is 等路径得到 Child 等。
            if (targetMetaType.IsArray() && expressRetMetaDefineType.IsArray()
                && relation != ETypeRelation.Same)
            {
                return ETypeRelation.No;
            }

            return relation;
        }

        #endregion

    }
}
