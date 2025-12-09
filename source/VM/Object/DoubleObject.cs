//****************************************************************************
//  File:      DoubleObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using System;
namespace SimpleLanguage.VM
{
    class DoubleObject : SObject
    {
        public Double value;
        public DoubleObject() : base(EType.Float64)
        { }
        public void SetValue(Double _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
