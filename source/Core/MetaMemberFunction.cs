//****************************************************************************
//  File:      MetaMemberFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;

namespace SimpleLanguage.Core
{
    public class MetaMemberFunction : MetaFunction
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
        public bool isTemplateFunction => m_IsTemplateFunction;
        public bool isTemplateClassFunction => m_IsTemplateClassFunction;
        public bool isWithInterface => m_IsWithInterface;
        public bool isOverrideFunction { get; set; } = false;
        public bool isConstructInitFunction => m_ConstructInitFunction;
        public bool isGet { get; set; } = false;
        public bool isSet { get; set; } = false;
        public bool isFinal { get; set; } = false;
        public bool isCanRewrite { get; set; } = false;
        public bool isTemplateInParam { get; set; } = false;
        public FileMetaMemberFunction fileMetaMemberFunction => m_FileMetaMemberFunction;

        #region 属性
        protected bool m_IsTemplateClassFunction = false;
        protected bool m_IsTemplateFunction = false;
        protected string m_FunctionAllName = null;
        protected bool m_ConstructInitFunction = false;
        protected bool m_IsWithInterface = false;
        protected FileMetaMemberFunction m_FileMetaMemberFunction = null;
        protected List<MetaGenTempalteFunction> m_GenTempalteFunctionList = new List<MetaGenTempalteFunction>();
        #endregion

        public MetaMemberFunction( MetaClass mc ):base(mc)
        {

        }
        public MetaMemberFunction( MetaClass mc, FileMetaMemberFunction fmmf):base( mc )
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(true, false);
            m_FileMetaMemberFunction = fmmf;
            m_Name = fmmf.name;

