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
        public static Dictionary<int, SObject> classObjectDict = new Dictionary<int, SObject>();
        private static readonly Dictionary<int, SObject> s_ObjectById = new Dictionary<int, SObject>();
        private static readonly object s_ObjectByIdGate = new object();
        //public static Dictionary<int, ArrayObject> arrayObjectDict = new Dictionary<int, ArrayObject>();

        public static void AddClassObject(SObject cl)
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
                s_ObjectById[obj.hashCode] = obj;
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
            if (sobj == null) return;
            try
            {
                UnregisterObjectById(sobj.hashCode);
                int key = sobj.GetHashCode();
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
                sobj = new SObject(EVMType.Boolean);
            }
            //else if (name == "Core.Num" || name == "Num")
            else if (rt == RuntimeTypeManager.numRuntimeType)
            {
                // abstract numeric base: create a Float64Object as default runtime representation
                sobj = new SObject(EVMType.Float64);
                sobj.runtimeType = RuntimeTypeManager.numRuntimeType;
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
                sobj = new SObject(EVMType.UInt8);
            }
            else if (rt == RuntimeTypeManager.int8RuntimeType)
            //else if (name == "Core.SByte" || name == "SByte")
            {
                sobj = new SObject(EVMType.Int8);
            }
            else if (rt == RuntimeTypeManager.int16RuntimeType)
            //else if (name == "Core.Int16" || name == "Int16")
            {
                sobj = new SObject(EVMType.Int16);
            }
            else if(rt == RuntimeTypeManager.uint16RuntimeType)
            //else if (name == "Core.UInt16" || name == "UInt16")
            {
                sobj = new SObject(EVMType.UInt16);
            }
            else if (rt == RuntimeTypeManager.int32RuntimeType)
            {
                sobj = new SObject(EVMType.Int32);
            }
            else if( rt == RuntimeTypeManager.uint32RuntimeType)
            //else if (name == "Core.UInt32" || name == "UInt32")
            {
                sobj = new SObject(EVMType.UInt32);
            }
            else if (rt == RuntimeTypeManager.int64RuntimeType)
            {
                sobj = new SObject(EVMType.Int64);
            }
            else if (rt == RuntimeTypeManager.uint64RuntimeType)
            {
                sobj = new SObject(EVMType.UInt64);
            }
            else if (rt == RuntimeTypeManager.float32RuntimeType)
            {
                sobj = new SObject(EVMType.Float32);
            }
            else if (rt == RuntimeTypeManager.float64RuntimeType)
            {
                sobj = new SObject(EVMType.Float64);
            }
            else if (rt == RuntimeTypeManager.stringRuntimeType)
            {
                sobj = new StringObject("");
            }
            else if (rt == RuntimeTypeManager.voidRuntimeType)
            {
                sobj = new SObject(EVMType.Object);
                sobj.runtimeType = RuntimeTypeManager.voidRuntimeType;
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
            }
            else
            {
                var co = new SObject(rt);
                if (isCreateMemObject)
                {
                    co.CreateObject();
                }
                sobj = co;
            }
            // Observable refCount for SystemObjectRefCount / Object.refCount: manual Retain adds on top.
            if (sobj != null && sobj.refCount == 0
                && (sobj.eType == EVMType.Class || sobj.eType == EVMType.Type || sobj.eType == EVMType.Array || sobj.eType == EVMType.Object))
                sobj.refCount = 1;
            RegisterObject(sobj);
            SlMemoryManager.Instance.RegisterAllocation(sobj);
            return sobj;
        }
    }
}