//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeType
    {
        public IRMetaClass irClass;
        public List<RuntimeType> runtimeTemplateList = new List<RuntimeType>();

        private SObject[] m_StaticMemObjectList = null;
        public RuntimeType(IRMetaClass rc, List<RuntimeType > rtList )
        {

            irClass = rc;
            if( rtList != null )
            {
                runtimeTemplateList = rtList;
            }

            m_StaticMemObjectList = new SObject[irClass.staticIRMetaVariableList.Count];
            for ( int i = 0; i < irClass.staticIRMetaVariableList.Count; i++ )
            {
                RuntimeType rt = GetClassRuntimeType(irClass.staticIRMetaVariableList[i].irMetaType, true );
                
                m_StaticMemObjectList[i] = ObjectManager.CreateObjectByRuntimeType( rt, true );
            }
        }
        public RuntimeType GetClassRuntimeType(IRMetaType irmt, bool isAdd = false)
        {
            if (irmt.templateIndex != -1)
            {
                return runtimeTemplateList[irmt.templateIndex];
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.irMetaTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], isAdd);
                        rtList.Add(crt);
                    }
                }
                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(irmt.irMetaClass, rtList);
                }
                return rt;
            }
        }
        public void GetMemberVariableSValue(int index, ref SValue svalue)
        {
            if (index < 0)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index > m_StaticMemObjectList.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            var mmv = m_StaticMemObjectList[index];
            switch (mmv)
            {
                case ByteObject byteob:
                    {
                        svalue.SetInt8Value(byteob.value);
                    }
                    break;
                case SByteObject sbyteobj:
                    {
                        svalue.SetSInt8Value(sbyteobj.value);
                    }
                    break;
                case Int16Object int16Obj:
                    {
                        svalue.SetInt16Value(int16Obj.value);
                    }
                    break;
                case UInt16Object uint16Obj:
                    {
                        svalue.SetUInt16Value(uint16Obj.value);
                    }
                    break;
                case Int32Object int32Obj:
                    {
                        svalue.SetInt32Value(int32Obj.value);
                    }
                    break;
                case UInt32Object uint32Obj:
                    {
                        svalue.SetUInt32Value(uint32Obj.value);
                    }
                    break;
                case Int64Object int64Obj:
                    {
                        svalue.SetInt64Value(int64Obj.value);
                    }
                    break;
                case UInt64Object uint64Obj:
                    {
                        svalue.SetUInt64Value(uint64Obj.value);
                    }
                    break;
                case FloatObject floatobj:
                    {
                        svalue.SetFloatValue(floatobj.value);
                    }
                    break;
                case DoubleObject doubleobj:
                    {
                        svalue.SetDoubleValue(doubleobj.value);
                    }
                    break;
                case StringObject stringObj:
                    {
                        svalue.SetStringValue(stringObj.value);
                    }
                    break;
                case ClassObject classObj:
                    {
                        svalue.SetSObject(classObj);
                    }
                    break;
                case TemplateObject templateObj:
                    {

                    }
                    break;
            }
        }
        public void SetMemberVariableSValue(int index, SValue svalue)
        {
            if (index > m_StaticMemObjectList.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        ClassObject classObj = m_StaticMemObjectList[index] as ClassObject;
                        if (classObj == null)
                        {
                            Log.AddVM(EError.None, "Null 该类型不是Int32类型!!");
                            return;
                        }
                        classObj.SetNull();
                    }
                    break;
                case EType.Boolean:
                    {

                    }
                    break;
                case EType.Byte:
                    {
                        ByteObject byteObj = m_StaticMemObjectList[index] as ByteObject;
                        if (byteObj == null)
                        {
                            Log.AddVM(EError.None, "Byte 该类型不是Int32类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EType.SByte:
                    {
                        SByteObject sbyteObj = m_StaticMemObjectList[index] as SByteObject;
                        if (sbyteObj == null)
                        {
                            Log.AddVM(EError.None, "Sbyte 该类型不是Int32类型!!");
                            return;
                        }
                        sbyteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        Int16Object int32Obj = m_StaticMemObjectList[index] as Int16Object;
                        if (int32Obj == null)
                        {
                            Log.AddVM(EError.None, "Int16 该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        UInt16Object uint16Obj = m_StaticMemObjectList[index] as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt16 该类型不是Int16类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        Int32Object int32Obj = m_StaticMemObjectList[index] as Int32Object;
                        if (int32Obj == null)
                        {
                            Log.AddVM(EError.None, "Int32 该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        UInt32Object uint32Obj = m_StaticMemObjectList[index] as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt32 该类型不是UInt32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        Int64Object int64Obj = m_StaticMemObjectList[index] as Int64Object;
                        if (int64Obj == null)
                        {
                            Log.AddVM(EError.None, "Int64 该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        UInt64Object uint64Obj = m_StaticMemObjectList[index] as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt64 该类型不是Int64类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EType.Float32:
                    {
                        FloatObject floatObj = m_StaticMemObjectList[index] as FloatObject;
                        if (floatObj == null)
                        {
                            Log.AddVM(EError.None, "Float 该类型不是float类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EType.Float64:
                    {
                        DoubleObject doubleObj = m_StaticMemObjectList[index] as DoubleObject;
                        if (doubleObj == null)
                        {
                            Log.AddVM(EError.None, "Double 该类型不是Double类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EType.String:
                    {
                        StringObject stringObj = m_StaticMemObjectList[index] as StringObject;
                        if (stringObj == null)
                        {
                            Log.AddVM(EError.None, "String 该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        var mva = m_StaticMemObjectList[index];
                        if (mva.eType == EType.Byte)
                        {

                            ByteObject byteObj = mva as ByteObject;
                            if (byteObj == null)
                            {
                                Log.AddVM(EError.None, "Class ByteObject 该类型不是Int32类型!!");
                                return;
                            }
                            byteObj.SetValue(svalue.int8Value);
                        }
                        else if (mva.eType == EType.SByte)
                        {

                            SByteObject sbyteObj = mva as SByteObject;
                            if (sbyteObj == null)
                            {
                                Log.AddVM(EError.None, "Class SByteObject 该类型不是Int32类型!!");
                                return;
                            }
                            sbyteObj.SetValue(svalue.sint8Value);
                        }
                        else if (mva.eType == EType.Int16)
                        {

                            Int16Object int16Obj = mva as Int16Object;
                            if (int16Obj == null)
                            {
                                Log.AddVM(EError.None, "Class Int16Object 该类型不是Int16类型!!");
                                return;
                            }
                            int16Obj.SetValue(svalue.int16Value);
                        }
                        else if (mva.eType == EType.UInt16)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class UInt32Object 该类型不是UInt32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int32)
                        {
                            Int32Object int32Obj = mva as Int32Object;
                            if (int32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class Int32Object 该类型不是Int32类型!!");
                                return;
                            }
                            int32Obj.SetValue(svalue.int32Value);
                        }
                        else if (mva.eType == EType.UInt32)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class UInt32Object 该类型不是Int32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int64)
                        {

                            Int64Object int64Obj = mva as Int64Object;
                            if (int64Obj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是Int64类型!!");
                                return;
                            }
                            int64Obj.SetValue(svalue.int64Value);
                        }
                        else if (mva.eType == EType.UInt64)
                        {

                            UInt64Object uint64Obj = mva as UInt64Object;
                            if (uint64Obj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是Int64类型!!");
                                return;
                            }
                            uint64Obj.SetValue(svalue.uint64Value);
                        }
                        else if (mva.eType == EType.String)
                        {

                            StringObject stringObj = mva as StringObject;
                            if (stringObj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是stringObj类型!!");
                                return;
                            }
                            stringObj.SetValue(svalue.stringValue);
                        }
                        else
                        {
                            ClassObject classObj = m_StaticMemObjectList[index] as ClassObject;
                            if (classObj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是classObj类型!!");
                                return;
                            }
                            //classObj.SetValue(svalue.sobject as ClassObject);
                            m_StaticMemObjectList[index] = svalue.sobject as ClassObject;
                        }
                    }
                    break;
            }
        }

        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            List<IRData> list = new List<IRData>();

            foreach (var v in  this.irClass.localIRMetaVariableList )
            {
                var irexp = new IRExpress(IRManager.instance, v.express);

                v.SetIRDataList(irexp.IRDataList);

                list.AddRange(irexp.IRDataList);
            }

            return list;
        }
        public static bool SameRuntimeType( RuntimeType rt1, RuntimeType rt2 )
        {
            if( rt1.irClass != rt2.irClass )
            {
                return false;
            }
            if( rt1.runtimeTemplateList.Count != rt2.runtimeTemplateList.Count )
            {
                return false;
            }
            for( int i = 0; i < rt1.runtimeTemplateList.Count; i++ )
            {
                if( SameRuntimeType(rt1.runtimeTemplateList[i], rt2.runtimeTemplateList[i] ) == false )
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(irClass.irName);

            if(runtimeTemplateList.Count > 0 )
            {
                sb.Append('<');
                for( int i = 0; i < runtimeTemplateList.Count; i++ )
                {
                    sb.Append(runtimeTemplateList[i].ToString());
                    if( i < runtimeTemplateList.Count - 1 )
                        sb.Append(",");
                }
                sb.Append(">");
            }

            return sb.ToString();
        }
    }

    public class RuntimeTypeManager
    {
        public static List<RuntimeType> runtimeList => s_RuntimeList;

        private static List<RuntimeType> s_RuntimeList = new List<RuntimeType>();

        public static RuntimeType GetRuntimeTypeByMTAndTemplateMT(IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            foreach (var v in s_RuntimeList)
            {
                if (v.irClass != rmc)
                {
                    continue;
                }

                if (v.runtimeTemplateList.Count == inputTemplateTypeList.Count)
                {
                    if( v.runtimeTemplateList.Count == 0 )
                    {
                        return v;
                    }
                    bool flag = true;
                    for (int i = 0; i < inputTemplateTypeList.Count; i++)
                    {
                        if ( !RuntimeType.SameRuntimeType(inputTemplateTypeList[i], v.runtimeTemplateList[i]))
                        {
                            flag = false;
                            break;
                        }
                    }
                    if(flag)
                        return v;
                }
            }
            return null;
        }
        public static RuntimeType GetRuntimeTypeByMTAndIRMetaClass(IRMetaClass rmc )
        {
            foreach (var v in s_RuntimeList)
            {
                if (v.runtimeTemplateList.Count != 0)
                {
                    continue;
                }
                if (v.irClass == rmc)
                {
                    return v;
                }
            }
            return null;
        }
        public static RuntimeType AddRuntimeTypeByClassAndTemplate(IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);

            s_RuntimeList.Add(rt);

            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass( IRMetaClass rmc )
        {
            RuntimeType rt = new RuntimeType(rmc, null );

            s_RuntimeList.Add(rt);

            return rt;

        }
    }
}
