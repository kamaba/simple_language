using System.Runtime.InteropServices;
using SimpleLanguage.VM.Runtime;
using System;
using System.Text;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public class RuntimeObject
    {
        public RuntimeType runtimeType => m_RuntimeType;
        public EVMType eType => m_RuntimeType != null ? m_RuntimeType.eType : EVMType.Null;
        public SObject sobject => m_SObject;
        public RuntimeVariable runtimeVariable => m_RuntimeVariable;
        public bool isNull => m_SObject == null;

        private RuntimeVariable m_RuntimeVariable = null;
        private RuntimeType m_RuntimeType = null;
        private SObject m_SObject = null;
        public RuntimeObject( RuntimeType rt, SObject sobj )
        {
            m_RuntimeType = rt;
            m_SObject = sobj;
        }
        public RuntimeObject( RuntimeType rt, RuntimeVariable rv, SObject sobj )
        {
            m_RuntimeVariable = rv;
            m_RuntimeType = rt;
            m_SObject = sobj;
        }   
        public void SetNull()
        {
            m_SObject = null;
        }
        public void SetSObject( SObject sobj )
        {
            m_SObject = sobj;
        }
        public void SetSObjectBySValue( ref SValue sval )
        {
            if( m_RuntimeType.eType == EVMType.Object )
            {
                m_SObject = sval.GetSObject();
                return;
            }

            switch(m_RuntimeType.eType)
            {
                case EVMType.Boolean:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as BoolObject;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int8Value == 1);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Boolean, sval.int8Value==1);
                        }
                    }
                    break;
                case EVMType.Byte:
                    {
                        if(m_SObject == null )
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int8Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int8Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Byte, sval.int8Value);
                        }
                    }
                    break;
                case EVMType.SByte:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as SInt8Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.sint8Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.SByte, sval.sint8Value);
                        }
                    }
                    break;
                case EVMType.Int16:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int16Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int16Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Int16, sval.int16Value);
                        }
                    }
                    break;
                case EVMType.UInt16:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as UInt16Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.uint16Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.UInt16, sval.uint16Value);
                        }
                    }
                    break;
                case EVMType.Int32:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int32Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int32Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Int32, sval.int32Value);
                        }
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int32Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.uint32Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.UInt32, sval.uint32Value);
                        }
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int64Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Int64, sval.int64Value);
                        }
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.uint64Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.UInt64, sval.uint64Value);
                        }
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Float32Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.floatValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Float32, sval.floatValue);
                        }
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Float64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.doubleValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Float64, sval.doubleValue);
                        }
                    }
                    break;
                case EVMType.String:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as StringObject;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.stringValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.String, sval.stringValue);
                        }
                    }
                    break;
                case EVMType.Class:
                case EVMType.Array:
                    {
                        m_SObject = sval.sobject;
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
        public SObject CreateObjectByRuntimeType()
        {
            if (m_SObject == null)
            {
                m_SObject = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType, true);
            }
            return m_SObject;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_RuntimeType != null )
            {
                sb.Append(m_RuntimeType.ToString());
            }
            if( m_SObject != null )
            {
                sb.Append(m_SObject.ToString());
            }

            return sb.ToString();
        }
    }
}
