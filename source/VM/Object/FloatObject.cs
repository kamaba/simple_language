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
    public class Float32Object : NumObject
    {
        public new float value => m_Numeric.f32;
        public Float32Object(float _val) : base(EVMType.Float32)
        {
            m_Numeric.f32 = _val;
            m_RuntimeType = RuntimeTypeManager.float32RuntimeType;
        }
    }
    public class Float64Object : NumObject
    {
        public new double value => m_Numeric.f64;

        public Float64Object(double _val) : base(EVMType.Float64)
        {
            m_Numeric.f64 = _val;
            m_RuntimeType = RuntimeTypeManager.float64RuntimeType;
        }
    }
}
