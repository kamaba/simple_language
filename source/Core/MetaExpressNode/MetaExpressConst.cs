//****************************************************************************
//  File:      MetaExpressConst.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/18 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;

namespace SimpleLanguage.Core
{
    public sealed class MetaConstExpressNode : MetaExpressNode
    {
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
            if (left.opLevel > right.opLevel)
            {
                left.ComputeMinusRight(right);
                return left;
            }
            else
            {
                right.ComputeMinusRight(left);
                return right;
            }
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
            if (left.opLevel > right.opLevel)
            {
                left.ComputeDivRight(right);
                return left;
            }
            else
            {
                right.ComputeDivRight(left);
                return right;
            }
        }
        public static MetaConstExpressNode operator %(MetaConstExpressNode left, MetaConstExpressNode right)
        {
            if (left.opLevel > right.opLevel)
            {
                left.ComputeModRight(right);
                return left;
            }
            else
            {
                right.ComputeModRight(left);
                return right;
            }
        }

        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;
        public object value { get; set; } = null;
        public EType eType { get; private set; } = EType.None;
        public MetaConstExpressNode(FileMetaConstValueTerm fmct)
        {
            m_FileMetaConstValueTerm = fmct;

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
        private void Parse1(EType _etype, object val)
        {
            eType = _etype;
            switch (eType)
            {
                case EType.Boolean:
                    {
                        value = val.ToString() == "true";
                    }
                    break;
                default:
                    {
                        value = val;
                    }
                    break;
            }
        }
        public override void CalcReturnType()
        {
            if (m_MetaDefineType != null)
            {
                if( m_MetaDefineType.metaClass == CoreMetaClassManager.nullMetaClass  )
                {
                    eType = EType.Null;
                    value = "null";
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.booleanMetaClass)
                {
                    eType = EType.Boolean;
                    value = Convert.ToBoolean(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.byteMetaClass)
                {
                    eType = EType.Byte;
                    value = Convert.ToByte(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.sbyteMetaClass)
                {
                    eType = EType.SByte;
                    value = Convert.ToSByte(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.int16MetaClass)
                {
                    eType = EType.Int16;
                    value = Convert.ToInt16(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.uint16MetaClass)
                {
                    eType = EType.UInt16;
                    value = Convert.ToUInt16(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.int32MetaClass)
                {
                    eType = EType.Int32;
                    value = Convert.ToInt32(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.uint32MetaClass)
                {
                    eType = EType.UInt32;
                    value = Convert.ToUInt32(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.int64MetaClass)
                {
                    eType = EType.Int64;
                    value = Convert.ToInt64(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.uint64MetaClass)
                {
                    eType = EType.UInt64;
                    value = Convert.ToUInt64(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.float32MetaClass)
                {
                    eType = EType.Float32;
                    value = Convert.ToSingle(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.float64MetaClass)
                {
                    eType = EType.Float64;
                    value = Convert.ToDouble(value);
                }
                else if (m_MetaDefineType.metaClass == CoreMetaClassManager.stringMetaClass)
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
                m_MetaDefineType = new MetaType(CoreMetaClassManager.nullMetaClass);
            }
            else
            {
                MetaClass mc = CoreMetaClassManager.GetMetaClassByEType(eType);

                if (mc == null)
                {
                    m_MetaDefineType = new MetaType(CoreMetaClassManager.objectMetaClass);
                }
                else
                {
                    MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
                    if (eType == EType.Array)
                    {
                        MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                        mitc.AddMetaTemplateParamsList(mitp);
                        m_MetaDefineType = new MetaType(mc, null, mitc);
                    }
                    else
                    {
                        m_MetaDefineType = new MetaType(mc);
                    }
                }
            }
        }
        public void ComputeAddRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
            }
        }
        public void ComputeMinusRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
            }
        }
        public void ComputeMulRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
            }
        }
        public void ComputeDivRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
            }
        }
        public void ComputeModRight(MetaConstExpressNode right)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
                case EType.String:
                    {
                        value = value.ToString() + (string)right.value;
                    }
                    break;
            }
        }
        public void ComputeEqualComputeRight(MetaConstExpressNode right, ELeftRightOpSign opSign)
        {
            switch (right.eType)
            {
                case EType.Byte:
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
        public override string ToTokenString()
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
