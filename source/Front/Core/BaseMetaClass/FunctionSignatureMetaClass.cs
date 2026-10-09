//****************************************************************************
//  File:      FunctionSignatureMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/26 12:00:00
//  Description: 函数签名类型, 用于 typealias 定义的函数类型结构
//      语法: typealias CalcFunc = int Function( int, int )
//      继承 FunctionMetaClass, 携带返回类型与参数类型列表
//      IR 序列化时映射回 functionMetaClass (C VM 统一用 NewClosure/CallClosure)
//****************************************************************************

using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class FunctionSignatureMetaClass : FunctionMetaClass
    {
        public MetaType returnMetaType => m_ReturnMetaType;
        public List<MetaType> paramMetaTypeList => m_ParamMetaTypeList;

        private MetaType m_ReturnMetaType = null;
        private List<MetaType> m_ParamMetaTypeList = new List<MetaType>();

        public FunctionSignatureMetaClass( string aliasName, MetaType returnType, List<MetaType> paramTypes )
            : base()
        {
            // allName 设为 "FunctionSig_<aliasName>", 使 classId 唯一但不与内置 functionMetaClass 冲突
            m_Name = "FunctionSig_" + aliasName;
            m_AllName = m_Name;
            // SL 类型系统层面继承内置 Function 类型(基类构造默认继承 Object),
            // 使 Func<> 签名类型与 function/function 声明变量之间可双向类型兼容
            // (ValidateClassTypeRelation 走 extend 链, 参数匹配/赋值检查依赖该关系)
            SetExtendClass( CoreMetaClassManager.functionMetaClass );
            m_ReturnMetaType = returnType;
            if ( paramTypes != null )
            {
                m_ParamMetaTypeList = new List<MetaType>( paramTypes );
            }
        }
    }
}
