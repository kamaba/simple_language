//****************************************************************************
//  File:      MetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Meta class's attribute
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Linq;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public enum EClassDefineType
    {
        StructDefine,
        InnerDefine,
        CodeDefine
    }
    public partial class MetaClass : MetaBase
    {
        public MetaNamespace topLevelMetaNamespace
        {
            get
            {
                if (parentNode == null) return null;
                return parentNode as MetaNamespace;
            }
        }
        public MetaClass topLevelMetaClass
        {
            get
            {
                if (parentNode == null) return null;
                return parentNode as MetaClass;
            }
        }
        public List<MetaClass> metaClassList
        {
            get
            {
                List<MetaClass> list = new List<MetaClass>();
                foreach (var v in m_ChildrenMetaClassDict.Values)
                {
                    list.Add(v);
                }
                return list;
            }
        }
        public virtual string allClassName=> this.allName ;
        public virtual string className => this.name;

        public EType eType => m_Type;
        public EClassDefineType classDefineType => m_ClassDefineType;
        public MetaClass extendClass => m_ExtendClass;
        public int extendLevel => m_ExtendLevel;
        public List<MetaClass> interfaceClass => m_InterfaceClass;
        public MetaExpressNode defaultExpressNode => m_DefaultExpressNode;
        public Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict
        {
            get
            {
                Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>(m_MetaMemberVariableDict);
                allMetaMemberVariableDict = allMetaMemberVariableDict.Concat(m_MetaExtendMemeberVariableDict).ToDictionary(k => k.Key, v => v.Value);
                return allMetaMemberVariableDict;
            }
        }
        public List<MetaMemberVariable> allMetaMemberVariableList
        {
            get
            {
                List<MetaMemberVariable> allMetaMemberVariableList = new List< MetaMemberVariable>(m_MetaMemberVariableDict.Count + m_MetaExtendMemeberVariableDict.Count);

                foreach (var v in allMetaMemberVariableDict)
                {
                    allMetaMemberVariableList.Add(v.Value);
                }
                return allMetaMemberVariableList;
            }
        }

        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        public Dictionary<string, MetaMemberVariable> metaExtendMemeberVariableDict => m_MetaExtendMemeberVariableDict;
        public Dictionary<Token, FileMetaClass> fileMetaClassDict => m_FileMetaClassDict;
        //public Dictionary<string, List<MetaMemberFunction>> metaMemberFunctionListDict => m_MetaMemberFunctionListDict;
        public bool isHandleExtendVariableDirty { get; set; } = false;

        protected int m_ExtendLevel = 0;
        protected EType m_Type = EType.None;
        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass  = null;
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();
        protected Dictionary<string, MetaClass> m_ChildrenMetaClassDict = new Dictionary<string, MetaClass>();
        protected Dictionary<int, MetaClass> m_MetaTemplateClassDict = new Dictionary<int, MetaClass>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunction> m_MetaMemberAllNameFunctionDict = new Dictionary<string, MetaMemberFunction>();
        protected Dictionary<string, List<MetaMemberFunction>> m_MetaMemberFunctionListDict = new Dictionary<string, List<MetaMemberFunction>>();
        protected List<MetaMemberFunction> m_TempInnerFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected MetaExpressNode m_DefaultExpressNode = null;
        protected EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;

        protected MetaClass()
        {

        }
        public MetaClass(string _name, EClassDefineType ecdt )
        {
            m_Name = _name;
            m_Type = EType.Class;
            m_ClassDefineType = ecdt;
        }

        public MetaClass(string _name, EType _type  = EType.Class )
        {
            m_Name = _name;
            m_Type = _type;
        }
        public MetaClass( MetaClass mc )
        {
            m_Name = mc.m_Name;
            m_Type = mc.m_Type;
            m_FileMetaClassDict = mc.m_FileMetaClassDict;
            m_ExtendClass = mc.m_ExtendClass;
            m_ExtendLevel = m_ExtendClass.m_ExtendLevel + 1;
            m_InterfaceClass = mc.m_InterfaceClass;
            m_ChildrenMetaClassDict = mc.m_ChildrenMetaClassDict;

            m_MetaMemberVariableDict = mc.m_MetaMemberVariableDict;
            m_MetaExtendMemeberVariableDict = mc.m_MetaExtendMemeberVariableDict;
            m_MetaMemberFunctionListDict = mc.m_MetaMemberFunctionListDict;
            m_MetaMemberAllNameFunctionDict = mc.m_MetaMemberAllNameFunctionDict;
            m_DefaultExpressNode = mc.m_DefaultExpressNode;
        }
        public override void SetDeep( int deep )
        {
            m_Deep = deep;

            foreach( var v in m_ChildrenMetaClassDict )
            {
                v.Value.SetDeep(deep + 1);
            }

            foreach( var v in m_MetaMemberVariableDict )
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach( var v in m_MetaMemberAllNameFunctionDict)
            {
                v.Value.SetDeep(deep + 1);
            }
        }
        public void SetDefaultExpressNode( MetaExpressNode defaultExpressNode )
        {
            m_DefaultExpressNode = defaultExpressNode;
        }
        public virtual void ParseInnerVariable()
        {
        }
        public virtual void ParseInnerFunction()
        {
        }
        public virtual void ParseInner()
        {
            ParseInnerVariable();
            ParseInnerFunction();
        }
        public void SetClassDefineType( EClassDefineType ecdt )
        {
            var type = typeof(MetaClass);

            this.m_ClassDefineType = ecdt;
        }
        public void ParseExtendsRelation()
        {
            if( this.classDefineType == EClassDefineType.InnerDefine )
            {
                return;
            }
            if (this.extendClass != null)
            {
                Debug.Write("已绑定过了继承类 : " + extendClass.name);
                return;
            }
            foreach( var v in m_FileMetaClassDict )
            {
                var mc = v.Value;
                MetaClass getmc = GetExtendMetaClass(mc);
                if (getmc != null)
                {
                    if( mc.templateDefineList.Count > 0 )
                    {

                    }
                    mc.metaClass.SetExtendClass(getmc);
                }
                else
                {
                    mc.metaClass.SetExtendClass(CoreMetaClassManager.objectMetaClass);
                }
            }
        }
        public MetaClass GetExtendMetaClass(FileMetaClass fmc )
        {
            if (fmc.extendClass != null)
            {
                MetaClass getmc = ClassManager.instance.GetMetaClassByRef( this, fmc.extendClass );
                if (getmc == null)
                {
                    //Debug.Write(" CheckExtendAndInterface 在判断继承的时候，发没的:" + m_ExtendClass.allName + "  类"
                    //    + "位置行: " + m_ExtendClass.token.sourceBeginLine.ToString() );


                    fmc.extendClass.AddError2(0);
                }
                return getmc;
            }
            return null;
        }

        public virtual void HandleExtendData()
        {
            if(m_ExtendClass == null )
            {
                return;
            }
            foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
            {
                var c = v.Value;
                if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                {
                    var ld = Log.AddInStructMeta( EError.None, $"Error 继承的类123:{allName} 在继承的父类{m_ExtendClass.allName} 中已包含:{c.name} " );
                    ld.valDict.Add(EMetaType.MetaClass, this );
                    ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                    ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                    continue;
                }
                this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
            }
            foreach (var v in m_ExtendClass.m_MetaExtendMemeberVariableDict )
            {
                var c = v.Value;
                if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                {
                    Debug.WriteLine($"Error 继承的类321:{allName} 在继承的父类{m_ExtendClass.allName} 中已包含:{c.name} ");
                    continue;
                }
                this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
            }
        }
        public void ParseTemplateRelation()
        {
        }
        public void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in m_MetaMemberVariableDict)
            {
                it.Value.ParseDefineMetaType();
            }
        }
        public void ParseMemberFunctionDefineMetaType()
        {
            foreach (var it in m_MetaMemberFunctionListDict)
            {
                foreach( var it2 in it.Value )
                {
                    it2.ParseDefineMetaType();
                }
            }
        }
        public bool CheckInterface()
        {
            return true;
        }
