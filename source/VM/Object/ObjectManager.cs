//****************************************************************************
//  File:      ObjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public class ObjectManager
    {
        //public static Dictionary<int, DataObject> dataObjectDict = new Dictionarny<int, DataObject>();
        public static Dictionary<int, ClassObject> classObjectDict = new Dictionary<int, ClassObject>();
        //public static Dictionary<int, ArrayObject> arrayObjectDict = new Dictionary<int, ArrayObject>();

        public static void AddClassObject(ClassObject cl)
        {
            if (!classObjectDict.ContainsKey(cl.GetHashCode()))
            {
                classObjectDict.Add(cl.GetHashCode(), cl);
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
            else if (rt == RuntimeTypeManager.byteRuntimeType)
            //else if (name == "Core.Byte" || name == "Byte")
            {
                sobj = new Int8Object(0);
                sobj.typeId = 3;
            }
            else if (rt == RuntimeTypeManager.sbyteRuntimeType)
            //else if (name == "Core.SByte" || name == "SByte")
            {
                sobj = new SInt8Object(0);
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
                || name == "Array<T>" )
            {
                var ao = new ArrayObject( rt, 0);
                ao.typeId = 0;
                if (isCreateMemObject)
                {
                    ao.CreateObject();
                }
                sobj = ao;
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
            return sobj;
        }
    }
}