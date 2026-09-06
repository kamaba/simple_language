//****************************************************************************
//  File:      MetaExpressOperator.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;


namespace SimpleLanguage.Core
{
    public class ConvertType
    {
        public EType oriType;
        public EType targetType;
    }
    public sealed class MetaUnaryOpExpressNode : MetaExpressNodeBase
    {
        public override Token token => m_Token;
        public ESingleOpSign opSign => m_OpSign;
        public MetaExpressNodeBase value => m_Value;

        private ESingleOpSign m_OpSign = ESingleOpSign.None;
        private MetaExpressNodeBase m_Value = null;             //左边值


        public MetaUnaryOpExpressNode(FileMetaSymbolTerm fme, MetaExpressNodeBase _value )
        {
            m_Value = _value;
            m_Token = fme.token;
            if ( fme.symBolType == ETokenType.Minus )
            {
                m_OpSign = ESingleOpSign.Neg;
            }
            else if( fme.symBolType == ETokenType.Not )
            {
                m_OpSign = ESingleOpSign.Not;
            }
            else if (fme.symBolType == ETokenType.Negative)
            {
                // '~' bitwise complement
                m_OpSign = ESingleOpSign.Xor;
            }
        }
        public void SetValue( MetaExpressNodeBase _value )
        {
            m_Value = _value;
        }
        public override void Parse( AllowUseSettings auc)
        {
            m_Value.Parse(auc);
            m_ParsedState = m_Value.parseSuccessed ? EParseState.ParseSuccess : EParseState.ParsedFailed;
        }
        //public override int CalcParseLevel(int level)
        //{
        //    return m_Value.CalcParseLevel(level);
        //}
        public override void CalcReturnType()
        {
            if (m_Value != null)
            {
                m_Value.CalcReturnType();
            }

            if ( m_OpSign == ESingleOpSign.Not)
            {
                m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
            }
            else
            {
                m_Value.CalcReturnType();
                m_ExpressReturnMetaType = m_Value.expressReturnMetaType;
            }
        }
        public MetaExpressNodeBase SimulateCompute()
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
                                case EType.UInt8:
                                    {
                                        mcen.value = -(byte)mcen.value;
                                        return mcen;
                                    }
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
                                case EType.Float8:
                                case EType.Float8_E5M2:
                                case EType.Float16:
                                case EType.Float16_Brain:
                                    {
                                        // 低精度浮点存储为位模式：先解码取负再重新编码
                                        var v = Float816Convert.BitsToDoubleByEType(eType, mcen.value);
                                        mcen.value = Float816Convert.ToBitsByEType(eType, -v);
                                        return mcen;
                                    }

                            }
                        }
                        break;
                    case ESingleOpSign.Not:
                        {
                            switch (eType)
                            {
                                case EType.Boolean:
                                    {
                                        mcen.value = !(bool)mcen.value;
                                        return mcen;
                                    }
                                case EType.UInt8:
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
                                        mcen.value = (ulong)mcen.value != 0;
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
                                case EType.UInt8:
                                    {
                                        mcen.value = ^(byte)mcen.value;
                                        return mcen;
                                    }
                                case EType.Int8:
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
            sb.Append(m_Token.lexeme.ToString());
            sb.Append(m_Value.ToFormatString());

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_Token != null)
            {
                sb.Append(m_Token.ToLexemeAllString());
            }
            if(m_Value != null )
            {
                sb.Append(m_Value.ToString());
            }
            return sb.ToString();
        }
    }

    public sealed class MetaOpExpressNode : MetaExpressNodeBase
    {
        public bool isEqualType { get; set; } = false;
        public MetaExpressNodeBase left => m_Left;
        public MetaExpressNodeBase right => m_Right;
        public ELeftRightOpSign opSign => m_OpLevelSign;
        public ConvertType leftConvert => m_LeftConvert;
        public ConvertType rightConvert => m_RightConvert;

        private MetaExpressNodeBase m_Left = null;
        private MetaExpressNodeBase m_Right = null;
        private ConvertType m_LeftConvert = null;
        private ConvertType m_RightConvert = null;
        private ELeftRightOpSign m_OpLevelSign;
        private Token m_SignToken = null;
        public override Token token => m_SignToken;
        private MetaType m_DefineMetaType = null;
        private MetaType m_RealMetaType = null;

        private FileMetaSymbolTerm m_FileMetaBaseTerm = null;
        public MetaOpExpressNode(FileMetaSymbolTerm fme, MetaType mt, MetaExpressNodeBase _left, MetaExpressNodeBase _right )
        {
            m_FileMetaBaseTerm = fme;

            m_Left = _left;
            m_Right = _right;

            m_DefineMetaType = mt;

            ETokenType ett = fme.token.type;
            m_SignToken = fme.token;
            m_Token = m_SignToken;
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
                case ETokenType.Combine:
                    m_OpLevelSign = ELeftRightOpSign.Combine;
                    break;
                case ETokenType.InclusiveOr:
                    m_OpLevelSign = ELeftRightOpSign.InclusiveOr;
                    break;
                case ETokenType.XOR:
                    m_OpLevelSign = ELeftRightOpSign.XOR;
                    break;
                case ETokenType.Shi:
                    m_OpLevelSign = ELeftRightOpSign.Shi;
                    break;
                case ETokenType.Shr:
                    m_OpLevelSign = ELeftRightOpSign.Shr;
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 没有适合的符号!!!" + ett.ToString());
                    }
                    break;
            }
            ComputeIsComputeType();
        }
        public MetaOpExpressNode(MetaExpressNodeBase _left, MetaExpressNodeBase _right, ELeftRightOpSign _opSign)
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
                    )
                isEqualType = true;
            else
                isEqualType = false;
        }
        public void SetLeft(MetaExpressNodeBase _left)
        {
            m_Left = _left;
        }
        public void SetRight(MetaExpressNodeBase _right)
        {
            m_Right = _right;
        }
        public override void Parse(AllowUseSettings auc)
        {
            m_Left.Parse(auc);
            m_Left = ExpressManager.ConvertNewExpress(m_Left, null);
            m_Right.Parse(auc);
            m_Right = ExpressManager.ConvertNewExpress(m_Right, null);

            if( m_Left.parseSuccessed && m_Right.parseSuccessed )
            {
                m_ParsedState = EParseState.ParseSuccess;
            }
            else
            {
                m_ParsedState = EParseState.ParsedFailed;
            }
        }
        //public override int CalcParseLevel(int level)
        //{
        //    int level1 = m_Left.CalcParseLevel(level);
        //    int level2 = m_Right.CalcParseLevel(level1);
        //
        //    return level2;
        //}
        public override void CalcReturnType()
        {
            if (this.m_Left != null)
            {
                m_Left.CalcReturnType();
            }
            if (m_Right != null)
            {
                m_Right.CalcReturnType();
            }
            ParseCompute();

            // 解析失败的错误路径可能未设置 m_RealMetaType，兜底为 object 避免空引用
            if (this.m_RealMetaType != null)
            {
                m_ExpressReturnMetaType = new MetaType(this.m_RealMetaType);
            }
            else
            {
                m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
        }
        public void ParseCompute()
        {
            if (m_Left == null || m_Right == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "right or left is null");
                return;
            }

            bool isChange = false;
            MetaExpressNodeBase left = m_Left;
            MetaExpressNodeBase right = m_Right;
            MetaType leftMt = left.GetReturnMetaType();
            MetaType rightMt = right.GetReturnMetaType();

            if (leftMt == null || rightMt == null)
            {
                m_RealMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                return;
            }

            if (leftMt.isEnum || rightMt.isEnum || leftMt.isEnumMember || rightMt.isEnumMember )
            {
                bool isEnumCompareOp = m_OpLevelSign == ELeftRightOpSign.Equal || m_OpLevelSign == ELeftRightOpSign.NotEqual;
                if (!isEnumCompareOp)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "enum类型只允许 == 或 != 运算!!");
                    m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                    return;
                }

                MetaExpressNodeBase enumExpr = null;
                MetaExpressNodeBase enumMemberExpr = null;

                MetaType enumType = null;
                MetaType anotherType = new MetaType();
                if ( leftMt.isEnum )
                {
                    enumExpr = left;
                    enumType = left.GetReturnMetaType();

                    if (rightMt.isEnum)
                    {
                        if (leftMt.metaEnum != rightMt.metaEnum)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "不是同一个enum");
                            return;
                        }
                    }
                    else if(rightMt.isEnumMember )
                    {
                        if( rightMt.enumValue.ownerMetaBase != leftMt.metaEnum )
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "不是同一个enum");
                            return;
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "is not enum member "); 
                        return;
                    }
                }
                else if( rightMt.isEnum )
                {
                    enumExpr = m_Right;
                    enumMemberExpr = left;

                    enumType = m_Right.GetReturnMetaType();

                    if (leftMt.isEnum)
                    {
                        if (leftMt.metaEnum != rightMt.metaEnum)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "不是同一个enum");
                            return;
                        }
                    }
                    else if ( leftMt.isEnumMember )
                    {
                        if (leftMt.enumValue.ownerMetaBase != rightMt.metaEnum)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "不是同一个enum");
                            return;
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaBaseTerm.token, "is not enum member ");
                        return;
                    }
                }
                else if( leftMt.isEnumMember && rightMt.isEnumMember )
                {
                    if (leftMt.enumValue.ownerMetaBase != rightMt.enumValue.ownerMetaBase )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaBaseTerm.token, "不是同一个enum");
                        return;
                    }
                }
                m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                return;
            }
            else
            {
                if (leftMt.isData || rightMt.isData)
                {
                    bool isEnumCompareOp = m_OpLevelSign == ELeftRightOpSign.Equal || m_OpLevelSign == ELeftRightOpSign.NotEqual;
                    if (!isEnumCompareOp)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "enum类型只允许 == 或 != 运算!!" );

                        m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                        return;
                    }
                    m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                }
                else if (leftMt.isClass || rightMt.isClass)
                {
                    if (m_Left.opLevel > m_Right.opLevel)
                    {
                        left = m_Right;
                        right = m_Left;
                        isChange = true;
                    }

                    bool isFindDefineFunction = false;
                    MetaClass leftMc = leftMt.metaClass;
                    MetaClass rightMc = rightMt.metaClass;

                    if( leftMc == null || rightMc == null )
                    {
                        if( leftMt == null )
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left or right class is null" + leftMt.ToFormatString() );
                        }
                        else if( rightMc == null )
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left or right class is null" + rightMt.ToFormatString() );
                        }
                        return;
                    }

                    bool isCompareOp = m_OpLevelSign == ELeftRightOpSign.Equal
                        || m_OpLevelSign == ELeftRightOpSign.NotEqual
                        || m_OpLevelSign == ELeftRightOpSign.Greater
                        || m_OpLevelSign == ELeftRightOpSign.GreaterOrEqual
                        || m_OpLevelSign == ELeftRightOpSign.Less
                        || m_OpLevelSign == ELeftRightOpSign.LessOrEqual
                        || m_OpLevelSign == ELeftRightOpSign.Or
                        || m_OpLevelSign == ELeftRightOpSign.And;

                    if (leftMc.eType == EType.Null || rightMc.eType == EType.Null)
                    {
                        isCompareOp = m_OpLevelSign == ELeftRightOpSign.Equal || m_OpLevelSign == ELeftRightOpSign.NotEqual;
                        if (!isCompareOp)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error null == 或 != 运算!!");
                            return;
                        }
                        m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                        return;
                    }

                    if (leftMc.eType == EType.Boolean)
                    {
                        if (rightMc.eType == EType.Boolean)
                        {
                            //都是布尔类型
                            m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                        }
                        else if (rightMc.eType == EType.String)
                        {
                            if (m_OpLevelSign == ELeftRightOpSign.Add)
                            {
                                m_RealMetaType = new MetaType(CoreMetaClassManager.stringMetaClass);
                                // ensure numeric left will be converted to string for concatenation
                                m_LeftConvert = new ConvertType() { oriType = leftMc.eType, targetType = EType.String };
                            }
                            else if (m_OpLevelSign == ELeftRightOpSign.Equal
                                || m_OpLevelSign == ELeftRightOpSign.NotEqual)
                            {
                                m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "string type only support _plus_,_equal_,_noequal_");
                            }
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "布尔类型不能参与加减运算");
                        }
                    }
                    else if (NumberManager.IsNumberClass(leftMc))
                    {
                        if (NumberManager.IsNumberClass(rightMc))
                        {
                            switch (m_OpLevelSign)
                            {
                                case ELeftRightOpSign.Add:
                                case ELeftRightOpSign.Minus:
                                case ELeftRightOpSign.Multiply:
                                case ELeftRightOpSign.Divide:
                                case ELeftRightOpSign.Modulo:
                                case ELeftRightOpSign.InclusiveOr:
                                case ELeftRightOpSign.Combine:
                                case ELeftRightOpSign.XOR:
                                case ELeftRightOpSign.Shi:
                                case ELeftRightOpSign.Shr:
                                    {
                                        //都是数字类型
                                        if (ProjectManager.config?.Compile?.RequireSameNumericTypes == true
                                            && leftMc.eType != rightMc.eType)
                                        {
                                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "已启用 compile.requireSameNumericTypes：算术/位运算两侧须为同一数字类型（如 byte+byte、Int32+Int32），禁止不同类型混合运算。");
                                            m_RealMetaType = new MetaType(leftMc);
                                            break;
                                        }

                                        EType etype = MetaTypeFactory.CalcETypeByLeftAndRight(leftMc.eType, rightMc.eType, m_OpLevelSign, out int error);
                                        if (error == 0 && etype != EType.None)
                                        {
                                            if (etype != rightMc.eType)
                                            {
                                                m_LeftConvert = new ConvertType()
                                                {
                                                    oriType = leftMc.eType,
                                                    targetType = etype
                                                };

                                                m_RightConvert = new ConvertType()
                                                {
                                                    oriType = rightMc.eType,
                                                    targetType = etype
                                                };
                                            }

                                            m_RealMetaType = new MetaType(CoreMetaClassManager.GetMetaClassByEType(etype));
                                        }
                                        else
                                        {
                                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "加减运算类型计算错误!!");
                                        }
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
                                    {
                                        m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                                    }
                                    break;
                            }
                        }
                        else if (rightMc.eType == EType.String)
                        {
                            if (m_OpLevelSign == ELeftRightOpSign.Add)
                            {
                                m_RealMetaType = new MetaType(CoreMetaClassManager.stringMetaClass);
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 字符串类型只能参与加法运算!!");
                            }
                        }
                        else if (rightMc.eType == EType.Enum)
                        {
                            if (isCompareOp)
                            {
                                // support number-backed enum comparisons, e.g. BridgeKind param vs BridgeKind.CLR member value
                                m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                            }
                            else
                            {
                                isFindDefineFunction = true;
                            }
                        }
                        else
                        {
                            isFindDefineFunction = true;
                        }
                    }
                    else if (leftMc.eType == EType.String)
                    {
                        if (m_OpLevelSign == ELeftRightOpSign.Add)
                        {
                            m_RealMetaType = new MetaType(CoreMetaClassManager.stringMetaClass);
                            // if right side is numeric, convert it to string
                            if (NumberManager.IsNumberClass(rightMc))
                            {
                                m_RightConvert = new ConvertType() { oriType = rightMc.eType, targetType = EType.String };
                            }
                        }
                        else if (m_OpLevelSign == ELeftRightOpSign.Equal
                            || m_OpLevelSign == ELeftRightOpSign.NotEqual)
                        {
                            m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "string type only support _plus_,_equal_,_noequal_");
                        }
                    }
                    else if ((leftMc.eType == EType.Boolean && rightMc.eType == EType.String) || (leftMc.eType == EType.String && rightMc.eType == EType.Boolean))
                    {
                        // implicit convert boolean to string when concatenating
                        m_RealMetaType = new MetaType(CoreMetaClassManager.stringMetaClass);
                        // inject convert operation: boolean -> string
                        if (leftMc.eType == EType.Boolean)
                        {
                            m_LeftConvert = new ConvertType() { oriType = EType.Boolean, targetType = EType.String };
                        }
                        if (rightMc.eType == EType.Boolean)
                        {
                            m_RightConvert = new ConvertType() { oriType = EType.Boolean, targetType = EType.String };
                        }
                    }
                    else
                    {
                        switch (m_OpLevelSign)
                        {
                            case ELeftRightOpSign.Add:
                            case ELeftRightOpSign.Minus:
                            case ELeftRightOpSign.Multiply:
                            case ELeftRightOpSign.Divide:
                            case ELeftRightOpSign.Modulo:
                            case ELeftRightOpSign.InclusiveOr:
                            case ELeftRightOpSign.Combine:
                            case ELeftRightOpSign.XOR:
                            case ELeftRightOpSign.Shi:
                            case ELeftRightOpSign.Shr:
                                {
                                    isFindDefineFunction = true;
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
                                {
                                    m_RealMetaType = new MetaType(CoreMetaClassManager.booleanMetaClass);
                                }
                                break;
                        }
                    }


                    if (isFindDefineFunction)
                    {
                        var mipc = new MetaInputParamCollection(left.expressReturnMetaType.metaBase, null);
                        MetaInputParam mip = new MetaInputParam(right);
                        mipc.AddMetaInputParam(mip);
                        var mmf = rightMc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_op_add_", 0, mipc);
                        if (mmf == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Left:" + left.token.ToLexemeAllString() + "右边类型不能转换为左边类型进行加减运算!! Right:" + right.token.ToLexemeAllString()  );
                            // 错误路径必须设置类型，否则 CalcReturnType 中 new MetaType(null) 会空引用崩溃
                            m_RealMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                            return;
                        }
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "");
                }
            }          
        }
        public MetaExpressNodeBase SimulateCompute(ExpressOptimizeConfig config)
        {
            if (config.greaterOrEqualConvertGeraterAndEqual && m_OpLevelSign == ELeftRightOpSign.GreaterOrEqual)
            {
                var constLeft = m_Left as MetaConstExpressNode;
                var constRight = m_Right as MetaConstExpressNode;
                if (constLeft == null || constRight == null)
                {
                    MetaExpressNodeBase left1 = m_Left;
                    MetaExpressNodeBase right1 = m_Right;
                    MetaExpressNodeBase left2 = m_Left;
                    MetaExpressNodeBase right2 = m_Right;
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
                    MetaExpressNodeBase left1 = m_Left;
                    MetaExpressNodeBase right1 = m_Right;
                    MetaExpressNodeBase left2 = m_Left;
                    MetaExpressNodeBase right2 = m_Right;
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
                case ELeftRightOpSign.Shi: { return "<<"; }
                case ELeftRightOpSign.Shr: { return ">>"; }

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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_Left?.ToString());
            if (m_SignToken != null)
            {
                sb.Append(m_SignToken.lexeme?.ToString());
            }
            else
            {
                sb.Append(GetSignString(m_OpLevelSign));
            }
            sb.Append(m_Right?.ToString());
            return sb.ToString();
        }
    }
}
