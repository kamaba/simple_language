
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
using System.Security.Cryptography;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;

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
                    if( m_MetaMemberParamCollection?.maxParamCount > 0 )
                    {
                        sb.Append("_");
                        sb.Append(m_MetaMemberParamCollection.maxParamCount.ToString() );
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
        protected FileMetaMemberFunction m_FileMetaMemberFunction = null;

        public MetaMemberFunction( MetaClass mc ):base(mc)
        {

        }
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

            var paramCount = fmmf.metaParamtersList.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var param = fmmf.metaParamtersList[i];
                MetaDefineParam mmp = new MetaDefineParam(mc, null, param);
                m_MetaMemberParamCollection.AddMetaDefineParam(mmp);
            }
            var templateCount = fmmf.metaTemplatesList.Count;         // Cast<T1>() 里边的T1 可以是多个
            for( int i = 0; i < templateCount; i++ )
            {
                var template = fmmf.metaTemplatesList[i];

                MetaTemplate mdt = new MetaTemplate( ownerMetaClass, template );
                AddMetaDefineTemplate(mdt);

                //下边的代码未来要转移支解析Meta过程中
                for( int j = 0; j < template.inClassNameTokenList.Count; j++)       //判断是否使用in []
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
        public MetaMemberFunction( MetaClass mc, string _name ) : base( mc )
        {
            m_Name = _name;
            isCanRewrite = true;
            m_MetaMemberParamCollection.Clear();

            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;

            Init();
        }
        void Init()
        {
            m_ConstructInitFunction = name == "_init_";
            if(m_DefineMetaType == null )
            {
                if (m_ConstructInitFunction)
                {
                    m_DefineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
                }
                else
                {
                    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                }
            }
            if( isSet && !isGet )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if ( isGet )
            {
                m_IsMustNeedReturnStatements = true;
            }
            if ( !isStatic )
            {
                m_ThisMetaVariable = new MetaVariable( "this_" + GetHashCode().ToString(), EVariableFrom.Argument, null, m_OwnerMetaClass, new MetaType( m_OwnerMetaClass ) );
            }
            m_ReturnMetaVariable = new MetaVariable("return_" + GetHashCode().ToString(), EVariableFrom.Argument, null, m_OwnerMetaClass, m_DefineMetaType);
        }

        public override void SetDeep(int deep)
        {
            m_Deep = deep;
            m_MetaBlockStatements?.SetDeep(deep);
        }
        public Token GetToken()
        {
            if( m_FileMetaMemberFunction?.finalToken != null )
            {
                return m_FileMetaMemberFunction.finalToken;
            }
            return this.pingToken;
        }
        public bool IsEqualWithMMFByNameAndParam( MetaMemberFunction mmf )
        {
            if (mmf.name != m_Name) return false;

            if( !m_MetaMemberParamCollection.IsEqualMetaDefineParamCollection( mmf.metaMemberParamCollection ) )
            {
                return false;
            }

            return true;
        }
        public void UpdateGenMemberFunction( MetaMemberFunction mmf )
        {
            MetaGenTemplateClass mtc = m_OwnerMetaClass as MetaGenTemplateClass;
            m_Name = mmf.m_Name;
            SetOwnerMetaClass(mtc);
            m_FileMetaMemberFunction = mmf.m_FileMetaMemberFunction;
            isStatic = mmf.isStatic;
            isGet = mmf.isGet;
            isSet = mmf.isSet;
            isFinal = mmf.isFinal;
            m_IsMustNeedReturnStatements = mmf.m_IsMustNeedReturnStatements;
            m_MethodCallType = mmf.m_MethodCallType;
            isTemplateInParam = mmf.isTemplateInParam;
            m_DefineMetaType = new MetaType(mmf.m_DefineMetaType);
            m_MetaBlockStatements = new MetaBlockStatements(this);
            m_MetaBlockStatements.isOnFunction = true;

            var list = mmf.metaMemberParamCollection.metaParamList;
            MetaGenTemplateClass mgtc = m_OwnerMetaClass as MetaGenTemplateClass;
            m_MetaMemberParamCollection = new MetaDefineParamCollection();
            for (int k = 0; k < list.Count; k++)
            {
                MetaDefineParam mdp = list[k] as MetaDefineParam;
                if (mdp.isTemplate)
                {
                    string pTName = mdp.metaVariable.metaDefineType.metaTemplate.name;
                    var find = mgtc.GetMetaGenTemplate(pTName);
                    if (find != null)
                    {
                        MetaDefineParam nmdp = new MetaDefineParam( mdp.metaVariable.name, m_OwnerMetaClass, m_MetaBlockStatements, new MetaType( find.metaType ) );
                        m_MetaMemberParamCollection.AddMetaDefineParam(nmdp);
                    }
                }
                else
                {
                    MetaDefineParam nmdp = new MetaDefineParam(mdp.metaVariable.name, m_OwnerMetaClass, m_MetaBlockStatements, mdp?.metaVariable?.metaDefineType );
                    m_MetaMemberParamCollection.AddMetaDefineParam(nmdp);
                }
            }

            MetaType mt = null;
            if(mmf.returnMetaVariable != null )
            {
                var mmf_retMV = mmf.returnMetaVariable;

                if (mmf_retMV.metaDefineType.isTemplate)
                {
                    string pTName = mmf_retMV.metaDefineType.metaTemplate.name;
                    var find = mgtc.GetMetaGenTemplate(pTName);
                    if (find != null)
                    {
                        mt = find.metaType;
                    }
                    else
                    {
                        Console.WriteLine("Error 没有发现模版对应的模板名称!!");
                    }
                }
                else
                {
                    mt = mmf_retMV.metaDefineType;
                }
                m_DefineMetaType = new MetaType(mt);
                m_ReturnMetaVariable = new MetaVariable(mmf.returnMetaVariable.name, EVariableFrom.LocalStatement, m_MetaBlockStatements, this.ownerMetaClass, m_DefineMetaType );

            }
            if( mmf.metaBlockStatements != null )
            {
                //MetaStatements ms = mmf.metaBlockStatements.GenTemplateClassStatement(m_OwnerMetaClass as MetaGenTemplateClass, m_MetaBlockStatements);
                //m_MetaBlockStatements.SetNextStatements(ms);
            }
            
        }
        public void AddMetaDefineParam( MetaDefineParam mdp )
        {
            m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
        }
        public void AddMetaDefineTemplate (MetaTemplate mt )
        {
            m_MetaMemberTemplateCollection.AddMetaDefineTemplate(mt);
        }
        public override void ParseName()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaParamList[i] as MetaDefineParam;
                mpl.Parse();
                if (mpl.isTemplate)
                {
                    isTemplateInParam = true;
                }
            }
        }
        public override void ParsDefineMetaType()
        {
            if (m_FileMetaMemberFunction != null)
            {
                /* 暂不支持横版函数  例   T1 Create<T1>(int a )  中的T1 是独立的，不互模版类的T相同时，就认为是模版函数
                for (int i = 0; i < m_FileMetaMemberFunction.metaTemplatesList.Count; i++ )
                {
                    var mt = m_FileMetaMemberFunction.metaTemplatesList[i];

                    var nmt = new MetaTemplate( this.ownerMetaClass, mt );

                    AddMetaDefineTemplate(nmt);
                }
                */
                if (m_FileMetaMemberFunction.defineMetaClass != null)
                {
                    MetaType retMT = null;
                    if (m_ConstructInitFunction)
                    {
                        Console.WriteLine("Error 当前类:" + allName + " 是构建Init类，不允许有返回类型 ");
                    }
                    else
                    {
                        FileMetaClassDefine cmr = m_FileMetaMemberFunction.defineMetaClass;

                        string templateName = cmr.name;
                        var getMetaTemplate = m_OwnerMetaClass.GetTemplateMetaClassByName(templateName);
                        if (getMetaTemplate != null)
                        {
                            retMT = new MetaType(getMetaTemplate);
                        }
                        else
                        {
                            var rawMC = ClassManager.instance.GetMetaClassByClassDefineAndFileMeta(m_OwnerMetaClass, cmr);
                            List<MetaTemplate> templates = new List<MetaTemplate>();
                            if (cmr.inputTemplateNodeList.Count > 0)
                            {
                                Console.WriteLine("Error 没有找到MetaTemplate相关信息，语法错误!!");
                                //for ( int i = 0; i < cmr.inputTemplateNodeList.Count; i++ )
                                //{
                                //    MetaTemplate cmt = m_MetaMemberTemplateCollection.GetMetaDefineTemplateByName(cmr.inputTemplateNodeList[i].nameList[0]);
                                //    if( cmt != null )
                                //    {
                                //        templates.Add( cmt );
                                //    }
                                //    else
                                //    {
                                //        // T2 Create<T1> 没有从<>找到T2
                                //        Console.WriteLine("Error 没有找到MetaTemplate相关信息，语法错误!!");
                                //    }
                                //}
                            }
                            retMT = new MetaType(rawMC);
                        }

                        if (retMT == null)
                        {
                            Console.WriteLine("Error 定义的返回类型，没有找到相对应的类型！！");
                        }
                        else
                        {
                            m_DefineMetaType = retMT;
                            m_ReturnMetaVariable.SetMetaDefineType(m_DefineMetaType);
                        }
                    }
                }
            }
        }
        public override bool ParseMetaExpress()
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
                if(m_ThisMetaVariable != null )
                {
                    m_ThisMetaVariable.AddPingToken(m_FileMetaMemberFunction.token);
                }
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
                        var mcs = new Statements.MetaCallStatements(currentBlockStatements, fmcs );
                        beforeStatements.SetNextStatements(mcs);
                        beforeStatements = mcs;
                        return mcs;
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

        public override string ToString()
        {
            return allName;
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
            if (isGet)
            {
                sb.Append(" get");
            }
            if (isSet)
            {
                sb.Append(" set");
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
}
