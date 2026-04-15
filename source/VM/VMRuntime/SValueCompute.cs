//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description:  compute left and right value's method example: +-*/%&|^>><<
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        // sign 0:+ 1:- 2:* 3:/ 4:% 5:& 6:| 7:^  8:<< 9:>>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeValueInline(ref SValue left, int sign, ref SValue right, bool isUnSign)
        {
            // support class-wrapped numeric objects: unbox them to primitive temporaries
            bool leftWasClass = false;
            bool rightWasClass = false;
            SValue leftPrim = left;
            SValue rightPrim = right;
            if (left.eType == EVMType.Class && left.sobject != null)
            {
                leftWasClass = true;
                leftPrim = default;
                leftPrim.SetSObject(left.sobject);
            }
            if (right.eType == EVMType.Class && right.sobject != null)
            {
                rightWasClass = true;
                rightPrim = default;
                rightPrim.SetSObject(right.sobject);
            }

            if (sign == 0)
            {
                bool leftIsString = leftPrim.eType == EVMType.String;
                bool rightIsString = rightPrim.eType == EVMType.String;
                if (leftIsString || rightIsString)
                {
                    string ls;
                    string rs;
                    if (leftIsString)
                    {
                        ls = leftPrim.stringValue ?? string.Empty;
                    }
                    else
                    {
                        var obj = left.GetValueObject();
                        ls = obj != null ? obj.ToString() : string.Empty;
                    }

                    if (rightIsString)
                    {
                        rs = rightPrim.stringValue ?? string.Empty;
                    }
                    else
                    {
                        var obj = right.GetValueObject();
                        rs = obj != null ? obj.ToString() : string.Empty;
                    }

                    left.SetStringValue(ls + rs);
                    return;
                }
            }
            // If either side is a class-wrapped NumObject, prefer NumObject operation methods
            bool leftIsNumObj = left.eType == EVMType.Class && left.sobject is NumObject;
            bool rightIsNumObj = right.eType == EVMType.Class && right.sobject is NumObject;
            if (leftIsNumObj || rightIsNumObj)
            {
                NumObject leftNum = leftIsNumObj ? (NumObject)left.sobject : null;
                NumObject rightNum = rightIsNumObj ? (NumObject)right.sobject : null;
                // left is NumObject -> perform operation on it
                if (leftNum != null)
                {
                    if (rightNum == null)
                    {
                        // wrap primitive right into a temporary NumObject
                        var tmp = new NumObject(EVMType.Float64);
                        switch (rightPrim.eType)
                        {
                            case EVMType.Float64: tmp.SetValue(rightPrim.doubleValue); break;
                            case EVMType.Float32: tmp.SetValue(rightPrim.floatValue); break;
                            case EVMType.Int64: tmp.SetValue(rightPrim.int64Value); break;
                            case EVMType.UInt64: tmp.SetValue(rightPrim.uint64Value); break;
                            case EVMType.Int32: tmp.SetValue(rightPrim.int32Value); break;
                            case EVMType.UInt32: tmp.SetValue(rightPrim.uint32Value); break;
                            case EVMType.Int16: tmp.SetValue(rightPrim.int16Value); break;
                            case EVMType.UInt16: tmp.SetValue(rightPrim.uint16Value); break;
                            case EVMType.UInt8: tmp.SetValue(rightPrim.int8Value); break;
                            case EVMType.Int8: tmp.SetValue(rightPrim.sint8Value); break;
                            default: tmp.SetValue(rightPrim.doubleValue); break;
                        }
                        leftNum.Operate(sign, tmp, isUnSign);
                        return;
                    }
                    else
                    {
                        leftNum.Operate(sign, rightNum, isUnSign);
                        return;
                    }
                }

                // Special-case: string concatenation for '+' when either side is a string
                // Treat any other type as its string representation (ToString)
                

                // right is NumObject, left is primitive -> compute into left primitive
                if (rightNum != null)
                {
                    var tmpLeft = new NumObject(EVMType.Float64);
                    switch (leftPrim.eType)
                    {
                        case EVMType.Float64: tmpLeft.SetValue(leftPrim.doubleValue); break;
                        case EVMType.Float32: tmpLeft.SetValue(leftPrim.floatValue); break;
                        case EVMType.Int64: tmpLeft.SetValue(leftPrim.int64Value); break;
                        case EVMType.UInt64: tmpLeft.SetValue(leftPrim.uint64Value); break;
                        case EVMType.Int32: tmpLeft.SetValue(leftPrim.int32Value); break;
                        case EVMType.UInt32: tmpLeft.SetValue(leftPrim.uint32Value); break;
                        case EVMType.Int16: tmpLeft.SetValue(leftPrim.int16Value); break;
                        case EVMType.UInt16: tmpLeft.SetValue(leftPrim.uint16Value); break;
                        case EVMType.UInt8: tmpLeft.SetValue(leftPrim.int8Value); break;
                        case EVMType.Int8: tmpLeft.SetValue(leftPrim.sint8Value); break;
                        default: tmpLeft.SetValue(leftPrim.doubleValue); break;
                    }
                    tmpLeft.Operate(sign, rightNum, isUnSign);
                    // write back as double into leftPrim
                    leftPrim.SetDoubleValue(tmpLeft.ToDouble());
                    left = leftPrim;
                    return;
                }
            }

            // try fast path with RawSValue for purely numeric types (treat Num as Float64 in raw path)
            bool leftNumericRaw = IsNumericType(leftPrim.eType) || leftPrim.eType == EVMType.Num;
            bool rightNumericRaw = IsNumericType(rightPrim.eType) || rightPrim.eType == EVMType.Num;
            if (leftNumericRaw && rightNumericRaw)
            {
                var rl = RawSValue.FromSValue(ref leftPrim);
                var rr = RawSValue.FromSValue(ref rightPrim);
                ComputeValueInlineRaw(ref rl, sign, ref rr, isUnSign);
                rl.ApplyToSValue(ref leftPrim);
                // write back to original left (if it was a class-wrapped numeric, unbox result to primitive)
                left = leftPrim;
                return;
            }
            // unify numeric operations with promotion rules
            // treat Num as float64 for promotion purposes
            bool leftIsFloat = (leftPrim.eType == EVMType.Float32 || leftPrim.eType == EVMType.Float64 || leftPrim.eType == EVMType.Num);
            bool rightIsFloat = (rightPrim.eType == EVMType.Float32 || rightPrim.eType == EVMType.Float64 || rightPrim.eType == EVMType.Num);

            // If any side is float -> use double precision
            if (leftIsFloat || rightIsFloat)
            {
                double a = (leftPrim.eType == EVMType.Float64 || leftPrim.eType == EVMType.Num) ? leftPrim.doubleValue : (leftPrim.eType == EVMType.Float32 ? (double)leftPrim.floatValue : leftPrim.ConvertToDoubleFromIntTypes());
                double b = (rightPrim.eType == EVMType.Float64 || rightPrim.eType == EVMType.Num) ? rightPrim.doubleValue : (rightPrim.eType == EVMType.Float32 ? (double)rightPrim.floatValue : rightPrim.ConvertToDoubleFromIntTypes());
                double r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0 : a / b; break;
                    case 4: r = (b == 0) ? 0 : a % b; break;
                    default:
                        Debug.Write("Error 不支持浮点的位运算");
                        break;
                }
                if (leftPrim.eType == EVMType.Float64 || leftPrim.eType == EVMType.Num)
                    leftPrim.doubleValue = r;
                else
                    leftPrim.floatValue = (float)r;
                left = leftPrim;
                return;
            }

            // integer operations - decide signed vs unsigned based on flag or operand types
            bool useUnsigned = isUnSign || left.IsUnsignedType(leftPrim.eType) || right.IsUnsignedType(rightPrim.eType);
            if (useUnsigned)
            {
                ulong a = leftPrim.ConvertToULong();
                ulong b = rightPrim.ConvertToULong();
                ulong r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0UL : a / b; break;
                    case 4: r = (b == 0) ? 0UL : a % b; break;
                    case 5: r = a & b; break;
                    case 6: r = a | b; break;
                    case 7: r = a ^ b; break;
                    case 8: r = a << (int)b; break;
                    case 9: r = a >> (int)b; break;
                }
                // write back according to original left type
                leftPrim.AssignULongToType(r);
                left = leftPrim;
                return;
            }

            // signed integer
            long la = leftPrim.ConvertToLong();
            long lb = rightPrim.ConvertToLong();
            long lr = 0;
            switch (sign)
            {
                case 0: lr = la + lb; break;
                case 1: lr = la - lb; break;
                case 2: lr = la * lb; break;
                case 3: lr = (lb == 0) ? 0L : la / lb; break;
                case 4: lr = (lb == 0) ? 0L : la % lb; break;
                case 5: lr = la & lb; break;
                case 6: lr = la | lb; break;
                case 7: lr = la ^ lb; break;
                case 8: lr = la << (int)lb; break;
                case 9: lr = la >> (int)lb; break;
            }
            leftPrim.AssignLongToType(lr);
            left = leftPrim;
        }

        public void ComputeSVAlue(int sign, ref SValue svalue, bool isUnSign)
        {
            ComputeValueInline(ref this, sign, ref svalue, isUnSign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeValueInlineRaw(ref RawSValue left, int sign, ref RawSValue right, bool isUnSign)
        {
            var promoteType = GetRawBinaryPromotionType(left.eType, right.eType, sign, isUnSign);
            if (promoteType == EVMType.Float64 || promoteType == EVMType.Num)
            {
                double a = (left.eType == EVMType.Float64 || left.eType == EVMType.Num) ? left.Float64 : (left.eType == EVMType.Float32 ? left.Float32 : (double)left.Int64);
                double b = (right.eType == EVMType.Float64 || right.eType == EVMType.Num) ? right.Float64 : (right.eType == EVMType.Float32 ? right.Float32 : (double)right.Int64);
                double r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0 : a / b; break;
                    case 4: r = (b == 0) ? 0 : a % b; break;
                    default:
                        Debug.Write("Error 不支持浮点的位运算");
                        break;
                }
                left.eType = EVMType.Float64;
                left.Float64 = r;
                return;
            }
            if (promoteType == EVMType.Float32)
            {
                float a = left.eType == EVMType.Float32
                    ? left.Float32
                    : (left.eType == EVMType.Float64 || left.eType == EVMType.Num ? (float)left.Float64 : (float)left.Int64);
                float b = right.eType == EVMType.Float32
                    ? right.Float32
                    : (right.eType == EVMType.Float64 || right.eType == EVMType.Num ? (float)right.Float64 : (float)right.Int64);
                float r = 0f;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0f) ? 0f : a / b; break;
                    case 4: r = (b == 0f) ? 0f : a % b; break;
                    default:
                        Debug.Write("Error 不支持浮点的位运算");
                        break;
                }
                left.eType = EVMType.Float32;
                left.Float32 = r;
                return;
            }

            bool useUnsigned = (promoteType == EVMType.UInt64 || promoteType == EVMType.UInt32 || promoteType == EVMType.UInt16 || promoteType == EVMType.UInt8);
            if (useUnsigned)
            {
                ulong a = left.UInt64;
                ulong b = right.UInt64;
                ulong r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0UL : a / b; break;
                    case 4: r = (b == 0) ? 0UL : a % b; break;
                    case 5: r = a & b; break;
                    case 6: r = a | b; break;
                    case 7: r = a ^ b; break;
                    case 8: r = a << (int)b; break;
                    case 9: r = a >> (int)b; break;
                }
                left.eType = promoteType;
                switch (promoteType)
                {
                    case EVMType.UInt64: left.UInt64 = r; break;
                    case EVMType.UInt32: left.UInt32 = (uint)r; break;
                    case EVMType.UInt16: left.UInt16 = (ushort)r; break;
                    case EVMType.UInt8: left.Int8 = (byte)r; break;
                    default: left.UInt64 = r; break;
                }
                return;
            }

            long la = left.Int64;
            long lb = right.Int64;
            long lr = 0;
            switch (sign)
            {
                case 0: lr = la + lb; break;
                case 1: lr = la - lb; break;
                case 2: lr = la * lb; break;
                case 3: lr = (lb == 0) ? 0L : la / lb; break;
                case 4: lr = (lb == 0) ? 0L : la % lb; break;
                case 5: lr = la & lb; break;
                case 6: lr = la | lb; break;
                case 7: lr = la ^ lb; break;
                case 8: lr = la << (int)lb; break;
                case 9: lr = la >> (int)lb; break;
            }
            left.eType = promoteType;
            switch (promoteType)
            {
                case EVMType.Int64: left.Int64 = lr; break;
                case EVMType.Int32: left.Int32 = (int)lr; break;
                case EVMType.Int16: left.Int16 = (short)lr; break;
                case EVMType.Int8: left.SInt8 = (sbyte)lr; break;
                default: left.Int64 = lr; break;
            }
        }

        private static bool IsRawUnsignedInt(EVMType t)
        {
            return t == EVMType.UInt8 || t == EVMType.UInt16 || t == EVMType.UInt32 || t == EVMType.UInt64;
        }

        private static bool IsRawSignedInt(EVMType t)
        {
            return t == EVMType.Int8 || t == EVMType.Int16 || t == EVMType.Int32 || t == EVMType.Int64;
        }

        private static EVMType GetRawBinaryPromotionType(EVMType left, EVMType right, int sign, bool isUnSign)
        {
            // Mainstream numeric promotion (C-like/C#/JVM style with pragmatic VM fallback):
            // 1) float64/num dominates, 2) float32 dominates over ints,
            // 3) integer ops promote narrow ints to int32, then widen by signed/unsigned mix.
            bool leftF64 = left == EVMType.Float64 || left == EVMType.Num;
            bool rightF64 = right == EVMType.Float64 || right == EVMType.Num;
            if (leftF64 || rightF64) return EVMType.Float64;
            if (left == EVMType.Float32 || right == EVMType.Float32) return EVMType.Float32;

            bool lUnsigned = IsRawUnsignedInt(left);
            bool rUnsigned = IsRawUnsignedInt(right);
            bool lSigned = IsRawSignedInt(left);
            bool rSigned = IsRawSignedInt(right);

            if (isUnSign)
            {
                if (left == EVMType.UInt64 || right == EVMType.UInt64) return EVMType.UInt64;
                if (left == EVMType.UInt32 || right == EVMType.UInt32) return EVMType.UInt32;
                return EVMType.Int32;
            }

            // Bitwise/shift on mixed sign types: keep 64-bit safety.
            bool isBitOp = sign >= 5 && sign <= 9;
            if (isBitOp && lUnsigned && rUnsigned)
            {
                // For pure unsigned bit operations, keep minimal unsigned width
                // instead of forcing int32 widening.
                if (left == EVMType.UInt64 || right == EVMType.UInt64) return EVMType.UInt64;
                if (left == EVMType.UInt32 || right == EVMType.UInt32) return EVMType.UInt32;
                if (left == EVMType.UInt16 || right == EVMType.UInt16) return EVMType.UInt16;
                return EVMType.UInt8;
            }

            if (left == EVMType.UInt64 || right == EVMType.UInt64)
            {
                if (lSigned || rSigned || isBitOp) return EVMType.Int64;
                return EVMType.UInt64;
            }
            if (left == EVMType.Int64 || right == EVMType.Int64) return EVMType.Int64;

            if (left == EVMType.UInt32 || right == EVMType.UInt32)
            {
                if (left == EVMType.UInt32 && right == EVMType.UInt32) return EVMType.UInt32;
                // int32 + uint32 => int64 (same as C# numeric promotion)
                return EVMType.Int64;
            }

            // byte/sbyte/short/ushort/int32 -> int32
            return EVMType.Int32;
        }

        public void AddSValue(ref SValue sval, bool isUnsign, out bool isMethodCall)
        {
            isMethodCall = false;
            if (sval.eType == EVMType.String)
            {
                string str = "";
                if (this.eType == EVMType.Class)
                {
                    ClassObject co = this.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.runtimeClass.GetNonStaticMethodIndexByName("toString", out int index);
                        if (method != null)
                        {
                            CLRVM.RunIRMethod(null, method, false);
                            var clrvm = CLRVM.clrRuntimeStack.Peek();
                            SValue curval = clrvm.GetCurrentIndexValue(clrvm.m_ValueIndex - 1);
                            str = curval.stringValue;
                        }
                    }
                    else
                    {
                        str = sval.sobject.ToString();
                    }
                }
                else
                {
                    str = sval.GetValueObject().ToString();
                }
                stringValue = this.GetValueObject().ToString() + str;
                this.eType = EVMType.String;
            }
            else if (this.eType == EVMType.String)
            {
                string str = "";
                if (sval.eType == EVMType.Class)
                {
                    ClassObject co = sval.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.runtimeClass.GetNonStaticMethodIndexByName("toString", out int index);
                        if (method != null)
                        {
                            CLRVM.RunIRMethod(null, method, false);
                            var clrvm = CLRVM.clrRuntimeStack.Peek();
                            SValue curval = clrvm.GetCurrentIndexValue(clrvm.m_ValueIndex - 1);
                            str = curval.stringValue;
                            clrvm.m_ValueIndex--;
                        }
                    }
                    else
                    {
                        str = sval.sobject.ToString();
                    }
                }
                else
                {
                    str = sval.GetValueObject().ToString();
                }
                stringValue = this.GetValueObject().ToString() + str;
            }
            else if (this.eType == EVMType.Array)
            {
                // 处理array1 + array2
            }
            else
            {
                if (this.eType == EVMType.Class)
                {
                    ClassObject co = sval.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.runtimeClass.GetOperatorMethodIndexByMethod("_add_", out int index);
                        if (method != null)
                        {
                            CLRVM.RunIRMethod(null, method, false);
                            isMethodCall = true;
                        }
                    }
                }
                else
                {
                    ComputeSVAlue(0, ref sval, isUnsign);
                }
            }
        }
        public void MinusSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(1, ref sval, isUnsign);
        }
        public void MultiplySValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(2, ref sval, isUnsign);
        }
        public void DivSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(3, ref sval, isUnsign);
        }
        public void ModuloSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(4, ref sval, isUnsign);
        }
        public void CombineSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(5, ref sval, isUnsign);
        }
        public void InclusiveOrSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(6, ref sval, isUnsign);
        }
        public void XORSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(7, ref sval, isUnsign);
        }
        public void ShrSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(9, ref sval, isUnsign);
        }
        public void ShiSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(8, ref sval, isUnsign);
        }
        public void NotSValue()
        {
            switch (eType)
            {
                case EVMType.UInt8:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int8:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (sint8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Boolean:
                    {
                        int8Value = (int8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                //case EVMType.Char:
                //    {
                //        eType = EVMType.Boolean;
                //        int8Value = (charValue == 0) ? (byte)1 : (byte)0;
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int32:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int64:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int64Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt64:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint64Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
            }
        }
        public void NegSValue(bool isUnsign)
        {
            switch (eType)
            {
                case EVMType.UInt8:
                    {
                        eType = EVMType.Int32;
                        int32Value = -int8Value;
                    }
                    break;
                case EVMType.Int8:
                    {
                        // Keep runtime type stable for unary minus.
                        sint8Value = unchecked((sbyte)(-sint8Value));
                    }
                    break;
                //case EVMType.Char:
                //    {
                //        eType = EVMType.Int32;
                //        int32Value = -charValue;
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        // Keep runtime type stable for unary minus.
                        int16Value = unchecked((short)(-int16Value));
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.Int32;
                        int32Value = (-uint16Value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        // Keep runtime type stable for unary minus.
                        int32Value = unchecked(-int32Value);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.Int64;
                        int64Value = -uint32Value;
                    }
                    break;
                case EVMType.Int64:
                    {
                        // Keep runtime type stable for unary minus.
                        int64Value = unchecked(-int64Value);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        Debug.Write("Error -value 1");
                    }
                    break;
                case EVMType.Float32:
                    {
                        floatValue = -floatValue;
                    }
                    break;
                case EVMType.Float64:
                    {
                        doubleValue = -doubleValue;
                    }
                    break;
                case EVMType.Num:
                    {
                        // treat Num as double
                        doubleValue = -doubleValue;
                    }
                    break;
                case EVMType.Class:
                    {
                        // if wrapped numeric object (NumObject), negate its value
                        if (sobject is NumObject nobj)
                        {
                            double val = nobj.ToDouble();
                            nobj.SetValue(-val);
                        }
                        else
                        {
                            Debug.Write("Error -value on non-numeric class");
                        }
                    }
                    break;
                default:
                    {
                        Debug.Write("Error -value 2");
                    }
                    break;
            }
        }
    }
}
