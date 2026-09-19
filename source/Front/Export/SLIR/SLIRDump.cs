using System;
using System.IO;
using System.Text;

namespace SimpleLanguage.Export.SLIR
{
    public static class SLIRDump
    {
        public static void DumpToText(string slirPath, string outputTxt)
        {
            var m = SLIRReader.ReadModule(slirPath);

            var sb = new StringBuilder();
            sb.AppendLine($"SLIR dump: {Path.GetFileName(slirPath)}");
            sb.AppendLine($"Classes: {m.Classes.Count}");
            sb.AppendLine($"Methods: {m.Methods.Count}");
            if (m.StringPool.Count > 0 || m.TypeTable.Count > 0)
            {
                sb.AppendLine($"StringPool: {m.StringPool.Count}");
                sb.AppendLine($"TypeTable: {m.TypeTable.Count}");
            }
            sb.AppendLine();

            foreach (var c in m.Classes)
            {
                sb.AppendLine($"class {c.AllName} extends {c.BaseAllName}");
                if (!string.IsNullOrEmpty(c.SourcePath))
                    sb.AppendLine($"  path: {c.SourcePath}");
                if (c.Kind != 0)
                    sb.AppendLine($"  kind: {c.Kind}");
                sb.AppendLine($"  flags: template={c.IsTemplate} interface={c.IsInterface} abstract={c.IsAbstract}");
                sb.AppendLine($"  fields({c.Fields.Count}):");
                foreach (var f in c.Fields)
                {
                    sb.AppendLine($"    {(f.IsStatic ? "static " : "")}{f.TypeName} {f.Name} {(f.IsConst ? "const" : "")} perm={f.Permission} idx={f.Index}");
                }
                sb.AppendLine($"  functions({c.Functions.Count}):");
                foreach (var fn in c.Functions)
                {
                    sb.AppendLine($"    {(fn.IsStatic ? "static " : "")} {fn.AllName} perm={fn.Permission}");
                }
                sb.AppendLine();
            }

            foreach (var meth in m.Methods)
            {
                sb.AppendLine($"method {meth.Id} (only={meth.OnlyName}) args={meth.ArgumentCount} locals={meth.LocalCount} rets={meth.ReturnCount}");
                for (int i = 0; i < meth.Instructions.Count; i++)
                {
                    var ins = meth.Instructions[i];
                    sb.AppendLine($"  [{i}] op={ins.OpCode} payloadLen={ins.Payload.Length}");
                }
                sb.AppendLine();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputTxt) ?? ".");
            File.WriteAllText(outputTxt, sb.ToString());
        }
    }
}
