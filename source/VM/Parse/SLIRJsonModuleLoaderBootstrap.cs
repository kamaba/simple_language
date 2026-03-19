using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    // Shared hook to provide const string lookup across different JSON loaders.
    internal static class SLIRJsonModuleLoaderBootstrap
    {
        private static Dictionary<int, string> s_Dict = new();

        public static void SetConstStringDict(Dictionary<int, string> dict)
        {
            s_Dict = dict ?? new Dictionary<int, string>();
        }

        public static string? TryGetConstString(int id)
        {
            if (s_Dict != null && s_Dict.TryGetValue(id, out var s)) return s;
            return null;
        }
    }
}
