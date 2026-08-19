//****************************************************************************
//  MapSystemMethodCall: Map 原生容器操作的 VM 处理器。
//
//  系统调用:
//    SystemMapIndexOfKey(list, key, length) - 遍历 Map 内部数组前 length 个元素，
//      对每个 MapEntity 读取其 "key" 成员，用完整 == 语义（含类键 _eq_ 回调）
//      与 key 比较，返回首个匹配下标（-1 未找到）。
//    SystemMapFindEntry(entries, buckets, key, hash, bucket) - 哈希表查找：
//      从 buckets[bucket] 读取链头（值=index+1, 0=空桶），遍历 entries 桶链，
//      对每个 MapEntity 比较 hashId 与 key（完整 == 语义），返回匹配下标（-1 未找到）。
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
                        keyMemberIndex = ResolveMemberIndex(entObj, "key");
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
        /// SystemMapFindEntry(entries, buckets, key, hash, bucket): 哈希表查找。
        /// 从 buckets[bucket] 读取链头（值=entryIndex+1, 0=空桶），遍历 entries 中的桶链，
        /// 对每个 MapEntity 先比较 hashId（快速跳过不匹配的），再比较 key（完整 == 语义），
        /// 返回首个匹配下标（-1 未找到）。对应 SL 层 findEntry 的 while 循环逻辑。
        /// </summary>
        public static void ExecuteSystemMapFindEntry(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 5 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMapFindEntry stack underflow, need={pc}");
                return;
            }

            var entriesObj = args[0].sobject as ArrayObject;
            var bucketsObj = args[1].sobject as ArrayObject;
            var keyArg = args[2];

            int hash = 0;
            var hashArg = args[3];
            hashArg.TryNormalizeObjectScalarInPlace();
            try { hash = Convert.ToInt32(hashArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { hash = 0; }

            int bucket = 0;
            var bucketArg = args[4];
            bucketArg.TryNormalizeObjectScalarInPlace();
            try { bucket = Convert.ToInt32(bucketArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { bucket = 0; }

            var outv = default(RuntimeValue);
            outv.SetInt32Value(-1);

            if (entriesObj == null || bucketsObj == null)
            {
                vm.PushSValueSynced(outv);
                return;
            }

            // 读取桶头：buckets[bucket] 存储 entryIndex + 1（0 = 空桶）
            if (bucket < 0 || bucket >= bucketsObj.length)
            {
                vm.PushSValueSynced(outv);
                return;
            }

            var bucketVal = default(RuntimeValue);
            bucketsObj.LoadValue(bucket, ref bucketVal);
            bucketVal.TryNormalizeObjectScalarInPlace();
            int bucketHead = 0;
            try { bucketHead = Convert.ToInt32(bucketVal.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { bucketHead = 0; }

            int entryIndex = bucketHead - 1;

            // 解析成员下标（所有 MapEntity 同类型，只需解析一次）
            int hashIdMemberIndex = -1;
            int keyMemberIndex = -1;
            int linkMemberIndex = -1;
            bool membersResolved = false;

            while (entryIndex >= 0 && entryIndex < entriesObj.length)
            {
                var ent = default(RuntimeValue);
                entriesObj.LoadValue(entryIndex, ref ent);

                SObject? entObj = ent.GetReferenceSObject(createStringRef: false);
                if (entObj == null)
                    break;

                if (!membersResolved)
                {
                    hashIdMemberIndex = ResolveMemberIndex(entObj, "hashId");
                    keyMemberIndex = ResolveMemberIndex(entObj, "key");
                    linkMemberIndex = ResolveMemberIndex(entObj, "link");
                    membersResolved = true;
                    if (keyMemberIndex < 0)
                        break; // 无法解析 key 成员
                }

                // 读取 ent.hashId 并比较（快速过滤）
                if (hashIdMemberIndex >= 0)
                {
                    var hashVal = default(RuntimeValue);
                    entObj.GetMemberVariableSValue(hashIdMemberIndex, ref hashVal);
                    hashVal.TryNormalizeObjectScalarInPlace();
                    int entHash = 0;
                    try { entHash = Convert.ToInt32(hashVal.GetValueObject(), CultureInfo.InvariantCulture); }
                    catch { entHash = 0; }

                    if (entHash != hash)
                    {
                        // hash 不匹配，跳到链表下一个
                        if (linkMemberIndex < 0)
                            break;
                        var linkVal = default(RuntimeValue);
                        entObj.GetMemberVariableSValue(linkMemberIndex, ref linkVal);
                        linkVal.TryNormalizeObjectScalarInPlace();
                        try { entryIndex = Convert.ToInt32(linkVal.GetValueObject(), CultureInfo.InvariantCulture); }
                        catch { entryIndex = -1; }
                        continue;
                    }
                }

                // hash 匹配，比较 key（完整 == 语义，支持类键 _eq_ 回调）
                var entKey = default(RuntimeValue);
                entObj.GetMemberVariableSValue(keyMemberIndex, ref entKey);
                if (vm.TryRuntimeValueEqual(entKey, keyArg, true))
                {
                    outv.SetInt32Value(entryIndex);
                    break;
                }

                // 不匹配，跳到链表下一个
                if (linkMemberIndex < 0)
                    break;
                var nextLink = default(RuntimeValue);
                entObj.GetMemberVariableSValue(linkMemberIndex, ref nextLink);
                nextLink.TryNormalizeObjectScalarInPlace();
                try { entryIndex = Convert.ToInt32(nextLink.GetValueObject(), CultureInfo.InvariantCulture); }
                catch { entryIndex = -1; }
            }

            vm.PushSValueSynced(outv);
        }

        /// <summary>
        /// 在 SObject 中按名查找成员的下标。
        /// 成员名可能为裸名（如 "key"）或带类型前缀（如 "MapEntity.key"）。
        /// </summary>
        private static int ResolveMemberIndex(SObject entObj, string memberName)
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
                if (nm == memberName || nm.EndsWith("." + memberName))
                    return members[j].index;
            }
            return -1;
        }
    }
}
