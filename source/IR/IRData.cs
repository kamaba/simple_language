//****************************************************************************
//  File:      IRData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;

using System;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRData
    {
        public int       id = 0;
        public EIROpCode opCode;                 //指令类型
        // 指令原始对象值（兼容旧代码）
        public object    opValue;                //指令值
        // 序列化后的原始数据（仅用于值类型或需要内嵌的常量）
        public byte[]    Payload = null;
        // 当前 IRData 的字节长度（包括 Payload 的长度）——用于导出序列化时参考
        public int       ByteLength = 0;
        public int       index;                  //索引
        public DebugInfo debugInfo;              //调试信息

        public IRData()
        {

        }

        // 设置 opValue 并尝试将值类型序列化到 Payload，同时更新 ByteLength
        public void SetOpValue(object v)
        {
            opValue = v;
            PackOpValue();
            UpdateByteLength();
        }

        // 将基础值类型序列化到 Payload（仅支持常见原始类型和字符串）
        private void PackOpValue()
        {
            Payload = null;
            if (opValue == null) return;

            switch (Type.GetTypeCode(opValue.GetType()))
            {
                case TypeCode.Boolean:
                    Payload = BitConverter.GetBytes((bool)opValue);
                    break;
                case TypeCode.Byte:
                    Payload = new byte[] { (byte)opValue };
                    break;
                case TypeCode.SByte:
                    Payload = new byte[] { (byte)((sbyte)opValue) };
                    break;
                case TypeCode.Int16:
                    Payload = BitConverter.GetBytes((short)opValue);
                    break;
                case TypeCode.UInt16:
                    Payload = BitConverter.GetBytes((ushort)opValue);
                    break;
                case TypeCode.Int32:
                    Payload = BitConverter.GetBytes((int)opValue);
                    break;
                case TypeCode.UInt32:
                    Payload = BitConverter.GetBytes((uint)opValue);
                    break;
                case TypeCode.Int64:
                    Payload = BitConverter.GetBytes((long)opValue);
                    break;
                case TypeCode.UInt64:
                    Payload = BitConverter.GetBytes((ulong)opValue);
                    break;
                case TypeCode.Single:
                    Payload = BitConverter.GetBytes((float)opValue);
                    break;
                case TypeCode.Double:
                    Payload = BitConverter.GetBytes((double)opValue);
                    break;
                case TypeCode.String:
                    {
                        var b = Encoding.UTF8.GetBytes((string)opValue);
                        // store raw bytes (no length prefix here; exporter can add metadata)
                        Payload = b;
                    }
                    break;
                default:
                    // 不对复杂类型序列化，保持 Payload=null
                    Payload = null;
                    break;
            }
        }

        // 更新 ByteLength（仅计算 Payload 长度，未来可扩展为包含头部字段）
        public void UpdateByteLength()
        {
            ByteLength = (Payload != null) ? Payload.Length : 0;
        }
        public void SetDebugInfoByValue( DebugInfo info )
        {
            debugInfo = info;
        }
        public void SetDebugInfoByToken( Token token )
        {
            if(token != null )
            {
                debugInfo.path = token.path;
                debugInfo.name = token.lexeme?.ToString();
                debugInfo.beginLine = token.sourceBeginLine;
                debugInfo.beginChar = token.sourceBeginChar;
                debugInfo.endLine = token.sourceEndLine;
                debugInfo.endChar = token.sourceEndChar;
            }
        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            m_StringBuilder.Append( id + "   [ " + debugInfo.path + ":" + debugInfo.beginLine.ToString() + "]" + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (opValue != null)
            {
                MetaType mt = opValue as MetaType;
                IRMethod irm = opValue as IRMethod;
                if ( opValue.GetType() == typeof( Int32 ) )
                {
                    m_StringBuilder.Append(" val: int32[" + opValue + "] ");
                }
                else
                {
                    if (mt != null)
                    {
                        m_StringBuilder.Append(" val mt:[" + mt.name + "] ");
                    }
                    else if( irm != null )
                    {
                        m_StringBuilder.Append(" val irm:[" + irm.id + "] ");
                    }
                    else
                    {
                        m_StringBuilder.Append(" val:[" + opValue.ToString() + "] ");
                    }
                }
            }
            return m_StringBuilder.ToString();
        }
    }
}
