//****************************************************************************
//  File:      ObjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
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
            string name = rt.irClass.irName;
            if (name == "Core.Boolean" || name == "Boolean")
            {
                sobj = new BoolObject(false);
                sobj.typeId = 1;
            }
            else if (name == "Core.Object" || name == "Object")
            {
                var ao = new SObject(EVMType.Object);
                sobj = ao;
            }
            else if (name == "Core.Byte" || name == "Byte")
            {
                sobj = new Int8Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.SByte" || name == "SByte")
            {
                sobj = new SInt8Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.Int16" || name == "Int16")
            {
                sobj = new Int16Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.UInt16" || name == "UInt16")
            {
                sobj = new UInt16Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.Int32" || name == "Int32")
            {
                sobj = new Int32Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.UInt32" || name == "UInt32")
            {
                sobj = new UInt32Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.Int64" || name == "Int64")
            {
                sobj = new Int64Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.UInt64" || name == "UInt64")
            {
                sobj = new UInt64Object(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.Float32" || name == "Float32")
            {
                sobj = new Float32Object(0.0f);
                sobj.typeId = 4;
            }
            else if (name == "Core.Float64" || name == "Float64")
            {
                sobj = new Float64Object(0.0d);
                sobj.typeId = 5;
            }
            else if (name == "Core.String" || name == "String")
            {
                sobj = new StringObject("");
                sobj.typeId = 10;
            }
            else if (name == "Core.Void" || name == "Void")
            {
                sobj = new VoidObject();
                sobj.typeId = 0;
            }
            else if (name == "Core.Array" || name == "Array")
            {
                sobj = new ArrayObject( RuntimeTypeManager.arrayRuntimeType, EArrayType.Array, 0);
                sobj.typeId = 0;
            }
            else if (name == "Core.Type" || name == "Type")
            {
                sobj = new ClassObject(rt);
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