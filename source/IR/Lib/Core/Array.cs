//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.VM;
using SimpleLanguage.VM.Runtime;
using System;
using System.Runtime.InteropServices;

namespace SimpleLanguage.Lib
{
    public static class ArrayClass
    {
        //public static Int64 CreateArray(Int32Object arrayLength, Int32Object elementSize) => CreateArray(arrayLength.value, elementSize.value);
        public static Int64 CreateArray(int arrayLength, int elementSize )
        {
            System.Array newa = new int[10];
            int totalBytes = arrayLength * elementSize;

            // 3. 分配非托管内存
            IntPtr unmanagedPtr = Marshal.AllocHGlobal((int)totalBytes);
            if (unmanagedPtr == IntPtr.Zero)
                throw new OutOfMemoryException("无法分配非托管内存");
            //// 4. 生成数组数据并填充到非托管内存
            //// 方式1：先创建托管数组，再复制（适合数据复杂时）
            //int[] managedData = new int[arrayLength];
            //for (int i = 0; i < arrayLength; i++)
            //{
            //    managedData[i] = (i + 1) * 2; // 生成偶数：2,4,6,...
            //}
            //Marshal.Copy(managedData, 0, unmanagedPtr, arrayLength);
            return unmanagedPtr.ToInt64();
        }
        public static void FreeArray( Int64 ptr )
        {
            IntPtr newptr = new IntPtr(ptr);
            Marshal.FreeHGlobal(newptr);
        }


        //public static void SetArrayValueThis(ArrayObject ao, Int32Object index, object value) => SetArrayValueThis(ao, index.value, value);
        public static void SetArrayValueThis(ArrayObject ao, int index, object value)
        {
            if (value is SObject sobj)
            {
                ao.StoreObject(index, sobj.value );
            }
            else
            {
                ao.StoreObject(index, value);
            }
        }
        public static void SetArrayValue( Int64 ptr, int type, int index, object value )
        {
            IntPtr newptr = new IntPtr(ptr);
            Mem.WritePointer(newptr, type, index, value);
        }
        public static object GetArrayValue(Int64 ptr, int type, int index)
        {
            IntPtr newptr = new IntPtr(ptr);
            return Mem.ReadPointer(newptr, type, index);
        }
        //public static object GetArrayValueThis(ArrayObject ao, Int32Object index ) => GetArrayValueThis(ao, index.value );
        public static object GetArrayValueThis(ArrayObject ao, int index)
        {
            var retobj =  ao.GetValue(index);
            if( retobj == null )
            {
                var retobj2 = new SObject(EVMType.Object);
                retobj2.SetNull();

                return retobj2;
            }
            return retobj;
        }
        public static void SetArrayValues( Int64 ptr, int type, int beginIndex, Int64 valPtr, int length )
        {
            IntPtr newptr = new IntPtr(ptr);
            IntPtr valueptr = new IntPtr(valPtr);
            Mem.WriteArraysByValuePointer(newptr, type, beginIndex, valueptr, length );
        }
    }
}
