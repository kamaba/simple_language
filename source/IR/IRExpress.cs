//****************************************************************************
//  File:      IRExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  express convert ir code!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Parse;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SimpleLanguage.IR
{
    public class IRExpress : IRBase
    {
        private IRManager m_IRManager = null;
        public IRExpress( IRMethod irMethod, MetaExpressNode node ) : base( irMethod )
        {
            //m_Node = node;
            CreateIRDataOne(node);
        }
        public IRExpress( IRManager _irManager, MetaExpressNode node ):base()
        {
            m_IRManager = _irManager;
            //m_Node = node;
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
                        //irdata.SetDebugInfoByToken( mcn.GetToken() );
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
                        if( moen.leftConvert != null )
                        {
                            IRConvert ircovn = new IRConvert(m_IRMethod, moen.leftConvert.oriType, moen.leftConvert.targetType);
                            AddIRData(ircovn.data);
                        }
                        CreateIRDataOne(rightNode);
                        if (moen.rightConvert != null)
                        {
                            IRConvert ircovn = new IRConvert(m_IRMethod, moen.rightConvert.oriType, moen.rightConvert.targetType);
                            AddIRData(ircovn.data);
                        }
                        var signData = CreateLeftAndRightIRData(moen.opSign);
                        //signData.SetDebugInfoByToken( moen.GetToken() );
                        AddIRData(signData);
                    }
                    break;
                case MetaCallLinkExpressNode mcn:
                    {
                        IRMetaCallLink irmc = new IRMetaCallLink();
                        if ( m_IRManager != null )
                        {
                            irmc.ParseToIRDataListByIRManager(m_IRManager, mcn.metaCallLink.callNodeList);
                        }
                        else
                        {
                            irmc.ParseToIRDataList(m_IRMethod, mcn.metaCallLink.callNodeList);
                        }
                        for( int i = 0; i < irmc.irList.Count; i++ )
                        {
                            m_IRDataList.AddRange(irmc.irList[i].IRDataList);
                        }
                    }
                    break;
                case MetaAsIsExpressNode maien:
                    {
                        //maien.GetReturnMetaClass();
                        if( maien.isAs )
                        {
                            var owirmc = IRManager.instance.GetIRMetaClassById(maien.currentVariable.GetOwnerClassTemplateClass().GetHashCode());
                            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(maien.currentVariable.metaDefineType, owirmc);
                            IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, maien.currentVariable );
                            AddIRRangeData(irload.IRDataList);

                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.opValue = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList( maien.convertTargetMetaType, owirmc);

                            m_IRDataList.Add(irdata);
                        }
                        else
                        {
                            var owirmc = IRManager.instance.GetIRMetaClassById(maien.currentVariable.GetOwnerClassTemplateClass().GetHashCode());
                            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(maien.currentVariable.metaDefineType, owirmc);
                            IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, maien.currentVariable);
                            AddIRRangeData(irload.IRDataList);

                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.opValue = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(maien.convertTargetMetaType, owirmc);
                            m_IRDataList.Add(irdata);

                            var owirmc2 = IRManager.instance.GetIRMetaClassById(maien.convertTargetMetaVariable.GetOwnerClassTemplateClass().GetHashCode());
                            var irmt2 = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(maien.convertTargetMetaVariable.metaDefineType, owirmc);
                            IRStoreVariable irstore = IRStoreVariable.CreateIRStoreVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irstore.IRDataList);

                            IRLoadVariable irload3 = IRLoadVariable.CreateLoadVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irload3.IRDataList);

                            IRData irdata4 = new IRData();
                            irdata4.opCode = IRManager.GetConstIROpCode( EType.Null );
                            irdata4.opValue = "null";
                            AddIRData(irdata4);

                            IRData irdata5 = new IRData();
                            irdata5.opCode = EIROpCode.Cne;
                            AddIRData(irdata5);
                        }
                    }
                    break;
                default:
                    {
                        Debug.Write("Error IR表达式错误!!");
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
                case ELeftRightOpSign.NotEqual:
                    {
                        data.opCode = EIROpCode.Cne;
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
                        Debug.Write("Error 未支持表达式中的IR代码"  + opSign.ToString() );
                    }
                    break;
            }
            return data;
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#LoadConst#" );
            sb.Append(base.ToIRString());
            return sb.ToString();
        }
    }

    public class IRNewExpress : IRBase
    {
        public IRNewExpress(IRMethod irMethod, MetaNewObjectExpressNode mnoen ) : base(irMethod)
        {
            IRMetaClass owirmc = null;
            IRMetaType newObjectIRMT = null;
            IRMetaClass irmc = null;

            if( mnoen.newType == MetaNewObjectExpressNode.ENewType.ArrayClass )
            {
                IRNewArray irnewArray = new IRNewArray();
                EArrayType arrayType = EArrayType.Int32;
                if (mnoen.arrayType.metaClass == CoreMetaClassManager.int32MetaClass)
                {
                    arrayType = EArrayType.Int32;
                }
                else if (mnoen.arrayType.metaClass == CoreMetaClassManager.stringMetaClass )
                {
                    arrayType = EArrayType.String;
                }
                else
                {
                    arrayType = EArrayType.Pointer;
                }
                irnewArray.eArrayType = arrayType;
                irnewArray.length = mnoen.arrayLength;

                IRNew irNew = new IRNew(irMethod, irnewArray);
                AddIRRangeData(irNew.IRDataList);

                if (mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList?.Count > 0)
                {
                    for (int y = 0; y < mnoen.metaBraceOrBracketStatementsContent.assignStatementsList.Count; y++)
                    {
                        var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[y];

                        IRDup irdup = new IRDup(irMethod);
                        AddIRRangeData(irdup.IRDataList);

                        IRExpress irexp = new IRExpress(irMethod, asl.expressNode);
                        AddIRRangeData(irexp.IRDataList);

                        IRData irdatastore = new IRData();
                        irdatastore.index = y;
                        irdatastore.opValue = true;
                        irdatastore.opCode = EIROpCode.StoreArrayIndex;
                        m_IRDataList.Add(irdatastore);
                    }
                }
            }
            else
            {
                if (mnoen.metaDefineType.eType == EMetaTypeType.MetaGenClass)
                {
                    if (mnoen.ownerMetaClass is MetaGenTemplateClass mgtc)
                    {
                        owirmc = IRManager.instance.GetIRMetaClassById(mgtc.metaTemplateClass.GetHashCode());
                    }
                    newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.metaDefineType, owirmc);
                    irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaDefineType.GetTemplateMetaClass().GetHashCode());

                }
                if (mnoen.metaDefineType.eType == EMetaTypeType.MetaClass)
                {
                    owirmc = IRManager.instance.GetIRMetaClassById(mnoen.ownerMetaClass.GetHashCode());
                    IRMetaClass newObjIRMC = IRManager.instance.GetIRMetaClassById(mnoen.metaDefineType.GetTemplateMetaClass().GetHashCode());
                    irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaDefineType.GetTemplateMetaClass().GetHashCode());
                    newObjectIRMT = new IRMetaType(newObjIRMC);
                    IRNew irNew = new IRNew(irMethod, newObjIRMC);
                    AddIRRangeData(irNew.IRDataList);
                }
                else
                {
                    int a = 10;
                }

                if (irmc.needInitMemberVariable)
                {
                    if (irmc.localIRMetaVariableList.Count > 0)
                    {
                        bool isUseAssign = false;
                        for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                        {
                            var lirmv = irmc.localIRMetaVariableList[x];
                            if (mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList?.Count > 0)
                            {
                                //var irmc = _irMethod.irManager.GetIRMetaClassByName(cnode.variable.metaDefineType.metaClass.allClassName);
                                IRLoadVariable irlv = null;// IRLoadVariable.CreateLoadVariable(null, irmc, irMethod, lirmv.GetOrgTemplateMetaVariable());
                                AddIRRangeData(irlv.IRDataList);
                                for (int y = 0; y < mnoen.metaBraceOrBracketStatementsContent.assignStatementsList.Count; y++)
                                {
                                    var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[y];
                                    if (asl.metaMemberVariable.name == lirmv.name)
                                    {
                                        IRExpress irexp = new IRExpress(irMethod, asl.expressNode);
                                        AddIRRangeData(irexp.IRDataList);

                                        IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(null, irMethod, lirmv.index, IRMetaVariableFrom.Member);
                                        AddIRRangeData(irStoreNodeVar3.IRDataList);
                                        isUseAssign = true;
                                        break;
                                    }
                                }

                                if (isUseAssign == false)
                                {
                                    IRExpress irexp = new IRExpress(irMethod, lirmv.express);
                                    AddIRRangeData(irexp.IRDataList);

                                    IRStoreVariable irStoreVar2 = new IRStoreVariable(null, irMethod, lirmv.index, IRMetaVariableFrom.Member);

                                    AddIRRangeData(irStoreVar2.IRDataList);

                                }
                            }
                        }
                    }
                }
                if (mnoen.metaMemberFunction != null)
                {
                    IRDup irdup = new IRDup(irMethod);
                    m_IRDataList.AddRange(irdup.IRDataList);

                    var paramCount = mnoen.metaInputParamList.Count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        IRExpress irexpress = new IRExpress(m_IRMethod, mnoen.metaInputParamList[j]);
                        AddIRRangeData(irexpress.IRDataList);
                    }

                    int callMethodIndex = -1;
                    string fname = "";

                    MetaClass mc2 = null;
                    if (mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                        mc2 = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                    else
                        mc2 = mnoen.metaMemberFunction.ownerMetaClass;

                    fname = mnoen.metaMemberFunction.virtualFunctionName;
                    irmc = IRManager.instance.GetIRMetaClassById(mc2.GetHashCode());

                    var runtimeMethod = irmc.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1)
                    {
                        Log.AddGenIR(EError.None, "没有找到构建对象函数!");
                    }
                    List<IRMetaType> functionMtList = new List<IRMetaType>();
                    var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.opValue = irmethodcall;
                    //datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
            }
        }
    }
}
