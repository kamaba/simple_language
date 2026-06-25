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

        protected FileInputParamNode m_FileInputParamNode;
        protected MetaExpressNodeBase m_Express = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements;
        protected MetaBase m_OwnerMetaBase = null;
        protected Token m_Token;
        public MetaInputParam( FileInputParamNode fipn, MetaBase mc, MetaBlockStatements mbs )
        {
            m_FileInputParamNode = fipn;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = mc;

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
        public virtual void Parse(AllowUseSettings allowUse )
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings(allowUse) { parseFrom = EParseFrom.InputParamExpress, ifNotVariableThenAddVariable = false } );
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
        public bool isMust { get { return m_MetaExpressNode == null; } }           
        public bool isExtendParams => m_FileMetaParamter?.paramsToken != null;
        public bool isHasExpress => m_IsHasExpress;

        protected bool m_IsFunctionTemplate = false;
        protected FileMetaParamterDefine m_FileMetaParamter = null;
        protected MetaExpressNodeBase m_MetaExpressNode = null;
        protected MetaVariable m_MetaVariable = null;
        protected MetaFunction m_OwnerMetaFunction = null;
        protected string m_Name = "";
        protected Token m_Token = null;
        protected bool m_IsHasExpress = false;

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
            m_IsHasExpress = m_FileMetaParamter.express != null;
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
                MetaType md = param.metaVariable.defineMetaType;
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
                return false;
            }
            return false;
        }
        public bool EqualsInputMetaParam(MetaInputParam mip)
        {
            if (m_MetaVariable == null) return false;
            
           var declaredMt = m_MetaVariable.GetFinalMetaType();
            var argMt = mip.express != null ? mip.express.GetReturnMetaType() : null;
            if (declaredMt == null || argMt == null) return false;

            return TypeManager.CompareFunctionDefineMetaTypeAndInputMetaType(declaredMt, argMt, mip.token);
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
                if (metaMemberParam.expressNode == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error AddMetaDefineParam ???????????????????????????????????!!");
                }
            }
            else
            {
                if (metaMemberParam.expressNode != null)
                {
                    m_IsHaveDefaultParamExpress = true;
                }      
                else
                {
                    m_MinParamCount++;
                }
            }
        }
        public bool IsEqualMetaInputParamCollection(MetaInputParamCollection mpc)
        {
            int inputCount = 0;
            if( mpc != null )
            {
                inputCount = mpc.metaInputParamList.Count;
            }
            if ( m_IsExtendParams )
            {
                //??????????????????????????params ?????????????????????????????????????????                
                if(m_MetaDefineParamList.Count == 0 )
                {
                    return false;
                }
                var lastMdp = m_MetaDefineParamList[m_MetaDefineParamList.Count - 1];
                if(lastMdp.isExtendParams && lastMdp.metaVariable.isArray )
                {
                    var mdt = lastMdp.metaVariable.isDefineMetaType ? lastMdp.metaVariable.defineMetaType : lastMdp.metaVariable.realMetaType;
                    for( int i = 0; i < m_MetaDefineParamList.Count - 1; i++ ) 
                    {
                        var mdp_metaType = m_MetaDefineParamList[i].metaVariable.GetFinalMetaType();
                        var mip = mpc.metaInputParamList[i];
                        var retmt = mip.GetRetMetaType();

                        if (retmt.isData)
                        { 
                        }
                        else if( retmt.isEnum )
                        {

                        }
                        else
                        {
                            var retmc = retmt.metaClass;
                            if (retmc is MetaGenTemplateClass mgtc)
                            {
                                retmc = mgtc.metaTemplateClass;
                            }
                            if (retmc != mdp_metaType.metaClass)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }

                return false;
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
        public bool IsEqualMetaDefineParamCollection(MetaDefineParamCollection mdpc)
        {
            if (mdpc == null)
            {
                return minParamCount == 0;
            }

            if (m_MetaDefineParamList.Count == mdpc.m_MetaDefineParamList.Count)
            {
                if(m_MetaDefineParamList.Count == 0 )
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
            List<FileInputParamNode> list = new List<FileInputParamNode>();
            for (int i = 0; i < splitList.Count; i++)
            {
                FileInputParamNode fnpn = new FileInputParamNode(splitList[i]);
                list.Add(fnpn);
            }
            ParseList(list);
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
