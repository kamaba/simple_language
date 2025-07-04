//****************************************************************************
//  File:      ClassHelp.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta class is help
//****************************************************************************

namespace SimpleLanguage.Core
{
    public static class ClassHelp
    {
        public static MetaClass GetTreeStructNode( this MetaClass mc )
        {
            if (mc == null) return null;
            
            //if( mc.isTemplateClass) { return mc.templateParentClass; }
            //else if( mc is MetaGenTemplateClass mgtc )
            //{
            //    return mgtc.metaTemplateClass.templateParentClass;
            //}
            return mc;
        }
    }
}
