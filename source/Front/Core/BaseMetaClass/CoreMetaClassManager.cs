//****************************************************************************
//  File:      CoreMetaClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Project;
using System.Collections.Generic;
using System.Diagnostics;


namespace SimpleLanguage.Core
{
    public enum DefaultObject
    {
        Void,
        Null,
        Object,
        Boolean,
        Num,
        UInt8,
        Int8,
        Char,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float32,
        Float64,
        String,
        Array,
        Range,
        Template,
        Class,
        Type,
        Dynamic,
        Data,
        Enum,
        Member,
        Error,
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
        public static MetaClass nullMetaClass { get; private set; } = null;
        public static MetaClass objectMetaClass { get; private set; } = null;
        public static MetaClass dataMetaClass { get; private set; } = null;
        public static MetaClass memberMetaClass { get; private set; } = null;
        public static MetaClass numMetaClass { get; private set; } = null;
        public static MetaClass stringMetaClass { get; private set; } = null;
        public static MetaClass voidMetaClass { get; set; } = null;
        public static MetaClass typeMetaClass { get; set; } = null;
        public static MetaClass booleanMetaClass { get; private set; } = null;
        //public static MetaClass charMetaClass { get; private set; } = null;
        public static MetaClass uint8MetaClass { get; private set; } = null;
        public static MetaClass int8MetaClass { get; private set; } = null;
        public static MetaClass int16MetaClass { get; private set; } = null;
        public static MetaClass uint16MetaClass { get; private set; } = null;
        public static MetaClass int32MetaClass { get; private set; } = null;
        public static MetaClass uint32MetaClass { get; private set; } = null;
        public static MetaClass int64MetaClass { get; private set; } = null;
        public static MetaClass uint64MetaClass { get; private set; } = null;
        public static MetaClass float32MetaClass { get; private set; } = null;
        public static MetaClass float64MetaClass { get; private set; } = null;
        public static MetaClass arrayMetaClass { get; private set; } = null;
        public static MetaClass listMetaClass { get; private set; } = null;
        public static MetaClass tulpeMetaClass { get; private set; } = null;
        public static MetaClass mapMetaClass { get; private set; } = null;
        public static MetaClass rangeMetaClass { get; private set; } = null;
        //public static MetaClass dynamicMetaClass { get; private set; } = null;
        public static MetaClass dynamicMetaData { get; private set; } = null;
        public static MetaClass enumMetaData { get; private set; } = null;
        public static MetaClass iteratorMetaClass { get; set; } = null;
        public static MetaClass iterableMetaClass { get; set; } = null;
        public static MetaClass errorMetaClass { get; private set; } = null;

        public static List<MetaClass> s_InnerDefineMetaClassList = new List<MetaClass>();

        static CoreMetaClassManager()
        {
            nullMetaClass = NullMetaClass.CreateMetaClass();
            objectMetaClass = ObjectMetaClass.CreateMetaClass();
            dataMetaClass = DataMetaClass.CreateMetaClass();
            numMetaClass = NumMetaClass.CreateMetaClass();
            voidMetaClass = VoidMetaClass.CreateMetaClass();
            booleanMetaClass = BooleanMetaClass.CreateMetaClass();
            uint8MetaClass = UInt8MetaClass.CreateMetaClass();
            int8MetaClass = Int8MetaClass.CreateMetaClass();
            //charMetaClass = CharMetaClass.CreateMetaClass();
            int16MetaClass = Int16MetaClass.CreateMetaClass();
            uint16MetaClass = UInt16MetaClass.CreateMetaClass();
            int32MetaClass = Int32MetaClass.CreateMetaClass();
            uint32MetaClass = UInt32MetaClass.CreateMetaClass();
            int64MetaClass = Int64MetaClass.CreateMetaClass();
            uint64MetaClass = UInt64MetaClass.CreateMetaClass();
            float32MetaClass = Float32MetaClass.CreateMetaClass();
            float64MetaClass = Float64MetaClass.CreateMetaClass();
            stringMetaClass = StringMetaClass.CreateMetaClass();
            iteratorMetaClass = IteratorMetaClass.CreateMetaClass();
            iterableMetaClass = IterableMetaClass.CreateMetaClass();
            arrayMetaClass = ArrayMetaClass.CreateMetaClass();
            rangeMetaClass = RangeMetaClass.CreateMetaClass();
            dynamicMetaData = DynamicMetaData.CreateMetaClass();
            enumMetaData = EnumMetaClass.CreateMetaClass();
            memberMetaClass = MemberMetaClass.CreateMetaClass();
            typeMetaClass = TypeMetaClass.CreateMetaClass();
            errorMetaClass = ErrorMetaClass.CreateMetaClass();

            s_InnerDefineMetaClassList.Add(objectMetaClass);
            s_InnerDefineMetaClassList.Add(voidMetaClass);
            s_InnerDefineMetaClassList.Add(dataMetaClass);
            s_InnerDefineMetaClassList.Add(numMetaClass);
            s_InnerDefineMetaClassList.Add(booleanMetaClass);
            s_InnerDefineMetaClassList.Add(uint8MetaClass);
            s_InnerDefineMetaClassList.Add(int8MetaClass);
            //s_InnerDefineMetaClassList.Add(charMetaClass);
            s_InnerDefineMetaClassList.Add(int16MetaClass);
            s_InnerDefineMetaClassList.Add(uint16MetaClass);
            s_InnerDefineMetaClassList.Add(int32MetaClass);
            s_InnerDefineMetaClassList.Add(uint32MetaClass);
            s_InnerDefineMetaClassList.Add(int64MetaClass);
            s_InnerDefineMetaClassList.Add(uint64MetaClass);
            s_InnerDefineMetaClassList.Add(float32MetaClass);
            s_InnerDefineMetaClassList.Add(float64MetaClass);
            s_InnerDefineMetaClassList.Add(stringMetaClass);
            s_InnerDefineMetaClassList.Add(iteratorMetaClass);
            s_InnerDefineMetaClassList.Add(iterableMetaClass);
            s_InnerDefineMetaClassList.Add(arrayMetaClass);
            s_InnerDefineMetaClassList.Add(rangeMetaClass);
            s_InnerDefineMetaClassList.Add(dynamicMetaData);
            s_InnerDefineMetaClassList.Add(enumMetaData);
            s_InnerDefineMetaClassList.Add(typeMetaClass);
            s_InnerDefineMetaClassList.Add(errorMetaClass);
            s_InnerDefineMetaClassList.Add(memberMetaClass);
        }
        public void Init()
        {
            foreach( var v in s_InnerDefineMetaClassList )
            {
                v.ParseInner();
                ClassManager.instance.AddMetaClass(v, ModuleManager.instance.coreModule);
                v.UpdateClassAllName();
            }
        }
        public static bool IsIncludeMetaClass( MetaClass metaclass )
        {
            if( s_InnerDefineMetaClassList.Contains(metaclass) )
            {
                return true;
            }
            return false;
        }

