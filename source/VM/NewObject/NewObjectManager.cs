//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.VM
{
    public class ObjectObjectManager
    {
        //public static Dictionary<int, DataObject> dataObjectDict = new Dictionarny<int, DataObject>();
        public static Dictionary<int, VMObjectHeader> classObjectDict = new Dictionary<int, VMObjectHeader>();
        //public static Dictionary<int, ArrayObject> arrayObjectDict = new Dictionary<int, ArrayObject>();

        public static void AddClassObject(VMObjectHeader cl)
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
        public static VMObjectHeader CreateObjectByRuntimeType(RuntimeType rt, bool isCreateMemObject = false)
        {
            VMObjectHeader sobj = new VMObjectHeader();
            string name = rt.runtimeClass.name;
            //if (name == "Core.Boolean" || name == "Boolean")
            //{
            //    sobj = new VMBooleanObject();
            //    sobj2.hea
            //    sobj.typeId = 1;
            //}
            //else if (name == "Core.Object" || name == "Object")
            //{
            //    var ao = new AnyObject();
            //    ao.SetValue(EType.Object, ao);
            //    sobj = ao;
            //}
            //else if (name == "Core.Byte" || name == "Byte")
            //{
            //    sobj = new UInt8Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.SByte" || name == "SByte")
            //{
            //    sobj = new Int8Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.Int16" || name == "Int16")
            //{
            //    sobj = new Int16Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.UInt16" || name == "UInt16")
            //{
            //    sobj = new UInt16Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.Int32" || name == "Int32")
            //{
            //    sobj = new Int32Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.UInt32" || name == "UInt32")
            //{
            //    sobj = new UInt32Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.Int64" || name == "Int64")
            //{
            //    sobj = new Int64Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.UInt64" || name == "UInt64")
            //{
            //    sobj = new UInt64Object(0);
            //    sobj.typeId = 3;
            //}
            //else if (name == "Core.Float32" || name == "Float32")
            //{
            //    sobj = new Float32Object();
            //    sobj.typeId = 4;
            //}
            //else if (name == "Core.Float64" || name == "Float64")
            //{
            //    sobj = new Float64Object();
            //    sobj.typeId = 5;
            //}
            //else if (name == "Core.String" || name == "String")
            //{
            //    sobj = new StringObject("");
            //    sobj.typeId = 10;
            //}
            //else if (name == "Core.Void" || name == "Void")
            //{
            //    sobj = new VoidObject();
            //    sobj.typeId = 0;
            //}
            //else if (name == "Core.Array" || name == "Array")
            //{
            //    sobj = new ArrayObject(EArrayType.Array, 0);
            //    sobj.typeId = 0;
            //}
            //else if (name == "Core.Type" || name == "Type")
            //{
            //    sobj = new ClassObject(rt);
            //    sobj.typeId = 0;
            //}
            //else
            //{
            //    var co = new ClassObject(rt);
            //    if (isCreateMemObject)
            //    {
            //        co.CreateObject();
            //    }
            //    sobj = co;
            //}
            return sobj;
        }
        /*
        public static void SetObjectByValue(SObject obj, ref RuntimeValue RuntimeValue)
        {
            if (RuntimeValue.isNull)
            {
                obj.SetNull();
                return;
            }
            switch (RuntimeValue.eType)
            {
                case EType.Null:
                    {
                        obj.SetNull();
                    }
                    break;
                case EType.Boolean:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Boolean, RuntimeValue.int8Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Boolean, RuntimeValue.int8Value);
                            return;
                        }
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        boolObj.SetValue(RuntimeValue.int8Value == 1);
                    }
                    break;
                case EType.UInt8:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt8, RuntimeValue.int8Value);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt8, RuntimeValue.int8Value);
                            return;
                        }
                        UInt8Object byteObj = obj as UInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        byteObj.SetValue(RuntimeValue.int8Value);
                    }
                    break;
                case EType.Int8:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int8, RuntimeValue.sint8Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int8, RuntimeValue.sint8Value);
                            return;
                        }
                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        byteObj.SetValue(RuntimeValue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int16, RuntimeValue.int16Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int16, RuntimeValue.int16Value);
                            return;
                        }
                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int16Obj.SetValue(RuntimeValue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt16, RuntimeValue.uint16Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt16, RuntimeValue.uint16Value);
                            return;
                        }
                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint16Obj.SetValue(RuntimeValue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int32, RuntimeValue.int32Value);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int32, RuntimeValue.int32Value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(RuntimeValue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt32, RuntimeValue.uint32Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt32, RuntimeValue.uint32Value);
                            return;
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(RuntimeValue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int64, RuntimeValue.int64Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int64, RuntimeValue.int64Value);
                            return;
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(RuntimeValue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt64, RuntimeValue.uint64Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt64, RuntimeValue.uint64Value);
                            return;
                        }
                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint64Obj.SetValue(RuntimeValue.uint64Value);
                    }
                    break;
                case EType.String:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.String, RuntimeValue.stringValue);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.String, RuntimeValue.stringValue);
                            return;
                        }
                        ClassObject classobj = obj as ClassObject;
                        if (classobj != null)
                        {
                        }
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(RuntimeValue.stringValue);
                    }
                    break;
                case EType.Array:
                    {
                        if (obj is ClassObject co)
                        {
                            var ao = RuntimeValue.sobject as ArrayObject;
                            Debug.Assert(ao != null);
                            co.SetValue(ao);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EType.Float32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Float32, RuntimeValue.floatValue);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Float32, RuntimeValue.floatValue);
                            return;
                        }
                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        floatObj.SetValue(RuntimeValue.floatValue);
                    }
                    break;
                case EType.Float64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Float64, RuntimeValue.doubleValue);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Float64, RuntimeValue.doubleValue);
                            return;
                        }
                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        doubleObj.SetValue(RuntimeValue.doubleValue);
                    }
                    break;
                case EType.Class:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(RuntimeValue.sobject as ClassObject);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            if (RuntimeValue.sobject is ClassObject co)
                            {
                                anyObject.SetValue(EType.Class, co.value);
                            }
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(RuntimeValue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(RuntimeValue.int8Value == 1 ? true : false);
                            return;
                        }
                        ClassObject classObj = obj as ClassObject;
                        if (classObj == null)
                        {
                            Debug.Write("该类型不是Class类型!!");
                            return;
                        }
                        classObj.SetValue(RuntimeValue.sobject as ClassObject);
                    }
                    break;
                case EType.Object:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Class, obj);
                            return;
                        }
                    }
                    break;
            }
        }
    */
    }
}
