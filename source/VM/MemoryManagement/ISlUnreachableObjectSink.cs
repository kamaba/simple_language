using System.Collections.Generic;

namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Observability when the SL tracer finds unreachable <see cref="SObject"/> instances (CLR may still retain them).</summary>
    public interface ISlUnreachableObjectSink
    {
        void OnUnreachableObjects(IReadOnlyList<SObject> unreachable);
    }
}
