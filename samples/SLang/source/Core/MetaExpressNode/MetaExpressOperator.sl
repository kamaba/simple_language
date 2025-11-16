//****************************************************************************
//  File:      MetaExpressOperator.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;

namespace SimpleLanguage.Core
{
    public class ConvertType
    {
        public EType oriType;
        public EType targetType;
    }
    public sealed class MetaUnaryOpExpressNode : MetaExpressNode
    {
        private Token tokeType => m_TokeType;
        public ESingleOpSign opSign => m_OpSign;
        public MetaExpressNode value => m_Value;

        private ESingleOpSign m_OpSign = ESingleOpSign.None;
        private MetaExpressNode m_Value = null;             //左边值
        private Token m_TokeType = null;

        public MetaUnaryOpExpressNode(FileMetaSymbolTerm fme, MetaExpressNode _value )
        {
            m_Value = _value;
            m_TokeType = fme.token;
            if ( fme.symBolType == ETokenType.Minus )
            {
                m_OpSign = ESingleOpSign.Neg;
            }
            else if( fme.symBolType == ETokenType.Not )
            {
                m_OpSign = ESingleOpSign.Not;
            }
        }
        public void SetValue( MetaExpressNode _value )
        {
            m_Value = _value;
        }
        public override void Parse( AllowUseSettings auc)
        {
            m_Value.Parse(auc);
        }
        public override int CalcParseLevel(int level)
        {
            return m_Value.CalcParseLevel(level);
        }
        public override void CalcReturnType()
        {
            if ( m_OpSign == ESingleOpSign.Not)
            {
                m_MetaDefineType.SetMetaClass( CoreMetaClassManager.booleanMetaClass );
            }
            else
            {
                m_Value.CalcReturnType();
                m_MetaDefineType = m_Value.metaDefineType;
            }
        }
        public MetaExpressNode SimulateCompute()
        {
            var mcen = value as MetaConstExpressNode;
            if (mcen != null)
            {
                var eType = mcen.eType;
                switch (opSign)
                {
                    case ESingleOpSign.Neg:
                        {
                            switch (eType)
                            {
                                case EType.Int16:
                                    {
                                        mcen.value = -(short)mcen.value;
                                        return mcen;
                                    }
                                case EType.UInt16:
                                    {
                                        mcen.value = -(ushort)mcen.value;
                                        return mcen;
                                    }
                                case EType.Int32:
                                    {
                                        mcen.value = -(int)mcen.value;
                                        return mcen;
                                    }
                                case EType.UInt32:
                                    {
                                        mcen.value = -(uint)mcen.value;
                                        return mcen;
                                    }
                                case EType.Int64:
                                    {
                                        mcen.value = -(long)mcen.value;
                                        return mcen;
                                    }

                            }
                        }
                        break;
                    case ESingleOpSign.Not:
                        {
                            switch (eType)
                            {
                                case EType.Byte:
                                    {
                                        mcen.value = (byte)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.Int16:
                                    {
                                        mcen.value = (short)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.UInt16:
                                    {
                                        mcen.value = (ushort)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.Int32:
                                    {
                                        mcen.value = (int)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.UInt32:
                                    {
                                        mcen.value = (uint)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.Int64:
                                    {
                                        mcen.value = (long)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.UInt64:
                                    {
                                        mcen.value = (long)mcen.value != 0;
                                        return mcen;
                                    }
                                case EType.String:
                                    {
                                        mcen.value = String.IsNullOrEmpty(mcen.value as string);
                                        return mcen;
                                    }

                            }
                        }
                        break;
                    case ESingleOpSign.Xor:
                        {
                            switch (eType)
                            {
                                case EType.Byte:
                                    {
                                        mcen.value = ^(byte)mcen.value;
                                        return mcen;
                                    }
                                case EType.SByte:
                                    {
                                        mcen.value = ^(sbyte)mcen.value;
                                        return mcen;
                                    }
                                case EType.Int16:
                                    {
                                        mcen.value = ^(short)mcen.value;
                                        return mcen;
                                    }
                                case EType.UInt16:
                                    {
                                        mcen.value = ^(ushort)mcen.value;
                                        return mcen;
                                    }
                                case EType.Int32:
                                    {
                                        mcen.value = ^(int)mcen.value;
                                        return mcen;
                                    }

                            }
                        }
                        break;
                }
            }
            return this;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(tokeType.lexeme.ToString());
            sb.Append(m_Value.ToFormatString());

            return sb.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            if (tokeType != null)
            {
                sb.Append(tokeType.ToLexemeAllString());
            }
            if(m_Value != null )
            {
                sb.Append(m_Value.ToTokenString());
            }
            return sb.ToString();
        }
    }
    public sealed class MetaOpExpressNode : MetaExpressNode
    {
        public bool isEqualType { get; set; } = false;
        public MetaExpressNode left => m_Left;
        public MetaExpressNode right => m_Right;
        public ELeftRightOpSign opSign => m_OpLevelSign;
        public ConvertType leftConvert => m_LeftConvert;
        public ConvertType rightConvert => m_RightConvert;

        private MetaExpressNode m_Left = null;
        private MetaExpressNode m_Right = null;
        private ConvertType m_LeftConvert = null;
        private ConvertType m_RightConvert = null;
        private ELeftRightOpSign m_OpLevelSign;
        private Token m_SignToken = null;

        private FileMetaSymbolTerm m_FileMetaBaseTerm = null;
        public MetaOpExpressNode(FileMetaSymbolTerm fme, MetaType mt, MetaExpressNode _left, MetaExpressNode _right )
        {
            m_Left = _left;
            m_Right = _right;
            m_FileMetaBaseTerm = fme;
            m_MetaDefineType = mt;

            ETokenType ett = fme.token.type;
            m_SignToken = fme.token;
            switch (ett)
            {
                case ETokenType.Plus:
                    m_OpLevelSign = ELeftRightOpSign.Add;
                    break;
                case ETokenType.Minus:
                    m_OpLevelSign = ELeftRightOpSign.Minus;
                    break;
                case ETokenType.Multiply:
                    m_OpLevelSign = ELeftRightOpSign.Multiply;
                    break;
                case ETokenType.Divide:
                    m_OpLevelSign = ELeftRightOpSign.Divide;
                    break;
                case ETokenType.Modulo:
                    m_OpLevelSign = ELeftRightOpSign.Modulo;
                    break;
                case ETokenType.GreaterOrEqual:
                    m_OpLevelSign = ELeftRightOpSign.GreaterOrEqual;
                    break;
                case ETokenType.Greater:
                    m_OpLevelSign = ELeftRightOpSign.Greater;
                    break;
                case ETokenType.LessOrEqual:
                    m_OpLevelSign = ELeftRightOpSign.LessOrEqual;
                    break;
                case ETokenType.Less:
                    m_OpLevelSign = ELeftRightOpSign.Less;
                    break;
                case ETokenType.Equal:
                    m_OpLevelSign = ELeftRightOpSign.Equal;
                    break;
                case ETokenType.NotEqual:
                    m_OpLevelSign = ELeftRightOpSign.NotEqual;
                    break;
                case ETokenType.And:
                    m_OpLevelSign = ELeftRightOpSign.And;
                    break;
                case ETokenType.Or:
                    m_OpLevelSign = ELeftRightOpSign.Or;
                    break;
                case ETokenType.As:
                    m_OpLevelSign = ELeftRightOpSign.Cast;
                    break;
                case ETokenType.Is:
                    m_OpLevelSign = ELeftRightOpSign.IsType;
                    break;
                default:
                    {
                        Debug.Write("Error 没有适合的符号!!!" + ett.ToString());
                    }
                    break;
            }
            ComputeIsComputeType();
        }
        public MetaOpExpressNode(MetaExpressNode _left, MetaExpressNode _right, ELeftRightOpSign _opSign)
        {
            m_Left = _left;
            m_Right = _right;
            m_OpLevelSign = _opSign;
            ComputeIsComputeType();
        }
        public void ComputeIsComputeType()
        {
            if (m_OpLevelSign == ELeftRightOpSign.Equal
                || m_OpLevelSign == ELeftRightOpSign.NotEqual
                || m_OpLevelSign == ELeftRightOpSign.Greater
                || m_OpLevelSign == ELeftRightOpSign.GreaterOrEqual
                || m_OpLevelSign == ELeftRightOpSign.Less
                || m_OpLevelSign == ELeftRightOpSign.LessOrEqual
                || m_OpLevelSign == ELeftRightOpSign.And
                || m_OpLevelSign == ELeftRightOpSign.Or
                || m_OpLevelSign == ELeftRightOpSign.IsType
                    )
                isEqualType = true;
            else
                isEqualType = false;
        }
        public void SetLeft(MetaExpressNode _left)
        {
            m_Left = _left;
        }
        public void SetRight(MetaExpressNode _right)
        {
            m_Right = _right;
        }
        public override void Parse(AllowUseSettings auc)
        {
            m_Left.Parse(auc);
            m_Right.Parse(auc);
        }
        public override int CalcParseLevel(int level)
        {
            int level1 = m_Left.CalcParseLevel(level);
            int level2 = m_Right.CalcParseLevel(level1);

            return level2;
        }
        public override void CalcReturnType()
        {
            if (m_Left != null)
            {
                m_Left.CalcReturnType();
            }
            if (m_Right != null)
            {
                m_Right.CalcReturnType();
            }
            ParseCompute();

            GetReturnMetaDefineType();
        }
        public void ParseCompute()
        {
            if (m_Left != null && m_Right != null)
            {
                switch (m_OpLevelSign)
                {
                    case ELeftRightOpSign.Add:
                    case ELeftRightOpSign.Minus:
                        {
                            ParseAddOrMinus();
                        }
                        break;
                    case ELeftRightOpSign.Multiply:
                        {
                            ParseMultiplyOrModulo();
                        }
                        break;
                    case ELeftRightOpSign.Divide:
                        {
                            ParseDivide();
                        }
                        break;
                    case ELeftRightOpSign.Modulo:
                        {
                            ParseMultiplyOrModulo();
                        }
                        break;
                    case ELeftRightOpSign.Cast:
                        {
                            ParseCast();
                        }
                        break;
                    case ELeftRightOpSign.Equal:
                    case ELeftRightOpSign.NotEqual:
                    case ELeftRightOpSign.Greater:
                    case ELeftRightOpSign.GreaterOrEqual:
                    case ELeftRightOpSign.Less:
                    case ELeftRightOpSign.LessOrEqual:
                    case ELeftRightOpSign.Or:
                    case ELeftRightOpSign.And:
                    case ELeftRightOpSign.IsType:
                        {
                            if(m_MetaDefineType != null )
                            {
                                m_MetaDefineType.SetMetaClass(CoreMetaClassManager.booleanMetaClass);
                            }
                            else
                            {
                                m_MetaDefineType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                            }
                        }
                        break;
                }
            }
            else
            {
                Debug.Write("Error 错误，左右值不对!!");
            }
        }
        public void ParseAddOrMinus()
        {
            if (m_Left.opLevel < m_Right.opLevel)
            {
                m_MetaDefineType = m_Right.metaDefineType;
                //m_Left.metaDefineType.SetMetaClass(m_MetaDefineType.metaClass);
                //m_Left.CalcReturnType();

                if( m_Right.opLevel < 10 )
                {
                    m_LeftConvert = new ConvertType()
                    {
                        oriType = m_Left.metaDefineType.metaClass.eType,
                        targetType = m_MetaDefineType.metaClass.eType
                    };
                }
            }
            else if (m_Left.opLevel > m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
                //MetaType newmt = new MetaType(m_MetaDefineType.metaClass);
                //m_Right.SetMetaType(newmt);
                //m_Right.CalcReturnType();
                if( m_Left.opLevel < 10 )
                {
                    m_RightConvert = new ConvertType()
                    {
                        oriType = m_Right.metaDefineType.metaClass.eType,
                        targetType = m_MetaDefineType.metaClass.eType
                    };
                }
            }
            else
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
        }
        public void ParseDivide()
        {
            if (m_Left.opLevel < m_Right.opLevel)
            {
                m_MetaDefineType = m_Right.metaDefineType;
            }
            else if (m_Left.opLevel > m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
            else
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
        }
        public void ParseMultiplyOrModulo()
        {
            if (m_Left.opLevel < m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
            else if (m_Left.opLevel > m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
            else
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
        }
        public void ParseCast()
        {
            if (m_Left.opLevel < m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
            else if (m_Left.opLevel > m_Right.opLevel)
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
            else
            {
                m_MetaDefineType = m_Left.metaDefineType;
            }
        }
        public MetaExpressNode SimulateCompute(ExpressOptimizeConfig config)
        {
            if (config.greaterOrEqualConvertGeraterAndEqual && m_OpLevelSign == ELeftRightOpSign.GreaterOrEqual)
            {
                var constLeft = m_Left as MetaConstExpressNode;
                var constRight = m_Right as MetaConstExpressNode;
                if (constLeft == null || constRight == null)
                {
                    MetaExpressNode left1 = m_Left;
                    MetaExpressNode right1 = m_Right;
                    MetaExpressNode left2 = m_Left;
                    MetaExpressNode right2 = m_Right;
                    m_Left = new MetaOpExpressNode(left1, right1, ELeftRightOpSign.Greater);
                    m_Right = new MetaOpExpressNode(left1, right1, ELeftRightOpSign.Equal);
                    m_OpLevelSign = ELeftRightOpSign.Or;
                }
            }
            if (config.lessOrEqualConvertLessAndEqual && m_OpLevelSign == ELeftRightOpSign.LessOrEqual)
            {
                var constLeft = m_Left as MetaConstExpressNode;
                var constRight = m_Right as MetaConstExpressNode;
                if (constLeft == null || constRight == null)
                {
                    MetaExpressNode left1 = m_Left;
                    MetaExpressNode right1 = m_Right;
                    MetaExpressNode left2 = m_Left;
                    MetaExpressNode right2 = m_Right;
                    m_Left = new MetaOpExpressNode(left1, right1, ELeftRightOpSign.Less);
                    m_Right = new MetaOpExpressNode(left1, right1, ELeftRightOpSign.Equal);
                    m_OpLevelSign = ELeftRightOpSign.Or;
                }
            }
            if (config.ifLeftAndRightIsConstThenCompute)
            {
                var constLeft = m_Left as MetaConstExpressNode;
                var constRight = m_Right as MetaConstExpressNode;
                if (constLeft != null && constRight != null )
                {
                    switch (m_OpLevelSign)
                    {
                        case ELeftRightOpSign.Add:
                            {
                                constLeft = constLeft + constRight;
                                return constLeft;
                            }
                        case ELeftRightOpSign.Minus:
                            {
                                constLeft = constLeft - constRight;
                                return constLeft;
                            }
                        case ELeftRightOpSign.Multiply:
                            {
                                constLeft = constLeft * constRight;
                                return constLeft;
                            }
                        case ELeftRightOpSign.Divide:
                            {
                                constLeft = constLeft / constRight;
                                return constLeft;
                            }
                        case ELeftRightOpSign.Modulo:
                            {
                                constLeft = constLeft % constRight;
                                return constLeft;
                            }
                        case ELeftRightOpSign.Equal:
                        case ELeftRightOpSign.NotEqual:
                        case ELeftRightOpSign.Greater:
                        case ELeftRightOpSign.GreaterOrEqual:
                        case ELeftRightOpSign.Less:
                        case ELeftRightOpSign.LessOrEqual:
                        case ELeftRightOpSign.And:
                        case ELeftRightOpSign.Or:
                            {
                                constLeft.ComputeEqualComputeRight(constRight, m_OpLevelSign);
                                return constLeft;
                            }
                    }
                }
            }
            return this;
        }
        public static string GetSignString(ELeftRightOpSign opSign)
        {
            switch (opSign)
            {
                case ELeftRightOpSign.Add: { return "+"; }
                case ELeftRightOpSign.Minus: { return "-"; }
                case ELeftRightOpSign.Multiply: { return "*"; }
                case ELeftRightOpSign.Divide: { return "/"; }
                case ELeftRightOpSign.Modulo: { return "%"; }
                case ELeftRightOpSign.Shi: { return ">>"; }
                case ELeftRightOpSign.Shr: { return "<<"; }

                case ELeftRightOpSign.Equal: { return "=="; }
                case ELeftRightOpSign.NotEqual: { return "!="; }
                case ELeftRightOpSign.Greater: { return ">"; }
                case ELeftRightOpSign.GreaterOrEqual: { return ">="; }
                case ELeftRightOpSign.Less: { return "<"; }
                case ELeftRightOpSign.LessOrEqual: { return "<="; }
                case ELeftRightOpSign.Or: { return "||"; }
                case ELeftRightOpSign.And: { return "&&"; }
            }
            return "NoSign";
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(" + m_Left.ToFormatString());
            sb.Append(" " + GetSignString(m_OpLevelSign));
            sb.Append(" " + m_Right.ToFormatString() + ")");

            return sb.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_Left?.ToTokenString());
            if (m_SignToken != null)
            {
                sb.Append(m_SignToken.lexeme?.ToString());
            }
            else
            {
                sb.Append(GetSignString(m_OpLevelSign));
            }
            sb.Append(m_Right?.ToTokenString());
            return sb.ToString();
        }
    }
}
