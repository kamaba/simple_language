//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.Core.MetaObjects
{
    public class MetaListObject : MetaObject
    {
        public Array m_Array; 
        public MetaListObject() 
        {

        }
        public int count()
        {
            return m_Array?.Length ?? 0;
        }
    }

    public class MetaList : MetaObject
    {
        public static string MetaToString(Int32 v)
        {
            return v.ToString();
        }

        public static int SetListCount( Int32 v )
        {
            IntPtr ptr = Marshal.AllocHGlobal(v * Marshal.SizeOf(typeof(int)));
            if( ptr != IntPtr.Zero )
            {
                return ptr.ToInt32();
            }
            return -1;
        }
    }
}
