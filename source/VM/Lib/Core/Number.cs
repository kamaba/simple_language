//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************

using System;
using SimpleLanguage.VM;

namespace SimpleLanguage.Lib
{
    public static class NumClass
    {

        public static bool NumToBool(double sobj)
        {
            return Convert.ToBoolean(sobj > 0);
        }
        public static byte NumToByte(double sobj, byte _byte)
        {
            return Convert.ToByte(sobj);
        }
        public static sbyte NumToSByte(double sobj, byte _byte)
        {
            return Convert.ToSByte(sobj);
        }
        public static short NumToInt16(double sobj)
        {
            return Convert.ToInt16(sobj);
        }
        public static UInt16 NumToUInt16(double sobj)
        {
            return Convert.ToUInt16(sobj);
        }
        public static UInt32 NumToInt32(double sobj)
        {
            return Convert.ToUInt32(sobj);
        }
        public static UInt32 NumToUInt32(double sobj)
        {
            return Convert.ToUInt32(sobj);
        }
        public static Int64 NumToInt64(double sobj)
        {
            return Convert.ToInt64(sobj);
        }
        public static UInt64 NumToUInt64(double sobj)
        {
            return Convert.ToUInt64(sobj);
        }
        public static Single NumToFloat32(double sobj)
        {
            return Convert.ToSingle(sobj);
        }
        public static Double NumToFloat64(double sobj)
        {
            return Convert.ToDouble(sobj);
        }

        public static bool NumToBool(object obj)
        {
            if (obj == null) return false;
            if (obj is NumObject n)
            {
                return n.ToDouble() != 0.0;
            }
            if (obj is SObject so)
            {
                if (so is NumObject no) return no.ToDouble() != 0.0;
                if (so is BoolObject b) return b.value;
                if (so is Int8Object ib) return ib.value != 0;
                if (so is SInt8Object sb) return sb.value != 0;
                if (so is Int16Object i16) return i16.value != 0;
                if (so is UInt16Object u16) return u16.value != 0;
                if (so is Int32Object i32) return i32.value != 0;
                if (so is UInt32Object u32) return u32.value != 0;
                if (so is Int64Object i64) return i64.value != 0;
                if (so is UInt64Object u64) return u64.value != 0;
                if (so is Float32Object f32) return f32.value != 0.0f;
                if (so is Float64Object f64) return f64.value != 0.0;
            }
            try { return Convert.ToBoolean(obj); } catch { return false; }
        }

        public static byte NumToByte(object obj, byte index = 0)
        {
            if (obj == null) return 0;
            if (obj is NumObject n) return (byte)n.ToDouble();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (byte)no.ToDouble();
                if (so is Int8Object ib) return ib.value;
                if (so is SInt8Object sb) return (byte)sb.value;
            }
            try { return Convert.ToByte(obj); } catch { return 0; }
        }

