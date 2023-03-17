using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaParam
    {
        public bool isAllConst
        {
            get
            {
                if(m_ParentCollection != null)
                {
                    return m_ParentCollection.isAllConst;
                }
                return false;
            }
        }
        public bool isCanCallFunction
        {
            get
            {
                if (m_ParentCollection != null)
                {
                    return m_ParentCollection.isCanCallFunction;
                }
                return true;
            }
        }
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;

        protected MetaClass m_OwnerMetaClass;
        protected MetaBlockStatements m_OwnerMetaBlockStatements;
        protected MetaParamCollectionBase m_ParentCollection;

        public virtual void Parse()
        {
            
        }
        public virtual void CaleReturnType()
        {
        }
        public void SetParentCollection( MetaParamCollectionBase cbase )
        {
            m_ParentCollection = cbase;
        }
        public virtual string ToTypeName()
        {
            //if(m_ReturnMetaType != null )
            //{
            //    return m_ReturnMetaType.defineType.ToString();
            //}
            return "";
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
    public partial class MetaInputParam : MetaParam
    {
        public MetaExpressNode express => m_Express;

        private FileInputParamNode m_FileInputParamNode;
        private MetaExpressNode m_Express = null;
        public MetaInputParam( FileInputParamNode fipn, MetaClass mc, MetaBlockStatements mbs )
        {
            m_FileInputParamNode = fipn;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;

            m_Express = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaBlockStatements, new MetaType(CoreMetaClassManager.objectMetaClass), m_FileInputParamNode.express, false, false );
        }
        public MetaInputParam( MetaExpressNode inputExpress )
        {
            m_Express = inputExpress;
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
    public partial class MetaDefineParam : MetaParam
    {
        public FileMetaParamterDefine fileMetaParamter => m_FileMetaParamter;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaExpressNode expressNode => m_MetaExpressNode;
        public bool isTemplate
        {
            get
            {
                return m_MetaVariable != null ? m_MetaVariable.metaDefineType.isTemplate : false;
            }
        }
        public bool isMust { get { return m_MetaExpressNode != null; } }            //是否为非省略参数
        public bool isTemplateMetaClass
        {
            get
            {
                return m_MetaVariable != null ? m_MetaVariable.metaDefineType.isGenTemplateClass : false;
            }
        }


        private FileMetaParamterDefine m_FileMetaParamter = null;
        private MetaVariable m_MetaVariable = null;
        private MetaExpressNode m_MetaExpressNode = null;
        public static bool operator ==(MetaDefineParam lmm, MetaDefineParam rmm)
        {
            return MetaDefineParam.Equals(lmm, rmm);
        }
        public static bool operator !=(MetaDefineParam lmm, MetaDefineParam rmm)
        {
            return !MetaDefineParam.Equals(lmm, rmm);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool EqualDefineMetaParam(MetaParam param)
        {
            MetaDefineParam mdp = (param as MetaDefineParam);
            if (mdp != null)
            {
                MetaType md = mdp.metaVariable.metaDefineType;
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
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;


            MetaDefineParam rec = obj as MetaDefineParam;
            if (rec == null) return false;

            //if (rec.m_DefineMetaClassType == null || m_DefineMetaClassType == null ) return false;

            //if (rec.m_DefineMetaClassType == m_DefineMetaClassType && rec.m_DefineMetaClassType.name == m_DefineMetaClassType.name)
            //    return true;

            return false;
        }
        public MetaDefineParam( MetaClass mc, MetaBlockStatements mbs, FileMetaParamterDefine fmp )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaParamter = fmp;
            MetaType mdt = null;
            if (m_FileMetaParamter.classDefineRef != null )
            {
                mdt = new MetaType(fmp.classDefineRef, mc);
            }
            else
            {
                mdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            m_MetaVariable = new MetaVariable(fmp.name, mbs, mc, mdt );
            m_MetaVariable.isArgument = true;

            if (m_FileMetaParamter.express != null)
            {
                m_MetaExpressNode = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(null, new MetaType(CoreMetaClassManager.objectMetaClass), m_FileMetaParamter.express);

            }
        }
        public MetaDefineParam(string _name, MetaClass ownerMC, MetaBlockStatements mbs, MetaType mt )
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            MetaType mdt = new MetaType(mt);
            m_MetaVariable = new MetaVariable(_name, mbs, ownerMC, mdt);
            m_MetaVariable.isArgument = true;
        }
        public MetaDefineParam( string _name, MetaClass ownerMC, MetaBlockStatements mbs, MetaClass _defineMetaClass, MetaExpressNode _expressNode )
        {
            MetaType mdt = new MetaType(_defineMetaClass);
            m_MetaVariable = new MetaVariable(_name, mbs, ownerMC, mdt );
            m_MetaExpressNode = _expressNode;
        }
        public MetaDefineParam( string _name, MetaClass ownerMC, MetaBlockStatements mbs, MetaTemplate mt )
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            MetaType mdt = new MetaType(mt);
            m_MetaVariable = new MetaVariable(_name, mbs, ownerMC, mdt);
            m_MetaVariable.isArgument = true;
        }
        public override void Parse()
        {            
            if(m_MetaExpressNode != null )
            {
                AllowUseConst auc = new AllowUseConst();
                auc.useNotConst = false;
                auc.useNotStatic = false;
                auc.callConstructFunction = true;
                auc.callFunction = true;
                m_MetaExpressNode.Parse(auc);
            }
        }
        public void SetOwnerMetaClass(MetaClass mc) { m_OwnerMetaClass = mc; }
        public void SetOwnerMetaBlockStatements(MetaBlockStatements mbs) { m_OwnerMetaBlockStatements = mbs; }
        public void SetMetaType( MetaType mt )
        {
            m_MetaVariable.SetMetaDefineType(mt);
        }
        public void SetMetaVariable( MetaVariable mv )
        {
            m_MetaVariable = mv;
        }
        public MetaExpressNode CreateExpressNodeInFunctionDefineParam()
        {
            //m_Express = null;// ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(ownerMetaClass, m_DefineMetaClassType, m_FileMetaParamter.express);
            return null;
        }
        public override void CaleReturnType()
        {
            //if(m_Express != null )
            //{
            //    m_Express.ParseExpress();                
            //    m_Express.CalcReturnType();
            //}
            //if( !isTemplate )
            //{
            //    ExpressManager.CalcDefineClassType(ref m_DefineMetaClassType, m_Express, m_OwnerMetaClass, m_OwnerMetaBlockStatements?.ownerMetaFunction, defineName, ref m_IsNeedCastStatements );
            //}

            //if (m_MetaVariable != null)
            //{
            //    m_MetaVariable.SetRetMetaClass(m_DefineMetaClassType);
            //}
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_MetaVariable?.ToFormatString());
            return sb.ToString();
        }
    }
    public class MetaParamCollectionBase
    {
        public bool fixedParam { get { return minParamCount == maxParamCount; } }
        public int minParamCount = 0;
        public int maxParamCount { get { return metaParamList.Count; } }
        public bool isAllConst = false;
        public bool isCanCallFunction = true;
        public List<MetaParam> metaParamList = new List<MetaParam>();
        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_MetaBlockStatements = null;
        
        public int count { get { return metaParamList.Count; } }
        public bool isNullParam
        {
            get
            {
                return metaParamList.Count == 0;
            }
        }
        public void Clear()
        {
            metaParamList.Clear();
        }
        public bool IsEqualMetaTemplateAndParamCollection( MetaInputTemplateCollection mitc, MetaParamCollectionBase mpc )
        {
            if (mpc == null)
            {
                return metaParamList.Count == 0;
            }

            int templateCount = 0;
            if( mitc!=null )
            {
                templateCount = mitc.metaTemplateParamsList.Count;
            }
            if (metaParamList.Count == mpc.metaParamList.Count + templateCount)
            {
                int index = 0;
                if( mitc != null )
                {
                    for (int i = 0; i < mitc.metaTemplateParamsList.Count; i++)
                    {
                        MetaDefineParam a = metaParamList[index++] as MetaDefineParam;
                        MetaType b = mitc.metaTemplateParamsList[i];
                        if (!a.isTemplateMetaClass )
                        {
                            return false;
                        }
                    }
                }
                for (int i = 0; i < mpc.metaParamList.Count; i++)
                {
                    MetaDefineParam a = metaParamList[index++] as MetaDefineParam;
                    MetaInputParam b = mpc.metaParamList[i] as MetaInputParam;
                    if (!CheckInputMetaParam(a, b))
                        return false;
                }
                return true;
            }
            return false;
        }
        public virtual bool CheckInputMetaParam( MetaDefineParam a, MetaInputParam b)
        {
            if( b == null )
            {
                return !a.isMust;      // 必须传参，但没有参数
            }
            if (a.EqualsInputMetaParam(b))
                return true;
            return false;
        }

        public virtual bool CheckDefineMetaParam(MetaParam a, MetaParam b)
        {
            if (a is MetaDefineParam)
            {
                var amd = a as MetaDefineParam;
                if (amd.EqualDefineMetaParam(b))
                    return true;

            }
            return a == b;
        }

        public virtual void CheckParse()
        {

        }

        public static bool operator ==(MetaParamCollectionBase lmm, MetaParamCollectionBase rmm)
        {
            return System.Object.Equals(lmm, rmm);
        }
        public static bool operator !=(MetaParamCollectionBase lmm, MetaParamCollectionBase rmm)
        {
            return !System.Object.Equals(lmm, rmm);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            MetaParamCollectionBase rec = obj as MetaParamCollectionBase;
            if (rec == null) return false;

            int count = rec.metaParamList.Count;
            for (int i = 0; i < count; i++)
            {
                if (this.metaParamList[i] != rec.metaParamList[i])
                {
                    return false;
                }
            }
            return true;
        }
        public void AddMetaParam( MetaParam metaMemberParam )
        {
            metaMemberParam.SetParentCollection( this );
            metaParamList.Add(metaMemberParam);
        }
        public string ToParamTypeName()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < metaParamList.Count; i++)
            {
                sb.Append(metaParamList[i].ToTypeName());
                if (i < metaParamList.Count - 1)
                    sb.Append("_");
            }
            return sb.ToString();
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
    public class MetaDefineParamCollection : MetaParamCollectionBase
    {
        public bool isHaveDefaultParamExpress { get; set; } = false;
        public MetaDefineParamCollection()
        {

        }
        public MetaDefineParamCollection(bool _isAllConst, bool _isCanCallFunction)
        { 
            isAllConst = _isAllConst; isCanCallFunction = _isCanCallFunction; 
        }
        public List<MetaDefineParam> GetMetaDefineList()
        {
            List<MetaDefineParam> retList = new List<MetaDefineParam>();
            for (int i = 0; i < metaParamList.Count; i++ )
            {
                retList.Add(metaParamList[i] as MetaDefineParam);
            }
            return retList;
        }
        public MetaDefineParam GetMetaDefineParamByName( string name )
        {
            for (int i = 0; i < metaParamList.Count; i++)
            {
                var dParam = metaParamList[i] as MetaDefineParam;
                if( dParam != null )
                {
                    if (dParam.EqualsName(name))
                        return dParam;
                }
            }
            return null;
        }
        public void AddMetaDefineParam(MetaDefineParam metaMemberParam)
        {
            metaMemberParam.SetParentCollection(this);
            metaParamList.Add(metaMemberParam);

            if(isHaveDefaultParamExpress)
            {
                if (metaMemberParam.expressNode == null)
                {
                    Console.WriteLine("Error AddMetaDefineParam 参数前边已定义表达式，后边必须跟进默认值表达式!!");
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
                    minParamCount++;
                }
            }
        }
        public bool IsEqualMetaInputParamCollection(MetaInputParamCollection mpc)
        {
            int inputCount = 0;
            if( mpc != null )
            {
                inputCount = mpc.metaParamList.Count;
            }
            if (metaParamList.Count >= inputCount )
            {
                for (int i = 0; i < metaParamList.Count; i++)
                {
                    MetaDefineParam a = metaParamList[i] as MetaDefineParam;
                    if (a == null)
                        return false;
                    MetaInputParam b = null;
                    if( mpc != null && i < inputCount )
                    {
                        b = mpc.metaParamList[i] as MetaInputParam;
                    }
                    if (!CheckInputMetaParam(a, b))
                        return false;
                }
                return true;
            }
            return false;
        }
        public bool IsEqualMetaDefineParamCollection(MetaDefineParamCollection mdpc)
        {
            if (mdpc == null)
            {
                return minParamCount == 0;
            }

            if (metaParamList.Count == mdpc.metaParamList.Count)
            {
                for (int i = 0; i < metaParamList.Count; i++)
                {
                    MetaParam a = metaParamList[i];
                    MetaParam b = mdpc.metaParamList[i];
                    if (!CheckDefineMetaParam(a, b))
                        return false;
                }
                return true;
            }
            return false;
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("(");
            for (int i = 0; i < metaParamList.Count; i++)
            {
                sb.Append(metaParamList[i].ToFormatString());
                if (i < metaParamList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
    public class MetaDefineTemplateCollection
    {
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();

        public int count { get { return m_MetaTemplateList.Count; } }
       
        public MetaTemplate GetMetaDefineTemplateByName( string _name )
        {
            for( int i = 0; i < m_MetaTemplateList.Count; i++ )
            {
                if (m_MetaTemplateList[i].name == _name)
                    return m_MetaTemplateList[i];
            }
            return null;
        }
        public bool IsEqualMetaInputTemplateCollection(MetaInputTemplateCollection mpc)
        {
            if (mpc == null)
            {
                return m_MetaTemplateList.Count == 0;
            }

            if (m_MetaTemplateList.Count == mpc.metaTemplateParamsList.Count)
            {
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    MetaTemplate a = m_MetaTemplateList[i];
                    MetaType b = mpc.metaTemplateParamsList[i];
                    if ( MatchMetaInputTemplate(a, b))
                        return true;
                }
            }
            return false;
        }
        public virtual bool MatchMetaInputTemplate(MetaTemplate a, MetaType b)
        {
            if (a.IsInConstraintMetaClass(b.metaClass) )
                return true;
            return false;
        }
        public virtual void AddMetaDefineTemplate(MetaTemplate defineTemplate)
        {
            m_MetaTemplateList.Add(defineTemplate);
        }
        public virtual string ToFormatString()
        {
            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < metaParamList.Count; i++)
            //    {
            //        sb.Append(metaParamList[i].ToTypeName());
            //        if (i < metaParamList.Count - 1)
            //            sb.Append("_");
            //    }
            //    return sb.ToString();
            return "";
        }
    }
    public partial class MetaInputParamCollection : MetaParamCollectionBase
    {
        public bool isStatic = false;
        public bool isNeedAllConst = false;

        public MetaInputParamCollection(MetaClass mc, MetaBlockStatements mbs)
        {
            m_OwnerMetaClass = mc;
            m_MetaBlockStatements = mbs;
        }
        public MetaInputParamCollection(FileMetaParTerm fmpt, MetaClass mc, MetaBlockStatements mbs )
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
            ParseList( list );
        }
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
            AddMetaParam(mip);
        }
        public void CaleReturnType()
        {
            for (int i = 0; i < metaParamList.Count; i++)
            {
                metaParamList[i].CaleReturnType();
            }
        }
        public MetaClass GetMaxLevelMetaClassType()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            bool isAllSame = true;
            for (int i = 0; i < metaParamList.Count - 1; i++)
            {
                MetaInputParam cmc = (metaParamList[i] as MetaInputParam);
                MetaInputParam nmc = (metaParamList[i + 1] as MetaInputParam);
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
            return mc;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("(");
            for( int i = 0; i < metaParamList.Count; i++ )
            {
                sb.Append(metaParamList[i].ToFormatString());
                if( i < metaParamList.Count - 1 )
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
    public class MetaInputTemplateCollection
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
                if( mp.isTemplate )
                {
                    m_IsTemplateName = true;
                }
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
                    if( cmdt.metaTemplate == null && nmdt.metaTemplate == null )
                    {
                        var cmc = cmdt.metaClass;
                        var nmc = nmdt.metaClass;
                        if (ClassManager.IsNumberMetaClass(cmc) && ClassManager.IsNumberMetaClass(nmc))
                        {
                            if (i == 0)
                            {
                                mc = MetaTypeFactory.GetOpLevel(cmc.eType) > MetaTypeFactory.GetOpLevel(nmc.eType) ? cmc : nmc;
                            }
                            else
                            {
                                mc = MetaTypeFactory.GetOpLevel(mc.eType) > MetaTypeFactory.GetOpLevel(nmc.eType) ? mc : nmc;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if(cmdt.metaTemplate == nmdt.metaTemplate )
                        {
                            isAllSame = true;
                        }
                    }
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

    public class MetaInputArrayCollection
    {
        public MetaInputArrayCollection( FileMetaBracketTerm fmbt )
        {

        }
    }
}
