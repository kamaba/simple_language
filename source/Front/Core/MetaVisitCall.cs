//****************************************************************************
//  File:      MetaVisitCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  create visit variable or method call!
//****************************************************************************


using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCall
    {
        public bool isRecieveReturnValue => m_IsRecieveReturnValue;
        public MetaVariable loadMetaVariable => m_LoadMetaVariable;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public MetaClass staticCallerMetaClass => m_StaticCallerMetaClass;
        public List<MetaType> staticMetaClassInputTemplateList => m_StaticMetaClassInputTemplateList;
        public MetaFunction function => m_VMCallMetaFunction;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public List<MetaExpressNode> metaInputParamList => m_MetaInputParamList;
        public List<MetaType> metaFunctionInputTemplateList => m_MetaFunctionInputTemplateList;


        protected MetaVariable m_LoadMetaVariable = null;
        protected MetaVariable m_StoreMetaVariable = null;
        protected MetaClass m_StaticCallerMetaClass = null;
        protected bool m_IsRecieveReturnValue = true;
        protected List<MetaType> m_StaticMetaClassInputTemplateList = new List<MetaType>();
        //模板或者是调用时的函数
        protected MetaFunction m_VMCallMetaFunction = null;
        //真实的成员函数
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected List<MetaExpressNode> m_MetaInputParamList = new List<MetaExpressNode>();
        protected List<MetaType> m_MetaFunctionInputTemplateList = new List<MetaType>();
        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        
        public MetaMethodCall( MetaClass ownerClass, MetaBlockStatements ownerMBS, MetaClass staticMc,
            List<MetaType> staticMmitList,  MetaFunction _fun, List<MetaType> mpipList, MetaInputParamCollection _paramCollection, MetaVariable loadMv, MetaVariable storeMv )
        {
            m_OwnerMetaClass = ownerClass;
            m_OwnerMetaBlockStatements = ownerMBS;
            m_StaticCallerMetaClass = staticMc;
            if( staticMmitList != null )
            {
                this.m_StaticMetaClassInputTemplateList = staticMmitList;
            }
            m_VMCallMetaFunction = _fun;
            MetaMemberFunction mmf = _fun as MetaMemberFunction;
            //m_MetaInputParamList = _param;
            if( mpipList != null )
            {
                this.m_MetaFunctionInputTemplateList = mpipList;
            }

            List<MetaDefineParam> mpList = new();
            if( m_VMCallMetaFunction?.metaMemberParamCollection != null )
            {
                mpList = m_VMCallMetaFunction.metaMemberParamCollection.metaDefineParamList;
            }
            int defineCount = m_VMCallMetaFunction.metaMemberParamCollection.maxParamCount;
            int inputCount = _paramCollection != null ?_paramCollection.metaInputParamList.Count : 0;

            if( _fun.IsExtentParams() )
            {
                for (int i = 0; i < defineCount - 1 ; i++)
                {
                    MetaInputParam dmip = _paramCollection.metaInputParamList[i];
                    m_MetaInputParamList.Add(dmip.express);
                }

                var mgobj = new MetaType(CoreMetaClassManager.objectMetaClass);
                List<MetaClass> ilist = new List<MetaClass>();
                ilist.Add(CoreMetaClassManager.objectMetaClass);
                var newarray = CoreMetaClassManager.arrayMetaClass.AddInstanceMetaClass(ilist, true);
                var mt = new MetaType(newarray);

                MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode( mt, m_OwnerMetaClass, m_OwnerMetaBlockStatements);

                for( int i = defineCount - 1; i < inputCount; i++ )
                {
                    var express = _paramCollection.metaInputParamList[i].express;
                    MetaBraceAssignStatements mbas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, null, express);
                    mnoe.metaContent.assignStatementsList.Add(mbas);

                }
                mnoe.Parse(new AllowUseSettings());
                mnoe.CalcReturnType();

                m_MetaInputParamList.Add(mnoe);
            }
            else
            {
                for (int i = 0; i < defineCount; i++)
                {
                    if (i < inputCount)
                    {
                        MetaInputParam mip = _paramCollection.metaInputParamList[i];
                        m_MetaInputParamList.Add(mip.express);
                    }
                    else
                    {
                        MetaDefineParam mdp = mpList[i];
                        if (mdp != null)
                        {
                            m_MetaInputParamList.Add(mdp.expressNode);
                        }
                    }
                }
            }
                
            m_LoadMetaVariable = loadMv;
            m_StoreMetaVariable = storeMv;

            if( m_VMCallMetaFunction?.returnMetaVariable?.defineMetaType?.metaClass?.eType == EType.Void )
            {
                m_IsRecieveReturnValue = true;
            }
            else
            {
                m_IsRecieveReturnValue = m_StoreMetaVariable != null;
            }
        }
        public void SetStoreMetaVariable( MetaVariable mv )
        {
            this.m_StoreMetaVariable = mv;
        }
        public MetaType GeMetaDefineType()
        {
            return m_VMCallMetaFunction.GetFinalMetaType();
        }
        public void AddMetaInputParamList(MetaExpressNode inputp )
        {
            m_MetaInputParamList.Add(inputp);
        }
        public bool ValidateInputParamAndDefineParam()
        {
            return true;
        }
        public MetaFunction GetTemplateMemberFunction()
        {
            if( m_VMCallMetaFunction is MetaGenTemplateFunction mgtf )
            {
                return mgtf.sourceTemplateFunctionMetaMemberFunction;
            }
            if( m_VMCallMetaFunction.ownerMetaClass is MetaGenTemplateClass mgtc )
            {
                return (m_VMCallMetaFunction as MetaMemberFunction).sourceMetaMemberFunction;
            }
            return m_VMCallMetaFunction;
        }
        public string ToCommonString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_VMCallMetaFunction != null)
            {
                sb.Append(m_VMCallMetaFunction.name + "(");
                int inputCount = m_MetaInputParamList.Count;
                for (int i = 0; i < inputCount; i++)
                {
                    sb.Append(m_MetaInputParamList[i].ToFormatString());
                    if (i < inputCount - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(")");
            }
            return sb.ToString();

        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( this.loadMetaVariable != null )
            {
                sb.Append("[");
                sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allClassName);
                sb.Append("]");

                sb.Append(this.loadMetaVariable.name);
                sb.Append(".");
            }
            else
            {
                //sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allClassName);
                //sb.Append(".");
            }
            if (m_VMCallMetaFunction != null)
            {
                sb.Append(m_VMCallMetaFunction.name + "(");
                int inputCount = m_MetaInputParamList.Count;
                for (int i = 0; i < inputCount; i++)
                {
                    sb.Append(m_MetaInputParamList[i].ToFormatString());
                    if (i < inputCount - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(")");
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            return ToFormatString();
        }
    }

    public class MetaVisitNode
    {
        public enum EVisitType
        {
            ConstValue,
            Variable,
            VisitVariable,
            IteratorVariable,
            MethodCall,
            New,
            NewConst,
            Enum,
            MetaClass,
            Express,
            TemplateName,
            GetTypeValue,
            SystemCall,
        }
        public MetaConstExpressNode constValueExpress { get; private set; } = null;
        public MetaExpressNode express { get; set; } = null;
        public EVisitType visitType { get; private set; }
        public MetaVariable variable { get; private set; } = null;
        public MetaVisitVariable visitVariable { get; private set; } = null;
        public MetaMethodCall methodCall { get; private set; } = null;
        //public MetaClass callerMetaClass => m_CallerMetaClass;
        public MetaType callMetaType => m_CallMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        //public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;

        //private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        protected MetaType m_ReturnMetaType = null;
        protected MetaClass m_OwnerMetaClass = null;
        protected MetaTemplate m_MetaTemplate = null;
        protected MetaType m_CallMetaType = null; //该变量，一般是为 T t = new() 这种情况准备的

        public static MetaVisitNode CreateByVisitMetaClass(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.MetaClass;
            if (mt?.metaClass is MetaEnum me)
            {
                me.CreateValues();
            }
            vn.m_ReturnMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByVisitMetaData(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.MetaClass;
            if (mt?.metaClass is MetaEnum me)
            {
                me.CreateValues();
            }
            vn.m_ReturnMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByVisitMetaEnum(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.MetaClass;
            if (mt?.metaClass is MetaEnum me)
            {
                me.CreateValues();
            }
            vn.m_ReturnMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByNewTemplate( MetaClass ownermc, MetaBlockStatements mbs, MetaType mt, MetaFunction mf, MetaVariable mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            vn.methodCall = new MetaMethodCall(ownermc, mbs, mt.metaClass, null, mf, null, null, null, mv);
            return vn;
        }
        public static MetaVisitNode CreateByNewConst(MetaClass ownermc, MetaBlockStatements mbs,
            MetaType mt, MetaConstExpressNode mce, MetaMemberFunction mmf, MetaInputParamCollection mipc, MetaVariable mv = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.NewConst;
            vn.constValueExpress = mce;
            vn.variable = mv;
            if (mt.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(mt);
            }
            vn.methodCall = new MetaMethodCall(ownermc, mbs, mt.metaClass, null, mmf, null, mipc, null, mv);

            return vn;
        }
        public static MetaVisitNode CreateByNewClass(MetaType mt, MetaVariable mv = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            if (mt.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(mt);
            }

            return vn;
        }
        public static MetaVisitNode CreateByNewArrayClass(MetaType mt, List<MetaExpressNode> list, MetaVariable mv = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            List<int> arrayLengthList = new List<int>();
            for( int i = 0; i < list.Count; i++ )
            {
                if (list[i] is MetaConstExpressNode mcen )
                {
                    arrayLengthList.Add( (int)mcen.value );
                }
                else if (list[i] is MetaArrayExpressNode maen )
                {
                    if( maen.metaCallArray.Count == 1 )
                    {
                        if( maen.metaCallArray[0] is MetaConstExpressNode mcen2 )
                        {
                            arrayLengthList.Add((int)mcen2.value);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else
                    {
                        arrayLengthList.Add(-1);
                    }
                }
                else
                {
                    arrayLengthList.Add(-1);
                }
            }

            MetaType newRMT = new MetaType(mt);
            newRMT = TypeManager.instance.AddArrayTemplate(newRMT, arrayLengthList);
            vn.m_CallMetaType = newRMT;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            if (newRMT.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(newRMT);
            }

            return vn;
        }
        public static MetaVisitNode CraeteByNewData(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;

            return vn;
        }
        public static MetaVisitNode CraeteByEnum(MetaType mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mv;
            vn.visitType = EVisitType.Enum;

            return vn;
        }
        public static MetaVisitNode CreateByConstExpress(MetaConstExpressNode constExpress, MetaVariable _variable)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.constValueExpress = constExpress;
            vn.variable = _variable;
            vn.visitType = EVisitType.ConstValue;

            return vn;
        }
        public static MetaVisitNode CreateByEpxress(MetaExpressNode _express)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.express = _express;
            vn.visitType = EVisitType.Express;

            return vn;
        }
        public static MetaVisitNode CreateByGetType( MetaClass mc, MetaType mt )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_OwnerMetaClass = mc;
            vn.visitType = EVisitType.GetTypeValue;

            return vn;
        }
        public static MetaVisitNode CreateByEnumDefaultValue( MetaType mt, MetaVariable _variable )
        {
            MetaVisitNode vn = new MetaVisitNode();
            vn.variable = _variable;
            vn.visitType = EVisitType.Variable;

            return vn;
        }
        public static MetaVisitNode CreateByMethodCall( MetaMethodCall _methodCall)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.MethodCall;
            vn.methodCall = _methodCall;

            return vn;
        }
        public static MetaVisitNode CreateBySystemCall(MetaMethodCall _methodCall)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.SystemCall;
            vn.methodCall = _methodCall;

            return vn;
        }
        public static MetaVisitNode CreateByVisitVariable(MetaVisitVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.VisitVariable;
            vn.visitVariable = _variale;

            return vn;
        }
        public static MetaVisitNode CreateByVariable(MetaVariable _variale, MetaType callerMt = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = callerMt;

            return vn;
        }
        public static MetaVisitNode CreateByThis(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public static MetaVisitNode CreateByBase(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public static MetaVisitNode CreateByTemplate(MetaTemplate _metatemplate)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.TemplateName;
            vn.m_MetaTemplate = _metatemplate;

            return vn;
        }
        public void SetMethodCall( MetaMethodCall _methodCall)
        {
            this.methodCall = _methodCall;
        }
        public MetaVariable GetOrgTemplateMetaVariable()
        {
            if (variable == null) { return null; }

            var t = variable;
            while(t != null )
            {
                if( t.sourceMetaVariable == null  )
                {
                    break;
                }
                t = t.sourceMetaVariable;
            }
            return t;
        }
        public MetaType GetMetaType()
        {
            if( m_ReturnMetaType != null )
            {
                return m_ReturnMetaType;
            }
            switch(visitType)
            {
                case EVisitType.MethodCall:
                case EVisitType.SystemCall:
                    {
                        if( methodCall.metaMemberFunction != null )
                        {
                            return methodCall.metaMemberFunction.returnMetaVariable.defineMetaType;
                        }
                        if( methodCall.function.returnMetaVariable.isDefineMetaType )
                        {
                            return methodCall.function.returnMetaVariable.defineMetaType;
                        }
                        return methodCall.function.returnMetaVariable.realMetaType;
                    }
                    case EVisitType.VisitVariable:
                    {
                        if( this.visitVariable.isDefineMetaType )
                        {
                            return visitVariable.defineMetaType;
                        }
                        return this.visitVariable.realMetaType;
                    }
                    case EVisitType.Variable:
                    {
                        if( this.variable.isDefineMetaType )
                        {
                            return this.variable.defineMetaType;
                        }
                        return this.variable.realMetaType;
                    }
                case EVisitType.New:
                    {
                        return m_CallMetaType;
                    }
                case EVisitType.TemplateName:
                    {
                        return new MetaType( m_MetaTemplate );
                    }
                case EVisitType.Express:
                    {
                        return this.express.metaType;
                    }
                case EVisitType.Enum:
                    {
                        return this.m_CallMetaType;
                    }
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error ---------" + visitType.ToString() );
                    }
                    break;
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public MetaClass GetMetaClass()
        {
            var mt = GetMetaType();
            if( mt == null )
            {
                Log.AddInStructMeta(EError.None, "Error");
                return null;
            }
            return mt.metaClass;
        }
        public MetaVariable GetRetMetaVariable()
        {
            switch( visitType )
            {
                case EVisitType.Variable:
                    {
                        return variable;
                    }
                case EVisitType.MethodCall:
                case EVisitType.SystemCall:
                    {
                        return methodCall.function.returnMetaVariable;
                    }
                case EVisitType.VisitVariable:
                    {
                        return visitVariable;
                    }
                case EVisitType.New:
                    {
                        return variable;
                    }
                case EVisitType.Enum:
                    {
                        return variable;
                    }
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error MetaVisiCall IsNull!");
                    }
                    break;
            }
            return null;
        }

        public int CalcParseLevel(int level)
        {
            //if (m_CurrentMetaBase == null) return level;
            //var mv = m_CurrentMetaBase as MetaMemberVariable;
            //if (mv != null)
            //{
            //    return mv.CalcParseLevelBeCall(level);
            //}
            return level;
        }
        public void CalcReturnType()
        {
            GetMetaType();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            switch (visitType)
            {
                case EVisitType.MethodCall:
                case EVisitType.SystemCall:
                    {
                        sb.Append(this.methodCall.ToFormatString());
                    }
                    break;
                case EVisitType.MetaClass:
                    {
                        sb.Append(this.m_ReturnMetaType.ToString() );
                    }
                    break;
                case EVisitType.VisitVariable:
                    {
                        sb.Append(this.visitVariable.ToFormatString());
                    }
                    break;
                case EVisitType.ConstValue:
                    {
                        sb.Append(this.constValueExpress.value.ToString());
                    }
                    break;
                case EVisitType.Variable:
                    {
                        sb.Append(this.variable.ToString());
                    }
                    break;
                case EVisitType.New:
                    {
                        sb.Append(this.m_CallMetaType.ToString());
                    }
                    break;
                default:
                    {
                        sb.Append("Error MetaVisitCall Default Parse!");
                    }
                    break;
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            switch (visitType)
            {
                case EVisitType.MethodCall:
                case EVisitType.SystemCall:
                    {
                        sb.Append(this.methodCall.ToFormatString());
                    }
                    break;
                case EVisitType.MetaClass:
                    {
                        sb.Append(this.m_ReturnMetaType.metaClass.metaNode.ToString());
                    }
                    break;
                case EVisitType.VisitVariable:
                    {
                        sb.Append(this.visitVariable.ToFormatString());
                    }
                    break;
                case EVisitType.ConstValue:
                    {
                        sb.Append(this.constValueExpress.value.ToString());
                    }
                    break;
                case EVisitType.Variable:
                    {
                        sb.Append(this.variable.ToString());
                    }
                    break;
                case EVisitType.New:
                    {
                        sb.Append(this.m_CallMetaType.ToString());
                    }
                    break;
                default:
                    {
                        sb.Append("Error MetaVisitCall Default Parse!");
                    }
                    break;
            }

            return sb.ToString();
        }
    }
}
