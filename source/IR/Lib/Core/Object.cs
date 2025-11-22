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
    public class MemTable
    {
        public int refCount = 0;
    }
    public class BaseObjectData
    {
        public int hashCode = 0;
        public RuntimeType runtimeType = null;
        public MemTable memTable = null;
        public EType etype = EType.None;
    }
    public class AnyObjectData : BaseObjectData
    {
        public BaseObjectData data;
    }
    public static class ObjectClass
    {
        public static int GetHashCodeByObject(BaseObjectData obj)
        {
            return obj.hashCode;
        }
        public static int GetHashCodeBySObject( System.Object obj)
        {
            SObject sobj = obj as SObject;
            return sobj.id;
        }
    }
}
