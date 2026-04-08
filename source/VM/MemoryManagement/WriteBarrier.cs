namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>
    /// Placeholder for a Dart-style remembered set / SATB barrier when an old object gains a reference to a young object.
    /// Call sites can be added where <see cref="SObject"/> fields are mutated; currently a no-op.
    /// </summary>
    public static class WriteBarrier
    {
        public static void NotifyReferenceStore(SObject? container, SObject? newReference)
        {
            _ = container;
            _ = newReference;
        }
    }
}
