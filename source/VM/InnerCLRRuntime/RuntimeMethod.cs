//****************************************************************************
//  File:      RuntimeMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: master use .net clr system. new create method instance than running code virtual machine 
//****************************************************************************
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
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

        private Dictionary<string, IRMetaClass>  m_InputTemplateClassDict = new Dictionary<string, IRMetaClass>();
        private SObject[] m_LocalVariableObjectArray = null;
        private SObject[] m_ArgumentObjectArray = null;
        private SObject[] m_ReturnObjectArray = null;


        private IRMetaClass m_IRMetaClass = null;
        //调用函数的实体类，如果是普通的类，则可以不管这个参数
        private IRMetaClass m_CallIRMetaClass = null;
        private IRMethod m_IRMethod = null;
        private IRData[] m_IRDataList = null;
        private ushort m_ExecuteIndex = 0;
        private ushort m_ExecuteCount = 0;
        public RuntimeMethod( IRMetaClass irmc, IRMethod mmf )
        {
            m_IRMetaClass = irmc;
            m_IRMethod = mmf;
            m_IRDataList = mmf.IRDataList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;

            id = mmf.id;

            Init();
        }
        public RuntimeMethod( List<IRData> irList )
        {
            m_IRDataList = irList.ToArray();
            m_ExecuteCount = (ushort)m_IRDataList.Length;
            Init();
        }
        void Init()
        {
            if(m_IRMetaClass != null )
            {
                if( m_IRMetaClass.genClass )
                {
                    foreach( var v in m_IRMetaClass.genTemplateIRMetaClassDict )
                    {
                        AddTemplateIRMetaClass(v.Key, v.Value);
                    }
                }
            }

            //参数列表 argument variable table
            if(m_IRMethod != null )
            {
                m_ReturnObjectArray = new SObject[m_IRMethod.methodReturnVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodReturnVariableList.Count; i++)
                {
                    SObject sobj = ObjectManager.CreateObjectByDefineType(m_IRMethod.methodReturnVariableList[i].irMetaClass );
                    //sobj.SetVoid();
                    m_ReturnObjectArray[i] = sobj;
                }

                m_ArgumentObjectArray = new SObject[m_IRMethod.methodArgumentList.Count];
                for (int i = 0; i < m_IRMethod.methodArgumentList.Count; i++)
                {
                    SObject sobj = ObjectManager.CreateObjectByDefineType(m_IRMethod.methodArgumentList[i].irMetaClass);
                    m_ArgumentObjectArray[i] = sobj;
                }
                for( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
                {
                    Log.AddVM(EError.None, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentObjectArray[i].ToString() + "]" );
                }                

                //局部变量列表 local variable table
                m_LocalVariableObjectArray = new SObject[m_IRMethod.methodLocalVariableList.Count];
                for (int i = 0; i < m_IRMethod.methodLocalVariableList.Count; i++)
                {
                    var mev = m_IRMethod.methodLocalVariableList[i];
                    SObject sobj = ObjectManager.CreateObjectByDefineType(mev.irMetaClass);
                    m_LocalVariableObjectArray[i] = sobj;
                }
                for (int i = 0; i < m_LocalVariableObjectArray.Length; i++)
                {
                    Log.AddVM(EError.None, "Variable_" + i.ToString() + m_LocalVariableObjectArray[i].ToString());
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
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return null;
            }
            if(index < 0 )
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!-");
                return null;
            }

            return m_LocalVariableObjectArray[index];
        }
        public SObject GetArgumentValue( int index )
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, $"SVM Error FunctionName:{this.id} 执行的参数超出范围!!");
                return null;
            }
            return m_ArgumentObjectArray[index];
        }
        public void SetArgumentValue( int index, SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_ArgumentObjectArray[index], ref svalue);
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetObjectByValue(m_LocalVariableObjectArray[index], ref svalue);
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (index > m_ReturnObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
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
        public IRMetaClass GetTemplateIRMetaClass( string name )
        {
            if(m_InputTemplateClassDict.ContainsKey(name ) )
            {
                return m_InputTemplateClassDict[name];
            }
            Log.AddVM(EError.None, "Erorr------没有找到模板内容!!!");
            return null;
        }
        public SObject CreateObjectByIRMetaClass( IRMetaClass mdt )
        {
            SObject sobj = null;
            if (mdt.isTemplate)
            {
                var newmdt = GetTemplateIRMetaClass(mdt.irName);
                sobj = ObjectManager.CreateObjectByDefineType(newmdt);
            }
            else
            {
                sobj = ObjectManager.CreateObjectByDefineType(mdt);
            }
            return sobj;
        }
        public void AddTemplateIRMetaClass( string name, IRMetaClass templateInstanceObject )
        {
            if( !m_InputTemplateClassDict.ContainsKey(name ) )
            {
                m_InputTemplateClassDict.Add(name, templateInstanceObject);
                return;
            }
            Log.AddVM(EError.None, "Erorr------添加模板内容重复!!!");
        }
        public void Run()
        {
            string funName = id;

            string pushChar = "";
            for( int i = 0; i < level; i++ )
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Push] Method: [" + funName +"]" );
            level++;

            var topClrRuntime = InnerCLRRuntimeVM.topCLRRuntime;
            for ( int i = 0; i < m_ArgumentObjectArray.Length; i++ )
            {
                SValue sval = topClrRuntime.GetCurrentIndexValue(true);
                SetArgumentValue(m_ArgumentObjectArray.Length - i - 1, sval);
            }

            while (true)
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
            Log.AddVM( EError.None, pushChar  + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        public IRMetaClass GetIRMetaClassByName( string tname )
        {
            if (string.IsNullOrEmpty(tname))
            {
                Log.AddVM(EError.None, "SetCallClass 没有找到相关的模板名称1 ");
                return null;
            }
            StringBuilder sb = new StringBuilder();
            string findname = "";
            bool isFind = false;
            foreach (var v in tname)
            {
                if (v == '$')
                {
                    if (isFind)
                    {
                        var fsb = sb.ToString();
                        var gmc = GetTemplateIRMetaClass(fsb);
                        if (gmc == null)
                        {
                            Log.AddVM(EError.None, $"没有找到该模板的对应类!{fsb}");
                            return null;
                        }
                        findname = findname + gmc.irName;
                        isFind = false;
                    }
                    else
                    {
                        findname = findname + sb.ToString();
                        isFind = true;
                    }
                    sb.Clear();
                }
                else
                {
                    sb.Append(v);
                }
            }
            findname = findname + sb.ToString();

            IRMetaClass irc = IRManager.instance.GetIRMetaClassByName(findname);
            if (irc == null)
            {
                Log.AddVM(EError.None, "NewTemplateClass IRC是调用虚函数为空!!");
                return null;
            }
            return irc;
        }
        public void RunInstruction( IRData iri )
        {
            //栈位的移动的规则，使用当前位为空的概念，只要栈被使用掉，索引则加1，所以索引最少为0
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
                //case EIROpCode.LoadConstChar:
                //    {
                //        m_ValueStack[m_ValueIndex++].SetCharValue((Char)iri.opValue);
                //    }
                //    break;
                case EIROpCode.LoadConstInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt16Value((Int16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt16Value((UInt16)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetInt32Value((Int32)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        m_ValueStack[m_ValueIndex++].SetUInt32Value((UInt32)iri.opValue);
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
                case EIROpCode.LoadConstFloat:
                    {
                        m_ValueStack[m_ValueIndex++].SetFloatValue((Single)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstDouble:
                    {
                        m_ValueStack[m_ValueIndex++].SetDoubleValue((Double)iri.opValue);
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        m_ValueStack[m_ValueIndex++].SetStringValue( (String)iri.opValue );
                    }
                    break;
                case EIROpCode.Convert_I8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Byte);
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.SByte);
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int16);
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt16);
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int32);
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt32);
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Int64);
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.UInt64);
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        m_ValueStack[m_ValueIndex-1].ConvertByEType(EType.Float32);
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EType.Float64);
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
                            var co = (v.sobject as ClassObject);
                            co.value.GetMemberVariableSValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
                        }
                        //栈位不变，因为当前对象位的被通过索引取出来的成员变量值，覆盖掉， 所以栈位不会发生变化
                    }
                    break;
                case EIROpCode.StoreNotStaticField:                   
                    {
                        // -2在存储的值 -1表示要存储的对象 存储完成，直接变成位置0
                        var v = m_ValueStack[m_ValueIndex - 1];
                        SValue sv = m_ValueStack[m_ValueIndex - 2];
                        switch ( v.eType )
                        {
                            case EType.Boolean:
                            case EType.Byte: v.int8Value = sv.int8Value; break;
                            case EType.SByte: v.sint8Value = sv.sint8Value; break;
                            case EType.Int16: v.int16Value = sv.int16Value; break;
                            case EType.UInt16: v.uint16Value = sv.uint16Value; break;
                            case EType.Int32: v.int32Value = sv.int32Value; break;
                            case EType.UInt32: v.uint32Value = sv.uint32Value; break;
                            case EType.Int64: v.int64Value = sv.int64Value; break;
                            case EType.UInt64: v.uint64Value = sv.uint64Value; break;
                            case EType.Float32: v.floatValue = sv.floatValue; break;
                            case EType.Float64: v.doubleValue = sv.doubleValue; break;
                            case EType.String: v.stringValue = sv.stringValue; break;
                            case EType.Class:
                                {
                                    (v.sobject as ClassObject).SetMemberVariableSValue(iri.index, sv );
                                }
                                break;
                            default:
                                {
                                    Log.AddVM(EError.None, "Error StoreNotStaticField Path:" + iri.debugInfo.path + " Line: " + iri.debugInfo.beginLine);
                                }
                                break;
                        }
                        m_ValueStack[m_ValueIndex - 2] = v;
                        m_ValueIndex -= 1;
                    }
                    break;
                case EIROpCode.LoadStaticField:
                    {
                        InnerCLRRuntimeVM.GetStaticVariable(m_CallIRMetaClass, iri.index, ref m_ValueStack[m_ValueIndex++]);
                    }
                    break;
                case EIROpCode.StoreStaticField:
                    {                        
                        InnerCLRRuntimeVM.SetStaticVariable(m_CallIRMetaClass, iri.index, ref m_ValueStack[--m_ValueIndex]);
                    }
                    break;
                case EIROpCode.TemplateReplace:
                    {
                        string tname = iri.opValue as String;
                        if( string.IsNullOrEmpty( tname ) )
                        {
                            Log.AddVM(EError.None, "没有找到相关的模板名称");
                            return;
                        }
                        m_CallIRMetaClass = GetTemplateIRMetaClass(tname);
                        if (string.IsNullOrEmpty(tname))
                        {
                            Log.AddVM(EError.None, "没有找到相关的模板名称的类!!");
                            return;
                        }
                    }
                    break;
                //case EIROpCode.SetCurrentClassCallClass:
                //    {
                //        m_CallIRMetaClass = m_IRMetaClass;
                //    }
                //    break;
                case EIROpCode.SetCallClass:
                    {
                        string tname = iri.opValue as String;

                        m_CallIRMetaClass = GetIRMetaClassByName(tname);
                    }
                    break;
                case EIROpCode.UnSetCallClass:
                    {
                        m_CallIRMetaClass = null;
                    }
                    break;
                case EIROpCode.Call:
                    {
                        var mfc = iri.opValue as IRMethod;
                        InnerCLRRuntimeVM.RunIRMethod(m_CallIRMetaClass, mfc);
                    }
                    break;
                case EIROpCode.CallVirt:
                    {
                        int stackFrontIndex = (int)iri.opValue;
                        int stackIndex = m_ValueIndex - stackFrontIndex;
                        if( stackIndex < 0 )
                        {
                            Log.AddVM(EError.None, "StackIndex 是负数!");
                            return;
                        }

                        var v = m_ValueStack[stackIndex];

                        IRMetaClass irc = null;
                        if (v.eType == EType.Class)
                        {
                            var co = (v.sobject as ClassObject);
                            irc = co.value.irMetaClass;
                        }
                        if( irc == null )
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            return;
                        }
                        IRMethod cfc = irc.GetIRNonStaticMethodByIndex(iri.index);
                        InnerCLRRuntimeVM.RunIRMethod(irc, cfc);
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
                case EIROpCode.NewObject:
                    {
                        IRMetaClass mdt = iri.opValue as IRMetaClass;
                        SObject sob = CreateObjectByIRMetaClass(mdt);
                        if ( sob is ClassObject co )
                        {
                            ObjectManager.AddClassObject(co);
                        }
                        m_ValueStack[m_ValueIndex++].SetSObject(sob);
                    }
                    break;
                case EIROpCode.NewTemplateClass:
                    {
                        //int stackIndex = m_ValueIndex - 1;
                        //if (stackIndex < 0)
                        //{
                        //    Log.AddVM(EError.None, "NewTemplateClass StackIndex 是负数!");
                        //    return;
                        //}
                        //var v = m_ValueStack[stackIndex];

                        //IRMetaClass irc = null;
                        //if (v.eType == EType.Class)
                        //{
                        //    var co2 = (v.sobject as ClassObject);
                        //    irc = co2.value.irMetaClass;
                        //}
                        string tname = iri.opValue as String;
                        if (string.IsNullOrEmpty(tname))
                        {
                            Log.AddVM(EError.None, "没有找到相关的模板名称");
                            return;
                        }
                        IRMetaClass irc = GetIRMetaClassByName(tname);
                        if (irc == null)
                        {
                            Log.AddVM(EError.None, "NewTemplateClass IRC是调用虚函数为空!!");
                            return;
                        }
                        SObject sob = CreateObjectByIRMetaClass(irc);
                        if (sob is ClassObject co)
                        {
                            ObjectManager.AddClassObject(co);
                        }
                        m_ValueStack[m_ValueIndex].SetSObject(sob);
                    }
                    break;
                case EIROpCode.Dup:
                    {
                        var sval = m_ValueStack[m_ValueIndex - 1];
                        m_ValueStack[m_ValueIndex++] = sval;
                    }
                    break;
                case EIROpCode.Pop:
                    {
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Label:
                    {
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
                        if( m_ValueIndex - 2 < 0 )
                        {
                            Log.AddVM(EError.None, "Error 加法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-2].AddSValue(ref m_ValueStack[m_ValueIndex-1], false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Minus:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 减法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex-2].ComputeSVAlue(1, ref m_ValueStack[m_ValueIndex - 1], false );
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Multiply:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 乘法运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(2, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Divide:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 除法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].DivSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(3, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Modulo:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 余法运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].ModuloSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(4, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Combine:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 合并运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].CombineSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(5, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.InclusiveOr:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 包括运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex-2].InclusiveOrSValue(m_ValueStack[m_ValueIndex-1], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(6, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.XOR:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 或运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].XORSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(7, ref m_ValueStack[m_ValueIndex - 1], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shr:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 右移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShrSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(8, ref m_ValueStack[m_ValueIndex], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Shi:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 左移运算!!超出的栈范围");
                            break;
                        }
                        //m_ValueStack[m_ValueIndex--].ShiSValue(m_ValueStack[m_ValueIndex], false);
                        m_ValueStack[m_ValueIndex - 2].ComputeSVAlue(9, ref m_ValueStack[m_ValueIndex], false);
                        m_ValueIndex--;
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Not运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex].NotSValue();
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Neg运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex].NegSValue(false);
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_ValueIndex - 2 < 0)
                        {
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
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
                            Log.AddVM(EError.None, "Error 比较符超出一当前的数据栈!!");
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 2].CompareSValue(m_ValueStack[m_ValueIndex - 1], 3, true);
                        m_ValueIndex--;
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, "Error 暂不支持" + iri.opCode.ToString() + "的处理!!");
                    }
                    break;
            }
        }
    }
}
