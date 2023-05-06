//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.Core.MetaObjects
{
    public class MetaArrayObject : MetaObject
    {
        public Array m_Array; 
        public MetaArrayObject() 
        {

        }
        public int count()
        {
            return m_Array?.Length ?? 0;
        }
    }
}
