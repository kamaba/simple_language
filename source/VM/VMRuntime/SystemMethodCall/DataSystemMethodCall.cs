using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using SimpleLanguage.VM;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// Built-in comparisons for <c>data</c> instances (<see cref="RuntimeClass.metaClassKind"/> == 2).
    /// </summary>
    internal static class DataSystemMethodCall
    {
        const int DataMetaClassKind = 2;

        public static void ExecuteDataAllEqual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!TryPopTwoDataOperands(vm, sysPkg, out var d1, out var d2))
            {
                PushBool(vm, false);
                return;
            }

            bool eq = false;
            if (d1.runtimeClass.isDynamicData == d1.runtimeClass.isDynamicData)
            {
                eq = d1.runtimeClass.id == d2.runtimeClass.id;
            }
            else
            {
                eq = MemberDataBuffersEqual(d1, d2);
            }
            PushBool(vm, eq);
        }

        public static void ExecuteDataTypeEqual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!TryPopTwoDataOperands(vm, sysPkg, out var d1, out var d2))
            {
                PushBool(vm, false);
                return;
            }

            PushBool(vm, DataLayoutsShapeEqual(d1, d2));
        }

        public static void ExecuteDataNameAndTypeEqual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!TryPopTwoDataOperands(vm, sysPkg, out var d1, out var d2))
            {
                PushBool(vm, false);
                return;
            }

            PushBool(vm, DataLayoutsNameAndTypeEqual(d1, d2));
        }

        public static void ExecuteDataDataEqual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!TryPopTwoDataOperands(vm, sysPkg, out var d1, out var d2))
            {
                PushBool(vm, false);
                return;
            }

            PushBool(vm, DataValuesEqual(d1, d2, new HashSet<(int, int)>()));
        }

        public static void ExecuteBuildDataString(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemBuildDataString stack underflow, need={pc}");
                return;
            }

            if (TryBuildDataString(ref args[0], out var text))
            {
                var outv = default(RuntimeValue);
                outv.SetStringValue(text);
                vm.PushSValueSynced(outv);
                return;
            }

            var fallback = SystemMethodConvertHelper.ConvertValue(ref args[0], ESystemMethodCall.SystemConvertString);
            vm.PushSValueSynced(fallback);
        }

        public static bool TryBuildDataString(ref RuntimeValue value, out string text)
        {
            text = string.Empty;
            if (value.isNull)
            {
                return false;
            }

            if (value.sobject is TypeObject typeObject && typeObject.currentRT?.runtimeClass?.metaClassKind == DataMetaClassKind)
            {
                text = FormatDataRuntimeType(typeObject.currentRT, new HashSet<int>());
                return true;
            }

            if (value.sobject is ClassObject dataObject && dataObject.runtimeClass?.metaClassKind == DataMetaClassKind)
            {
                text = FormatDataObject(dataObject, new HashSet<int>());
                return true;
            }

            return false;
        }

        private static string FormatDataObject(ClassObject dataObject, HashSet<int> visitPath)
        {
            if (!visitPath.Add(dataObject.hashCode))
                return QuoteJsonString("<cycle>");

            try
            {
                var runtimeClass = dataObject.runtimeClass;
                var fieldList = runtimeClass?.nonStaticIRMetaVariableList;
                if (fieldList == null || fieldList.Count == 0)
                    return "{}";

                var sb = new StringBuilder();
                sb.Append('{');
                for (int i = 0; i < fieldList.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var field = fieldList[i];
                    var memberValue = default(RuntimeValue);
                    ReadInstanceMemberValueByField(dataObject, field, i, fieldList.Count, ref memberValue);

                    sb.Append('"');
                    sb.Append(EscapeJsonString(field?.name ?? string.Empty));
                    sb.Append("\": ");
                    sb.Append(FormatNestedValue(ref memberValue, visitPath));
                }
                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(dataObject.hashCode);
            }
        }

        private static string FormatDataRuntimeType(RuntimeType runtimeType, HashSet<int> visitPath)
        {
            int visitKey = GetRuntimeTypeVisitKey(runtimeType);
            if (!visitPath.Add(visitKey))
                return QuoteJsonString("<cycle>");

            try
            {
                runtimeType.EnsureStaticMemberObjectsInitialized();

                var runtimeClass = runtimeType.runtimeClass;
                var fieldList = runtimeClass?.staticIRMetaVariableList;
                if (fieldList == null || fieldList.Count == 0)
                    return "{}";

                var sb = new StringBuilder();
                sb.Append(runtimeType.runtimeClass.name);
                sb.Append('{');
                for (int i = 0; i < fieldList.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var field = fieldList[i];
                    var memberValue = default(RuntimeValue);
                    ReadStaticMemberValueByField(runtimeType, field, i, fieldList.Count, ref memberValue);

                    sb.Append('"');
                    sb.Append(EscapeJsonString(field?.name ?? string.Empty));
                    sb.Append("\": ");
                    sb.Append(FormatNestedValue(ref memberValue, visitPath));
                }
                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(visitKey);
            }
        }

        private static string FormatArrayObject(ArrayObject arrayObject, HashSet<int> visitPath)
        {
            if (!visitPath.Add(arrayObject.hashCode))
                return QuoteJsonString("<cycle>");

            try
            {
                var sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < arrayObject.length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var itemValue = default(RuntimeValue);
                    arrayObject.LoadValue(i, ref itemValue);
                    sb.Append(FormatNestedValue(ref itemValue, visitPath));
                }
                sb.Append(']');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(arrayObject.hashCode);
            }
        }

        private static string FormatNestedValue(ref RuntimeValue value, HashSet<int> visitPath)
        {
            if (value.isNull)
                return "null";

            if (TryUnwrapObjectReferenceValue(ref value, out var unwrappedValue))
                value = unwrappedValue;

            if (value.sobject is ClassObject dataObject && dataObject.runtimeClass?.metaClassKind == DataMetaClassKind)
                return FormatDataObject(dataObject, visitPath);

            if (value.sobject is ClassObject classObject)
                return FormatClassObject(classObject, visitPath);

            if (value.sobject is TypeObject typeObject && typeObject.currentRT?.runtimeClass?.metaClassKind == DataMetaClassKind)
                return FormatDataRuntimeType(typeObject.currentRT, visitPath);

            if (value.sobject is ArrayObject arrayObject)
                return FormatArrayObject(arrayObject, visitPath);

            switch (value.eType)
            {
                case EVMType.String:
                    return QuoteJsonString(value.stringValue ?? string.Empty);
                case EVMType.Boolean:
                    return value.uint8Value == 1 ? "true" : "false";
                case EVMType.UInt8:
                case EVMType.Int8:
                case EVMType.Int16:
                case EVMType.UInt16:
                case EVMType.Int32:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                case EVMType.Float32:
                case EVMType.Float64:
                case EVMType.Num:
                    return Convert.ToString(value.GetValueObject(), CultureInfo.InvariantCulture) ?? "null";
                default:
                    if (value.eType == EVMType.Object && value.sobject is SObject sobj)
                    {
                        if (sobj.value is SObject nestedObj && !ReferenceEquals(nestedObj, sobj))
                        {
                            var nestedValue = default(RuntimeValue);
                            nestedValue.SetValueBySObject(nestedObj);
                            return FormatNestedValue(ref nestedValue, visitPath);
                        }
                    }

                    var raw = value.GetValueObject();
                    return raw?.ToString() ?? "null";
            }
        }

        private static string FormatClassObject(ClassObject classObject, HashSet<int> visitPath)
        {
            if (classObject.runtimeClass?.metaClassKind == DataMetaClassKind)
                return FormatDataObject(classObject, visitPath);

            if (!visitPath.Add(classObject.hashCode))
                return QuoteJsonString("<cycle>");

            try
            {
                var runtimeClass = classObject.runtimeClass;
                var fieldList = runtimeClass?.nonStaticIRMetaVariableList;
                if (fieldList == null || fieldList.Count == 0)
                    return classObject.ToString();

                var sb = new StringBuilder();
                sb.Append(runtimeClass?.name ?? "Object");
                sb.Append('{');
                for (int i = 0; i < fieldList.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var field = fieldList[i];
                    var memberValue = default(RuntimeValue);
                    ReadInstanceMemberValueByField(classObject, field, i, fieldList.Count, ref memberValue);

                    sb.Append('"');
                    sb.Append(EscapeJsonString(field?.name ?? string.Empty));
                    sb.Append("\": ");
                    sb.Append(FormatNestedValue(ref memberValue, visitPath));
                }
                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(classObject.hashCode);
            }
        }

        private static bool TryUnwrapObjectReferenceValue(ref RuntimeValue value, out RuntimeValue unwrapped)
        {
            unwrapped = value;
            if (value.isNull || value.sobject == null)
                return false;

            var current = value;
            bool changed = false;
            for (int i = 0; i < 16; i++)
            {
                if (current.eType != EVMType.Object)
                    break;

                if (current.sobject is not SObject wrapper)
                    break;

                if (wrapper.value is not SObject inner)
                    break;

                if (ReferenceEquals(inner, wrapper))
                    break;

                current.SetValueBySObject(inner);
                changed = true;
            }

            if (changed)
            {
                unwrapped = current;
                return true;
            }

            return false;
        }

        private static int ResolveFieldSlotIndex(RuntimeVariable? field, int fallbackOrdinal, int maxCount)
        {
            int idx = field?.index ?? fallbackOrdinal;
            if (idx < 0 || idx >= maxCount)
                return fallbackOrdinal;
            return idx;
        }

        private static void ReadInstanceMemberValueByField(
            ClassObject dataObject,
            RuntimeVariable? field,
            int fieldOrdinal,
            int memberCount,
            ref RuntimeValue memberValue)
        {
            int ordinalIndex = ResolveFieldSlotIndex(null, fieldOrdinal, memberCount);
            int variableIndex = ResolveFieldSlotIndex(field, fieldOrdinal, memberCount);

            RuntimeValue byOrdinal = default;
            bool hasOrdinal = dataObject.TryReadMemberDataAsSValue(ordinalIndex, ref byOrdinal);
            if (hasOrdinal)
            {
                memberValue = byOrdinal;
                if (IsValueCompatibleWithField(dataObject.runtimeType, field, ref byOrdinal))
                    return;
            }

            if (variableIndex != ordinalIndex)
            {
                RuntimeValue byVariableIndex = default;
                if (dataObject.TryReadMemberDataAsSValue(variableIndex, ref byVariableIndex))
                {
                    memberValue = byVariableIndex;
                    if (IsValueCompatibleWithField(dataObject.runtimeType, field, ref byVariableIndex))
                        return;
                }
            }

            if (TryReadCompatibleInstanceMemberValue(dataObject, field, memberCount, ordinalIndex, variableIndex, ref memberValue))
                return;

            dataObject.GetMemberVariableSValue(ordinalIndex, ref memberValue);
        }

        private static void ReadStaticMemberValueByField(
            RuntimeType runtimeType,
            RuntimeVariable? field,
            int fieldOrdinal,
            int memberCount,
            ref RuntimeValue memberValue)
        {
            int ordinalIndex = ResolveFieldSlotIndex(null, fieldOrdinal, memberCount);
            int variableIndex = ResolveFieldSlotIndex(field, fieldOrdinal, memberCount);

            RuntimeValue byOrdinal = default;
            runtimeType.GetStaticMemberVariableSValue(ordinalIndex, ref byOrdinal);
            memberValue = byOrdinal;
            if (IsValueCompatibleWithField(runtimeType, field, ref byOrdinal))
                return;

            if (variableIndex != ordinalIndex)
            {
                RuntimeValue byVariableIndex = default;
                runtimeType.GetStaticMemberVariableSValue(variableIndex, ref byVariableIndex);
                memberValue = byVariableIndex;
                if (IsValueCompatibleWithField(runtimeType, field, ref byVariableIndex))
                    return;
            }

            if (TryReadCompatibleStaticMemberValue(runtimeType, field, memberCount, ordinalIndex, variableIndex, ref memberValue))
                return;
        }

        private static bool TryReadCompatibleInstanceMemberValue(
            ClassObject dataObject,
            RuntimeVariable? field,
            int memberCount,
            int excludeIndex1,
            int excludeIndex2,
            ref RuntimeValue memberValue)
        {
            for (int i = 0; i < memberCount; i++)
            {
                if (i == excludeIndex1 || i == excludeIndex2)
                    continue;

                RuntimeValue candidate = default;
                if (!dataObject.TryReadMemberDataAsSValue(i, ref candidate))
                    continue;

                if (!IsValueCompatibleWithField(dataObject.runtimeType, field, ref candidate))
                    continue;

                memberValue = candidate;
                return true;
            }

            return false;
        }

        private static bool TryReadCompatibleStaticMemberValue(
            RuntimeType runtimeType,
            RuntimeVariable? field,
            int memberCount,
            int excludeIndex1,
            int excludeIndex2,
            ref RuntimeValue memberValue)
        {
            for (int i = 0; i < memberCount; i++)
            {
                if (i == excludeIndex1 || i == excludeIndex2)
                    continue;

                RuntimeValue candidate = default;
                runtimeType.GetStaticMemberVariableSValue(i, ref candidate);
                if (!IsValueCompatibleWithField(runtimeType, field, ref candidate))
                    continue;

                memberValue = candidate;
                return true;
            }

            return false;
        }

        private static bool IsValueCompatibleWithField(RuntimeType? ownerRuntimeType, RuntimeVariable? field, ref RuntimeValue value)
        {
            if (ownerRuntimeType == null || field?.runtimeDefType == null)
                return true;

            var expectedRuntimeType = RuntimeVM.GetRuntimeTypeByDefType(
                field.runtimeDefType,
                ownerRuntimeType.runtimeClass,
                ownerRuntimeType.runtimeTemplateList,
                false);
            if (expectedRuntimeType == null)
                return true;

            if (expectedRuntimeType.runtimeClass?.metaClassKind == DataMetaClassKind)
            {
                return value.sobject is ClassObject;
            }

            if (expectedRuntimeType.eType == EVMType.Array)
            {
                return value.sobject is ArrayObject || value.eType == EVMType.Array;
            }

            if (expectedRuntimeType.eType == EVMType.String)
            {
                return value.eType == EVMType.String;
            }

            return true;
        }

        private static string QuoteJsonString(string text)
        {
            return "\"" + EscapeJsonString(text) + "\"";
        }

        private static string EscapeJsonString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(text[i]); break;
                }
            }
            return sb.ToString();
        }

        private static int GetRuntimeTypeVisitKey(RuntimeType runtimeType)
        {
            return int.MinValue + runtimeType.id;
        }

        static void PushBool(RuntimeVM vm, bool value)
        {
            var outv = default(RuntimeValue);
            outv.SetBoolValue(value);
            vm.PushSValueSynced(outv);
        }

        static bool TryPopTwoDataOperands(RuntimeVM vm, SLSystemMethodCallPackage sysPkg, out ClassObject d1, out ClassObject d2)
        {
            d1 = null!;
            d2 = null!;
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"Data compare stack underflow, need={pc}");
                return false;
            }

            if (!TryGetDataInstance(ref args[0], out d1) || !TryGetDataInstance(ref args[1], out d2))
            {
                return false;
            }

            return true;
        }

        static bool TryGetDataInstance(ref RuntimeValue value, out ClassObject dataObject)
        {
            dataObject = null!;
            if (value.isNull)
            {
                return false;
            }

            if (value.sobject is ClassObject co && co.runtimeClass?.metaClassKind == DataMetaClassKind)
            {
                dataObject = co;
                return true;
            }

            return false;
        }

        static bool MemberDataBuffersEqual(ClassObject a, ClassObject b)
        {
            var bufA = a.memberData;
            var bufB = b.memberData;
            if (bufA == null && bufB == null)
            {
                return true;
            }

            if (bufA == null || bufB == null)
            {
                return false;
            }

            return bufA.AsSpan().SequenceEqual(bufB);
        }

        static bool DataLayoutsShapeEqual(ClassObject a, ClassObject b)
        {
            return string.Equals(
                BuildLayoutShapeKey(a),
                BuildLayoutShapeKey(b),
                StringComparison.Ordinal);
        }

        static bool DataLayoutsNameAndTypeEqual(ClassObject a, ClassObject b)
        {
            return string.Equals(
                BuildLayoutNameAndTypeKey(a),
                BuildLayoutNameAndTypeKey(b),
                StringComparison.Ordinal);
        }

        static string BuildLayoutShapeKey(ClassObject dataObject)
        {
            var fields = dataObject.runtimeClass.nonStaticIRMetaVariableList;
            var sb = new StringBuilder();
            sb.Append('{');
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                var field = fields[i];
                sb.Append(field.name);
                sb.Append(':');
                sb.Append(BuildTypeShapeKey(field.runtimeDefType?.runtimeClass != null
                    ? RuntimeVM.GetRuntimeTypeByDefType(
                        field.runtimeDefType,
                        dataObject.runtimeClass,
                        dataObject.runtimeType.runtimeTemplateList,
                        false)
                    : null));
            }
            sb.Append('}');
            return sb.ToString();
        }

        static string BuildLayoutNameAndTypeKey(ClassObject dataObject)
        {
            var fields = dataObject.runtimeClass.nonStaticIRMetaVariableList;
            var sb = new StringBuilder();
            sb.Append('{');
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                var field = fields[i];
                sb.Append(field.name);
                sb.Append(':');
                sb.Append(BuildTypeIdentityKey(field.runtimeDefType?.runtimeClass != null
                    ? RuntimeVM.GetRuntimeTypeByDefType(
                        field.runtimeDefType,
                        dataObject.runtimeClass,
                        dataObject.runtimeType.runtimeTemplateList,
                        false)
                    : null));
            }
            sb.Append('}');
            return sb.ToString();
        }

        static string BuildTypeShapeKey(RuntimeType? rt)
        {
            if (rt == null)
            {
                return "null";
            }

            if (rt.runtimeClass?.metaClassKind == DataMetaClassKind)
            {
                return BuildDataClassShapeFromRuntimeClass(rt.runtimeClass);
            }

            if (IsArrayRuntimeType(rt))
            {
                var elem = rt.runtimeTemplateList != null && rt.runtimeTemplateList.Count > 0
                    ? rt.runtimeTemplateList[0]
                    : null;
                return "array[" + BuildTypeShapeKey(elem) + "]";
            }

            return ScalarShapeCategory(rt.eType);
        }

        static string BuildDataClassShapeFromRuntimeClass(RuntimeClass? rc)
        {
            if (rc == null)
            {
                return "data{}";
            }

            var fields = rc.nonStaticIRMetaVariableList;
            var sb = new StringBuilder();
            sb.Append("data{");
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                var field = fields[i];
                sb.Append(field.name);
                sb.Append(':');
                sb.Append(BuildDefTypeShapeKey(field.runtimeDefType, rc));
            }
            sb.Append('}');
            return sb.ToString();
        }

        static string BuildDefTypeShapeKey(RuntimeDefType? rdt, RuntimeClass ownerClass)
        {
            if (rdt == null)
            {
                return "null";
            }

            var rt = RuntimeVM.GetRuntimeTypeByDefType(rdt, ownerClass, null, false);
            return BuildTypeShapeKey(rt);
        }

        static string BuildTypeIdentityKey(RuntimeType? rt)
        {
            if (rt == null)
            {
                return "null";
            }

            var sb = new StringBuilder();
            sb.Append(rt.runtimeClass?.id ?? 0);
            if (rt.runtimeTemplateList != null && rt.runtimeTemplateList.Count > 0)
            {
                sb.Append('<');
                for (int i = 0; i < rt.runtimeTemplateList.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(BuildTypeIdentityKey(rt.runtimeTemplateList[i]));
                }
                sb.Append('>');
            }

            return sb.ToString();
        }

        static string ScalarShapeCategory(EVMType evmType)
        {
            return evmType switch
            {
                EVMType.Boolean => "bool",
                EVMType.String => "string",
                EVMType.UInt8 or EVMType.Int8 => "byte",
                EVMType.Int16 or EVMType.UInt16 or EVMType.Int32 or EVMType.UInt32
                    or EVMType.Int64 or EVMType.UInt64 or EVMType.Num => "int",
                EVMType.Float32 or EVMType.Float64 => "float",
                _ => "object",
            };
        }

        static bool IsArrayRuntimeType(RuntimeType rt)
        {
            if (rt?.runtimeClass == null)
            {
                return false;
            }

            return rt.runtimeClass.name.Contains("Array", StringComparison.Ordinal)
                || (rt.runtimeTemplateList != null && rt.runtimeTemplateList.Count > 0
                    && rt.eType == EVMType.Class);
        }

        static bool DataValuesEqual(ClassObject a, ClassObject b, HashSet<(int, int)> pairVisit)
        {
            if (!DataFieldNamesAligned(a, b))
            {
                return false;
            }

            var key = (Math.Min(a.hashCode, b.hashCode), Math.Max(a.hashCode, b.hashCode));
            if (!pairVisit.Add(key))
            {
                return true;
            }

            try
            {
                var fieldsA = a.runtimeClass.nonStaticIRMetaVariableList;
                var fieldsB = b.runtimeClass.nonStaticIRMetaVariableList;
                if (fieldsA.Count != fieldsB.Count)
                {
                    return false;
                }

                for (int i = 0; i < fieldsA.Count; i++)
                {
                    var va = default(RuntimeValue);
                    var vb = default(RuntimeValue);
                    a.GetMemberVariableSValue(i, ref va);
                    b.GetMemberVariableSValue(i, ref vb);
                    if (!CompatibleValuesEqual(ref va, ref vb, pairVisit))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                pairVisit.Remove(key);
            }
        }

        static bool DataFieldNamesAligned(ClassObject a, ClassObject b)
        {
            var fieldsA = a.runtimeClass.nonStaticIRMetaVariableList;
            var fieldsB = b.runtimeClass.nonStaticIRMetaVariableList;
            if (fieldsA.Count != fieldsB.Count)
            {
                return false;
            }

            for (int i = 0; i < fieldsA.Count; i++)
            {
                if (!string.Equals(fieldsA[i].name, fieldsB[i].name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        static bool CompatibleValuesEqual(ref RuntimeValue a, ref RuntimeValue b, HashSet<(int, int)> pairVisit)
        {
            a.TryNormalizeObjectScalarInPlace();
            b.TryNormalizeObjectScalarInPlace();

            if (a.isNull && b.isNull)
            {
                return true;
            }

            if (a.isNull || b.isNull)
            {
                return false;
            }

            if (TryGetDataInstance(ref a, out var da) && TryGetDataInstance(ref b, out var db))
            {
                return DataValuesEqual(da, db, pairVisit);
            }

            if (a.sobject is ArrayObject arrA && b.sobject is ArrayObject arrB)
            {
                if (arrA.length != arrB.length)
                {
                    return false;
                }

                for (int i = 0; i < arrA.length; i++)
                {
                    var ea = default(RuntimeValue);
                    var eb = default(RuntimeValue);
                    arrA.LoadValue(i, ref ea);
                    arrB.LoadValue(i, ref eb);
                    if (!CompatibleValuesEqual(ref ea, ref eb, pairVisit))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (IsNumericEvm(a.eType) && IsNumericEvm(b.eType))
            {
                return NumericValuesEqual(ref a, ref b);
            }

            if (a.eType == EVMType.String && b.eType == EVMType.String)
            {
                return string.Equals(a.stringValue, b.stringValue, StringComparison.Ordinal);
            }

            if (a.eType == EVMType.Boolean && b.eType == EVMType.Boolean)
            {
                return a.int8Value == b.int8Value;
            }

            if (ReferenceEquals(a.sobject, b.sobject))
            {
                return true;
            }

            object? av = a.GetValueObject();
            object? bv = b.GetValueObject();
            if (ReferenceEquals(av, bv))
            {
                return true;
            }

            if (av == null || bv == null)
            {
                return false;
            }

            return Equals(av, bv);
        }

        static bool NumericValuesEqual(ref RuntimeValue a, ref RuntimeValue b)
        {
            if (IsFloatFamily(a.eType) || IsFloatFamily(b.eType))
            {
                double da = ToDouble(ref a);
                double db = ToDouble(ref b);
                return da.Equals(db);
            }

            long la = ToInt64(ref a);
            long lb = ToInt64(ref b);
            return la == lb;
        }

        static bool IsNumericEvm(EVMType t)
        {
            return t == EVMType.UInt8 || t == EVMType.Int8 || t == EVMType.Int16 || t == EVMType.UInt16
                || t == EVMType.Int32 || t == EVMType.UInt32 || t == EVMType.Int64 || t == EVMType.UInt64
                || t == EVMType.Float32 || t == EVMType.Float64 || t == EVMType.Num;
        }

        static bool IsFloatFamily(EVMType t)
        {
            return t == EVMType.Float32 || t == EVMType.Float64 || t == EVMType.Num;
        }

        static double ToDouble(ref RuntimeValue v)
        {
            return v.eType switch
            {
                EVMType.Float64 or EVMType.Num => v.float64Value,
                EVMType.Float32 => v.float32Value,
                _ => Convert.ToDouble(v.GetValueObject(), CultureInfo.InvariantCulture),
            };
        }

        static long ToInt64(ref RuntimeValue v)
        {
            return v.eType switch
            {
                EVMType.Int64 => v.int64Value,
                EVMType.UInt64 => unchecked((long)v.uint64Value),
                EVMType.Int32 => v.int32Value,
                EVMType.UInt32 => v.uint32Value,
                EVMType.Int16 => v.int16Value,
                EVMType.UInt16 => v.uint16Value,
                EVMType.Int8 => v.int8Value,
                EVMType.UInt8 => v.uint8Value,
                _ => Convert.ToInt64(v.GetValueObject(), CultureInfo.InvariantCulture),
            };
        }
    }
}
