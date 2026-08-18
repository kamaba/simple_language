//****************************************************************************
//  MapSystemMethodCall: Map 原生容器操作的 VM 处理器。
//
//  系统调用:
//    SystemMapIndexOfKey(list, key, length) - 遍历 Map 内部数组前 length 个元素，
//      对每个 MapEntity 读取其 "key" 成员，用完整 == 语义（含类键 _eq_ 回调）
//      与 key 比较，返回首个匹配下标（-1 未找到）。
//****************************************************************************

using System;
using System.Diagnostics;
using System.Globalization;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM.MemoryManagement;

namespace SimpleLanguage.VM.Runtime
{
    internal static class MapSystemMethodCall
    {
        /// <summary>
        /// SystemMapIndexOfKey(list, key, length): 遍历 Map 内部数组的前 length 个元素，
        /// 对每个 MapEntity 读取其 "key" 成员，用完整 == 语义与 key 比较，
        /// 返回首个匹配下标（-1 未找到）。对应 SL 层 indexOfKey 的遍历逻辑。
        /// </summary>
        public static void ExecuteSystemMapIndexOfKey(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMapIndexOfKey stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            var keyArg = args[1];

            int length = 0;
            var lenArg = args[2];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            var outv = default(RuntimeValue);
            outv.SetInt32Value(-1);

            if (arrObj != null)
            {
                if (length < 0) length = 0;
                if (length > arrObj.length) length = arrObj.length;

                // 所有元素同类型（MapEntity），只需解析一次 "key" 成员下标。
                int keyMemberIndex = -1;
                bool keyIndexResolved = false;

                for (int i = 0; i < length; i++)
                {
                    var ent = default(RuntimeValue);
                    arrObj.LoadValue(i, ref ent);

                    // 对应 SL: if ent != null && ent.key == key
                    SObject? entObj = ent.GetReferenceSObject(createStringRef: false);
                    if (entObj == null)
                        continue;

                    if (!keyIndexResolved)
                    {
                        keyMemberIndex = ResolveKeyMemberIndex(entObj);
                        keyIndexResolved = true;
                        if (keyMemberIndex < 0)
                            break; // 无法解析 key 成员，保持 -1 返回
                    }
                    if (keyMemberIndex < 0)
                        continue;

                    // 读取 ent.key
                    var entKey = default(RuntimeValue);
                    entObj.GetMemberVariableSValue(keyMemberIndex, ref entKey);

                    // 用完整 == 语义比较（支持类键的 _eq_/_ne_ 回调，
                    // 复用 ExecuteEqualityOperation 的 byte-stack 弹栈逻辑）
                    if (vm.TryRuntimeValueEqual(entKey, keyArg, true))
                    {
                        outv.SetInt32Value(i);
                        break;
                    }
                }
            }

            vm.PushSValueSynced(outv);
        }

        /// <summary>
        /// 在 MapEntity SObject 中按名查找 "key" 成员的下标。
        /// 成员名可能为裸名 "key" 或带类型前缀如 "MapEntity.key"。
        /// </summary>
        private static int ResolveKeyMemberIndex(SObject entObj)
        {
            var rc = entObj.runtimeClass;
            if (rc == null)
                return -1;
            var members = rc.nonStaticIRMetaVariableList;
            if (members == null)
                return -1;
            for (int j = 0; j < members.Count; j++)
            {
                string? nm = members[j].name;
                if (string.IsNullOrEmpty(nm))
                    continue;
                if (nm == "key" || nm.EndsWith(".key"))
                    return members[j].index;
            }
            return -1;
        }
    }
}
