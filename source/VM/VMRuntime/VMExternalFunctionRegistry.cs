//****************************************************************************
//  VMExternalFunctionRegistry: 动态加载外部 DLL，注册和分发外部函数。
//
//  使用方式：
//    VMExternalFunctionRegistry.LoadDirectory("path/to/dlls");
//    VMExternalFunctionRegistry.TryInvoke("Console.println", args, out var ret);
//****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SimpleLanguage.VM.Runtime
{
    public static class VMExternalFunctionRegistry
    {
        private static readonly Dictionary<string, SLExternalFunctionDelegate> s_functions =
            new(StringComparer.Ordinal);
        private static readonly List<Assembly> s_loadedAssemblies = new();

        public static int FunctionCount => s_functions.Count;
        public static int LoadedModuleCount => s_loadedAssemblies.Count;

        /// <summary>
        /// 注册单个外部函数。
        /// </summary>
        public static void Register(string name, SLExternalFunctionDelegate fn)
        {
            if (string.IsNullOrEmpty(name) || fn == null) return;
            s_functions[name] = fn;
        }

        /// <summary>
        /// 查找并调用已注册的外部函数。返回 false 表示未找到。
        /// </summary>
        public static bool TryInvoke(string name, object?[] args, out object? result)
        {
            result = null;
            if (!s_functions.TryGetValue(name, out var fn))
                return false;
            result = fn(args);
            return true;
        }

        /// <summary>
        /// 检查函数是否已注册。
        /// </summary>
        public static bool IsRegistered(string name)
        {
            return s_functions.ContainsKey(name);
        }

        /// <summary>
        /// 加载目录下所有 .dll 文件，查找实现 ISLExternalFunctionModule 的类型并注册函数。
        /// </summary>
        public static int LoadDirectory(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
                return 0;

            int totalRegistered = 0;
            foreach (var dllPath in Directory.GetFiles(dirPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                totalRegistered += LoadDll(dllPath);
            }
            return totalRegistered;
        }

        /// <summary>
        /// 加载单个 DLL 文件，查找实现 ISLExternalFunctionModule 的类型并注册函数。
        /// </summary>
        public static int LoadDll(string dllPath)
        {
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"[ExternalFunction] DLL not found: {dllPath}");
                return 0;
            }

            Assembly asm;
            try
            {
                asm = Assembly.LoadFrom(dllPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExternalFunction] Failed to load assembly: {dllPath}, error: {ex.Message}");
                return 0;
            }

            s_loadedAssemblies.Add(asm);

            var moduleType = typeof(ISLExternalFunctionModule);
            int registered = 0;

            foreach (var type in asm.GetExportedTypes())
            {
                if (!moduleType.IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                    continue;

                ISLExternalFunctionModule? moduleInstance;
                try
                {
                    moduleInstance = Activator.CreateInstance(type) as ISLExternalFunctionModule;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExternalFunction] Failed to instantiate {type.FullName}: {ex.Message}");
                    continue;
                }

                if (moduleInstance == null)
                    continue;

                var registrar = new Registrar();
                try
                {
                    moduleInstance.Register(registrar);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExternalFunction] Register() failed for {type.FullName}: {ex.Message}");
                    continue;
                }

                foreach (var kv in registrar.GetRegistered())
                {
                    s_functions[kv.Key] = kv.Value;
                    registered++;
                }

                Console.WriteLine($"[ExternalFunction] Module loaded: {type.FullName}, registered {registrar.GetRegistered().Count} functions");
            }

            if (registered == 0)
            {
                Console.WriteLine($"[ExternalFunction] No external function modules found in: {dllPath}");
            }

            return registered;
        }

        /// <summary>
        /// 清除所有已注册的外部函数（用于 VM 重置）。
        /// </summary>
        public static void Clear()
        {
            s_functions.Clear();
            s_loadedAssemblies.Clear();
        }

        private sealed class Registrar : ISLExternalFunctionRegistrar
        {
            private readonly Dictionary<string, SLExternalFunctionDelegate> _map = new(StringComparer.Ordinal);

            public void Register(string name, SLExternalFunctionDelegate fn)
            {
                if (string.IsNullOrEmpty(name) || fn == null) return;
                _map[name] = fn;
            }

            public Dictionary<string, SLExternalFunctionDelegate> GetRegistered() => _map;
        }
    }
}
