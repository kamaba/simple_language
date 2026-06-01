//****************************************************************************
//  File:      DynamicMetaData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class DynamicMetaData : MetaClass
    {
        public DynamicMetaData():base(DefaultObject.Data.ToString())
        {            
            m_InnderDefine = true;
        }      
        public static MetaClass CreateMetaClass()
        {
            DynamicMetaData mc = new DynamicMetaData();
            return mc;
        }
    }
}
