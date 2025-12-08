//****************************************************************************
//  File:      BoolObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.VM
{
    public class BoolObject : SObject
    {
        public bool value { get; protected set; } = false;

        public BoolObject( bool flag ) : base(EType.Boolean )
        {
            value = flag;
        }

        public void SetValue(bool _val )
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
