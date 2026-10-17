//****************************************************************************
//  File:      TypeRelation.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/5/19
//  Description: MetaType 赋�?/ as-is / 参数匹配的统一关系枚举与辅助扩�?
//****************************************************************************

using System;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 两个 <see cref="MetaType"/> 之间的结�?继承/赋值关系（涵盖 class、data、enum）�?
    /// </summary>
    public enum ETypeRelation
    {
        None = 0,
        /// <summary>左�?/ 目标 <see cref="MetaType"/> 无效或未识别�?/summary>
        TargetTypeError = 1,
        /// <summary>右�?/ 表达�?<see cref="MetaType"/> 无效或未识别�?/summary>
        ExpressTypeError = 2,
        /// <summary>data / enum / class 种类不一致，无法建立关系�?/summary>
        KindMismatch = 3,
        No = 4,
        Same = 5,
        /// <summary>表达式类型为目标的子类型（向上赋值需强转）�?/summary>
        Child = 6,
        /// <summary>表达式类型为目标父类型（向下赋值，通常不安全）�?/summary>
        Parent = 7,
        Similar = 8,
        Interface = 9,
        Num = 10,
        SameClassNotSameInputTemplate = 11,
        SameClassAndSameInputTemplate = 12,
    }

    [Flags]
    public enum ETypeRelationResolveFlags
    {
        None = 0,
        UseTemplateExactMatch = 1,
        /// <summary>允许 Core.Enum 存储视图下的 enum 成员表达式�?/summary>
        AllowEnumStorageMember = 2,
    }

    public static class ETypeRelationExtensions
    {
        public static bool IsError(this ETypeRelation relation)
        {
            return relation == ETypeRelation.TargetTypeError
                || relation == ETypeRelation.ExpressTypeError
                || relation == ETypeRelation.KindMismatch;
        }

        public static bool IsStrictMismatch(this ETypeRelation relation)
        {
            return relation == ETypeRelation.No || relation.IsError();
        }

        /// <summary>赋值语境下可直接接受或经窄化强转后可接受�?/summary>
        public static bool IsAssignableForAssign(this ETypeRelation relation)
        {
            return relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Interface
                || relation == ETypeRelation.Num
                || relation == ETypeRelation.Similar;
        }

        /// <summary>as / is 语境下允许探测到的关系�?/summary>
        public static bool IsAcceptableForAsIs(this ETypeRelation relation)
        {
            return relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Parent
                || relation == ETypeRelation.Interface
                || relation == ETypeRelation.Num;
        }

        public static bool IsInheritanceLike(this ETypeRelation relation)
        {
            return relation == ETypeRelation.Child
                || relation == ETypeRelation.Parent
                || relation == ETypeRelation.Interface;
        }
    }
}
