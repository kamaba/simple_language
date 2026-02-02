//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  handle for loop statements or while/dowhile statements create instruction 
//****************************************************************************



using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRForStatements : IRStatements
    {
        public IRForStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRNop startIRData = null;       //for开始执行起点
        public IRNop forStartIRData = null;    //for 循环返回起点
        public IRNop endIRData = null;           //for语句点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //for结束点返回起点语句

        private IRExpressBase m_IRConditionExpress = null;
        public void ParseIRStatements(MetaForStatements ms)
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);

            if (ms.isForIn)
            {
                /*
                 * for v in variable1
                 * 以下是对上边的IR解释
                 * v = variable1.iterator()
                 * startLabel                 * 
                 * if( v.hasMove() )   //conditionExpress
                 *      thenstatement
                 *      goto startLabel
                 * else
                 *      goto endLabel
                 * endLabel
                 * nextStatements
                 */
                var irownermc = IRManager.instance.GetIRMetaClassById(ms.forIterateVariable.GetOwnerClassTemplateClass().GetHashCode());
                var itv_irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(ms.forIterateVariable.defineMetaType, irownermc);

                //var content_irownermc = IRManager.instance.GetIRMetaClassById(ms.forInContent.GetOwnerClassTemplateClass().GetHashCode());
                MetaType dmt = ms.forInContent.isDefineMetaType ? ms.forInContent.defineMetaType : ms.forInContent.realMetaType;
                var content_irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList( dmt, irownermc);

                //var iterator_irownermc = IRManager.instance.GetIRMetaClassById(ms.forInContentIterator.GetOwnerClassTemplateClass().GetHashCode());
                MetaType it_dmt = ms.forInContentIterator.isDefineMetaType ? ms.forInContentIterator.defineMetaType : ms.forInContentIterator.realMetaType;
                var iterator_irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(it_dmt, irownermc );

                if (ms.conditionExpress != null)
                {
                    if (ms.conditionExpress is MetaNewObjectExpressNode mnoen)
                    {
                        var irNewConditionExpress = new IRNewExpress(irMethod, mnoen);
                        m_IRStatements.Add(irNewConditionExpress);


                        IRStoreVariable irStoreVar = IRStoreVariable.CreateIRStoreVariable(content_irmt, null, irMethod, ms.forInContent);
                        m_IRStatements.Add(irStoreVar);
                    }
                }

                // 1. 创建迭代器对象，并赋值给循环变量

                IRLoadVariable loadContentVar = IRLoadVariable.CreateLoadVariable(content_irmt, null, irMethod, ms.forInContent );
                m_IRStatements.Add(loadContentVar);

                var resetMethodInst = content_irmt.irMetaClass.GetIRNonStaticMethodIndexByName("reset", out int restIndex);
                var restCall = new IRMethodCall(content_irmt, new List<IRMetaType>(), resetMethodInst, 0);
                IRData restCallData = new IRData();
                restCallData.opCode = EIROpCode.CallDynamic;
                restCallData.opValue = restCall;
                restCallData.index = 1;
                IRBase restCallBase = new IRBase(restCallData);
                m_IRStatements.Add(restCallBase);

                m_IRStatements.Add(loadContentVar);
                var iteratorMethodInst = content_irmt.irMetaClass.GetIRNonStaticMethodIndexByName("iterator", out int iteratorIndex);
                var iteratorCall = new IRMethodCall(content_irmt, new List<IRMetaType>(), iteratorMethodInst, 0);
                IRData iteratorCallData = new IRData();
                iteratorCallData.opCode = EIROpCode.CallDynamic;
                iteratorCallData.opValue = iteratorCall;
                iteratorCallData.index = 1;
                IRBase iteratorCallBase = new IRBase(iteratorCallData);
                m_IRStatements.Add(iteratorCallBase);

                IRStoreVariable storeIterator = IRStoreVariable.CreateIRStoreVariable(iterator_irmt, null, irMethod, ms.forInContentIterator );
                m_IRStatements.Add(storeIterator);

                // 2. 循环起点
                m_IRStatements.Add(startIRData);

                // 3. 加载迭代器对象，调用 moveNext()
                IRLoadVariable loadIterator = IRLoadVariable.CreateLoadVariable(iterator_irmt, null, irMethod, ms.forInContentIterator );
                m_IRStatements.Add(loadIterator);

                var moveNextMethodIndex = iterator_irmt.irMetaClass.GetIRNonStaticMethodIndexByName("moveNext", out int moveNextIndex);
                var moveNextCall = new IRMethodCall(iterator_irmt, new List<IRMetaType>(), moveNextMethodIndex, 0);
                IRData moveNextCallData = new IRData();
                moveNextCallData.opCode = EIROpCode.CallDynamic;
                moveNextCallData.opValue = moveNextCall;
                moveNextCallData.index = 1;
                IRBase moveNextCallBase = new IRBase(moveNextCallData);
                m_IRStatements.Add(moveNextCallBase);

                // 4. 判断 moveNext() 返回值，false 跳出循环
                ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                m_IRStatements.Add(ifIRData);

                //// 5. 加载迭代器对象，调用 current()，并赋值给变量
                IRLoadVariable loadIteratorForCurrent = IRLoadVariable.CreateLoadVariable(iterator_irmt, null, irMethod, ms.forInContentIterator );
                m_IRStatements.Add(loadIteratorForCurrent);

                var currentMethodIndex = iterator_irmt.irMetaClass.GetIRNonStaticMethodIndexByName("current", out int currentIndex);
                var currentCall = new IRMethodCall(iterator_irmt, new List<IRMetaType>(), currentMethodIndex, 0);
                IRData currentCallData = new IRData();
                currentCallData.opCode = EIROpCode.CallDynamic;
                currentCallData.opValue = currentCall;
                currentCallData.index = 1;
                IRBase currentCallBase = new IRBase(currentCallData);
                m_IRStatements.Add(currentCallBase);

                // 6. 存储当前值到变量（如有需要）
                IRStoreVariable storeCurrentValue = IRStoreVariable.CreateIRStoreVariable(itv_irmt, null, irMethod, ms.forIterateVariable);
                m_IRStatements.Add(storeCurrentValue);

                // 7. 执行循环体
                IRBlockStatements loopBody = new IRBlockStatements(irMethod);
                loopBody.ParseAllIRStatements(ms.thenMetaStatements);
                m_IRStatements.AddRange(loopBody.irStatements);

                // 8. 跳回循环起点
                brIRData = new IRBranch(irMethod, EIROpCode.Br, startIRData.data);
                m_IRStatements.Add(brIRData);

                // 9. 循环结束标记
                m_IRStatements.Add(endIRData);

                // 10. 释放迭代器资源
                /*
                IRLoadVariable loadIteratorForRelease = IRLoadVariable.CreateLoadVariable(it_irmt, it_irmc, irMethod, ms.forIterateVariable);
                m_IRStatements.Add(loadIteratorForRelease);

                var releaseMethodIndex = it_irmc.GetIRNonStaticMethodIndexByName("release", out int releaseIndex);
                
                if (releaseMethodIndex != null && releaseIndex >= 0)
                {
                    var releaseCall = new IRMethodCall(it_irmt, new List<IRMetaType>(), releaseMethodIndex, 0);
                    IRData releaseCallData = new IRData();
                    releaseCallData.opCode = EIROpCode.CallDynamic;
                    releaseCallData.opValue = releaseCall;
                    releaseCallData.index = 1;
                    IRBase releaseCallBase = new IRBase(releaseCallData);
                    m_IRStatements.Add(releaseCallBase);
                }
                */
            }
            else
            {
                /*
                 * for( i = 0, i < express; i++ ) 
                 * 以下是对上边的IR解释
                 * define i = 0  如果i在前边声明 则i = 0
                 * startLabel
                 * i = i + 1
                 * if( i < express )   //conditionExpress
                 *      thenstatement
                 *      goto startLabel
                 * else
                 *      goto endLabel
                 * endLabel
                 * nextStatements
                 */

                if (ms.defineVarStatements != null)
                {
                    IRDefineVarStatements irdvs = new IRDefineVarStatements( this.irMethod );
                    irdvs.ParseIRStatements(ms.defineVarStatements);                       
                    m_IRStatements.AddRange(irdvs.irStatements);
                }
                else if (ms.assignStatements != null)
                {
                    IRAssignStatements iras = new IRAssignStatements(this.irMethod);
                    iras.ParseIRStatements(ms.assignStatements);
                    m_IRStatements.AddRange(iras.irStatements);
                }
                // 2. 循环起点
                m_IRStatements.Add(startIRData);

                if ( ms.conditionExpress != null)
                {
                    m_IRConditionExpress = IRExpressManager.CreateExpress(irMethod, ms.conditionExpress);
                    m_IRStatements.Add(m_IRConditionExpress);

                    // 4. 判断 moveNext() 返回值，false 跳出循环
                    ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                    m_IRStatements.Add(ifIRData);
                }
                IRBlockStatements loopBody = new IRBlockStatements(irMethod);
                loopBody.ParseAllIRStatements(ms.thenMetaStatements);
                m_IRStatements.AddRange(loopBody.irStatements);


                if (ms.stepStatements != null)
                {
                    IRAssignStatements irstep = new IRAssignStatements(this.irMethod);
                    irstep.ParseIRStatements(ms.stepStatements);
                    m_IRStatements.AddRange(irstep.irStatements);
                }

                // 8. 跳回循环起点
                brIRData = new IRBranch(irMethod, EIROpCode.Br, startIRData.data);
                m_IRStatements.Add(brIRData);

                // 9. 循环结束标记
                m_IRStatements.Add(endIRData);
            }

            if (ms.nextMetaStatements != null)
            {
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irbs.ParseAnyIRStatements(ms.nextMetaStatements);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#for ");
            /*
            if (m_IsForIn)
            {
                sb.Append(m_ForMetaVariable.name);
                sb.Append(" in ");
                sb.Append(m_ForInContent.name);
            }
            sb.AppendLine("#");
            sb.Append("{");
            sb.Append(Environment.NewLine);

            if (!m_IsForIn)
            {
                if (m_NewStatements != null)
                {
                    sb.Append(m_NewStatements.ToIRString());
                }
                if (m_AssignStatements != null)
                {
                    sb.Append(m_AssignStatements.ToIRString());
                }

                if (m_ConditionExpress != null)
                {
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append("if ");
                    sb.Append(m_ConditionExpress.ToFormatString());
                    sb.Append("{break;}");
                    sb.Append(Environment.NewLine);
                }
                if (m_StepStatements != null)
                {
                    sb.Append(m_StepStatements.ToIRString());
                }
                sb.Append(m_ThenMetaStatements?.ToIRString());
            }
            else
            {
            }

            sb.Append("}");

            sb.Append(Environment.NewLine);
            */

            return sb.ToString();
        }
    }

    public class IRWhileDoWhileStatements : IRStatements
    {
        public IRWhileDoWhileStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRNop startIRData = null;       //while 开始执行起点
        public IRNop whileStartIRData = null;    //while 循环返回起点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //while结束点返回起点语句
        public IRNop endIRData = null;           //while语句点

        private IRExpress m_IRConditionExpress = null;
        public void ParseIRStatements(MetaWhileDoWhileStatements ms)
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);
            m_IRStatements.Add(startIRData);

            whileStartIRData = new IRNop(irMethod);
            m_IRStatements.Add(whileStartIRData);


            //if (m_IsWhile)
            //{
            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRStatements.Add(m_IRConditionExpress);

            //        ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
            //        m_IRStatements.Add(ifIRData);
            //    }
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
            //}
            //else
            //{
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);

            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRStatements.Add(m_IRConditionExpress);

            //        ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
            //        m_IRStatements.Add(ifIRData);
            //    }
            //}

            //brIRData = new IRBranch(irMethod, EIROpCode.Br, whileStartIRData.data);
            //m_IRStatements.Add(brIRData);
            //m_IRStatements.Add(endIRData);

            //if (m_ConditionExpress != null)
            //{
            //    ifIRData.data.SetDebugInfoByToken(m_ConditionExpress.GetToken());
            //}
            //if (m_FileMetaKeyWhileSyntax != null)
            //{
            //    brIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
            //    startIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.token);
            //    whileStartIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.beginBlock);
            //    endIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
            //}

            //if (m_NextMetaStatements != null)
            //{
            //    m_NextMetaStatements.ParseIRStatements();
            //}
        }

        //public override string ToIRString()
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.AppendLine("#" + (m_IsWhile ? "while" : "dowhile#   {") );
        //    sb.AppendLine( base.ToIRString() );
        //    sb.AppendLine("}");
        //    if (m_ConditionExpress != null)
        //    {
        //        sb.AppendLine("#condition" + m_ConditionExpress.ToFormatString() + "#");
        //        sb.AppendLine("{");
        //        sb.Append(m_IRConditionExpress.ToIRString());
        //        sb.AppendLine("}");
        //    }

        //    sb.AppendLine(m_ThenMetaStatements.ToIRString());

        //    sb.AppendLine("}");

        //    return sb.ToString();
        //}
    }
}
