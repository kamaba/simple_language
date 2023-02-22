
using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;

namespace SimpleLanguage.Core
{
    public partial class MetaMemberFunction : MetaFunction
    {
        public override string functionAllName
        {
            get
            {
                if(string.IsNullOrEmpty( m_FunctionAllName ) )
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(name);
                    if( m_ParamCount > 0 )
                    {
                        sb.Append("_");
                        sb.Append(m_ParamCount.ToString() );
                        sb.Append("_");
                        sb.Append(m_MetaMemberParamCollection.ToParamTypeName() );
                        sb.Append("_");
                        sb.Append(GetHashCode().ToString());
                    }
                    m_FunctionAllName = sb.ToString();
                }
                return m_FunctionAllName;
            }
        }

        public bool isWithInterface { get; set; } = false;
        public bool isOverrideFunction { get; set; } = false;
        public bool isConstructInitFunction => m_ConstructInitFunction;
        public bool isGet { get; set; } = false;
        public bool isSet { get; set; } = false;
        public bool isFinal { get; set; } = false;
        public bool isCanRewrite { get; set; } = false;
        public bool isTemplateInParam { get; set; } = false;
        public bool isCastFunction
        {
            get
            {
                return m_Name == "Cast";
            }
        }

        private string m_FunctionAllName = null;
        private bool m_ConstructInitFunction = false;
        private FileMetaMemberFunction m_FileMetaMemberFunction = null;

        public MetaMemberFunction( MetaClass mc, FileMetaMemberFunction fmmf):base( mc )
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(true, false);
            m_FileMetaMemberFunction = fmmf;
            m_Name = fmmf.name;

            isStatic = fmmf.staticToken != null;
            isGet = fmmf.getToken != null;
            isSet = fmmf.setToken != null;
            isFinal = fmmf.finalToken != null;
            if ( fmmf.virtualOverrideToken != null )
            {
                if (fmmf.virtualOverrideToken.type == ETokenType.Override)
                    isOverrideFunction = true;
            }
            if( fmmf.interfaceToken != null )
            {
                isWithInterface = true;
            }

