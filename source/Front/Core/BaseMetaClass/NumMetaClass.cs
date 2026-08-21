//****************************************************************************
//  File:      NumMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/1/18 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class NumMetaClass : MetaClass
    {
        public NumMetaClass() : base(DefaultObject.Num.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Num;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new NumMetaClass();
            return mc;
        }
    }
    public class UInt8MetaClass : MetaClass
    {
        public UInt8MetaClass() : base(DefaultObject.UInt8.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.UInt8;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt8MetaClass();
            return mc;
        }
    }
    public class Int8MetaClass : MetaClass
    {
        public Int8MetaClass() : base(DefaultObject.Int8.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Int8;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int8MetaClass();
            return mc;
        }
    }

    public class Int16MetaClass : MetaClass
    {
        public Int16MetaClass() : base(DefaultObject.Int16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Int16;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int16MetaClass();
            return mc;
        }
    }
    public class UInt16MetaClass : MetaClass
    {
        public UInt16MetaClass() : base(DefaultObject.UInt16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.UInt16;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt16MetaClass();
            return mc;
        }
    }
    public class Int32MetaClass : MetaClass
    {
        public Int32MetaClass() : base(DefaultObject.Int32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Int32;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int32MetaClass();
            return mc;
        }
    }
    public class UInt32MetaClass : MetaClass
    {
        public UInt32MetaClass() : base(DefaultObject.UInt32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.UInt32;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt32MetaClass();
            return mc;
        }
    }
    public class Int64MetaClass : MetaClass
    {
        public Int64MetaClass() : base(DefaultObject.Int64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Int64;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int64MetaClass();
            return mc;
        }
    }
    public class UInt64MetaClass : MetaClass
    {
        public UInt64MetaClass() : base(DefaultObject.UInt64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.UInt64;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt64MetaClass();
            return mc;
        }
    }
    public class Float8MetaClass : MetaClass
    {
        public Float8MetaClass() : base(DefaultObject.Float8.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Float8;
            m_InnderDefine = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float8, 0.0f);
            SetDefaultExpressNode(mcen);


        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float8MetaClass();
            return mc;
        }
    }
    public class Float16MetaClass : MetaClass
    {
        public Float16MetaClass() : base(DefaultObject.Float16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Float16;
            m_InnderDefine = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float16, 0.0f);
            SetDefaultExpressNode(mcen);


        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float16MetaClass();
            return mc;
        }
    }
    public class Float32MetaClass : MetaClass
    {
        public Float32MetaClass() : base(DefaultObject.Float32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_Type = EType.Float32;
            m_InnderDefine = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float32, 0.0f);
            SetDefaultExpressNode(mcen);


        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float32MetaClass();
            return mc;
        }
    }

    public class Float64MetaClass : MetaClass
    {
        public Float64MetaClass() : base(DefaultObject.Float64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.numMetaClass);
            m_InnderDefine = true;
            m_Type = EType.Float64;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float64MetaClass();
            return mc;
        }
    }
}
