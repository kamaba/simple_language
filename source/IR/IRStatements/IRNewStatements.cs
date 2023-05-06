//****************************************************************************
//  File:      IRNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaNewStatements
    {
        IRExpress m_IRExpress = null;
        public override void ParseIRStatements()
        {
            MetaNewObjectExpressNode mnoen = null;
            if ( m_ExpressNode != null)
            {
                if( m_ExpressNode is MetaNewObjectExpressNode )
                {
                    mnoen = m_ExpressNode as MetaNewObjectExpressNode;
                    IRNew irNew = new IRNew( irMethod, mnoen.GetReturnMetaDefineType());
                    m_IRDataList.AddRange(irNew.IRDataList);                    
                }
                else
                {
                    m_IRExpress = new IRExpress(irMethod, m_ExpressNode);
                    m_IRDataList.AddRange(m_IRExpress.IRDataList);
                }
            }
            
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.StoreLocal;
            insNode.index = irMethod.GetLocalVariableIndex(m_MetaVariable);
            m_IRDataList.Add(insNode);

            if( mnoen!= null )
            {
                var metaClass = mnoen.GetReturnMetaClass();
                var mmvs = metaClass.metaClassData.metaMemberVariables;
                for ( int i = 0; i < mmvs.Count; i++ )
                {
                    IRExpress irExp = new IRExpress(irMethod, mmvs[i].express);
                    m_IRDataList.AddRange(irExp.IRDataList);

                    IRData mmvsNode = new IRData();
                    mmvsNode.opCode = EIROpCode.LoadLocal;
                    mmvsNode.index = irMethod.GetLocalVariableIndex(m_MetaVariable);
                    m_IRDataList.Add(mmvsNode);

                    IRData storeNode = new IRData();
                    storeNode.opCode = EIROpCode.StoreNotStaticField;
                    storeNode.index = metaClass.metaClassData.GetMemberVariableIndex(mmvs[i]);
                    m_IRDataList.Add(storeNode);
                }

                IRData localThisNode = new IRData();
                localThisNode.opCode = EIROpCode.LoadLocal;
                localThisNode.index = irMethod.GetLocalVariableIndex(m_MetaVariable);
                m_IRDataList.Add(localThisNode);

                var irCallFun = new IRCallFunction(irMethod, mnoen.constructFunctionCall);
                m_IRDataList.AddRange(irCallFun.IRDataList);

                for (int i = 0; i < mnoen.metaBraceOrBracketStatementsContent.assignStatementsList.Count; i++)
                {
                    var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[i];

                    IRExpress irExp = new IRExpress(irMethod, mmvs[i].express);
                    m_IRDataList.AddRange(irExp.IRDataList);

                    IRData mmvsNode = new IRData();
                    mmvsNode.opCode = EIROpCode.LoadLocal;
                    mmvsNode.index = irMethod.GetLocalVariableIndex(m_MetaVariable);
                    m_IRDataList.Add(insNode);

                    IRData storeNode = new IRData();
                    storeNode.opCode = EIROpCode.StoreNotStaticField;
                    storeNode.index = metaClass.metaClassData.GetMemberVariableIndex(asl.metaMemberVariable);
                    m_IRDataList.Add(insNode);
                }
            }
        }
    }
}
