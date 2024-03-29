﻿//****************************************************************************
//  File:      ClassObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.VM
{
    public class MemberVariableData
    {
        public MetaMemberVariable memberVariable = null;
        public int index { get; set; } = 0;
        public int start { get; set; } = 0;
        public int length { get; set; } = 0;
    }
    public class ClassObject : SObject
    {
        public ClassObject value => m_Object;

        private MetaType m_MetaDefineType;
        private ClassObject m_Object = null;
        private byte[] m_Data = null;
        private MemberVariableData[] m_MemberVariableData = null;
        //Debug
        private SObject[] m_MemberVariableArray = null;


        public ClassObject( MetaType mdt )
        {
            m_MetaDefineType = mdt;

            int byteCount = mdt.metaClass.byteCount;
            m_Data = new byte[byteCount];

            var mvdict = mdt.metaClass.localMetaMemberVariables;

            m_MemberVariableData = new MemberVariableData[mvdict.Count];
            m_MemberVariableArray = new SObject[mvdict.Count];
            for( int i = 0; i < mvdict.Count; i++ )
            {
                var obj = ObjectManager.CreateObjectByDefineType(mvdict[i].metaDefineType);
                m_MemberVariableArray[i] = obj;
            }
        }
        public int GetMemberVariableIndex( MetaMemberVariable mmv )
        {
            foreach (var v in m_MemberVariableData)
            {
                if (v.memberVariable == mmv)
                    return v.index;
            }
            return -1;
        }
        public SObject GetMemberVariable(int index)
        {
            if (index > m_MemberVariableArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return null;
            }
            return m_MemberVariableArray[index];
        }
        public void SetValue(ClassObject val )
        {
            m_Object = val;
            val.refCount++;
        }
        public void GetMemberVariableSValue( int index, ref SValue svalue )
        {
            if (index > m_MemberVariableArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return;
            }
            var mmv = m_MemberVariableArray[index];
            switch (mmv)
            {
                case Int16Object int16Obj:
                    {
                        svalue.SetInt16Value( int16Obj.value );
                    }
                    break;
                case Int32Object int32Obj:
                    {
                        svalue.SetInt32Value( int32Obj.value );
                    }
                    break;
                case Int64Object int64Obj:
                    {
                        svalue.SetInt64Value( int64Obj.value );
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
            }
        }
        public void SetMemberVariableSValue( int index, SValue svalue)
        {
            if (index > m_MemberVariableArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return;
            }
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        ClassObject classObj = m_MemberVariableArray[index] as ClassObject;
                        if (classObj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        classObj.SetNull();
                    }
                    break;
                case EType.Int16:
                    {
                        Int16Object int32Obj = m_MemberVariableArray[index] as Int16Object;
                        if (int32Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        Int32Object int32Obj = m_MemberVariableArray[index] as Int32Object;
                        if (int32Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        Int64Object int64Obj = m_MemberVariableArray[index] as Int64Object;
                        if (int64Obj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.String:
                    {
                        StringObject stringObj = m_MemberVariableArray[index] as StringObject;
                        if (stringObj == null)
                        {
                            Console.WriteLine("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        var mva = m_MemberVariableArray[index];
                        if (mva.eType == EType.Byte)
                        {

                            ByteObject byteObj = mva as ByteObject;
                            if (byteObj == null)
                            {
                                Console.WriteLine("该类型不是Int32类型!!");
                                return;
                            }
                            byteObj.SetValue(svalue.int8Value);
                        }
                        else if (mva.eType == EType.SByte)
                        {

                            SByteObject sbyteObj = mva as SByteObject;
                            if (sbyteObj == null)
                            {
                                Console.WriteLine("该类型不是Int32类型!!");
                                return;
                            }
                            sbyteObj.SetValue(svalue.sint8Value);
                        }
                        else if (mva.eType == EType.Int16)
                        {

                            Int16Object int16Obj = mva as Int16Object;
                            if (int16Obj == null)
                            {
                                Console.WriteLine("该类型不是Int16类型!!");
                                return;
                            }
                            int16Obj.SetValue(svalue.int16Value);
                        }
                        else if (mva.eType == EType.UInt16)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Console.WriteLine("该类型不是UInt32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int32)
                        {
                            Int32Object int32Obj = mva as Int32Object;
                            if (int32Obj == null)
                            {
                                Console.WriteLine("该类型不是Int32类型!!");
                                return;
                            }
                            int32Obj.SetValue(svalue.int32Value);
                        }
                        else if (mva.eType == EType.UInt32)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Console.WriteLine("该类型不是Int32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int64)
                        {

                            Int64Object int64Obj = mva as Int64Object;
                            if (int64Obj == null)
                            {
                                Console.WriteLine("该类型不是Int64类型!!");
                                return;
                            }
                            int64Obj.SetValue(svalue.int64Value);
                        }
                        else if (mva.eType == EType.UInt64)
                        {

                            UInt64Object uint64Obj = mva as UInt64Object;
                            if (uint64Obj == null)
                            {
                                Console.WriteLine("该类型不是Int64类型!!");
                                return;
                            }
                            uint64Obj.SetValue(svalue.uint64Value);
                        }
                        else if (mva.eType == EType.String)
                        {

                            StringObject stringObj = mva as StringObject;
                            if (stringObj == null)
                            {
                                Console.WriteLine("该类型不是stringObj类型!!");
                                return;
                            }
                            stringObj.SetValue(svalue.stringValue);
                        }
                        else
                        {
                            ClassObject classObj = m_MemberVariableArray[index] as ClassObject;
                            if (classObj == null)
                            {
                                Console.WriteLine("该类型不是Int32类型!!");
                                return;
                            }
                            classObj.SetValue(svalue.sobject as ClassObject);
                        }
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
            sb.Append(m_MetaDefineType.ToFormatString());
            //for( int i = 0; i < m_MemberVariableArray)

            return sb.ToString();
        }
        public override string ToString()
        {
            return m_MetaDefineType.allName + "  " ;
         }
    }
}
