//****************************************************************************
//  File:      FunctionMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/26 12:00:00
//  Description: Function 内建类型, 用于闭包变量的类型标注
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class FunctionMetaClass : MetaClass
    {
        public FunctionMetaClass() : base( DefaultObject.Function.ToString() )
        {
            SetExtendClass( CoreMetaClassManager.objectMetaClass );
            m_Type = EType.Function;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new FunctionMetaClass();
            return mc;
        }
    }
}
