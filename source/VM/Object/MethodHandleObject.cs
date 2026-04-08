using System.Reflection;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class MethodHandleObject : SObject
    {
        public MethodInfo MethodInfo { get; private set; }
        public object TargetInstance { get; private set; }
        public RuntimeMethod Method { get; private set; }
        public List<RuntimeType> IRTemplateRuntimeTypes { get; private set; }

        public bool IsCLR => MethodInfo != null;
        public bool IsIR => Method != null;

        public MethodHandleObject(MethodInfo mi, object target = null) : base(EVMType.Object)
        {
            MethodInfo = mi;
            TargetInstance = target;
            SetValue(mi);
        }

        public MethodHandleObject(RuntimeMethod irm, List<RuntimeType> templateTypes = null) : base(EVMType.Object)
        {
            Method = irm;
            IRTemplateRuntimeTypes = templateTypes ?? new List<RuntimeType>();
            SetValue(irm);
        }

        public object Invoke(params object[] args)
        {
            if (IsCLR)
            {
                return MethodInfo.Invoke(TargetInstance, args);
            }
            else if (IsIR)
            {
                // Minimal support: only invoke parameterless or already-wired IR methods.
                // Full argument marshalling into the runtime is not implemented yet.
                try
                {
                    //ICLRRuntimeVM.RunIRMethod(IRTemplateRuntimeTypes ?? new List<RuntimeType>(), IRMethod, true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to invoke IR method handle", ex);
                }
                return null;
            }
            else
            {
                throw new InvalidOperationException("MethodHandle has no target method");
            }
        }

        public override string ToFormatString()
        {
            if (IsCLR) return MethodInfo.ToString();
            if (IsIR) return Method?.ToString() ?? base.ToFormatString();
            return base.ToFormatString();
        }
    }
}
