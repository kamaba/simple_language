using System;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Enumerates outgoing <see cref="SObject"/> references from a graph node (mark phase).</summary>
    internal static class SObjectGraphTracer
    {
        public static void VisitOutgoing(SObject current, Action<SObject> visitChild)
        {
            switch (current)
            {
                case ArrayObject ao:
                    VisitArrayElements(ao, visitChild);
                    VisitClassMembers(ao, visitChild);
                    break;
                case SObject co:
                    VisitClassMembers(co, visitChild);
                    break;
            }
        }

        private static void VisitClassMembers(SObject co, Action<SObject> visitChild)
        {
            var rc = co.runtimeType?.runtimeClass;
            int n = rc?.nonStaticIRMetaVariableList?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                var ro = co.GetMemberRuntimeObject(i);
                if (ro?.sobject != null)
                    visitChild(ro.sobject);
            }
        }

        private static void VisitArrayElements(ArrayObject ao, Action<SObject> visitChild)
        {
            int len = ao.length;
            for (int i = 0; i < len; i++)
            {
                object? v = ao.GetValue(i);
                if (v is SObject so)
                    visitChild(so);
            }
        }
    }
}
