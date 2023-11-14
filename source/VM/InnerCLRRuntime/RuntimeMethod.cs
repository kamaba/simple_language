//****************************************************************************
//  File:      RuntimeMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: master use .net clr system. new create method instance than running code virtual machine 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SimpleLanguage.VM.Runtime
{
    public class RuntimeMethod
    {
        public string id { get; set; } = "";
        public int level { get; set; } = 0;
        public bool isPersistent { get; set; } = false;
        public SObject[] returnObjectArray => m_ReturnObjectArray;

        private SValue[] m_ValueStack = null;
        private ushort m_ValueIndex = 0;
        //private SValue[] m_ArgumentVariableValueArray = null;
        //private SValue[] m_LocalVariableValueArray = null;

        private SObject[] m_LocalVariableObjectArray = null;
        private SObject[] m_ArgumentObjectArray = null;
        private SObject[] m_ReturnObjectArray = null;


        private IRMethod m_IRMethod = null;
        private IRData[] m_IRDataList = null;
        private ushort m_ExecuteIndex = 0;
        private ushort m_ExecuteCount = 0;
        public RuntimeMethod(IRMethod mmf)
        {
            m_IRMethod = mmf;
            m_IRDataList = mmf.IRDataList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;

            id = mmf.id;

            Init();
        }
        public RuntimeMethod(List<IRData> irList )
        {
            m_IRDataList = irList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            Init();
        }
        void Init()
        {
            //参数列表 argument variable table
            if(m_IRMethod != null )
            {
                m_ReturnObjectArray = new SObject[m_IRMethod.methodReturnVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodReturnVariableList.Count; i++)
                {
                    SObject sobj = ObjectManager.CreateObjectByDefineType(m_IRMethod.methodReturnVariableList[i].metaVariable.metaDefineType);
                    sobj.SetVoid();
                    m_ReturnObjectArray[i] = sobj;
                }
                m_ArgumentObjectArray = new SObject[m_IRMethod.methodArgumentList.Count];
                for (int i = 0; i < m_IRMethod.methodArgumentList.Count; i++)
                {
                    SObject sobj = ObjectManager.CreateObjectByDefineType(m_IRMethod.methodArgumentList[i].metaVariable.metaDefineType);
                    m_ArgumentObjectArray[i] = sobj;
                }
                for( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
                {
                    //Console.WriteLine( "Argu_" + i.ToString() + "_Value: [" + m_ArgumentObjectArray[i].ToString() + "]" );
                }                

                //局部变量列表 local variable table
                m_LocalVariableObjectArray = new SObject[m_IRMethod.methodLocalVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodLocalVariableList.Count; i++)
                {
                    var mev = m_IRMethod.methodLocalVariableList[i];
                    var mdt = mev.metaVariable.metaDefineType;
                    if (mdt.metaClass != null)
                    {
                        SObject sobj = ObjectManager.CreateObjectByDefineType(mdt);
                        m_LocalVariableObjectArray[i] = sobj;
                    }
                }
                for (int i = 0; i < m_LocalVariableObjectArray.Length; i++)
                {
                    //Console.WriteLine("Variable_" + i.ToString() + m_LocalVariableObjectArray[i].ToString());
                }

                var count = m_IRMethod.IRDataList.Count;
                if (count < 48)
                {
                    m_ValueStack = new SValue[128];
                }
                else if (count >= 48 && count < 150)
                {
                    m_ValueStack = new SValue[160];
                }
                else if (count >= 150 && count < 300)
                {
                    m_ValueStack = new SValue[200];
                }
                else if (count >= 300 && count < 500)
                {
                    m_ValueStack = new SValue[300];
                }
                else if (count >= 500 && count < 800)
                {
                    m_ValueStack = new SValue[400];
                }
                else
                {
                    m_ValueStack = new SValue[500];
                }
            }
            else
            {
                m_ArgumentObjectArray = new SObject[0];
                m_LocalVariableObjectArray = new SObject[0];
                m_ValueStack = new SValue[255];
            }
        }
        public void AddReturnObjectArray( SObject[] sobjs )
        {
            for( int i = 0; i < sobjs.Length; i++ )
            {
                if( !sobjs[i].isVoid )
                {
                    m_ValueStack[m_ValueIndex++].SetSObject(sobjs[i]);
                }
            }
        }
        public SObject GetLocalVariableValue( int index )
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Console.WriteLine("执行的栈超出范围!!");
                return null;
            }
            return m_LocalVariableObjectArray[index];
        }
        public SObject GetArgumentValue( int index )
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return null;
            }
            return m_ArgumentObjectArray[index];
        }
        public void SetArgumentValue( int index, SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_ArgumentObjectArray[index], ref svalue);
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Console.WriteLine("执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_LocalVariableObjectArray[index], ref svalue);
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (index > m_ReturnObjectArray.Length)
            {
                Console.WriteLine("执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_ReturnObjectArray[index], ref svalue);
        }
        public SValue GetCurrentIndexValue( bool isRecude )
        {
            if(isRecude)
            {
                m_ValueIndex--;
                return m_ValueStack[m_ValueIndex];
            }
            else
            {
                return m_ValueStack[m_ValueIndex];
            }
        }
        public void Run()
        {
            string funName = id;

            string pushChar = "";
            for( int i = 0; i < level; i++ )
            {
                pushChar = '\t' + pushChar;
            }
            Console.WriteLine(pushChar + "[VMRuntime] [Push] Method: [" + funName +"]-------------------------" );
            level++;

            var topClrRuntime = InnerCLRRuntimeVM.topCLRRuntime;
            for( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
            {
                SValue sval = topClrRuntime.GetCurrentIndexValue(true);
                SetArgumentValue(m_ArgumentObjectArray.Length - i - 1, sval);
            }

            while(true)
            {
                if (m_ExecuteIndex >= m_ExecuteCount)
                {                    
                    break;
                }
                RunInstruction(m_IRDataList[m_ExecuteIndex]);
            }
            level--;
            pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Console.WriteLine();
            Console.WriteLine(pushChar  + "[VMRuntime] [Pop] Method: [" + funName + "]-------------------------");
        }
        public unsafe void RunInstruction( IRData iri )
        {
            m_ExecuteIndex++;
            switch ( iri.opCode )
            {
                case EIROpCode.Nop:
                    break;
                case EIROpCode.LoadConstNull:
                    {
                        m_ValueStack[m_ValueIndex++].SetNullValue();
                    }
                    break; 
                case EIROpCode.LoadConstByte:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt8Value((Byte)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstSByte:
                    {
                        m_ValueStack[m_ValueIndex++].SetSInt8Value((SByte)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        m_ValueStack[m_ValueIndex++].SetBoolValue((bool)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstChar:
                    {
                        m_ValueStack[m_ValueIndex++].SetCharValue((Char)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt16Value((Int16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt16Value((Int16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt32Value((Int32)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt32Value((Int32)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt64Value((Int64)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt64Value((UInt64)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        m_ValueStack[m_ValueIndex++].SetStringValue( (String)iri.opValue );
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        var vval = GetArgumentValue(iri.index);
                        m_ValueStack[m_ValueIndex++].SetSObject(vval);
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        var vval = GetLocalVariableValue(iri.index);
                        m_ValueStack[m_ValueIndex++].SetSObject( vval );
                    }
                    break;
                case EIROpCode.StoreLocal:
                    {
                        SetLocalVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                    }
                    break;
                case EIROpCode.StoreReturn:
                    {
                        SetReturnVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                    }
                    break; 
                case EIROpCode.LoadNotStaticField:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];
                        if (v.eType == EType.Class)
                        {
                            (v.sobject as ClassObject).GetMemberVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex-1]);
                        }
                    }
                    break;
                case EIROpCode.StoreNotStaticField:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];
                        if (v.eType == EType.Class)
                        {
                            (v.sobject as ClassObject).SetMemberVariableSValue(iri.index, m_ValueStack[m_ValueIndex - 2]);
                        }
                        m_ValueIndex-=2;
                    }
                    break; 
                case EIROpCode.LoadStaticField:
                    {
                        InnerCLRRuntimeVM.GetStaticVariable(iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.StoreStaticField:
                    {                        
                        InnerCLRRuntimeVM.SetStaticVariable(iri.index, m_ValueStack[--m_ValueIndex]);
                    }
                    break;
                case EIROpCode.Call:
                    {
                        var mfc = iri.opValue as IRMethod;
                        InnerCLRRuntimeVM.RunIRMethod(mfc);
                    }
                    break;
                case EIROpCode.CallCSharpMethod:
                    {
                        var mfc = iri.opValue as IRCallFunction;
                        Object targetObject = null;
                        Object[] paramsObj = new Object[mfc.paramCount];
                        if( mfc.target )
                        {
                            targetObject = m_ValueStack[--m_ValueIndex].CreateCSharpObject();
                        }
                        for (int i = 0; i < paramsObj.Length; i++)
                        {
                            paramsObj[i] = m_ValueStack[--m_ValueIndex].CreateCSharpObject();
                        }
                        Object obj = mfc.InvokeCSharp(targetObject, paramsObj);
                        if (obj != null)
                        {
                            m_ValueStack[m_ValueIndex++].CreateSObjectByCSharpObject(obj);
                        }
                    }
                    break;
                case EIROpCode.CallCSharpStatements:
                    {
                        //var mfc = iri.opValue as IRCallFunction;
                        //Object[] paramsObj = new Object[mfc.paramCount];
                        //for (int i = 0; i < paramsObj.Length; i++)
                        //{
                        //    paramsObj[i] = m_ValueStack[--m_ValueIndex].CreateCSharpObject();
                        //}
                        //Object obj = mfc.InvokeCSharp(paramsObj);
                        //if (obj != null)
                        //{
                        //    m_ValueStack[m_ValueIndex++].CreateSObjectByCSharpObject(obj);
                        //}
                    }
                    break;
                case EIROpCode.NewObject:
                    {
                        MetaType mdt = iri.opValue as MetaType;
                        ClassObject co = new ClassObject(mdt);
                        m_ValueStack[m_ValueIndex++].SetSObject( co );
                    }
                    break;
                case EIROpCode.Label:
                    {                        
                        m_ValueIndex++;
                    }
                    break;
                case EIROpCode.Br:
                case EIROpCode.BrLabel:
                    {
                        m_ExecuteIndex = (ushort)iri.index;
                    }
                    break;
                case EIROpCode.BrFalse:
                    {
                        var v = m_ValueStack[--m_ValueIndex];
                        if (v.eType == EType.Boolean)
                        {
                            if (v.int8Value == 0)
                            {
                                m_ExecuteIndex = (ushort)iri.index;
                            }
                        }
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        var v = m_ValueStack[--m_ValueIndex];
                        if (v.eType == EType.Boolean)
                        {
                            if (v.int8Value == 1)
                            {
                                m_ExecuteIndex = (ushort)iri.index;
                            }
                        }
                    }
                    break;
                case EIROpCode.Add:
                    {
                        if( m_ValueIndex - 1 < 0 )
                        {
                            Console.WriteLine("Error 加法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-2].AddSValue(ref m_ValueStack[m_ValueIndex-1], false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Minus:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 减法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].MinusSValue(m_ValueStack[m_ValueIndex-1], false );
                        m_ValueStack[m_ValueIndex-2].ComputeSVAlue(1, ref m_ValueStack[m_ValueIndex-1], false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Multiply:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 乘法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].MultiplySValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(2, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Divide:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 除法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].DivSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(3, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Modulo:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 余法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].ModuloSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(4, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Combine:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 合并运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].CombineSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(5, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.InclusiveOr:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 包括运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].InclusiveOrSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(6, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.XOR:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 或运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].XORSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(7, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shr:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 右移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShrSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(8, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shi:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error 左移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShiSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(9, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error Not运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex--].NotSValue();
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Console.WriteLine("Error Neg运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex--].NegSValue(false);
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 4, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Or:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 5, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Ceq:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 0, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cne:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 1, false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cgt:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 2, false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cge:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 2, true);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Clt:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 3, false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Cle:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Console.WriteLine("Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 3, true);
                        m_ValueIndex--;
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error 暂不支持" + iri.opCode.ToString() + "的处理!!");
                    }
                    break;
            }
        }
    }
}
