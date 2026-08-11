//****************************************************************************
//  ListSystemMethodCall: List<T> 原生容器操作的 VM 处理器。
//
//  List 对象自身是普通 SObject，VM 通过 s_listStorage 字典为每个 List
//  关联一个内部 ArrayObject 作为元素存储。
//
//  系统调用:
//    SystemListInit(this, capacity)         - 初始化内部存储
//    SystemListGetValueThis(this, index)   - 读取元素
//    SystemListSetValueThis(this, index, v)- 写入元素
//    SystemListGetCapacity(this)           - 获取内部容量
//    SystemListSetCapacity(this, newCap)   - 扩缩容
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using SimpleLanuageVM.Load;
using SimpleLanguage.Logging;
using SimpleLanguage.VM.MemoryManagement;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ListSystemMethodCall
    {
        /// <summary>每个 List SObject 对应的内部 ArrayObject 存储。</summary>
        private static readonly Dictionary<SObject, ArrayObject> s_listStorage = new();

        #region Public dispatch
        public static void ExecuteListInit(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListInit stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null) return;

            int capacity = 0;
            try { capacity = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { capacity = 0; }
            if (capacity < 0) capacity = 0;

            // 获取 List 的 RuntimeType，从中提取模板参数 T 的 RuntimeType
            var rt = listObj.runtimeType;
            if (rt?.runtimeTemplateList == null || rt.runtimeTemplateList.Count == 0)
            {
                Log.AddRuntimeLog(LID.ShowMessageWarning, "SystemListInit: List has no template type, cannot create storage");
                return;
            }

            // 创建内部 ArrayObject 作为元素存储
            var elementRt = rt.runtimeTemplateList[0];
            var arrObj = new ArrayObject(elementRt, capacity);
            arrObj.CreateObject();
            ObjectManager.RegisterObject(arrObj);

            s_listStorage[listObj] = arrObj;
        }

        public static void ExecuteListGetValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListGetValueThis stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            var sv = default(RuntimeValue);

            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var arrObj))
            {
                sv.SetNull();
                vm.PushSValueSynced(sv);
                return;
            }

            int index = 0;
            try { index = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            arrObj.LoadValue(index, ref sv);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteListSetValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListSetValueThis stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var arrObj))
                return;

            int index = 0;
            var idxArg = args[1];
            idxArg.TryNormalizeObjectScalarInPlace();
            try { index = Convert.ToInt32(idxArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            var value = args[2];
            var elementRt = arrObj.runtimeType?.runtimeTemplateList;
            if (elementRt != null && elementRt.Count > 0)
            {
                var targetEvm = elementRt[0].eType;
                value.TryNormalizeObjectScalarInPlace();
                value.TryCoerceScalarForAssignment(targetEvm);
            }

            arrObj.StoreValue(index, value);
        }

        public static void ExecuteListGetCapacity(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListGetCapacity stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            int capacity = 0;

            if (listObj != null && s_listStorage.TryGetValue(listObj, out var arrObj))
            {
                capacity = arrObj.length;
            }

            var sv = default(RuntimeValue);
            sv.SetInt32Value(capacity);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteListSetCapacity(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListSetCapacity stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var oldArrObj))
                return;

            int newCapacity = 0;
            try { newCapacity = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { newCapacity = 0; }
            if (newCapacity < 0) newCapacity = 0;

            int oldLen = oldArrObj.length;
            if (newCapacity == oldLen) return;

            // 创建新的 ArrayObject 并拷贝元素
            var elementRt = oldArrObj.runtimeType?.runtimeTemplateList?[0];
            if (elementRt == null) return;

            var newArrObj = new ArrayObject(elementRt, newCapacity);
            newArrObj.CreateObject();
            ObjectManager.RegisterObject(newArrObj);

            int copyCount = Math.Min(oldLen, newCapacity);
            for (int i = 0; i < copyCount; i++)
            {
                var val = default(RuntimeValue);
                oldArrObj.LoadValue(i, ref val);
                newArrObj.StoreValue(i, val);
            }

            s_listStorage[listObj] = newArrObj;
        }

        /// <summary>清除所有 List 存储（VM 重置时调用）。</summary>
        public static void Clear()
        {
            s_listStorage.Clear();
        }
        #endregion

        #region Remove / Clear operations

        /// <summary>
        /// SystemListRemoveValueThis(this, item): 在内部存储中查找第一个等于 item 的元素，
        /// 将其后所有元素左移一位覆盖，末尾清空。SL 层负责递减 _length。
        /// </summary>
        public static void ExecuteListRemoveValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListRemoveValueThis stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var arrObj))
                return;

            var targetValue = args[1];

            for (int i = 0; i < arrObj.length; i++)
            {
                var element = default(RuntimeValue);
                arrObj.LoadValue(i, ref element);

                // 比较 element 与 targetValue 是否相等
                var cmpLeft = element;
                var cmpRight = targetValue;
                RuntimeValueMethod.CompareEuqalSValue1AndValue2(ref cmpLeft, ref cmpRight, true, out _);
                if (cmpLeft.eType == EVMType.Boolean && cmpLeft.int8Value != 0)
                {
                    // 找到匹配，左移后续元素
                    ShiftLeft(arrObj, i);
                    return;
                }
            }
        }

        /// <summary>
        /// SystemListRemoveIndexValueThis(this, index): 将 index 之后所有元素左移一位覆盖，
        /// 末尾清空。SL 层负责递减 _length。
        /// </summary>
        public static void ExecuteListRemoveIndexValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListRemoveIndexValueThis stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var arrObj))
                return;

            int index = 0;
            try { index = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            if (index < 0 || index >= arrObj.length)
                return;

            ShiftLeft(arrObj, index);
        }

        /// <summary>
        /// SystemListClearValueThis(this): 清空内部存储所有元素（置 null/default），
        /// 但保留容量不变。SL 层负责重置 _length。
        /// </summary>
        public static void ExecuteListClearValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemListClearValueThis stack underflow, need={pc}");
                return;
            }

            var listObj = args[0].sobject;
            if (listObj == null || !s_listStorage.TryGetValue(listObj, out var arrObj))
                return;

            var nullVal = default(RuntimeValue);
            nullVal.SetNull();
            for (int i = 0; i < arrObj.length; i++)
            {
                arrObj.StoreValue(i, nullVal);
            }
        }

        /// <summary>
        /// 将 arrObj 中从 startIndex+1 开始的元素左移一位，覆盖 startIndex，
        /// 末尾位置（length-1）清空。
        /// </summary>
        private static void ShiftLeft(ArrayObject arrObj, int startIndex)
        {
            int last = arrObj.length - 1;
            for (int i = startIndex; i < last; i++)
            {
                var val = default(RuntimeValue);
                arrObj.LoadValue(i + 1, ref val);
                arrObj.StoreValue(i, val);
            }
            // 清空末尾
            if (last >= 0)
            {
                var nullVal = default(RuntimeValue);
                nullVal.SetNull();
                arrObj.StoreValue(last, nullVal);
            }
        }

        #endregion
    }
}
