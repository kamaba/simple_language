//****************************************************************************
//  File:      MetaExpressConst.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/18 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
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

        static class StringPool
        {
            private static List<byte> s_pool = new List<byte>();
            private static readonly object s_lock = new object();

            public static StringRef AddString(string s)
            {
                if (s == null)
                {
                    return new StringRef { Offset = -1, Length = 0 };
                }
                var bytes = Encoding.UTF8.GetBytes(s);
                lock (s_lock)
                {
                    int off = s_pool.Count;
                    s_pool.AddRange(bytes);
                    return new StringRef { Offset = off, Length = bytes.Length };
                }
            }

            public static string GetString(StringRef r)
            {
                if (r.Offset < 0 || r.Length == 0) return null;
                // make copy for decoding
                byte[] arr;
                lock (s_lock)
                {
                    arr = s_pool.ToArray();
                }
                return Encoding.UTF8.GetString(arr, r.Offset, r.Length);
            }

            public static byte[] GetPoolBytes()
            {
                lock (s_lock)
                {
                    return s_pool.ToArray();
                }
            }
        }

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
        public MetaCallLinkExpressNode metaCallLinkExpressNode => m_MetaCallLinkExpressNode;
        public List<MetaExpressNodeBase> stringParseExpressList => m_StringParseExpressList;
        public object value { get; set; } = null;
        // when eType == String, this holds the pooled string reference
        public StringRef stringRef { get; private set; }
        // helper to inspect pooled string during debugging
        public string PooledString => StringPool.GetString(stringRef);
        public EType eType { get; private set; } = EType.None;
        public override Token token => m_FileMetaConstValueTerm?.token;

        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;
        private List<MetaExpressNodeBase> m_StringParseExpressList = new List<MetaExpressNodeBase>();
        private MetaCallLinkExpressNode m_MetaCallLinkExpressNode = null;
        public MetaConstExpressNode( MetaBase omc, MetaBlockStatements mbs, FileMetaConstValueTerm fmct)
        {
            m_FileMetaConstValueTerm = fmct;
            m_OwnerMetaBase = omc;
            m_OwnerMetaBlockStatements = mbs;

            eType = fmct.token.GetEType();

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
        public override void Parse(AllowUseSettings auc)
        {
            if (m_Parse) return;
            m_Parse = true;
            ETokenType tt = ETokenType.None;
            if( m_FileMetaConstValueTerm?.token != null )
            {
                tt = m_FileMetaConstValueTerm.token.type;
            }
            if (tt == ETokenType.NumberReal)
            {
                var lexeme = m_FileMetaConstValueTerm.token.lexeme;
                if (lexeme is sbyte sbVal)
                {
                    eType = EType.Int8;
                    value = sbVal;
                }
                else if (lexeme is byte bVal)
                {
                    eType = EType.UInt8;
                    value = bVal;
                }
                else if (lexeme is short sVal)
                {
                    eType = EType.Int16;
                    value = sVal;
                }
                else if (lexeme is ushort usVal)
                {
                    eType = EType.UInt16;
                    value = usVal;
                }
                else if (lexeme is int iVal)
                {
                    eType = EType.Int32;
                    value = iVal;
                }
                else if (lexeme is uint uiVal)
                {
                    eType = EType.UInt32;
                    value = uiVal;
                }
                else if (lexeme is long lVal)
                {
                    eType = EType.Int64;
                    value = lVal;
                }
                else if (lexeme is ulong ulVal)
                {
                    eType = EType.UInt64;
                    value = ulVal;
                }
                else if (lexeme is float fVal)
                {
                    eType = EType.Float32;
                    value = fVal;
                }
                else if (lexeme is double dVal)
                {
                    eType = EType.Float64;
                    value = dVal;
                }
                else
                {
                    eType = EType.Num;
                    value = lexeme;
                }
            }
            else if (tt == ETokenType.String)
            {
                var cdlist = m_FileMetaConstValueTerm.token.childrenTokensList;
                if( cdlist.Count == 1 && (cdlist[0].Count == 1 && cdlist[0][0].type == ETokenType.String ) )
                {
                    var s = cdlist[0][0].lexeme.ToString();
                    value = s;
                    stringRef = StringPool.AddString(s);
                    return;
                }
                else
                {
                    //解析 string里边的${} $name 这种的提取
                    for (int i = 0; i < cdlist.Count; i++)
                    {
                        Node node = new Node(null);
                        TokenParse tp = new TokenParse( null, cdlist[i]);
                        tp.BuildStruct();

                        List<Node> nodeList = tp.rootNode.childList;
                        if (nodeList.Count > 0)
                        {
                            List<Node> expressNodeList = StructParse.HandleNodeSingleLine(nodeList);
                            //var elnd = nodeList[nodeList.Count - 1].extendLinkNodeList;
                            //for ( int j = 0; j < elnd.Count; j++ )
                            //{
                            //    expressNodeList.Add(elnd[i]);
                            //}

                            var filemetaExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMetaConstValueTerm.fileMeta, expressNodeList, FileMetaTermExpress.EExpressType.Common);

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
                        m_ConvertCallExpressNode = true;
                    }

                }
            }
        }
        private void Parse1(EType _etype, object val)
        {
            switch (eType)
            {
                case EType.Boolean:
                    {
                        value = val.ToString() == "true";
                    }
                    break;
                case EType.Null:
                    {
                        value = "null";
                    }
                    break;
                default:
                    {
                        value = val;
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
                else if(m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.numMetaClass )
                {
                    eType = EType.Num;
                }
                else if (m_ExpressReturnMetaType.metaClass == CoreMetaClassManager.stringMetaClass)
                {
                    eType = EType.String;
                    value = value.ToString();
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
            StringBuilder sb = new StringBuilder();
            if (m_FileMetaConstValueTerm != null)
            {
                sb.Append(m_FileMetaConstValueTerm.ToTokenString());
            }
            return sb.ToString();
        }
    }
}
