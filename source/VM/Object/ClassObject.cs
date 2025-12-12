//****************************************************************************
//  File:      ClassObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.VM
{
    public class MemberVariableData
    {
        public int index { get; set; } = 0;
        public int start { get; set; } = 0;
        public int length { get; set; } = 0;
    }
    public class ClassObject : SObject
    {
        public new ClassObject value => m_Object;

        protected ClassObject m_Object = null;
        //protected byte[] m_Data = null;   /*  m_Data  结构  bit形，只有运算时要用 1-> byte 2->sbyte   3-> int16  4-> uint16    */
        //protected short[] m_Type = null;
        protected SObject[] m_MemberObjectArray = null;
        protected List<IRMetaVariable> m_IRMetaVariableList = null;
        protected List<RuntimeType> m_IRTemplateList = new List<RuntimeType>();


        public ClassObject( RuntimeType irmt, bool isStatic = false )
        {
            m_RuntimeType = irmt;

            int byteCount = m_RuntimeType.irClass.byteCount;
            //m_Data = new byte[byteCount];
            typeId = (short)m_RuntimeType.irClass.id;
            m_IRTemplateList = irmt.runtimeTemplateList;

            m_IRMetaVariableList = isStatic ? m_RuntimeType.irClass.staticIRMetaVariableList : m_RuntimeType.irClass.localIRMetaVariableList;
            m_MemberObjectArray = new SObject[m_IRMetaVariableList.Count];
            //m_Type = new short[m_IRMetaVariableList.Count];
        }
        public void SetClassObject( ClassObject co )
        {
            this.m_Object = co;
        }
        //public RuntimeType GetClassRuntimeType(IRMetaType irmt, IRMetaClass ownerMC, bool isAdd = false)
        //{
        //    if (irmt.templateIndex != -1)
        //    {
        //        if( irmt.irOwnerMetaClass == irMetaClass )
        //        {
        //            return m_IRTemplateList[irmt.templateIndex];
        //        }
        //        else
        //        {
        //            var mt = irMetaClass.GetIRMetaTypeByTemplateAndClassRelation(irmt.irOwnerMetaClass, irmt.templateIndex);

        //            return GetClassRuntimeType(mt, isAdd);
        //        }
        //    }
        //    else
        //    {
        //        List<RuntimeType> rtList = new List<RuntimeType>();
        //        if (irmt.irMetaTypeList.Count > 0)
        //        {
        //            for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
        //            {
        //                var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], isAdd);
        //                rtList.Add(crt);
        //            }
        //        }
        //        var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
        //        if (rt == null && isAdd)
        //        {
        //            rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(irmt.irMetaClass, rtList);
        //        }
        //        return rt;
        //    }
        //}
        public void CreateObject()
        {
            for (int i = 0; i < m_IRMetaVariableList.Count; i++)
            {
                var irmv = m_IRMetaVariableList[i].irMetaType;
                var rt = m_RuntimeType.GetClassRuntimeType( irmv, true );// GetClassRuntimeType(irmv, true );
                SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt );
                if(sobj == null )
                {
                    continue;
                }
                //m_Type[i] = sobj.typeId;
                m_MemberObjectArray[i] = sobj;
            }
        }
        //public SObject GetMemberVariable(int index)
        //{
        //    if (index > m_MemberObjectArray.Length)
        //    {
        //        Log.AddVM(EError.None, "执行的参数超出范围!!");
        //        return null;
        //    }
        //    return m_MemberObjectArray[index];
        //}
        public virtual void SetSValue(ClassObject val )
        {
            m_Object = val.m_Object;
            val.refCount++;
        }
        public void GetMemberVariableSValue( int index, ref SValue svalue )
        {
            if (index < 0 )
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index > m_MemberObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            var mmv = m_MemberObjectArray[index];
            if( mmv.isNull )
            {
                svalue.SetNull();
                return;
            }
            switch (mmv)
            {
                case BoolObject boolObj:
                    {
                        svalue.SetBoolValue(boolObj.value);
                    }
                    break;
                case Int8Object byteob:
                    {
                        svalue.SetInt8Value(byteob.value);
                    }
                    break;
                case SInt8Object sbyteobj:
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
                case Float32Object floatobj:
                    {
                        svalue.SetFloatValue(floatobj.value);
                    }
                    break;
                case Float64Object doubleobj:
                    {
                        svalue.SetDoubleValue(doubleobj.value);
                    }
                    break;
                case StringObject stringObj:
                    {
                        svalue.SetStringValue( stringObj.value );
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
                default:
                    {
                        svalue.SetSObject(mmv.value as SObject);
                    }
                    break;
            }
        }
        public void SetMemberVariableSValue( int index, SValue svalue)
        {
            if (index > m_MemberObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            if( svalue.isNull )
            {
                m_MemberObjectArray[index].SetNull();
                return;
            }
            SObject anyobj = null;
            if( m_MemberObjectArray[index] != null )
            {
                if(m_MemberObjectArray[index].eType == EVMType.Object)
                {
                    anyobj = m_MemberObjectArray[index];
                }
            }
            switch (svalue.eType)
            {
                case EVMType.Null:
                    {
                        m_MemberObjectArray[index].SetNull();
                    }
                    break;
                case EVMType.Boolean:
                //case EVMType.RawBoolean:
                    {
                        BoolObject boolObj = null;
                        if (anyobj != null)
                        {
                            boolObj = new BoolObject(svalue.int8Value == 1);
                            //anyobj.SetValue(boolObj);
                            m_MemberObjectArray[index] = boolObj;
                            return;
                        }

                        boolObj = m_MemberObjectArray[index] as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Boolean 该类型不是Int32类型!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value == 1);

                    }
                    break;
                case EVMType.Byte:
                //case EVMType.RawByte:
                    {
                        Int8Object byteObj = null;
                        if (anyobj != null)
                        {
                            byteObj = new Int8Object(svalue.int8Value);
                            //anyobj.SetValue(byteObj);
                            m_MemberObjectArray[index] = byteObj;
                            return;
                        }


                        byteObj = m_MemberObjectArray[index] as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Byte 该类型不是Int32类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EVMType.SByte:
                //case EVMType.RawSByte:
                    {
                        SInt8Object sbyteObj = null;
                        if (anyobj != null)
                        {
                            sbyteObj = new SInt8Object(svalue.sint8Value);
                            //anyobj.SetValue(sbyteObj);
                            m_MemberObjectArray[index] = sbyteObj;
                            return;
                        }

                        sbyteObj = m_MemberObjectArray[index] as SInt8Object;
                        if (sbyteObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Sbyte 该类型不是Int32类型!!");
                            return;
                        }
                        sbyteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EVMType.Int16:
                //case EVMType.RawInt16:
                    {
                        Int16Object int16Obj = null;
                        if (anyobj != null)
                        {
                            int16Obj = new Int16Object(svalue.int16Value);
                            //anyobj.SetValue(int16Obj);
                            m_MemberObjectArray[index] = int16Obj;
                            return;
                        }

                        int16Obj = m_MemberObjectArray[index] as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Int16 该类型不是Int32类型!!");
                            return;
                        }
                        int16Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EVMType.UInt16:
                //case EVMType.RawUInt16:
                    {
                        UInt16Object uint16Obj = null;
                        if (anyobj != null)
                        {
                            uint16Obj = new UInt16Object(svalue.uint16Value);
                            //anyobj.SetValue(uint16Obj);
                            m_MemberObjectArray[index] = uint16Obj;
                            return;
                        }

                        uint16Obj = m_MemberObjectArray[index] as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "UInt16 该类型不是Int16类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EVMType.Int32:
                //case EVMType.RawInt32:
                    {
                        Int32Object int32Obj = null;
                        if (anyobj != null)
                        {
                            int32Obj = new Int32Object(svalue.int32Value);
                            //anyobj.SetValue(int32Obj);
                            m_MemberObjectArray[index] = int32Obj;
                            return;
                        }
                        int32Obj = m_MemberObjectArray[index] as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Int32 该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EVMType.UInt32:
                //case EVMType.RawUInt32:
                    {
                        UInt32Object uint32Obj = null;
                        if (anyobj != null)
                        {
                            uint32Obj = new UInt32Object(svalue.uint32Value);
                            //anyobj.SetValue(uint32Obj);
                            m_MemberObjectArray[index] = uint32Obj;
                            return;
                        }

                        uint32Obj = m_MemberObjectArray[index] as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "UInt32 该类型不是UInt32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EVMType.Int64:
                //case EVMType.RawInt64:
                    {
                        Int64Object int64Obj = null;
                        if (anyobj != null)
                        {
                            int64Obj = new Int64Object(svalue.int64Value);
                            //anyobj.SetValue(int64Obj);
                            m_MemberObjectArray[index] = int64Obj;
                            return;
                        }

                        int64Obj = m_MemberObjectArray[index] as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Int64 该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EVMType.UInt64:
                //case EVMType.RawUInt64:
                    {
                        UInt64Object uint64Obj = null;
                        if (anyobj != null)
                        {
                            uint64Obj = new UInt64Object(svalue.uint64Value);
                            //anyobj.SetValue(uint64Obj);
                            m_MemberObjectArray[index] = uint64Obj;
                            return;
                        }

                        uint64Obj = m_MemberObjectArray[index] as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "UInt64 该类型不是Int64类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EVMType.Float32:
                //case EVMType.RawFloat32:
                    {
                        Float32Object floatObj = null;
                        if (anyobj != null)
                        {
                            floatObj = new Float32Object(svalue.floatValue);
                            //anyobj.SetValue(floatObj);
                            m_MemberObjectArray[index] = floatObj;
                            return;
                        }

                        floatObj = m_MemberObjectArray[index] as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Float 该类型不是float类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EVMType.Float64:
                //case EVMType.RawFloat64:
                    {
                        Float64Object doubleObj = null;
                        if (anyobj != null)
                        {
                            doubleObj = new Float64Object(svalue.doubleValue);
                            //anyobj.SetValue(doubleObj);
                            m_MemberObjectArray[index] = doubleObj;
                            return;
                        }

                        doubleObj = m_MemberObjectArray[index] as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "Double 该类型不是Double类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EVMType.String:
                //case EVMType.RawString:
                    {
                        StringObject stringObj = null;
                        if (anyobj != null)
                        {
                            stringObj = new StringObject(svalue.stringValue);
                            //anyobj.SetValue(stringObj);
                            m_MemberObjectArray[index] = stringObj;
                            return;
                        }

                        stringObj = m_MemberObjectArray[index] as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "String 该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EVMType.Object:
                    {
                        if( anyobj != null )
                        {
                            anyobj.SetValue(svalue.sobject.value as SObject);
                        }
                        else
                        {
                            Debug.Assert(false, "没有适当的匹配类型");
                        }
                    }break;
                case EVMType.Class:
                case EVMType.Array:
                    {
                        var mva = m_MemberObjectArray[index];

                        
                        //if (mva.eType == EVMType.Byte)
                        //{
                        //    Int8Object byteObj = mva as Int8Object;
                        //    if (byteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int8Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    byteObj.SetValue(svalue.int8Value);
                        //}
                        //else if (mva.eType == EVMType.SByte)
                        //{

                        //    SInt8Object sbyteObj = mva as SInt8Object;
                        //    if (sbyteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class SInt8Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    sbyteObj.SetValue(svalue.sint8Value);
                        //}
                        //else if (mva.eType == EVMType.Int16)
                        //{

                        //    Int16Object int16Obj = mva as Int16Object;
                        //    if (int16Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int16Object 该类型不是Int16类型!!");
                        //        return;
                        //    }
                        //    int16Obj.SetValue(svalue.int16Value);
                        //}
                        //else if (mva.eType == EVMType.UInt16)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是UInt32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int32)
                        //{
                        //    Int32Object int32Obj = mva as Int32Object;
                        //    if (int32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    int32Obj.SetValue(svalue.int32Value);
                        //}
                        //else if (mva.eType == EVMType.UInt32)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int64)
                        //{

                        //    Int64Object int64Obj = mva as Int64Object;
                        //    if (int64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    int64Obj.SetValue(svalue.int64Value);
                        //}
                        //else if (mva.eType == EVMType.UInt64)
                        //{

                        //    UInt64Object uint64Obj = mva as UInt64Object;
                        //    if (uint64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    uint64Obj.SetValue(svalue.uint64Value);
                        //}
                        //else if (mva.eType == EVMType.String)
                        //{

                        //    StringObject stringObj = mva as StringObject;
                        //    if (stringObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是stringObj类型!!");
                        //        return;
                        //    }
                        //    stringObj.SetValue(svalue.stringValue);
                        //}
                        ////else if( mva.eType == EVMType.Object )
                        ////{
                        ////    AnyObject anyObj = m_MemberObjectArray[index] as AnyObject;
                        ////    if (anyObj == null)
                        ////    {
                        ////        anyObj.SetValue(EVMType.Class, svalue.sobject);
                        ////        return;
                        ////    }
                        ////    //classObj.SetValue(svalue.sobject as ClassObject);
                        ////    m_MemberObjectArray[index] = svalue.sobject;
                        ////}
                        //else
                        {
                            ClassObject classObj = null;
                            if (anyobj != null)
                            {
                                m_MemberObjectArray[index] = svalue.sobject;
                                //anyobj.SetValue(svalue.sobject);
                                return;
                            }
                            classObj = m_MemberObjectArray[index] as ClassObject;
                            if (classObj == null)
                            {
                                //AnyObject anyObj = m_MemberObjectArray[index] as AnyObject;
                                //if( anyObj != null )
                                //{
                                //    anyObj.SetValue(EVMType.Class, svalue.sobject);
                                //    return;
                                //}
                                Debug.Assert(false);
                                Log.AddVM(EError.None, "该类型不是classObj类型!!");
                                return;
                            }
                            //classObj.SetValue(svalue.sobject as ClassObject);
                            m_MemberObjectArray[index] = svalue.sobject as ClassObject;
                        }
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_Object != null )
            {
                sb.Append(m_Object.ToFormatString());
            }
            sb.Append(m_RuntimeType.irClass.ToString());
            //for( int i = 0; i < m_MemberVariableArray)

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_RuntimeType.ToString());

            return sb.ToString();
         }
    }
}
