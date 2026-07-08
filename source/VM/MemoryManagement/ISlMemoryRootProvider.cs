using System.Collections.Generic;

namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Optional extra GC roots (statics, native handles, etc.).</summary>
    public interface ISlMemoryRootProvider
    {
        void AppendRoots(HashSet<SObject> roots);
    }
}
