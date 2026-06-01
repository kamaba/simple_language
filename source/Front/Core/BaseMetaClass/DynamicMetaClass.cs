//****************************************************************************
//  File:      DynamicMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class DynamicMetaClass : MetaClass
    {
        public DynamicMetaClass():base(DefaultObject.Dynamic.ToString())
        {
            m_InnderDefine = true;
        }     
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new DynamicMetaClass();
            return mc;
        }
    }
}
