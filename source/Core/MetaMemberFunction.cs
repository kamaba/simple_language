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
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class MetaMemberFunctionTemplateNode
    {
        public Dictionary<int, MetaMemberFunctionNode> metaTemplateFunctionNodeDict => m_MetaTemplateFunctionNodeDict;

        //模板数据匹配，只有在是模板函数时处理   fun<T>(){} fun<T1,T2>(){}
        protected Dictionary<int, MetaMemberFunctionNode> m_MetaTemplateFunctionNodeDict = new Dictionary<int, MetaMemberFunctionNode>();

        public void SetDeep(int deep)
        {
        }
        public MetaMemberFunction IsSameMetaMemeberFunction(MetaMemberFunction mmf)
        {
            MetaMemberFunctionNode find = null;
            if (m_MetaTemplateFunctionNodeDict.ContainsKey(mmf.metaMemberTemplateCollection.count))
            {
                find = m_MetaTemplateFunctionNodeDict[mmf.metaMemberTemplateCollection.count];
            }
            else
            {
                find = new MetaMemberFunctionNode();
            }
            return find.IsSameMetaMemeberFunction(mmf);
        }


        public bool AddMetaMemberFunction(MetaMemberFunction mmf)
        {
            MetaMemberFunctionNode find = null;
            if (m_MetaTemplateFunctionNodeDict.ContainsKey(mmf.metaMemberTemplateCollection.count))
            {
                find = m_MetaTemplateFunctionNodeDict[mmf.metaMemberTemplateCollection.count];
            }
            else
            {
                find = new MetaMemberFunctionNode();
                m_MetaTemplateFunctionNodeDict[mmf.metaMemberTemplateCollection.count] = find;
            }
            return find.AddMetaMemberFunction(mmf);
        }
        public void ParseMemberFunctionDefineMetaType()
        {
            foreach( var v in m_MetaTemplateFunctionNodeDict )
            {
                v.Value.ParseMemberFunctionDefineMetaType();
            }
        }
    }
    public class MetaMemberFunctionNode
    {
        public Dictionary<int, List<MetaMemberFunction>> metaParamFunctionDict => m_MetaParamFunctionDict;

        //参数个数匹配，可以相同参数的不同类接口   fun( int a ){}  fun( string a ){}  int=1
        protected Dictionary<int, List<MetaMemberFunction>> m_MetaParamFunctionDict = new Dictionary<int, List<MetaMemberFunction>>();

        public void SetDeep(int deep)
        {
        }
        public MetaMemberFunction IsSameMetaMemeberFunction( MetaMemberFunction mmf )
        {
            List<MetaMemberFunction> list = null;
            if (m_MetaParamFunctionDict.ContainsKey(mmf.metaMemberParamCollection.metaDefineParamList.Count))
            {
                list = m_MetaParamFunctionDict[mmf.metaMemberParamCollection.metaDefineParamList.Count];
            }
            else
            {
                list = new List<MetaMemberFunction>();
            }

            MetaMemberFunction find2 = null;
            for (int i = 0; i < list.Count; i++)
            {
                var curFun = list[i];
                if (curFun.metaMemberParamCollection.IsEqualMetaDefineParamCollection(mmf.metaMemberParamCollection))
                {
                    find2 = curFun;
                    break;
                }
            }
            if (find2 == null)
            {
                list.Add(mmf);
                return null;
            }
            else
            {
                //Log.AddInStructMeta(EError.None, "发现已经定义过某某类1" + mmf.functionAllName);
                return find2;
            }
        }
        public bool AddMetaMemberFunction( MetaMemberFunction mmf )
        {
            List<MetaMemberFunction> list = null;
            if (m_MetaParamFunctionDict.ContainsKey(mmf.metaMemberParamCollection.metaDefineParamList.Count))
            {
                list = m_MetaParamFunctionDict[mmf.metaMemberParamCollection.metaDefineParamList.Count];
            }
            else
            {
                list = new List<MetaMemberFunction>();
                m_MetaParamFunctionDict[mmf.metaMemberParamCollection.metaDefineParamList.Count] = list;
            }

            MetaMemberFunction find2 = null;
            for (int i = 0; i < list.Count; i++)
            {
                var curFun = list[i];
                if (curFun.metaMemberParamCollection.IsEqualMetaDefineParamCollection(mmf.metaMemberParamCollection))
                {
                    find2 = curFun;
                    break;
                }
            }
            if (find2 == null)
            {
                list.Add(mmf);
                return true;
            }
            else
            {
                Log.AddInStructMeta(EError.None, "发现已经定义过某某类2" + mmf.functionAllName);
            }
            return false;
        }

        public void ParseMemberFunctionDefineMetaType()
        {
            foreach( var v in m_MetaParamFunctionDict )
            {
                foreach( var v2 in v.Value )
                {
                    v2.ParseDefineMetaType();
                }
            }
        }
        public List<MetaMemberFunction> GetMetaMemberFunctionListByParamCount( int count )
        {
            if (m_MetaParamFunctionDict.ContainsKey(count))
            {
                return m_MetaParamFunctionDict[count];
            }
            return null;
        }
    }
    public class MetaMemberFunction : MetaFunction
    {
        public override string functionAllName
        {
            get
            {
                if(string.IsNullOrEmpty( m_FunctionAllName ) )
                {
                    return base.functionAllName;
                }
                return m_FunctionAllName;
            }
        }
        public int parseLevel
        {
            get
            {
                if( m_IsTemplateFunction )
                {
                    return 0;
                }
                else if( m_OwnerMetaClass.isTemplateClass ){
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }
        public bool isTemplateFunction => m_IsTemplateFunction;
        public bool isWithInterface => m_IsWithInterface;
        public bool isOverrideFunction => m_IsOverrideFunction;
        public bool isConstructInitFunction => m_ConstructInitFunction;
        public bool isGet => m_IsGet;
        public bool isSet => m_IsSet;
        public bool isFinal => m_IsFinal;
        public bool isCanRewrite => m_IsCanRewrite;
        public bool isTemplateInParam => m_IsTemplateInParam;
        public FileMetaMemberFunction fileMetaMemberFunction => m_FileMetaMemberFunction;
        public MetaMemberFunction sourceMetaMemberFunction => m_SourceMetaMemberFunction;
        public List<MetaType> bindStructTemplateFunctionMtList => m_BindStructTemplateFunctionMtList;
        public List<MetaType> bindStructTemplateFunctionAndClassMtList => m_BindStructTemplateFunctionAndClassMtList;
        public List<MetaGenTempalteFunction> genTempalteFunctionList => m_GenTempalteFunctionList;


        #region 属性
        protected bool m_IsTemplateFunction = false;
        protected bool m_IsOverrideFunction = false;
        protected bool m_IsGet = false;
        protected bool m_IsSet = false;
        protected bool m_IsFinal = false;
        protected bool m_IsCanRewrite = false;
        protected bool m_IsTemplateInParam = false;
        protected bool m_ConstructInitFunction = false;
        protected bool m_IsWithInterface = false;
        protected MetaMemberFunction m_SourceMetaMemberFunction = null;
        protected FileMetaMemberFunction m_FileMetaMemberFunction = null;
        //绑定构建 元类型  
        protected List<MetaType> m_BindStructTemplateFunctionMtList = new List<MetaType>();
        protected List<MetaType> m_BindStructTemplateFunctionAndClassMtList = new List<MetaType>();

        //模板生成函数，如果匹配了，模板函数后，再进行看是否生成过该函数
        protected List<MetaGenTempalteFunction> m_GenTempalteFunctionList = new List<MetaGenTempalteFunction>();
        #endregion

        public MetaMemberFunction( MetaClass mc ):base(mc)
        {

        }
        public MetaMemberFunction( MetaClass mc, FileMetaMemberFunction fmmf):base( mc )
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(true, false);
            m_FileMetaMemberFunction = fmmf;
            this.m_Name = fmmf.name;

            m_IsStatic = fmmf.staticToken != null;
            m_IsGet = fmmf.getToken != null;
            m_IsSet = fmmf.setToken != null;
            m_IsFinal = fmmf.finalToken != null;
            if ( fmmf.overrideToken != null )
            {
                if (fmmf.overrideToken.type == ETokenType.Override)
                    m_IsOverrideFunction = true;
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

                MetaTemplate mdt = new MetaTemplate( ownerMetaClass, template, mc.metaTemplateList.Count + i );
                AddMetaDefineTemplate(mdt);

                //下边的代码未来要转移支解析Meta过程中
                if( template.inClassNameTemplateNode != null )       //判断是否使用例似于where(csharp) where T : object
                {
                    var inClassToken = template.inClassNameTemplateNode;
                    MetaNode mn = ClassManager.instance.GetMetaClassByNameAndFileMeta(ownerMetaClass, inClassToken.fileMeta, inClassToken.nameList);
                    if( mn == null )
                    {
                        continue;
                    }
                    MetaClass gmc = mn.GetMetaClassByTemplateCount(0);
                    if( gmc == null )
                    {
                        Log.AddInStructMeta( EError.None, "Error 没有查找到inClass的类名, " + inClassToken.ToFormatString());
                        continue;
                    }
                    mdt.SetInConstraintMetaClass(gmc);
                }
                else
                {
                    mdt.SetInConstraintMetaClass(CoreMetaClassManager.objectMetaClass);
                }
            }
            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;

            Init();
        }
        public MetaMemberFunction( MetaClass mc, string _name ) : base( mc )
        {
            m_Name = _name;
            m_IsCanRewrite = true;
            m_MetaMemberParamCollection.Clear();

            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;

            Init();
        }
        public MetaMemberFunction( MetaMemberFunction mmf ) : base( mmf )
        {
            m_IsTemplateFunction = mmf.m_IsTemplateFunction;
            m_ConstructInitFunction = mmf.m_ConstructInitFunction;
            m_IsWithInterface = mmf.m_IsWithInterface;
            m_FileMetaMemberFunction = mmf.m_FileMetaMemberFunction;
            m_GenTempalteFunctionList = mmf.m_GenTempalteFunctionList;
            m_SourceMetaMemberFunction = mmf.sourceMetaMemberFunction;
        }
        protected void Init()
        {
            m_ConstructInitFunction = this.name == "_init_";

            MetaType defineMetaType = null;
            if (m_ConstructInitFunction)
            {
                defineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            else
            {
                defineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            if( isSet && !isGet )
            {
                defineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if (!isStatic)
            {
                var mt = new MetaType(m_OwnerMetaClass);
                if( m_OwnerMetaClass.isTemplateClass )
                {
                    var tt = new MetaTemplate(m_OwnerMetaClass, "this");
                    tt.SetIndex(0);
                    tt.SetInConstraintMetaClass(m_OwnerMetaClass);
                    mt = new MetaType(tt);
                }
                m_ThisMetaVariable = new MetaVariable(m_OwnerMetaClass.allClassName + "." + m_Name + ".this", MetaVariable.EVariableFrom.Argument, null, m_OwnerMetaClass, mt );
            }
            m_ReturnMetaVariable = new MetaVariable(m_OwnerMetaClass.allClassName + "." + m_Name + ".define", MetaVariable.EVariableFrom.Argument, null, m_OwnerMetaClass, defineMetaType );
        }
        public override void SetDeep(int deep)
        {
            base.SetDeep(deep);
        }
        public override void SetAnchorDeep(int addep)
        {
            base.SetAnchorDeep(addep);
        }
        public void SetSourceMetaMemberFunction( MetaMemberFunction mmf )
        {
            this.m_SourceMetaMemberFunction = mmf;
        }
        public void SetIsGet(bool isGet)
        {
            m_IsGet = isGet;
        }
        public void SetIsSet(bool isSet)
        {
            m_IsSet = isSet;
        }
        public void SetIsOverrideFunction(bool flag )
        {
            m_IsOverrideFunction = flag;
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
        public MetaGenTempalteFunction AddGenTemplateMemberFunctionByMetaTypeList(MetaClass mc, List<MetaType> list)
        {
            if (mc.isTemplateClass)
            {
                return null;
            }

            List<MetaClass> mcList = new List<MetaClass>();

            foreach (var v in list)
            {
                if (v.eType == EMetaTypeType.MetaClass
                    || v.eType == EMetaTypeType.MetaGenClass )
                {
                    mcList.Add(v.metaClass);
                }
            }
            if( mcList.Count == list.Count )
            {
                return AddGenTemplateMemberFunctionBySelf(mc, mcList);
            }
            return null;
        }
        public MetaGenTempalteFunction AddGenTemplateMemberFunctionBySelf( MetaClass mc, List<MetaClass> list)
        {
            MetaGenTempalteFunction mgtf = GetGenTemplateFunction(list);
            if (mgtf == null)
            {
                List<MetaGenTemplate> mgtList = new List<MetaGenTemplate>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var l1 = this.m_MetaMemberTemplateCollection.metaTemplateList[i];
                    MetaGenTemplate mgt = new MetaGenTemplate(l1, new MetaType(list[i]));
                    mgtList.Add(mgt);
                }
                mgtf = new MetaGenTempalteFunction(this, mgtList);
                mgtf.SetOwnerMetaClass(mc);

                this.m_GenTempalteFunctionList.Add(mgtf);

                mgtf.Parse();
            }
            return mgtf;
        }
        public MetaGenTempalteFunction GetGenTemplateFunction(List<MetaClass> mcList)
        {
            if( mcList.Count == m_GenTempalteFunctionList.Count )
            {
                for (int i = 0; i < m_GenTempalteFunctionList.Count; i++)
                {
                    var c = m_GenTempalteFunctionList[i];

                    if (c.MatchInputTemplateInsance(mcList))
                    {
                        return c;
                    }
                }

            }
            return null;
        }
        public override bool Parse()
        {
            bool flag = base.Parse();

            UpdateVritualFunctionName();

            return flag;
        }
        public virtual void ParseDefineMetaType()
        {
            if (this.m_FileMetaMemberFunction != null)
            {
                if (m_FileMetaMemberFunction.defineMetaClass != null)
                {
                    FileMetaClassDefine cmr = m_FileMetaMemberFunction.defineMetaClass;
                    var defineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(m_OwnerMetaClass, this, cmr);

                    if (m_ConstructInitFunction && defineMetaType.metaClass != CoreMetaClassManager.voidMetaClass )
                    {
                        Log.AddInStructMeta(EError.None, "Error 当前类:" + m_AllName + " 是构建Init类，不允许有返回类型 ");
                    }
                    else
                    {
                        m_ReturnMetaVariable.SetMetaDefineType(defineMetaType);
                        m_ReturnMetaVariable.SetRealMetaType(defineMetaType);
                    }
                }
            }
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.ParseMetaDefineType();
            }
            UpdateVritualFunctionName();
        }
        public virtual void CreateMetaExpress()
        {
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.CreateExpress();
            }
        }
        public virtual bool ParseMetaExpress()
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
            bool nohasContent = false;
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
                    nohasContent = true;
                }
            }
            if( !m_IsWithInterface || this.m_OwnerMetaClass.isInterfaceClass )
            {
            }
            else
            {
                if (nohasContent)
                {
                    Log.AddInStructMeta(EError.None, $"Error 类[{this.m_OwnerMetaClass.allClassName}] 该函数[{this.functionAllName}] 没有定义函数内容！！");
                }
            }
        }
        public void UpdateVritualFunctionName()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_Name);
            sb.Append("_");
            sb.Append(m_ReturnMetaVariable.metaDefineType.ToString() );
            sb.Append("_");
            sb.Append(m_MetaMemberParamCollection.maxParamCount);
            if(m_MetaMemberParamCollection.maxParamCount > 0 )
            {
                sb.Append("_");
                for (int i = 0; i < m_MetaMemberParamCollection.maxParamCount; i++)
                {
                    var mdp = m_MetaMemberParamCollection.metaDefineParamList[i];
                    sb.Append(mdp.metaVariable.metaDefineType.ToString());
                    if (i < m_MetaMemberParamCollection.maxParamCount - 1)
                    {
                        sb.Append("_");
                    }
                }
            }
            m_VirtualFunctionName = sb.ToString();
        }
        public MetaType AddMetaPreTemplateFunction(MetaType mt, out bool isGenMetaClass)
        {
            /*----------------------
            isGenMetaClass = false;
            if (mt.metaClass == null)
            {
                return null;
            }
            List<MetaClass> mcList = new List<MetaClass>();
            for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
            {
                var mtc = mt.templateMetaTypeList[i];
                if (mtc.eType == EMetaTypeType.MetaClass)
                {
                    mcList.Add(mtc.metaClass);
                }
                else if (mtc.eType == EMetaTypeType.MetaGenClass)
                {
                    mcList.Add(mtc.metaGenTemplateClass);
                }
            }
            if (mcList.Count == mt.templateMetaTypeList.Count)
            {
                MetaGenTemplateClass mgtc = mt.metaClass.AddInstanceMetaClass(mcList);
                isGenMetaClass = true;
                return new MetaType(mgtc, mt.templateMetaTypeList);
            }

            var find = BindStructTemplateMetaClassList(mt);
            if (find == null)
            {
                this.m_BindStructTemplateMetaClassList.Add(new MetaType(mt));
            }
            //--------------------------------------
            */
                
            isGenMetaClass = false;
            if (mt.metaClass == null)
            {
                return null;
            }
            bool isIncludeTemplateClass = mt.IsIncludeClassTemplate(m_OwnerMetaClass);
            List<MetaClass> mcList = new List<MetaClass>();
            for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
            {
                var mtc = mt.templateMetaTypeList[i];
                if (mtc.eType == EMetaTypeType.MetaClass)
                {
                    mcList.Add(mtc.metaClass);
                }
            }
            if (mcList.Count == mt.templateMetaTypeList.Count)
            {
                MetaGenTemplateClass mgtc = mt.metaClass.AddInstanceMetaClass(mcList);
                isGenMetaClass = true;
                return new MetaType(mgtc);
            }
            if(isIncludeTemplateClass )
            {
                var find = FindBindStructTemplateFunctionAndClassMtList(mt);
                if (find == null)
                {
                    this.m_BindStructTemplateFunctionAndClassMtList.Add(new MetaType(mt));
                }
            }
            else
            {
                var find = FindBindStructTemplateFunctionMtList(mt);
                if (find == null)
                {
                    this.m_BindStructTemplateFunctionMtList.Add(new MetaType(mt));
                }
            }
            return mt;
        }
        public MetaType FindBindStructTemplateFunctionMtList(MetaType mt)
        {
            foreach (var v in m_BindStructTemplateFunctionMtList)
            {
                if (MetaType.EqualMetaDefineType(v, mt))
                {
                    return v;
                }
            }
            return null;
        }
        public MetaType FindBindStructTemplateFunctionAndClassMtList(MetaType mt)
        {
            foreach (var v in m_BindStructTemplateFunctionAndClassMtList)
            {
                if (MetaType.EqualMetaDefineType(v, mt))
                {
                    return v;
                }
            }
            return null;
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
                            Log.AddInStructMeta(EError.None, "Error FileMetaConditionExpressSyntax: 暂不支持该类型的解析!!");
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
                                    Log.AddInStructMeta(EError.None, "Error 如果使用了var/data/dynamic/int 等前缀，有重复定义的行为" + fmos.variableRef.ToTokenString());
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
                                    if (mb == null)
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
                            //    Log.AddInStructMeta( EError.None, "Error 构造函数中，不允许使用定义字段，必须使用this.非静态或者是类名.静态字段赋值!" + fmos.variableRef.ToTokenString());
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
                            Log.AddInStructMeta(EError.None, "Error 定义变量名称与类函数临时名称一样!!" + fmvs.token?.ToLexemeAllString());
                            return null;
                        }
                        else
                        {
                            var mv = currentBlockStatements.ownerMetaClass.GetMetaMemberVariableByName(name1);
                            if( mv == null )
                            {
                                isDefineVarStatements = true;
                            }
                            else
                            {
                                if (!mv.isStatic)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 定义变量名称与类定义名称一样 如果调用成员变量，需要在前边使用this.!!" + fmvs.token?.ToLexemeAllString());
                                    return null;
                                }
                            }
                        }
                        if ( isDefineVarStatements )
                        {
                            MetaDefineVarStatements mnvs11 = new MetaDefineVarStatements(currentBlockStatements, fmvs);                           
                            beforeStatements.SetNextStatements(mnvs11);
                            beforeStatements = mnvs11;
                        }
                        else
                        {
                            MetaAssignStatements mas = new MetaAssignStatements(currentBlockStatements, fmvs );
                            beforeStatements.SetNextStatements(mas);
                            beforeStatements = mas;
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
                            Log.AddInStructMeta(EError.None, "Error 生成MetaStatements出错KeyReturnSyntax类型错误!!");
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
                    Log.AddInStructMeta(EError.None, "Waning 还有没有解析的语句!! MetaMemberFunction 314");
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
            if( m_ReturnMetaVariable != null )
            {
                sb.Append(m_ReturnMetaVariable.metaDefineType.ToFormatString());
            }
            sb.Append(" "); 
            
            if (m_OwnerMetaClass != null)
            {
                sb.Append(m_OwnerMetaClass.allClassName);
                sb.Append(".");
            }
            sb.Append(m_Name);
            if (m_MetaMemberTemplateCollection.metaTemplateList.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_MetaMemberTemplateCollection.metaTemplateList.Count; i++)
                {
                    var mtl = m_MetaMemberTemplateCollection.metaTemplateList[i];
                    sb.Append(mtl.name);
                    if (i < m_MetaMemberTemplateCollection.metaTemplateList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }
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
            sb.Append( m_ReturnMetaVariable?.metaDefineType.ToFormatString() );
            sb.Append(" " + name );
            sb.Append(m_MetaMemberParamCollection.ToFormatString());
            sb.Append(Environment.NewLine);

            //for (int i = 0; i < realDeep; i++)
            //    sb.Append(Global.tabChar);
            //sb.Append("{");

            if(m_MetaBlockStatements != null )
                sb.Append(m_MetaBlockStatements.ToFormatString());

            sb.Append(Environment.NewLine);
            //for (int i = 0; i < realDeep; i++)
            //    sb.Append(Global.tabChar);
            //sb.Append("}");

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
