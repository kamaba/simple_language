//****************************************************************************
//  File:      IRMetaModule.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/08/03 12:00:00
//  Description: 管理 ref module 导入的 IRMetaClass 集合
//              导出和导入对称：导出时 IRMetaClass -> SLClassPackage，
//              导入时 SLClassPackage -> IRMetaClass（直接还原，不经过 Meta 层重建）
//
//  IR 先行流程：
//    Phase A（IR 层，本类负责）：
//      1. CreateIRMetaClassesFromPackage: 用导出的逆方法从 SLClassPackage 生成
//         IRMetaClass shell（id=cls.id, irName=cls.fullName），注册到 IRManager。
//         Core 内建类型复用 Core init 时已注册的 IRMetaClass。
//      2. BuildAllMembersFromPackage: 从 SLClassPackage 数据填充字段和方法列表，
//         贴到 IRMetaClass 上（不经过 Meta 层重建）。
//    Phase B（Meta 层，由 ProjectReferenceModuleLoader 负责）：
//      生成 MetaModule / MetaClass / MetaData / MetaEnum 及其成员。
//    Phase C（关联）：
//      LinkMetaOwners: 调用 IRMetaClass.LinkMetaOwner 把 MetaBase 关联到 IRMetaClass。
//****************************************************************************

using System;
using System.Collections.Generic;
using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage.IR
{
    /// <summary>
    /// 管理从一个 ref module 包导入的所有 IRMetaClass。
    /// IR 先行：先从 package 数据构建 IRMetaClass 并注册到 IRManager，
    /// Meta 层构建完成后再通过 LinkMetaOwners 关联宿主。
    /// </summary>
    public class IRMetaModule
    {
        public string moduleName { get; private set; } = "";
        public List<IRMetaClass> irMetaClassList => m_IRMetaClassList;

        private List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();
        private Dictionary<string, SLMethodPackage> m_MethodLookup;
        private Dictionary<int, IRMetaClass> m_IdToIRMetaClass = new Dictionary<int, IRMetaClass>();
        private bool m_IsCoreReplacement;

        public IRMetaModule(string name, Dictionary<string, SLMethodPackage> methodLookup, bool isCoreReplacement)
        {
            moduleName = name ?? "";
            m_MethodLookup = methodLookup ?? new Dictionary<string, SLMethodPackage>();
            m_IsCoreReplacement = isCoreReplacement;
        }

        /// <summary>
        /// Phase A Step 1: 用导出的逆方法从 SLClassPackage 生成 IRMetaClass shell 并注册到 IRManager。
        /// id 使用包内 cls.id（与包内 baseClassId / interfaceId 等一致），
        /// irName 使用 cls.fullName（去模块前缀，供 IRMetaType.CreateFromPackage 按名查找）。
        /// Core 内建类型复用 Core init 时已注册的 IRMetaClass，不重复创建。
        /// </summary>
        public void CreateIRMetaClassesFromPackage(List<SLClassPackage> classList)
        {
            if (classList == null) return;

            foreach (var cls in classList)
            {
                if (cls == null) continue;

                IRMetaClass irmc = null;

                /* Core 替换：Core 内建类型的 IRMetaClass 在 Core init 时已注册，
                 * 这里复用已有的（按 classId 查），不新建。
                 * 如果还没注册（ParseClass 尚未运行），从 MetaBase 创建 IRMetaClass
                 * （不是从 SLClassPackage），这样 typeOwner 会被设置，
                 * ParseClass 后的 CreateMemberData() 能正常注册成员变量 hash。 */
                if (m_IsCoreReplacement)
                {
                    var coreMetaBase = TryResolveCoreMetaBase(cls);
                    if (coreMetaBase != null)
                    {
                        irmc = IRManager.instance.GetIRMetaClassById(coreMetaBase.classId);
                        if (irmc == null)
                        {
                            irmc = coreMetaBase is MetaClass mc ? new IRMetaClass(mc)
                                : coreMetaBase is MetaData md ? new IRMetaClass(md)
                                : coreMetaBase is MetaEnum me ? new IRMetaClass(me)
                                : null;
                            if (irmc != null)
                            {
                                IRManager.instance.AddIRMetaClass(irmc);
                                m_IRMetaClassList.Add(irmc);
                            }
                        }
                    }
                }

                /* 非 Core：从 package 直接构建 IRMetaClass shell */
                if (irmc == null)
                {
                    irmc = new IRMetaClass(cls);
                    IRManager.instance.AddIRMetaClass(irmc);
                    m_IRMetaClassList.Add(irmc);
                }

                m_IdToIRMetaClass[cls.id] = irmc;
            }
        }

        /// <summary>
        /// Phase A Step 2: 从 package 数据填充所有 IRMetaClass 的字段和方法列表。
        /// 在所有 IRMetaClass 创建并注册后调用，确保 IRMetaType.CreateFromPackage 能按名解析类型引用。
        /// </summary>
        public void BuildAllMembersFromPackage(List<SLClassPackage> classList)
        {
            if (classList == null) return;

            foreach (var cls in classList)
            {
                if (cls == null) continue;
                if (!m_IdToIRMetaClass.TryGetValue(cls.id, out var irmc)) continue;
                if (irmc.isRefModulePreBuilt) continue;
                /* Core 替换类型（从 MetaBase 创建，typeOwner != null）也运行 BuildFields：
                 * PopulateReferenceTypeMembersFromIR 的 ClearExistingMembers 清空了
                 * MetaClass 的字段，这里需要从 package 重新填充 IRMetaVariable 列表。
                 * 哈希注册在 AddClassFieldFromIR/AddEnumMembersFromIR 中完成。 */

                BuildFields(irmc, cls);
                BuildMethods(irmc, cls);

                irmc.isRefModulePreBuilt = true;
            }
        }

        /// <summary>
        /// Phase C: Meta 层构建完成后，把 MetaBase 关联到对应的 IRMetaClass。
        /// Core 内建类型的 IRMetaClass 在 Core init 时已关联宿主，跳过。
        /// </summary>
        public void LinkMetaOwners(List<(SLClassPackage cls, MetaBase metaBase)> createdTypes)
        {
            if (createdTypes == null) return;

            foreach (var (cls, metaBase) in createdTypes)
            {
                if (cls == null || metaBase == null) continue;
                if (!m_IdToIRMetaClass.TryGetValue(cls.id, out var irmc)) continue;
                if (irmc.typeOwner != null) continue; // Core 复用的已关联

                irmc.LinkMetaOwner(metaBase);
            }
        }

        /// <summary>
        /// 按 SLClassPackage.id 查回 Phase A 创建/复用的 IRMetaClass，
        /// 供 Phase B2 反向构建 Meta 成员时使用。
        /// </summary>
        public bool TryGetIRMetaClass(int clsId, out IRMetaClass irmc)
        {
            return m_IdToIRMetaClass.TryGetValue(clsId, out irmc);
        }

        private void BuildFields(IRMetaClass irmc, SLClassPackage cls)
        {
            if (cls.fieldList == null) return;
            /* 导出端（SLModulePackageWriter）与本地模块路径（IRMetaClass.CreateMemberData）
             * 均按"实例字段 / 静态字段"两个独立 index 空间编号（各自从 0 开始）。
             * fieldList 数组位置把两类字段混在一起计数，不能直接当 index 用，
             * 否则跨模块静态字段访问索引会整体错位（偏移量 = 实例字段数量）。 */
            int localIndex = 0;
            int staticIndex = 0;
            foreach (var fp in cls.fieldList)
            {
                if (fp == null) continue;
                var irmt = IRMetaType.CreateFromPackage(fp.typeDef, irmc);
                // SLFieldPackage flags: 32 = static（与 IRMetaVariable 构造函数判定一致）
                bool isStatic = (fp.flags & 32) != 0;
                int fieldIndex = isStatic ? staticIndex++ : localIndex++;
                var imv = new IRMetaVariable(irmc, fp, irmt, fieldIndex);
                if (imv.isStatic)
                    irmc.staticIRMetaVariableList.Add(imv);
                else
                    irmc.localIRMetaVariableList.Add(imv);
            }
        }

        private void BuildMethods(IRMetaClass irmc, SLClassPackage cls)
        {
            void BuildMethodList(List<SLMethodMeta> metaList, List<IRMethod> targetList, bool isOperator = false)
            {
                if (metaList == null) return;
                foreach (var meta in metaList)
                {
                    if (meta == null || string.IsNullOrWhiteSpace(meta.id)) continue;
                    if (!m_MethodLookup.TryGetValue(meta.id, out var mp)) continue;

                    var irm = new IRMethod(IRManager.instance, mp, irmc);
                    IRManager.instance.AddIRMethod(irm);
                    if (!isOperator)
                        targetList.Add(irm);
                    else
                    {
                        if (!irmc.operatorMethodList.Contains(irm))
                            irmc.operatorMethodList.Add(irm);
                    }
                }
            }

            BuildMethodList(cls.staticMethodList, irmc.staticMethodList);
            BuildMethodList(cls.nonStaticMethodList, irmc.nonStaticMethodList);
            BuildMethodList(cls.operatorMethodList, irmc.operatorMethodList, isOperator: true);
        }

        /// <summary>
        /// 按 SLClassPackage 匹配 CoreMetaClassManager 中已注册的 Core 内建类型，
        /// 返回其 MetaBase（供复用已有 IRMetaClass）。匹配规则与 CreateReferenceTypeShell 一致：
        /// 去掉泛型后缀后按短名查找，再按 templateParameterCount 选取对应模板类。
        /// </summary>
        private static MetaBase TryResolveCoreMetaBase(SLClassPackage cls)
        {
            var lookupName = cls.name ?? cls.fullName ?? "";
            var ltIdx = lookupName.IndexOf('<');
            if (ltIdx > 0)
            {
                lookupName = lookupName.Substring(0, ltIdx);
            }
            if (string.IsNullOrWhiteSpace(lookupName)) return null;

            var coreNode = CoreMetaClassManager.GetCoreMetaClass(lookupName);
            if (coreNode == null) return null;

            if (coreNode.isMetaData && coreNode.metaData != null)
            {
                return coreNode.metaData;
            }
            if (coreNode.isMetaEnum && coreNode.metaEnum != null)
            {
                return coreNode.metaEnum;
            }
            var tpc = cls.templateParameterCount;
            var mc = coreNode.GetMetaClassByTemplateCount(tpc) ?? coreNode.GetMetaClassByTemplateCount(0);
            return mc;
        }
    }
}
