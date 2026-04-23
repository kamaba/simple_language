using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM
{
    public sealed class SLIRRuntimeLoadModel
    {
        public string rootPath { get; init; } = string.Empty;
        public string rootDirectory { get; init; } = string.Empty;
        public string format { get; init; } = string.Empty;
        public List<SLPackageRootJson> packageList { get; init; } = new();
        public List<SLAssembly> assemblyList { get; init; } = new();
        public SLPackageRootJson? currentPackage { get; init; }
        public SLAssembly? currentAssembly { get; init; }
    }

    public interface ISLIRRuntimeLoader
    {
        bool CanLoad(string path);
        string? ResolveInputPath(string[] args);
        SLIRRuntimeLoadModel LoadInExecutionOrder(string path);
    }

    public sealed class SLIRJsonRuntimeLoader : ISLIRRuntimeLoader
    {
        public bool CanLoad(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        public string? ResolveInputPath(string[] args)
        {
            return SLIRJsonModuleLoader.ResolveJsonPath(args);
        }

        public SLIRRuntimeLoadModel LoadInExecutionOrder(string path)
        {
            var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(path);
            var asmList = graph.packageList.Select(SLIRJsonModuleLoader.BuildRuntimeModel).ToList();
            var currentPkg = graph.packageList.Count > 0 ? graph.packageList[graph.packageList.Count - 1] : null;
            var currentAsm = asmList.Count > 0 ? asmList[asmList.Count - 1] : null;

            return new SLIRRuntimeLoadModel
            {
                rootPath = graph.rootPackagePath,
                rootDirectory = graph.rootDirectory,
                format = "json",
                packageList = graph.packageList,
                assemblyList = asmList,
                currentPackage = currentPkg,
                currentAssembly = currentAsm,
            };
        }
    }

    public sealed class SLIRBinRuntimeLoader : ISLIRRuntimeLoader
    {
        public bool CanLoad(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && (path.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".slir", StringComparison.OrdinalIgnoreCase));
        }

        public string? ResolveInputPath(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            var first = args[0];
            return CanLoad(first) ? first : null;
        }

        public SLIRRuntimeLoadModel LoadInExecutionOrder(string path)
        {
            throw new NotSupportedException("SLIR binary runtime loader is not integrated yet. Please use JSON package for now.");
        }
    }

    public static class SLIRRuntimeLoader
    {
        private static readonly ISLIRRuntimeLoader[] s_Loaders =
        {
            new SLIRJsonRuntimeLoader(),
            new SLIRBinRuntimeLoader(),
        };

        public static string? ResolveInputPath(string[] args)
        {
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                var candidate = args[0];
                for (int i = 0; i < s_Loaders.Length; i++)
                {
                    if (s_Loaders[i].CanLoad(candidate))
                        return candidate;
                }
            }

            for (int i = 0; i < s_Loaders.Length; i++)
            {
                var resolved = s_Loaders[i].ResolveInputPath(args);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;
            }
            return null;
        }

        public static SLIRRuntimeLoadModel LoadInExecutionOrder(string path)
        {
            for (int i = 0; i < s_Loaders.Length; i++)
            {
                var loader = s_Loaders[i];
                if (loader.CanLoad(path))
                    return loader.LoadInExecutionOrder(path);
            }
            throw new NotSupportedException($"Unsupported SLIR input: {path}");
        }
    }
}
