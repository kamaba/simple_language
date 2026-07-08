//****************************************************************************
//  File:      WrapperModule.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Wrapper
{
    public class WrapperDefineType
    {
        public WrapperDefineType() { }
    }
    public class WrapperVariable
    {
        public int id { get; set; }
        public string name { get; set; }
        public int index { get; set; }
        public int from { get; set; }
        public string irMetaType { get; set; }
    }
}
