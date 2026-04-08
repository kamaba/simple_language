using System.Collections.Generic;

namespace SimpleLanguage.VM.MemoryManagement
{
    internal static class SlGarbageCollector
    {
        public static HashSet<SObject> MarkFromRoots(IEnumerable<SObject> explicitRoots, HashSet<SObject>? pinned)
        {
            var marked = new HashSet<SObject>();
            var queue = new Queue<SObject>();

            void EnqueueRoot(SObject? r)
            {
                if (r == null) return;
                if (marked.Add(r))
                    queue.Enqueue(r);
            }

            foreach (var r in explicitRoots)
                EnqueueRoot(r);

            if (pinned != null)
            {
                foreach (var p in pinned)
                    EnqueueRoot(p);
            }

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                SObjectGraphTracer.VisitOutgoing(cur, child =>
                {
                    if (child != null && marked.Add(child))
                        queue.Enqueue(child);
                });
            }

            return marked;
        }
    }
}
