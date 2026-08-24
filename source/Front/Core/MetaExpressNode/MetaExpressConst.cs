//****************************************************************************
//  File:      MetaExpressConst.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/18 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Text;


namespace SimpleLanguage.Core
{
    public sealed class MetaConstExpressNode : MetaExpressNodeBase
    {
        // pooled string reference (offset/length into shared byte pool)
        public struct StringRef
        {
            public int Offset;
            public int Length;
        }

        //static class StringPool
        //{
        //    private static List<byte> s_pool = new List<byte>();
        //    private static readonly object s_lock = new object();

        //    public static StringRef AddString(string s)
        //    {
        //        if (s == null)
        //        {
        //            return new StringRef { Offset = -1, Length = 0 };
        //        }
        //        var bytes = Encoding.UTF8.GetBytes(s);
        //        lock (s_lock)
        //        {
        //            int off = s_pool.Count;
        //            s_pool.AddRange(bytes);
        //            return new StringRef { Offset = off, Length = bytes.Length };
        //        }
        //    }

        //    public static string GetString(StringRef r)
        //    {
        //        if (r.Offset < 0 || r.Length == 0) return null;
        //        // make copy for decoding
        //        byte[] arr;
        //        lock (s_lock)
        //        {
        //            arr = s_pool.ToArray();
        //        }
        //        return Encoding.UTF8.GetString(arr, r.Offset, r.Length);
        //    }

        //    public static byte[] GetPoolBytes()
        //    {
        //        lock (s_lock)
        //        {
        //            return s_pool.ToArray();
        //        }
        //    }
        //}

