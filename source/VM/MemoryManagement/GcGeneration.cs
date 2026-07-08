namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Dart-inspired logical spaces for SL-managed <see cref="SObject"/> tracking (not moving CLR heaps).</summary>
    public enum GcGeneration : byte
    {
        /// <summary>New / nursery space (young generation).</summary>
        Young = 0,
        /// <summary>Old space after promotion or full collection.</summary>
        Old = 1,
    }
}
