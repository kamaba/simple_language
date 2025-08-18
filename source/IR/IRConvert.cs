//****************************************************************************
//  File:      IRConvert.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: type's convert about
//****************************************************************************

using SimpleLanguage.Compile;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRConvert : IRBase
    {
        public IRData data = new IRData();
        public IRConvert( IRMethod _irMethod, EType ori, EType target ) : base(_irMethod)
        {
            EIROpCode irop = EIROpCode.Nop;
            switch ( target )
            {
                case EType.Byte:
                    irop = EIROpCode.Convert_I8;
                    break;
                case EType.SByte:
                    irop = EIROpCode.Convert_SI8;
                    break;
                case EType.Int16:
                    irop = EIROpCode.Convert_I16;
                    break;
                case EType.UInt16:
                    irop = EIROpCode.Convert_UI16;
                    break;
                case EType.Int32:
                    irop = EIROpCode.Convert_I32;
                    break;
                case EType.UInt32:
                    irop = EIROpCode.Convert_UI32;
                    break;
                case EType.Int64:
                    irop = EIROpCode.Convert_I64;
                    break;
                case EType.UInt64:
                    irop = EIROpCode.Convert_UI64;
                    break;
                case EType.Float32:
                    irop = EIROpCode.Convert_R4;
                    break;
                case EType.Float64:
                    irop = EIROpCode.Convert_R8;
                    break;
            }
            data.opCode = irop;
            AddIRData( data );
        }
        public void SetOpValue(IRData opValue)
        {
            data.opValue = opValue;
        }
        public void SetDebugInfoByToken(Token token)
        {
            data.SetDebugInfoByToken(token);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToIRString());

            return sb.ToString();
        }
    }
}
