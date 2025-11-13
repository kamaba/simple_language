//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************


using System;
using System.Runtime.InteropServices;

namespace SimpleLanguage.Lib
{
    public static class Array
    {
        public static Int32 CreateArray(int arrayLength, int elementSize )
        {
            //System.Array
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
            return unmanagedPtr.ToInt32();
        }
    }
}
