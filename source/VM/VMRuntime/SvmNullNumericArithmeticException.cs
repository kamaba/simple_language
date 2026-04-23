//****************************************************************************
//  Thrown when VM binary arithmetic (ComputeValueInline) mixes null with a numeric.
//  LID.VMOperatorNotShouldHaveNull 在 CompareEuqalSValue1AndValue2 / SValueCompute 中先记录再抛出；Run 的 catch 不再打该条。
//****************************************************************************

using System;

namespace SimpleLanguage.VM
{
    public sealed class SvmNullNumericArithmeticException : InvalidOperationException
    {
        /// <summary>Operator symbol for {0} in the log template (e.g. +, -, *)</summary>
        public string OperatorDisplay { get; }

        /// <summary>Which operand carried null, for {1} (e.g. 左操作数 / 右操作数)</summary>
        public string NullOperandPosition { get; }

        public SvmNullNumericArithmeticException(string operatorDisplay, string nullOperandPosition)
            : base($"操作符 {operatorDisplay} 运算中，{nullOperandPosition} 为 null 且另一侧为数字")
        {
            OperatorDisplay = operatorDisplay;
            NullOperandPosition = nullOperandPosition;
        }
    }
}
