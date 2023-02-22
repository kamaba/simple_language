//****************************************************************************
//  File:      CoreMetaClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public enum DefaultObject
    {
        Void,
        Object,
        Boolean,
        Byte,
        SByte,
        Char,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        String,
        Array,
        Range,
        Template,
        Class,
    }
    class CoreMetaClassManager
    {
        public static CoreMetaClassManager s_Instance = null;
        public static CoreMetaClassManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CoreMetaClassManager();
                }
                return s_Instance;
            }
        }
        public static MetaClass objectMetaClass { get; private set; } = null;
        public static MetaClass stringMetaClass { get; private set; } = null;
        public static MetaClass voidMetaClass { get; set; } = null;
        public static MetaClass booleanMetaClass { get; private set; } = null;
        public static MetaClass charMetaClass { get; private set; } = null;
        public static MetaClass byteMetaClass { get; private set; } = null;
        public static MetaClass sbyteMetaClass { get; private set; } = null;
        public static MetaClass int16MetaClass { get; private set; } = null;
        public static MetaClass uint16MetaClass { get; private set; } = null;
        public static MetaClass int32MetaClass { get; private set; } = null;
        public static MetaClass uint32MetaClass { get; private set; } = null;
        public static MetaClass int64MetaClass { get; private set; } = null;
        public static MetaClass uint64MetaClass { get; private set; } = null;
        public static MetaClass floatMetaClass { get; private set; } = null;
        public static MetaClass doubleMetaClass { get; private set; } = null;
        public static MetaClass arrayMetaClass { get; set; } = null;
        public static MetaClass rangeMetaClass { get; set; } = null;
        public static MetaClass arrayIteratorMetaClass { get; set; } = null;

        public static List<MetaClass> s_InnerDefineMetaClassList = new List<MetaClass>();

        static CoreMetaClassManager()
        {
            objectMetaClass = ObjectMetaClass.CreateMetaClass();
            voidMetaClass = VoidMetaClass.CreateMetaClass();
            booleanMetaClass = BooleanMetaClass.CreateMetaClass();
            byteMetaClass = ByteMetaClass.CreateMetaClass();
            sbyteMetaClass = SByteMetaClass.CreateMetaClass();
            charMetaClass = CharMetaClass.CreateMetaClass();
            int16MetaClass = Int16MetaClass.CreateMetaClass();
            uint16MetaClass = UInt16MetaClass.CreateMetaClass();
            int32MetaClass = Int32MetaClass.CreateMetaClass();
            uint32MetaClass = UInt32MetaClass.CreateMetaClass();
            int64MetaClass = Int64MetaClass.CreateMetaClass();
            uint64MetaClass = UInt64MetaClass.CreateMetaClass();
            floatMetaClass = FloatMetaClass.CreateMetaClass();
            doubleMetaClass = DoubleMetaClass.CreateMetaClass();
            stringMetaClass = StringMetaClass.CreateMetaClass();
            arrayIteratorMetaClass = ArrayIteratorMetaClass.CreateMetaClass();
            arrayMetaClass = ArrayMetaClass.CreateMetaClass();
            rangeMetaClass = RangeMetaClass.CreateMetaClass();
            s_InnerDefineMetaClassList.Add(objectMetaClass);
            s_InnerDefineMetaClassList.Add(voidMetaClass);
            s_InnerDefineMetaClassList.Add(booleanMetaClass);
            s_InnerDefineMetaClassList.Add(byteMetaClass);
            s_InnerDefineMetaClassList.Add(sbyteMetaClass);
            s_InnerDefineMetaClassList.Add(charMetaClass);
            s_InnerDefineMetaClassList.Add(int16MetaClass);
            s_InnerDefineMetaClassList.Add(uint16MetaClass);
            s_InnerDefineMetaClassList.Add(int32MetaClass);
            s_InnerDefineMetaClassList.Add(uint32MetaClass);
            s_InnerDefineMetaClassList.Add(int64MetaClass);
            s_InnerDefineMetaClassList.Add(uint64MetaClass);
            s_InnerDefineMetaClassList.Add(floatMetaClass);
            s_InnerDefineMetaClassList.Add(doubleMetaClass);
            s_InnerDefineMetaClassList.Add(stringMetaClass);
            s_InnerDefineMetaClassList.Add(arrayIteratorMetaClass);
            s_InnerDefineMetaClassList.Add(arrayMetaClass);
            s_InnerDefineMetaClassList.Add(rangeMetaClass);
        }
        public void Init()
        {
            foreach( var v in s_InnerDefineMetaClassList )
            {
                v.ParseInner();
            }
        }
        public static MetaClass GetMetaClassByEType(EType etype)
        {
            switch (etype)
            {
                case EType.Void:
                    return voidMetaClass;
                case EType.Boolean:
                    return booleanMetaClass;
                case EType.Byte:
                    return byteMetaClass;
                case EType.SByte:
                    return sbyteMetaClass;
                case EType.Char:
                    return charMetaClass;
                case EType.Int16:
                    return int16MetaClass;
                case EType.UInt16:
                    return uint16MetaClass;
                case EType.Int32:
                    return int32MetaClass;
                case EType.UInt32:
                    return uint32MetaClass;
                case EType.Int64:
                    return int64MetaClass;
                case EType.UInt64:
                    return uint64MetaClass;
                case EType.Float:
                    return floatMetaClass;
                case EType.Double:
                    return doubleMetaClass;
                case EType.String:
                    return stringMetaClass;
                case EType.Array:
                    return arrayMetaClass;
                case EType.Range:
                    return rangeMetaClass;
                default:
                    {
                        Console.WriteLine("Warning ClassManager GetMetaClassByEType 1111");
                    }
                    break;
            }
            return null;
        }
        public static string GetSelfMetaName( string name )
        {
            switch( name )
            {
                case "void":
                    return DefaultObject.Void.ToString();
                case "object":
                    return DefaultObject.Object.ToString();
                case "bool":
                    return DefaultObject.Boolean.ToString();
                case "byte":
                    return DefaultObject.Byte.ToString();
                case "sbyte":
                    return DefaultObject.Byte.ToString();
                case "long":
                case "Int64":
                    return DefaultObject.Int64.ToString();
                case "ulong":
                case "UInt64":
                    return DefaultObject.UInt64.ToString();
                case "int":
                case "Int32":
                    return DefaultObject.Int32.ToString();
                case "uint":
                case "UInt32":
                    return DefaultObject.UInt32.ToString();
                case "short":
                case "Int16":
                    return DefaultObject.Int16.ToString();
                case "ushort":
                case "UInt16":
                    return DefaultObject.UInt16.ToString();
                case "char":
                case "Char":
                    return DefaultObject.Char.ToString();
                case "string":
                case "String":
                    return DefaultObject.String.ToString();
                case "float":
                case "Float":
                    return DefaultObject.Float.ToString();
                case "double":
                case "Double":
                    return DefaultObject.Double.ToString();
                case "range":
                case "Range":
                    return DefaultObject.Range.ToString();
                default:return name;
            }
        }
        public static MetaClass GetSelfMetaClass( string name )
        {
            string name1 = GetSelfMetaName(name);
            return ModuleManager.instance.coreModule.GetChildrenMetaBaseByName(name1) as MetaClass;
        }
    }
}