        public static short NumToInt16(object obj)
        {
            if (obj == null) return 0;
            if (obj is NumObject n) return (short)n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (short)no.ToInt64();
                if (so is Int16Object i16) return i16.value;
            }
            try { return Convert.ToInt16(obj); } catch { return 0; }
        }

        public static ushort NumToUInt16(object obj)
        {
            if (obj == null) return 0;
            if (obj is NumObject n) return (ushort)n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (ushort)no.ToInt64();
                if (so is UInt16Object u16) return u16.value;
            }
            try { return Convert.ToUInt16(obj); } catch { return 0; }
        }

        public static int NumToInt32(object obj)
        {
            if (obj == null) return 0;
            if (obj is NumObject n) return (int)n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (int)no.ToInt64();
                if (so is Int32Object i32) return i32.value;
            }
            try { return Convert.ToInt32(obj); } catch { return 0; }
        }

        public static uint NumToUInt32(object obj)
        {
            if (obj == null) return 0;
            if (obj is NumObject n) return (uint)n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (uint)no.ToInt64();
                if (so is UInt32Object u32) return u32.value;
            }
            try { return Convert.ToUInt32(obj); } catch { return 0; }
        }

        public static long NumToInt64(object obj)
        {
            if (obj == null) return 0L;
            if (obj is NumObject n) return n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return no.ToInt64();
                if (so is Int64Object i64) return i64.value;
            }
            try { return Convert.ToInt64(obj); } catch { return 0L; }
        }

        public static ulong NumToUInt64(object obj)
        {
            if (obj == null) return 0UL;
            if (obj is NumObject n) return (ulong)n.ToInt64();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (ulong)no.ToInt64();
                if (so is UInt64Object u64) return u64.value;
            }
            try { return Convert.ToUInt64(obj); } catch { return 0UL; }
        }

        public static float NumToFloat32(object obj)
        {
            if (obj == null) return 0.0f;
            if (obj is NumObject n) return (float)n.ToDouble();
            if (obj is SObject so)
            {
                if (so is NumObject no) return (float)no.ToDouble();
                if (so is Float32Object f32) return f32.value;
            }
            try { return Convert.ToSingle(obj); } catch { return 0.0f; }
        }

        public static double NumToFloat64(object obj)
        {
            if (obj == null) return 0.0;
            if (obj is NumObject n) return n.ToDouble();
            if (obj is SObject so)
            {
                if (so is NumObject no) return no.ToDouble();
                if (so is Float64Object f64) return f64.value;
            }
            try { return Convert.ToDouble(obj); } catch { return 0.0; }
        }
    }
    public static class ByteClass
    {
        public static string ByteToString(byte sobj)
        {
            return sobj.ToString();
        }
        // parse helpers
        public static int Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            try { return Convert.ToInt32(s); } catch { return 0; }
        }
        public static int ParseInt(string s, int radix)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // support common radices using Convert when possible
            if (radix == 10)
            {
                return Parse(s);
            }
            try { return Convert.ToInt32(s, radix); } catch { /* fallback to manual */ }
            // manual parse for other radices
            int r = radix;
            if (r < 2 || r > 36) return 0;
            int sign = 1;
            int idx = 0;
            if (s.Length > 0 && (s[0] == '+' || s[0] == '-'))
            {
                if (s[0] == '-') sign = -1;
                idx = 1;
            }
            long acc = 0;
            for (int i = idx; i < s.Length; i++)
            {
                char c = s[i];
                int digit = 0;
                if (c >= '0' && c <= '9') digit = c - '0';
                else if (c >= 'A' && c <= 'Z') digit = 10 + (c - 'A');
                else if (c >= 'a' && c <= 'z') digit = 10 + (c - 'a');
                else break;
                if (digit >= r) break;
                acc = acc * r + digit;
                if (acc > 0x7FFFFFFF) acc = 0x7FFFFFFF;
            }
            return (int)(acc * sign);
        }

        public static string ToRadixString(int value, int radix)
        {
            if (radix < 2 || radix > 36) return "";
            try { return Convert.ToString(value, radix); } catch { }
            // fallback manual
            if (value == 0) return "0";
            bool neg = value < 0;
            long v = neg ? -(long)value : value;
            var sb = new System.Text.StringBuilder();
            while (v != 0)
            {
                int d = (int)(v % radix);
                char c = (d < 10) ? (char)('0' + d) : (char)('a' + (d - 10));
                sb.Append(c);
                v = v / radix;
            }
            if (neg) sb.Append('-');
            var arr = sb.ToString().ToCharArray();
            System.Array.Reverse(arr);
            return new string(arr);
        }
        public static string ToBinaryString(int value) { return ToRadixString(value, 2); }
        public static string ToHexString(int value) { return ToRadixString(value, 16); }
        public static string ToOctalString(int value) { return ToRadixString(value, 8); }

        // numeric helpers
        public static int Abs(int value) { return Math.Abs(value); }
        public static int Sign(int value) { return Math.Sign(value); }
        public static bool IsEven(int value) { return (value & 1) == 0; }
        public static bool IsOdd(int value) { return (value & 1) != 0; }
        public static int BitLength(int value)
        {
            if (value == 0) return 0;
            uint v = (uint)(value < 0 ? ~value : value);
            int bits = 0;
            while (v != 0)
            {
                v >>= 1;
                bits++;
            }
            return bits;
        }
    }
    
    public static class SByteClass
    {
        public static string SByteToString(sbyte sobj)
        {
            return sobj.ToString();
        }
    }
    public static class Int16Class
    {
        public static string Int16ToString(Int16 sobj)
        {
            return sobj.ToString();
        }
    }
    public static class UInt16Class
    {
        public static string Int16ToString(Int16 sobj)
        {
            return sobj.ToString();
        }
    }

    public static class Int32Class
    {
        public static int? Parse( string val )
        {
            return 0;
        }
        public static string GetValueToString(Int32Object sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.value.ToString();
            }
            return "object is not int32";
        }
        public static int Abs(int sobj)
        {
            return Math.Abs(sobj);
        }
        public static string Int32ToString(Int32Object sobj)
        {
            return GetValueToString(sobj);
        }
        public static bool Int32ToBool(int sobj)
        {
            return Convert.ToBoolean(sobj>0);
        }
        public static byte Int32ToByte(int sobj, byte _byte )
        {
            return Convert.ToByte(sobj);
        }
        public static sbyte Int32ToSByte(int sobj, byte _byte )
        {
            return Convert.ToSByte(sobj);
        }
        public static short Int32ToInt16(int sobj)
        {
            return Convert.ToInt16(sobj);
        }
        public static UInt16 Int32ToUInt16(int sobj)
        {
            return Convert.ToUInt16(sobj);
        }
        public static UInt32 Int32ToUInt32(int sobj)
        {
            return Convert.ToUInt32(sobj);
        }
        public static Int64 Int32ToInt64(int sobj)
        {
            return Convert.ToInt64(sobj);
        }
        public static UInt64 Int32ToUInt64(int sobj)
        {
            return Convert.ToUInt64(sobj);
        }
        public static Single Int32ToFloat32(int sobj)
        {
            return Convert.ToSingle(sobj);
        }
        public static Double Int32ToFloat64(int sobj)
        {
            return Convert.ToDouble(sobj);
        }
        public static string Int32ToString(int sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
    }
    public static class UInt32Class
    {
        public static string GetValueToString(UInt32Object sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.value.ToString();
            }
            return "object is not int32";
        }
        public static string UInt32ToString(uint sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
    }

    public static class Int64Class
    {
        public static string GetValueToString(Int64Object sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.value.ToString();
            }
            return "object is not int32";
        }
        public static string Int32ToString(int sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
    }
    public static class UInt64Class
    {
        public static string GetValueToString(UInt64 sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.ToString();
            }
            return "object is not int32";
        }
        public static string UInt32ToString(uint sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
    }
    public static class Float32Class
    {
        public static string GetValueToString(float sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.ToString();
            }
            return "object is not int32";
        }
        public static string Int32ToString(int sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
        public static float ToFloat32(float sobj)
        {
            if (sobj == null) return 0.0f;
            return sobj;
        }
        public static int ToInt32(float sobj)
        {
            if (sobj == null) return 0;
            return (int)sobj;
        }
        public static float Abs(float sobj)
        {
            if (sobj == null) return 0.0f;
            return Math.Abs(sobj);
        }
        public static float Floor(float sobj)
        {
            if (sobj == null) return 0.0f;
            return (float)Math.Floor(sobj);
        }
        public static float Ceil(float sobj)
        {
            if (sobj == null) return 0.0f;
            return (float)Math.Ceiling(sobj.value);
        }
        public static int Compare(float a, float b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a == b) return 0;
            return a > b ? 1 : -1;
        }
        public static int ToInt32(object obj)
        {
            if (obj == null) return 0;
            //if (obj is Float32Object f) return (int)f.value;
            //if (obj is Float64Object d) return (int)d.value;
            //if (obj is Int32Object i) return i.value;
            //if (obj is SObject so)
            //{
            //    if (so is Float32Object f2) return (int)f2.value;
            //    if (so is Float64Object d2) return (int)d2.value;
            //    if (so is Int32Object i2) return i2.value;
            //}
            try { return Convert.ToInt32(obj); } catch { return 0; }
        }
        public static float Abs(object obj)
        {
            if (obj == null) return 0.0f;
            //if (obj is Float32Object f) return Math.Abs(f.value);
            //if (obj is Float64Object d) return (float)Math.Abs(d.value);
            //if (obj is Int32Object i) return Math.Abs(i.value);
            //if (obj is SObject so)
            //{
            //    if (so is Float32Object f2) return Math.Abs(f2.value);
            //    if (so is Float64Object d2) return (float)Math.Abs(d2.value);
            //    if (so is Int32Object i2) return Math.Abs(i2.value);
            //}
            //try { return (float)Math.Abs(Convert.ToDouble(obj)); } catch { return 0.0f; }
            return 0.0f;
        }
        public static float Floor(object obj)
        {
            if (obj == null) return 0.0f;
            //if (obj is Float32Object f) return (float)Math.Floor(f.value);
            //if (obj is Float64Object d) return (float)Math.Floor(d.value);
            //if (obj is Int32Object i) return i.value;
            //if (obj is SObject so)
            //{
            //    if (so is Float32Object f2) return (float)Math.Floor(f2.value);
            //    if (so is Float64Object d2) return (float)Math.Floor(d2.value);
            //    if (so is Int32Object i2) return i2.value;
            //}
            try { return (float)Math.Floor(Convert.ToDouble(obj)); } catch { return 0.0f; }
        }
        public static float Ceil(object obj)
        {
            if (obj == null) return 0.0f;
            //if (obj is Float32Object f) return (float)Math.Ceiling(f.value);
            //if (obj is Float64Object d) return (float)Math.Ceiling(d.value);
            //if (obj is Int32Object i) return i.value;
            //if (obj is SObject so)
            //{
            //    if (so is Float32Object f2) return (float)Math.Ceiling(f2.value);
            //    if (so is Float64Object d2) return (float)Math.Ceiling(d2.value);
            //    if (so is Int32Object i2) return i2.value;
            //}
            try { return (float)Math.Ceiling(Convert.ToDouble(obj)); } catch { return 0.0f; }
        }
        public static int Compare(object aobj, object bobj)
        {
            if (aobj == null && bobj == null) return 0;
            if (aobj == null) return -1;
            if (bobj == null) return 1;
            double av = 0.0, bv = 0.0;
            //if (aobj is Float32Object fa) av = fa.value; else if (aobj is Float64Object da) av = da.value; else if (aobj is Int32Object ia) av = ia.value; else if (aobj is SObject so1) { if (so1 is Float32Object f1) av = f1.value; else if (so1 is Float64Object d1) av = d1.value; else if (so1 is Int32Object i1) av = i1.value; }
            //if (bobj is Float32Object fb) bv = fb.value; else if (bobj is Float64Object db) bv = db.value; else if (bobj is Int32Object ib) bv = ib.value; else if (bobj is SObject so2) { if (so2 is Float32Object f2) bv = f2.value; else if (so2 is Float64Object d2) bv = d2.value; else if (so2 is Int32Object i2) bv = i2.value; }
            if (av == bv) return 0; return av > bv ? 1 : -1;
        }
    }
    public static class Float64Class
    {
        public static string GetValueToString( float sobj)
        {
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            if (sobj != null)
            {
                return sobj.ToString();
            }
            return "object is not int32";
        }
        public static string UInt32ToString(uint sobj)
        {
            return sobj.ToString();
            //if( obj.GetType() == typeof(System.Int32) )
            //{
            //    return obj.ToString();
            //}
            //Int32Object sobj = obj as Int32Object;
            //if (sobj != null)
            //{
            //    return sobj.value.ToString();
            //}
            //return "object is not int32";
        }
        public static double ToDouble(Float64Object sobj)
        {
            if (sobj == null) return 0.0;
            return sobj.value;
        }
        public static int ToInt32(Float64Object sobj)
        {
            if (sobj == null) return 0;
            return (int)sobj.value;
        }
        public static double Abs(Float64Object sobj)
        {
            if (sobj == null) return 0.0;
            return Math.Abs(sobj.value);
        }
        public static double Floor(Float64Object sobj)
        {
            if (sobj == null) return 0.0;
            return Math.Floor(sobj.value);
        }
        public static double Ceil(Float64Object sobj)
        {
            if (sobj == null) return 0.0;
            return Math.Ceiling(sobj.value);
        }
        public static int Compare(Float64Object a, Float64Object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a.value == b.value) return 0;
            return a.value > b.value ? 1 : -1;
        }
        public static int ToInt32(object obj)
        {
            if (obj == null) return 0;
            if (obj is Float64Object d) return (int)d.value;
            if (obj is Float32Object f) return (int)f.value;
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float64Object d2) return (int)d2.value;
                if (so is Float32Object f2) return (int)f2.value;
                if (so is Int32Object i2) return i2.value;
            }
            try { return Convert.ToInt32(obj); } catch { return 0; }
        }
        public static double Abs(object obj)
        {
            if (obj == null) return 0.0;
            if (obj is Float64Object d) return Math.Abs(d.value);
            if (obj is Float32Object f) return Math.Abs(f.value);
            if (obj is Int32Object i) return Math.Abs(i.value);
            if (obj is SObject so)
            {
                if (so is Float64Object d2) return Math.Abs(d2.value);
                if (so is Float32Object f2) return Math.Abs(f2.value);
                if (so is Int32Object i2) return Math.Abs(i2.value);
            }
            try { return Math.Abs(Convert.ToDouble(obj)); } catch { return 0.0; }
        }
        public static double Floor(object obj)
        {
            if (obj == null) return 0.0;
            if (obj is Float64Object d) return Math.Floor(d.value);
            if (obj is Float32Object f) return Math.Floor(f.value);
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float64Object d2) return Math.Floor(d2.value);
                if (so is Float32Object f2) return Math.Floor(f2.value);
                if (so is Int32Object i2) return i2.value;
            }
            try { return Math.Floor(Convert.ToDouble(obj)); } catch { return 0.0; }
        }
        public static double Ceil(object obj)
        {
            if (obj == null) return 0.0;
            if (obj is Float64Object d) return Math.Ceiling(d.value);
            if (obj is Float32Object f) return Math.Ceiling(f.value);
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float64Object d2) return Math.Ceiling(d2.value);
                if (so is Float32Object f2) return Math.Ceiling(f2.value);
                if (so is Int32Object i2) return i2.value;
            }
            try { return Math.Ceiling(Convert.ToDouble(obj)); } catch { return 0.0; }
        }
    }
}
