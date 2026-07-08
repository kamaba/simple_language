//****************************************************************************
//  File:      ObjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.MemoryManagement;
using SimpleLanguage.VM.Runtime;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public class ObjectManager
    {
        //public static Dictionary<int, DataObject> dataObjectDict = new Dictionarny<int, DataObject>();
        public static Dictionary<int, ClassObject> classObjectDict = new Dictionary<int, ClassObject>();
        private static readonly Dictionary<int, SObject> s_ObjectById = new Dictionary<int, SObject>();
        private static readonly object s_ObjectByIdGate = new object();
        //public static Dictionary<int, ArrayObject> arrayObjectDict = new Dictionary<int, ArrayObject>();

        public static void AddClassObject(ClassObject cl)
        {
            if (!classObjectDict.ContainsKey(cl.GetHashCode()))
            {
                classObjectDict.Add(cl.GetHashCode(), cl);
            }
            RegisterObject(cl);
        }

        public static void RegisterObject(SObject obj)
        {
            if (obj == null) return;
            lock (s_ObjectByIdGate)
            {
                s_ObjectById[obj.id] = obj;
            }
        }

        public static SObject? GetObjectById(int id)
        {
            if (id <= 0) return null;
            lock (s_ObjectByIdGate)
            {
                return s_ObjectById.TryGetValue(id, out var obj) ? obj : null;
            }
        }

        public static void UnregisterObjectById(int id)
        {
            if (id <= 0) return;
            lock (s_ObjectByIdGate)
            {
                s_ObjectById.Remove(id);
            }
        }

        /// <summary>Manual strong-reference retain counter (pairs with system Object.ref).</summary>
        public static void RetainObject(SObject obj)
        {
            if (obj == null) return;
            obj.refCount++;
        }

        /// <summary>
        /// Manual release of strong-reference counter.
        /// When it reaches zero, remove ClassObject registry entry (legacy-compatible behavior).
        /// </summary>
        public static void ReleaseObject(SObject obj)
        {
            if (obj == null) return;
            if (obj.refCount <= 0) return;
            obj.refCount--;
            if (obj.refCount != 0) return;
            TryRemoveClassObjectRegistryEntry(obj);
        }

        /// <summary>For paths that force <see cref="SObject.refCount"/> to zero directly (e.g. SystemObjectFree).</summary>
        public static void OnManualRefForcedZero(SObject obj)
        {
            if (obj == null) return;
            TryRemoveClassObjectRegistryEntry(obj);
        }

        private static void TryRemoveClassObjectRegistryEntry(SObject sobj)
        {
            if (sobj is not ClassObject co) return;
            try
            {
                UnregisterObjectById(co.id);
                int key = co.GetHashCode();
                if (classObjectDict.ContainsKey(key))
                    classObjectDict.Remove(key);
            }
            catch
            {
                // Keep legacy behavior: cleanup must never break runtime path.
            }
        }
        //public static void AddArrayObject(ArrayObject cl)
        //{
        //    if (!arrayObjectDict.ContainsKey(cl.GetHashCode()))
        //    {
        //        arrayObjectDict.Add(cl.GetHashCode(), cl);
        //    }
        //}
        public static SObject CreateObjectByRuntimeType(RuntimeType rt, bool isCreateMemObject = false)
        {
            SObject sobj = null;
            string name = rt.runtimeClass.name;
            //if (name == "Core.Boolean" || name == "Boolean")
            if( rt == RuntimeTypeManager.boolRuntimeType )
            {
                sobj = new BoolObject(false);
                sobj.typeId = 1;
            }
            //else if (name == "Core.Num" || name == "Num")
            else if (rt == RuntimeTypeManager.numRuntimeType)
            {
                // abstract numeric base: create a Float64Object as default runtime representation
                sobj = new NumObject();
                sobj.typeId = 5;
            }
            else if (rt == RuntimeTypeManager.objectRuntimeType)
            //else if (name == "Core.Object" || name == "Object")
            {
                var ao = new SObject(EVMType.Object);
                sobj = ao;
            }
            else if (rt == RuntimeTypeManager.uint8RuntimeType)
            //else if (name == "Core.Byte" || name == "Byte")
            {
                sobj = new UInt8Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.int8RuntimeType)
            //else if (name == "Core.SByte" || name == "SByte")
            {
                sobj = new Int8Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.int16RuntimeType)
            //else if (name == "Core.Int16" || name == "Int16")
            {
                sobj = new Int16Object(0);
                sobj.typeId = 3;
            }
            else if(rt == RuntimeTypeManager.uint16RuntimeType)
            //else if (name == "Core.UInt16" || name == "UInt16")
            {
                sobj = new UInt16Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.int32RuntimeType)
            {
                sobj = new Int32Object(0);
                sobj.typeId = 3;
            }
            else if( rt == RuntimeTypeManager.uint32RuntimeType)
            //else if (name == "Core.UInt32" || name == "UInt32")
            {
                sobj = new UInt32Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.int64RuntimeType)
            {
                sobj = new Int64Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.uint64RuntimeType)
            {
                sobj = new UInt64Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.float32RuntimeType)
            {
                sobj = new Float32Object(0.0f);
                sobj.typeId = 4;
            }
            else if (rt == RuntimeTypeManager.float64RuntimeType)
            {
                sobj = new Float64Object(0.0d);
                sobj.typeId = 5;
            }
            else if (rt == RuntimeTypeManager.stringRuntimeType)
            {
                sobj = new StringObject("");
                sobj.typeId = 10;
            }
            else if (rt == RuntimeTypeManager.voidRuntimeType)
            {
                sobj = new VoidObject();
                sobj.typeId = 0;
            }
            else if (name == "Core.Array" || name == "Array"
                || name == "Core.Array<T>")
            {
                //var ao = new ArrayObject( rt, 0);
                //ao.typeId = 0;
                //if (isCreateMemObject)
                //{
                //    ao.CreateObject();
                //}
                //sobj = ao;
                Debug.Assert(false);
            }
            else if (rt == RuntimeTypeManager.typeRuntimeType)
            {
                sobj = new TypeObject(rt);
                sobj.typeId = 0;
            }
            else
            {
                var co = new ClassObject(rt);
                if (isCreateMemObject)
                {
                    co.CreateObject();
                }
                sobj = co;
            }
            // Observable refCount for SystemObjectRefCount / Object.refCount: manual Retain adds on top.
            if (sobj != null && sobj.refCount == 0
                && (sobj is ClassObject || sobj is TypeObject || sobj is ArrayObject || sobj.eType == EVMType.Object))
                sobj.refCount = 1;
            RegisterObject(sobj);
            SlMemoryManager.Instance.RegisterAllocation(sobj);
            return sobj;
        }
    }
}