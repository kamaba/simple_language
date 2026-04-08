namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Tunables for Dart-like nursery triggers (approximate).</summary>
    public sealed class SlMemoryOptions
    {
        /// <summary>Allocations in nursery before a young collection is attempted (0 = disable auto young GC).</summary>
        public int NurseryAllocationBudget { get; set; } = 256;

        /// <summary>When true, <see cref="SlMemoryManager.RegisterAllocation"/> may run a young collection.</summary>
        public bool AutoYoungCollection { get; set; } = true;

        /// <summary>Optional: request CLR GC after SL young/full collections (diagnostics / pressure alignment).</summary>
        public bool RequestClrGcAfterSlCollection { get; set; } = false;
    }
}
