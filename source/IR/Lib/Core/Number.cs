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
    }
}
