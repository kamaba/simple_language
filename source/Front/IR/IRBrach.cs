//****************************************************************************
//  File:      IRBrach.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using System.Text;

namespace SimpleLanguage.IR
{
    public class IRBranch : IRBase
    {
        public IRData data = new IRData();
        public IRBranch( IRMethod _irMethod, EIROpCode type, IRData brIRData ) :base(_irMethod)
        {
            data.opCode = type;
            data.SetOpValue(brIRData);
            AddIRData( data );
        }
        public void SetOpValue(IRData opValue)
        {
            data.opValue = opValue;
        }
        public void SetDebugInfoByToken(Token token, string info = null)
        {
            data.SetDebugInfoByToken(token, info);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToIRString());

            return sb.ToString();
        }
    }
    public class IRLabel : IRBase
    {
        public IRData data = new IRData();
        /// <summary>
        /// goto/label 语句 IR。
        /// isLabelStatement=true  -> label 语句: 直接发射目标 IRData 实例本身(OpCode.Label, VM 中为 no-op)，
        ///                            与 goto 的 BrLabel.opValue 保持同一引用，保证回填阶段 FindIndex 命中。
        /// isLabelStatement=false -> goto 语句: 发射 BrLabel 无条件跳转指令，opValue 指向目标 Label IRData，
        ///                            IRMethod.Parse() 回填阶段按引用求目标指令索引并嵌入 payload。
        /// </summary>
        public IRLabel(IRMethod _irMethod, IRData targetIRData, bool isLabelStatement, Token token = null, string info = null) : base(_irMethod)
        {
            if (targetIRData == null)
                return;
            if (isLabelStatement)
            {
                data = targetIRData;
            }
            else
            {
                data = new IRData();
                data.opCode = EIROpCode.BrLabel;
                data.SetOpValue(targetIRData);
            }
            if (token != null)
            {
                data.SetDebugInfoByToken(token, info);
            }
            // 关键: 必须加入 IRDataList, 否则指令不会进入最终指令序列(此前的 bug)
            AddIRData(data);
        }
        public void SetDebugInfoByToken(Token token, string info = null)
        {
            data.SetDebugInfoByToken(token, info);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("labal");
            sb.Append(base.ToIRString());

            return sb.ToString();
        }
    }
}
