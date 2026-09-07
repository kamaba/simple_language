//****************************************************************************
//  File:      BuiltinMemberFunctionRegistry.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/7 12:00:00
//  Description: 系统内部定义方法(魔法方法)注册表与签名约束规则
//****************************************************************************

using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    /// <summary>内置方法参数类型检查模式</summary>
    public enum EBuiltinParamCheckMode
    {
        /// <summary>不检查参数类型(如索引器的key可以是int/string/泛型等任意类型)</summary>
        Any,
        /// <summary>每个参数都必须是object类型(数值/比较类内置方法约定)</summary>
        Object,
    }
    /// <summary>内置方法返回类型检查模式</summary>
    public enum EBuiltinReturnCheckMode
    {
        /// <summary>不检查返回类型(如索引器_getItem_)</summary>
        Any,
        /// <summary>返回当前声明类的类型(数值操作类)</summary>
        CurrentClass,
        /// <summary>返回bool(比较操作类)</summary>
        Boolean,
        /// <summary>返回void(写入类操作)</summary>
        Void,
    }

    /// <summary>
    /// 单个系统内部方法的签名约束规则
    /// </summary>
    public class BuiltinMemberFunctionRule
    {
        public string name => m_Name;
        public int paramCount => m_ParamCount;
        public EBuiltinParamCheckMode paramCheckMode => m_ParamCheckMode;
        public EBuiltinReturnCheckMode returnCheckMode => m_ReturnCheckMode;

        private string m_Name;
        private int m_ParamCount;
        private EBuiltinParamCheckMode m_ParamCheckMode;
        private EBuiltinReturnCheckMode m_ReturnCheckMode;

        public BuiltinMemberFunctionRule(string name, int paramCount,
            EBuiltinParamCheckMode pcm, EBuiltinReturnCheckMode rcm)
        {
            m_Name = name;
            m_ParamCount = paramCount;
            m_ParamCheckMode = pcm;
            m_ReturnCheckMode = rcm;
        }
    }

    /// <summary>
    /// 系统内部定义方法(魔法方法)注册表
    /// FrontEnd层在类中遇到注册表内的方法名时执行约束检查:
    ///  1. 不允许static声明(内置方法约定为实例方法)
    ///  2. 必须携带override标记(允许final)
    ///  3. 按规则校验参数个数与类型
    ///  4. 按规则校验返回类型
    ///  5. 父链中存在同名final方法时, 不允许再定义该内置方法(编译报错)
    /// 注意: _init_ 虽然也是系统内部方法, 但它由Object真实声明并传递下来,
    ///       走普通类的override/final检查流程, 不在此注册
    /// </summary>
    public static class BuiltinMemberFunctionRegistry
    {
        private static readonly Dictionary<string, BuiltinMemberFunctionRule> s_RuleDict =
            new Dictionary<string, BuiltinMemberFunctionRule>();

        static BuiltinMemberFunctionRegistry()
        {
            // 算术运算: 恰好1个object参数, 返回当前类类型
            AddRule(new BuiltinMemberFunctionRule("_add_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_sub_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_mul_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_truediv_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_mod_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));

            // 复合赋值运算: 恰好1个object参数, 返回当前类类型
            AddRule(new BuiltinMemberFunctionRule("_iadd_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_imul_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_itruediv_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));

            // 比较运算: 恰好1个object参数, 返回bool
            AddRule(new BuiltinMemberFunctionRule("_lt_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));
            AddRule(new BuiltinMemberFunctionRule("_le_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));
            AddRule(new BuiltinMemberFunctionRule("_gt_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));
            AddRule(new BuiltinMemberFunctionRule("_ge_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));
            AddRule(new BuiltinMemberFunctionRule("_eq_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));
            AddRule(new BuiltinMemberFunctionRule("_ne_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.Boolean));

            // 逻辑运算: 恰好1个object参数, 返回当前类类型
            AddRule(new BuiltinMemberFunctionRule("_and_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_or_", 1, EBuiltinParamCheckMode.Object, EBuiltinReturnCheckMode.CurrentClass));

            // 索引器: 参数类型任意(key可以是int/string/泛型等), _getItem_返回任意, _setItem_返回void
            AddRule(new BuiltinMemberFunctionRule("_getItem_", 1, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.Any));
            AddRule(new BuiltinMemberFunctionRule("_setItem_", 2, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.Void));

            // 资源管理(预留): 0个参数, _enter_返回当前类类型, _exit_返回void
            // 同时收录 _enter_/_exit_ 与 __enter_/__exit_ 两种写法
            AddRule(new BuiltinMemberFunctionRule("_enter_", 0, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("_exit_", 0, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.Void));
            AddRule(new BuiltinMemberFunctionRule("__enter_", 0, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.CurrentClass));
            AddRule(new BuiltinMemberFunctionRule("__exit_", 0, EBuiltinParamCheckMode.Any, EBuiltinReturnCheckMode.Void));
        }

        private static void AddRule(BuiltinMemberFunctionRule rule)
        {
            s_RuleDict[rule.name] = rule;
        }

        public static bool Contains(string name)
        {
            return name != null && s_RuleDict.ContainsKey(name);
        }

        public static BuiltinMemberFunctionRule GetRule(string name)
        {
            if (name == null)
            {
                return null;
            }
            s_RuleDict.TryGetValue(name, out var rule);
            return rule;
        }

        /// <summary>
        /// 双目运算符 -> 内置operator方法名 映射。
        /// 返回 null 表示该运算符没有对应的内置方法(如位运算)。
        /// 用于表达式层把 left op right 解析为 left._op_(right) 的方法调用。
        /// </summary>
        public static string GetBuiltinOperatorMethodName(ELeftRightOpSign opSign)
        {
            switch (opSign)
            {
                case ELeftRightOpSign.Add: return "_add_";
                case ELeftRightOpSign.Minus: return "_sub_";
                case ELeftRightOpSign.Multiply: return "_mul_";
                case ELeftRightOpSign.Divide: return "_truediv_";
                case ELeftRightOpSign.Modulo: return "_mod_";
                case ELeftRightOpSign.Equal: return "_eq_";
                case ELeftRightOpSign.NotEqual: return "_ne_";
                case ELeftRightOpSign.Greater: return "_gt_";
                case ELeftRightOpSign.GreaterOrEqual: return "_ge_";
                case ELeftRightOpSign.Less: return "_lt_";
                case ELeftRightOpSign.LessOrEqual: return "_le_";
                case ELeftRightOpSign.And: return "_and_";
                case ELeftRightOpSign.Or: return "_or_";
                default: return null;
            }
        }
    }
}
