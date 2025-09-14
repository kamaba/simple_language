//****************************************************************************
//  File:      IRUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: IR common function
//****************************************************************************

using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRUtil
    {
        public static int GetTypeSize(EType etype)
        {
            switch (etype)
            {
                case EType.Bit:
                    return 1;
                case EType.Byte:
                case EType.Boolean:
                    return 1;
                //case EType.Char:
                //    return 2;
                case EType.Int16:
                case EType.UInt16:
                    return 2;
                case EType.Int32:
                case EType.UInt32:
                case EType.Class:
                case EType.String:
                case EType.Float32:
                    return 4;
                case EType.Int64:
                case EType.UInt64:
                case EType.Float64:
                    return 8;
                case EType.Int128:
                case EType.UInt128:
                    return 16;
                case EType.Float2:
                    return 8;

            }
            return 1;
        }

        public static IRBase GetSetCallClassByMetaClass(MetaClass inputmc, List<MetaType> inputList, out IRMetaClass irmc)
        {
            irmc = null;
            StringBuilder sb = new StringBuilder();
            if (inputmc != null)
            {
                sb.Append(inputmc.metaNode.allName);
                sb.Append("<");
                for (int i = 0; i < inputList.Count; i++)
                {
                    string tname2 = IRManager.GetIRNameByMetaType(inputList[i]);
                    sb.Append("$");
                    sb.Append(tname2);
                    sb.Append("$");
                    if (i < inputList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append('>');
                irmc = IRManager.instance.GetIRMetaClassByName(inputmc.metaNode.allName);

                IRData sc23 = new IRData();
                //sc23.opCode = EIROpCode.SetCallClass;
                //sc23.opValue = sb.ToString();
                IRBase irbase23 = new IRBase(sc23);
                return irbase23;
            }
            return null;
        }

        public static IRBase GetSetCallClass(MetaType mt, MetaClass mc, out IRMetaClass irmc)
        {
            irmc = null;
            if (mt != null)
            {
                string tname = IRManager.GetIRNameByMetaType(mt);
                if (mt.eType == EMetaTypeType.MetaClass)
                {
                    string tn1 = IRManager.GetIRNameByMetaClass(mt.metaClass);
                    irmc = IRManager.instance.GetIRMetaClassByName(tn1);
                }
                else if (mt.eType == EMetaTypeType.Template)
                {
                    string irnewclass = IRManager.GetIRNameByMetaClass(mc != null ? mc : mt.metaClass);
                    //if( mc != null )
                    //{
                    //    tname = IRManager.GetIRNameByMetaClass(mc);
                    //}
                    irmc = IRManager.instance.GetIRMetaClassByName(irnewclass);
                }
                else
                {
                    string tt = IRManager.GetIRNameByMetaClass(mt.metaClass);
                    irmc = IRManager.instance.GetIRMetaClassByName(tt);
                }
                IRData sc23 = new IRData();
                //sc23.opCode = EIROpCode.SetCallClass;
                //sc23.opValue = tname;
                IRBase irbase23 = new IRBase(sc23);
                return irbase23;
            }
            return null;
        }
    }

}