            m_ParamCount = fmmf.metaParamtersList.Count;
            for (int i = 0; i < m_ParamCount; i++)
            {
                var param = fmmf.metaParamtersList[i];
                MetaDefineParam mmp = new MetaDefineParam(mc, null, param);
                m_MetaMemberParamCollection.AddMetaDefineParam(mmp);
            }
            m_TemplateCount = fmmf.metaTemplatesList.Count;
            for( int i = 0; i < m_TemplateCount; i++ )
            {
                var template = fmmf.metaTemplatesList[i];

                MetaTemplate mdt = new MetaTemplate( ownerMetaClass, template );
                AddMetaDefineTemplate(mdt);
                for( int j = 0; j < template.inClassNameTokenList.Count; j++)
                {
                    var inClassToken = template.inClassNameTokenList[j];
                    MetaClass gmc = ClassManager.instance.GetMetaClassByListString( ownerMetaClass, inClassToken.nameList);
                    if( gmc == null )
                    {
                        Console.WriteLine("Error 没有查找到inClass的类名, " + inClassToken.ToFormatString());
                        continue;
                    }
                    mdt.AddInConstraintMetaClass(gmc);
                }
            }
            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;
            Init();
        }
        public MetaMemberFunction(MetaMemberFunction mmf) : base( mmf )
        {

        }
        public MetaMemberFunction( MetaClass mc, string _name ) : base( mc )
        {
            m_Name = _name;
            isCanRewrite = true;
            m_ParamCount = 0;
            m_MetaMemberParamCollection.Clear();

            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;

            Init();
        }
       
        void Init()
        {
            m_ConstructInitFunction = name == "__Init__";
            if (m_ConstructInitFunction)
            {             
                m_DefineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            else
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            if( isGet )
            {
                m_IsMustNeedReturnStatements = true;
            }
            if( !isStatic )
            {
                m_ThisMetaVariable = new MetaVariable( "this_" + GetHashCode().ToString(), null, m_OwnerMetaClass, new MetaType( m_OwnerMetaClass ) );
            }
            m_ReturnMetaVariable = new MetaVariable("return_" + GetHashCode().ToString(), null, m_OwnerMetaClass, m_DefineMetaType);
        }
        public bool IsEqualWithMMFByNameAndParam( MetaMemberFunction mmf )
        {
            if (mmf.name != m_Name) return false;

            if( !m_MetaMemberParamCollection.IsEqualMetaParamCollection( mmf.metaMemberParamCollection ) )
            {
                return false;
            }

            return true;
        }
        public void AddMetaDefineParam( MetaDefineParam mdp )
        {
            m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
            if( mdp.metaVariable.isTemplate )
            {
                isTemplateInParam = true;
            }
        }
        public void AddMetaDefineTemplate (MetaTemplate mt )
        {
            m_MetaMemberTemplateCollection.AddMetaDefineTemplate(mt);
        }
        public override void Parse()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaParamList.Count; i++)
            {
                m_MetaMemberParamCollection.metaParamList[i].Parse();
            }
            if ( m_FileMetaMemberFunction != null )
            {
                if(m_FileMetaMemberFunction.returnMetaClass != null )
                {
                    if(m_ConstructInitFunction)
                    {
                        Console.WriteLine("Error 当前类:" + allName + " 是构建Init类，不允许有返回类型 ");
                    }
                    else
                    {
                        var mdt = new MetaType(m_FileMetaMemberFunction.returnMetaClass, ownerMetaClass);

                        if (mdt == null )
                        {
                            Console.WriteLine("Error 定义的返回类型，没有找到相对应的类型！！");
                        }
                        else
                        {
                            m_DefineMetaType = mdt;
                        }
                    }
                }
            }
        }
        public bool ParseExpress()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaParamList.Count; i++)
            {
                m_MetaMemberParamCollection.metaParamList[i].CaleReturnType();
            }
            return true;
        }
        public void ParseStatements()
        {
            if( m_FileMetaMemberFunction != null )
            {
                if (m_FileMetaMemberFunction.fileMetaBlockSyntax != null)
                {
                    Token beginToken = m_FileMetaMemberFunction.fileMetaBlockSyntax.beginBlock;
                    Token endToken = m_FileMetaMemberFunction.fileMetaBlockSyntax.endBlock;
                    m_MetaBlockStatements.SetFileMetaBlockSyntax(m_FileMetaMemberFunction.fileMetaBlockSyntax);
                    m_MetaBlockStatements.SetMetaMemberParamCollection(m_MetaMemberParamCollection);
                    CreateMetaSyntax(m_FileMetaMemberFunction.fileMetaBlockSyntax, m_MetaBlockStatements);
                }
                else
                {
                    Console.WriteLine("Error 该函数没有定义内容！！");
                }
            }
            m_MetaBlockStatements.SetDeep(deep);
        }
        public void Check()
        {
            m_MetaMemberParamCollection.CheckParse();
        }
        public static MetaStatements CreateMetaSyntax( FileMetaSyntax rootMs, MetaBlockStatements currentBlockStatements)
        {    
            MetaStatements beforeStatements = currentBlockStatements;            
            while (rootMs.IsNotEnd() )
            {
                var childFms = rootMs.GetCurrentSyntaxAndMove();
                HandleMetaSyntax(currentBlockStatements, ref beforeStatements,  rootMs, childFms );
            }
            return beforeStatements;
        }
        public static MetaStatements HandleMetaSyntax(MetaBlockStatements currentBlockStatements, ref MetaStatements beforeStatements, 
            FileMetaSyntax fms, FileMetaSyntax childFms )
        {
            switch (childFms)
            {
                case FileMetaBlockSyntax fmbs1:
                    {
                        var createBlockStatements = new MetaBlockStatements(currentBlockStatements, fmbs1);
                        createBlockStatements.parent = currentBlockStatements;
                        var cms = CreateMetaSyntax(fmbs1, createBlockStatements);
                        beforeStatements.SetNextStatements(createBlockStatements);
                        beforeStatements = createBlockStatements;
                    }
                    break;
                case FileMetaKeyIfSyntax fmkis:
                    {
                        var metaIfStatements = new MetaIfStatements(currentBlockStatements, fmkis);
                        beforeStatements.SetNextStatements( metaIfStatements );
                        beforeStatements = metaIfStatements;
                    }
                    break;
                case FileMetaKeySwitchSyntax fmkss:
                    {
                        var metaSwitchStatements = new MetaSwitchStatements(currentBlockStatements, fmkss);
                        beforeStatements.SetNextStatements( metaSwitchStatements );
                        beforeStatements = metaSwitchStatements;
                    }
                    break;
                case FileMetaKeyForSyntax fmkfs:
                    {
                        var metaForStatements = new MetaForStatements(currentBlockStatements, fmkfs );
                        beforeStatements.SetNextStatements( metaForStatements );
                        beforeStatements = metaForStatements;
                    }
                    break;
                case FileMetaConditionExpressSyntax fmkes:
                    {     
                        if (fmkes.token.type == ETokenType.While
                            || fmkes.token.type == ETokenType.DoWhile )
                        {
                            var metaWhileStatements = new MetaWhileDoWhileStatements(currentBlockStatements, fmkes);
                            beforeStatements.SetNextStatements( metaWhileStatements );
                            beforeStatements = metaWhileStatements;
                        }
                        else
                        {
                            Console.WriteLine("Error FileMetaConditionExpressSyntax: 暂不支持该类型的解析!!");
                        }
                    }
                    break;
                case FileMetaKeyOnlySyntax fmoks:
                    {
                        if (fmoks.token.type == ETokenType.Break)
                        {
                            var metaBreakStatements = new MetaBreakStatements(currentBlockStatements, fmoks);
                            beforeStatements.SetNextStatements(metaBreakStatements);
                            beforeStatements = metaBreakStatements;
                        }
                        else if (fmoks.token.type == ETokenType.Continue)
                        {
                            var metaContinueStatements = new MetaContinueStatements(currentBlockStatements, fmoks);
                            beforeStatements.SetNextStatements(metaContinueStatements);
                            beforeStatements = metaContinueStatements;
                        }
                    }
                    break;
                case FileMetaOpAssignSyntax fmos:
                    {
                        bool isNewStatements = false;
                        if (fmos.variableRef.isOnlyName)
                        {
                            string name1 = fmos.variableRef.name;
                            if (!currentBlockStatements.GetIsMetaVariable(name1))
                            {
                                var ownerclass = currentBlockStatements.ownerMetaClass;
                                MetaBase mb = ownerclass.GetMetaMemberVariableByName(name1);
                                if( mb != null )
                                {
                                    Console.WriteLine("Error 如果是使用类成员，必须使用this.变量的方式" + fmos.variableRef.ToTokenString() );
                                }
                                else
                                {
                                    isNewStatements = true;
                                }
                            }
                        }
                        if (isNewStatements)
                        {
                            //if (currentBlockStatements.ownerMetaFunction?.isConstructFunction)
                            //{
                            //    Console.WriteLine("Error 构造函数中，不允许使用定义字段，必须使用this.非静态或者是类名.静态字段赋值!" + fmos.variableRef.ToTokenString());
                            //}
                            MetaNewStatements mnvs11 = new MetaNewStatements( currentBlockStatements, fmos );
                            beforeStatements.SetNextStatements(mnvs11);
                            beforeStatements = mnvs11;
                        }
                        else
                        {
                            MetaAssignStatements mas = new MetaAssignStatements( currentBlockStatements, fmos );
                            beforeStatements.SetNextStatements(mas);
                            beforeStatements = mas;
                        }
                    }
                    break;
                case FileMetaDefineVariableSyntax fmvs:
                    {
                        bool isNewStatements = false;
                        string name1 = fmvs.name;
                        if (currentBlockStatements.GetIsMetaVariable(name1))
                        {
                            isNewStatements = true;
                            Console.WriteLine("Error 定义变量名称与类函数临时名称一样!!" + fmvs.token?.ToLexemeAllString());                            
                        }
                        else
                        {
                            isNewStatements = currentBlockStatements.ownerMetaClass.GetMetaMemberVariableByName(name1) == null;
                            if (!isNewStatements)
                            {
                                Console.WriteLine("Error 定义变量名称与类定义名称一样!!" + fmvs.token?.ToLexemeAllString());
                            }
                        }
                        if ( isNewStatements )
                        {
                            MetaNewStatements mnvs11 = new MetaNewStatements(currentBlockStatements, fmvs);                           
                            beforeStatements.SetNextStatements(mnvs11);
                            beforeStatements = mnvs11;
                        }
                    }
                    break;
                case FileMetaCallSyntax fmcs:
                    {
                        MetaCallStatements mas = new MetaCallStatements(currentBlockStatements, fmcs);
                        beforeStatements.SetNextStatements(mas);
                        beforeStatements = mas;
                        return mas;
                    }
                case FileMetaKeyReturnSyntax fmrs:
                    {
                        if( fmrs.token?.type == ETokenType.Return )
                        {
                            MetaReturnStatements mrs = new MetaReturnStatements(currentBlockStatements, fmrs);
                            beforeStatements.SetNextStatements(mrs);
                            beforeStatements = mrs;
                            return mrs;
                        }
                        else if( fmrs.token?.type == ETokenType.Transience )
                        {
                            MetaTRStatements mtrs = new MetaTRStatements(currentBlockStatements, fmrs);
                            beforeStatements.SetNextStatements(mtrs);
                            beforeStatements = mtrs;
                            return mtrs;
                        }
                        else
                        {
                            Console.WriteLine("Error 生成MetaStatements出错KeyReturnSyntax类型错误!!");
                        }
                    }
                    break;
                case FileMetaKeyGotoLabelSyntax fmkgls:
                    {
                        var metaGotoStatements = new MetaGotoLabelStatements(currentBlockStatements, fmkgls);
                        beforeStatements.SetNextStatements(metaGotoStatements);
                        beforeStatements = metaGotoStatements;
                        return metaGotoStatements;
                    }
                default:
                    Console.WriteLine("Waning 还有没有解析的语句!! MetaMemberFunction 314");
                    break;
            }
            return null;
        }
        public static bool operator ==(MetaMemberFunction lmm, MetaMemberFunction rmm)
        {
            return System.Object.Equals(lmm, rmm);
        }
        public static bool operator !=(MetaMemberFunction lmm, MetaMemberFunction rmm)
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

            MetaMemberFunction rec = obj as MetaMemberFunction;
            if (rec == null) return false;

            if (rec.name.Equals(name) && rec.metaMemberParamCollection.Equals(metaMemberParamCollection))
                return true;
           
            return false;
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);

            sb.Append(permission.ToFormatString() + " ");
            if (isStatic)
            {
                sb.Append(" static");
            }
            if (isOverrideFunction)
            {
                sb.Append(" override");
            }
            if (isWithInterface)
            {
                sb.Append(" interface");
            }
            sb.Append(" ");
            sb.Append( m_DefineMetaType.ToFormatString() );
            sb.Append(" " + name );
            sb.Append(m_MetaMemberParamCollection.ToFormatString());
            sb.Append(Environment.NewLine);

            if(m_MetaBlockStatements != null )
                sb.Append(m_MetaBlockStatements.ToFormatString());

            return sb.ToString();
        }
    }
    public class MetaMemberFunctionCollection : MetaBase
    {
        public List<MetaMemberFunction> functionList = new List<MetaMemberFunction>();

        public MetaMemberFunctionCollection( string _name)
        {
            m_Name = _name;
        }
        public void AddFunction( MetaMemberFunction mmf )
        {
            functionList.Add(mmf);

            AddMetaBase(mmf.name, mmf);
        }
    }

    public class MetaGenMemberFunction : MetaMemberFunction
    {
        List<MetaGenTemplate> m_MetaGenTemplate = null;
        public MetaGenMemberFunction(MetaMemberFunction mmf, List<MetaGenTemplate> mgt) : base(mmf)
        {
            m_MetaGenTemplate = mgt;
        }
        public void UpdateGenFunction()
        {
            if (isTemplateInParam == false)
            {
                m_MetaBlockStatements = new MetaBlockStatements(this);
            }
            else
            {
                var list = metaMemberParamCollection.metaParamList;
                for (int k = 0; k < list.Count; k++)
                {
                    MetaDefineParam mdp = list[k] as MetaDefineParam;
                    if (mdp.metaVariable.isTemplate)
                    {
                        string pTName = mdp.metaVariable.metaDefineType.name;
                        //if (m_MetaGenTemplate.name == pTName)
                        //{
                        //    mdp.SetMetaType(metaType);
                        //}
                    }
                }
            }
        }
    }
}
