using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace SimpleLanguage.VM
{
    public class ObjectManager
    {
        //public static Dictionary<int, DataObject> dataObjectDict = new Dictionarny<int, DataObject>();
        public static Dictionary<int, ClassObject> classObjectDict = new Dictionary<int, ClassObject>();

        public static void AddClassObject(ClassObject cl)
        {
            if (!classObjectDict.ContainsKey(cl.GetHashCode()))
            {
                classObjectDict.Add(cl.GetHashCode(), cl);
            }
        }
        public static SValue CreateValueByDefineType(IRMetaClass mdt)
        {
            SValue svalue = new SValue();
            //if (mdt.metaClass == CoreMetaClassManager.booleanMetaClass)
            //{
            //    svalue.SetBoolValue(false);
            //}
            //else if (mdt.metaClass == CoreMetaClassManager.int16MetaClass)
            //{
            //    svalue.SetInt16Value(0);
            //}
            //else if (mdt.metaClass == CoreMetaClassManager.int32MetaClass)
            //{
            //    svalue.SetInt32Value(0);
            //}
            //else if (mdt.metaClass == CoreMetaClassManager.int64MetaClass)
            //{
            //    svalue.SetInt64Value(0);
            //}
            //else if (mdt.metaClass == CoreMetaClassManager.stringMetaClass)
            //{
            //    svalue.SetStringValue("");
            //}
            //else if (mdt.metaClass == CoreMetaClassManager.arrayMetaClass)
            //{
            //}
            //else
            //{
            //    //var sobj = new ClassObject(mdt);
            //    //svalue.SetSObject(sobj);
            //}
            return svalue;
        }        
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
                sobj = new AnyObject();
            }
            else if (name == "Core.Byte" || name == "Byte")
            {
                sobj = new ByteObject(0);
                sobj.typeId = 3;
            }
            else if (name == "Core.SByte" || name == "SByte")
            {
                sobj = new SByteObject(0);
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
                sobj = new FloatObject();
                sobj.typeId = 4;
            }
            else if (name == "Core.Float64" || name == "Float64")
            {
                sobj = new DoubleObject();
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
        public static void SetObjectByValue(SObject obj, ref SValue svalue)
        {

            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        obj.SetNull();
                    }
                    break;
                case EType.Boolean:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if( to != null )
                        {
                            to.SetValue(EType.Boolean, svalue.int8Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Boolean, svalue.int8Value);
                            return;
                        }
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value == 1);
                    }
                    break;
                case EType.Byte:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Byte, svalue.int8Value);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Byte, svalue.int8Value);
                            return;
                        }
                        ByteObject byteObj = obj as ByteObject;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EType.SByte:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.SByte, svalue.sint8Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.SByte, svalue.sint8Value);
                            return;
                        }
                        SByteObject byteObj = obj as SByteObject;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int16, svalue.int16Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int16, svalue.int16Value);
                            return;
                        }
                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int16Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt16, svalue.uint16Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt16, svalue.uint16Value);
                            return;
                        }
                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int32, svalue.int32Value);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int32, svalue.int32Value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt32, svalue.uint32Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt32, svalue.uint32Value);
                            return;
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Int64, svalue.int64Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int64, svalue.int64Value);
                            return;
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.UInt64, svalue.uint64Value);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.UInt64, svalue.uint64Value);
                            return;
                        }
                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EType.String:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.String, svalue.stringValue);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.String, svalue.stringValue);
                            return;
                        }
                        ClassObject classobj = obj as ClassObject;
                        if ( classobj != null )
                        {
                        }
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Float32:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Float32, svalue.floatValue);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Float32, svalue.floatValue);
                            return;
                        }
                        FloatObject floatObj = obj as FloatObject;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EType.Float64:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EType.Float64, svalue.doubleValue);
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Float64, svalue.doubleValue);
                            return;
                        }
                        DoubleObject doubleObj = obj as DoubleObject;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EType.Class:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject);
                            return;
                        }
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            if( svalue.sobject is ClassObject co )
                            {
                                anyObject.SetValue(EType.Class, co.value );
                            }
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if( boolObject != null )
                        {
                            boolObject.SetValue(svalue.int8Value==1 ? true : false);
                            return;
                        }
                        ClassObject classObj = obj as ClassObject;
                        if (classObj == null)
                        {
                            Debug.Write("该类型不是Class类型!!");
                            return;
                        }
                        classObj.SetValue(svalue.sobject);
                    }
                    break;
            }
        }
        public static void SetValueByValue(ref SValue target, ref SValue svalue)
        {

            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        target.SetNullValue();
                    }
                    break;
                case EType.Byte:
                    {
                        target.SetInt8Value(svalue.int8Value);
                    }
                    break;
                case EType.SByte:
                    {
                        target.SetSInt8Value(svalue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        target.SetInt16Value(svalue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        target.SetUInt16Value(svalue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        target.SetInt32Value(svalue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        target.SetUInt32Value(svalue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        target.SetInt64Value(svalue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        target.SetUInt64Value(svalue.uint64Value);
                    }
                    break;
                case EType.String:
                    {
                        target.SetStringValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        target.SetSObject(svalue.sobject);
                    }
                    break;
            }
        }
    }
}