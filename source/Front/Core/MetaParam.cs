//****************************************************************************
//  File:      ClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta params about info class!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{

    public class MetaInputParam
    {
        public MetaExpressNodeBase express => m_Express;
        public Token token => m_Token;
        public string paramName => m_ParamName;

        protected FileInputParamNode m_FileInputParamNode;
        protected MetaExpressNodeBase m_Express = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements;
        protected MetaBase m_OwnerMetaBase = null;
        protected Token m_Token;
        protected string m_ParamName = null;
        public MetaInputParam( FileInputParamNode fipn, MetaBase mc, MetaBlockStatements mbs, string keywordName = null )
        {
            m_FileInputParamNode = fipn;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = mc;
            m_ParamName = keywordName;

            CreateExpressParam cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaBase = m_OwnerMetaBase,
                metaType = null,
                fme = m_FileInputParamNode.express,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.InputParamExpress
            };
            m_Express = ExpressManager.CreateExpressNode(cep);
            m_Token = m_FileInputParamNode.express.token;
        }
        public MetaInputParam( MetaExpressNodeBase inputExpress )
        {
            m_Express = inputExpress;
        }
        public void ReplaceExpress( MetaExpressNodeBase men )
        {
            m_Express = men;
        }
        public virtual void Parse(AllowUseSettings allowUse )
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings(allowUse) { parseFrom = EParseFrom.InputParamExpress, ifNotVariableThenAddVariable = false } );
                m_Express.CalcReturnType();
                m_Express = ExpressManager.ConvertNewExpress(m_Express, null);
            }
        }
        public virtual void CaleReturnType()
        {
            if(m_Express != null )
            {
                m_Express.CalcReturnType();                
            }
        }
        public MetaType GetRetMetaType()
        {
            if( m_Express != null )
            {
                return m_Express.GetReturnMetaType();
            }
            return null;
        }
        public virtual string ToFormatString()
        {
            return m_Express?.ToFormatString();
        }
        public string ToStatementString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Express.ToFormatString());

            return sb.ToString();
        }
    }
    public class MetaDefineParam 
    {
        public string name => m_Name;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaExpressNodeBase expressNode => m_MetaExpressNode;
        //public bool isFunctionTemplate => m_IsFunctionTemplate;
        public bool isMust { get { return m_MetaExpressNode == null && !m_HasExpressImported; } }
        public bool isExtendParams => m_FileMetaParamter?.paramsToken != null || m_ExtendParamsForced;
        public bool isHasExpress => m_IsHasExpress || m_HasExpressImported;

        // 从编译后的引用模块还原方法时没有 FileMeta 语法节点，
        // 用该标记补上 params 可变参数属性（配合 MetaDefineParamCollection.isExtendParams 参与调用匹配）。
        protected bool m_ExtendParamsForced = false;
        public void SetExtendParams() { m_ExtendParamsForced = true; }

        /// <summary>
        /// 标记该参数有默认表达式（从引用模块导入，避免 isMust 匹配失败）。
        /// </summary>
        protected bool m_HasExpressImported = false;
        public void SetHasExpress() { m_HasExpressImported = true; }

        protected bool m_IsFunctionTemplate = false;
        protected FileMetaParamterDefine m_FileMetaParamter = null;
        protected MetaExpressNodeBase m_MetaExpressNode = null;
        protected MetaVariable m_MetaVariable = null;
        protected MetaFunction m_OwnerMetaFunction = null;
        protected string m_Name = "";
        protected Token m_Token = null;
        protected bool m_IsHasExpress = false;

        public MetaDefineParam()
        {

        }
        public MetaDefineParam( string _name, MetaFunction mf )
        {
            m_Name = _name;
            m_OwnerMetaFunction = mf;
            m_MetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.Argument,
                null, m_OwnerMetaFunction.ownerMetaClass, null );
        }
        public MetaDefineParam(MetaDefineParam mdp )
        {
            m_Name = mdp.m_Name;
            m_IsFunctionTemplate = mdp.m_IsFunctionTemplate;
            m_FileMetaParamter = mdp.m_FileMetaParamter;
            m_IsHasExpress = m_FileMetaParamter != null && m_FileMetaParamter.express != null;
            // 从引用模块导入的参数没有 FileMeta / 表达式 AST，只有标志位，拷贝时必须保留，
            // 否则 isMust 判定回退为 true，省略默认参数的调用会匹配失败（模板实例化 / MetaMethod 复制路径）。
            m_HasExpressImported = mdp.m_HasExpressImported;
            m_ExtendParamsForced = mdp.m_ExtendParamsForced;
            m_MetaExpressNode = mdp.m_MetaExpressNode;
            m_OwnerMetaFunction = mdp.m_OwnerMetaFunction;
            m_MetaVariable = new MetaVariable( mdp.m_MetaVariable );
            m_Token = mdp.m_Token;
        }
        public MetaDefineParam(MetaFunction mf, FileMetaParamterDefine fmp)
        {
            m_OwnerMetaFunction = mf;
            m_FileMetaParamter = fmp;
            m_Name = m_FileMetaParamter.name;

            m_MetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.Argument,
                null, m_OwnerMetaFunction.ownerMetaClass, null );
            m_Token = m_FileMetaParamter.token;
            m_MetaVariable.SetToken(m_Token);
            m_IsHasExpress = m_FileMetaParamter.express != null;
        }
        public void SetOwnerMetaFunction(MetaFunction mf)
        {
            m_OwnerMetaFunction = mf;
            if (m_MetaVariable != null)
            {
                m_MetaVariable.SetOwnerMetaBase(mf?.ownerMetaBase);
            }
        }
        public void ParseMetaDefineType()
        {
            if ( this.m_FileMetaParamter?.classDefineRef != null)
            {
                var mdt = TypeManager.instance.GetMetaTypeByTemplateFunction(m_OwnerMetaFunction.ownerMetaClass, m_OwnerMetaFunction as MetaMemberFunction, m_FileMetaParamter.classDefineRef);
                m_MetaVariable.SetMetaDefineType(mdt);
                m_MetaVariable.SetIsDefineMetaType(true);
            }
            else
            {
                MetaType mdt = new MetaType(CoreMetaClassManager.objectMetaClass);
                m_MetaVariable.SetMetaDefineType(mdt);
            }

        }
        public void CreateExpress()
        {
            if (m_FileMetaParamter?.express != null)
            {
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMBS = null,
                    ownerMetaBase = m_OwnerMetaFunction.ownerMetaBase,
                    metaType = m_MetaVariable.GetFinalMetaType(),
                    fme = m_FileMetaParamter.express,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.InputParamExpress
                };
                m_MetaExpressNode = ExpressManager.CreateExpressNode(cep);
            }
            else
            {
                m_MetaVariable.SetIsDefineMetaType(true);
            }
        }
        public virtual void Parse()
        {
            if (m_MetaExpressNode != null)
            {
                AllowUseSettings auc = new AllowUseSettings();
                auc.useNotConst = false;
                auc.useNotStatic = false;
                auc.callConstructFunction = true;
                auc.callFunction = true;
                m_MetaExpressNode.Parse(auc);
            }
        }
        public bool EqualDefineMetaParam(MetaDefineParam param)
        {
            if (param != null)
            {
                MetaType left = param.metaVariable.defineMetaType;
                MetaType right = metaVariable.defineMetaType;

                if( left.isClass && right.isClass )
                {
                    if( left.metaClass == right.metaClass )
                    {
                        return true;
                    }
                }
                else if( left.isData && right.isData )
                {
                    if( left.metaData == right.metaData )
                    {
                        return true;
                    }
                }
                else if( left.isEnum && right.isEnum )
                {
                    if (left.metaEnum == right.metaEnum )
                    {
                        return true;
                    }
                }
                return false;
                /*
                // exact match
                if (TypeManager.CompareMetaType(md, metaVariable.defineMetaType))
                {
                    return true;
                }

                if ( TypeManager.TryNumberArrayCovarianceAllow(md, metaVariable.defineMetaType, metaVariable))
                    return true;

                // ??????????????????
                if (md.IsArray() && metaVariable.defineMetaType.IsArray())
                    return false;

                // allow match when types are in inheritance relationship (e.g., defined: Num, concrete: SByte)
                MetaClass thisClass = metaVariable.defineMetaType?.GetTemplateMetaClass();
                MetaClass otherClass = md?.GetTemplateMetaClass();
                if (thisClass != null && otherClass != null)
                {
                    var relation = TypeManager.ValidateClassTypeRelation(thisClass, otherClass);
                    if (relation == ETypeRelation.Same
                        || relation == ETypeRelation.Child
                        || relation == ETypeRelation.Parent
                        || relation == ETypeRelation.Interface )
                    {
                        return true;
                    }
                }
                */
                return false;
            }
            return false;
        }
        public bool EqualsInputMetaParam(MetaInputParam mip)
        {
            if (m_MetaVariable == null) return false;

            var declaredMt = m_MetaVariable.defineMetaType;
            var argMt = mip.express != null ? mip.express.GetReturnMetaType() : null;
            if (declaredMt == null || argMt == null) return false;

            if (TypeManager.CompareFunctionDefineMetaTypeAndInputMetaType(declaredMt, argMt, mip.token))
                return true;

            // C#-style implicit constant conversion at call sites (e.g. f(60) with a
            // byte parameter). Reuses the same range-checked narrowing as the
            // assignment path (TryAdjustConstExpressByDefineMetaType). Guarded to
            // numeric targets so non-numeric conversions (e.g. to string) never run.
            var mcen = mip.express as MetaConstExpressNode;

            // "-literal" is a constant expression in C#: fold the unary negation
            // into its inner numeric constant before applying the conversion.
            if (mcen == null
                && mip.express is MetaUnaryOpExpressNode muoen
                && muoen.opSign == ESingleOpSign.Neg
                && muoen.value is MetaConstExpressNode negCen
                && NumberManager.IsNumericEType(negCen.eType))
            {
                mcen = muoen.SimulateCompute() as MetaConstExpressNode;
                if (mcen != null)
                {
                    mip.ReplaceExpress(mcen);
                }
            }

            if (mcen != null)
            {
                var declaredEType = CoreMetaClassManager.GetETypeByMetaClass(declaredMt.metaClass);
                if (declaredEType != EType.Object
                    && NumberManager.IsNumericEType(declaredEType)
                    && NumberManager.IsNumericEType(mcen.eType)
                    && ExpressManager.TryAdjustConstExpressByDefineMetaType(declaredMt, mcen))
                {
                    var adjustedMt = mip.express.GetReturnMetaType();
                    if (adjustedMt != null
                        && TypeManager.CompareFunctionDefineMetaTypeAndInputMetaType(declaredMt, adjustedMt, mip.token))
                        return true;
                }
            }

            return false;
        }
        public bool EqualsName( string name )
        {
            return m_MetaVariable.name.Equals(name);
        }
        public void SetDefineMetaType( MetaType mt )
        {
            m_MetaVariable.SetMetaDefineType(mt);
        }       
        public void CaleReturnType()
        {
            if(m_MetaExpressNode != null )
            {
                m_MetaExpressNode.CalcReturnType();
                m_MetaVariable.SetRealMetaType(m_MetaExpressNode.GetReturnMetaType());


                if( !TypeManager.CompareLeftRightMetaType( m_MetaVariable.defineMetaType, m_MetaVariable.realMetaType, m_Token, out MetaType convertMt ) )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "define param compare error");
                }
            }
            //if( !isTemplate )
            {
               // ExpressManager.CalcDefineClassType(ref m_DefineMetaClassType, m_Express, m_OwnerMetaClass, m_OwnerMetaBlockStatements?.ownerMetaFunction, defineName, ref m_IsNeedCastStatements );
            }   
        }
        public virtual string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_MetaVariable?.ToFormatString());
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_MetaVariable != null)
            {
                sb.Append(m_MetaVariable.defineMetaType.ToFormatString() );
                sb.Append(" ");
                sb.Append(m_Name);
            }
            if( m_MetaExpressNode != null )
            {
                sb.Append(" = ");
                sb.Append(m_MetaExpressNode.ToFormatString() );
            }

            return sb.ToString();
        }
    }

    public sealed class MetaDefineParamCollection
    {
        public bool isExtendParams => m_IsExtendParams;
        public int maxParamCount => m_MetaDefineParamList.Count;
        public List<MetaDefineParam> metaDefineParamList => m_MetaDefineParamList;
        public bool isCanCallFunction => m_IsCanCallFunction;
        public bool isAllConst => m_IsAllConst;
        public int minParamCount => m_MinParamCount;
        public bool isHaveDefaultParamExpress => m_IsHaveDefaultParamExpress;
        public bool isHasExpress => m_IsHasExpress;

        private bool m_IsCanCallFunction = true;
        private bool m_IsExtendParams = false;
        private int m_MinParamCount = 0;
        private bool m_IsAllConst = false;
        private bool m_IsHaveDefaultParamExpress = false;
        private bool m_IsHasExpress = false;
        private List<MetaDefineParam> m_MetaDefineParamList = new List<MetaDefineParam>();
        public MetaDefineParamCollection()
        {

        }
        public MetaDefineParamCollection(MetaDefineParamCollection mdpc )
        {
            m_IsCanCallFunction = mdpc.m_IsCanCallFunction;
            m_IsExtendParams = mdpc.m_IsExtendParams;
            m_MinParamCount = mdpc.m_MinParamCount;
            m_IsAllConst = mdpc.m_IsAllConst;
            m_IsHaveDefaultParamExpress = mdpc.m_IsHaveDefaultParamExpress;
            
            for( int i = 0; i < mdpc.m_MetaDefineParamList.Count; i++ )
            {
                var mdp = new MetaDefineParam(mdpc.m_MetaDefineParamList[i]);
                m_MetaDefineParamList.Add(mdp);
                if( mdp.isHasExpress )
                {
                    m_IsHasExpress = true;
                }
            }
        }
        public MetaDefineParamCollection(bool _isAllConst, bool _isCanCallFunction)
        {
            m_IsAllConst = _isAllConst; 
            m_IsCanCallFunction = _isCanCallFunction;
        }
        public void Clear()
        {
            m_MetaDefineParamList.Clear();
        }
        public void SetOwnerMetaBase( MetaBase ownerBase)
        {
            for (int i = 0; i < m_MetaDefineParamList.Count; i++)
            {
                var dParam = m_MetaDefineParamList[i];
                dParam?.metaVariable?.SetOwnerMetaBase(ownerBase);
            }
        }
        public void SetOwnerMetaFunction(MetaFunction ownerFunction)
        {
            for (int i = 0; i < m_MetaDefineParamList.Count; i++)
            {
                var dParam = m_MetaDefineParamList[i];
                dParam?.SetOwnerMetaFunction(ownerFunction);
            }
        }
        public MetaDefineParam GetMetaDefineParamByName( string name )
        {
            for (int i = 0; i < m_MetaDefineParamList.Count; i++)
            {
                var dParam = m_MetaDefineParamList[i];
                if (dParam.EqualsName(name))
                        return dParam;
            }
            return null;
        }
        public bool CheckDefineMetaParam(MetaDefineParam a, MetaDefineParam b)
        {
            if (a.EqualDefineMetaParam(b))
                    return true;
            return a == b;
        }
        public void AddMetaDefineParam(MetaDefineParam metaMemberParam)
        {
            if( m_IsExtendParams )
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Params ???????????????????????????????????");
                return;
            }

            m_MetaDefineParamList.Add(metaMemberParam);
            if( metaMemberParam.isExtendParams )
            {
                m_IsExtendParams = true;
            }

            if(isHaveDefaultParamExpress)
            {
                // 已进入默认参数段：后续参数必须带默认值。
                // 注意导入（ref module）函数没有表达式 AST（expressNode 恒为 null），
                // 只保留 isHasExpress 标志，因此必须用标志判断而不能用 expressNode。
                if (!metaMemberParam.isHasExpress)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error AddMetaDefineParam 参数前边已定义默认值，后边必须跟进默认值表达式!!");
                }
            }
            else if (metaMemberParam.isMust)
            {
                // 必须参数段：最小调用实参数 = 必须参数个数（最大形式即全部参数，见 maxParamCount）。
                m_MinParamCount++;
            }
            else
            {
                // 首个默认参数：进入默认参数段。
                m_IsHaveDefaultParamExpress = true;
            }
        }
        public bool IsEqualMetaInputParamCollection(MetaInputParamCollection mpc)
        {
            int inputCount = 0;
            if( mpc != null )
            {
                inputCount = mpc.metaInputParamList.Count;
            }
            // 关键字（命名）参数：按定义名称匹配，不要求实参顺序与形参定义顺序一致
            if( mpc != null && mpc.hasKeywordParam )
            {
                return IsEqualKeywordMetaInputParamCollection(mpc, inputCount);
            }
            if ( m_IsExtendParams )
            {
                //??????????????????????????params ?????????????????????????????????????????                
                if(m_MetaDefineParamList.Count == 0 )
                {
                    return false;
                }


                if (inputCount <= m_MetaDefineParamList.Count )
                {
                    for (int i = 0; i < inputCount; i++)
                    {
                        MetaDefineParam a = m_MetaDefineParamList[i];
                        if (a == null)
                            return false;
                        if (a.isExtendParams) break;
                        MetaInputParam b = null;
                        if (mpc != null && i < inputCount)
                        {
                            b = mpc.metaInputParamList[i];
                        }
                        if (!MetaInputParamCollection.CheckInputMetaParam(a, b))
                            return false;
                    }
                }

                //var lastMdp = m_MetaDefineParamList[m_MetaDefineParamList.Count - 1];
                //if(lastMdp.isExtendParams && lastMdp.metaVariable.isArray )
                //{
                //    var mdt = lastMdp.metaVariable.isDefineMetaType ? lastMdp.metaVariable.defineMetaType : lastMdp.metaVariable.realMetaType;
                //    for( int i = inputCount; i < m_MetaDefineParamList.Count - 1; i++ ) 
                //    {
                //        var mdp_metaType = m_MetaDefineParamList[i].metaVariable.GetFinalMetaType();
                //        var mip = mpc.metaInputParamList[i];
                //        var retmt = mip.GetRetMetaType();

                //        if (retmt.isData)
                //        { 
                //        }
                //        else if( retmt.isEnum )
                //        {

                //        }
                //        else
                //        {
                //            var retmc = retmt.metaClass;
                //            if (retmc is MetaGenTemplateClass mgtc)
                //            {
                //                retmc = mgtc.metaTemplateClass;
                //            }
                //            if (retmc != mdp_metaType.metaClass)
                //            {
                //                return false;
                //            }
                //        }
                //    }
                //    return true;
                //}

                return true;
            }
            else
            {
                if (m_MetaDefineParamList.Count >= inputCount)
                {
                    for (int i = 0; i < m_MetaDefineParamList.Count; i++)
                    {
                        MetaDefineParam a = m_MetaDefineParamList[i];
                        if (a == null)
                            return false;
                        //if( a.metaDefineTypeName )
                        MetaInputParam b = null;
                        if (mpc != null && i < inputCount)
                        {
                            b = mpc.metaInputParamList[i];
                        }
                        if (!MetaInputParamCollection.CheckInputMetaParam(a, b))
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// 关键字（命名）参数匹配：位置实参按顺序填充形参槽位，命名实参按名称填充槽位，
        /// 因此实参顺序不必与形参定义顺序一致（与 MetaMethodCall.ReorderKeywordArgs 语义保持一致）。
        /// </summary>
        private bool IsEqualKeywordMetaInputParamCollection(MetaInputParamCollection mpc, int inputCount)
        {
            int defineCount = m_MetaDefineParamList.Count;
            MetaInputParam[] matched = new MetaInputParam[defineCount];
            // params 可变参数槽位（最后一个），允许多余的位置实参进入
            int extendParamIndex = m_IsExtendParams ? defineCount - 1 : -1;
            int positionalSlot = 0;

            for (int i = 0; i < inputCount; i++)
            {
                MetaInputParam mip = mpc.metaInputParamList[i];
                if (string.IsNullOrEmpty(mip.paramName))
                {
                    // 位置参数：按顺序填充槽位
                    if (positionalSlot < defineCount)
                    {
                        if (matched[positionalSlot] != null)
                            return false;
                        matched[positionalSlot] = mip;
                        positionalSlot++;
                    }
                    else if (extendParamIndex >= 0)
                    {
                        // 多余的位置参数进入 params 可变参数
                        if (matched[extendParamIndex] == null)
                            matched[extendParamIndex] = mip;
                    }
                    else
                    {
                        return false; // 实参数量超过形参数量
                    }
                }
                else
                {
                    // 命名参数：按名称查找形参槽位
                    int targetIndex = -1;
                    for (int j = 0; j < defineCount; j++)
                    {
                        var mdp = m_MetaDefineParamList[j];
                        if (mdp == null)
                            return false;
                        if (mdp.name == mip.paramName)
                        {
                            targetIndex = j;
                            break;
                        }
                    }
                    if (targetIndex < 0)
                        return false; // 不存在该名称的形参
                    if (targetIndex == extendParamIndex)
                        return false; // params 可变参数不支持命名传参
                    if (matched[targetIndex] != null)
                        return false; // 该参数被重复赋值
                    matched[targetIndex] = mip;
                }
            }

            // 逐槽位检查类型匹配 / 缺省参数
            for (int i = 0; i < defineCount; i++)
            {
                MetaDefineParam a = m_MetaDefineParamList[i];
                if (a == null)
                    return false;
                if (i == extendParamIndex)
                    continue; // params 槽位由剩余位置实参填充，不做类型强校验
                if (!MetaInputParamCollection.CheckInputMetaParam(a, matched[i]))
                    return false;
            }
            return true;
        }
        public bool IsEqualMetaDefineParamCollection(MetaDefineParamCollection mdpc)
        {
            if (mdpc == null)
            {
                return minParamCount == 0;
            }

            if (m_MetaDefineParamList.Count == mdpc.m_MetaDefineParamList.Count)
            {
                if (m_MetaDefineParamList.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < m_MetaDefineParamList.Count; i++)
                {
                    var a = m_MetaDefineParamList[i];
                    var b = mdpc.m_MetaDefineParamList[i];
                    if (!CheckDefineMetaParam(a, b))
                        return false;
                }
                return true;
            }
            return false;
        }
        public bool IsEqualMetaTypeList(List<MetaType> mtList )
        {

            if (m_MetaDefineParamList.Count == mtList.Count)
            {
                if (m_MetaDefineParamList.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < m_MetaDefineParamList.Count; i++)
                {
                    var left = m_MetaDefineParamList[i]?.metaVariable?.GetFinalMetaType();
                    var right = mtList[i];
                    if (left == null || right == null ) return false;

                    if(left.isClass && right.isClass )
                    {
                        if( left.metaClass == right.metaClass )
                        {
                            return true;
                        }
                        return false;
                    }
                    else if (left.isData && right.isData )
                    {
                        if (left.metaData == right.metaData )
                        {
                            return true;
                        }
                        return false;
                    }
                    else if( left.isEnum && right.isEnum )
                    {
                        if( left.metaEnum == right.metaEnum )
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        public string ToParamTypeName()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_MetaDefineParamList.Count; i++)
            {
                // functionAllName/id generation must distinguish overloads.
                // Previously we appended parameter *variable name* (often `_value` for many overloads),
                // which caused different overloads to collide and only one IRMethod to survive.
                // Use parameter *declared type* instead; fallback to variable name when type is unavailable.
                var param = m_MetaDefineParamList[i];
                var dt = param?.metaVariable?.defineMetaType;
                if (dt != null)
                    sb.Append(dt.ToString());
                else
                    sb.Append(param?.name ?? string.Empty);
                if (i < m_MetaDefineParamList.Count - 1)
                    sb.Append("_");
            }
            return sb.ToString();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("(");
            for (int i = 0; i < m_MetaDefineParamList.Count; i++)
            {
                sb.Append(m_MetaDefineParamList[i].ToFormatString());
                if (i < m_MetaDefineParamList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
    public sealed class MetaInputParamCollection
    {
        public List<MetaInputParam> metaInputParamList => m_MetaInputParamList;
        public int count { get { return m_MetaInputParamList.Count; } }
        /// <summary>
        /// 是否存在关键字（命名）参数（如 foo( name = "x", id = 1 )）。
        /// 存在时函数匹配需要按参数名称匹配，而不依赖实参顺序。
        /// </summary>
        public bool hasKeywordParam
        {
            get
            {
                for (int i = 0; i < m_MetaInputParamList.Count; i++)
                {
                    if (!string.IsNullOrEmpty(m_MetaInputParamList[i].paramName))
                        return true;
                }
                return false;
            }
        }
        private MetaBase m_OwnerMetaBase = null;
        private MetaBlockStatements m_MetaBlockStatements = null;
        private List<MetaInputParam> m_MetaInputParamList = new List<MetaInputParam>();

        public MetaInputParamCollection(MetaBase mc, MetaBlockStatements mbs)
        {
            m_OwnerMetaBase = mc;
            m_MetaBlockStatements = mbs;
        }
        public MetaInputParamCollection(FileMetaParTerm fmpt, MetaBase mc, MetaBlockStatements mbs)
        {
            m_OwnerMetaBase = mc;
            m_MetaBlockStatements = mbs;
            var splitList = fmpt.SplitParamList();
            for (int i = 0; i < splitList.Count; i++)
            {
                var term = splitList[i];
                string keywordName = TryExtractKeywordArg(ref term);
                FileInputParamNode fnpn = new FileInputParamNode(term);
                MetaInputParam mp = new MetaInputParam(fnpn, m_OwnerMetaBase, m_MetaBlockStatements, keywordName);
                AddMetaInputParam(mp);
            }
        }
        /// <summary>
        /// Detects "name = expr" pattern in a FileMetaBaseTerm and extracts
        /// the keyword name and expression-only term. Returns the param name
        /// or null if no keyword arg is present.
        /// </summary>
        private static string TryExtractKeywordArg(ref FileMetaBaseTerm term)
        {
            if (term is FileMetaTermExpress fmte)
            {
                var subList = fmte.fileMetaExpressList;
                int assignIndex = -1;
                for (int j = 0; j < subList.Count; j++)
                {
                    if (subList[j] is FileMetaSymbolTerm fst && fst.symBolType == ETokenType.Assign)
                    {
                        assignIndex = j;
                        break;
                    }
                }
                if (assignIndex > 0 && assignIndex < subList.Count - 1)
                {
                    var nameTerm = subList[assignIndex - 1];
                    // 关键字参数名必须是单个纯标识符（如 foo( name = expr ) 中的 name）。
                    // 标识符被包装为 FileMetaCallTerm，而 FileMetaCallTerm 自身不持有 token，
                    // 必须从 callLink 的首节点取名称；链式（a.b）、数组、泛型、带 brace 的形式不算关键字名。
                    string paramName = null;
                    if (nameTerm is FileMetaCallTerm fmct
                        && fmct.callLink != null
                        && fmct.callLink.isOnlyName)
                    {
                        var kcn = fmct.callLink.callNodeList[0];
                        if (!kcn.isArray
                            && kcn.fileMetaBraceTerm == null
                            && kcn.inputTemplateNodeList.Count == 0)
                        {
                            paramName = fmct.callLink.name;
                        }
                    }
                    if (string.IsNullOrEmpty(paramName))
                    {
                        // 左侧不是合法的参数名标识符：不作为关键字参数处理，保留原表达式
                        return null;
                    }

                    var afterTerms = new List<FileMetaBaseTerm>();
                    for (int j = assignIndex + 1; j < subList.Count; j++)
                    {
                        afterTerms.Add(subList[j]);
                    }

                    if (afterTerms.Count == 1)
                    {
                        term = afterTerms[0];
                    }
                    else
                    {
                        term = new FileMetaTermExpress(fmte.fileMeta, afterTerms, FileMetaTermExpress.EExpressType.Common);
                    }

                    return paramName;
                }
            }
            return null;
        }
        public void Clear()
        {
            m_MetaInputParamList.Clear();
        }
        public static bool CheckInputMetaParam(MetaDefineParam a, MetaInputParam b)
        {
            if (b == null)
            {
                return !a.isMust;      // ???????????????
            }
            if (a.EqualsInputMetaParam(b))
                return true;
            return false;
        }
        /*
        public bool IsEqualMetaTemplateAndParamCollection(MetaInputTemplateCollection mitc, MetaInputParamCollection mpc)
        {
            if (mpc == null)
            {
                return m_MetaInputParamList.Count == 0;
            }

            int templateCount = 0;
            //if (mitc != null)
            //{
            //    templateCount = mitc.metaTemplateParamsList.Count;
            //}
            //if (m_MetaInputParamList.Count == mpc.metaInputParamList.Count + templateCount)
            //{
            //    int index = 0;
            //    if (mitc != null)
            //    {
            //        for (int i = 0; i < mitc.metaTemplateParamsList.Count; i++)
            //        {
            //            MetaDefineParam a = m_MetaDefineParamList[index++];
            //            MetaType b = mitc.metaTemplateParamsList[i];
            //            if (!a.isTemplateMetaClass)
            //            {
            //                return false;
            //            }
            //        }
            //    }
            //    for (int i = 0; i < mpc.metaParamList.Count; i++)
            //    {
            //        MetaDefineParam a = metaParamList[index++] as MetaDefineParam;
            //        MetaInputParam b = mpc.metaParamList[i] as MetaInputParam;
            //        if (!CheckInputMetaParam(a, b))
            //            return false;
            //    }
            //    return true;
            //}
            return false;
        }
        */

        public void ParseList( List<FileInputParamNode> splitList )
        {
            for (int i = 0; i < splitList.Count; i++)
            {
                MetaInputParam mp = new MetaInputParam(splitList[i], m_OwnerMetaBase, m_MetaBlockStatements);
                AddMetaInputParam(mp);
            }
        }
        public void AddMetaInputParam( MetaInputParam mip )
        {
            m_MetaInputParamList.Add(mip);
        }
        public void Parse(AllowUseSettings alu)
        {
            for (int i = 0; i < m_MetaInputParamList.Count; i++)
            {
                m_MetaInputParamList[i].Parse(alu);
            }
        }
        public void CaleReturnType()
        {
            for (int i = 0; i < m_MetaInputParamList.Count; i++)
            {
                m_MetaInputParamList[i].CaleReturnType();
            }
        }
        //public MetaClass GetMaxLevelMetaClassType()
        //{
        //    MetaClass mc = CoreMetaClassManager.objectMetaClass;
        //    bool isAllSame = true;
        //    for (int i = 0; i < m_MetaInputParamList.Count - 1; i++)
        //    {
        //        MetaInputParam cmc = m_MetaInputParamList[i];
        //        MetaInputParam nmc = m_MetaInputParamList[i + 1];
        //        if (mc == null || nmc == null) continue;
        //        if (cmc.express.opLevel == nmc.express.opLevel)
        //        {
        //            if( cmc.express.opLevel == 10 )
        //            {
        //                var cur = cmc.GetRetMetaType();
        //                var next = nmc.GetRetMetaType();

        //                if (cur.isData && next.isData)
        //                {

        //                }
        //                else if (cur.isEnum && next.isEnum)
        //                {
        //                }
        //                else
        //                {
        //                    var curmc = cur.metaClass;
        //                    var nextmc = next.metaClass;
        //                    if (curmc is MetaGenTemplateClass cmgtc)
        //                    {
        //                        curmc = cmgtc.metaTemplateClass;
        //                    }
        //                    if (nextmc is MetaGenTemplateClass nmgtc)
        //                    {
        //                        nextmc = nmgtc.metaTemplateClass;
        //                    }
        //                    var relation = ClassManager.ValidateClassTypeRelation(curmc, nextmc );
        //                    if (relation == ETypeRelation.Same
        //                        || relation == ETypeRelation.Child)
        //                    {
        //                        mc = nextmc;
        //                    }
        //                    else if (relation == ETypeRelation.Parent)
        //                    {
        //                        mc = curmc;
        //                    }
        //                    else
        //                    {
        //                        isAllSame = false;
        //                        break;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var mt = cmc.GetRetMetaType();
        //                isAllSame = true;
        //            }

        //        }
        //        else 
        //        {
        //            if (cmc.express.opLevel > nmc.express.opLevel)
        //            {
        //                var mt = cmc.GetRetMetaType();
        //            }
        //            else
        //            {
        //                var mt = nmc.GetRetMetaType();
        //            }
        //        }
        //    }
        //    if(isAllSame )
        //    {
        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "??????");
        //    }
        //    return mc;
        //}
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for( int i = 0; i < m_MetaInputParamList.Count; i++ )
            {
                sb.Append(m_MetaInputParamList[i].ToFormatString());
                if( i < m_MetaInputParamList.Count - 1 )
                {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }
    }
    public sealed class MetaInputTemplateCollection
    {
        public bool isTemplateName => m_IsTemplateName;
        public List<MetaType> metaTemplateParamsList => m_MetaTemplateParamsList;


        private List<MetaType> m_MetaTemplateParamsList = new List<MetaType>();
        private bool m_IsTemplateName = false;
        public MetaInputTemplateCollection()
        {
        }
        public List<MetaClass> GetMetaClassList( out bool isAllMetaClass )
        {
            isAllMetaClass = false;
            List<MetaClass> mcList = new List<MetaClass>();
            for (int i = 0; i < m_MetaTemplateParamsList.Count; i++)
            {
                if (m_MetaTemplateParamsList[i].metaClass != null)
                {
                    mcList.Add(m_MetaTemplateParamsList[i].metaClass);
                }
            }
            if( mcList.Count > 0 && mcList.Count == m_MetaTemplateParamsList.Count )
            {
                isAllMetaClass = true;
            }
            return mcList;
        }
        //public MetaInputTemplateCollection(List<FileInputTemplateNode> callNodeList, MetaBlockStatements bms, MetaClass mc )
        //{
        //    for (int i = 0; i < callNodeList.Count; i++)
        //    {
        //        var cnc = callNodeList[i];
        //        string cname = "";
        //        if( cnc.nameList.Count == 1 )
        //        {
        //            cname = cnc.nameList[0];
        //        }

        //        MetaTemplate mgtc = null;
        //        if (mgtc != null)
        //        {
        //            mgtc = mc.GetMetaTemplateByName(cname);
        //            if (mgtc != null)
        //            {
        //            }
        //        }
        //        if (mgtc == null)
        //        {
        //            bms.ownerMetaFunction.GetMetaDefineTemplateByName(cname);
        //        }
        //        if( mgtc == null )
        //        {
        //            //var getmc = ClassManager.instance.GetMetaClassAndRegisterExptendTemplateClassInstance(mc, cnc.defineClassCallLink);

        //            MetaType mp = new MetaType(getmc);
        //            m_MetaTemplateParamsList.Add(mp);
        //        }

        //        //if( mp.isTemplate )
        //        //{
        //        //    m_IsTemplateName = true;
        //        //}
        //    }
        //}
        public void AddMetaTemplateParamsList( MetaType mp )
        {
            m_MetaTemplateParamsList.Add(mp);
        }
        public List<MetaClass> GetMetaClassParamsList()
        {
            List<MetaClass> list = new List<MetaClass>();

            foreach( var v in m_MetaTemplateParamsList )
            {
                if (v.metaClass == null)
                    return null;
                list.Add(v.metaClass);
            }

            return list;
        }
        public MetaClass GetMaxLevelMetaClassType()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            bool isAllSame = true;
            for( int i = 0; i < m_MetaTemplateParamsList.Count -1; i++ )
            {
                MetaType cmdt = m_MetaTemplateParamsList[i];
                MetaType nmdt = m_MetaTemplateParamsList[i + 1];
                if(cmdt == nmdt)
                {
                    isAllSame = true;
                }
                else
                {
                    //if( cmdt.metaTemplate == null && nmdt.metaTemplate == null )
                    //{
                    //    var cmc = cmdt.metaClass;
                    //    var nmc = nmdt.metaClass;
                    //    if (ClassManager.IsNumberMetaClass(cmc) && ClassManager.IsNumberMetaClass(nmc))
                    //    {
                    //        if (i == 0)
                    //        {
                    //            mc = MetaTypeFactory.GetOpLevel(cmc.eType) > MetaTypeFactory.GetOpLevel(nmc.eType) ? cmc : nmc;
                    //        }
                    //        else
                    //        {
                    //            mc = MetaTypeFactory.GetOpLevel(mc.eType) > MetaTypeFactory.GetOpLevel(nmc.eType) ? mc : nmc;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}
                    //else
                    //{
                    //    if(cmdt.metaTemplate == nmdt.metaTemplate )
                    //    {
                    //        isAllSame = true;
                    //    }
                    //}
                }
            }
            if( isAllSame )
            {
                mc = m_MetaTemplateParamsList[0].metaClass;
            }
            return mc;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<");
            for (int i = 0; i < metaTemplateParamsList.Count; i++)
            {

                sb.Append(metaTemplateParamsList[i].ToFormatString());
                if (i < metaTemplateParamsList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(">");
            return sb.ToString();
        }
    }
    public sealed class MetaInputArrayCollection
    {
        public MetaInputArrayCollection( FileMetaBracketTerm fmbt )
        {

        }
    }
}
