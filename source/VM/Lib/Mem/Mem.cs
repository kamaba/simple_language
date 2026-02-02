//****************************************************************************
//  File:      Mem.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: memral manager
//****************************************************************************

using System.Runtime.InteropServices;

namespace SimpleLanguage.Lib
{
    public static class Mem
    {
        public unsafe static void WriteArraysByValuePointer(IntPtr newptr, int type, int index, IntPtr src, int length )
        {
            switch (type)
            {
                case 0:/*void*/
                    {

                    }
                    break;
                case 1:/*byte*/
                case 2:/*sbyte*/
                    {

                    }
                    break;
                case 3:/*int16*/
                    {

                    }
                    break;
                case 4:/*uint16*/
                    {

                    }
                    break;
                case 5:/*int32*/
                case 11:/*uint32*/
                    {
                        IntPtr offsetPtr = newptr + sizeof(Int32) * index;
                        Buffer.MemoryCopy(offsetPtr.ToPointer(), src.ToPointer(), length, length);

                    }
                    break;
                case 9:/*string*/
                    {

                    }
                    break;
            }
        }
        public static void WritePointer(IntPtr newptr, int type, int index, object value)
        {
            switch (type)
            {
                case 0:/*void*/
                    {

                    }
                    break;
                case 1:/*byte*/
                case 2:/*sbyte*/
                    {

                    }
                    break;
                case 3:/*int16*/
                    {

                    }
                    break;
                case 4:/*uint16*/
                    {

                    }
                    break;
                case 5:/*int32*/
                case 6:/*uint32*/
                    {
                        IntPtr offsetPtr = newptr + sizeof(Int32) * index;
                        Marshal.WriteInt32(offsetPtr, (Int32)value);

                    }
                    break;
                case 9:/*string*/
                    {

                    }
                    break;
            }
        }
        public static object ReadPointer(IntPtr newptr, int type, int index )
        {
            switch (type)
            {
                case 0:/*void*/
                    {

                    }
                    break;
                case 1:/*byte*/
                case 2:/*sbyte*/
                    {

                    }
                    break;
                case 3:/*int16*/
                    {

                    }
                    break;
                case 4:/*uint16*/
                    {

                    }
                    break;
                case 5:/*int32*/
                case 6:/*uint32*/
                    {
                        IntPtr offsetPtr = newptr + sizeof(Int32) * index;
                        return Marshal.ReadInt32(offsetPtr);

                    }
                case 9:/*string*/
                    {

                    }
                    break;
            }
            return null;
        }
    }
}
