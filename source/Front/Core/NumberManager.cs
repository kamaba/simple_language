//****************************************************************************
//  File:      NumberManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/4/21
//  Description: 编译期数值类型统一管理：字面量升阶、常量隐式/强制转换、范围校验与日志。
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 数值字面量/常量转换失败时抛出，供工具或测试捕获；常规编译路径以返回值 + 日志为主。
    /// </summary>
    public sealed class NumericConversionException : Exception
    {
        public EType SourceType { get; }
        public EType TargetType { get; }

        public NumericConversionException(EType source, EType target, string message)
            : base(message)
        {
            SourceType = source;
            TargetType = target;
        }
    }

    /// <summary>
    /// 数字类型的升阶、降阶与常量强制转换的单一入口。
    /// </summary>
    public static class NumberManager
    {
        //public static EClassRelation ValidateClassRelation( string curName, string compareName )
        //{
        //    MetaClass currentClass = instance.GetClassByName(curName);
        //    if (currentClass == null)
        //    {
        //        return EClassRelation.CurClassError;
        //    }
        //    MetaClass compareClass = instance.GetClassByName(compareName);
        //    if (compareClass == null)
        //    {
        //        return EClassRelation.CompareClassError;
        //    }
        //    return ValidateClassRelationByMetaClass(currentClass, compareClass);
        //}
        public static bool IsNumberClass(MetaClass curClass)
        {
            if (curClass == null)
            {
                return false;
            }

            if (curClass == CoreMetaClassManager.numMetaClass
                || curClass == CoreMetaClassManager.uint8MetaClass
                || curClass == CoreMetaClassManager.int8MetaClass
                || curClass == CoreMetaClassManager.int16MetaClass
                || curClass == CoreMetaClassManager.uint16MetaClass
                || curClass == CoreMetaClassManager.int32MetaClass
                || curClass == CoreMetaClassManager.uint32MetaClass
                || curClass == CoreMetaClassManager.int64MetaClass
                || curClass == CoreMetaClassManager.uint64MetaClass
                || curClass == CoreMetaClassManager.float32MetaClass
                || curClass == CoreMetaClassManager.float64MetaClass)
            {
                return true;
            }

            // ?????????????????????????????????????????????????????Num????????????????????????????
            if (curClass.IsParseMetaClass(CoreMetaClassManager.numMetaClass))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 数组字面量等元素均为数值时的统一升阶顺序（阶越大类型越高）：
        /// byte → sbyte → int16 → uint16 → int32 → uint32 → float32 → int64 → uint64 → float64。
        /// 抽象 Num 或其它未单独列出的 Num 子类为最低阶 -1。
        /// </summary>
        public static bool TryGetLiteralPromotionRank(MetaClass mc, out int rank)
        {
            rank = -1;
            if (mc == null)
            {
                return false;
            }
            if (mc == CoreMetaClassManager.uint8MetaClass) { rank = 0; return true; }
            if (mc == CoreMetaClassManager.int8MetaClass) { rank = 1; return true; }
            if (mc == CoreMetaClassManager.int16MetaClass) { rank = 2; return true; }
            if (mc == CoreMetaClassManager.uint16MetaClass) { rank = 3; return true; }
            if (mc == CoreMetaClassManager.int32MetaClass) { rank = 4; return true; }
            if (mc == CoreMetaClassManager.uint32MetaClass) { rank = 5; return true; }
            if (mc == CoreMetaClassManager.float32MetaClass) { rank = 6; return true; }
            if (mc == CoreMetaClassManager.int64MetaClass) { rank = 7; return true; }
            if (mc == CoreMetaClassManager.uint64MetaClass) { rank = 8; return true; }
            if (mc == CoreMetaClassManager.float64MetaClass) { rank = 9; return true; }
            if (IsNumberClass(mc))
            {
                rank = -1;
                return true;
            }
            return false;
        }

        public static MetaClass GetMetaClassForLiteralPromotionRank(int rank)
        {
            return rank switch
            {
                -1 => CoreMetaClassManager.numMetaClass,
                0 => CoreMetaClassManager.uint8MetaClass,
                1 => CoreMetaClassManager.int8MetaClass,
                2 => CoreMetaClassManager.int16MetaClass,
                3 => CoreMetaClassManager.uint16MetaClass,
                4 => CoreMetaClassManager.int32MetaClass,
                5 => CoreMetaClassManager.uint32MetaClass,
                6 => CoreMetaClassManager.float32MetaClass,
                7 => CoreMetaClassManager.int64MetaClass,
                8 => CoreMetaClassManager.uint64MetaClass,
                9 => CoreMetaClassManager.float64MetaClass,
                _ => null,
            };
        }

        public static bool IsNumericEType(EType t)
        {
            return t == EType.UInt8
                || t == EType.Int8
                || t == EType.Int16
                || t == EType.UInt16
                || t == EType.Int32
                || t == EType.UInt32
                || t == EType.Int64
                || t == EType.UInt64
                || t == EType.Float16
                || t == EType.Float32
                || t == EType.Float64
                || t == EType.Num;
        }

        public static bool TryConvertConstValueByEType(EType targetType, object input, out object converted)
        {
            converted = null;
            try
            {
                switch (targetType)
                {
                    case EType.Boolean:
                        converted = Convert.ToBoolean(input);
                        return true;
                    case EType.UInt8:
                        converted = Convert.ToByte(input);
                        return true;
                    case EType.Int8:
                        converted = Convert.ToSByte(input);
                        return true;
                    case EType.Int16:
                        converted = Convert.ToInt16(input);
                        return true;
                    case EType.UInt16:
                        converted = Convert.ToUInt16(input);
                        return true;
                    case EType.Int32:
                        converted = Convert.ToInt32(input);
                        return true;
                    case EType.UInt32:
                        converted = Convert.ToUInt32(input);
                        return true;
                    case EType.Int64:
                        converted = Convert.ToInt64(input);
                        return true;
                    case EType.UInt64:
                        converted = Convert.ToUInt64(input);
                        return true;
                    case EType.Float16:
                        converted = (Half)Convert.ToSingle(input);
                        return true;
                    case EType.Float32:
                        converted = Convert.ToSingle(input);
                        return true;
                    case EType.Float64:
                    case EType.Num:
                        converted = Convert.ToDouble(input);
                        return true;
                    case EType.String:
                        converted = Convert.ToString(input) ?? string.Empty;
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        /// <summary>
        /// 与 <see cref="MetaVariable.TryAdjustConstExpressByDefineMetaType"/> 相同的隐式数值规则（含进制字面量）。
        /// </summary>
        public static bool TryAdjustConstExpressToNumericTarget(MetaConstExpressNode mcen, EType defineEType, EType expressEType, Token token)
        {
            if (mcen == null)
            {
                return false;
            }

            if (defineEType == EType.Object)
            {
                return true;
            }

            if (expressEType == EType.Null)
            {
                return true;
            }

            if (!IsNumericEType(defineEType) || !IsNumericEType(expressEType))
            {
                return false;
            }

            if (defineEType == expressEType)
            {
                return true;
            }

            if (defineEType == EType.Num)
            {
                return true;
            }

            bool canConvert = expressEType == EType.Num;
            if (!canConvert)
            {
                switch (defineEType)
                {
                    case EType.Int8:
                    case EType.UInt8:
                        canConvert = expressEType == EType.UInt8 || expressEType == EType.Int8;
                        break;
                    case EType.Int16:
                    case EType.UInt16:
                        canConvert = expressEType == EType.UInt8 || expressEType == EType.Int8
                            || expressEType == EType.UInt16 || expressEType == EType.Int16;
                        break;
                    case EType.Int32:
                    case EType.UInt32:
                    case EType.Float32:
                        canConvert = expressEType == EType.UInt8 || expressEType == EType.Int8
                            || expressEType == EType.UInt16 || expressEType == EType.Int16
                            || expressEType == EType.Int32 || expressEType == EType.UInt32;
                        break;
                    case EType.Int64:
                    case EType.UInt64:
                    case EType.Float64:
                        canConvert = true;
                        break;
                    case EType.Num:
                        canConvert = true;
                        break;
                }
            }

            if (canConvert && TryConvertConstValueByEType(defineEType, mcen.value, out var convertedValue))
            {
                mcen.SetConstValue(defineEType, convertedValue);
                return true;
            }

            if (canConvert && IsRadixNumberLiteral(mcen)
                && TryConvertRadixUnsignedToSignedByEType(defineEType, mcen.value, out var radixConvertedValue))
            {
                mcen.SetConstValue(defineEType, radixConvertedValue);
                return true;
            }

            Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, token, (mcen.value?.ToString() ?? "null"), defineEType.ToString(), expressEType.ToString());
            return false;
        }

        /// <summary>
        /// 数组等元素向已声明的数值元素类型强制对齐：先走隐式规则，失败时再尝试带范围检查的升阶/降阶。
        /// </summary>
        public static bool TryForceAdjustConstExpressByMetaType(MetaConstExpressNode mcen, MetaType defineMetaType, Token errorAnchor)
        {
            if (mcen == null || defineMetaType == null)
            {
                return false;
            }

            var targetEt = CoreMetaClassManager.GetETypeByMetaClass(defineMetaType.metaClass);
            if (targetEt == EType.Object)
            {
                targetEt = mcen.eType;
            }

            var expressEt = mcen.eType;
            if (expressEt == EType.Null)
            {
                return true;
            }

            if (!IsNumericEType(targetEt) || !IsNumericEType(expressEt))
            {
                return false;
            }

            if (TryAdjustConstExpressToNumericTarget(mcen, targetEt, expressEt, errorAnchor ?? mcen.token))
            {
                return true;
            }

            if (TryForceConvertConstValueWithRangeCheck(targetEt, mcen.value, out var forced))
            {
                mcen.SetConstValue(targetEt, forced);
                return true;
            }

            Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, errorAnchor ?? mcen.token,
                (mcen.value?.ToString() ?? "null"), targetEt.ToString(), expressEt.ToString());
            return false;
        }

        /// <summary>
        /// 在隐式规则不允许时，仍尝试数值互转；整数降阶要求值落在目标类型范围内，浮点降阶要求无精度损失（双转单）。
        /// </summary>
        public static bool TryForceConvertConstValueWithRangeCheck(EType targetType, object input, out object converted)
        {
            converted = null;
            if (input == null)
            {
                return false;
            }

            if (!IsNumericEType(targetType) || targetType == EType.Num)
            {
                return TryConvertConstValueByEType(targetType, input, out converted);
            }

            try
            {
                switch (targetType)
                {
                    case EType.UInt8:
                        {
                            var v = ToBigInteger(input);
                            if (v < byte.MinValue || v > byte.MaxValue) return false;
                            converted = (byte)v;
                            return true;
                        }
                    case EType.Int8:
                        {
                            var v = ToBigInteger(input);
                            if (v < sbyte.MinValue || v > sbyte.MaxValue) return false;
                            converted = (sbyte)v;
                            return true;
                        }
                    case EType.Int16:
                        {
                            var v = ToBigInteger(input);
                            if (v < short.MinValue || v > short.MaxValue) return false;
                            converted = (short)v;
                            return true;
                        }
                    case EType.UInt16:
                        {
                            var v = ToBigInteger(input);
                            if (v < ushort.MinValue || v > ushort.MaxValue) return false;
                            converted = (ushort)v;
                            return true;
                        }
                    case EType.Int32:
                        {
                            var v = ToBigInteger(input);
                            if (v < int.MinValue || v > int.MaxValue) return false;
                            converted = (int)v;
                            return true;
                        }
                    case EType.UInt32:
                        {
                            var v = ToBigInteger(input);
                            if (v < uint.MinValue || v > uint.MaxValue) return false;
                            converted = (uint)v;
                            return true;
                        }
                    case EType.Int64:
                        {
                            var v = ToBigInteger(input);
                            if (v < long.MinValue || v > long.MaxValue) return false;
                            converted = (long)v;
                            return true;
                        }
                    case EType.UInt64:
                        {
                            var v = ToBigInteger(input);
                            if (v < 0 || v > (BigInteger)ulong.MaxValue) return false;
                            converted = (ulong)v;
                            return true;
                        }
                    case EType.Float32:
                        {
                            double d = Convert.ToDouble(input, CultureInfo.InvariantCulture);
                            float f = (float)d;
                            if (float.IsInfinity(f) || float.IsNaN(f))
                            {
                                return false;
                            }
                            if (Math.Abs(d) <= (double)float.MaxValue && (double)f != d)
                            {
                                return false;
                            }
                            converted = f;
                            return true;
                        }
                    case EType.Float64:
                        converted = Convert.ToDouble(input, CultureInfo.InvariantCulture);
                        return true;
                    default:
                        return TryConvertConstValueByEType(targetType, input, out converted);
                }
            }
            catch
            {
                converted = null;
                return false;
            }
        }
        /// <summary>
        /// 当数组类型已确定为 <c>Array&lt;T&gt;</c> 且 <c>T</c> 为具体数值类型时，将 <paramref name="newObjectNode"/> 中字面量常量统一转为 <c>T</c>；
        /// 混合整型会强转对齐；整数目标与非整值浮点字面量、或无法转换的数值组合会记录日志并返回 false。
        /// 嵌套的数组字面量（元素类型仍为数组）会按声明的内层数组类型递归处理。
        /// </summary>
        public static bool TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(
            MetaNewObjectExpressNode newObjectNode,
            MetaType declaredArrayMetaType,
            Token anchor)
        {
            if (newObjectNode?.assignStatementsList == null || declaredArrayMetaType == null || !declaredArrayMetaType.IsArray())
            {
                return true;
            }

            var list = newObjectNode.assignStatementsList;
            var elemType = TypeManager.GetSingleTemplateArgMetaType(declaredArrayMetaType);
            if (elemType?.metaClass == null)
            {
                return true;
            }

            if (elemType.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            if (!NumberManager.IsNumberClass(elemType.metaClass))
            {
                return true;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var mas = list[i];
                if (mas?.expressNode == null)
                {
                    continue;
                }

                var expr = mas.expressNode;
                if (expr is MetaConstExpressNode c)
                {
                    var targetEt = CoreMetaClassManager.GetETypeByMetaClass(elemType.metaClass);
                    var srcEt = c.eType;
                    if (IsIntegralNumericEType(targetEt) && IsFloatingNumericEType(srcEt) && !IsConstNumericWholeNumber(c))
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, anchor ?? c.token,
                            "数组元素[" + i.ToString() + "]：声明为整数元素类型 " + elemType.ToString()
                            + "，不能从非整值的浮点字面量 " + srcEt.ToString() + "（值 " + (c.value?.ToString() ?? "null") + "）转换。");
                        return false;
                    }

                    if (!TryForceAdjustConstExpressByMetaType(c, elemType, anchor ?? c.token))
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, anchor ?? c.token,
                            "数组元素[" + i.ToString() + "]：无法将常量转为声明的元素类型 " + elemType.ToString()
                            + "（当前表达式类型 " + srcEt.ToString() + "，值 " + (c.value?.ToString() ?? "null") + "）。");
                        return false;
                    }
                    c.CalcReturnType();
                }
                else if (expr is MetaNewObjectExpressNode nested
                    && nested.newType == MetaNewObjectExpressNode.ENewType.ArrayClass
                    && nested.assignStatementsList != null
                    && elemType.IsArray())
                {
                    nested.CalcReturnType();
                }

                mas.CalcReturnType();
            }

            return true;
        }

        /// <summary>
        /// 以 <paramref name="defineArray"/> 的模板（含元素类型，如 Array&lt;Int32&gt;）为准生成新 <see cref="MetaType"/>，
        /// 数组长度优先取 <paramref name="newArray"/>，其次 <paramref name="realArray"/>。
        /// 用于左值 Array&lt;Int32&gt; 与右值 Array&lt;Int16&gt; 等数值元素不一致时，结果类型跟随左值元素类型。
        /// </summary>
        public static MetaType BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
            MetaType defineArray,
            MetaType newArray )
        {
            if (defineArray == null)
            {
                return null;
            }

            var r = new MetaType(defineArray);
            int len = -1;
            if (len < 0 && defineArray != null && defineArray.arrayLength >= 0)
            {
                len = defineArray.arrayLength;
            }
            if (newArray != null && newArray.arrayLength >= 0)
            {
                len = newArray.arrayLength;
            }

            if (len >= 0)
            {
                r.SetArrayLength(len);
            }

            return r;
        }

        private static bool IsIntegralNumericEType(EType t)
        {
            return t == EType.UInt8 || t == EType.Int8
                || t == EType.UInt16 || t == EType.Int16
                || t == EType.UInt32 || t == EType.Int32
                || t == EType.UInt64 || t == EType.Int64;
        }

        private static bool IsFloatingNumericEType(EType t)
        {
            return t == EType.Float16 || t == EType.Float32 || t == EType.Float64;
        }

        private static bool IsConstNumericWholeNumber(MetaConstExpressNode c)
        {
            if (c?.value == null)
            {
                return true;
            }

            switch (c.value)
            {
                case float f:
                    return MathF.Abs(f - MathF.Truncate(f)) <= MathF.Max(1e-4f * MathF.Max(1f, MathF.Abs(f)), 1e-5f);
                case double d:
                    return Math.Abs(d - Math.Truncate(d)) <= 1e-9 * Math.Max(1.0, Math.Abs(d));
                case Half h:
                    {
                        float hf = (float)h;
                        return MathF.Abs(hf - MathF.Truncate(hf)) <= MathF.Max(1e-4f * MathF.Max(1f, MathF.Abs(hf)), 1e-5f);
                    }
                default:
                    return true;
            }
        }

        private static BigInteger ToBigInteger(object input)
        {
            switch (input)
            {
                case byte b: return b;
                case sbyte sb: return sb;
                case short s: return s;
                case ushort us: return us;
                case int i: return i;
                case uint ui: return ui;
                case long l: return l;
                case ulong ul: return ul;
                case float f: return (BigInteger)f;
                case double d: return (BigInteger)d;
                case decimal m: return (BigInteger)m;
                default:
                    return new BigInteger(Convert.ToDecimal(input, CultureInfo.InvariantCulture));
            }
        }

        public static bool IsRadixNumberLiteral(MetaConstExpressNode mcen)
        {
            var token = mcen?.token;
            if (token == null)
            {
                return false;
            }

            if (token.type == ETokenType.NumberReal)
            {
                return true;
            }

            if (token.type != ETokenType.Number)
            {
                return false;
            }

            if (string.IsNullOrEmpty(token.path) || !File.Exists(token.path))
            {
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(token.path);
                int lineIndex = token.sourceBeginLine - 1;
                if (lineIndex < 0 || lineIndex >= lines.Length)
                {
                    return false;
                }

                var line = lines[lineIndex];
                int start = token.sourceBeginChar;
                if (start < 0 || start + 1 >= line.Length)
                {
                    return false;
                }

                return line[start] == '0' &&
                       (line[start + 1] == 'x' || line[start + 1] == 'X'
                        || line[start + 1] == 'o' || line[start + 1] == 'O'
                        || line[start + 1] == 'b' || line[start + 1] == 'B');
            }
            catch
            {
                return false;
            }
        }

        public static bool TryConvertRadixUnsignedToSignedByEType(EType targetType, object input, out object converted)
        {
            converted = null;
            try
            {
                ulong u = Convert.ToUInt64(input);
                switch (targetType)
                {
                    case EType.Int8:
                        if (u <= byte.MaxValue)
                        {
                            converted = unchecked((sbyte)(byte)u);
                            return true;
                        }
                        break;
                    case EType.Int16:
                        if (u <= ushort.MaxValue)
                        {
                            converted = unchecked((short)(ushort)u);
                            return true;
                        }
                        break;
                    case EType.Int32:
                        if (u <= uint.MaxValue)
                        {
                            converted = unchecked((int)(uint)u);
                            return true;
                        }
                        break;
                    case EType.Int64:
                        converted = unchecked((long)u);
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
