//****************************************************************************
//  File:      InlineLambdaMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/16 12:00:00
//  Description: InlineLambda 伪类型, 仅用于内联 lambda 变量的内部标记;
//               用户不可显式标注, 与 Function 类型不兼容( 跨边界误用由类型系统拦截 )
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class InlineLambdaMetaClass : MetaClass
    {
        public InlineLambdaMetaClass() : base( DefaultObject.InlineLambda.ToString() )
        {
            SetExtendClass( CoreMetaClassManager.objectMetaClass );
            m_Type = EType.InlineLambda;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new InlineLambdaMetaClass();
            return mc;
        }
    }
}