        public static MetaConstExpressNode operator +(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            if (left.opLevel > right.opLevel)
            {
                left.ComputeAddRight(right);
                return left;
            }
            else
            {
                right.ComputeAddRight(left);
                return right;
            }
        }
        public static MetaConstExpressNode operator -(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            // subtraction is not commutative: always compute left - right
            left.ComputeMinusRight(right);
            return left;
        }
        public static MetaConstExpressNode operator *(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            if (left.opLevel > right.opLevel)
            {
                left.ComputeMulRight(right);
                return left;
            }
            else
            {
                right.ComputeMulRight(left);
                return right;
            }
        }
        public static MetaConstExpressNode operator /(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            // division is not commutative: always compute left / right
            left.ComputeDivRight(right);
            return left;
        }
        public static MetaConstExpressNode operator %(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            // modulo is not commutative: always compute left % right
            left.ComputeModRight(right);
            return left;
        }
        public List<MetaExpressNodeBase> stringParseExpressList => m_StringParseExpressList;
        public object value { get; set; } = null;
        // when eType == String, this holds the pooled string reference
        //public StringRef stringRef { get; private set; }
        // helper to inspect pooled string during debugging
        //public string PooledString => StringPool.GetString(stringRef);
        public EType eType { get; private set; } = EType.None;

        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;
        private List<MetaExpressNodeBase> m_StringParseExpressList = new List<MetaExpressNodeBase>();
        public MetaConstExpressNode( MetaBase omc, MetaBlockStatements mbs, FileMetaConstValueTerm fmct)
        {
            m_FileMetaConstValueTerm = fmct;
            m_OwnerMetaBase = omc;
            m_OwnerMetaBlockStatements = mbs;

            eType = fmct.token.GetEType();
            m_Token = fmct.token;

            Parse1(eType, fmct.token.lexeme);
        }
        public MetaConstExpressNode(EType _eType, object val)
        {
            eType = _eType;
            Parse1(_eType, val);
        }
        public MetaConstExpressNode(MetaType mt, object val)
        {
            Parse1(eType, val);
        }
        public void SetConstValue(EType etype, object val)
        {
            eType = etype;
            Parse1(etype, val);
        }
        public void SetNumType( EType etype )
        {
            eType = etype;

            try
            {
                switch (etype)
                {
                    case EType.Int8:
                        {
                            value = Convert.ToSByte(value);
                        }
                        break;
                    case EType.UInt8:
                        {
                            value = Convert.ToByte(value);
                        }
                        break;
                    case EType.Int16:
                        {
                            value = Convert.ToInt16(value);
                        }
                        break;
                    case EType.UInt16:
                        {
                            value = Convert.ToUInt16(value);
                        }
                        break;
                    case EType.Int32:
                        {
                            value = Convert.ToInt32(value);
                        }
                        break;
                    case EType.UInt32:
                        {
                            value = Convert.ToUInt32(value);
                        }
                        break;
                    case EType.Int64:
                        {
                            value = Convert.ToInt64(value);
                        }
                        break;
                    case EType.UInt64:
                        {
                            value = Convert.ToUInt64(value);
                        }
                        break;
                    case EType.Float8:
                        {
                            value = Convert.ToByte(value);
                        }
                        break;
                    case EType.Float8_E5M2:
                        {
                            value = Convert.ToByte(value);
                        }
                        break;
                    case EType.Float16:
                        {
                            value = Convert.ToUInt16(value);
                        }
                        break;
                    case EType.Float16_Brain:
                        {
                            value = Convert.ToUInt16(value);
                        }
                        break;
                    case EType.Float32:
                        {
                            value = Convert.ToSingle(value);
                        }
                        break;
                    case EType.Float64:
                        {
                            value = Convert.ToDouble(value);
                        }
                        break;
                }
            }catch( Exception e)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, e.Message);
            }
        }
        public override void Parse(AllowUseSettings auc)
        {
            if (m_ParsedState != EParseState.None) return;
            m_ParsedState = EParseState.ParseSuccess;
            ETokenType tt = ETokenType.None;
            if( m_FileMetaConstValueTerm?.token != null )
            {
                tt = m_FileMetaConstValueTerm.token.type;
            }
            //if (tt == ETokenType.NumberReal)
            //{
            //    //var lexeme = m_FileMetaConstValueTerm.token.lexeme;
            //    //if (lexeme is sbyte sbVal)
            //    //{
            //    //    eType = EType.Int8;
            //    //    value = sbVal;
            //    //}
            //    //else if (lexeme is byte bVal)
            //    //{
            //    //    eType = EType.UInt8;
            //    //    value = bVal;
            //    //}
            //    //else if (lexeme is short sVal)
            //    //{
            //    //    eType = EType.Int16;
            //    //    value = sVal;
            //    //}
            //    //else if (lexeme is ushort usVal)
            //    //{
            //    //    eType = EType.UInt16;
            //    //    value = usVal;
            //    //}
            //    //else if (lexeme is int iVal)
            //    //{
            //    //    eType = EType.Int32;
            //    //    value = iVal;
            //    //}
            //    //else if (lexeme is uint uiVal)
            //    //{
            //    //    eType = EType.UInt32;
            //    //    value = uiVal;
            //    //}
            //    //else if (lexeme is long lVal)
            //    //{
            //    //    eType = EType.Int64;
            //    //    value = lVal;
            //    //}
            //    //else if (lexeme is ulong ulVal)
            //    //{
            //    //    eType = EType.UInt64;
            //    //    value = ulVal;
            //    //}
            //    //else if (lexeme is float fVal)
            //    //{
            //    //    eType = EType.Float32;
            //    //    value = fVal;
            //    //}
            //    //else if (lexeme is double dVal)
            //    //{
            //    //    eType = EType.Float64;
            //    //    value = dVal;
            //    //}
            //    //else
            //    //{
            //    //    eType = EType.Num;
            //    //    value = lexeme;
            //    //}
            //}
            //else if (tt == ETokenType.Number)
            //{                
            //}
            //else 
                if (tt == ETokenType.String)
            {
                var cdlist = m_FileMetaConstValueTerm.token.childrenTokensList;
                if( cdlist.Count == 1 && (cdlist[0].Count == 1 && cdlist[0][0].type == ETokenType.String ) )
                {
                    var s = cdlist[0][0].lexeme.ToString();
                    value = s;
                    //stringRef = s;
                    return;
                }
                else
                {
                    //解析 string里边的${} $name 这种的提取
                    for (int i = 0; i < cdlist.Count; i++)
                    {
                        TokenParse tp = new TokenParse( null, cdlist[i]);
                        tp.BuildStruct();

                        List<Node> nodeList = tp.rootNode.childList;
                        if (nodeList.Count > 0)
                        {
                            var filemetaExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMetaConstValueTerm.fileMeta, nodeList, FileMetaTermExpress.EExpressType.Common);

                            CreateExpressParam cep = new CreateExpressParam();
                            cep.fme = filemetaExpress;
                            cep.equalMetaVariable = null;
                            cep.metaType = new MetaType(CoreMetaClassManager.stringMetaClass);
                            cep.ownerMBS = m_OwnerMetaBlockStatements;
                            cep.ownerMetaBase = m_OwnerMetaBase;

                            var expressc = ExpressManager.CreateExpressNode(cep);
                            expressc.Parse(auc);
                            expressc.CalcReturnType();

                            m_StringParseExpressList.Add(expressc);

                        }
                    }
                    if (m_StringParseExpressList.Count > 0)
                    {
                        m_ConvertOpExpressNode = true;
                    }

                }
            }
        }
        private void Parse1(EType _etype, object val)
        {
            switch (_etype)
            {
                case EType.Boolean:
                    {
                        value = val.ToString() == "true";
                        eType = EType.Boolean;
                    }
                    break;
                case EType.Null:
                    {
                        value = "null";
                        eType = EType.Null;
                    }
                    break;
                case EType.String:
                    {
                        value = val?.ToString();
                        //stringRef = StringPool.AddString(value as string);
                        eType = EType.String;
                    }
                    break;
                default:
                    {
                        value = val;
                        eType = _etype;
                    }
                    break;
            }
            m_ExpressReturnMetaType = new MetaType(eType);
        }
        public override void CalcReturnType()
        {
            if (m_ExpressReturnMetaType != null)
            {
                if(m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.nullMetaClass  )
                {
                    eType = EType.Null;
                    value = "null";
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.booleanMetaClass)
                {
                    eType = EType.Boolean;
                    value = Convert.ToBoolean(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.uint8MetaClass)
                {
                    eType = EType.UInt8;
                    value = Convert.ToByte(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.int8MetaClass)
                {
                    eType = EType.Int8;
                    value = Convert.ToSByte(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.int16MetaClass)
                {
                    eType = EType.Int16;
                    value = Convert.ToInt16(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.uint16MetaClass)
                {
                    eType = EType.UInt16;
                    value = Convert.ToUInt16(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.int32MetaClass)
                {
                    eType = EType.Int32;
                    value = Convert.ToInt32(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.uint32MetaClass)
                {
                    eType = EType.UInt32;
                    value = Convert.ToUInt32(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.int64MetaClass)
                {
                    eType = EType.Int64;
                    value = Convert.ToInt64(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.uint64MetaClass)
                {
                    eType = EType.UInt64;
                    value = Convert.ToUInt64(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float32MetaClass)
                {
                    eType = EType.Float32;
                    value = Convert.ToSingle(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float64MetaClass)
                {
                    eType = EType.Float64;
                    value = Convert.ToDouble(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float8MetaClass)
                {
                    // float8 常量存储为 byte 位模式
                    eType = EType.Float8;
                    value = Convert.ToByte(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float8_E5M2MetaClass)
                {
                    eType = EType.Float8_E5M2;
                    value = Convert.ToByte(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float16MetaClass)
                {
                    // float16/bfloat16 常量存储为 ushort 位模式
                    eType = EType.Float16;
                    value = Convert.ToUInt16(value);
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.float16_BrainMetaClass)
                {
                    eType = EType.Float16_Brain;
                    value = Convert.ToUInt16(value);
                }
                else if(m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.numMetaClass )
                {
                    eType = EType.Num;
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.stringMetaClass)
                {
                    eType = EType.String;
                    value = value.ToString();
                    //stringRef = StringPool.AddString(value as string);
                }
                else
                {
                    eType = EType.Class;
                }
                return;
            }
            //MetaType mdt = null;
            if (eType == EType.Null)
            {
                m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.nullMetaClass);
            }
            else
            {
                MetaClass mc = CoreMetaClassManager.GetMetaClassByEType(eType);

                if (mc == null)
                {
                    m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                }
                else
                {
                    MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
                    if (eType == EType.Array)
                    {
                        MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                        mitc.AddMetaTemplateParamsList(mitp);
                        m_ExpressReturnMetaType = new MetaType(mc, null, mitc);
                    }
                    else
                    {
                        m_ExpressReturnMetaType = new MetaType(mc);
                    }
                }
            }
        }
        public void ComputeAddRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    value = (byte)value + (byte)right.value;
                    break;
                case EType.Int16:
                    value = (short)value + (short)right.value;
                    break;
                case EType.UInt16:
                    value = (ushort)value + (ushort)right.value;
                    break;
                case EType.Int32:
                    value = (int)value + (int)right.value;
                    break;
                case EType.UInt32:
                    value = (uint)value + (uint)right.value;
                    break;
                case EType.Int64:
                    value = (long)value + (long)right.value;
                    break;
                case EType.UInt64:
                    value = (ulong)value + (ulong)right.value;
                    break;
                case EType.String:
                    {
                        value = value.ToString() + (string)right.value;
                    }
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        // 位模式解码为数值运算后再编码回位模式
                        double fa = Float816Convert.BitsToDoubleByEType(eType, value);
                        double fb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        value = Float816Convert.ToBitsByEType(eType, fa + fb);
                    }
                    break;
                case EType.Float32:
                    value = (float)value + (float)right.value;
                    break;
                case EType.Float64:
                case EType.Num:
                    value = Convert.ToDouble(value) + Convert.ToDouble(right.value);
                    break;
            }
        }
        public void ComputeMinusRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    value = (byte)value - (byte)right.value;
                    break;
                case EType.Int16:
                    value = (short)value - (short)right.value;
                    break;
                case EType.UInt16:
                    value = (ushort)value - (ushort)right.value;
                    break;
                case EType.Int32:
                    value = (int)value - (int)right.value;
                    break;
                case EType.UInt32:
                    value = (uint)value - (uint)right.value;
                    break;
                case EType.Int64:
                    value = (long)value - (long)right.value;
                    break;
                case EType.UInt64:
                    value = (ulong)value - (ulong)right.value;
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        double fa = Float816Convert.BitsToDoubleByEType(eType, value);
                        double fb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        value = Float816Convert.ToBitsByEType(eType, fa - fb);
                    }
                    break;
                case EType.Float32:
                    value = (float)value - (float)right.value;
                    break;
                case EType.Float64:
                case EType.Num:
                    value = Convert.ToDouble(value) - Convert.ToDouble(right.value);
                    break;
            }
        }
        public void ComputeMulRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    value = (byte)value * (byte)right.value;
                    break;
                case EType.Int16:
                    value = (short)value * (short)right.value;
                    break;
                case EType.UInt16:
                    value = (ushort)value * (ushort)right.value;
                    break;
                case EType.Int32:
                    value = (int)value * (int)right.value;
                    break;
                case EType.UInt32:
                    value = (uint)value * (uint)right.value;
                    break;
                case EType.Int64:
                    value = (long)value * (long)right.value;
                    break;
                case EType.UInt64:
                    value = (ulong)value * (ulong)right.value;
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        double fa = Float816Convert.BitsToDoubleByEType(eType, value);
                        double fb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        value = Float816Convert.ToBitsByEType(eType, fa * fb);
                    }
                    break;
                case EType.Float32:
                    value = (float)value * (float)right.value;
                    break;
                case EType.Float64:
                case EType.Num:
                    value = Convert.ToDouble(value) * Convert.ToDouble(right.value);
                    break;
            }
        }
        public void ComputeDivRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    value = (byte)value / (byte)right.value;
                    break;
                case EType.Int16:
                    value = (short)value / (short)right.value;
                    break;
                case EType.UInt16:
                    value = (ushort)value / (ushort)right.value;
                    break;
                case EType.Int32:
                    value = (int)value / (int)right.value;
                    break;
                case EType.UInt32:
                    value = (uint)value / (uint)right.value;
                    break;
                case EType.Int64:
                    value = (long)value / (long)right.value;
                    break;
                case EType.UInt64:
                    value = (ulong)value / (ulong)right.value;
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        double fa = Float816Convert.BitsToDoubleByEType(eType, value);
                        double fb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        value = Float816Convert.ToBitsByEType(eType, fa / fb);
                    }
                    break;
                case EType.Float32:
                    value = (float)value / (float)right.value;
                    break;
                case EType.Float64:
                case EType.Num:
                    value = Convert.ToDouble(value) / Convert.ToDouble(right.value);
                    break;
            }
        }
        public void ComputeModRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    value = (byte)value % (byte)right.value;
                    break;
                case EType.Int16:
                    value = (short)value % (short)right.value;
                    break;
                case EType.UInt16:
                    value = (ushort)value % (ushort)right.value;
                    break;
                case EType.Int32:
                    value = (int)value % (int)right.value;
                    break;
                case EType.UInt32:
                    value = (uint)value % (uint)right.value;
                    break;
                case EType.Int64:
                    value = (long)value % (long)right.value;
                    break;
                case EType.UInt64:
                    value = (ulong)value % (ulong)right.value;
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        double fa = Float816Convert.BitsToDoubleByEType(eType, value);
                        double fb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        value = Float816Convert.ToBitsByEType(eType, fa % fb);
                    }
                    break;
                case EType.Float32:
                    value = (float)value % (float)right.value;
                    break;
                case EType.Float64:
                case EType.Num:
                    value = Convert.ToDouble(value) % Convert.ToDouble(right.value);
                    break;
            }
        }
        public void ComputeEqualComputeRight(MetaConstExpressNode right, ELeftRightOpSign opSign)
        {
            switch (right.eType)
            {
                case EType.UInt8:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (byte)value == (byte)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (byte)value != (byte)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (byte)value > (byte)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (byte)value >= (byte)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (byte)value < (byte)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (byte)value <= (byte)right.value;
                            break;
                    }
                    break;
                case EType.Int16:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (short)value == (short)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (short)value != (short)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (short)value > (short)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (short)value >= (short)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (short)value < (short)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (short)value <= (short)right.value;
                            break;
                    }
                    break;
                case EType.UInt16:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (ushort)value == (ushort)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (ushort)value != (ushort)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (ushort)value > (ushort)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (ushort)value >= (ushort)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (ushort)value < (ushort)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (ushort)value <= (ushort)right.value;
                            break;
                    }
                    break;
                case EType.Int32:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (int)value == (int)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (int)value != (int)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (int)value > (int)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (int)value >= (int)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (int)value < (int)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (int)value <= (int)right.value;
                            break;
                    }
                    break;
                case EType.UInt32:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (uint)value == (uint)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (uint)value != (uint)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (uint)value > (uint)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (uint)value >= (uint)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (uint)value < (uint)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (uint)value <= (uint)right.value;
                            break;
                    }
                    break;
                case EType.Int64:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (long)value == (long)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (long)value != (long)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (long)value > (long)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (long)value >= (long)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (long)value < (long)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (long)value <= (long)right.value;
                            break;
                    }
                    break;
                case EType.UInt64:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (ulong)value == (ulong)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (ulong)value != (ulong)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (ulong)value > (ulong)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (ulong)value >= (ulong)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (ulong)value < (ulong)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (ulong)value <= (ulong)right.value;
                            break;
                    }
                    break;
                case EType.String:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (string)value == (string)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (string)value != (string)right.value;
                            break;
                        default:
                            {
                                Debug.Write("Error Not Support string < <= > >=sign operator!!");
                            }
                            break;
                    }
                    break;
                case EType.Float8:
                case EType.Float8_E5M2:
                case EType.Float16:
                case EType.Float16_Brain:
                    {
                        // 位模式先解码为数值再比较
                        var lt = eType;
                        double ca = Float816Convert.BitsToDoubleByEType(lt, value);
                        double cb = Float816Convert.BitsToDoubleByEType(right.eType, right.value);
                        switch (opSign)
                        {
                            case ELeftRightOpSign.Equal:
                                eType = EType.Boolean;
                                value = ca == cb;
                                break;
                            case ELeftRightOpSign.NotEqual:
                                eType = EType.Boolean;
                                value = ca != cb;
                                break;
                            case ELeftRightOpSign.Greater:
                                eType = EType.Boolean;
                                value = ca > cb;
                                break;
                            case ELeftRightOpSign.GreaterOrEqual:
                                eType = EType.Boolean;
                                value = ca >= cb;
                                break;
                            case ELeftRightOpSign.Less:
                                eType = EType.Boolean;
                                value = ca < cb;
                                break;
                            case ELeftRightOpSign.LessOrEqual:
                                eType = EType.Boolean;
                                value = ca <= cb;
                                break;
                        }
                    }
                    break;
                case EType.Float32:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (float)value == (float)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (float)value != (float)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (float)value > (float)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (float)value >= (float)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (float)value < (float)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (float)value <= (float)right.value;
                            break;
                    }
                    break;
                case EType.Float64:
                case EType.Num:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) == Convert.ToDouble(right.value);
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) != Convert.ToDouble(right.value);
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) > Convert.ToDouble(right.value);
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) >= Convert.ToDouble(right.value);
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) < Convert.ToDouble(right.value);
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = Convert.ToDouble(value) <= Convert.ToDouble(right.value);
                            break;
                    }
                    break;
            }
        }
        public override string ToFormatString()
        {
            string signEn = "";
            string str = value.ToString();
            switch (eType)
            {
                case EType.Null:
                    {
                        str = "null";
                    }
                    break;
                case EType.String:
                    {
                        str = "\"" + value.ToString() + "\"";
                    }
                    break;
                //case EType.Char:
                //    {
                //        str = "\'" + value.ToString() + "\'";
                //    }
                //    break;
                case EType.Int16:
                    {
                        signEn = "s";
                    }
                    break;
                case EType.UInt16:
                    {
                        signEn = "us";
                    }
                    break;
                case EType.Int32:
                    {
                        signEn = "i";
                    }
                    break;
                case EType.UInt32:
                    {
                        signEn = "ui";
                    }
                    break;
                case EType.Int64:
                    {
                        signEn = "L";
                    }
                    break;
                case EType.UInt64:
                    {
                        signEn = "uL";
                    }
                    break;
                case EType.Float8:
                    {
                        str = Float816Convert.BitsToDoubleByEType(EType.Float8, value).ToString();
                        signEn = "fe4";
                    }
                    break;
                case EType.Float8_E5M2:
                    {
                        str = Float816Convert.BitsToDoubleByEType(EType.Float8_E5M2, value).ToString();
                        signEn = "fe5";
                    }
                    break;
                case EType.Float16:
                    {
                        str = Float816Convert.BitsToDoubleByEType(EType.Float16, value).ToString();
                        signEn = "h";
                    }
                    break;
                case EType.Float16_Brain:
                    {
                        str = Float816Convert.BitsToDoubleByEType(EType.Float16_Brain, value).ToString();
                        signEn = "hb";
                    }
                    break;
                case EType.Float32:
                    {
                        signEn = "f";
                    }
                    break;
                case EType.Float64:
                    {
                        signEn = "d";
                    }
                    break;
            }
            return str + signEn;
        }
        public override string ToString()
        {
            return "ConstExpressNode: [" + eType.ToString() + "] " + ToFormatString();
        }
    }
}
