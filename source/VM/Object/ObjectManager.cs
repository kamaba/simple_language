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
        public static SObject CreateObjectByRuntimeType( RuntimeType rt, bool isCreateMemObject = false )
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
                var ao = new SObject( EVMType.Object );
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
                sobj = new ArrayObject( EArrayType.Array, 0 );
                sobj.typeId = 0;
            }
            else if( name == "Core.Type" || name == "Type" )
            {
                sobj = new ClassObject( rt );
                sobj.typeId = 0;
            }
            else
            {
                var co = new ClassObject(rt);
                if(isCreateMemObject )
                {
                    co.CreateObject();
                }
                sobj = co;
            }
            return sobj;
        }
        public static void GetObjectByValue(SObject obj, ref SValue svalue)
        {
            if( obj == null )
            {
                svalue.SetNull();
                return;
            }
            if (obj.isNull)
            {
                svalue.SetNull();
                return;
            }
            switch (obj.eType)
            {
                case EVMType.Null:
                    {
                        svalue.SetNull();
                    }
                    break;
                case EVMType.Boolean:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Boolean, svalue.int8Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Boolean, svalue.int8Value);
                        //    return;
                        //}
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        svalue.SetBoolValue(boolObj.value);
                    }
                    break;
                case EVMType.Byte:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Byte, svalue.int8Value);
                        //    return;
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Byte, svalue.int8Value);
                        //    return;
                        //}
                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        svalue.SetInt8Value(byteObj.value);
                    }
                    break;
                case EVMType.SByte:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.SByte, svalue.sint8Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.SByte, svalue.sint8Value);
                        //    return;
                        //}
                        SInt8Object byteObj = obj as SInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        svalue.SetSInt8Value(byteObj.value);
                    }
                    break;
                case EVMType.Int16:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Int16, svalue.int16Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Int16, svalue.int16Value);
                        //    return;
                        //}
                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt16Value(int16Obj.value);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.UInt16, svalue.uint16Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.UInt16, svalue.uint16Value);
                        //    return;
                        //}
                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt16Value(uint16Obj.value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Int32, svalue.int32Value);
                        //    return;
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Int32, svalue.int32Value);
                        //    return;
                        //}
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt32Value(int32Obj.value);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.UInt32, svalue.uint32Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.UInt32, svalue.uint32Value);
                        //    return;
                        //}
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt32Value(uint32Obj.value);
                    }
                    break;
                case EVMType.Int64:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Int64, svalue.int64Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Int64, svalue.int64Value);
                        //    return;
                        //}
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt64Value(int64Obj.value);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.UInt64, svalue.uint64Value);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.UInt64, svalue.uint64Value);
                        //    return;
                        //}
                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt64Value(uint64Obj.value);
                    }
                    break;
                case EVMType.String:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.String, svalue.stringValue);
                        //    return;
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.String, svalue.stringValue);
                        //    return;
                        //}
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetStringValue(stringObj.value);
                    }
                    break;
                case EVMType.Float32:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Float32, svalue.floatValue);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Float32, svalue.floatValue);
                        //    return;
                        //}
                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetFloatValue(floatObj.value);
                    }
                    break;
                case EVMType.Float64:
                    {
                        //TemplateObject to = obj as TemplateObject;
                        //if (to != null)
                        //{
                        //    to.SetValue(EVMType.Float64, svalue.doubleValue);
                        //}
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    anyObject.SetValue(EVMType.Float64, svalue.doubleValue);
                        //    return;
                        //}
                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetDoubleValue(doubleObj.value);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (obj is ClassObject co)
                        {
                            svalue.SetSObject(co.value);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        if (obj.eType == EVMType.Object)
                        {
                            svalue.SetSObject(obj.value as SObject );
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Class:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}
                        if (obj is ClassObject co)
                        {
                            svalue.SetSObject(co.value);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                        /*
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(svalue.int8Value == 1 ? true : false);
                            return;
                        }
                        */
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
    }
}