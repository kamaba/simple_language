//****************************************************************************
//  File:      Float32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using System;
namespace SimpleLanguage.VM
{
    public class Float32Object : SObject
    {
        public Single value;
        public Float32Object() : base(EType.Float32)
        { }
        public void SetValue(Single _val)
        {
            value = _val;
        }
        public Int32 ToInt()
        {
            return Int32.Parse( value.ToString() );
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
