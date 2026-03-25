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
        public new float value
        {
            get
            {
                return (float)m_Value;
            }
        }
        public Float32Object( Single _val ) : base(EVMType.Float32)
        {
            m_Value = _val;
            m_RuntimeType = RuntimeTypeManager.float32RuntimeType;
        }
    }
    public class Float64Object : NumObject
    {
        public new double value
        {
            get
            {
                return (double)m_Value;
            }
        }
        public Float64Object(Double _val) : base(EVMType.Float64)
        {
            m_Value = _val;
            m_RuntimeType = RuntimeTypeManager.float64runtimeType;
        }
    }
}