        public static EType GetETypeByMetaClass(MetaClass mc)
        {
            if (mc == voidMetaClass)
            {
                return EType.Void;
            }
            else if (mc == nullMetaClass )
            {
                return EType.Null;
            }
            else if (mc == uint8MetaClass)
            {
                return EType.UInt8;
            }
            else if (mc == numMetaClass)
            {
                return EType.Num;
            }
            else if (mc == int8MetaClass)
            {
                return EType.Int8;
            }
            else if (mc == int16MetaClass)
            {
                return EType.Int16;
            }
            else if (mc == uint16MetaClass)
            {
                return EType.UInt16;
            }
            else if (mc == int32MetaClass)
            {
                return EType.Int32;
            }
            else if (mc == uint32MetaClass)
            {
                return EType.UInt32;
            }
            else if (mc == int64MetaClass)
            {
                return EType.Int64;
            }
            else if (mc == uint64MetaClass)
            {
                return EType.UInt64;
            }
            else if (mc == float32MetaClass)
            {
                return EType.Float32;
            }
            else if (mc == float64MetaClass)
            {
                return EType.Float64;
            }
            else if (mc == stringMetaClass)
            {
                return EType.String;
            }
            else if (mc == objectMetaClass )
            {
                return EType.Object;
            }
            else if (mc == typeMetaClass )
            {
                return EType.Type;
            }
            else if( mc == booleanMetaClass )
            {
                return EType.Boolean;
            }
            else if (mc == arrayMetaClass )
            {
                return EType.Array;
            }
            else if (mc == rangeMetaClass )
            {
                return EType.Range;
            }
            else
            {
                return EType.Class;
            }
        }
        public static MetaClass GetMetaClassByEType(EType etype)
        {
            switch (etype)
            {
                case EType.Void:
                    return voidMetaClass;
                case EType.Null:
                    return nullMetaClass;
                case EType.Boolean:
                    return booleanMetaClass;
                case EType.Num:
                    return numMetaClass;
                case EType.UInt8:
                    return uint8MetaClass;
                case EType.Int8:
                    return int8MetaClass;
                //case EType.Char:
                //    return charMetaClass;
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
                case EType.Float32:
                    return float32MetaClass;
                case EType.Float64:
                    return float64MetaClass;
                case EType.String:
                    return stringMetaClass;
                case EType.Array:
                    return arrayMetaClass;
                case EType.Range:
                    return rangeMetaClass;
                default:
                    {
                        Debug.WriteLine("Warning ClassManager GetMetaClassByEType 1111");
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
                case "null":
                    return DefaultObject.Null.ToString();
                case "num":
                    return DefaultObject.Num.ToString();
                case "object":
                case "Object":
                    return DefaultObject.Object.ToString();
                case "bool":
                    return DefaultObject.Boolean.ToString();
                case "byte":
                case "UInt8":
                    return DefaultObject.UInt8.ToString();
                case "sbyte":
                case "Int8":
                    return DefaultObject.Int8.ToString();
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
                case "Byte":
                    return DefaultObject.UInt8.ToString();
                case "SByte":
                    return DefaultObject.Int8.ToString();
                case "half":
                    return null;
                case "float":
                case "Float32":
                    return DefaultObject.Float32.ToString();
                case "double":
                case "Float64":
                case "Core.Float64":
                    return DefaultObject.Float64.ToString();
                case "range":
                case "Range":
                    return DefaultObject.Range.ToString();
                case "dynamic":
                    return DefaultObject.Dynamic.ToString();
                case "data":
                    return DefaultObject.Data.ToString();
                case "array":
                    return DefaultObject.Array.ToString();
                default:return name;
            }
        }
        public static MetaNode GetCoreMetaClass( string name )
        {
            string name1 = GetSelfMetaName(name);
            return ModuleManager.instance.coreModule.metaNode.GetChildrenMetaNodeByName(name1);
        }
    }
}
