using System;
using System.Collections.Generic;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.SLVM
{
    public static class SLVMLoader
    {
        // Read .slvm file and convert a SLVM method into IRData[] so RuntimeVM can execute it
        public static IRData[] ConvertSLVMMethodToIRDataList(string path, string methodId)
        {
            var module = SLVMSerializer.ReadModule(path);
            if (module == null) return null;
            var m = module.GetMethodById(methodId);
            if (m == null) return null;

            var list = new List<IRData>();
            for (int i = 0; i < m.instructions.Count; i++)
            {
                var ins = m.instructions[i];
                var d = new IRData();
                d.id = i;
                // parse opcode string to EIROpCode
                try
                {
                    d.opCode = (EIROpCode)Enum.Parse(typeof(EIROpCode), ins.opcode);
                }
                catch
                {
                    d.opCode = EIROpCode.Nop;
                }
                d.index = ins.index;
                // restore opValue from string pool when opValueIndex provided
                if (ins.opValueIndex >= 0 && ins.opValueIndex < module.stringPool.Count)
                {
                    d.opValue = module.stringPool[ins.opValueIndex];
                }
                else
                {
                    d.opValue = ins.opValue;
                }
                if (ins.payload != null && ins.payload.Length > 0)
                {
                    d.Payload = ins.payload;
                }
                // also set opValue when payload represents a string
                if (ins.payload != null && ins.payload.Length > 0 && ins.payloadType == SLVMPayloadType.String && (d.opValue == null || d.opValue.ToString() == ""))
                {
                    d.opValue = System.Text.Encoding.UTF8.GetString(ins.payload);
                }
                d.UpdateByteLength();
                list.Add(d);
            }
            return list.ToArray();
        }
    }
}