#if EditorMode
        public void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass( this );
            m_FileMetaClassDict.Add(fmc.token, fmc);
        }
        public void ParseFileMetaClassMemeberVarAndFunc( FileMetaClass fmc )
        {
            bool isHave = false;
            foreach (var v2 in fmc.memberVariableList)
            {
                MetaBase mb = GetChildrenMetaBaseByName(v2.name);
                if (mb != null)
                {
                    MetaMemberVariable cmmv = mb as MetaMemberVariable;
                    if(cmmv != null && cmmv.isInnerDefine )
                    {
                        break;
                    }
                    else
                    {
                        Debug.Write("Error MetaClass MemberVarAndFunc已有定义类: " + allName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    }
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable(this, v2);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberVariable(mmv);
            }
            foreach (var v2 in fmc.memberFunctionList)
            {
                MetaMemberFunction mmf = new MetaMemberFunction(this, v2 );
                AddMetaMemberFunction(mmf);
                //原生函数，只添加 非模板类的，非模板函数的
                if( !mmf.isTemplateFunction )
                {
                    MethodManager.instance.AddOriginalMemeberFunction(mmf);
                }
            }            
        }
        //解析 自动构建函数  
        public virtual void ParseDefineComplete()
        {
            AddDefineConstructFunction();
            //AddDefineInstanceValue();

            if (m_DefaultExpressNode == null )
            {
                MetaType mdt = new MetaType(this);
                var defaultFunction = GetMetaMemberConstructDefaultFunction();
                MetaMethodCall mfc = null;
                if (defaultFunction != null)
                {
                    mfc = new MetaMethodCall(this, defaultFunction );
                }
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, mfc);
            }

            List<MetaMemberFunction> addList = new List<MetaMemberFunction>();
            for( int i = 0; i < m_TempInnerFunctionList.Count; i++ ) 
            {
                MetaMemberFunction mmf = m_TempInnerFunctionList[i];

                bool isAdd = true;
                if (m_MetaMemberFunctionListDict.ContainsKey(mmf.name))
                {
                    var list = m_MetaMemberFunctionListDict[mmf.name];
                    MetaMemberFunction curFun = IsSameMetaMemeberFunction(list, mmf);
                    if (curFun != null)
                    {
                        isAdd = false;
                        if (mmf.isCanRewrite)
                        {
                            int index = list.IndexOf(curFun);
                            list[index] = mmf;
                        }
                        else
                        {
                            RemoveMetaMemberFunction(curFun);
                            isAdd = true;
                            break;
                        }
                    }
                }                
                if (isAdd)
                {
                    addList.Add(mmf);
                }
            }
            for( int i = 0; i < addList.Count; i++ )
            {
                AddMetaMemberFunction(addList[i]);
            }
            m_TempInnerFunctionList.Clear();
        }
#endif
        public void AddChildrenMetaClass(MetaClass mc)
        {
            if ( m_ChildrenMetaClassDict.ContainsKey(mc.className))
            {
                return;
            }
            m_ChildrenMetaClassDict.Add(mc.className, mc);
            AddMetaBase(mc.className, mc);
        }
        public MetaClass GetChildrenMetaClass( string name )
        {
            if( m_ChildrenMetaClassDict.ContainsKey( name ) )
            {
                return m_ChildrenMetaClassDict[name];
            }
            return null;
        }
        public MetaGenTemplateClass GetGenTemplateMetaClassIfNotThenGenTemplateClass(MetaInputTemplateCollection mtic )
        {
            MetaGenTemplateClass mtc = GetGenTemplateMetaClass(mtic);
            if( mtc == null )
            {
                mtc = MetaGenTemplateClass.GenerateTemplateClass(this, mtic);
                ClassManager.instance.AddGenTemplateClass(mtc);
            }
            if( mtc == null )
            {
                Debug.Write("Error 没有找到合适的Template");
            }
            return mtc;
        }
        public MetaClass GetChildrenMetaClassByName( string name )
        {
            if( m_ChildrenMetaClassDict.ContainsKey(name ) )
            {
                return m_ChildrenMetaClassDict[name];
            }
            return null;
        }
        public void SetExtendClass(MetaClass sec)
        {
            m_ExtendClass = sec;
            m_ExtendLevel = m_ExtendClass.m_ExtendLevel + 1;
        }
        public bool IsParseMetaClass(MetaClass parentClass, bool isIncludeSelf = true )
        {
            MetaClass mc = isIncludeSelf ? this : this.m_ExtendClass;
            while( mc != null )
            {
                if (mc == CoreMetaClassManager.objectMetaClass)
                    break;
                
                if( mc == parentClass)
                {
                    return true;
                }
                mc = mc.m_ExtendClass;
            }
            return false;
        }
        public void AddInterfaceClass(MetaClass aic)
        {
            if (!m_InterfaceClass.Contains(aic))
            {
                m_InterfaceClass.Add(aic);
            }
        }
        public void AddMetaMemberVariable( MetaMemberVariable mmv, bool isAddManager = true )
        {
            if( m_MetaMemberVariableDict.ContainsKey( mmv.name ) )
            {
                return;
            }
            m_MetaMemberVariableDict.Add(mmv.name, mmv);
            AddMetaBase(mmv.name, mmv);
            if( isAddManager )
            {
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
        }
        public MetaMemberFunction IsSameMetaMemeberFunction(List<MetaMemberFunction> list, MetaMemberFunction mmf)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var curFun = list[i];
                if( curFun.metaMemberParamCollection.IsEqualMetaDefineParamCollection( mmf.metaMemberParamCollection ) )
                {
                    return curFun;
                }
            }
            return null;
        }
        public void AddInnerMetaMemberFunction( MetaMemberFunction mmf )
        {
            m_TempInnerFunctionList.Add(mmf);
        }
        public void AddMetaMemberFunction(MetaMemberFunction mmf )
        {
            if(this.m_MetaMemberFunctionListDict.ContainsKey(mmf.name ) )
            {
                var list = m_MetaMemberFunctionListDict[mmf.name];

                if( IsSameMetaMemeberFunction(list, mmf) == null )
                {
                    list.Add(mmf);
                    m_MetaMemberAllNameFunctionDict.Add(mmf.functionAllName, mmf);
                }
            }
            else
            {
                var list = new List<MetaMemberFunction>();
                list.Add(mmf);
                m_MetaMemberFunctionListDict.Add(mmf.name, list);
                m_MetaMemberAllNameFunctionDict.Add(mmf.functionAllName, mmf);
            }
            AddMetaBase(mmf.functionAllName, mmf);
        }
        public void RemoveMetaMemberFunction( MetaMemberFunction mmf )
        {
            if (m_MetaMemberFunctionListDict.ContainsKey(mmf.name))
            {
                var list = m_MetaMemberFunctionListDict[mmf.name];

                list.Remove(mmf);
            }
            RemoveMetaBase(mmf);

            //MethodManager.instance.AddMemeberFunction(mmf);
        }
        public void AddDefineConstructFunction()
        {
            //MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
            //if (mmf == null)
            //{
            //    mmf = new MetaMemberFunction(this, "_init_");
            //    mmf.SetDefineMetaClass(this);
            //    AddMetaMemberFunction(mmf);
            //}
        }
        public void AddDefineInstanceValue()
        {
            MetaMemberVariable mmv = this.GetChildrenMetaBaseByName( "instance" ) as MetaMemberVariable;
            if (mmv == null)
            {
                mmv = new MetaMemberVariable(this, "instance");
                mmv.SetDefineMetaClass(this);
                AddMetaMemberVariable(mmv);
            }
        }
        public void HandleExtendClassVariable()
        {
            isHandleExtendVariableDirty = true;
            if ( m_ExtendClass != null)
            {
                foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
                {
                    m_MetaExtendMemeberVariableDict.Add(v.Key,v.Value);
                }
            }
        }
        public virtual MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaExtendMemeberVariableDict.ContainsKey(name))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            return null;
        }
        public List<MetaMemberVariable> GetMetaMemberVariableListByFlag( bool isStatic, bool isConst )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            MetaMemberVariable tempMmv = null;
            foreach (var v in m_MetaMemberVariableDict)
            {
                tempMmv = v.Value;
                if( (tempMmv.isStatic == isStatic) && (tempMmv.isConst==isConst) )
                {
                    mmvList.Add(tempMmv);
                }
            }
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                tempMmv = v.Value;
                if ((tempMmv.isStatic == isStatic) && (tempMmv.isConst == isConst))
                {
                    mmvList.Add(tempMmv);
                }
            }
            return mmvList;
        }
        public virtual MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName( string name, bool isGet , bool isSet )
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if (m_ExtendClass != null)
                {
                    var func = m_ExtendClass.GetMetaDefineGetSetMemberFunctionByName(name, isGet, isSet);
                    if (func != null)
                    {
                        return func;
                    }
                }
                return null;
            }
            var mmf = m_MetaMemberFunctionListDict[name];

            for (int i = 0; i < mmf.Count; i++)
            {
                var fun = mmf[i];
                if ( (fun.isGet == isGet) || (fun.isSet == isSet ) )
                    return fun;
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputParamCollect(string name, MetaInputParamCollection mmpc, bool isIncludeExtendClass = true )
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if (isIncludeExtendClass  && m_ExtendClass != null )
                {
                    var func = m_ExtendClass.GetMetaMemberFunctionByNameAndInputParamCollect(name, mmpc, isIncludeExtendClass );
                    if (func != null)
                    {
                        return func;
                    }
                }
                return null;
            }
            
            var mmf = m_MetaMemberFunctionListDict[name];
            for (int i = 0; i < mmf.Count; i++)
            {
                var fun = mmf[i];
                if (fun.IsEqualMetaInputParamCollection(mmpc))
                    return fun;
            }

            return null;
        }
        //该方法，只能查找Cast<T1>() 模版函数使用 不能用Class<T>{ Fun() } 这种的
        //暂不支持使用模版方法的查找
        //public MetaMemberFunction GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(string name, MetaInputTemplateCollection mitc, MetaInputParamCollection mmpc)
        //{
        //    if (!m_MetaMemberFunctionListDict.ContainsKey(name))
        //    {
        //        if ( m_ExtendClass != null)
        //        {
        //            var func = m_ExtendClass.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(name, mitc, mmpc);
        //            if (func != null)
        //            {
        //                return func;
        //            }
        //        }
        //        return null;
        //    }


        //    var mmf = m_MetaMemberFunctionListDict[name];

        //    for (int i = 0; i < mmf.Count; i++)
        //    {
        //        var fun = mmf[i];
        //        if (fun.IsEqualMetaTemplateCollectionAndMetaParamCollection(mitc, mmpc))
        //            return fun;
        //    }
        //    return null;
        //}
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public virtual MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect("_init_", mmpc, false );
        }
        public MetaMemberFunction GetFirstMetaMemberFunctionByName( string name )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect( name, null );
        }
        public MetaBase GetMetaBaseByTopLevel( string _name )
        {
            if ( m_ChildrenMetaClassDict.ContainsKey(_name))
                return m_ChildrenMetaClassDict[_name];

            MetaBase parentMB = parentNode;
            while( true )
            {
                if ( parentMB != null )
                {
                    var rmb = parentMB.GetChildrenMetaBaseByName(_name);
                    if (rmb != null) return rmb;

                    parentMB = parentMB.parentNode;
                }
                else
                    break;
            }

            return null;
        }
        public List<MetaMemberFunction> GetMemberFunctionList()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            foreach( var v in m_MetaMemberAllNameFunctionDict )
            {
                mmf.Add(v.Value);
            }
            return mmf;
        }
        public List<MetaMemberFunction> GetMemberInterfaceFunction()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            foreach( var v in m_MetaMemberAllNameFunctionDict )
            {
                var fun = v.Value;
                if (fun.isWithInterface)
                {
                    mmf.Add(fun);
                }
            }
            return mmf;
        }
        public bool GetMemberInterfaceFunctionByFunc( MetaMemberFunction func )
        {
            foreach( var v in m_MetaMemberAllNameFunctionDict)
            {
                if (v.Value.Equals(func))
                {
                    return true;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return this.allClassName;
        }
        public override string ToFormatString()
        {
            return GetFormatString(false);
        }
        public virtual string ToDefineTypeString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(name);

            return stringBuilder.ToString();

        }
        public string GetFormatString( bool isShowNamespace )
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");
            if(isShowNamespace)
            {
                stringBuilder.Append("class ");
                if (topLevelMetaNamespace != null)
                {
                    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                }
                stringBuilder.Append( name);
            }
            else
            {
                stringBuilder.Append("class " + name);
            }
            //if (m_MetaTemplateList.Count > 0)
            //{
            //    stringBuilder.Append("<");
            //    for (int i = 0; i < m_MetaTemplateList.Count; i++)
            //    {
            //        stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
            //        if (i < m_MetaTemplateList.Count - 1)
            //        {
            //            stringBuilder.Append(",");
            //        }
            //    }
            //    stringBuilder.Append(">");
            //}
            if ( m_ExtendClass != null )
            {
                stringBuilder.Append(" extends ");
                stringBuilder.Append(m_ExtendClass.allName);
                //var mtl = m_ExtendClass.metaTemplateList;
                //if( mtl.Count > 0 )
                //{
                //    stringBuilder.Append("<");
                //    for (int i = 0; i < mtl.Count; i++)
                //    {
                //        stringBuilder.Append(mtl[i].ToFormatString());
                //        if (i < mtl.Count - 1)
                //        {
                //            stringBuilder.Append(",");
                //        }
                //    }
                //    stringBuilder.Append(">");
                //}
            }
            if( m_InterfaceClass.Count > 0 )
            {
                stringBuilder.Append(" interface ");
            }
            for( int i = 0; i < m_InterfaceClass.Count; i++ )
            {
                stringBuilder.Append(m_InterfaceClass[i].allName);
                if( i != m_InterfaceClass.Count - 1 )
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(Environment.NewLine);

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in m_ChildrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if (mb is MetaClass)
                {
                    if (mb is MetaEnum)
                    {
                        stringBuilder.Append((mb as MetaEnum).ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                    else
                    {
                        stringBuilder.Append((mb as MetaClass).ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else if (mb is MetaMemberVariable)
                {
                    MetaMemberVariable mmv = mb as MetaMemberVariable;
                    if( mmv.fromType == EFromType.Code )
                    {
                        stringBuilder.Append(mmv.ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else if (mb is MetaMemberFunction)
                {
                    MetaMemberFunction mmfc = mb as MetaMemberFunction;
                    if( mmfc.methodCallType == EMethodCallType.Local )
                    {
                        stringBuilder.Append(mmfc.ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else
                {
                    stringBuilder.Append("Errrrrroooorrr ---" + mb.ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }
            stringBuilder.Append(Environment.NewLine);
            //if( m_MetaGenTemplateClassList.Count > 0 )
            //{
            //    for (int i = 0; i <= realDeep; i++)
            //        stringBuilder.Append(Global.tabChar);
            //    stringBuilder.AppendLine("------------Generator Template List-------------");
            //    for (int i = 0; i < m_MetaGenTemplateClassList.Count; i++)
            //    {
            //        stringBuilder.Append(m_MetaGenTemplateClassList[i].ToFormatString());
            //        stringBuilder.Append(Environment.NewLine);
            //    }
            //}

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
