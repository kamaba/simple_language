//****************************************************************************
//  File:      MetaVisitCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  create visit variable or method call!
//****************************************************************************


using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCall
    {
        public bool isRecieveReturnValue => m_IsRecieveReturnValue;
        //public MetaVariable loadMetaVariable => m_LoadMetaVariable;
        public MetaType staticCallMetaType => m_StaticCallerMetaType;
        public MetaType returnMetaType => m_ReturnMetaType;
        public MetaFunction function => m_VMCallMetaFunction;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public List<MetaExpressNodeBase> metaInputParamList => m_MetaInputParamList;
        public List<MetaType> metaFunctionInputTemplateList => m_MetaFunctionInputTemplateList;
        public Token token => m_Token;


        protected MetaVariable m_LoadMetaVariable = null;
        //protected MetaVariable m_StoreMetaVariable = null;
        protected MetaType m_StaticCallerMetaType = null;
        protected MetaType m_ReturnMetaType = null;
        protected bool m_IsRecieveReturnValue = true;
        // Debug-only: in some call-site shapes, meta member param count may resolve to 0,
        // which results in empty m_MetaInputParamList and missing args in Meta.txt.
        // Keep the parsed input param collection so we can still print args.
        private MetaInputParamCollection? m_InputParamCollectionForDebug = null;
        //妯℃澘鎴栬€呮槸璋冪敤鏃剁殑鍑芥暟
        protected MetaFunction m_VMCallMetaFunction = null;
        //鐪熷疄鐨勬垚鍛樺嚱鏁?
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected List<MetaExpressNodeBase> m_MetaInputParamList = new List<MetaExpressNodeBase>();
        protected List<MetaType> m_MetaFunctionInputTemplateList = new List<MetaType>();
        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        // Debug-only: keep raw "(...)" text from call-site to avoid empty args in Meta.txt.
        // This is independent from overload resolution / typed param list building.
        private string? m_DebugInputParTermText = null;
        private Token m_Token = null;
        
        public MetaMethodCall( MetaClass ownerClass, MetaBlockStatements ownerMBS, MetaType staticMt,  MetaFunction _fun, List<MetaType> mpipList,
            MetaInputParamCollection _paramCollection, MetaType returnMt,
            MetaVariable loadMv, MetaVariable storeMv )
        {
            m_OwnerMetaClass = ownerClass;
            m_OwnerMetaBlockStatements = ownerMBS;
            m_InputParamCollectionForDebug = _paramCollection;
            m_StaticCallerMetaType = staticMt;
            m_VMCallMetaFunction = _fun;
            m_ReturnMetaType = returnMt;
            MetaMemberFunction mmf = _fun as MetaMemberFunction;
            m_MetaMemberFunction = mmf;
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
            int defineCount = m_VMCallMetaFunction?.metaMemberParamCollection?.maxParamCount ?? 0;
            int inputCount = _paramCollection != null ?_paramCollection.metaInputParamList.Count : 0;

            if( _fun != null && _fun.IsExtentParams() )
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

                MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode( mt, m_OwnerMetaClass, m_OwnerMetaBlockStatements );

                MetaType cmt = new MetaType(CoreMetaClassManager.objectMetaClass);
                for( int i = defineCount - 1; i < inputCount; i++ )
                {
                    var express = _paramCollection.metaInputParamList[i].express;
                    MetaBraceAssignStatements mbas = new MetaBraceAssignStatements(null, m_OwnerMetaBlockStatements, m_OwnerMetaClass, cmt, express);
                    mnoe.assignStatementsList.Add(mbas);

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
            //m_StoreMetaVariable = storeMv;

            if( m_VMCallMetaFunction?.returnMetaVariable?.defineMetaType?.metaClass?.eType == EType.Void )
            {
                m_IsRecieveReturnValue = true;
            }
            else
            {
                m_IsRecieveReturnValue = storeMv != null;
            }
        }

        public MetaMethodCall(MetaClass ownerClass, MetaBlockStatements ownerMBS, MetaFunction _fun, List<MetaType> mpipList, 
            MetaInputParamCollection _paramCollection, MetaType returnMt )
        {
            m_OwnerMetaClass = ownerClass;
            m_OwnerMetaBlockStatements = ownerMBS;
            m_InputParamCollectionForDebug = _paramCollection;
            m_VMCallMetaFunction = _fun;
            MetaMemberFunction mmf = _fun as MetaMemberFunction;
            m_MetaMemberFunction = mmf;
            m_ReturnMetaType = returnMt;
            //m_MetaInputParamList = _param;
            if (mpipList != null)
            {
                this.m_MetaFunctionInputTemplateList = mpipList;
            }

            List<MetaDefineParam> mpList = new();
            if (m_VMCallMetaFunction?.metaMemberParamCollection != null)
            {
                mpList = m_VMCallMetaFunction.metaMemberParamCollection.metaDefineParamList;
            }
            int inputCount = _paramCollection != null ? _paramCollection.metaInputParamList.Count : 0;

            for (int i = 0; i < inputCount; i++)
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
            if (m_VMCallMetaFunction?.returnMetaVariable?.defineMetaType?.metaClass?.eType == EType.Void)
            {
                m_IsRecieveReturnValue = true;
            }
            else
            {
                m_IsRecieveReturnValue = false;
            }
        }
        public void SetDebugInputParTermText(string? text)
        {
            m_DebugInputParTermText = text;
        }
        public MetaType GeMetaDefineType()
        {
            return m_VMCallMetaFunction.GetFinalMetaType();
        }
        public void SetToken( Token token )
        {
            this.m_Token = token;
        }
        public void AddMetaInputParamList(MetaExpressNodeBase inputp )
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
                if (!string.IsNullOrEmpty(m_DebugInputParTermText))
                {
                    sb.Append(m_VMCallMetaFunction.name);
                    sb.Append(m_DebugInputParTermText);
                }
                else
                {
                    sb.Append(m_VMCallMetaFunction.name + "(");
                    if (m_InputParamCollectionForDebug != null && m_InputParamCollectionForDebug.count > 0)
                    {
                        sb.Append(m_InputParamCollectionForDebug.ToFormatString());
                    }
                    else
                    {
                        int inputCount = m_MetaInputParamList.Count;
                        for (int i = 0; i < inputCount; i++)
                        {
                            sb.Append(m_MetaInputParamList[i].ToFormatString());
                            if (i < inputCount - 1)
                            {
                                sb.Append(",");
                            }
                        }
                    }
                    sb.Append(")");
                }
            }
            return sb.ToString();

        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( this.m_LoadMetaVariable != null )
            {
                sb.Append("[");
                sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allName);
                sb.Append("]");

                sb.Append(this.m_LoadMetaVariable.name);
                sb.Append(".");
            }
            else
            {
                //sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allClassName);
                //sb.Append(".");
            }
            if (m_VMCallMetaFunction != null)
            {
                if (!string.IsNullOrEmpty(m_DebugInputParTermText))
                {
                    sb.Append(m_VMCallMetaFunction.name);
                    sb.Append(m_DebugInputParTermText);
                }
                else
                {
                    sb.Append(m_VMCallMetaFunction.name + "(");
                    if (m_InputParamCollectionForDebug != null && m_InputParamCollectionForDebug.count > 0)
                    {
                        sb.Append(m_InputParamCollectionForDebug.ToFormatString());
                    }
                    else
                    {
                        int inputCount = m_MetaInputParamList.Count;
                        for (int i = 0; i < inputCount; i++)
                        {
                            sb.Append(m_MetaInputParamList[i].ToFormatString());
                            if (i < inputCount - 1)
                            {
                                sb.Append(",");
                            }
                        }
                    }
                    sb.Append(")");
                }
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
            None,
            ConstValue,
            Variable,
            VisitVariable,
            IteratorVariable,
            MethodCall,
            New,
            NewConst,
            Enum,
            EnumMember,
            MetaClass,
            MetaData,
            Express,
            TemplateName,
            GetTypeValue,
            SystemCall,
        }
        public MetaConstExpressNode constValueExpress => m_Express as MetaConstExpressNode;
        public MetaExpressNodeBase express => m_Express;
        public EVisitType visitType => m_VisitType;
        public MetaVariable variable => m_Variable;
        public MetaVisitVariable visitVariable => m_VisitVariable;
        public MetaMethodCall methodCall => m_MethodCall;
        public MetaType callMetaType => m_CallMetaType;
        public bool isQuestionMarkDot => m_IsQuestionMarkDot;
        /// <summary>仅 <see cref="EVisitType.GetTypeValue"/> 等场景填充；可能为 <see cref="MetaData"/>/<see cref="MetaEnum"/>。</summary>
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        public MetaClass ownerMetaClass => m_OwnerMetaBase as MetaClass;
        public Token token => m_Token;



        private EVisitType m_VisitType = EVisitType.None;
        private MetaMethodCall m_MethodCall = null;
        private MetaVisitVariable m_VisitVariable = null;
        private MetaVariable m_Variable  = null;
        private MetaExpressNodeBase m_Express  = null;
        private MetaType m_ReturnMetaType = null;
        private MetaBase m_OwnerMetaBase = null;
        private MetaTemplate m_MetaTemplate = null;
        private MetaType m_CallMetaType = null; //璇ュ彉閲忥紝涓€鑸槸涓?T t = new() 杩欑鎯呭喌鍑嗗鐨?
        private Token m_Token = null;
        private bool m_IsQuestionMarkDot = false;

        public static MetaVisitNode CreateByVisitMetaClass(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.MetaClass;
            if (mt?.metaClass == CoreMetaClassManager.enumMetaData)
            {
                mt.enumValue?.ownerMetaClass?.metaNode?.metaEnum?.GetOrCreateValuesVariable();
            }
            vn.m_ReturnMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByVisitMetaData(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.MetaData;
            vn.m_ReturnMetaType = mt;
            // 填充 callMetaType，使 IR 层能为 data 类型直接调用（如 Student.toString()）生成 LoadConstType
            vn.m_CallMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByVisitMetaEnum(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.Enum;
            if (mt.isEnum )
            {
                vn.m_Variable = mt.metaEnum?.GetOrCreateValuesVariable();
            }
            vn.m_ReturnMetaType = mt;

            return vn;
        }
        public static MetaVisitNode CreateByNewTemplate( MetaClass ownermc, MetaBlockStatements mbs, MetaType mt, MetaFunction mf, MetaVariable mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_VisitType = EVisitType.New;
            vn.m_Variable = mv;
            vn.m_Token = mv.token;
            vn.m_MethodCall = new MetaMethodCall(ownermc, mbs, mt, mf, null, null, null, null, mv);
            return vn;
        }
        public static MetaVisitNode CreateByNewConst(MetaClass ownermc, MetaBlockStatements mbs,
            MetaType mt, MetaConstExpressNode mce, MetaMemberFunction mmf, MetaInputParamCollection mipc )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.m_VisitType = EVisitType.NewConst;
            vn.m_Express = mce;
            if (mt.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(mt);
            }
            vn.m_MethodCall = new MetaMethodCall(ownermc, mbs, mt, mmf, null, mipc, null, null, null );

            return vn;
        }
        public static MetaVisitNode CreateByNewClass(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            //vn.m_MetaBraceStatementsContent = mb;
            vn.m_VisitType = EVisitType.New;
            if (mt.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(mt);
            }

            return vn;
        }
        public static MetaVisitNode CreateByNewArrayClass(MetaType mt, List<MetaExpressNodeBase> list )
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
                            int val = Convert.ToInt32(mcen2.value);
                            arrayLengthList.Add(val);
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
            vn.m_VisitType = EVisitType.New;
            if (newRMT.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = newRMT;
            }

            return vn;
        }
        public static MetaVisitNode CraeteByNewData(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_VisitType = EVisitType.New;

            return vn;
        }
        public static MetaVisitNode CraeteByEnum(MetaType mt)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_VisitType = EVisitType.Enum;

            return vn;
        }
        public static MetaVisitNode CreateByConstExpress(MetaConstExpressNode constExpress)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_Express = constExpress;
            vn.m_VisitType = EVisitType.ConstValue;

            return vn;
        }
        public static MetaVisitNode CreateByEpxress(MetaExpressNodeBase _express)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_Express = _express;
            vn.m_VisitType = EVisitType.Express;

            return vn;
        }
        public static MetaVisitNode CreateByGetType( MetaBase owner, MetaType mt, MetaVariable mv )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_OwnerMetaBase = owner;
            vn.m_Variable = mv;
            vn.m_VisitType = EVisitType.GetTypeValue;

            return vn;
        }
        public static MetaVisitNode CreateByEnumMember( MetaType mt, MetaVariable _variable )
        {
            MetaVisitNode vn = new MetaVisitNode();
            vn.m_Variable = _variable;
            // Important: the enum member argument must be treated as an enum value when
            // matching against a function parameter declared as enum.
            vn.m_CallMetaType = mt;
            vn.m_VisitType = EVisitType.EnumMember;

            return vn;
        }
        public static MetaVisitNode CreateByMethodCall( MetaMethodCall _methodCall)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.MethodCall;
            vn.m_MethodCall = _methodCall;
            vn.SetToken(_methodCall.token);

            return vn;
        }
        public static MetaVisitNode CreateBySystemCall(MetaMethodCall _methodCall)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.SystemCall;
            vn.m_MethodCall = _methodCall;

            return vn;
        }
        public static MetaVisitNode CreateByVisitVariable(MetaVisitVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.VisitVariable;
            vn.m_VisitVariable = _variale;

            return vn;
        }
        public static MetaVisitNode CreateByVariable(MetaVariable _variale, MetaType callerMt = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.Variable;
            vn.m_Variable = _variale;
            vn.m_CallMetaType = callerMt;

            return vn;
        }
        public static MetaVisitNode CreateByThis(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.Variable;
            vn.m_Variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public static MetaVisitNode CreateByBase(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.Variable;
            vn.m_Variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public static MetaVisitNode CreateByTemplate(MetaTemplate _metatemplate)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_VisitType = EVisitType.TemplateName;
            vn.m_MetaTemplate = _metatemplate;

            return vn;
        }
        public void SetToken( Token token )
        {
            this.m_Token = token;
        }
        public void SetQuestionMarkDot(bool isQMD)
        {
            this.m_IsQuestionMarkDot = isQMD;
        }
        public void SetMethodCall( MetaMethodCall _methodCall)
        {
            this.m_MethodCall = _methodCall;
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
                        return methodCall.returnMetaType;
                        //if( methodCall.metaMemberFunction != null )
                        //{
                        //    return methodCall.metaMemberFunction.returnMetaVariable.GetFinalMetaType()
                        //        ?? methodCall.metaMemberFunction.GetFinalMetaType();
                        //}
                        //var finalRetMetaType = methodCall.function.returnMetaVariable.GetFinalMetaType();
                        //if (finalRetMetaType != null)
                        //{
                        //    return finalRetMetaType;
                        //}
                        //return methodCall.function.GetFinalMetaType();
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
                case EVisitType.EnumMember:
                    {
                        return m_CallMetaType;
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
                        return this.express.expressReturnMetaType;
                    }
                case EVisitType.Enum:
                    {
                        return this.m_CallMetaType;
                    }
                case EVisitType.GetTypeValue:
                    {
                        return new MetaType(CoreMetaClassManager.typeMetaClass);
                    }
                case EVisitType.NewConst:
                    {
                        return this.constValueExpress.expressReturnMetaType;
                    }
                case EVisitType.MetaClass:
                    {
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreVisitCallTypeError, m_Token, visitType.ToString() );
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
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error");
                return null;
            }
            return mt.metaClass;
        }
        public MetaVariable GetDefineMetaVariable()
        {
            switch (visitType)
            {
                case EVisitType.Variable:
                    {
                        return variable;
                    }
                case EVisitType.MethodCall:
                case EVisitType.SystemCall:
                    {
                        if( methodCall.function is MetaMemberFunction mmf )
                        {
                            if( mmf.isSet )
                            {
                                if( mmf.metaMemberParamCollection.metaDefineParamList.Count > 0 )
                                {
                                    return mmf.metaMemberParamCollection.metaDefineParamList[0].metaVariable;
                                }
                            }
                            else
                            {
                                if (mmf.metaMemberParamCollection.metaDefineParamList.Count > 0)
                                {
                                    return mmf.metaMemberParamCollection.metaDefineParamList[0].metaVariable;
                                }
                            }
                        }
                        else
                        {
                            if(methodCall.function.metaMemberParamCollection.metaDefineParamList?.Count > 0 )
                                return methodCall.function.metaMemberParamCollection.metaDefineParamList[0].metaVariable;
                        }
                        return null;
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
                case EVisitType.MetaData:
                    {
                    }
                    break;
                case EVisitType.EnumMember:
                    {
                        return variable;
                    }
                default:
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreVisitCallTypeError, m_Token, "Error MetaVisiCall IsNull!");
                    }
                    break;
            }
            return null;
        }
        public MetaVariable GetStoreMetaVariable()
        {
            switch (visitType)
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
                case EVisitType.MetaData:
                    {
                    }
                    break;
                case EVisitType.EnumMember:
                    {
                        return variable;
                    }
                default:
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreVisitCallTypeError, m_Token, "Error MetaVisiCall IsNull!");
                    }
                    break;
            }
            return null;
        }
        public MetaVariable GetReturnMetaVariable()
        {
            switch (visitType)
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
                case EVisitType.MetaData:
                    {
                    }
                    break;
                case EVisitType.EnumMember:
                    {
                        return variable;
                    }
                case EVisitType.GetTypeValue:
                    {
                        return variable;
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreVisitCallTypeError, m_Token, "Error MetaVisiCall IsNull!");
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
                        //sb.Append(this.m_CallMetaType.ToString() );
                    }
                    break;
                case EVisitType.MetaData:
                    {
                        sb.Append(this.m_ReturnMetaType.metaData.metaNode.ToString());
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
                case EVisitType.Enum:
                    {
                        if (this.variable != null)
                        {
                            sb.Append(this.variable.ToString());
                        }
                        else if (this.m_CallMetaType != null)
                        {
                            sb.Append(this.m_CallMetaType.ToString());
                        }
                    }
                    break;
                case EVisitType.EnumMember:
                    {
                        if (this.variable != null)
                        {
                            sb.Append(this.variable.ToString());
                        }
                        else if (this.visitVariable != null)
                        {
                            sb.Append(this.visitVariable.ToFormatString());
                        }
                    }
                    break;
                case EVisitType.GetTypeValue:
                    {
                        sb.Append("type");
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
                case EVisitType.Enum:
                    {
                        sb.Append(this.m_ReturnMetaType.metaEnum.metaNode.ToString());
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
                case EVisitType.MetaData:
                    {

                        sb.Append(this.m_ReturnMetaType.metaData.metaNode.ToString());
                    }
                    break;
                case MetaVisitNode.EVisitType.EnumMember:
                    {
                        sb.Append(this.variable.ToFormatString());
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
