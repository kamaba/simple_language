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
        public static int GetHashCodeBySObject(System.Object obj)
        {
            SObject sobj = obj as SObject;
            return sobj.id;
        }
        public static int RefCount(System.Object obj)
        {
            SObject sobj = obj as SObject;
            return sobj.refCount;
        }
        public static SObject ObjectWeakRef(System.Object obj)
        {
            //SObject sobj = obj as SObject;
            //return sobj;
            return null;
        }
        public static bool EqualObject( System.Object obj1, System.Object obj2 )
        {
            SObject sobj1 = obj1 as SObject;
            SObject sobj2 = obj2 as SObject;

            return sobj1 == sobj2;
        }
    }
}
