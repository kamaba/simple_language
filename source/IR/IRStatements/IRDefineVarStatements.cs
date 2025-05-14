//****************************************************************************
//  File:     IRDefineVarStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using SimpleLanguage.IR.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class MetaIRDefineVarStatements : MetaIRStatements
    {
        IRExpress m_IRExpress = null;
        public MetaIRDefineVarStatements( IRMethod _method ) 
        {
            this.irMethod = _method;
        }
        public void ParseIRStatements( MetaDefineVarStatements ms )
        {
            MetaNewObjectExpressNode mnoen = null;
            if (ms.expressNode != null)
            {
                mnoen = ms.expressNode as MetaNewObjectExpressNode;
                if (mnoen != null)
                {
                    IRMetaClass irmc = IRManager.instance.GetIRMetaClassByName(ms.expressNode.GetReturnMetaClass().allName);
                    IRNew irNew = new IRNew( irMethod, irmc );
                    m_IRStatements.Add( irNew );
                }
                else
                {
                    m_IRExpress = new IRExpress(irMethod, ms.expressNode);
                    m_IRStatements.Add(m_IRExpress);
                }
            }
            IRStoreVariable irStoreVar = new IRStoreVariable(irMethod, ms.defineVarMetaVariable.GetHashCode() );
            //if(m_FileMetaOpAssignSyntax != null )
            //{
            //    irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
            //}
            m_IRStatements.Add(irStoreVar);

            if ( mnoen!= null )
            {
                var mt = mnoen.GetReturnMetaDefineType();

                if( mt.isData )
                {
                    MetaData md = (mt.metaClass as MetaData);
                    bool isFZ = false;
                    foreach ( var v in md.metaMemberDataDict )
                    {
                        isFZ = false;
                        // Class1{ a = 1; b = 2 }  如果已经配置 {}内容，则不走默认赋值，而是走{}内容赋值
                        for (int j = 0; j < mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList.Count; j++)
                        {
                            var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[j];

                            if (asl.metaMemberData.name == v.Key )
                            {
                                //IRLoadVariable mmvsNodeVar = new IRLoadVariable(irMethod, m_MetaVariable);
                                //m_IRStatements.Add(mmvsNodeVar);

                                //IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(irMethod, asl.metaMemberData );
                                //m_IRStatements.Add(irStoreNodeVar3);
                                isFZ = true;
                                break;
                            }
                        }

                        if (isFZ == false)
                        {
                            if (v.Value.memberDataType == EMemberDataType.MemberClass
                                || v.Value.memberDataType == EMemberDataType.ConstValue )
                            {
                                IRExpress irexp = new IRExpress(irMethod, v.Value.expressNode );
                                m_IRStatements.Add(irexp);

                                //IRLoadVariable irLoadVar1 = new IRLoadVariable(irMethod, m_MetaVariable);
                                //m_IRStatements.Add(irLoadVar1);

                                IRStoreVariable irStoreVar2 = new IRStoreVariable(irMethod, v.Value.GetHashCode() );
                                m_IRStatements.Add(irStoreVar2);
                            }
                            else if( v.Value.memberDataType == EMemberDataType.MemberArray )
                            {

                            }
                            else
                            {
                                Console.WriteLine("Error 不支持其它 的数据成员格式");
                            }
                        }
                    }
                }
                else if (mt.isEnum)
                {
                }
                else
                {
                    MetaClass metaClass = mt.metaClass;
                    //var mmvs = metaClass.localMetaMemberVariables;

                    //bool isFZ = false;
                    //for (int i = 0; i < mmvs.Count; i++)
                    //{
                    //    isFZ = false;
                    //    // Class1{ a = 1; b = 2 }  如果已经配置 {}内容，则不走默认赋值，而是走{}内容赋值
                    //    for (int j = 0; j < mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList.Count; j++)
                    //    {
                    //        var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[j];

                    //        if (asl.metaMemberVariable.name == mmvs[i].name)
                    //        {
                    //            IRLoadVariable mmvsNodeVar = new IRLoadVariable(irMethod, m_MetaVariable);
                    //            m_IRStatements.Add(mmvsNodeVar);

                    //            IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(irMethod, asl.metaMemberVariable);
                    //            m_IRStatements.Add(irStoreNodeVar3);
                    //            isFZ = true;
                    //            break;
                    //        }
                    //    }

                    //    if (isFZ == false)
                    //    {
                    //        IRExpress irexp = new IRExpress(irMethod, mmvs[i].express);
                    //        m_IRStatements.Add(irexp);

                    //        IRLoadVariable irLoadVar1 = new IRLoadVariable(irMethod, m_MetaVariable);
                    //        m_IRStatements.Add(irLoadVar1);

                    //        IRStoreVariable irStoreVar2 = new IRStoreVariable(irMethod, mmvs[i]);
                    //        m_IRStatements.Add(irStoreVar2);
                    //    }
                    }
                    // Class1().Init();
                    //var irCallFun = new IRCallFunction(irMethod, mnoen.constructFunctionCall);
                    //m_IRStatements.Add(irCallFun);
                }
            }
        //public override string ToIRString()
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.Append(" #new var ");
        //    sb.Append(m_MetaVariable.ToFormatString() );
        //    if(m_ExpressNode != null )
        //    {
        //        sb.Append( " = " + m_ExpressNode.ToFormatString());
        //    }
        //    sb.AppendLine(" #");

        //    sb.AppendLine("{");
        //    for (int i = 0; i < m_IRStatements.Count; i++)
        //    {
        //        sb.AppendLine(m_IRStatements[i].ToIRString());
        //    }
        //    sb.AppendLine("}");
        //    return sb.ToString();
        //}
    }
}
