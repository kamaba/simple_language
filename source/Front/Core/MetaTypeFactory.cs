//****************************************************************************
//  File:      MetaTypeFactory.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/1/15 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class MetaTypeFactory
    {
        public enum EMetaClassLevel :int 
        {
            Null,
            Boolean,
            Num,
            SByte,
            Byte,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Float8,
            Float16,
            Float32,
            Float64,
            String,
            Array,
            Class,
        }
        public static int GetOpLevelByMetaType( MetaType mt)
        {
            if (mt.metaClass == CoreMetaClassManager.nullMetaClass)
            {
                return (int)EMetaClassLevel.Null;
            }
            else if ( mt.metaClass == CoreMetaClassManager.booleanMetaClass)
            {
                return (int)EMetaClassLevel.Boolean;
            }
            else if( mt.metaClass == CoreMetaClassManager.numMetaClass )
            {
                return (int)EMetaClassLevel.Num;
            }
            else if (mt.metaClass == CoreMetaClassManager.int8MetaClass)
            {
                return (int)EMetaClassLevel.SByte;
            }
            else if (mt.metaClass == CoreMetaClassManager.uint8MetaClass)
            {
                return (int)EMetaClassLevel.Byte;
            }
            else if (mt.metaClass == CoreMetaClassManager.int16MetaClass)
            {
                return (int)EMetaClassLevel.Int16;
            }
            else if (mt.metaClass == CoreMetaClassManager.uint16MetaClass)
            {
                return (int)EMetaClassLevel.UInt16;
            }
            else if (mt.metaClass == CoreMetaClassManager.int32MetaClass)
            {
                return (int)EMetaClassLevel.Int32;
            }
            else if (mt.metaClass == CoreMetaClassManager.uint32MetaClass)
            {
                return (int)EMetaClassLevel.UInt32;
            }
            else if (mt.metaClass == CoreMetaClassManager.int64MetaClass)
            {
                return (int)EMetaClassLevel.Int64;
            }
            else if (mt.metaClass == CoreMetaClassManager.uint64MetaClass)
            {
                return (int)EMetaClassLevel.UInt64;
            }
            else if (mt.metaClass == CoreMetaClassManager.float8MetaClass
                || mt.metaClass == CoreMetaClassManager.float8_E5M2MetaClass)
            {
                return (int)EMetaClassLevel.Float8;
            }
            else if (mt.metaClass == CoreMetaClassManager.float16MetaClass
                || mt.metaClass == CoreMetaClassManager.float16_BrainMetaClass)
            {
                return (int)EMetaClassLevel.Float16;
            }
            else if (mt.metaClass == CoreMetaClassManager.float32MetaClass)
            {
                return (int)EMetaClassLevel.Float32;
            }
            else if (mt.metaClass == CoreMetaClassManager.float64MetaClass)
            {
                return (int)EMetaClassLevel.Float64;
            }
            else if (mt.metaClass == CoreMetaClassManager.arrayMetaClass)
            {
                return (int)EMetaClassLevel.Array;
            }
            else if (mt.metaClass == CoreMetaClassManager.stringMetaClass)
            {
                return (int)EMetaClassLevel.String;
            }
            return 100;
        }

        private static bool IsLowFloatEType(EType t)
        {
            return t == EType.Float8 || t == EType.Float8_E5M2
                || t == EType.Float16 || t == EType.Float16_Brain;
        }

        /// <summary>
        /// 低精度浮点（float8e4m3/e5m2、float16、float16brain）参与二元数值运算时的结果类型推导：
        /// 同类型保持不变；跨格式（e4m3 vs e5m2、float16 vs bfloat16、float8 族 vs float16 族）升为 Float32；
        /// 与小整数（byte..uint）运算保持低精度类型（镜像 Float32 规则）；
        /// 与 Int64/UInt64/Float32 运算升为 Float32；与 Float64 运算升为 Float64。
        /// </summary>
        private static EType CalcLowFloatETypeByLeftAndRight(EType etype1, EType etype2, out int error)
        {
            error = 0;
            // 抽象 Num 类型保持既有行为：结果为 Num
            if (etype1 == EType.Num || etype2 == EType.Num)
            {
                return EType.Num;
            }

            bool low1 = IsLowFloatEType(etype1);
            bool low2 = IsLowFloatEType(etype2);
            if (low1 && low2)
            {
                if (etype1 == etype2)
                {
                    return etype1;
                }
                // 跨格式混合统一升为 Float32
                return EType.Float32;
            }

            EType lowType = low1 ? etype1 : etype2;
            EType otherType = low1 ? etype2 : etype1;
            switch (otherType)
            {
                case EType.UInt8:
                case EType.Int8:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                    {
                        return lowType;
                    }
                case EType.Int64:
                case EType.UInt64:
                case EType.Float32:
                    {
                        return EType.Float32;
                    }
                case EType.Float64:
                    {
                        return EType.Float64;
                    }
                default:
                    {
                        error = 1;
                        return EType.None;
                    }
            }
        }

        public static EType CalcETypeByLeftAndRight(EType etype1, EType etype2, ELeftRightOpSign op, out int error )
        {
            error = 0;
            // 低精度浮点类型统一在此推导，避免落入下方旧 switch 后返回 EType.None 且 error=0
            if (IsLowFloatEType(etype1) || IsLowFloatEType(etype2))
            {
                return CalcLowFloatETypeByLeftAndRight(etype1, etype2, out error);
            }
            switch ( etype1 )
            {
                case EType.Num:
                    {
                        return EType.Num;
                    }
                case EType.UInt8:
                    {
                        switch( etype2 )
                        {
                            case EType.UInt8:
                                {
                                    return EType.UInt8;
                                }
                            case EType.Int8:
                                {
                                    return EType.Int16;
                                }
                            case EType.Int16:
                                {
                                    return EType.Int16;
                                }
                            case EType.UInt16:
                                {
                                    return EType.UInt16;
                                }
                            case EType.Int32:
                                {
                                    return EType.Int32;
                                }  
                            case EType.UInt32:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }       
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }   
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Int8:
                    {
                        switch( etype2 )
                        {
                            case EType.UInt8:
                                {
                                    return EType.Int16;
                                }
                            case EType.Int8:
                                {
                                    return EType.Int8;
                                }
                            case EType.Int16:
                                {
                                    return EType.Int16;
                                }
                            case EType.UInt16:
                                {
                                    return EType.Int32;
                                }
                            case EType.Int32:
                                {
                                    return EType.Int32;
                                }  
                            case EType.UInt32:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }       
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }   
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Int16:
                    {
                        switch( etype2 )
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                                {
                                    return EType.Int16;
                                }
                            case EType.UInt16:
                                {
                                    return EType.Int32;
                                }
                            case EType.Int32:
                                {
                                    return EType.Int32;
                                }  
                            case EType.UInt32:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }       
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }   
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.UInt16:
                    {
                        switch( etype2 )
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                                {
                                    return EType.Int32;    
                                }
                            case EType.UInt16:
                                {
                                    return EType.UInt16;
                                }
                            case EType.Int32:
                                {
                                    return EType.Int32;
                                }  
                            case EType.UInt32:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }       
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }   
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Int32:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                            case EType.Int32:
                                {
                                    return EType.Int32;
                                }
                            case EType.UInt32:
                                {
                                    return EType.Int64;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.UInt32:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int32:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt32:
                                {
                                    return EType.UInt32;
                                }
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Int64:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                            case EType.Int32:
                            case EType.UInt32:
                            case EType.Int64:
                                {
                                    return EType.Int64;
                                }
                            case EType.UInt64:
                                {
                                    error = 2;
                                    return EType.UInt64;
                                }
                            case EType.Float32:
                                {
                                    return EType.Float64;
                                }
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.UInt64:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                            case EType.Int32:
                            case EType.UInt32:
                                {
                                    return EType.UInt64;
                                }
                            case EType.Int64:
                                {
                                    error = 2;
                                    return EType.UInt64;
                                }
                            case EType.UInt64:
                                {
                                    return EType.UInt64;
                                }
                            case EType.Float32:
                                {
                                    return EType.Float64;
                                }
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Float32:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                            case EType.Int32:
                            case EType.UInt32:
                            case EType.Float32:
                                {
                                    return EType.Float32;
                                }
                            case EType.Int64:
                            case EType.UInt64:
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
                case EType.Float64:
                    {
                        switch (etype2)
                        {
                            case EType.UInt8:
                            case EType.Int8:
                            case EType.Int16:
                            case EType.UInt16:
                            case EType.Int32:
                            case EType.UInt32:
                            case EType.Int64:
                            case EType.UInt64:
                            case EType.Float32:
                            case EType.Float64:
                                {
                                    return EType.Float64;
                                }
                            default:
                                {
                                    error = 1;
                                }
                                break;
                        }
                    }
                    break;
            }

            return EType.None;
        }
        public static int GetOpLevel(EType defineType)
        {
            if (defineType == EType.Boolean)
                return 0;
            //else if (defineType == EType.Char)
            //    return 1;
            else if (defineType == EType.Int16 || defineType == EType.UInt16)
                return 2;
            else if (defineType == EType.Int32 || defineType == EType.UInt32)
                return 3;
            else if (defineType == EType.Int64 || defineType == EType.UInt64)
                return 4;
            else if (defineType == EType.Float32)
                return 5;
            else if (defineType == EType.Float64)
                return 6;
            else if (defineType == EType.Array)
                return 8;
            else if (defineType == EType.String)
                return 11;

            return 10;
        }
        public static string DefineTypeToClassName(string tokenname)
        {
            string className = tokenname;
            switch (tokenname)
            {
                case "short":
                    {
                        //etype = etype.int16;
                        className = "Int16";
                    }
                    break;
                case "ushort":
                    {
                        //etype = etype.uint16;
                        className = "UInt16";
                    }
                    break;
                case "int":
                    {
                        //etype = etype.int32;
                        className = "Int32";
                    }
                    break;
                case "uint":
                    {
                        //etype = etype.uint32;
                        className = "UInt32";
                    }
                    break;
                case "long":
                    {
                        //etype = etype.int64;
                        className = "Int64";
                    }
                    break;
                case "ulong":
                    {
                        //etype = etype.uint64;
                        className = "UInt64";
                    }
                    break;
                case "float":
                    {
                        //etype = etype.float;
                        className = "Float";
                    }
                    break;
                case "double":
                    {
                        //etype = etype.double;
                        className = "Double";
                    }
                    break;
                case "char":
                    {
                        //etype = etype.char;
                        className = "Char";
                    }
                    break;
                case "byte":
                    {
                        //etype = EType.UInt8;
                        className = "UInt8";
                    }
                    break;
                case "sbyte":
                    {
                        //etype = EType.Int8;
                        className = "Int8";
                    }
                    break;
                case "string":
                    {
                        //etype = etype.string;
                        className = "String";
                    }
                    break;
            }
            return className;
        }
    }
}
