//****************************************************************************
//  File:      PtrMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/19 12:00:00
//  Description: 
//****************************************************************************

using System;

namespace SimpleLanguage.Core
{
    public class ResultMetaClass : MetaClass
    {
        public ResultMetaClass() : base(DefaultObject.Result.ToString())
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ResultMetaClass();
            return mc;
        }
    }
    public class ResultTMetaClass : MetaClass
    {
        // 名字必须与 Result.sl 的 "class Result<T>" 一致（"Result"），注册到
        // MetaNode "Result" 的 tc=1 槽，这样：
        //   1) Core 自身编译时源码 class Result<T> 会复用本 inner-form（同 Ptr<T> 模式）；
        //   2) 引用 Core 的工程做 Core replacement 时按 ("Result", tc=1) 能命中本类，
        //      否则会 fallback 错误命中 tc=0 的 Result，导致 Result<T> 的 IRMetaClass 缺失。
        public ResultTMetaClass() : base(DefaultObject.Result.ToString())
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);


            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.None);
            mt.SetIndex(0);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ResultTMetaClass();
            return mc;
        }
    }
}
