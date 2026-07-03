//****************************************************************************
//  File:      MetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Meta class's attribute
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile;
using System.Linq;

using SimpleLanguage.Logging;

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
        public List<MetaAttribute> attributeList => m_AttributeList;
        public bool isAbstractClass => m_IsAbstractClass;
        public bool allowExtendsClassWithTemplate => m_GenMetaClassTemplateList.Count > 0 ;             //允许继承类 是否可携带模板  像 ListInt : List<int>{} ListIntEx<T> : List<int> 这种情况不允许
        public MetaClass extendClass => m_ExtendClass;
        public MetaType extendClassMetaType => m_ExtendClassMetaType;
        public int extendLevel => m_ExtendLevel;
        public bool isInterfaceClass => m_IsInterfaceClass;
        public List<MetaClass> interfaceClass => m_InterfaceClass;
        public List<MetaType> interfaceMetaType => m_InterfaceMetaType;
        public MetaExpressNodeBase defaultExpressNode => m_DefaultExpressNode;
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
        public List<MetaType> genMetaTypeTemplateList => m_GenMetaTypeTemplateList;
        public List<MetaClass> genMetaClassTemplateList => m_GenMetaClassTemplateList;
        public List<MetaMemberFunction> nonStaticVirtualMetaMemberFunctionList => m_NonStaticVirtualMetaMemberFunctionList;
        public List<MetaMemberFunction> staticMetaMemberFunctionList => m_StaticMetaMemberFunctionList;
        public List<MetaMemberVariable> fileCollectMetaMemberVariable => m_FileCollectMetaMemberVariable;
        public List<MetaMemberFunction> fileCollectMetaMemberFunctionList => m_FileCollectMetaMemberFunctionList;
        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        public Dictionary<string, MetaMemberFunctionTemplateNode> metaMemberFunctionTemplateNodeDict => m_MetaMemberFunctionTemplateNodeDict;
        public Dictionary<string, MetaMemberVariable> metaExtendMemeberVariableDict => m_MetaExtendMemeberVariableDict;
        public List<MetaType> bindStructTemplateMetaClassList => m_BindStructTemplateMetaClassList;
        public Dictionary<Token, FileMetaClass> fileMetaClassDict => m_FileMetaClassDict;
        public bool needInitMemberVariables => m_NeedInitMemberVariables;
        public bool isHandleExtendVariableDirty { get; set; } = false;
        public bool innderDefine => m_InnderDefine;
        public bool structDefine => m_StructDefine;
        public bool manaualDefine => m_ManaualDefine;
        public bool isPartial => m_IsPartial;


        protected int m_ExtendLevel = 0;
        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass = null;
        protected MetaType m_ExtendClassMetaType = null;
        protected List<MetaType> m_GenMetaTypeTemplateList = new List<MetaType>();  //生成类的传入模板值 比如 ListInst extends List<int> 这野牛ListInst 也有<int>这个属性
        protected List<MetaClass> m_GenMetaClassTemplateList = new List<MetaClass>();//未来使用这种方式使用 绑定模板
        protected List<MetaType> m_BindStructTemplateMetaClassList = new List<MetaType>();
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();
        protected List<MetaType> m_InterfaceMetaType = new List<MetaType>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected List<MetaMemberVariable> m_FileCollectMetaMemberVariable = new List<MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunctionTemplateNode> m_MetaMemberFunctionTemplateNodeDict = new Dictionary<string, MetaMemberFunctionTemplateNode>();
        protected List<MetaMemberFunction> m_FileCollectMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaType> m_FileCollectMetaInterfaceList = new List<MetaType>();
        protected List<MetaMemberFunction> m_NonStaticVirtualMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_StaticMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_TempInnerFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected MetaExpressNodeBase m_DefaultExpressNode = null;
        protected bool m_IsInterfaceClass = false;
        protected bool m_IsAbstractClass = false;
        protected bool m_NeedInitMemberVariables = true;
        protected bool m_InnderDefine = false;
        protected bool m_StructDefine = false;
        protected bool m_ManaualDefine = false;//手动编译的代码
        protected bool m_IsPartial = false;

        protected readonly List<MetaAttribute> m_AttributeList = new List<MetaAttribute>();

        public void AddAttributes(List<FileMetaAttributeSyntax> list)
        {
            if (list == null || list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                m_AttributeList.Add(new MetaAttribute(list[i]));
            }
        }

        protected MetaClass()
        {

        }
        public MetaClass(string _name, EClassDefineType ecdt )
        {
            m_Name = _name;
            m_Type = EType.Class;
            m_InnderDefine = true;
            this.m_AllName = _name;
        }
        public MetaClass(string _name, EType _type  = EType.Class )
        {
            m_Name = _name;
            m_Type = _type;
        }
        public MetaClass( MetaClass mc ) : base(mc)
        {
            m_Name = mc.m_Name;
            this.m_AllName = mc.m_AllName; 
            m_Type = mc.m_Type;
            m_FileMetaClassDict = mc.m_FileMetaClassDict;
            m_ExtendClass = mc.m_ExtendClass;
            if(m_ExtendClass != null )
            {
                m_ExtendLevel = m_ExtendClass.m_ExtendLevel;
            }
            m_InterfaceClass = mc.m_InterfaceClass;
            m_InterfaceMetaType = mc.m_InterfaceMetaType;

            m_MetaExtendMemeberVariableDict = mc.m_MetaExtendMemeberVariableDict;
            m_MetaMemberVariableDict = mc.m_MetaMemberVariableDict;

            m_FileCollectMetaMemberVariable = mc.m_FileCollectMetaMemberVariable;
            m_FileCollectMetaInterfaceList = mc.m_FileCollectMetaInterfaceList;
            m_FileCollectMetaMemberFunctionList = mc.m_FileCollectMetaMemberFunctionList;
            m_GenMetaClassTemplateList = mc.m_GenMetaClassTemplateList;
            m_GenMetaTypeTemplateList = mc.m_GenMetaTypeTemplateList;

            m_MetaMemberFunctionTemplateNodeDict = mc.m_MetaMemberFunctionTemplateNodeDict;
            m_NonStaticVirtualMetaMemberFunctionList = mc.m_NonStaticVirtualMetaMemberFunctionList;
            m_StaticMetaMemberFunctionList = mc.m_StaticMetaMemberFunctionList;
            m_DefaultExpressNode = mc.m_DefaultExpressNode;
            m_IsAbstractClass = mc.m_IsAbstractClass;
            m_IsPartial = mc.m_IsPartial;
        }
        public override void SetDeep( int deep )
        {
            this.m_Deep = deep;                        
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach (var v in m_MetaMemberVariableDict)
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach (var v in m_NonStaticVirtualMetaMemberFunctionList)
            {
                v.SetDeep(deep + 1);
            }
            foreach (var v in m_StaticMetaMemberFunctionList)
            {
                v.SetDeep(deep + 1);
            }
        }
        public void SetNeedInitMemberVariables( bool flag) { m_NeedInitMemberVariables = flag; }
        public void SetAbstractClass(bool v) { m_IsAbstractClass = v; }
        public void UpdateClassAllName()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_MetaNode.GetAllName());

            if( m_MetaTemplateList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_MetaTemplateList.Count; i++ )
                {
                    sb.Append(m_MetaTemplateList[i].name);
                    if(m_MetaTemplateList[i].extendsMetaClass != null )
                    {
                        sb.Append(":");
                        sb.Append(m_MetaTemplateList[i].extendsMetaClass.allName );
                    }
                    if( i < m_MetaTemplateList.Count - 1 )
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }

            this.m_AllName = sb.ToString();
        }
        public void UpdateGenMetaClassTemplateList()
        {
            m_GenMetaTypeTemplateList.Clear();  
            for( int i = 0; i < m_GenMetaClassTemplateList.Count; i++ )
            {
                m_GenMetaTypeTemplateList.Add( new MetaType( m_GenMetaClassTemplateList[i] ) );
            }
        }
        public void SetDefaultExpressNode( MetaExpressNodeBase defaultExpressNode )
        {
            m_DefaultExpressNode = defaultExpressNode;
        }
        public virtual void ParseGenTemplateClass( MetaGenTemplateClass mgtc)
        {
        //    ParseExtendsRelation();
        //    ParseTemplateRelation();
        //    HandleExtendData();
        }
        public virtual void ParseGenMemberVarible()
        {

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
        public void SetManaualDefine( bool ecdt )
        {
            this.m_ManaualDefine = ecdt;
        }
        public virtual void ParseExtendsRelation()
        {
            if( this == CoreMetaClassManager.objectMetaClass )
            {
                return;
            }
            if (this.m_ExtendClassMetaType != null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, this.m_Token, "已绑定过了继承类 : " + extendClass.name );
                return;
            }
            foreach( var v in m_FileMetaClassDict )
            {
                var mc = v.Value;
                if(mc.fileMetaExtendClass == null )
                {
                    continue;
                }
                if(this.m_ExtendClassMetaType != null )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, this.m_Token, "已绑定过了继承类 : " + mc.metaClass.extendClass.name );
                    continue;
                }

                MetaType getmt = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance( this, mc.fileMetaExtendClass );
                if (getmt != null)
                {
                    this.m_ExtendClassMetaType = getmt;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, this.m_Token, "没有发现继承类的类型!!! " + mc.metaClass.extendClass.name );
                }
            }

            if(m_ExtendClassMetaType == null && this != CoreMetaClassManager.objectMetaClass )
            {
                m_ExtendClassMetaType = new MetaType( CoreMetaClassManager.objectMetaClass );
            }

            if (!m_ExtendClassMetaType.DefineTemplateIsIncludeTemplate())
            {
                this.m_ExtendClass = this.m_ExtendClassMetaType.metaClass;
            }
            else
            {
                this.m_ExtendClass = m_ExtendClassMetaType.metaClass;
            }
            HandleParentClassTemplateMapRelation();
            HandleExtendClassTemplateMapRelation();
        }
        public virtual void ParseInterfaceRelation()
        {
            m_FileCollectMetaInterfaceList.Clear();
            foreach ( var v in this.fileMetaClassDict )
            {
                for( int i = 0; i < v.Value.interfaceClassList.Count; i++ )
                {
                    var icd = v.Value.interfaceClassList[i];

                    MetaType getmt = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(this, icd );
                    if (getmt == null )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到接口相关的定义类!!");
                        continue;
                    }
                    this.m_FileCollectMetaInterfaceList.Add(getmt);
                }
            }
        }
        public virtual void HandleExtendInterface()
        {
            if (m_ExtendClass == null)
            {
                return;
            }
            else
            {
                foreach (var v in m_ExtendClass.m_InterfaceMetaType )
                {
                    this.m_InterfaceMetaType.Add(new MetaType(v) );
                }
                foreach( var v in m_FileCollectMetaInterfaceList )
                {
                    this.m_InterfaceMetaType.Add(v);
                }
                foreach( var v in m_InterfaceMetaType )
                {
                    this.m_InterfaceClass.Add(v.metaClass);
                }

                HandleInterfaceClassTemplateMapRelation();
            }
        }
        public virtual void HandleExtendMemberVariable()
        {
            if(this.m_ExtendClass == null )
            {
                foreach( var v in this.m_FileCollectMetaMemberVariable )
                {
                    m_MetaMemberVariableDict.Add(v.name, v);
                }
                return;
            }
            else
            {
                foreach (var v in m_ExtendClass.m_MetaExtendMemeberVariableDict)
                {
                    var c = v.Value;
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        var ld = Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.m_AllName} 中已包含:{c.name} ");
                        //ld.valDict.Add(EMetaType.MetaClass, this);
                        //ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                        //ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                        continue;
                    }
                    this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
                }
                foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
                {
                    var c = v.Value;
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        var ld = Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.m_AllName} 中已包含:{c.name} ");
                        //ld.valDict.Add(EMetaType.MetaClass, this);
                        //ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                        //ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                        continue;
                    }
                    this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
                }
                foreach (var c in this.m_FileCollectMetaMemberVariable)
                {
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        if( !this.innderDefine )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 继承的类321:{m_AllName} 在继承的父类{m_ExtendClass.m_AllName} 中已包含:{c.name} ");
                        }
                        continue;
                    }
                    this.m_MetaMemberVariableDict.Add(c.name, c);
                }
            }
            int nonStaticIndex = 0;
            int staticIndex = 0;
            foreach( var v in this.m_MetaMemberVariableDict)
            {
                if( v.Value.isStatic)
                {
                    v.Value.SetIndex(staticIndex);
                    staticIndex++;
                }
                else
                {
                    v.Value.SetIndex(nonStaticIndex);
                    nonStaticIndex++;
                }
            }
        }
        public virtual void HandleExtendMemberFunction()
        {
            if (this.m_ExtendClass == null)
            {
                foreach (var v in m_FileCollectMetaMemberFunctionList)
                {
                    if (v.isStatic)
                    {
                        m_StaticMetaMemberFunctionList.Add(v);
                    }
                    else
                    {
                        if (v.isWithInterface) continue;
                        //if (v.isConstructInitFunction) continue;
                        if( v.isOverrideFunction )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, v.token, "有override标记，但没有父类 ");                            
                        }
                        m_NonStaticVirtualMetaMemberFunctionList.Add(v);
                    }
                }
            }
            else
            {
                bool canAdd = false;
                foreach (var v in this.m_ExtendClass.m_NonStaticVirtualMetaMemberFunctionList)
                {
                    canAdd = true;
                    var efun = v;
                    //if (efun.isConstructInitFunction) { continue; }

                    foreach (var v2 in m_FileCollectMetaMemberFunctionList)
                    {
                        //if (v2.isConstructInitFunction) continue;
                        if (efun.IsEqualMetaFunction(v2))
                        {
                            // child provides an implementation
                            // if parent is abstract, child must mark method with 'override'
                            if (efun.isAbstract && !v2.isOverrideFunction)
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 子类[" + this.m_AllName + "] 方法: " + v2.name + " 实现了抽象父方法但未使用 override 标记");
                            }
                            v2.SetOverrideMetaMemberFunction(efun);
                            canAdd = false;
                            m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                            continue;
                        }
                    }
                    if (canAdd)
                    {
                        // If parent function is abstract and current class is concrete, require override
                        if (efun.isAbstract && !this.m_IsAbstractClass)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAbstractFunctionNeedInstance, this.token, "", this.m_ExtendClass.allName, efun.name, this.allName);
                        }
                        m_NonStaticVirtualMetaMemberFunctionList.Add(efun);
                    }
                }

                foreach (var v2 in this.m_FileCollectMetaMemberFunctionList)
                {
                    if (v2.isStatic)
                    {
                        var find = m_StaticMetaMemberFunctionList.Find(a => a == v2);
                        if (find != null) continue;

                        m_StaticMetaMemberFunctionList.Add(v2);
                    }
                    else
                    {
                        var find = m_NonStaticVirtualMetaMemberFunctionList.Find(a => a == v2);
                        if (find != null) continue;

                        if (v2.isOverrideFunction && v2.overrideMetaMemberFunction != null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, find.token, "有override标记，但没有父类 ");
                        }

                        m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                    }
                }
            }




            foreach (var v2 in m_NonStaticVirtualMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
            foreach (var v2 in m_StaticMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }


            List<MetaMemberFunction> addList = new List<MetaMemberFunction>();
            for (int i = 0; i < this.m_TempInnerFunctionList.Count; i++)
            {
                MetaMemberFunction mmf = m_TempInnerFunctionList[i];

                bool isAdd = true;
                if (m_MetaMemberFunctionTemplateNodeDict.ContainsKey(mmf.name))
                {
                    var list = m_MetaMemberFunctionTemplateNodeDict[mmf.name];
                    MetaMemberFunction curFun = list.IsSameMetaMemeberFunction(mmf);
                    if (curFun != null)
                    {
                        isAdd = false;
                        if (mmf.isCanRewrite)
                        {
                            //int index = list.IndexOf(curFun);
                            //list[index] = mmf;
                        }
                        else
                        {
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
            for (int i = 0; i < addList.Count; i++)
            {
                var v = addList[i];
                if (v.isStatic)
                {
                    m_StaticMetaMemberFunctionList.Add(v);
                }
                else
                {
                    m_NonStaticVirtualMetaMemberFunctionList.Add(v);
                }
            }
            m_TempInnerFunctionList.Clear();
        }
        public virtual void HandleExtendAndInterfaceMetaTypeInstnace()
        {
            if(m_ExtendClassMetaType?.metaClass is MetaGenTemplateClass mgtc )
            {
                mgtc.ParseGenTemplateClass(mgtc);
                mgtc.ParseGenMemberVarible();
                m_ExtendClass = mgtc;

                for( int i = 0; i < mgtc.genMetaClassTemplateList.Count; i++ )
                {
                    this.m_GenMetaClassTemplateList.Add(mgtc.genMetaClassTemplateList[i]);
                }
            }
            else
            {
            }
            this.UpdateGenMetaClassTemplateList();


            //for (int i = 0; i < this.m_InterfaceMetaType.Count; i++)
            //{
            //    if( this.m_InterfaceMetaType[i].metaClass is MetaGenTemplateClass mgtc2 )
            //    {
            //        mgtc2.ParseGenTemplateClass(mgtc2);
            //        mgtc2.ParseGenMemberVarible();
            //        this.m_InterfaceClass.Add(mgtc2);
            //    }
            //}

            HandleExtendInterface();
            HandleExtendMemberVariable();
            HandleExtendMemberFunction();
        }
        public void SetGenMetaClassTemplateList( List<MetaClass> mtlist )
        {
            m_GenMetaClassTemplateList = mtlist;
            this.UpdateGenMetaClassTemplateList();
        }
        public MetaType GetGenMetaTypeTemplateByIndex(int index)
        {
            if (index < 0 || index >= this.m_GenMetaClassTemplateList.Count)
            {
                return null;
            }
            return m_GenMetaTypeTemplateList[index];
        }
        public virtual void ParseFileCollectMemberVariableDefineMetaType()
        {
            foreach (var it in this.m_FileCollectMetaMemberVariable )
            {
                it.ParseDefineMetaType();
                it.CreateMetaExpress();
            }
        }
        public virtual void ParseFileCollectMemberFunctionDefineMetaType()
        {
            foreach (var it in m_FileCollectMetaMemberFunctionList )
            {
                it.ParseDefineMetaType();
            }
        }
        public void CheckInterface()
        {
            foreach (var it in this.m_InterfaceMetaType )
            {
                MetaClass interfaceMc = it.GetTemplateMetaClass();

                Token token = m_Token;
                foreach( var interfaceMMF in interfaceMc.m_FileCollectMetaMemberFunctionList )
                {
                    bool certified = false;
                    foreach ( var selfMMF in this.m_FileCollectMetaMemberFunctionList )
                    {
                        if (!interfaceMMF.name.Equals(selfMMF.name))
                            continue;
                        if ( interfaceMMF.metaMemberTemplateCollection.IsEqualMetaDefineTemplateCollection( selfMMF.metaMemberTemplateCollection )
                            && interfaceMMF.metaMemberParamCollection.IsEqualMetaDefineParamCollection(selfMMF.metaMemberParamCollection))
                        {
                            if( selfMMF.isOverrideFunction )
                            {
                                certified = true;
                                selfMMF.SetIsOverrideInterface(true);
                                break;
                            }
                            else
                            {
                                certified = true;
                                Log.AddMetaCoreLog(LID.MetaCoreFunctionNeedOverrideFlag, token, "interface function need override flag", this.allName, interfaceMMF.name );
                                break;
                            }
                        }
                    }
                    if (!certified)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreInterfaceNeedInstance, token, "", interfaceMc.allName, interfaceMMF.name, this.allName);
                    }
                }
            }
        }
        public bool GetInterfaceByMetaType( MetaType mc )
        {
            foreach( var v in m_InterfaceMetaType )
            {
                if( TypeManager.CompareMetaType( v, mc ) )
                {
                    return true;
                }
            }
            return false;
        }
        public bool GetInterfaceByMetaClass(MetaClass mc )
        {
            if( this == mc )
            {
                return true;
            }
            if( this == mc )
            {
                return true;
            }
            foreach( var v in this.m_InterfaceClass )
            {
                if( v is MetaGenTemplateClass mgtc )
                {
                    if( mgtc.metaTemplateClass == mc )
                    {
                        return true;
                    }
                }
                if (v == mc)
                {
                    return true;
                }
            }
            /*
            foreach (var v in m_InterfaceMetaType)
            {
                if( v.metaClass == mc )
                {
                    return true;
                }
            }
            */
            return false;
        }
        public MetaType AddMetaPreTemplateClass( MetaType mt, bool isParse, out bool isGenMetaClass )
        {
            isGenMetaClass = false;
            if ( mt.metaClass == null )
            {
                return null;
            }
            //原来使用的是genTemplateMetaType 现在换成了 define方式 试试，以后 gen 和define要分离，gen只在生成类中取
            MetaGenTemplateClass mgtc = mt.metaClass.AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList( mt.defineTemplateMetaTypeList );

            if ( mgtc  != null )
            {
                isGenMetaClass = true;
                if(isParse )
                {
                    mgtc.ParseGenTemplateClass(mgtc);
                    mgtc.ParseGenMemberVarible();
                }
                return new MetaType(mgtc, mt.defineTemplateMetaTypeList );
            }

            var find = BindStructTemplateMetaClassList( mt );
            if( find == null )
            {
                this.m_BindStructTemplateMetaClassList.Add(new MetaType(mt) );
            }
            return mt;
        }
        public void EnsureParsedGenTemplateMetaClasses()
        {
            var list = new List<MetaGenTemplateClass>(m_MetaGenTemplateClassList);
            foreach (var mgtc in list)
            {
                if (mgtc == null)
                {
                    continue;
                }
                mgtc.ParseGenTemplateClass(mgtc);
                mgtc.ParseGenMemberVarible();
            }
        }
        public MetaGenTemplateClass AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList( List<MetaType> templateMetaTypeList )
        {
            List<MetaClass> mcList = new List<MetaClass>();
            for (int i = 0; i < templateMetaTypeList.Count; i++)
            {
                var mtc = templateMetaTypeList[i];
                if (mtc.eMetaTypeType == EMetaTypeType.MetaClass
                    || mtc.eMetaTypeType == EMetaTypeType.MetaGenClass )
                {
                    mcList.Add(mtc.metaClass);
                }
            }
            if (mcList.Count == templateMetaTypeList.Count)
            {
                MetaGenTemplateClass mgtc = AddInstanceMetaClass(mcList);
                return mgtc;
            }
            return null;
        }
        public MetaType BindStructTemplateMetaClassList( MetaType mt )
        {
            foreach( var v in m_BindStructTemplateMetaClassList )
            {
                if(TypeManager.CompareMetaType(v,mt ) )
                {
                    return v;
                }
            }
            return null;
        }
#if EditorMode
        public void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass(this);
            m_FileMetaClassDict.Add(fmc.token, fmc);
            AddPingToken(fmc.token);

            if (fmc.attributeList != null && fmc.attributeList.Count > 0)
            {
                AddAttributes(fmc.attributeList);
            }

            if(m_IsInterfaceClass == false )
            {
                m_IsInterfaceClass = fmc.preInterfaceToken != null;
            }

            m_IsPartial = fmc.isPartial;
        }
        public void ParseFileMetaClassMemeberVarAndFunc( FileMetaClass fmc )
        {
            bool isProjectSpecialClass = string.Equals(this.name, "Project", StringComparison.OrdinalIgnoreCase)
                && fmc?.fileMeta?.path?.EndsWith(".sp", StringComparison.OrdinalIgnoreCase) == true;

            bool isHave = false;
            foreach (var v2 in fmc.memberVariableList)
            {
                var mn = this.m_MetaNode.GetChildrenMetaNodeByName(v2.name);
                if( mn != null )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                MetaMemberVariable cmmv = GetMetaMemberVariableByName(v2.name);
                if (cmmv != null)
                {
                    if (cmmv != null && cmmv.isInnerDefine)
                    {
                        m_FileCollectMetaMemberVariable.Add(cmmv);
                        cmmv.SetToken(v2.token);
                        cmmv.SetFileMetaMemeberVariable(v2);
                        MetaVariableManager.instance.AddMetaMemberVariable(cmmv);
                        continue;
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    }
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable(this, v2);
                if (isProjectSpecialClass)
                {
                    if (v2.staticToken != null || v2.constToken != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Project类成员变量不允许显式定义 static/const，系统会按全局语义处理: " + v2.token?.ToLexemeAllString());
                    }
                    mmv.SetIsStatic(true);
                    mmv.SetIsConst(true);
                }
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                m_FileCollectMetaMemberVariable.Add(mmv);
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
            foreach (var v2 in fmc.memberFunctionList)
            {
                var mn = this.m_MetaNode.GetChildrenMetaNodeByName(v2.name);
                if (mn != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                MetaMemberFunction mmf = new MetaMemberFunction( this, v2 );
                if (isProjectSpecialClass)
                {
                    if (v2.staticToken != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Info Project类成员函数默认按 static 处理: " + v2.token?.ToLexemeAllString());
                    }
                    mmf.SetIsStatic(true);
                }
                m_FileCollectMetaMemberFunctionList.Add(mmf);
                MethodManager.instance.AddOriginalMemeberFunction(mmf);
            }            
        }
        //解析 自动构建函数  
        public virtual void ParseDefineComplete()
        {
            if( this.m_IsInterfaceClass )
            {
                return;
            }

            //AddDefineConstructFunction();
            if (m_DefaultExpressNode == null )
            {
                MetaType mdt = new MetaType(this);
                if( eType == EType.Data || eType == EType.Enum )
                {
                    return;
                }
                var defaultFunction = GetMetaMemberConstructDefaultFunction();
                if (defaultFunction == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找发现默认构造函数");
                    return;
                }
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, defaultFunction.metaBlockStatements );
            }
        }
#endif        
        public void SetExtendClass(MetaClass sec)
        {
            this.m_ExtendClass = sec;
        }
        public void CalcExtendLevel()
        {
            if( this.m_ExtendClass == null )
            {
                m_ExtendLevel = 0;
            }
            else
            {
                MetaClass mc = m_ExtendClass;
                int level = 0;
                while (mc != null)
                {
                    level++;
                    if( mc is MetaGenTemplateClass mgtc )
                    {
                        mc = mgtc.metaTemplateClass;
                    }
                    else
                    {
                        mc = mc.extendClass;
                    }
                }
                m_ExtendLevel = level;
            }
        }
        public bool IsInterfaceByMetaClass( MetaClass mc )
        {
            if (m_InterfaceClass.Count == 0)
            {
                return false;
            }

            var interfaceMc = m_InterfaceClass.Find(a => a == mc);
            return interfaceMc != null;
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
        public bool ExtendClassContainMetaClass(MetaClass commc )
        {
            MetaClass mc = this;
            while (mc != null)
            {
                if( mc is MetaGenTemplateClass mgtc )
                {
                    if( mgtc.metaTemplateClass == commc )
                    {
                        return true;
                    }
                }
                if (mc == commc)
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
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "重复添加接口");
            }
        }
        public void AddMetaMemberVariable( MetaMemberVariable mmv, bool isAddManager = true )
        {
            if( m_MetaMemberVariableDict.ContainsKey( mmv.name ) )
            {
                return;
            }
            m_MetaMemberVariableDict.Add(mmv.name, mmv);
            //AddMetaBase(mmv.name, mmv);
            if( isAddManager )
            {
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
        }
        public void AddInnerMetaMemberFunction( MetaMemberFunction mmf )
        {
            m_TempInnerFunctionList.Add(mmf);
        }
        public bool AddMetaMemberFunction( MetaMemberFunction mmf )
        {
            MetaMemberFunctionTemplateNode find = null;
            if(this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(mmf.name ) )
            {
                find = m_MetaMemberFunctionTemplateNodeDict[mmf.name];                
            }
            else
            {
                find = new MetaMemberFunctionTemplateNode();
                m_MetaMemberFunctionTemplateNodeDict.Add(mmf.name, find);
            }
            if( find.AddMetaMemberFunction(mmf) )
            {
                //m_CurrentClassMetaMemberFunctionList.Add(mmf);
                //m_AllMetaMemberFunctionList.Add(mmf);
                return true;
            }
            return false;
        }
        //public void AddDefineConstructFunction()
        //{
        //    MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
        //    if (mmf == null)
        //    {
        //        mmf = new MetaMemberFunction(this, "_init_");
        //        mmf.SetReturnMetaClass(CoreMetaClassManager.voidMetaClass);
        //        mmf.Parse();
        //        AddMetaMemberFunction(mmf);
        //        MethodManager.instance.AddOriginalMemeberFunction(mmf);
        //    }
        //}
        public void AddDefineInstanceValue()
        {
            MetaMemberVariable mmv = this.GetMetaMemberVariableByName( "instance" );
            if (mmv == null)
            {
                mmv = new MetaMemberVariable(this, "instance");
                mmv.defineMetaType.SetMetaClass(this);
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
        public MetaMemberVariable GetMetaMemberVariableByIndex(int index)
        {
            if (index < 0) return null;
            var list = allMetaMemberVariableList;
            if (index >= list.Count) return null;
            return list[index];
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
        public virtual List<MetaMemberVariable>  GetMetaMemberVariableListByFlag( bool isStatic )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();

            if(isStatic )
            {
                foreach (var v in m_MetaExtendMemeberVariableDict)
                {
                    if( v.Value.isStatic)
                    {
                        mmvList.Add(v.Value);
                    }
                }

                foreach (var v in this.m_MetaMemberVariableDict)
                {
                    if( v.Value.isStatic )
                    {
                        mmvList.Add(v.Value);
                    }
                }
            }
            else
            {
                foreach (var v in m_MetaExtendMemeberVariableDict)
                {
                    if (!v.Value.isStatic)
                    {
                        mmvList.Add(v.Value);
                    }
                }

                foreach (var v in this.m_MetaMemberVariableDict)
                {
                    if (!v.Value.isStatic)
                    {
                        mmvList.Add(v.Value);
                    }
                }
            }
            return mmvList;
        }
        public virtual MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName(string name, MetaInputParamCollection inputParam, bool isGet, bool isSet)
        {
            if (!m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            var tnode = m_MetaMemberFunctionTemplateNodeDict[name];
            

            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(0))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[0];

            var inputparamcount = inputParam != null ? inputParam.count : 0;

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputparamcount);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    return fun;
                }
                else
                {
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                }
            }            
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(string name, int templateParamCount, MetaInputParamCollection inputParam, bool isIncludeExtendClass = true )
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name) )
            {
                return null;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if( !tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount) )
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputParam != null ? inputParam.count : 0 );
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                //if (fun.isTemplateFunction)
                //{
                //    return fun;
                //}
                //else
                //{
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                //}
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParam(string name, List<MetaType> mtList, MetaInputParamCollection inputParam, bool isIncludeExtendClass = true)
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            int templateParamCount = 0;
            if( mtList != null )
            {
                templateParamCount = mtList.Count;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputParam != null ? inputParam.count : 0);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    var gfun = fun.GetGenTemplateFunction(mtList);

                    if( gfun != null )
                    {
                        return gfun;
                    }
                    return fun;
                }
                else
                {
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                }
            }
            return null;
        }
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public virtual MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, mmpc, false );
        }
        public MetaMemberFunction GetFirstMetaMemberFunctionByName( string name )
        {
            return GetMetaMemberFunctionByNameAndInputTemplateInputParamCount( name, 0, null );
        }
        public List<MetaMemberFunction> GetMemberInterfaceFunction()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            //foreach( var v in m_MetaMemberFunctionDict )
            //{
            //    var fun = v.Value;
            //    if (fun.isWithInterface)
            //    {
            //        mmf.Add(fun);
            //    }
            //}
            return mmf;
        }
        public bool GetMemberInterfaceFunctionByFunc( MetaMemberFunction func )
        {
            //foreach( var v in m_MetaMemberFunctionDict )
            //{
            //    if (v.Value.Equals(func))
            //    {
            //        return true;
            //    }
            //}
            return true;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.isGenTemplate)
            {
                stringBuilder.Append(" [Gen] ");
            }
            else
            {
                if (this.isTemplateClass)
                {
                    stringBuilder.Append(" [Template] ");
                }
            }              

            stringBuilder.Append(allName);

            return stringBuilder.ToString();
        }
        public override string ToFormatString()
        {
            return GetFormatString(false);
        }
        public virtual string ToDefineTypeString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(allName);

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
            if (isShowNamespace)
            {
                stringBuilder.Append("class ");
                //if (topLevelMetaNamespace != null)
                //{
                //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                //}
                stringBuilder.Append(name);
            }
            else
            {
                stringBuilder.Append("class " + name);
            }
            if (m_MetaTemplateList.Count > 0)
            {
                stringBuilder.Append("<");
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
                    if (i < m_MetaTemplateList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append(">");
            }
            if (m_ExtendClass != null)
            {
                stringBuilder.Append(" extends ");
                stringBuilder.Append(m_ExtendClass.allName);
                var mtl = m_ExtendClass.metaTemplateList;
                if (mtl.Count > 0)
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        stringBuilder.Append(mtl[i].ToFormatString());
                        if (i < mtl.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }
            }
            if (m_InterfaceMetaType.Count > 0)
            {
                stringBuilder.Append(" interface ");
            }
            for (int i = 0; i < m_InterfaceMetaType.Count; i++)
            {
                stringBuilder.Append(m_InterfaceMetaType[i].ToFormatString() );
                if (i != m_InterfaceClass.Count - 1)
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(Environment.NewLine);

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v2 in m_MetaNode.childrenMetaNodeDict )
            {
                stringBuilder.Append(v2.Value.ToFormatString());
            }

            foreach (var v in m_MetaMemberVariableDict )
            {
                stringBuilder.AppendLine(v.Value.ToFormatString());
            }

            foreach (var v in m_StaticMetaMemberFunctionList )
            {
                MetaMemberFunction mmfc = v;
                stringBuilder.Append(mmfc.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }

            foreach (var v in m_NonStaticVirtualMetaMemberFunctionList)
            {
                MetaMemberFunction mmfc = v;
                stringBuilder.Append(mmfc.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }
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



        public static bool CompareMetaClass(MetaClass leftmc, MetaClass rightmc)
        {
            if (leftmc == null || rightmc == null) return false;
            // 左值 object：可接受任意右值。
            if (leftmc == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }
            else if (NumberManager.IsNumberClass(leftmc))
            {
                if (leftmc == rightmc)
                {
                    return true;
                }
                else
                {
                    if (leftmc == CoreMetaClassManager.int8MetaClass
                       || leftmc == CoreMetaClassManager.uint8MetaClass)
                    {
                        return false;
                    }
                    else if (leftmc == CoreMetaClassManager.int16MetaClass
                       || leftmc == CoreMetaClassManager.uint16MetaClass)
                    {
                        if (rightmc == CoreMetaClassManager.int8MetaClass
                            || rightmc == CoreMetaClassManager.uint8MetaClass)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (leftmc == CoreMetaClassManager.int32MetaClass
                       || leftmc == CoreMetaClassManager.uint32MetaClass)
                    {
                        if (rightmc == CoreMetaClassManager.int8MetaClass
                            || rightmc == CoreMetaClassManager.uint8MetaClass
                            || rightmc == CoreMetaClassManager.int16MetaClass
                            || rightmc == CoreMetaClassManager.uint16MetaClass)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (leftmc == CoreMetaClassManager.int64MetaClass
                       || leftmc == CoreMetaClassManager.uint64MetaClass)
                    {

                        if (rightmc == CoreMetaClassManager.int8MetaClass
                            || rightmc == CoreMetaClassManager.uint8MetaClass
                            || rightmc == CoreMetaClassManager.int16MetaClass
                            || rightmc == CoreMetaClassManager.uint16MetaClass
                            || rightmc == CoreMetaClassManager.int32MetaClass
                            || rightmc == CoreMetaClassManager.uint32MetaClass)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (leftmc == CoreMetaClassManager.numMetaClass)
                    {
                        if (NumberManager.IsNumberClass(rightmc))
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
            }
            else
            {
                //// 普通 class：调用 ResolveAssignRelation 并根据关系决定是否需要强转。
                //var classRelation = Re(leftMetaType, rightMt, out MetaClass mc, out MetaClass cmc);
                //if (classRelation == ETypeRelation.ExpressTypeError)
                //{
                //    Log.AddMetaCoreLog(LID.ShowExtendMessage, token,
                //        "Error 赋值表达式返回定义类型为空");
                //    return false;
                //}

                //switch (classRelation)
                //{
                //    case ETypeRelation.Same:
                //        //convertMetaType = rightClassType ?? leftMetaType;
                //        return true;
                //    case ETypeRelation.Child:
                //        //if (compareClass != null)
                //        //{
                //        //    convertMetaType = rightClassType;
                //        //}
                //        return true;
                //    case ETypeRelation.Interface:
                //    case ETypeRelation.Num:
                //    case ETypeRelation.Similar:
                //        //convertMetaType = rightClassType;
                //        //isNeedCast = classRelation != ETypeRelation.Interface;
                //        return true;
                //    case ETypeRelation.Parent:
                //        {
                //            var sb = new System.Text.StringBuilder();
                //            sb.Append("Warning 类型不相同 ");
                //            //if (curClass != null) sb.Append("定义类: ").Append(curClass.allName).Append(' ');
                //            //if (compareClass != null) sb.Append("表达式类: ").Append(compareClass.allName).Append(' ');
                //            //sb.Append("返回值是父类型向子类型转换，存在错误转换!!");
                //            //Log.AddMetaCoreLog(LID.ShowExtendMessage, errorAnchorToken, sb.ToString());
                //            //isNeedCast = true;
                //            return true;
                //        }
                //    case ETypeRelation.No:
                //        {
                //            //var targetTemplateList = leftMetaType.GetGenTemplateMetaTypeList();
                //            //var exprTemplateList = rightMt?.GetGenTemplateMetaTypeList();
                //            //bool hasTemplateInEither =
                //            //    (targetTemplateList != null && targetTemplateList.Count > 0)
                //            //    || (exprTemplateList != null && exprTemplateList.Count > 0);
                //            //if (hasTemplateInEither)
                //            //{
                //            //    Log.AddMetaCoreLog(LID.ShowExtendMessage, errorAnchorToken,
                //            //        "模板类型不匹配（接口模板位置仅在可协变标记下允许协变），请检查模板参数或接口变型规则。");
                //            //    return false;
                //            //}
                //            var sb = new System.Text.StringBuilder();
                //            sb.Append("Warning 类型不相同 ");
                //            //if (curClass != null) sb.Append("定义类: ").Append(curClass.allName).Append(' ');
                //            //if (compareClass != null) sb.Append("表达式类: ").Append(compareClass.allName).Append(' ');
                //            //sb.Append("可能会有强转，强转后可能默认值为null");
                //            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, sb.ToString());
                //            //isNeedCast = true;
                //            return true;
                //        }
                //    default:
                //        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "表达式错误，或者是定义类型错误");
                //        return false;
                //}
            }


            return true;
        }
    }
}
