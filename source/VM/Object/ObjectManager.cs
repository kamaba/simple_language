using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.VM
{
    public class ObjectManager
    {
        public static SValue CreateValueByDefineType(MetaType mdt)
        {
            SValue svalue = new SValue();
            if (mdt.metaClass == CoreMetaClassManager.booleanMetaClass)
            {
                svalue.SetBoolValue(false);
            }
            else if (mdt.metaClass == CoreMetaClassManager.int16MetaClass)
            {
                svalue.SetInt16Value(0);
            }
            else if (mdt.metaClass == CoreMetaClassManager.int32MetaClass)
            {
                svalue.SetInt32Value(0);
            }
            else if (mdt.metaClass == CoreMetaClassManager.int64MetaClass)
            {
                svalue.SetInt64Value(0);
            }
            else if (mdt.metaClass == CoreMetaClassManager.stringMetaClass)
            {
                svalue.SetStringValue("");
            }
            else if (mdt.metaClass == CoreMetaClassManager.arrayMetaClass)
            {
            }
            else
            {
                var sobj = new ClassObject(mdt);
                svalue.SetSObject(sobj);
            }
            return svalue;
        }
        public static SObject CreateObjectByDefineType(MetaType mdt)
        {
            SObject sobj = null;

            if (mdt.metaClass == CoreMetaClassManager.booleanMetaClass)
            {
                sobj = new BoolObject(false);
            }
            else if (mdt.metaClass == CoreMetaClassManager.int32MetaClass)
            {
                sobj = new Int32Object(0);
            }
            else if (mdt.metaClass == CoreMetaClassManager.stringMetaClass)
            {
                sobj = new StringObject("");
            }
            else if (mdt.metaClass == CoreMetaClassManager.arrayMetaClass)
            {
                sobj = new ArrayObject<int>();
            }
            else if (mdt.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                sobj = new AnyObject();
            }
            else
            {
                sobj = new ClassObject(mdt);
            }
            return sobj;
        }
        public static SObject CreateObject(MetaClass mc)
        {
            SObject sobj = null;
            EType etype = mc.eType;
            /*
            if (mc.metaType.defineType == EType.Byte)
            {
                ByteObject obj = new ByteObject();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.Boolean)
            {
                BoolObject obj = new BoolObject();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.Char)
            {
                CharObject obj = new CharObject();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.Int16)
            {
                Int16Object obj = new Int16Object();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.UInt16)
            {
                UInt16Object obj = new UInt16Object();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.Int32)
            {
                Int32Object obj = new Int32Object();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.UInt32)
            {
                UInt32Object obj = new UInt32Object();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.Int64)
            {
                UInt64Object obj = new UInt64Object();
                sobj = obj;
            }
            else if (mc.metaType.defineType == EType.String)
            {
                StringObject obj = new StringObject();
                sobj = obj;
            }
            else
            {
                ClassObject obj = new ClassObject(mc);
                sobj = obj;
            }
            */

            return sobj;
        }
        public static void SetObjectByValue(SObject obj, ref SValue svalue)
        {

            switch (svalue.eType)
            {
                case EType.Boolean:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Boolean, svalue.int8Value );
                            return;
                        }
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Console.WriteLine("该类型不是Boolean类型!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value==1);
                    }
                    break;
                case EType.Int16:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int16, svalue.int16Value);
                            return;
                        }
                        Int16Object int32Obj = obj as Int16Object;
                        if (int32Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int32, svalue.int32Value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Int64, svalue.int64Value);
                            return;
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.String:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.String, svalue.stringValue);
                            return;
                        }
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        AnyObject anyObject = obj as AnyObject;
                        if (anyObject != null)
                        {
                            anyObject.SetValue(EType.Class, svalue.int32Value);
                            return;
                        }
                        ClassObject classObj = obj as ClassObject;
                        if (classObj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
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
                case EType.Byte:
                    {
                        target.SetInt8Value(svalue.int8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        target.SetInt16Value(svalue.int16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        target.SetInt32Value(svalue.int32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        target.SetInt64Value(svalue.int64Value);
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
