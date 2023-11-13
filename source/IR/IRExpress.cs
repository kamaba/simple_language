//****************************************************************************
//  File:      IRExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  express convert ir code!
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRExpress : IRBase
    {
        private IRManager m_IRManager = null;
        private MetaExpressNode m_Node = null;
        public IRExpress( IRMethod irMethod, MetaExpressNode node ) : base( irMethod )
        {
            m_Node = node;
            CreateIRDataOne(node);
        }
        public IRExpress( IRManager _irManager, MetaExpressNode node ):base()
        {
            m_IRManager = _irManager;
            m_Node = node;
            CreateIRDataOne(node);
        }
        public void CreateIRDataOne(MetaExpressNode node)
        {
            switch (node)
            {
                case MetaConstExpressNode mcn:
                    {
                        IRData irdata = new IRData();
                        irdata.opCode = IRManager.GetConstIROpCode(mcn.eType);
                        irdata.opValue = mcn.value;
                        irdata.SetDebugInfoByToken( mcn.GetToken() );
                        AddIRData(irdata);
                    }
                    break;
                case MetaUnaryOpExpressNode muoen:
                    {
                        MetaExpressNode valNode = muoen.value;
                        CreateIRDataOne(valNode);
                        var signData = CreateOneSignIRData(muoen.opSign);
                        AddIRData(signData);
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        MetaExpressNode leftNode = moen.left;
                        MetaExpressNode rightNode = moen.right;
                        CreateIRDataOne(leftNode);
                        CreateIRDataOne(rightNode);
                        var signData = CreateLeftAndRightIRData(moen.opSign);
                        signData.SetDebugInfoByToken( moen.GetToken() );
                        AddIRData(signData);
                    }
                    break;
                case MetaCallExpressNode mcn:
                    {
                        if( m_IRManager != null )
                        {
                            mcn.metaCallLink.ParseToIRDataListByIRManager(m_IRManager);
                        }
                        else
                        {
                            mcn.metaCallLink.ParseToIRDataList(m_IRMethod);
                        }
                        var list = mcn.metaCallLink.irList;
                        for( int i = 0; i < list.Count; i++ )
                        {
                            m_IRDataList.AddRange(list[i].IRDataList);
                        }
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error IR表达式错误!!");
                    }
                    break;
            }
        }
        public IRData CreateOneSignIRData(ESingleOpSign opSign)
        {
            IRData data = new IRData();
            switch (opSign)
            {
                case ESingleOpSign.Neg:
                    {
                        data.opCode = EIROpCode.Neg;
                    }
                    break;
                case ESingleOpSign.Not:
                    {
                        data.opCode = EIROpCode.Not;
                    }
                    break;
            }
            return data;
        }
        public IRData CreateLeftAndRightIRData(ELeftRightOpSign opSign)
        {
            IRData data = new IRData();
            switch (opSign)
            {
                case ELeftRightOpSign.Add:
                    {
                        data.opCode = EIROpCode.Add;
                    }
                    break;
                case ELeftRightOpSign.Minus:
                    {
                        data.opCode = EIROpCode.Minus;
                    }
                    break;
                case ELeftRightOpSign.Multiply:
                    {
                        data.opCode = EIROpCode.Multiply;
                    }
                    break;
                case ELeftRightOpSign.Divide:
                    {
                        data.opCode = EIROpCode.Divide;
                    }
                    break;
                case ELeftRightOpSign.Modulo:
                    {
                        data.opCode = EIROpCode.Modulo;
                    }
                    break;
                case ELeftRightOpSign.InclusiveOr:
                    {
                        data.opCode = EIROpCode.InclusiveOr;
                    }
                    break;
                case ELeftRightOpSign.Combine:
                    {
                        data.opCode = EIROpCode.Combine;
                    }
                    break;
                case ELeftRightOpSign.XOR:
                    {
                        data.opCode = EIROpCode.XOR;
                    }
                    break;
                case ELeftRightOpSign.Shi:
                    {
                        data.opCode = EIROpCode.Shi;
                    }
                    break;
                case ELeftRightOpSign.Shr:
                    {
                        data.opCode = EIROpCode.Shr;
                    }
                    break;
                    
                case ELeftRightOpSign.Equal:
                    {
                        data.opCode = EIROpCode.Ceq;
                    }
                    break;
                case ELeftRightOpSign.Greater:
                    {
                        data.opCode = EIROpCode.Cgt;
                    }
                    break;
                case ELeftRightOpSign.GreaterOrEqual:
                    {
                        data.opCode = EIROpCode.Cge;
                    }
                    break;
                case ELeftRightOpSign.Less:
                    {
                        data.opCode = EIROpCode.Clt;
                    }
                    break;
                case ELeftRightOpSign.LessOrEqual:
                    {
                        data.opCode = EIROpCode.Cle;
                    }
                    break;
                case ELeftRightOpSign.Or:
                    {
                        data.opCode = EIROpCode.Or;
                    }
                    break;
                case ELeftRightOpSign.And:
                    {
                        data.opCode = EIROpCode.And;
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error 未支持表达式中的IR代码"  + opSign.ToString() );
                    }
                    break;
            }
            return data;
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
}
