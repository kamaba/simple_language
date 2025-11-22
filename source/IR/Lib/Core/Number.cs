//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;

namespace SimpleLanguage.Lib
{
    public class ByteObjectData : BaseObjectData
    {

    }
    public static class ByteObject
    {

    }


    public class Int32ObjectData : BaseObjectData
    {

    }
    public static class Int32Class
    {
        public static string GetValueToString(System.Object obj)
        {
            if( obj.GetType() == typeof(System.Int32) )
            {
                return obj.ToString();
            }
            Int32Object sobj = obj as Int32Object;
            if(sobj != null )
            {
                return sobj.value.ToString();
            }
            return "object is not int32";
        }
    }
}
