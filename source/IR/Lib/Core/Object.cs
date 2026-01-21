//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;
using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;

namespace SimpleLanguage.Lib
{
    public class MemTable
    {
        public int refCount = 0;
    }
    public class BaseObjectData
    {
        public int hashCode = 0;
        public RuntimeType runtimeType = null;
        public MemTable memTable = null;
        public EType etype = EType.None;
    }
    public class AnyObjectData : BaseObjectData
    {
        public BaseObjectData data;
    }
    public static class ObjectClass
    {
        // cache Type wrapper objects per runtime class id
        private static readonly Dictionary<int, TypeObject> s_typeObjectCache = new Dictionary<int, TypeObject>();

        public static SObject GetObjectType(SObject sobj)
        {
            if (sobj == null)
                return null;

            try
            {
                var rt = sobj.runtimeType;
                if (rt == null)
                {
                    return null;
                }

                int key = rt.irClass != null ? rt.irClass.id : rt.GetHashCode();

                lock (s_typeObjectCache)
                {
                    if (s_typeObjectCache.TryGetValue(key, out var exist))
                        return exist;

                    // create a public TypeObject and cache it
                    var to = new TypeObject(rt);
                    s_typeObjectCache[key] = to;
                    return to;
                }
            }
            catch
            {
                return null;
            }
        }
        
        public static int GetHashCodeByObject(BaseObjectData obj)
        {
            if (obj == null) return 0;
            return obj.hashCode;
        }
        public static int GetHashCodeBySObject(System.Object obj)
        {
            if (obj == null) return 0;
            SObject sobj = obj as SObject;
            if (sobj == null) return 0;
            return sobj.id;
        }
        public static int RefCount(System.Object obj)
        {
            if (obj == null) return 0;
            SObject sobj = obj as SObject;
            if (sobj == null) return 0;
            return sobj.refCount;
        }
        public static SObject ObjectWeakRef(System.Object obj)
        {
            if (obj == null) return null;
            SObject sobj = obj as SObject;
            return sobj;
        }
        public static System.Object CloneObject(System.Object obj)
        {
            // Shallow clone: for now return the same object reference.
            // Future: perform deep clone based on runtimeType if needed.
            return obj;
        }
        public static SObject ObjectRef(System.Object obj)
        {
            if (obj == null) return null;
            SObject sobj = obj as SObject;
            return sobj;
        }
        public static bool EqualObject( System.Object obj1, System.Object obj2 )
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            SObject sobj1 = obj1 as SObject;
            SObject sobj2 = obj2 as SObject;
            return sobj1 == sobj2;
        }
        
        // decrease reference count and free resources when zero
        public static void FreeObject(System.Object obj)
        {
            if (obj == null) return;
            SObject sobj = obj as SObject;
            if (sobj == null) return;
            if (sobj.refCount > 0) sobj.refCount--;
            if (sobj.refCount <= 0)
            {
                // try to remove from manager if it's a class object
                try
                {
                    var co = sobj as ClassObject;
                    if (co != null)
                    {
                        var dict = ObjectManager.classObjectDict;
                        int key = co.GetHashCode();
                        if (dict.ContainsKey(key))
                        {
                            dict.Remove(key);
                        }
                    }
                }
                catch { }
            }
        }

        // increase reference count and register object
        public static void ReleaseObject(System.Object obj)
        {
            if (obj == null) return;
            SObject sobj = obj as SObject;
            if (sobj == null) return;
            sobj.refCount++;
            try
            {
                var co = sobj as ClassObject;
                if (co != null)
                {
                    var dict = ObjectManager.classObjectDict;
                    int key = co.GetHashCode();
                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(key, co);
                    }
                }
            }
            catch { }
        }

        // public TypeObject wrapper for runtime types
        public class TypeObject : ClassObject
        {
            public RuntimeType TargetRuntimeType { get; }

            public TypeObject(RuntimeType target) : base(target, false)
            {
                TargetRuntimeType = target;
                // ensure runtimeType is set on base
                this.SetClassObject(this);
            }

            public string metaClassName
            {
                get
                {
                    try { return TargetRuntimeType?.irClass?.irName ?? "no_type"; }
                    catch { return "no_type"; }
                }
            }

            public override string ToFormatString()
            {
                return metaClassName;
            }

            public override string ToString()
            {
                return ToFormatString();
            }
        }
    }
}
