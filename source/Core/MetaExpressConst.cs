using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class MetaConstExpressNode : MetaExpressNode
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
        public MetaConstExpressNode(FileMetaConstValueTerm fmct )
        {
            m_FileMetaConstValueTerm = fmct;

            Parse1(fmct.token.GetEType(), fmct.token.lexeme);
        }
        public MetaConstExpressNode(EType etype, object val)
        {
            Parse1(etype, val);
        }
        private void Parse1(EType _etype, object val)
        {
            eType = _etype;
            switch( eType )
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
        public override MetaType GetReturnMetaDefineType()
        {
            if (m_MetaDefineType != null)
            {
                return m_MetaDefineType;
            }
            //MetaType mdt = null;
            if( eType == EType.Null )
            {
                m_MetaDefineType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            else
            {
                MetaClass mc = CoreMetaClassManager.GetMetaClassByEType(eType);
                MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
                if (eType == EType.Array)
                {
                    MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                    mitc.AddMetaTemplateParamsList(mitp);
                    m_MetaDefineType = new MetaType(mc, mitc);
                }
                else
                {
                    m_MetaDefineType = new MetaType(mc);
                }
            }
            return m_MetaDefineType;
        }
        public override Token GetToken() { return m_FileMetaConstValueTerm?.token; }
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
        public void ComputeEqualComputeRight(MetaConstExpressNode right, ELeftRightOpSign opSign )
        {
            switch (right.eType)
            {
                case EType.Byte:
                    switch(opSign)
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
                            value = (UInt32)value == (UInt32)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (UInt32)value != (UInt32)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (UInt32)value > (UInt32)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (UInt32)value >= (UInt32)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (UInt32)value < (UInt32)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (UInt32)value <= (UInt32)right.value;
                            break;
                    }
                    break;
                case EType.Int64:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (Int64)value == (Int64)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (Int64)value != (Int64)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (Int64)value > (Int64)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (Int64)value >= (Int64)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (Int64)value < (Int64)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (Int64)value <= (Int64)right.value;
                            break;
                    }
                    break;
                case EType.UInt64:
                    switch (opSign)
                    {
                        case ELeftRightOpSign.Equal:
                            eType = EType.Boolean;
                            value = (UInt64)value == (UInt64)right.value;
                            break;
                        case ELeftRightOpSign.NotEqual:
                            eType = EType.Boolean;
                            value = (UInt64)value != (UInt64)right.value;
                            break;
                        case ELeftRightOpSign.Greater:
                            eType = EType.Boolean;
                            value = (UInt64)value > (UInt64)right.value;
                            break;
                        case ELeftRightOpSign.GreaterOrEqual:
                            eType = EType.Boolean;
                            value = (UInt64)value >= (UInt64)right.value;
                            break;
                        case ELeftRightOpSign.Less:
                            eType = EType.Boolean;
                            value = (UInt64)value < (UInt64)right.value;
                            break;
                        case ELeftRightOpSign.LessOrEqual:
                            eType = EType.Boolean;
                            value = (UInt64)value <= (UInt64)right.value;
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
                                Console.WriteLine("Error Not Support string < <= > >=sign operator!!");
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
            switch(eType)
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
                case EType.Char:
                    {
                        str = "\'" + value.ToString() + "\'";
                    }
                    break;
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
                case EType.Float:
                    {
                        signEn = "f";
                    }
                    break;
                case EType.Double:
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
            if(m_FileMetaConstValueTerm!= null )
            {
                sb.Append(m_FileMetaConstValueTerm.ToTokenString());
            }
            return sb.ToString();
        }        
    }
}
