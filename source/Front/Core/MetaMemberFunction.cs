//****************************************************************************
//  File:      MetaMemberFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************



using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile;

using SimpleLanguage.Logging;

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
            if (this.m_MetaTemplateFunctionNodeDict.ContainsKey(mmf.metaMemberTemplateCollection.count))
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
                //Log.AddMetaCoreLog(LID.ShowExtendMessage, "发现已经定义过某某类1" + mmf.functionAllName);
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
                bool isAdd = true;
                if(mmf.metaMemberParamCollection.metaDefineParamList.Count > 0 )
                {
                    if (mmf.metaMemberParamCollection.metaDefineParamList[mmf.metaMemberParamCollection.metaDefineParamList.Count-1].isExtendParams )
                    {
                        m_MetaParamFunctionDict[mmf.metaMemberParamCollection.metaDefineParamList.Count+19] = list;
                        isAdd = false;
                    }
                }
                if( isAdd )
                {
                    m_MetaParamFunctionDict[mmf.metaMemberParamCollection.metaDefineParamList.Count] = list;
                }
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
                string oldfunctiontoken = "";
                string newfunctiontoken = "";

                if( find2.fileMetaMemberFunction?.token != null )
                {
                    oldfunctiontoken = find2.fileMetaMemberFunction?.token.ToLexemeAllString();
                }
                if( mmf.fileMetaMemberFunction?.token != null )
                {
                    newfunctiontoken = mmf.fileMetaMemberFunction?.token.ToLexemeAllString();
                }

                Log.AddMetaCoreLog(LID.MetaCoreRepeatDefineFunction, find2.fileMetaMemberFunction?.token, "AddMetaMemberFunction", oldfunctiontoken, newfunctiontoken );
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
        public List<MetaMemberFunction> GetMetaMemberFunctionByParamCount( int count )
        {
            if( m_MetaParamFunctionDict.ContainsKey( count ) )
            {
                return m_MetaParamFunctionDict[count];
            }
            return null;
        }
        public List<MetaMemberFunction> GetMetaMemberFunctionListByParamCount( int count )
        {
            List<MetaMemberFunction> list = new List<MetaMemberFunction>();
            foreach( var v in m_MetaParamFunctionDict )
            {
                if( v.Key >= count )
                {
                    list.AddRange(v.Value);
                }
            }
            return list;
        }
    }
    public class MetaMemberFunction : MetaFunction
    {
        public List<MetaAttribute> attributeList => m_AttributeList;
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
                else if( ownerMetaClass?.isTemplateClass == true ){
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
        public MetaMemberFunction overrideMetaMemberFunction => m_OverrideMetaMemberFunction;
        public bool isAbstract => m_IsAbstract;
        public bool isOverrideInterface => m_IsOverrideInterface;        
        public bool isConstructInitFunction => m_ConstructInitFunction;
        public bool isGet => m_IsGet;
        public bool isSet => m_IsSet;
        public bool isFinal => m_IsFinal;
        public bool isThrows => m_IsThrows;
        public virtual bool isStatic => m_IsStatic;
        public bool isCanRewrite => m_IsCanRewrite;
        public bool isTemplateInParam => m_IsTemplateInParam;
        public FileMetaMemberFunction fileMetaMemberFunction => m_FileMetaMemberFunction;
        public MetaMemberFunction sourceMetaMemberFunction => m_SourceMetaMemberFunction;
        public List<MetaType> bindStructTemplateFunctionMtList => m_BindStructTemplateFunctionMtList;
        public List<MetaType> bindStructTemplateFunctionAndClassMtList => m_BindStructTemplateFunctionAndClassMtList;
        public List<MetaGenTemplateFunction> genTempalteFunctionList => m_GenTempalteFunctionList;



        #region 属性
        protected bool m_IsTemplateFunction = false;
        protected bool m_IsOverrideFunction = false;
        protected bool m_IsOverrideInterface = false;
        protected bool m_IsAbstract = false;
        protected bool m_IsGet = false;
        protected bool m_IsSet = false;
        protected bool m_IsStatic = false;
        protected bool m_IsFinal = false;
        protected bool m_IsThrows = false;
        protected bool m_IsCanRewrite = false;
        protected bool m_IsTemplateInParam = false;
        protected bool m_ConstructInitFunction = false;
        protected bool m_IsWithInterface = false;
        protected MetaMemberFunction m_SourceMetaMemberFunction = null; //模板里边的源函数
        protected MetaMemberFunction m_OverrideMetaMemberFunction = null;           //override member function的函数

        protected FileMetaMemberFunction m_FileMetaMemberFunction = null;

        private readonly List<MetaAttribute> m_AttributeList = new List<MetaAttribute>();

        // ── Scope validation context ──
        // Tracks whether we're currently processing statements inside a label{} block
        // (try-catch scope) or a checked scope. Used to enforce:
        //   - try/checked expressions only inside label{} or checked label{}
        //   - unchecked{} only inside checked{} or checked label{}
        public static bool isInTryBlock => s_IsInTryBlock;
        public static bool isInCheckedContext => s_IsInCheckedContext;
        private static bool s_IsInTryBlock = false;
        private static bool s_IsInCheckedContext = false;
        //绑定构建 元类型  
        protected List<MetaType> m_BindStructTemplateFunctionMtList = new List<MetaType>();
        protected List<MetaType> m_BindStructTemplateFunctionAndClassMtList = new List<MetaType>();

        //模板生成函数，如果匹配了，模板函数后，再进行看是否生成过该函数
        protected List<MetaGenTemplateFunction> m_GenTempalteFunctionList = new List<MetaGenTemplateFunction>();
        #endregion

        public MetaMemberFunction( MetaClass mc ):base(mc)
        {

        }

        // Lightweight builtin wrapper for functions provided by the LocalRuntimeVM (native lib)
        public class MetaBuiltinFunction : MetaMemberFunction
        {
            public MetaBuiltinFunction(MetaClass mc, SystemMethodCallDeclaration decl ) : base(mc)
            {
                this.m_Name = decl.name;
                this.m_IsStatic = true;
                m_Index = (int)decl.Index();
                m_MetaMemberParamCollection.Clear();
                for (int i = 0; i < decl.paramMetaTypeList.Count; i++)
                {
                    var p = new MetaDefineParam("p" + i.ToString(), this);
                    p.SetDefineMetaType(new MetaType(decl.paramMetaTypeList[i]));
                    m_MetaMemberParamCollection.AddMetaDefineParam(p);
                }

                var ret = new MetaType(decl.returnMetaType);
                m_IsDefineMetaType = true;
                m_DefineMetaType = ret;
                m_RealMetaType = new MetaType(ret);

                if (m_ReturnMetaVariable != null)
                {
                    m_ReturnMetaVariable.SetMetaDefineType(new MetaType(ret));
                    m_ReturnMetaVariable.SetRealMetaType(new MetaType(ret));
                    m_ReturnMetaVariable.SetIsDefineMetaType(true);
                }
            }
        }
        public MetaMemberFunction( MetaClass mc, FileMetaMemberFunction fmmf):base( mc )
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(true, false);
            m_FileMetaMemberFunction = fmmf;
            this.m_Name = fmmf.name;
            m_Token = fmmf?.token;

            m_IsStatic = fmmf.staticToken != null;
            bool isProjectSpecialClass = string.Equals(mc?.name, "Project", StringComparison.OrdinalIgnoreCase)
                && fmmf?.fileMeta?.path?.EndsWith(".sp", StringComparison.OrdinalIgnoreCase) == true;
            if (isProjectSpecialClass)
            {
                // Project{}.sp methods are treated as static by default.
                m_IsStatic = true;
            }
            m_IsGet = fmmf.getToken != null;
            m_IsSet = fmmf.setToken != null;
            m_IsFinal = fmmf.finalToken != null;
            m_IsThrows = fmmf.throwsToken != null;
            m_IsAbstract = fmmf.abstractToken != null;
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
                        Log.AddMetaCoreLog( LID.ShowExtendMessage, "Error 没有查找到inClass的类名, " + inClassToken.ToFormatString());
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
            if(mc.isInterfaceClass )
            {
                m_IsOverrideInterface = true;
            }

            if (fmmf.attributeList != null && fmmf.attributeList.Count > 0)
            {
                for (int i = 0; i < fmmf.attributeList.Count; i++)
                {
                    m_AttributeList.Add(new MetaAttribute(fmmf.attributeList[i]));
                }
            }

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
            m_IsSet = mmf.m_IsSet;
            m_IsGet = mmf.m_IsGet;
            m_IsOverrideFunction = mmf.m_IsOverrideFunction;
            m_IsOverrideInterface = mmf.isOverrideInterface;
            m_IsAbstract = mmf.isAbstract;
            m_IsFinal = mmf.isFinal;
            m_IsStatic = mmf.isStatic;
        }
        /// <summary>
        /// Clears the FileMetaMemberFunction binding so that ParseRealMetaType/ParseStatements
        /// skip re-parsing from source file data. Used by gen template copies and reference-loaded methods.
        /// </summary>
        public void ClearFileMetaMemberFunction()
        {
            m_FileMetaMemberFunction = null;
            m_CanParse = false;
        }
        protected void Init()
        {
            m_ConstructInitFunction = this.m_Name == "_init_";

            MetaType defineMetaType = null;
            if (m_ConstructInitFunction)
            {
                defineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            else
            {
                // 没有显式声明返回类型的函数，默认返回 void
                defineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if( isSet && !isGet )
            {
                defineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if (!isStatic)
            {
                MetaClass omc = ownerMetaClass;
                if (omc != null)
                {
                    var mt = new MetaType(omc);
                    if (omc.isTemplateClass)
                    {
                        var thisTemplateArgs = new List<MetaType>();
                        for (int i = 0; i < omc.metaTemplateList.Count; i++)
                        {
                            var ct = omc.metaTemplateList[i];
                            if (ct == null) continue;
                            thisTemplateArgs.Add(new MetaType(ct, ct.name));
                        }
                        mt = new MetaType(omc, thisTemplateArgs);
                    }
                    m_ThisMetaVariable = new MetaVariable(omc.allName + "." + m_Name + ".this", MetaVariable.EVariableFrom.Argument, null, omc, mt );
                }
            }
            {
                string qn = m_OwnerMetaClass is MetaClass c ? c.allName
                    : m_OwnerMetaClass is MetaData md ? md.allName
                    : m_OwnerMetaClass is MetaEnum me ? me.allName
                    : m_OwnerMetaClass?.name ?? "?";
                m_ReturnMetaVariable = new MetaVariable(qn + "." + m_Name + ".return", MetaVariable.EVariableFrom.None, null, m_OwnerMetaClass, defineMetaType );
            }
        }
        public override void SetDeep(int deep)
        {
            base.SetDeep(deep);
        }
        public override void SetAnchorDeep(int addep)
        {
            base.SetAnchorDeep(addep);
        }
        public void SetOverrideMetaMemberFunction( MetaMemberFunction mmf )
        {
            this.m_OverrideMetaMemberFunction = mmf;
        }
        public void SetSourceMetaMemberFunction( MetaMemberFunction mmf )
        {
            this.m_SourceMetaMemberFunction = mmf;
        }
        public void SetIsStatic(bool isStatic)
        {
            m_IsStatic = isStatic;
        }
        public void SetIsGet(bool isGet)
        {
            m_IsGet = isGet;
        }
        public void SetIsSet(bool isSet)
        {
            m_IsSet = isSet;
        }
        public void SetIsFinal(bool flag)
        {
            m_IsFinal = flag;
        }
        public void SetIsAbstract(bool flag)
        {
            m_IsAbstract = flag;
        }
        public void SetIsOverrideFunction(bool flag )
        {
            m_IsOverrideFunction = flag;
        }
        public void SetIsOverrideInterface(bool flag )
        {
            this.m_IsOverrideInterface = flag;
        }
        public void SetIsTemplateFunction(bool flag)
        {
            this.m_IsTemplateFunction = flag;
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
        public MetaGenTemplateFunction AddGenTemplateMemberFunctionByMetaTypeList(MetaClass mc, List<MetaType> list)
        {
            if (mc.isTemplateClass && !mc.isGenTemplate )
            {
                // 如果模板参数本身是模板类型（如函数级T绑定到类级T），允许实例化
                bool hasTemplateTypeArg = false;
                foreach (var mt in list)
                {
                    if (mt.isTemplate)
                    {
                        hasTemplateTypeArg = true;
                        break;
                    }
                }
                if (hasTemplateTypeArg)
                {
                    return null;
                }
            }

            return AddGenTemplateMemberFunctionBySelf(mc, list);
        }
        public MetaGenTemplateFunction AddGenTemplateMemberFunctionBySelf( MetaClass mc, List<MetaType> mtList)
        {
            MetaGenTemplateFunction mgtf = GetGenTemplateFunction(mtList);
            if (mgtf == null)
            {
                List<MetaGenTemplate> mgtList = new List<MetaGenTemplate>(mtList.Count);
                for (int i = 0; i < mtList.Count; i++)
                {
                    var l1 = this.m_MetaMemberTemplateCollection.metaTemplateList[i];
                    MetaGenTemplate mgt = new MetaGenTemplate(l1, new MetaType(mtList[i]));
                    mgtList.Add(mgt);
                }
                mgtf = new MetaGenTemplateFunction(this, mgtList);
                mgtf.SetOwnerMetaClass(mc);

                this.m_GenTempalteFunctionList.Add(mgtf);

                mgtf.Parse();
            }
            return mgtf;
        }
        public MetaGenTemplateFunction GetGenTemplateFunction(List<MetaType> mtList)
        {
            if( mtList.Count == m_GenTempalteFunctionList.Count )
            {
                for (int i = 0; i < m_GenTempalteFunctionList.Count; i++)
                {
                    var c = m_GenTempalteFunctionList[i];

                    if (c.MatchInputTemplateInsance(mtList))
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
            // ref module 导入的函数类型已在导入时设置完毕，无需从 FileMeta 解析，
            // 但仍需走到 UpdateVritualFunctionName 设置虚函数名
            if (refFromType != RefFromType.RefModule)
            {
                if (this.m_FileMetaMemberFunction != null)
                {
                    if (m_FileMetaMemberFunction.defineMetaClass != null)
                    {
                        FileMetaClassDefine cmr = m_FileMetaMemberFunction.defineMetaClass;
                        m_DefineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, this, cmr);
                        m_IsDefineMetaType = true;

                        if (m_DefineMetaType == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, this.m_FileMetaMemberFunction.token, $"没有找到{cmr.stringList[0]} 的相关返回类型!");
                            return;
                        }
                        if (m_ConstructInitFunction && defineMetaType.metaClass != CoreMetaClassManager.voidMetaClass )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 当前类:" + m_AllName + " 是构建Init类，不允许有返回类型 ");
                        }
                        else
                        {
                            m_ReturnMetaVariable.SetMetaDefineType(defineMetaType);
                            m_ReturnMetaVariable.SetRealMetaType(defineMetaType);
                        }
                        m_ReturnMetaVariable.SetMetaDefineType(m_DefineMetaType);
                        m_ReturnMetaVariable.SetRealMetaType(new MetaType(m_DefineMetaType));
                    }
                    else
                    {
                        if( m_IsSet )
                        {
                            m_DefineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
                        }
                        else
                        {
                            // 没有显式声明返回类型的函数，默认返回 void
                            m_DefineMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
                        }
                        m_IsDefineMetaType = true;
                        m_ReturnMetaVariable.SetRealMetaType(new MetaType(m_DefineMetaType));
                    }
                }
                for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
                {
                    MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                    mpl.ParseMetaDefineType();
                }
            }
            UpdateVritualFunctionName();
        }
        public bool ParseInterface()
        {
            return true;
        }
        public void ParseRealMetaType()
        {
            // ref module 导入的函数参数类型已在导入时设置完毕，无需再解析
            if (refFromType == RefFromType.RefModule)
                return;

            /* Skip reference-loaded methods: they have no FileMetaParamter/express,
              * defineMetaType and realMetaType are already set during module loading. */
            if (m_FileMetaMemberFunction == null)
            {
                if (m_MetaMemberParamCollection != null)
                {
                    bool allHaveTypes = true;
                    for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
                    {
                        var mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                        if (mpl.metaVariable?.defineMetaType == null)
                        {
                            allHaveTypes = false;
                            break;
                        }
                    }
                    if (allHaveTypes) return;
                }
            }

            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                mpl.CreateExpress();
                mpl.Parse();
                mpl.CaleReturnType();
            }

        }
        public void ParseStatements()
        {
            // ref module 导入的函数没有源码语法树，无需解析语句
            if (refFromType == RefFromType.RefModule)
                return;
            if (!m_CanParse) return;

            // If this function is declared abstract, skip parsing its body/content.
            if (m_IsAbstract)
            {
                return;
            }
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
            if( !m_IsWithInterface || ownerMetaClass?.isInterfaceClass == true )
            {
            }
            else
            {
                if (nohasContent)
                {
                    string ownerLabel = m_OwnerMetaClass is MetaClass x ? x.allName
                        : m_OwnerMetaClass is MetaData d ? d.allName
                        : m_OwnerMetaClass is MetaEnum e ? e.allName
                        : m_OwnerMetaClass?.name;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 类[{ownerLabel}] 该函数[{this.functionAllName}] 没有定义函数内容！！");
                }
            }

            // 处理完statements后，检查非void返回类型的函数是否所有代码路径都有ret返回
            // 跳过构造函数和没有函数体的函数
            if( !m_ConstructInitFunction && !nohasContent )
            {
                CheckAllPathsReturn();
            }
        }
        public void UpdateVritualFunctionName()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_Name);
            sb.Append("_");
            sb.Append(m_ReturnMetaVariable.defineMetaType.ToString() );
            sb.Append("_");
            sb.Append(m_MetaMemberParamCollection.maxParamCount);
            if(m_MetaMemberParamCollection.maxParamCount > 0 )
            {
                sb.Append("_");
                for (int i = 0; i < this.m_MetaMemberParamCollection.maxParamCount; i++)
                {
                    var mdp = m_MetaMemberParamCollection.metaDefineParamList[i];
                   
                    sb.Append(mdp.metaVariable.defineMetaType?.ToString());
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
                if (mtc.eMetaTypeType == EMetaTypeType.MetaClass)
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
            for (int i = 0; i < mt.defineTemplateMetaTypeList.Count; i++)
            {
                var mtc = mt.defineTemplateMetaTypeList[i];
                if (mtc.eMetaTypeType == EMetaTypeType.MetaClass)
                {
                    mcList.Add(mtc.metaClass);
                }
            }
            if (mcList.Count == mt.defineTemplateMetaTypeList.Count)
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
                if (TypeManager.CompareMetaType(v, mt))
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
                if (TypeManager.CompareMetaType(v, mt))
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
                        beforeStatements = cms;
                        //createBlockStatements.SetEndJumMetaStatements(cms);
                    }
                    break;
                case FileMetaKeyIfSyntax fmkis:
                    {
                        var metaIfStatements = new MetaIfStatements(currentBlockStatements, fmkis);
                        beforeStatements.SetNextStatements( metaIfStatements );
                        beforeStatements = metaIfStatements;
                    }
                    break;
                case FileMetaKeyTrySyntax fmts:
                    {
                        // Set scope context: inside label{} block, and optionally checked
                        bool savedTry = s_IsInTryBlock;
                        bool savedChecked = s_IsInCheckedContext;
                        s_IsInTryBlock = true;
                        if (fmts.isChecked) s_IsInCheckedContext = true;
                        var metaTryStatements = new MetaTryStatements(currentBlockStatements, fmts);
                        s_IsInTryBlock = savedTry;
                        s_IsInCheckedContext = savedChecked;
                        beforeStatements.SetNextStatements(metaTryStatements);
                        beforeStatements = metaTryStatements;
                    }
                    break;
                case FileMetaKeyThrowSyntax fmtks:
                    {
                        var metaThrowStatements = new MetaThrowStatements(currentBlockStatements, fmtks);
                        beforeStatements.SetNextStatements(metaThrowStatements);
                        beforeStatements = metaThrowStatements;
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error FileMetaConditionExpressSyntax: 暂不支持该类型的解析!!");
                        }
                    }
                    break;
                case FileMetaKeyOnlySyntax fmoks:
                    {
                        if (fmoks.token.type == ETokenType.Defer)
                        {
                            var metaDeferStatements = new MetaDeferStatements(currentBlockStatements, fmoks);
                            currentBlockStatements.ownerMetaFunction?.AddDeferStatements(metaDeferStatements);
                            beforeStatements.SetNextStatements(metaDeferStatements);
                            beforeStatements = metaDeferStatements;
                        }
                        else if (fmoks.token.type == ETokenType.ErrDefer)
                        {
                            var metaErrDeferStatements = new MetaErrDeferStatements(currentBlockStatements, fmoks);
                            currentBlockStatements.ownerMetaFunction?.AddErrDeferStatements(metaErrDeferStatements);
                            beforeStatements.SetNextStatements(metaErrDeferStatements);
                            beforeStatements = metaErrDeferStatements;
                        }
                        else if (fmoks.token.type == ETokenType.Checked)
                        {
                            // Set checked context for the block body
                            bool savedChecked = s_IsInCheckedContext;
                            s_IsInCheckedContext = true;
                            var metaCheckedStatements = new MetaCheckedStatements(currentBlockStatements, fmoks);
                            s_IsInCheckedContext = savedChecked;
                            beforeStatements.SetNextStatements(metaCheckedStatements);
                            beforeStatements = metaCheckedStatements;
                        }
                        else if (fmoks.token.type == ETokenType.Unchecked)
                        {
                            // unchecked{} can only be used inside a checked context
                            if (!s_IsInCheckedContext)
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, fmoks.token,
                                    "Error: unchecked{} 只能在 checked 上下文中使用 (checked label{} 或 checked{})");
                            }
                            var metaUncheckedStatements = new MetaUncheckedStatements(currentBlockStatements, fmoks);
                            beforeStatements.SetNextStatements(metaUncheckedStatements);
                            beforeStatements = metaUncheckedStatements;
                        }
                        else if (fmoks.token.type == ETokenType.Break)
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
                        else if (fmoks.token.type == ETokenType.Next)
                        {
                            var metaNextStatements = new MetaNextStatements(currentBlockStatements, fmoks);
                            beforeStatements.SetNextStatements(metaNextStatements);
                            beforeStatements = metaNextStatements;
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
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 如果使用了var/data/dynamic/int 等前缀，有重复定义的行为" + fmos.variableRef.ToTokenString());
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
                            //    Log.AddMetaCoreLog( LID.ShowExtendMessage, "Error 构造函数中，不允许使用定义字段，必须使用this.非静态或者是类名.静态字段赋值!" + fmos.variableRef.ToTokenString());
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, fmvs.token, "Error 定义变量名称与类函数临时名称一样!!" + fmvs.token?.ToLexemeAllString());
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
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 定义变量名称与类定义名称一样 如果调用成员变量，需要在前边使用this.!!" + fmvs.token?.ToLexemeAllString());
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
                        var mcs = new MetaCallStatements(currentBlockStatements, fmcs );
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 生成MetaStatements出错KeyReturnSyntax类型错误!!");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Waning 还有没有解析的语句!! MetaMemberFunction 314");
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
                sb.Append(m_ReturnMetaVariable.defineMetaType.ToFormatString());
            }
            sb.Append(" "); 
            
            if (m_OwnerMetaClass != null)
            {
                sb.Append(m_OwnerMetaClass is MetaClass c ? c.allName
                    : m_OwnerMetaClass is MetaData md ? md.allName
                    : m_OwnerMetaClass is MetaEnum me ? me.allName
                    : m_OwnerMetaClass.name);
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
            if (isThrows)
            {
                sb.Append(" throws");
            }
            sb.Append(" ");
            sb.Append( m_ReturnMetaVariable?.GetFinalMetaType().ToFormatString() );
            sb.Append(" " + name );
            sb.Append(m_MetaMemberParamCollection.ToFormatString());
            sb.Append(Environment.NewLine);

            //for (int i = 0; i < realDeep; i++)
            //    sb.Append(Global.tabChar);
            //sb.Append("{");

            if(m_MetaBlockStatements != null )
                sb.Append(this.m_MetaBlockStatements.ToFormatString());

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
