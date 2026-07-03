//****************************************************************************
//  File:      RuntimeValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public partial class RuntimeValueMethod
    {
        public static void ComputeRuntimeValue(ref RuntimeValue _rv,  RuntimeValue curval, RuntimeValue sval, bool isUnsignCompute )
        {
#pragma warning disable CS0219 // 鍙橀噺宸茶璧嬪€硷紝浣嗕粠鏈娇鐢ㄨ繃瀹冪殑锟?
            bool isNumber = false;
#pragma warning restore CS0219 // 鍙橀噺宸茶璧嬪€硷紝浣嗕粠鏈娇鐢ㄨ繃瀹冪殑锟?
#pragma warning disable CS0219 // 鍙橀噺宸茶璧嬪€硷紝浣嗕粠鏈娇鐢ㄨ繃瀹冪殑锟?
            bool isUnsign = false;
#pragma warning restore CS0219 // 鍙橀噺宸茶璧嬪€硷紝浣嗕粠鏈娇鐢ㄨ繃瀹冪殑锟?
            switch (_rv.eType)
            {
                case EVMType.Int32:
                case EVMType.UInt32:
                    {
                        
                    }
                    break;
                case EVMType.String:
                    {
                        switch (sval.eType)
                        {
                            case EVMType.UInt8:
                                {
                                    _rv.stringValue += sval.uint8Value.ToString();
                                }
                                break;
                            case EVMType.Int8:
                                {
                                    _rv.stringValue += sval.int8Value.ToString();
                                }
                                break;
                            //case EVMType.Char:
                            //    {
                            //        _rv.stringValue += sval.charValue.ToString();
                            //    }
                            //    break;
                            case EVMType.Int16:
                                {
                                    _rv.stringValue += sval.int16Value.ToString();
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    _rv.stringValue += sval.uint16Value.ToString();
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    _rv.stringValue += sval.int32Value.ToString();
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    _rv.stringValue += sval.uint32Value.ToString();
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    _rv.stringValue += sval.int64Value.ToString();
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    _rv.stringValue += sval.uint64Value.ToString();
                                }
                                break;
                            case EVMType.String:
                                {
                                    _rv.stringValue += sval.stringValue;
                                }
                                break;
                        }
                    }
                    break;
            }
            switch (sval.eType)
            {
                case EVMType.Int32: _rv.int32Value += sval.int32Value; break;
                case EVMType.String:
                    {
                        _rv.SetStringValue(_rv.int32Value.ToString() + sval.stringValue);
                    }
                    break;
            }
        }       
        public static void SetInt8Compare(ref RuntimeValue _rv, byte a, byte b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                _rv.SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                _rv.SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a >= b);
                }
                else
                {
                    _rv.SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a <= b);
                }
                else
                {
                    _rv.SetBoolValue(a < b);
                }
            }
        }
        public static void SetInt16Compare(ref RuntimeValue _rv, short a, short b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                _rv.SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                _rv.SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a >= b);
                }
                else
                {
                    _rv.SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a <= b);
                }
                else
                {
                    _rv.SetBoolValue(a < b);
                }
            }
        }
        public static void SetInt32Compare(ref RuntimeValue _rv, int a, int b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                _rv.SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                _rv.SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a >= b);
                }
                else
                {
                    _rv.SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a <= b);
                }
                else
                {
                    _rv.SetBoolValue(a < b);
                }
            }
        }
        public static void SetInt64Compare(ref RuntimeValue _rv, long a, long b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                _rv.SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                _rv.SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a >= b);
                }
                else
                {
                    _rv.SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    _rv.SetBoolValue(a <= b);
                }
                else
                {
                    _rv.SetBoolValue(a < b);
                }
            }
        }

        /// <summary>锟?SValueCompute 涓畻锟?null 妫€娴嬩竴鑷达紝鐢ㄤ簬 ==/!= 涓庢暟瀛楁贩鍚堟椂锟?VMOperator 鏃ュ織锟?/summary>
        private static bool IsNullLikeForOperatorVmLog(ref RuntimeValue v)
        {
            if (v.isNull || v.eType == EVMType.Null) return true;
            if (v.eType == EVMType.Object || v.eType == EVMType.Class) return v.sobject == null;
            return false;
        }

        private static bool IsStrictNumericForOperatorVmLog(ref RuntimeValue v)
        {
            if (v.isNull || v.eType == EVMType.Null) return false;
            return IsNumericType(v.eType) || v.eType == EVMType.Num;
        }

        private static bool IsDataRuntimeType(RuntimeType rt)
        {
            return rt?.runtimeClass != null && rt.runtimeClass.metaClassKind == 2;
        }

        private static bool IsEnumRuntimeType(RuntimeType rt)
        {
            return rt?.runtimeClass != null && rt.runtimeClass.metaClassKind == 1;
        }

        private static bool IsMemberClass(RuntimeClass rc)
        {
            if (rc == null)
                return false;
            return string.Equals(rc.name, "Member", StringComparison.Ordinal)
                || string.Equals(rc.name, "Core.Member", StringComparison.Ordinal)
                || rc.name.EndsWith(".Member", StringComparison.Ordinal);
        }

        private static void NormalizeObjectReferenceKindInPlace(ref RuntimeValue sval)
        {
            if (sval.isNull || sval.sobject == null)
                return;

            if (sval.eType != EVMType.Object)
                return;

            if (sval.sobject.eType == EVMType.Object)
                return;

            sval.SetValueBySObject(sval.sobject);
        }

        private static bool IsAnonymousDynamicDataRuntimeType(RuntimeType rt)
        {
            return IsDataRuntimeType(rt) && rt.runtimeClass.isDynamicData;
        }

        private static bool TryGetDataClassObject(ref RuntimeValue sval, out ClassObject dataObject)
        {
            dataObject = null;
            if (sval.isNull)
                return false;

            if( sval.eType != EVMType.Class )
            {
                return false;
            }

            var co = sval.sobject as ClassObject;
            if (co == null)
                return false;

            if (!IsDataRuntimeType(co.runtimeType))
                return false;

            dataObject = co;
            return true;
        }

        private static bool TryGetClassObject(ref RuntimeValue sval, out ClassObject classObject)
        {
            classObject = null;
            if (sval.isNull)
                return false;

            if (sval.eType != EVMType.Class && sval.eType != EVMType.Object)
                return false;

            classObject = sval.sobject as ClassObject;
            return classObject != null;
        }

        private static bool TryGetEnumClassObject(ref RuntimeValue sval, out ClassObject enumObject)
        {
            enumObject = null;
            if (!TryGetClassObject(ref sval, out var co))
                return false;

            var rt = co.runtimeType;
            var rc = rt?.runtimeClass;
            if (IsEnumRuntimeType(rt) || IsMemberClass(rc))
            {
                enumObject = co;
                return true;
            }

            return false;
        }

        private static bool TryReadEnumMemberValue(ClassObject enumMemberObject, ref RuntimeValue value)
        {
            if (enumMemberObject == null)
                return false;

            var members = enumMemberObject.runtimeClass?.nonStaticIRMetaVariableList;
            if (members == null || members.Count == 0)
                return false;

            int valueIndex = -1;
            for (int i = 0; i < members.Count; i++)
            {
                var name = members[i]?.name ?? string.Empty;
                if (string.Equals(name, "value", StringComparison.Ordinal)
                    || name.EndsWith(".value", StringComparison.Ordinal))
                {
                    valueIndex = i;
                    break;
                }
            }

            if (valueIndex < 0 && members.Count > 2)
                valueIndex = 2;

            if (valueIndex < 0)
                return false;

            enumMemberObject.GetMemberVariableSValue(valueIndex, ref value);
            return true;
        }

        private static bool IsReferenceTypeForEquality(EVMType type)
        {
            return type == EVMType.Array
                || type == EVMType.Object
                || type == EVMType.Type
                || type == EVMType.Member;
        }

        private static bool TryCompareReferenceWithoutClass(ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual, out bool handled)
        {
            handled = false;
            if (!IsReferenceTypeForEquality(sval1.eType) && !IsReferenceTypeForEquality(sval2.eType))
                return false;

            handled = true;
            if (sval1.eType == EVMType.Type && sval2.eType == EVMType.Type)
            {
                TypeObject bo1 = sval1.sobject as TypeObject;
                TypeObject bo2 = sval2.sobject as TypeObject;
                bool eq = bo1 != null && bo2 != null && bo1.currentRT == bo2.currentRT;
                sval1.SetBoolValue(isEqual ? eq : !eq);
                return true;
            }

            bool sameType = sval1.eType == sval2.eType;
            bool sameRef = sameType && sval1.sobject == sval2.sobject;
            sval1.SetBoolValue(isEqual ? sameRef : !sameRef);
            return true;
        }

        private static bool TryCompareEnumValue(ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual)
        {
            bool leftIsEnum = TryGetEnumClassObject(ref sval1, out var leftEnum);
            bool rightIsEnum = TryGetEnumClassObject(ref sval2, out var rightEnum);
            if (!leftIsEnum && !rightIsEnum)
                return false;

            bool equals = false;
            if (leftIsEnum && rightIsEnum)
            {
                if (ReferenceEquals(leftEnum, rightEnum))
                {
                    equals = true;
                }
                else
                {
                    RuntimeValue leftValue = default;
                    RuntimeValue rightValue = default;
                    if (TryReadEnumMemberValue(leftEnum, ref leftValue)
                        && TryReadEnumMemberValue(rightEnum, ref rightValue))
                    {
                        equals = IsSValueEqual(ref leftValue, ref rightValue);
                    }
                }
            }

            sval1.SetBoolValue(isEqual ? equals : !equals);
            return true;
        }

        private static bool TryCompareDataValue(ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual)
        {
            bool leftIsData = TryGetDataClassObject(ref sval1, out var leftData);
            bool rightIsData = TryGetDataClassObject(ref sval2, out var rightData);
            if (!leftIsData && !rightIsData)
                return false;

            bool equals = false;
            if (leftIsData && rightIsData)
            {
                if (ReferenceEquals(leftData, rightData))
                {
                    equals = true;
                }
                else
                {
                    bool leftAnon = IsAnonymousDynamicDataRuntimeType(leftData.runtimeType);
                    bool rightAnon = IsAnonymousDynamicDataRuntimeType(rightData.runtimeType);

                    if (leftAnon && rightAnon)
                    {
                        equals = IsAnonymousDataShapeEqual(leftData, rightData)
                            && IsDataMemberValuesEqual(leftData, rightData);
                    }
                    else if (!leftAnon && !rightAnon)
                    {
                        equals = IsDataValueEqual(leftData, rightData);
                    }
                }
            }

            sval1.SetBoolValue(isEqual ? equals : !equals);
            return true;
        }

        private static bool TryRunClassEqualityOperator(ClassObject co, bool isEqual, out bool needInvert)
        {
            needInvert = false;
            if (co == null)
                return false;

            RuntimeType irt = co.runtimeType;
            if (irt == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageError, "IRC鏄皟鐢ㄨ櫄鍑芥暟涓虹┖!!");
                return false;
            }

            RuntimeMethod cfc = irt.runtimeClass.GetOperatorMethodIndexByMethod(isEqual ? "_eq_" : "_ne_", out int index);
            if (cfc == null && !isEqual)
            {
                cfc = irt.runtimeClass.GetOperatorMethodIndexByMethod("_eq_", out index);
                if (cfc != null)
                    needInvert = true;
            }

            if (cfc == null)
                return false;

            List<RuntimeType> irmtList = new List<RuntimeType>();
            CLRVM.RunIRMethodByRuntimeType(irt, irmtList, cfc, false);
            if (needInvert)
            {
                TryInvertTopMethodBoolResult();
            }
            return true;
        }

        private static bool TryCompareClassValue(ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual, out bool methodCall)
        {
            methodCall = false;
            bool leftIsClass = TryGetClassObject(ref sval1, out var leftClass);
            bool rightIsClass = TryGetClassObject(ref sval2, out var rightClass);
            if (!leftIsClass && !rightIsClass)
                return false;

            if (leftIsClass && TryRunClassEqualityOperator(leftClass, isEqual, out _))
            {
                methodCall = true;
                return true;
            }

            if (!leftIsClass && rightIsClass && TryRunClassEqualityOperator(rightClass, isEqual, out _))
            {
                methodCall = true;
                return true;
            }

            bool equals = leftIsClass && rightIsClass && ReferenceEquals(leftClass, rightClass);
            sval1.SetBoolValue(isEqual ? equals : !equals);
            return true;
        }

        private static bool TryCompareNumericValue(ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual)
        {
            if (!((IsNumericType(sval1.eType) || sval1.eType == EVMType.Num) && (IsNumericType(sval2.eType) || sval2.eType == EVMType.Num)))
                return false;

            bool leftFloat = (sval1.eType == EVMType.Float32 || sval1.eType == EVMType.Float64 || sval1.eType == EVMType.Num);
            bool rightFloat = (sval2.eType == EVMType.Float32 || sval2.eType == EVMType.Float64 || sval2.eType == EVMType.Num);
            if (leftFloat || rightFloat)
            {
                double a = (sval1.eType == EVMType.Float64 || sval1.eType == EVMType.Num) ? sval1.float64Value : (sval1.eType == EVMType.Float32 ? sval1.float32Value : sval1.ConvertToDoubleFromIntTypes());
                double b = (sval2.eType == EVMType.Float64 || sval2.eType == EVMType.Num) ? sval2.float64Value : (sval2.eType == EVMType.Float32 ? sval2.float32Value : sval2.ConvertToDoubleFromIntTypes());
                sval1.SetBoolValue(isEqual ? a == b : a != b);
                return true;
            }

            bool useUnsigned = sval1.IsUnsignedType(sval1.eType) || sval2.IsUnsignedType(sval2.eType);
            if (useUnsigned)
            {
                ulong a = RuntimeValueMethod.ConvertToULong(sval1);
                ulong b = RuntimeValueMethod.ConvertToULong(sval2);
                sval1.SetBoolValue(isEqual ? a == b : a != b);
                return true;
            }

            long la = RuntimeValueMethod.ConvertToLong(sval1);
            long lb = RuntimeValueMethod.ConvertToLong(sval2);
            sval1.SetBoolValue(isEqual ? la == lb : la != lb);
            return true;
        }

        private static bool IsSameDataRuntimeType(RuntimeType left, RuntimeType right)
        {
            if (left == null || right == null)
                return false;

            if (left.runtimeClass == null || right.runtimeClass == null)
                return false;

            if (left.runtimeClass.id != right.runtimeClass.id)
                return false;

            var leftTemplates = left.runtimeTemplateList;
            var rightTemplates = right.runtimeTemplateList;
            if (leftTemplates == null || rightTemplates == null)
                return leftTemplates == rightTemplates;

            if (leftTemplates.Count != rightTemplates.Count)
                return false;

            for (int i = 0; i < leftTemplates.Count; i++)
            {
                if (!IsSameDataRuntimeType(leftTemplates[i], rightTemplates[i]))
                    return false;
            }

            return true;
        }

        private static bool IsSameDataMemberBuffer(byte[] leftBuffer, byte[] rightBuffer)
        {
            if (ReferenceEquals(leftBuffer, rightBuffer))
                return true;
            if (leftBuffer == null || rightBuffer == null)
                return false;
            if (leftBuffer.Length != rightBuffer.Length)
                return false;

            for (int i = 0; i < leftBuffer.Length; i++)
            {
                if (leftBuffer[i] != rightBuffer[i])
                    return false;
            }
            return true;
        }

        private static bool IsDataValueEqual(ClassObject leftData, ClassObject rightData)
        {
            if (leftData == null || rightData == null)
                return false;

            if (!IsSameDataRuntimeType(leftData.runtimeType, rightData.runtimeType))
                return false;

            if (IsSameDataMemberBuffer(leftData.memberData, rightData.memberData))
                return true;

            return IsDataMemberValuesEqual(leftData, rightData);
        }

        private static bool IsSValueEqual(ref RuntimeValue left, ref RuntimeValue right)
        {
            var l = left;
            var r = right;
            CompareEuqalSValue1AndValue2(ref l, ref r, true, out _);
            return !l.isNull && l.eType == EVMType.Boolean && l.uint8Value == 1;
        }

        private static bool IsDataMemberValuesEqual(ClassObject leftData, ClassObject rightData)
        {
            if (leftData == null || rightData == null)
                return false;

            var leftClass = leftData.runtimeType?.runtimeClass;
            var rightClass = rightData.runtimeType?.runtimeClass;
            var leftMembers = leftClass?.nonStaticIRMetaVariableList;
            var rightMembers = rightClass?.nonStaticIRMetaVariableList;
            if (leftMembers == null || rightMembers == null)
                return false;
            if (leftMembers.Count != rightMembers.Count)
                return false;

            for (int i = 0; i < leftMembers.Count; i++)
            {
                RuntimeValue lv = default;
                RuntimeValue rv = default;
                if (!leftData.TryReadMemberDataAsSValue(i, ref lv))
                    return false;
                if (!rightData.TryReadMemberDataAsSValue(i, ref rv))
                    return false;

                bool leftIsData = TryGetDataClassObject(ref lv, out var leftChildData);
                bool rightIsData = TryGetDataClassObject(ref rv, out var rightChildData);
                if (leftIsData || rightIsData)
                {
                    if (!(leftIsData && rightIsData))
                        return false;

                    if (ReferenceEquals(leftChildData, rightChildData))
                        continue;

                    bool leftAnon = IsAnonymousDynamicDataRuntimeType(leftChildData.runtimeType);
                    bool rightAnon = IsAnonymousDynamicDataRuntimeType(rightChildData.runtimeType);
                    if (leftAnon != rightAnon)
                        return false;

                    if (leftAnon)
                    {
                        if (!IsAnonymousDataShapeEqual(leftChildData, rightChildData))
                            return false;
                        if (!IsDataMemberValuesEqual(leftChildData, rightChildData))
                            return false;
                        continue;
                    }

                    if (!IsDataValueEqual(leftChildData, rightChildData))
                        return false;
                    continue;
                }

                if (!IsSValueEqual(ref lv, ref rv))
                    return false;
            }

            return true;
        }

        private static bool IsAnonymousDataShapeEqual(ClassObject leftData, ClassObject rightData)
        {
            if (leftData == null || rightData == null)
                return false;

            var leftType = leftData.runtimeType;
            var rightType = rightData.runtimeType;
            if (!IsAnonymousDynamicDataRuntimeType(leftType) || !IsAnonymousDynamicDataRuntimeType(rightType))
                return false;

            var leftClass = leftType.runtimeClass;
            var rightClass = rightType.runtimeClass;
            var leftMembers = leftClass?.nonStaticIRMetaVariableList;
            var rightMembers = rightClass?.nonStaticIRMetaVariableList;
            if (leftMembers == null || rightMembers == null)
                return false;

            if (leftMembers.Count != rightMembers.Count)
                return false;

            for (int i = 0; i < leftMembers.Count; i++)
            {
                var lm = leftMembers[i];
                var rm = rightMembers[i];
                if (lm == null || rm == null)
                    return false;

                if (!string.Equals(lm.name, rm.name, StringComparison.Ordinal))
                    return false;

                var ldt = lm.runtimeDefType;
                var rdt = rm.runtimeDefType;
                var lrc = ldt?.runtimeClass;
                var rrc = rdt?.runtimeClass;
                if (lrc == null || rrc == null)
                    return false;
                if (lrc.id != rrc.id)
                    return false;
            }

            return true;
        }

        // compareSign 0:== 1:!= 
        public static void CompareEuqalSValue1AndValue2( ref RuntimeValue sval1, ref RuntimeValue sval2, bool isEqual, out bool methodCall )
        {
            methodCall = false;

            sval1.TryNormalizeObjectScalarInPlace();
            sval2.TryNormalizeObjectScalarInPlace();
            NormalizeObjectReferenceKindInPlace(ref sval1);
            NormalizeObjectReferenceKindInPlace(ref sval2);

            if (sval1.isNull || sval1.eType == EVMType.Null || sval2.isNull || sval2.eType == EVMType.Null)
            {
                bool equals = (sval1.isNull || sval1.eType == EVMType.Null) && (sval2.isNull || sval2.eType == EVMType.Null);
                sval1.SetBoolValue(isEqual ? equals : !equals);
                return;
            }

            if (TryCompareEnumValue(ref sval1, ref sval2, isEqual))
            {
                return;
            }

            if (TryCompareDataValue(ref sval1, ref sval2, isEqual))
            {
                return;
            }

            if (TryCompareClassValue(ref sval1, ref sval2, isEqual, out methodCall))
            {
                return;
            }

            if (TryCompareNumericValue(ref sval1, ref sval2, isEqual))
            {
                return;
            }

            if (sval1.eType == EVMType.String || sval2.eType == EVMType.String)
            {
                bool equals = sval1.eType == EVMType.String && sval2.eType == EVMType.String && sval1.stringValue == sval2.stringValue;
                sval1.SetBoolValue(isEqual ? equals : !equals);
                return;
            }

            if (sval1.eType == EVMType.Boolean || sval2.eType == EVMType.Boolean)
            {
                bool equals = sval1.eType == EVMType.Boolean && sval2.eType == EVMType.Boolean && sval1.int8Value == sval2.int8Value;
                sval1.SetBoolValue(isEqual ? equals : !equals);
                return;
            }

            if (TryCompareReferenceWithoutClass(ref sval1, ref sval2, isEqual, out _))
            {
                return;
            }

            sval1.SetBoolValue(!isEqual);
            Log.AddRuntimeLog(LID.ShowMessageError, "VM Compare RuntimeValue 姣旇緝鐨勪綆鐮佽繕娌℃湁瀹屽杽!!");
        }

        private static void TryInvertTopMethodBoolResult()
        {
            if (CLRVM.clrRuntimeStack == null || CLRVM.clrRuntimeStack.Count == 0)
                return;
            var vm = CLRVM.clrRuntimeStack.Peek();
            if (vm == null || vm.valueIndex == 0)
                return;

            var topIndex = vm.valueIndex - 1;
            var cur = vm.GetCurrentIndexValue(topIndex);
            if (cur.eType == EVMType.Boolean)
            {
                RuntimeValueMethod.NotSValue(ref cur);
            }
            else
            {
                bool b = IsTruthy(ref cur);
                cur.SetBoolValue(!b);
            }
            vm.SetValueIndex(topIndex);
            vm.PushSValueSynced(cur);
        }


        //0> 1:>= 2:< 3:<= 
        public static void CompareSValue1AndValue2(ref RuntimeValue sval1, ref RuntimeValue sval2, int compareSign)
        {
            sval1.TryNormalizeObjectScalarInPlace();
            sval2.TryNormalizeObjectScalarInPlace();

            // logical operators (used by VM OpCode And/Or)
            if (compareSign == 4)
            {
                // logical AND
                bool a = IsTruthy(ref sval1);
                bool b = IsTruthy(ref sval2);
                sval1.SetBoolValue(a && b);
                return;
            }
            if (compareSign == 6)
            {
                // logical OR
                bool a = IsTruthy(ref sval1);
                bool b = IsTruthy(ref sval2);
                sval1.SetBoolValue(a || b);
                return;
            }

            // numeric comparisons
            // consider Num as numeric (float) for comparisons
            if ((IsNumericType(sval1.eType) || sval1.eType == EVMType.Num) && (IsNumericType(sval2.eType) || sval2.eType == EVMType.Num))
            {
                bool leftFloat = (sval1.eType == EVMType.Float32 || sval1.eType == EVMType.Float64 || sval1.eType == EVMType.Num);
                bool rightFloat = (sval2.eType == EVMType.Float32 || sval2.eType == EVMType.Float64 || sval2.eType == EVMType.Num);
                if (leftFloat || rightFloat)
                {
                    double a = (sval1.eType == EVMType.Float64 || sval1.eType == EVMType.Num) ? sval1.float64Value : (sval1.eType == EVMType.Float32 ? sval1.float32Value : sval1.ConvertToDoubleFromIntTypes());
                    double b = (sval2.eType == EVMType.Float64 || sval2.eType == EVMType.Num) ? sval2.float64Value : (sval2.eType == EVMType.Float32 ? sval2.float32Value : sval2.ConvertToDoubleFromIntTypes());
                    switch (compareSign)
                    {
                        case 0: sval1.SetBoolValue(a > b); break;
                        case 1: sval1.SetBoolValue(a >= b); break;
                        case 2: sval1.SetBoolValue(a < b); break;
                        case 3: sval1.SetBoolValue(a <= b); break;
                    }
                    return;
                }

                bool useUnsigned = sval1.IsUnsignedType(sval1.eType) || sval2.IsUnsignedType(sval2.eType);
                if (useUnsigned)
                {
                    ulong a = RuntimeValueMethod.ConvertToULong(sval1);
                    ulong b = RuntimeValueMethod.ConvertToULong(sval2);
                    switch (compareSign)
                    {
                        case 0: sval1.SetBoolValue(a > b); break;
                        case 1: sval1.SetBoolValue(a >= b); break;
                        case 2: sval1.SetBoolValue(a < b); break;
                        case 3: sval1.SetBoolValue(a <= b); break;
                    }
                    return;
                }

                long la = RuntimeValueMethod.ConvertToLong(sval1);
                long lb = RuntimeValueMethod.ConvertToLong(sval2);
                switch (compareSign)
                {
                    case 0: sval1.SetBoolValue(la > lb); break;
                    case 1: sval1.SetBoolValue(la >= lb); break;
                    case 2: sval1.SetBoolValue(la < lb); break;
                    case 3: sval1.SetBoolValue(la <= lb); break;
                }
                return;
            }
        }

        // helper: numeric type check
        static bool IsNumericType(EVMType t)
        {
            switch (t)
            {
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
                    return true;
                default:
                    return false;
            }
        }

        // logical && and || on truthiness
        private static bool TryRunClassLogicalOperator(ref RuntimeValue left, ref RuntimeValue right, string opName)
        {
            if (left.eType != EVMType.Class || left.sobject is not ClassObject co)
                return false;

            var method = co.runtimeType?.runtimeClass?.GetOperatorMethodIndexByMethod(opName, out _);
            if (method == null)
                return false;

            CLRVM.RunIRMethodByRuntimeType(co.runtimeType, new List<RuntimeType> {}, method );
            return true;
        }

        public static void LogicalAnd(ref RuntimeValue left, ref RuntimeValue right, out bool methodCall)
        {
            methodCall = TryRunClassLogicalOperator(ref left, ref right, "_and_");
            if (methodCall)
                return;

            bool a = IsTruthy(ref left);
            bool b = IsTruthy(ref right);
            left.SetBoolValue(a && b);
        }

        public static void LogicalAnd(ref RuntimeValue left, ref RuntimeValue right)
        {
            LogicalAnd(ref left, ref right, out _);
        }

        public static void LogicalOr(ref RuntimeValue left, ref RuntimeValue right, out bool methodCall)
        {
            methodCall = TryRunClassLogicalOperator(ref left, ref right, "_or_");
            if (methodCall)
                return;

            bool a = IsTruthy(ref left);
            bool b = IsTruthy(ref right);
            left.SetBoolValue(a || b);
        }

        public static void LogicalOr(ref RuntimeValue left, ref RuntimeValue right)
        {
            LogicalOr(ref left, ref right, out _);
        }
        public static bool IsTruthy(ref RuntimeValue v)
        {
            v.TryNormalizeObjectScalarInPlace();

            if (v.isNull) return false;
            switch (v.eType)
            {
                case EVMType.Boolean: return v.int8Value != 0;
                case EVMType.String: return !string.IsNullOrEmpty(v.stringValue);
                case EVMType.Float32: return v.float32Value != 0.0f;
                case EVMType.Float64: return v.float64Value != 0.0;
                case EVMType.UInt8: return v.uint8Value != 0;
                case EVMType.Int8: return v.int8Value != 0;
                case EVMType.Int16: return v.int16Value != 0;
                case EVMType.UInt16: return v.uint16Value != 0;
                case EVMType.Int32: return v.int32Value != 0;
                case EVMType.UInt32: return v.uint32Value != 0;
                case EVMType.Int64: return v.int64Value != 0;
                case EVMType.UInt64: return v.uint64Value != 0;
                default: return v.sobject != null;
            }
        }
    }
}