            m_IsStatic = fmmf.staticToken != null;
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
                m_IsWithInterface = true;
            }

            var paramCount = fmmf.metaParamtersList.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var param = fmmf.metaParamtersList[i];
                MetaDefineParam mmp = new MetaDefineParam( this, param );
                AddMetaDefineParam(mmp);
            }

            var templateCount = fmmf.metaTemplatesList.Count;         // Cast<T1>() 里边的T1 可以是多个
            for( int i = 0; i < templateCount; i++ )
            {
                m_IsTemplateFunction = true;

                var template = fmmf.metaTemplatesList[i];

                MetaTemplate mdt = new MetaTemplate( ownerMetaClass, template );
                AddMetaDefineTemplate(mdt);

                //下边的代码未来要转移支解析Meta过程中
                if( template.inClassNameTemplateNode != null )       //判断是否使用例似于where(csharp) where T : object
                {
                    var inClassToken = template.inClassNameTemplateNode;
                    MetaClass gmc = ClassManager.instance.GetMetaClassByNameAndFileMeta( ownerMetaClass, inClassToken.fileMeta, inClassToken.nameList );
                    if( gmc == null )
                    {
                        Debug.Write("Error 没有查找到inClass的类名, " + inClassToken.ToFormatString());
                        continue;
                    }
                    mdt.SetInConstraintMetaClass(gmc);
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
        public MetaMemberFunction( MetaMemberFunction mmf ) : base( mmf )
        {
            m_IsTemplateClassFunction = mmf.m_IsTemplateClassFunction;
            m_IsTemplateFunction = mmf.m_IsTemplateFunction;
            m_FunctionAllName = mmf.m_FunctionAllName;
            m_ConstructInitFunction = mmf.m_ConstructInitFunction;
            m_IsWithInterface = mmf.m_IsWithInterface;
            m_FileMetaMemberFunction = mmf.m_FileMetaMemberFunction;
            m_GenTempalteFunctionList = mmf.m_GenTempalteFunctionList;
        }
        protected void Init()
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
        public override void SetOwnerMetaClass(MetaClass ownerclass)
        {
            base.SetOwnerMetaClass(ownerclass);
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
        public void AddMetaDefineParam( MetaDefineParam mdp )
        {
            m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
        }
        public void AddMetaDefineTemplate ( MetaTemplate mt )
        {
            m_MetaMemberTemplateCollection.AddMetaDefineTemplate(mt);
        }
        //如果是模板函数，需要在实例化类后，进行新的实体函数的解析
        public MetaGenTempalteFunction AddGenTemplateMemberFunctionBySelf( List<MetaClass> list )
        {
            List<MetaGenTemplate> mgtList = new List<MetaGenTemplate>(list.Count);
            for( int i = 0; i < list.Count; i++ )
            {
                var l1 = this.m_MetaMemberTemplateCollection.metaTemplateList[i];
                MetaGenTemplate mgt = new MetaGenTemplate(l1, new MetaType(list[i]));
                mgtList.Add(mgt);
            }

           MetaGenTempalteFunction mgtf = new MetaGenTempalteFunction( this, mgtList );

            this.m_GenTempalteFunctionList.Add(mgtf);

            mgtf.Parse();

            return mgtf;
        }
        public MetaGenTempalteFunction GetGenTemplateFunction( List<MetaClass> mcList )
        {
            for( int i = 0; i < m_GenTempalteFunctionList.Count; i++ )
            {
                var c = m_GenTempalteFunctionList[i];
                if( c.MatchInputTemplateInsance( mcList ) )
                {
                    return c;
                }
            }
            return null;
        }
        public override void Parse()
        {
            base.Parse();
        }
        //public void ParseTemplateClassDefine()
        //{
        //    if( this.m_OriginalMetaMemberFunction != null )
        //    {
        //        var mmf = this.m_OriginalMetaMemberFunction;
        //        var list = m_OriginalMetaMemberFunction.metaMemberParamCollection.metaDefineParamList;
        //        //var list = metaMemberParamCollection.metaDefineParamList;
        //        for (int k = 0; k < list.Count; k++)
        //        {
        //            MetaDefineParam mdp = list[k];
        //            if (mdp.isFunctionTemplate)
        //            {
        //                MetaDefineParam nmdp = new MetaDefineParam(mdp.name, this);
        //                m_MetaMemberParamCollection.AddMetaDefineParam(nmdp);
        //                continue;
        //            }
        //            else if (mdp.isClassTemplate)
        //            {
        //                string pTName = mdp.metaDefineTypeName;
        //                var find = (m_OwnerMetaClass as MetaGenTemplateClass).GetMetaGenTemplate(pTName);
        //                if (find != null)
        //                {
        //                    //MetaDefineParam nmdp = new MetaDefineParam(mdp.name, mmf, new MetaType(find.metaType));
        //                    //m_MetaMemberParamCollection.AddMetaDefineParam(nmdp);
        //                }
        //            }
        //            else
        //            {
        //                //MetaDefineParam nmdp = new MetaDefineParam(mdp.name, mmf, mdp?.metaVariable?.metaDefineType);
        //                //m_MetaMemberParamCollection.AddMetaDefineParam(nmdp);
        //            }
        //        }

        //        if (mmf.returnMetaVariable != null)
        //        {
        //            m_DefineMetaType = new MetaType(mmf.returnMetaVariable.metaDefineType);
        //            m_ReturnMetaVariable = new MetaVariable(mmf.returnMetaVariable.name, EVariableFrom.LocalStatement, m_MetaBlockStatements, this.ownerMetaClass, m_DefineMetaType);
        //        }
        //        if (mmf.metaBlockStatements != null)
        //        {
        //            m_MetaBlockStatements.AddFrontToEndStatements(mmf.metaBlockStatements);
        //            MetaStatements ms = mmf.metaBlockStatements.GenTemplateClassStatement(m_OwnerMetaClass as MetaGenTemplateClass, m_MetaBlockStatements);
        //            m_MetaBlockStatements.SetNextStatements(ms);
        //        }
        //        ParseDefineMetaType();
        //    }
        //}
        public override void ParseDefineMetaType()
        {
            if (this.m_FileMetaMemberFunction != null)
            {
                if (m_FileMetaMemberFunction.defineMetaClass != null)
                {
                    if (m_ConstructInitFunction)
                    {
                        Debug.Write("Error 当前类:" + allName + " 是构建Init类，不允许有返回类型 ");
                    }
                    else
                    {
                        FileMetaClassDefine cmr = m_FileMetaMemberFunction.defineMetaClass;
                        m_DefineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction( m_OwnerMetaClass, this, cmr );
                        m_ReturnMetaVariable.SetMetaDefineType(m_DefineMetaType);

                        if (m_DefineMetaType.IsIncludeClassTemplate(m_OwnerMetaClass  ))
                        {
                            m_IsTemplateClassFunction = true;
                        }
                    }
                }
            }
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.ParseMetaDefineType();
                if ( mpl.isClassTemplate)
                {
                    m_IsTemplateClassFunction = true;
                }
            }
        }
        public override void CreateMetaExpress()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.CreateExpress();
            }
        }
        public override bool ParseMetaExpress()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.Parse();
                mpl.CaleReturnType();
            }
            return true;
        }
        public void ParseStatements()
        {
            if( this.m_FileMetaMemberFunction != null )
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
                    Debug.Write("Error 该函数没有定义内容！！");
                }
            }
            if( !m_IsWithInterface)
            {
                m_MetaBlockStatements.SetDeep(deep);
            }
        }
        public static MetaStatements CreateMetaSyntax( FileMetaSyntax rootMs, MetaBlockStatements currentBlockStatements)
        {    
            MetaStatements beforeStatements = currentBlockStatements;            
            while (rootMs.IsNotEnd() )
            {
                var childFms = rootMs.GetCurrentSyntaxAndMove();
                HandleMetaSyntax(currentBlockStatements, ref beforeStatements,  childFms );
            }
            return beforeStatements;
        }
        public static MetaStatements HandleMetaSyntax(MetaBlockStatements currentBlockStatements, 
            ref MetaStatements beforeStatements,
            FileMetaSyntax childFms )
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
                case FileMetaConditionExpressSyntax fmkes:  //dowhile/while conditionvarabile
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
                            Debug.Write("Error FileMetaConditionExpressSyntax: 暂不支持该类型的解析!!");
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
                        bool isDefineVarStatements = false;
                        if (fmos.variableRef.isOnlyName)
                        {
                            string name1 = fmos.variableRef.name;
                            if( fmos.hasDefine )
                            {
                                if (currentBlockStatements.GetIsMetaVariable(name1))
                                {
                                    Debug.Write("Error 如果使用了var/data/dynamic/int 等前缀，有重复定义的行为" + fmos.variableRef.ToTokenString());
                                    isDefineVarStatements = false;
                                }
                                else
                                {
                                    isDefineVarStatements = true;
                                }
                            }
                            else
                            {
                                if (!currentBlockStatements.GetIsMetaVariable(name1))
                                {
                                    var ownerclass = currentBlockStatements.ownerMetaClass;
                                    MetaBase mb = ownerclass.GetMetaMemberVariableByName(name1);
                                    if (mb != null)
                                    {
                                        Debug.Write("Error 如果是使用类成员，必须使用this.变量的方式" + fmos.variableRef.ToTokenString());
                                    }
                                    else
                                    {
                                        isDefineVarStatements = true;
                                    }
                                }
                            }
                        }
                        if (isDefineVarStatements)
                        {
                            //if (currentBlockStatements.ownerMetaFunction?.isConstructFunction)
                            //{
                            //    Debug.Write("Error 构造函数中，不允许使用定义字段，必须使用this.非静态或者是类名.静态字段赋值!" + fmos.variableRef.ToTokenString());
                            //}
                            MetaDefineVarStatements mnvs11 = new MetaDefineVarStatements( currentBlockStatements, fmos );
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
                case FileMetaDefineVariableSyntax fmvs: // x = 2;
                    {
                        bool isDefineVarStatements = false;
                        string name1 = fmvs.name;
                        if (currentBlockStatements.GetIsMetaVariable(name1))
                        {
                            isDefineVarStatements = true;
                            Debug.Write("Error 定义变量名称与类函数临时名称一样!!" + fmvs.token?.ToLexemeAllString());                            
                        }
                        else
                        {
                            isDefineVarStatements = currentBlockStatements.ownerMetaClass.GetMetaMemberVariableByName(name1) == null;
                            if (!isDefineVarStatements)
                            {
                                Debug.Write("Error 定义变量名称与类定义名称一样!!" + fmvs.token?.ToLexemeAllString());
                            }
                        }
                        if ( isDefineVarStatements )
                        {
                            MetaDefineVarStatements mnvs11 = new MetaDefineVarStatements(currentBlockStatements, fmvs);                           
                            beforeStatements.SetNextStatements(mnvs11);
                            beforeStatements = mnvs11;
                        }
                    }
                    break;
                case FileMetaCallSyntax fmcs:       //a.value.SetH(100);
                    {
                        var mcs = new Statements.MetaCallStatements(currentBlockStatements, fmcs );
                        beforeStatements.SetNextStatements(mcs);
                        beforeStatements = mcs;
                        return mcs;
                    }
                case FileMetaKeyReturnSyntax fmrs:      //ret 100
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
                            Debug.Write("Error 生成MetaStatements出错KeyReturnSyntax类型错误!!");
                        }
                    }
                    break;
                case FileMetaKeyGotoLabelSyntax fmkgls: //goto 1// label 1
                    {
                        var metaGotoStatements = new MetaGotoLabelStatements(currentBlockStatements, fmkgls);
                        beforeStatements.SetNextStatements(metaGotoStatements);
                        beforeStatements = metaGotoStatements;
                        return metaGotoStatements;
                    }
                default:
                    Debug.Write("Waning 还有没有解析的语句!! MetaMemberFunction 314");
                    break;
            }
            return null;
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
            StringBuilder sb = new StringBuilder();
            sb.Append(m_DefineMetaType.ToFormatString());
            sb.Append(" ");
            sb.Append( allName );
            sb.Append("(");

            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                sb.Append(mpl.ToString());
                if( i < m_MetaMemberParamCollection.metaDefineParamList.Count -1  )
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");

            return sb.ToString();
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
