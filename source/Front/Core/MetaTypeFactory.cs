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
            else if (mt.metaClass == CoreMetaClassManager.sbyteMetaClass)
            {
                return (int)EMetaClassLevel.SByte;
            }
            else if (mt.metaClass == CoreMetaClassManager.byteMetaClass)
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
        public static EType CalcETypeByLeftAndRight(EType etype1, EType etype2, ELeftRightOpSign op, out int error )
        {
            error = 0;
            switch ( etype1 )
            {
                case EType.Num:
                    {
                        return EType.Num;
                    }
                case EType.Byte:
                    {
                        switch( etype2 )
                        {
                            case EType.Byte:
                                {
                                    return EType.Byte;
                                }
                            case EType.SByte:
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
                case EType.SByte:
                    {
                        switch( etype2 )
                        {
                            case EType.Byte:
                                {
                                    return EType.Int16;
                                }
                            case EType.SByte:
                                {
                                    return EType.SByte;
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                            case EType.Byte:
                            case EType.SByte:
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
                        //etype = etype.byte;
                        className = "Byte";
                    }
                    break;
                case "sbyte":
                    {
                        //etype = etype.sbyte;
                        className = "SByte";
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
