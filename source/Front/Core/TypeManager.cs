//****************************************************************************
//  File:      TypeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static SimpleLanguage.Core.MetaNewObjectExpressNode;

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
                if(objMt.isEnumMember )
                {
                    return new MetaType(CoreMetaClassManager.memberMetaClass);
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
                if (!NumberManager.IsNumberClass(t.metaClass))
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
                    && TypeManager.TryGetCompatibleArrayMetaType(merged, next, out var compatibleArrayMetaType))
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
                    var relation = TypeManager.ValidateClassTypeRelation(left, right);
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

                if (objMt.isEnumMember)
                {
                    return new MetaType(CoreMetaClassManager.memberMetaClass);
                }
                return objMt;
            }
            if (merged.isEnumMember)
            {
                return new MetaType(CoreMetaClassManager.memberMetaClass);
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
        public static bool IsCoreMetaType(MetaType mt)
        {
            if (mt.eMetaTypeType == EMetaTypeType.MetaClass)
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
                    || curClass == CoreMetaClassManager.arrayMetaClass)
                {
                    return true;
                }
            }
            return false;
        }
        public bool UpdateMetaTypeByGenClassAndFunction(MetaType mt, MetaGenTemplateClass mgtc, MetaGenTemplateFunction mgtf)
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
                var newmc = mt.metaClass.AddInstanceMetaClass(regMCList, true);
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

            var relation = TypeManager.ValidateClassTypeRelation(tClass, eClass);
            return relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Interface
                || relation == ETypeRelation.Num;
        }

        private static bool IsTemplateArgAssignableByCovariance(MetaTemplate template, MetaType targetArg, MetaType exprArg)
        {
            if (CompareMetaType(targetArg, exprArg))
                return true;

            if (template == null || template.covariance == ECovariance.None)
                return false;

            var targetClass = targetArg?.GetTemplateMetaClass();
            var exprClass = exprArg?.GetTemplateMetaClass();
            if (targetClass == null || exprClass == null)
                return false;

            var relation = ValidateClassTypeRelation(targetClass, exprClass);
            if (template.covariance == ECovariance.Out)
            {
                return relation == ETypeRelation.Child
                    || relation == ETypeRelation.Interface
                    || relation == ETypeRelation.Num;
            }
            if (template.covariance == ECovariance.In)
            {
                return relation == ETypeRelation.Parent
                    || relation == ETypeRelation.Num;
            }

            return false;
        }

        private static bool CompareTemplateArgByIndex(MetaClass templateOwnerClass, int index, MetaType targetArg, MetaType exprArg)
        {
            MetaTemplate template = null;
            if (templateOwnerClass != null && index >= 0 && index < templateOwnerClass.metaTemplateList.Count)
            {
                template = templateOwnerClass.metaTemplateList[index];
            }

            return IsTemplateArgAssignableByCovariance(template, targetArg, exprArg);
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
                    bool ok = CompareTemplateArgByIndex(targetClass, i, targetArgs[i], exprArgsSameInterface[i]);
                    if (!ok && targetClass.isInterfaceClass && IsInterfaceTemplateArgCovariant(targetClass, i))
                    {
                        ok = IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsSameInterface[i]);
                    }
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
                    bool ok = CompareTemplateArgByIndex(targetClass, i, targetArgs[i], exprArgsFromInterface[i]);
                    if (!ok && IsInterfaceTemplateArgCovariant(targetClass, i))
                    {
                        ok = IsCovariantTemplateArgAssignable(targetArgs[i], exprArgsFromInterface[i]);
                    }
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
                    retMt = AddArrayTemplate(retMt, fmcd.arrayDimsionLengthList);
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
        public static bool CompareMetaType(MetaType mdtL, MetaType mdtR, ECovariance eCovariance = ECovariance.Out )
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
                return MetaData.CompareMetaDataMember(leftMd, rightMd);
            }

            else if (mdtL.isEnum || mdtR.isEnum || mdtL.isEnumMember || mdtR.isEnumMember )
            {
                return CompareEnumMetaType(mdtL, mdtR, null );
            }

            else
            {
                MetaClass leftBaseClass = mdtL.GetTemplateMetaClass();
                MetaClass rightBaseClass = mdtR.GetTemplateMetaClass();

                if (leftBaseClass == CoreMetaClassManager.objectMetaClass)
                {
                    return true;
                }

                if(eCovariance == ECovariance.Out )
                {
                    if( !rightBaseClass.ExtendClassContainMetaClass(leftBaseClass) )
                    {
                        return false;
                    }
                }
                else if( eCovariance == ECovariance.In )
                {
                    if( !leftBaseClass.ExtendClassContainMetaClass(rightBaseClass) )
                    {
                        return false;
                    }
                }
                else
                {
                    if (leftBaseClass != rightBaseClass)
                        return false;
                }


                List<MetaType> leftTemplateList = mdtL.GetGenTemplateMetaTypeList();
                List<MetaType> rightTemplateList = mdtR.GetGenTemplateMetaTypeList();
                if (leftTemplateList.Count != rightTemplateList.Count)
                    return false;

                for (int i = 0; i < leftTemplateList.Count; i++)
                {
                    if (!CompareTemplateArgByIndex(leftBaseClass, i, leftTemplateList[i], rightTemplateList[i]))
                        return false;
                }

                return true;
            }
        }


        public static bool CompareEnumMetaType(MetaType leftMt, MetaType rightMt, Token token)
        {
            // 左值 data：右值需为 data 或 null。
            if (leftMt.isEnum)
            {
                if (rightMt.isNull || rightMt.metaClass == CoreMetaClassManager.nullMetaClass)
                {
                    return false;
                }
                if (rightMt.isEnum)
                {
                    if (leftMt.metaEnum != rightMt.metaEnum)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "不是同一个enum");
                        return false;
                    }
                }
                else if (rightMt.isEnumMember)
                {
                    if (rightMt.enumValue.ownerMetaBase != leftMt.metaEnum)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "不是同一个enum");
                        return false;
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "is not enum member ");
                    return false;
                }
            }
            else if (rightMt.isEnum)
            {
                if (leftMt.isNull || leftMt.metaClass == CoreMetaClassManager.nullMetaClass)
                {
                    return false;
                }
                if (leftMt.isEnum)
                {
                    if (leftMt.metaEnum != rightMt.metaEnum)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "不是同一个enum");
                        return false;
                    }
                }
                else if (leftMt.isEnumMember)
                {
                    if (leftMt.enumValue.ownerMetaBase != rightMt.metaEnum)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "不是同一个enum");
                        return false;
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, token, "is not enum member ");
                    return false;
                }
            }
            else if (leftMt.isEnumMember && rightMt.isEnumMember)
            {
                if (leftMt.enumValue.ownerMetaBase != rightMt.enumValue.ownerMetaBase)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "不是同一个enum");
                    return false;
                }
            }
            return true;
        }


        /// <summary> ?????????????????Int32???Float32 ???????????? Num ??????? </summary>
        public static bool IsConcreteNumericElementType(MetaType elem)
        {
            if (elem?.metaClass == null) return false;
            if (elem.metaClass == CoreMetaClassManager.numMetaClass) return false;
            return NumberManager.IsNumberClass(elem.metaClass);
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

        /// <summary> Iterator&lt;Number&gt; &lt;- Iterator&lt;???????????&gt;????????????????? Number ?????????? </summary>
        public static bool TryIteratorNumberFromConcreteNumericIterator(MetaType targetIterator, MetaType exprIterator)
        {
            if (targetIterator == null || exprIterator == null) return false;
            if (!targetIterator.IsIterator() || !exprIterator.IsIterator()) return false;
            var tArg = GetSingleTemplateArgMetaType(targetIterator);
            var eArg = GetSingleTemplateArgMetaType(exprIterator);
            return IsAbstractNumberMetaType(tArg) && IsConcreteNumericElementType(eArg);
        }

        public static bool IsAbstractNumberMetaType(MetaType mt)
        {
            return mt != null && mt.metaClass == CoreMetaClassManager.numMetaClass;
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
                if ( NumberManager.IsNumberClass(curClass) || curClass == CoreMetaClassManager.booleanMetaClass)
                    return ETypeRelation.No;

                if (curClass == CoreMetaClassManager.objectMetaClass)
                    return ETypeRelation.Same;
                return ETypeRelation.Parent;
            }
            if (curClass == CoreMetaClassManager.objectMetaClass)
            {
                if (curClass == compareClass)
                    return ETypeRelation.Same;
                return ETypeRelation.Child;
            }
            if (curClass.Equals(compareClass))
            {
                return ETypeRelation.Same;
            }

            if (curClass == CoreMetaClassManager.numMetaClass)
            {
                if (NumberManager.IsNumberClass(compareClass))
                    return ETypeRelation.Num;
                return ETypeRelation.No;
            }
            if (NumberManager.IsNumberClass(curClass) && NumberManager.IsNumberClass(compareClass))
            {
                return ETypeRelation.Num;
            }

            if (compareClass.IsInterfaceByMetaClass(curClass))
                return ETypeRelation.Interface;
            if (curClass.IsParseMetaClass(compareClass))
                return ETypeRelation.Parent;
            if (compareClass.IsParseMetaClass(curClass))
                return ETypeRelation.Child;
            return ETypeRelation.No;
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
                    return TypeManager.ValidateClassTypeRelation(extendClass, expressClass);
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
                    return TypeManager.ValidateClassTypeRelation(targetClass, expressClass);
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

                return TypeManager.ValidateClassTypeRelation(targetClass, expressClass);
            }

            return ETypeRelation.TargetTypeError;
        }
        public static bool CompareFunctionDefineMetaTypeAndInputMetaType( MetaType declaredMt, MetaType argMt, Token token )
        {
            if (declaredMt.eType == EType.Object)
            {
                return true;
            }

            if (declaredMt.isEnum || declaredMt.isEnumMember || argMt.isEnum || argMt.isEnumMember)
            {
                return CompareEnumMetaType(declaredMt, argMt, token);
            }
            else if (declaredMt.isData && argMt.isData)
            {
                if (declaredMt.metaData == argMt.metaData)
                {
                    return true;
                }
            }
            else if (declaredMt.isClass && argMt.isClass)
            {
                if (declaredMt.IsIterator() )
                {
                    if( argMt.IsArray()
                        && TypeManager.TryIteratorNumberFromConcreteNumericArray(declaredMt, argMt))
                    {
                        return true;
                    }
                    if ( argMt.IsIterator()
                        && TypeManager.TryIteratorNumberFromConcreteNumericIterator(declaredMt, argMt))
                        return true;
                    if (TypeManager.TryIteratorNumberFromConcreteNumericArray(declaredMt, argMt))
                        return true;
                    return false;
                }
                else if (declaredMt.IsIterable() )
                {
                    if (argMt.IsArray()
                    && TypeManager.TryIterableFromArrayElementAssignable(declaredMt, argMt))
                        return true;
                    else
                        return false;
                }
                else if (declaredMt.IsArray() )
                {
                    if (argMt.IsArray())
                        return TypeManager.CompareMetaType(declaredMt, argMt);
                    else
                        return false;
                }
                var declaredMC = declaredMt.GetTemplateMetaClass();
                var retMC = argMt.metaClass;
                if (retMC is MetaGenTemplateClass mgtc)
                {
                    retMC = mgtc.metaTemplateClass;
                }
                var relation = TypeManager.ValidateClassTypeRelation(declaredMC, retMC);

                if (relation == ETypeRelation.Same
                    || relation == ETypeRelation.Child
                    || relation == ETypeRelation.Interface
                    || relation == ETypeRelation.Parent
                    )
                {
                    return true;
                }
                if (relation == ETypeRelation.Num)
                {
                    // ? Num ?????????????????????????????????? int/float ?????
                    if (!TypeManager.IsNarrowerCorePrimitiveWideningOkForCallSite(retMC, declaredMC))
                        return false;
                    return true;
                }
            }
            else
            {
            }
            return false;
        }
        public static bool CompareLeftRightMetaType(MetaType leftMt, MetaType rightMt, Token token, out MetaType convertMt)
        {
            convertMt = null;
            // 左值 enum：仅允许是同一 enum 的成员调用表达式，或同 enum 的常量值。
            if (leftMt == null || rightMt == null)
                return false;

            if (leftMt.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            if( leftMt.metaClass == CoreMetaClassManager.memberMetaClass )
            {

            }
            else if ( leftMt.isEnum || leftMt.isEnumMember || rightMt.isEnum || rightMt.isEnumMember )
            {
                return CompareEnumMetaType(leftMt, rightMt, token);
            }
            else if (leftMt.isData )
            {
                if (rightMt.isNull || rightMt.metaClass == CoreMetaClassManager.nullMetaClass)
                {
                    return true;
                }
                else
                {
                    if (!rightMt.isData)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, token,
                            "data 声明类型与右侧表达式类型不匹配：右值非 data 类型。");
                        return false;
                    }
                    else
                    {
                        if (ReferenceEquals(rightMt, rightMt))
                            return true;
                        return MetaData.CompareMetaDataMember(rightMt.metaData, rightMt.metaData);
                    }
                }
            }
            else
            {
                if (leftMt.isTemplate)
                {
                    MetaClass leftmc = leftMt.metaClass;
                    MetaClass rightmc = rightMt.metaClass;
                    if (rightMt.isNull || rightmc == CoreMetaClassManager.nullMetaClass)
                    {
                        return true;
                    }
                    else
                    {
                        if (!rightMt.isTemplate)
                        {
                            if (leftmc == CoreMetaClassManager.numMetaClass)
                            {
                                if (rightMt.IsNum())
                                {
                                    return true;
                                }
                            }
                            if (leftmc == CoreMetaClassManager.int8MetaClass
                               || leftmc == CoreMetaClassManager.uint8MetaClass)
                            {
                                return false;
                            }
                            else if (leftmc == CoreMetaClassManager.int16MetaClass
                               || leftmc == CoreMetaClassManager.uint16MetaClass)
                            {
                                if (rightmc == CoreMetaClassManager.int8MetaClass
                                    || rightmc == CoreMetaClassManager.uint8MetaClass)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else if (leftmc == CoreMetaClassManager.int32MetaClass
                               || leftmc == CoreMetaClassManager.uint32MetaClass)
                            {
                                if (rightmc == CoreMetaClassManager.int8MetaClass
                                    || rightmc == CoreMetaClassManager.uint8MetaClass
                                    || rightmc == CoreMetaClassManager.int16MetaClass
                                    || rightmc == CoreMetaClassManager.uint16MetaClass)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else if (leftmc == CoreMetaClassManager.int64MetaClass
                               || leftmc == CoreMetaClassManager.uint64MetaClass)
                            {

                                if (rightmc == CoreMetaClassManager.int8MetaClass
                                    || rightmc == CoreMetaClassManager.uint8MetaClass
                                    || rightmc == CoreMetaClassManager.int16MetaClass
                                    || rightmc == CoreMetaClassManager.uint16MetaClass
                                    || rightmc == CoreMetaClassManager.int32MetaClass
                                    || rightmc == CoreMetaClassManager.uint32MetaClass)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else if (leftmc == CoreMetaClassManager.numMetaClass)
                            {
                                if (rightMt.IsNum())
                                {
                                    return true;
                                }
                                return false;
                            }

                            Log.AddMetaCoreLog(LID.ShowExtendMessage, token,
                                "data 声明类型与右侧表达式类型不匹配：右值非 data 类型。");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else if (leftMt.GetGenTemplateMetaTypeList().Count > 0)
                {
                    if (rightMt.isNull || rightMt.metaClass == CoreMetaClassManager.nullMetaClass)
                    {
                        return true;
                    }

                    if (MetaClass.CompareMetaClass(leftMt.GetTemplateMetaClass(), rightMt.GetTemplateMetaClass()) == false)
                    {
                        return false;
                    }

                    var leftDmtList = leftMt.GetGenTemplateMetaTypeList();
                    var rightDmtList = rightMt.GetGenTemplateMetaTypeList();
                    if (leftDmtList.Count == rightDmtList.Count && leftMt.metaClass.metaTemplateList.Count == rightDmtList.Count)
                    {
                        for (int i = 0; i < leftDmtList.Count; i++)
                        {
                            var dtmt = leftDmtList[i];
                            var dtmt2 = rightDmtList[i];
                            var mtl = leftMt.metaClass.metaTemplateList[i];

                            if (TypeManager.CompareMetaType(dtmt, dtmt2, mtl.covariance) == false)
                            {
                                return false;
                            }

                        }
                        return true;
                    }
                }
                else
                {
                    MetaClass leftmc = leftMt.metaClass;
                    MetaClass rightmc = rightMt.metaClass;
                    if (rightMt.isNull || rightmc == CoreMetaClassManager.nullMetaClass)
                    {
                        return true;
                    }
                    else
                    {
                        if (leftmc == rightmc )
                        {
                            return true;
                        }

                        if (leftMt.eType == EType.Int8
                           || leftMt.eType == EType.UInt8)
                        {
                            if (rightMt.eType == EType.Int8
                                || rightMt.eType == EType.UInt8
                                || rightMt.eType == EType.Int16
                                || rightMt.eType == EType.UInt16
                                || rightMt.eType == EType.Int32
                                || rightMt.eType == EType.UInt32
                                || rightMt.eType == EType.Int64
                                || rightMt.eType == EType.UInt64)
                            {
                                convertMt = leftMt;
                                return true;
                            }
                            return false;
                        }
                        else if (leftMt.eType == EType.Int16
                           || leftMt.eType == EType.UInt16 )
                        {
                            if (rightMt.eType == EType.Int8
                                || rightMt.eType == EType.UInt8 )
                            {
                                return true;
                            }
                            else if (rightMt.eType == EType.Int16
                                || rightMt.eType == EType.UInt16
                                || rightMt.eType == EType.Int32
                                || rightMt.eType == EType.UInt32
                                || rightMt.eType == EType.Int64
                                || rightMt.eType == EType.UInt64 )
                            {
                                convertMt = leftMt;
                                return true;
                            }
                            return false;
                        }
                        else if (leftMt.eType == EType.Int32
                           || leftMt.eType == EType.UInt32 )
                        {
                            if (rightMt.eType == EType.Int8
                                || rightMt.eType == EType.UInt8
                                || rightMt.eType == EType.Int16
                                || rightMt.eType == EType.UInt16
                                || rightMt.eType == EType.Int32
                                || rightMt.eType == EType.UInt32 )
                            {
                                return true;
                            }
                            else if ( rightMt.eType == EType.Int64
                                || rightMt.eType == EType.UInt64)
                            {
                                convertMt = leftMt;
                                return true;
                            }
                            else if (rightMt.eType == leftMt.eType)
                            {
                                return true;
                            }
                            return false;
                        }
                        else if (leftMt.eType == EType.Int64
                           || leftMt.eType == EType.UInt64)
                        {
                            if (rightMt.eType == EType.Int8
                                || rightMt.eType == EType.UInt8
                                || rightMt.eType == EType.Int16
                                || rightMt.eType == EType.UInt16
                                || rightMt.eType == EType.Int32
                                || rightMt.eType == EType.UInt32
                                || rightMt.eType == EType.Int64
                                || rightMt.eType == EType.UInt64 )
                            {
                                return true;
                            }
                            return false;
                        }
                        else if (leftMt.eType == EType.Float32)
                        {
                            if (rightMt.eType == EType.Float32)
                            {
                                return true;
                            }
                            else if ( rightMt.eType == EType.Float64)
                            {
                                convertMt = leftMt;
                                return true;
                            }
                            return true;
                        }
                        else if (leftMt.eType == EType.Float64)
                        {
                            if ( rightMt.eType == EType.Float32 )
                            {
                                return true;
                            }
                            return true;
                        }
                        else if (leftMt.eType == EType.Num )
                        {
                            if (rightMt.IsNum())
                            {
                                return true;
                            }
                            return false;
                        }
                        else
                        {
                            return MetaClass.CompareMetaClass(leftmc, rightmc);
                        }

                        //Log.AddMetaCoreLog(LID.ShowExtendMessage, token,
                        //    "data 声明类型与右侧表达式类型不匹配：右值非 data 类型。");
                        return false;
                    }
                }
            }
            return true;
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

        private static bool TryForceConvertArrayLiteralElements(
            MetaNewObjectExpressNode arrayNode,
            MetaType targetElemType,
            Token errorAnchorToken)
        {
            if (arrayNode == null || targetElemType == null)
            {
                return true;
            }

            var list = arrayNode.assignStatementsList;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var expr = item?.expressNode;
                if (expr == null) continue;

                if (expr is MetaConstExpressNode c)
                {
                    if (!NumberManager.TryForceAdjustConstExpressByMetaType(c, targetElemType, errorAnchorToken))
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, errorAnchorToken,
                            "数组元素强制转换失败（可能溢出或类型不匹配）: 目标类型 " + targetElemType.ToString());
                        return false;
                    }
                    c.CalcReturnType();
                    continue;
                }

                if (expr is MetaNewObjectExpressNode childArrayNode && targetElemType.IsArray())
                {
                    var nextElemType = TypeManager.GetSingleTemplateArgMetaType(targetElemType);
                    if (nextElemType != null && !TryForceConvertArrayLiteralElements(childArrayNode, nextElemType, errorAnchorToken))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsClassAdapt(MetaClass mc1, MetaClass mc2)
        {
            if (mc1 == CoreMetaClassManager.int64MetaClass
                || mc1 == CoreMetaClassManager.uint64MetaClass)
            {
                if (mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass
                    || mc2 == CoreMetaClassManager.int16MetaClass
                    || mc2 == CoreMetaClassManager.uint16MetaClass
                    || mc2 == CoreMetaClassManager.int32MetaClass
                    || mc2 == CoreMetaClassManager.uint32MetaClass)
                {
                    return true;
                }
            }
            else if (mc1 == CoreMetaClassManager.int32MetaClass
                || mc1 == CoreMetaClassManager.uint32MetaClass)
            {
                if (mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass
                    || mc2 == CoreMetaClassManager.int16MetaClass
                    || mc2 == CoreMetaClassManager.uint16MetaClass)
                {
                    return true;
                }
            }
            else if (mc1 == CoreMetaClassManager.int16MetaClass
                || mc1 == CoreMetaClassManager.uint16MetaClass)
            {
                if (mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public static bool TryGetPreferredElementMetaTypeFromDefine(MetaType defineMetaType, out MetaType preferredElementMetaType)
        {
            preferredElementMetaType = null;
            if (defineMetaType == null || !defineMetaType.IsArray())
            {
                return false;
            }

            var defineTemplateList = defineMetaType.GetGenTemplateMetaTypeList();
            if (defineTemplateList == null || defineTemplateList.Count != 1)
            {
                return false;
            }

            preferredElementMetaType = defineTemplateList[0];
            return preferredElementMetaType != null;
        }

        public static bool IsArrayLiteralElementAssignableToTarget(MetaType targetMetaType, MetaType sourceMetaType)
        {
            if (targetMetaType == null || sourceMetaType == null)
            {
                return false;
            }

            if (TypeManager.CompareMetaType(targetMetaType, sourceMetaType))
            {
                return true;
            }

            if (targetMetaType.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            if (targetMetaType.IsArray() && sourceMetaType.IsArray())
            {
                var targetArgs = targetMetaType.GetGenTemplateMetaTypeList();
                var sourceArgs = sourceMetaType.GetGenTemplateMetaTypeList();
                if (targetArgs == null || sourceArgs == null || targetArgs.Count != 1 || sourceArgs.Count != 1)
                {
                    return false;
                }

                return IsArrayLiteralElementAssignableToTarget(targetArgs[0], sourceArgs[0]);
            }

            if (targetMetaType.IsArray() && sourceMetaType.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            return false;
        }

        public static bool TryGetCompatibleArrayMetaType(MetaType leftArray, MetaType rightArray, out MetaType result)
        {
            result = null;
            if (leftArray == null || rightArray == null) return false;
            if (!leftArray.IsArray() || !rightArray.IsArray()) return false;

            var leftTemplate = leftArray.GetTemplateMetaClass();
            var rightTemplate = rightArray.GetTemplateMetaClass();
            if (leftTemplate != rightTemplate) return false;

            var leftArgs = leftArray.GetGenTemplateMetaTypeList();
            var rightArgs = rightArray.GetGenTemplateMetaTypeList();
            if (leftArgs == null || rightArgs == null || leftArgs.Count != rightArgs.Count || leftArgs.Count == 0)
            {
                return false;
            }

            var leftElement = leftArgs[0];
            var rightElement = rightArgs[0];

            if (TypeManager.CompareMetaType(leftElement, rightElement))
            {
                result = new MetaType(leftArray);
                return true;
            }

            if (leftElement.IsArray() && rightElement.IsArray())
            {
                if (!TryGetCompatibleArrayMetaType(leftElement, rightElement, out var nestedCompatible))
                {
                    return false;
                }

                MetaType build = new MetaType();
                build.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                build.AddDefineTemplateMetaType(nestedCompatible);
                result = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(build, true, out bool _);

                if (leftArray.arrayLength != -1)
                {
                    result.SetArrayLength(leftArray.arrayLength);
                }
                else if (rightArray.arrayLength != -1)
                {
                    result.SetArrayLength(rightArray.arrayLength);
                }
                return true;
            }

            return false;
        }
    }
}
