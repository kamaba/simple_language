//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;
using System;

namespace SimpleLanguage.Lib
{
    public static class ByteClass
    {
        public static string ByteToString(byte sobj)
        {
            return sobj.ToString();
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
        public static string Int32ToString(Int32Object sobj)
        {
            return GetValueToString(sobj);
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
        public static string GetValueToString(UInt64Object sobj)
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
    public static class Float32Class
    {
        public static string GetValueToString(Float32Object sobj)
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
        public static float ToFloat32(Float32Object sobj)
        {
            if (sobj == null) return 0.0f;
            return sobj.value;
        }
        public static int ToInt32(Float32Object sobj)
        {
            if (sobj == null) return 0;
            return (int)sobj.value;
        }
        public static float Abs(Float32Object sobj)
        {
            if (sobj == null) return 0.0f;
            return Math.Abs(sobj.value);
        }
        public static float Floor(Float32Object sobj)
        {
            if (sobj == null) return 0.0f;
            return (float)Math.Floor(sobj.value);
        }
        public static float Ceil(Float32Object sobj)
        {
            if (sobj == null) return 0.0f;
            return (float)Math.Ceiling(sobj.value);
        }
        public static int Compare(Float32Object a, Float32Object b)
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
            if (obj is Float32Object f) return (int)f.value;
            if (obj is Float64Object d) return (int)d.value;
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float32Object f2) return (int)f2.value;
                if (so is Float64Object d2) return (int)d2.value;
                if (so is Int32Object i2) return i2.value;
            }
            try { return Convert.ToInt32(obj); } catch { return 0; }
        }
        public static float Abs(object obj)
        {
            if (obj == null) return 0.0f;
            if (obj is Float32Object f) return Math.Abs(f.value);
            if (obj is Float64Object d) return (float)Math.Abs(d.value);
            if (obj is Int32Object i) return Math.Abs(i.value);
            if (obj is SObject so)
            {
                if (so is Float32Object f2) return Math.Abs(f2.value);
                if (so is Float64Object d2) return (float)Math.Abs(d2.value);
                if (so is Int32Object i2) return Math.Abs(i2.value);
            }
            try { return (float)Math.Abs(Convert.ToDouble(obj)); } catch { return 0.0f; }
        }
        public static float Floor(object obj)
        {
            if (obj == null) return 0.0f;
            if (obj is Float32Object f) return (float)Math.Floor(f.value);
            if (obj is Float64Object d) return (float)Math.Floor(d.value);
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float32Object f2) return (float)Math.Floor(f2.value);
                if (so is Float64Object d2) return (float)Math.Floor(d2.value);
                if (so is Int32Object i2) return i2.value;
            }
            try { return (float)Math.Floor(Convert.ToDouble(obj)); } catch { return 0.0f; }
        }
        public static float Ceil(object obj)
        {
            if (obj == null) return 0.0f;
            if (obj is Float32Object f) return (float)Math.Ceiling(f.value);
            if (obj is Float64Object d) return (float)Math.Ceiling(d.value);
            if (obj is Int32Object i) return i.value;
            if (obj is SObject so)
            {
                if (so is Float32Object f2) return (float)Math.Ceiling(f2.value);
                if (so is Float64Object d2) return (float)Math.Ceiling(d2.value);
                if (so is Int32Object i2) return i2.value;
            }
            try { return (float)Math.Ceiling(Convert.ToDouble(obj)); } catch { return 0.0f; }
        }
        public static int Compare(object aobj, object bobj)
        {
            if (aobj == null && bobj == null) return 0;
            if (aobj == null) return -1;
            if (bobj == null) return 1;
            double av = 0.0, bv = 0.0;
            if (aobj is Float32Object fa) av = fa.value; else if (aobj is Float64Object da) av = da.value; else if (aobj is Int32Object ia) av = ia.value; else if (aobj is SObject so1) { if (so1 is Float32Object f1) av = f1.value; else if (so1 is Float64Object d1) av = d1.value; else if (so1 is Int32Object i1) av = i1.value; }
            if (bobj is Float32Object fb) bv = fb.value; else if (bobj is Float64Object db) bv = db.value; else if (bobj is Int32Object ib) bv = ib.value; else if (bobj is SObject so2) { if (so2 is Float32Object f2) bv = f2.value; else if (so2 is Float64Object d2) bv = d2.value; else if (so2 is Int32Object i2) bv = i2.value; }
            if (av == bv) return 0; return av > bv ? 1 : -1;
        }
    }
    public static class Float64Class
    {
        public static string GetValueToString(Float64Object sobj)
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
