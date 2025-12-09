//****************************************************************************
//  File:      Float32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System;
namespace SimpleLanguage.VM
{
    public class Float32Object : SObject
    {
        public Single value;
        public Float32Object( Single _val ) : base(EVMType.Float32)
        {
            value = _val;
        }
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

    class Float64Object : SObject
    {
        public Double value;
        public Float64Object(Double _val) : base(EVMType.Float64)
        {
            value = _val;
        }
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
