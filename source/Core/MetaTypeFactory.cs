using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core.Type;

namespace SimpleLanguage.Core
{
    public class MetaTypeFactory
    {
        //    public static MetaType CreateMetaTypeByToken( Token typeToken, Token nameToken )
        //    {
        //        EType etype = EType.Class;
        //        string typename = "";
        //        if( typeToken == null )
        //        {
        //            return new MetaType(EType.Class, "Object");
        //        }
        //        if (typeToken.type == ETokenType.Class)
        //        {
        //            etype = EType.Class;
        //            typename = nameToken.lexeme.ToString();
        //        }
        //        else if (typeToken.type == ETokenType.String)
        //        {
        //            etype = EType.String;
        //            typename = "string";
        //        }
        //        else if (typeToken.type == ETokenType.Number)
        //        {
        //            MatchSystemType(typeToken.lexeme.ToString(), ref etype, ref typename);
        //        }
        //        var type = new MetaType(etype, typename);

        //        return type;
        //    }
        public static int GetOpLevel(EType defineType)
        {
            if (defineType == EType.Boolean)
                return 0;
            else if (defineType == EType.Char)
                return 1;
            else if (defineType == EType.Int16 || defineType == EType.UInt16)
                return 2;
            else if (defineType == EType.Int32 || defineType == EType.UInt32)
                return 3;
            else if (defineType == EType.Int64 || defineType == EType.UInt64)
                return 4;
            else if (defineType == EType.Float)
                return 5;
            else if (defineType == EType.Double)
                return 6;
            else if (defineType == EType.String)
                return 7;
            else if (defineType == EType.Array)
                return 8;

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
