//****************************************************************************
//  File:      VMRuntimeAttributeRegistry.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/18 12:00:00
//  Description: VM-side runtime attribute registry for Route, Condition etc.
//  Mirrors Front's RuntimeAttributeRegistry for use in the standalone VM.
//****************************************************************************

using SimpleLanuageVM.Load;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// VM 运行时属性注册表。
    /// 在模块加载时从 SLAttributePackage 数据填充，
    /// 供框架代码查询路由、条件等运行时属性。
    /// </summary>
    public class VMRuntimeAttributeRegistry
    {
        public static VMRuntimeAttributeRegistry s_Instance;
        public static VMRuntimeAttributeRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new VMRuntimeAttributeRegistry();
                return s_Instance;
            }
        }

        public class RouteEntry
        {
            public string route { get; set; }
            public string classFullName { get; set; }
            public string methodName { get; set; }

            public RouteEntry(string route, string classFullName, string methodName)
            {
                this.route = route;
                this.classFullName = classFullName;
                this.methodName = methodName;
            }
        }

        public class ConditionEntry
        {
            public string condition { get; set; }
            public string classFullName { get; set; }
            public string methodName { get; set; }

            public ConditionEntry(string condition, string classFullName, string methodName)
            {
                this.condition = condition;
                this.classFullName = classFullName;
                this.methodName = methodName;
            }
        }

        private Dictionary<string, RouteEntry> m_RouteDict = new Dictionary<string, RouteEntry>();
        private Dictionary<string, List<ConditionEntry>> m_ConditionDict = new Dictionary<string, List<ConditionEntry>>();
        private HashSet<string> m_ActiveConditions = new HashSet<string>();

        public void RegisterRoute(string classFullName, string methodName, string route)
        {
            if (string.IsNullOrEmpty(route)) return;
            m_RouteDict[route] = new RouteEntry(route, classFullName ?? "", methodName ?? "");
        }

        public RouteEntry GetRouteEntry(string route)
        {
            if (string.IsNullOrEmpty(route)) return null;
            m_RouteDict.TryGetValue(route, out var entry);
            return entry;
        }

        public IEnumerable<RouteEntry> GetAllRoutes() => m_RouteDict.Values;

        public void RegisterCondition(string classFullName, string methodName, string condition)
        {
            if (string.IsNullOrEmpty(condition)) return;
            var key = (classFullName ?? "") + "." + (methodName ?? "");
            if (!m_ConditionDict.TryGetValue(key, out var list))
            {
                list = new List<ConditionEntry>();
                m_ConditionDict[key] = list;
            }
            list.Add(new ConditionEntry(condition, classFullName ?? "", methodName ?? ""));
        }

        public List<string> GetConditions(string classFullName, string methodName)
        {
            var key = (classFullName ?? "") + "." + (methodName ?? "");
            if (!m_ConditionDict.TryGetValue(key, out var list)) return null;
            var result = new List<string>();
            foreach (var entry in list) result.Add(entry.condition);
            return result;
        }

        public bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;
            if (condition == "Debug")
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
            if (condition == "Release")
            {
#if DEBUG
                return false;
#else
                return true;
#endif
            }
            return m_ActiveConditions.Contains(condition);
        }

        public void ActivateCondition(string condition)
        {
            if (!string.IsNullOrEmpty(condition)) m_ActiveConditions.Add(condition);
        }

        public void DeactivateCondition(string condition)
        {
            if (!string.IsNullOrEmpty(condition)) m_ActiveConditions.Remove(condition);
        }

        /// <summary>从加载的 SLIR 包注册所有运行时属性</summary>
        public void RegisterFromPackages(List<SLPackageRootJson> packageList)
        {
            if (packageList == null) return;
            foreach (var pkg in packageList)
            {
                if (pkg?.moduleList == null) continue;
                foreach (var mod in pkg.moduleList)
                {
                    if (mod == null) continue;
                    // 类级别的属性
                    if (mod.classList != null)
                    {
                        foreach (var cls in mod.classList)
                        {
                            if (cls?.attributeList == null) continue;
                            foreach (var attr in cls.attributeList)
                            {
                                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;
                                ProcessRuntimeAttribute(attr.name, attr.args, cls.fullName ?? cls.name ?? "", "", attr.handleType);
                            }
                        }
                    }
                    // 方法级别的属性
                    if (mod.methodList != null)
                    {
                        foreach (var mtd in mod.methodList)
                        {
                            if (mtd?.attributeList == null) continue;
                            var classFullName = mtd.declaringTypeFullName ?? "";
                            var methodName = mtd.name ?? mtd.id ?? "";
                            foreach (var attr in mtd.attributeList)
                            {
                                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;
                                ProcessRuntimeAttribute(attr.name, attr.args, classFullName, methodName, attr.handleType);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessRuntimeAttribute(string attrName, List<string> args, string classFullName, string methodName, int handleType)
        {
            // 只处理 Runtime 类型的属性（handleType == 1）
            if (handleType != 1) return;

            string arg0 = (args != null && args.Count > 0) ? args[0] : "";
            switch (attrName)
            {
                case "Route":
                    RegisterRoute(classFullName, methodName, arg0);
                    break;
                case "Condition":
                    RegisterCondition(classFullName, methodName, arg0);
                    break;
            }
        }

        public void Clear()
        {
            m_RouteDict.Clear();
            m_ConditionDict.Clear();
            m_ActiveConditions.Clear();
        }
    }
}
