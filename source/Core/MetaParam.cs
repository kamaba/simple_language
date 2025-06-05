//****************************************************************************
//  File:      ClassManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaParam
    {
        public string name => m_Name;
        protected string m_Name = "";

        public virtual void Parse()
        {
            
        }
        public virtual void CaleReturnType()
        {
        }
        public virtual string ToTypeName()
        {
            return "";
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }


    public class MetaInputParam : MetaParam
    {
        public MetaExpressNode express => m_Express;

        protected FileInputParamNode m_FileInputParamNode;
        protected MetaExpressNode m_Express = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements;
        protected MetaClass m_OwnerMetaClass = null;
        public MetaInputParam( FileInputParamNode fipn, MetaClass mc, MetaBlockStatements mbs )
        {
            m_FileInputParamNode = fipn;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;

            CreateExpressParam cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaClass = m_OwnerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.objectMetaClass),
                fme = m_FileInputParamNode.express,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.InputParamExpress
            };
            m_Express = ExpressManager.CreateExpressNode(cep);
        }
        public MetaInputParam( MetaExpressNode inputExpress )
        {
            m_Express = inputExpress;
        }
        public override void Parse()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.InputParamExpress } );
            }
        }
        public override void CaleReturnType()
        {
            if(m_Express != null )
            {
                m_Express.CalcReturnType();                
            }
        }
        public MetaClass GetRetMetaClass()
        {
            if( m_Express != null )
            {
                return m_Express.GetReturnMetaClass();
            }
            return CoreMetaClassManager.objectMetaClass;
        }
        public override string ToFormatString()
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
    public class MetaOutputParam : MetaParam
    {
        public MetaExpressNode express;
        public MetaOutputParam(FileInputParamNode fipn, MetaClass mc, MetaBlockStatements mbs)
        {
            //express = ExpressManager.instance.CreateMetaClassByFileMetaClass(mc, mbs, null, fipn.express);
        }
    }
    public class MetaDefineParam : MetaParam
    {
        //public MetaClass ownerMetaClass => m_OwnerMetaFunction != null ? m_OwnerMetaFunction.ownerMetaClass : null;
        //public MetaFunction ownerMetaFunction => m_OwnerMetaFunction;
        public FileMetaParamterDefine fileMetaParamter => m_FileMetaParamter;
        public string metaDefineTypeName => m_MetaDefineTypeName;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaExpressNode expressNode => m_MetaExpressNode;
        public bool isFunctionTemplate => m_IsFunctionTemplate;
        public bool isClassTemplate => m_IsClassTemplate;
        public bool isMust { get { return m_MetaExpressNode == null; } }            //是否为非省略参数
        public bool isExtendParams => m_FileMetaParamter?.paramsToken != null;

        protected bool m_IsFunctionTemplate = false;
        protected bool m_IsClassTemplate = false;
        protected FileMetaParamterDefine m_FileMetaParamter = null;
        protected MetaExpressNode m_MetaExpressNode = null;
        protected MetaVariable m_MetaVariable = null;
        protected MetaFunction m_OwnerMetaFunction = null;
        protected string m_MetaDefineTypeName = "";

        public MetaDefineParam( string _name, MetaFunction mf )
        {
            m_Name = _name;
            m_OwnerMetaFunction = mf;
            m_MetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.Argument,
                null, m_OwnerMetaFunction.ownerMetaClass, null );
        }
        public MetaDefineParam(MetaFunction mf, FileMetaParamterDefine fmp)
        {
            m_OwnerMetaFunction = mf;
            m_FileMetaParamter = fmp;
            m_Name = m_FileMetaParamter.name;

            m_MetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.Argument,
                null, m_OwnerMetaFunction.ownerMetaClass, null );
        }
        public void ParseMetaDefineType()
        {
            string typename = "";
            MetaType mdt = null;
            if ( this.m_FileMetaParamter?.classDefineRef != null)
            {
                typename = m_FileMetaParamter.classDefineRef.name;
                if (m_OwnerMetaFunction != null)
                {
                    m_IsFunctionTemplate = m_OwnerMetaFunction.IsDefineTemplate(typename);
                }
                if(m_OwnerMetaFunction.ownerMetaClass != null )
                {
                    m_IsClassTemplate = m_OwnerMetaFunction.ownerMetaClass.isDefineTemplate(typename);
                }

                mdt = new MetaType(m_FileMetaParamter.classDefineRef, m_OwnerMetaFunction.ownerMetaClass);
            }
            else
            {
                mdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            m_MetaVariable.SetMetaDefineType(mdt);
            if(m_FileMetaParamter != null )
            {
                m_MetaVariable.AddPingToken(m_FileMetaParamter.token);
            }
        }
        public void CreateExpress()
        {
            if (m_FileMetaParamter?.express != null)
            {
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMBS = null,
                    metaType = new MetaType(CoreMetaClassManager.objectMetaClass),
                    fme = m_FileMetaParamter.express,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.InputParamExpress
                };
                m_MetaExpressNode = ExpressManager.CreateExpressNode(cep);
            }
        }
        public override void Parse()
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
                MetaType md = param.metaVariable.metaDefineType;
                if ( !MetaType.EqualMetaDefineType( md, metaVariable.metaDefineType ) )
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        public bool EqualsInputMetaParam(MetaInputParam mip)
        {
            if (m_MetaVariable == null)
            {
                return true;
            }
            var tmc = m_MetaVariable.metaDefineType.metaClass as MetaGenTemplateClass;
            if (tmc != null)
            {
                //return tmc.IsInConstraintMetaClass(mp.ownerMetaClass);
                return true;
            }
            if( mip != null)
            {
                var retMC = mip.GetRetMetaClass();
                var relation = ClassManager.ValidateClassRelationByMetaClass(m_MetaVariable.metaDefineType.metaClass, retMC);

                if (relation == ClassManager.EClassRelation.Same || relation == ClassManager.EClassRelation.Child)
                {
                    return true;
                }
            }

            return false;
        }
        public bool EqualsName( string name )
        {
            return m_MetaVariable.name.Equals(name);
        }
        public void SetMetaType( MetaType mt )
        {
            m_MetaVariable.SetMetaDefineType(mt);
        }       
        public override void CaleReturnType()
        {
            if(m_MetaExpressNode != null )
            {
                m_MetaExpressNode.CalcReturnType();
            }
            //if( !isTemplate )
            {
               // ExpressManager.CalcDefineClassType(ref m_DefineMetaClassType, m_Express, m_OwnerMetaClass, m_OwnerMetaBlockStatements?.ownerMetaFunction, defineName, ref m_IsNeedCastStatements );
            }
            
            if (m_MetaVariable != null)
            {
                //m_MetaVariable.SetRetMetaClass(m_DefineMetaClassType);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_MetaVariable?.ToFormatString());
            return sb.ToString();
        }
    }

    public sealed class MetaDefineParamCollection
    {
        public bool isExtendParams => m_IsExtendParams;
        public int maxParamCount => m_MetaDefineParamList.Count;
        public List<MetaDefineParam> metaDefineParamList => m_MetaDefineParamList;
        public bool isCanCallFunction { get; private set; } = true;
        public bool isAllConst { get; private set; } = false;
        public int minParamCount => m_MinParamCount;
        public bool isHaveDefaultParamExpress { get; set; } = false;

        private bool m_IsExtendParams = false;
        private int m_MinParamCount  = 0;
        private List<MetaDefineParam> m_MetaDefineParamList = new List<MetaDefineParam>();
        public MetaDefineParamCollection()
        {

        }
        public MetaDefineParamCollection(bool _isAllConst, bool _isCanCallFunction)
        { 
            isAllConst = _isAllConst; isCanCallFunction = _isCanCallFunction; 
        }
        public void Clear()
        {
            m_MetaDefineParamList.Clear();
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
                Debug.WriteLine("Error Params 模式下，只允许 使用一个参数，多余参数为无效模式");
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
                    Debug.Write("Error AddMetaDefineParam 参数前边已定义表达式，后边必须跟进默认值表达式!!");
                }
            }
            else
            {
                if (metaMemberParam.expressNode != null)
                {
                    isHaveDefaultParamExpress = true;
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
                //传入值 ，可以与定义值不同，因为使用params 的方式 后边一般跟一个对象数组，或者是类型数组进行限制                
                if(m_MetaDefineParamList.Count == 0 )
                {
                    return false;
                }
                var mdp = m_MetaDefineParamList[m_MetaDefineParamList.Count - 1];
                if( mdp.isExtendParams && mdp.metaVariable.isArray )
                {
                    var mdt = mdp.metaVariable.metaDefineType;

                    for( int i = 0; i < mpc.metaInputParamList.Count; i++ ) 
                    {
                        var mip = mpc.metaInputParamList[i];
                        if( mip.GetRetMetaClass() != mdt.metaClass )
                        {
                            return false;
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
                sb.Append(m_MetaDefineParamList[i].ToTypeName());
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
        public bool fixedParam { get { return minParamCount == maxParamCount; } }
        public int minParamCount => m_MinParamCount;
        public int maxParamCount { get { return m_MetaInputParamList.Count; } }
        public bool isCanCallFunction { get; set; } = true;

        private int m_MinParamCount = 0;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_MetaBlockStatements = null;
        private List<MetaInputParam> m_MetaInputParamList = new List<MetaInputParam>();

        public MetaInputParamCollection(MetaClass mc, MetaBlockStatements mbs)
        {
            m_OwnerMetaClass = mc;
            m_MetaBlockStatements = mbs;
        }
        public MetaInputParamCollection(FileMetaParTerm fmpt, MetaClass mc, MetaBlockStatements mbs)
        {
            m_OwnerMetaClass = mc;
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
                return !a.isMust;      // 必须传参，但没有参数
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
                MetaInputParam mp = new MetaInputParam(splitList[i], m_OwnerMetaClass, m_MetaBlockStatements);
                AddMetaInputParam(mp);
            }
        }
        public void AddMetaInputParam( MetaInputParam mip )
        {
            m_MetaInputParamList.Add(mip);
        }
        public void CaleReturnType()
        {
            for (int i = 0; i < m_MetaInputParamList.Count; i++)
            {
                m_MetaInputParamList[i].Parse();
                m_MetaInputParamList[i].CaleReturnType();
            }
        }
        public MetaClass GetMaxLevelMetaClassType()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            bool isAllSame = true;
            for (int i = 0; i < m_MetaInputParamList.Count - 1; i++)
            {
                MetaInputParam cmc = m_MetaInputParamList[i];
                MetaInputParam nmc = m_MetaInputParamList[i + 1];
                if (mc == null || nmc == null) continue;
                if (cmc.express.opLevel == nmc.express.opLevel)
                {
                    if( cmc.express.opLevel == 10 )
                    {
                        var cur = cmc.GetRetMetaClass();
                        var next = nmc.GetRetMetaClass();
                        var relation = ClassManager.ValidateClassRelationByMetaClass(cur, next);
                        if( relation == ClassManager.EClassRelation.Same 
                            || relation == ClassManager.EClassRelation.Child )
                        {
                            mc = next;
                        }
                        else if( relation == ClassManager.EClassRelation.Parent )
                        {
                            mc = cur;
                        }
                        else
                        {
                            isAllSame = false;
                            break;
                        }
                    }
                    else
                    {
                        mc = cmc.GetRetMetaClass();
                        isAllSame = true;
                    }
                    
                }
                else 
                {
                    if (cmc.express.opLevel > nmc.express.opLevel)
                    {
                        mc = cmc.GetRetMetaClass();
                    }
                    else
                    {
                        mc = nmc.GetRetMetaClass();
                    }
                }
            }
            if(isAllSame )
            {
                Debug.Write("全都相似");
            }
            return mc;
        }
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
        public MetaInputTemplateCollection(List<FileInputTemplateNode> callNodeList, MetaClass mc )
        {
            for (int i = 0; i < callNodeList.Count; i++)
            {
                MetaType mp = new MetaType( callNodeList[i], mc );
                m_MetaTemplateParamsList.Add(mp);
                //if( mp.isTemplate )
                //{
                //    m_IsTemplateName = true;
                //}
            }
        }
        public void AddMetaTemplateParamsList( MetaType mp )
        {
            m_MetaTemplateParamsList.Add(mp);
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
