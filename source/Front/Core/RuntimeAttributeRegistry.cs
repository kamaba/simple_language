//****************************************************************************
//  File:      RuntimeAttributeRegistry.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/18 12:00:00
//  Description: runtime attribute registry for Route, Condition etc.
//****************************************************************************

using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 运行时属性注册表。
    /// 存储 Route("/action/getfin")、Condition("Debug") 等运行时属性的注册信息，
    /// 供框架代码（类似 FastAPI 的路由分发器）查询。
    /// 
    /// 此注册表在两个时机被填充：
    /// 1. 编译导出时（Export 阶段）：属性数据被序列化到 SLModulePackage 中
    /// 2. VM 加载模块时（RuntimeLoad 阶段）：从 SLIR 包反序列化后注册
    /// </summary>
    public class RuntimeAttributeRegistry
    {
        public static RuntimeAttributeRegistry s_Instance;
        public static RuntimeAttributeRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new RuntimeAttributeRegistry();
                return s_Instance;
            }
        }

        /// <summary>路由目标信息</summary>
        public class RouteEntry
        {
            public string route { get; set; }
            public string classAllName { get; set; }
            public string methodAllName { get; set; }
            public string methodName { get; set; }

            public RouteEntry(string route, string classAllName, string methodAllName, string methodName)
            {
                this.route = route;
                this.classAllName = classAllName;
                this.methodAllName = methodAllName;
                this.methodName = methodName;
            }
        }

        /// <summary>条件标记信息</summary>
        public class ConditionEntry
        {
            public string condition { get; set; }
            public string classAllName { get; set; }
            public string methodAllName { get; set; }

            public ConditionEntry(string condition, string classAllName, string methodAllName)
            {
                this.condition = condition;
                this.classAllName = classAllName;
                this.methodAllName = methodAllName;
            }
        }

        // 路由表: route -> RouteEntry
        private Dictionary<string, RouteEntry> m_RouteDict = new Dictionary<string, RouteEntry>();
        // 条件表: (classAllName, methodAllName) -> List<ConditionEntry>
        private Dictionary<string, List<ConditionEntry>> m_ConditionDict = new Dictionary<string, List<ConditionEntry>>();
        // 活跃的条件集合（运行时设置的条件标记）
        private HashSet<string> m_ActiveConditions = new HashSet<string>();

        #region Route Registration

        /// <summary>注册路由</summary>
        public void RegisterRoute(string classAllName, string methodAllName, string route)
        {
            if (string.IsNullOrEmpty(route)) return;
            var entry = new RouteEntry(route, classAllName, methodAllName, ExtractMethodName(methodAllName));
            m_RouteDict[route] = entry;
            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                $"Route registered: '{route}' -> {classAllName}.{methodAllName}");
        }

        /// <summary>通过路由查找目标</summary>
        public RouteEntry GetRouteEntry(string route)
        {
            if (string.IsNullOrEmpty(route)) return null;
            m_RouteDict.TryGetValue(route, out var entry);
            return entry;
        }

        /// <summary>获取所有路由</summary>
        public IEnumerable<RouteEntry> GetAllRoutes()
        {
            return m_RouteDict.Values;
        }

        #endregion

        #region Condition Registration

        /// <summary>注册条件标记</summary>
        public void RegisterCondition(string classAllName, string methodAllName, string condition)
        {
            if (string.IsNullOrEmpty(condition)) return;
            var entry = new ConditionEntry(condition, classAllName, methodAllName);
            var key = BuildConditionKey(classAllName, methodAllName);
            if (!m_ConditionDict.TryGetValue(key, out var list))
            {
                list = new List<ConditionEntry>();
                m_ConditionDict[key] = list;
            }
            list.Add(entry);
            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                $"Condition registered: '{condition}' for {classAllName}.{methodAllName}");
        }

        /// <summary>获取指定类/方法上的条件列表</summary>
        public List<string> GetConditions(string classAllName, string methodAllName)
        {
            var key = BuildConditionKey(classAllName, methodAllName);
            if (!m_ConditionDict.TryGetValue(key, out var list)) return null;
            var result = new List<string>();
            foreach (var entry in list)
                result.Add(entry.condition);
            return result;
        }

        /// <summary>检查条件是否满足</summary>
        public bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;
            // Debug 条件：只在 debug 构建时为 true
            if (condition == "Debug")
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
            // Release 条件：只在 release 构建时为 true
            if (condition == "Release")
            {
#if DEBUG
                return false;
#else
                return true;
#endif
            }
            // 自定义条件：检查是否在活跃条件集合中
            return m_ActiveConditions.Contains(condition);
        }

        /// <summary>设置运行时条件标记为活跃</summary>
        public void ActivateCondition(string condition)
        {
            if (!string.IsNullOrEmpty(condition))
                m_ActiveConditions.Add(condition);
        }

        /// <summary>取消运行时条件标记</summary>
        public void DeactivateCondition(string condition)
        {
            if (!string.IsNullOrEmpty(condition))
                m_ActiveConditions.Remove(condition);
        }

        #endregion

        /// <summary>构建条件字典的 key</summary>
        private string BuildConditionKey(string classAllName, string methodAllName)
        {
            return classAllName + "." + (methodAllName ?? "");
        }

        /// <summary>从方法全名提取简短名</summary>
        private string ExtractMethodName(string methodAllName)
        {
            if (string.IsNullOrEmpty(methodAllName)) return "";
            var idx = methodAllName.LastIndexOf('.');
            return idx >= 0 ? methodAllName.Substring(idx + 1) : methodAllName;
        }

        /// <summary>清空所有注册（用于重新加载）</summary>
        public void Clear()
        {
            m_RouteDict.Clear();
            m_ConditionDict.Clear();
            m_ActiveConditions.Clear();
        }
    }
}
