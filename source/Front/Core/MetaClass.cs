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
        public List<MetaMemberFunction> interfaceDeclareMetaMemberFunctionList => m_InterfaceDeclareMetaMemberFunctionList;
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
        private List<MetaMemberVariable> m_FileCollectMetaMemberVariable = new List<MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunctionTemplateNode> m_MetaMemberFunctionTemplateNodeDict = new Dictionary<string, MetaMemberFunctionTemplateNode>();
        private List<MetaMemberFunction> m_FileCollectMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        private List<MetaType> m_FileCollectMetaInterfaceList = new List<MetaType>();
        protected List<MetaMemberFunction> m_NonStaticVirtualMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_StaticMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_InterfaceDeclareMetaMemberFunctionList = new List<MetaMemberFunction>();// 接口类自身声明的接口函数（不含从Object继承的函数）
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
            m_IsPartial = true;
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
            m_IsInterfaceClass = mc.m_IsInterfaceClass;
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
                    // 不把模板参数的约束类型（extendsMetaClass）写进 allName：
                    // 约束不是类型身份的一部分。包含它会导致 inner-form（如 ArrayMetaClass 构造时
                    // 设了 extendsMetaClass=Object）的 allName 为 "Core.Array<T:Core.Object>"，
                    // 而源码编译/导出的为 "Core.Array<T>"，两者 classId 不同，产生重复 IRMetaClass。
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, this.m_Token, "没有发现继承类的类型!!! " + mc.fileMetaExtendClass.name );
                }
            }

            if(m_ExtendClassMetaType == null && this != CoreMetaClassManager.objectMetaClass && !m_IsInterfaceClass )
            {
                m_ExtendClassMetaType = new MetaType( CoreMetaClassManager.objectMetaClass );
            }

            // 接口类不需要继承自 Object，m_ExtendClassMetaType 保持 null，直接返回
            if (m_ExtendClassMetaType == null)
            {
                return;
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
                //foreach (var v in m_ExtendClass.m_InterfaceMetaType )
                //{
                //    this.m_InterfaceMetaType.Add(new MetaType(v) );
                //}
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
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, c.token, $"Error 继承的类321:{m_AllName} 在继承的父类{m_ExtendClass.m_AllName} 中已包含:{c.name} ");
                        }
                        continue;
                    }
                    if( this.m_MetaExtendMemeberVariableDict.ContainsKey( c.name ) )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, c.token, $"Error 继承的类321:{m_AllName} 在继承的父类{m_ExtendClass.m_AllName} 中已包含:{c.name} ");
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
        // 检查方法是否为本类所实现接口中声明的方法 (override标记也用于接口实现)
        // 使用名字匹配: 接口模板参数名可能不同(如 IT1 vs LT23)，且接口类可能尚未完成自身解析
        private bool IsMatchInterfaceMemberFunction(MetaMemberFunction mmf)
        {
            foreach (var it in this.m_InterfaceMetaType)
            {
                MetaClass interfaceMc = it.GetTemplateMetaClass();
                if (interfaceMc == null || interfaceMc == this) continue;

                var visited = new HashSet<MetaClass>();
                if (IsInterfaceContainsFunction(interfaceMc, mmf.name, visited)) return true;
            }
            return false;
        }
        // 递归遍历接口继承链 (interface IPet extends IAnimal, 方法可能在父接口中)
        private static bool IsInterfaceContainsFunction(MetaClass interfaceMc, string name, HashSet<MetaClass> visited)
        {
            if (interfaceMc == null || !visited.Add(interfaceMc)) return false;

            if (ContainsFunctionByName(interfaceMc.nonStaticVirtualMetaMemberFunctionList, name)) return true;
            if (ContainsFunctionByName(interfaceMc.staticMetaMemberFunctionList, name)) return true;
            if (ContainsFunctionByName(interfaceMc.fileCollectMetaMemberFunctionList, name)) return true;

            foreach (var it in interfaceMc.interfaceMetaType)
            {
                var parentMc = it.GetTemplateMetaClass();
                if (IsInterfaceContainsFunction(parentMc, name, visited)) return true;
            }
            if (IsInterfaceContainsFunction(interfaceMc.extendClass, name, visited)) return true;
            return false;
        }
        private static bool ContainsFunctionByName(List<MetaMemberFunction> list, string name)
        {
            foreach (var f in list)
            {
                if (f.name == name) return true;
            }
            return false;
        }
        public virtual void HandleExtendMemberFunction()
        {
            List<MetaMemberFunction> addmmfList = new List<MetaMemberFunction>();
            if (this.m_ExtendClass == null)
            {
                foreach (var v in m_FileCollectMetaMemberFunctionList)
                {
                    if (v.isStatic)
                    {
                        addmmfList.Add(v);
                    }
                    else
                    {
                        if (v.isWithInterface) continue;
                        //if (v.isConstructInitFunction) continue;
                        if( v.isOverrideFunction && !IsMatchInterfaceMemberFunction(v) )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, v.token,
                                "Error 类[" + this.m_AllName + "] 的方法: " + v.name + " 有override标记，但没有父类");
                        }
                        addmmfList.Add(v);
                    }
                }
            }
            else
            {
                List<MetaMemberFunction> mmfList = new List<MetaMemberFunction>();
                mmfList.AddRange(this.m_ExtendClass.m_NonStaticVirtualMetaMemberFunctionList);
                mmfList.AddRange(this.m_ExtendClass.m_StaticMetaMemberFunctionList);

                foreach (var v in mmfList)
                {
                    // 在子类定义的方法中查找与父类方法签名相同的方法
                    MetaMemberFunction matchedChild = null;
                    foreach (var v2 in m_FileCollectMetaMemberFunctionList)
                    {
                        //if (v2.isConstructInitFunction) continue;
                        if (v.IsEqualMetaFunction(v2))
                        {
                            matchedChild = v2;
                            break;
                        }
                    }

                    if (matchedChild == null)
                    {
                        // 子类没有重写该方法: 直接继承父类方法
                        // 如果父类方法是abstract方法，且当前类不是abstract类，则必须实现
                        if (v.isAbstract && !this.m_IsAbstractClass)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAbstractFunctionNeedInstance, this.token, "",
                                this.m_ExtendClass.allName, v.name, this.allName);
                        }
                        addmmfList.Add(v);
                        continue;
                    }

                    // 子类存在签名相同的方法: 用子类方法替换父类方法
                    if (v.isStatic != matchedChild.isStatic)
                    {
                        // static声明不匹配: 不能替换，父类方法与子类方法都保留
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, matchedChild.token,
                            "Error 子类[" + this.m_AllName + "] 方法: " + matchedChild.name +
                            " 与父类方法: " + this.m_ExtendClass.m_AllName + "." + v.name + " 的static声明不匹配");
                        addmmfList.Add(v);
                        addmmfList.Add(matchedChild);
                        continue;
                    }

                    if (!v.isStatic)
                    {
                        // final方法不允许被override
                        if (v.isFinal && !v.isAbstract)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreFinalFunctionCannotOverride, matchedChild.token,
                                "子类[" + this.m_AllName + "] 方法: " + matchedChild.name +
                                " 不能override父类的final方法: " + this.m_ExtendClass.m_AllName + "." + v.name);
                        }
                        // 实现抽象父方法时必须使用override标记
                        if (v.isAbstract && !matchedChild.isOverrideFunction)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, matchedChild.token,
                                "Error 子类[" + this.m_AllName + "] 方法: " + matchedChild.name +
                                " 实现了抽象父方法但未使用 override 标记");
                        }
                        // 子类重写了父类的非abstract、非final方法但未使用override标记
                        if (!v.isAbstract && !v.isFinal && !matchedChild.isOverrideFunction)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, matchedChild.token,
                                "Warning 类[" + this.m_AllName + "] 方法: " + matchedChild.name +
                                " 重写了父类方法但未使用 override 标记: " + this.m_ExtendClass.m_AllName + "." + v.name);
                        }
                        // 记录override链，供 base.xxx() 调用解析使用
                        matchedChild.SetOverrideMetaMemberFunction(v);
                    }
                    else
                    {
                        // static方法不支持override标记 (只能隐藏)
                        if (matchedChild.isOverrideFunction)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, matchedChild.token,
                                "Error 子类[" + this.m_AllName + "] 的static方法: " + matchedChild.name +
                                " 不能使用override标记");
                        }
                    }
                    addmmfList.Add(matchedChild);
                }

                // 添加子类中新增的方法 (没有与父类方法匹配的)
                foreach (var v2 in this.m_FileCollectMetaMemberFunctionList)
                {
                    if (addmmfList.Contains(v2))
                    {
                        continue;
                    }
                    // 有override标记，但父类中不存在签名相同的方法 (排除接口实现)
                    if (!v2.isStatic && v2.isOverrideFunction && v2.overrideMetaMemberFunction == null
                        && !IsMatchInterfaceMemberFunction(v2))
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, v2.token,
                            "Error 类[" + this.m_AllName + "] 方法: " + v2.name +
                            " 有override标记，但没有找到父类中相同签名的方法");
                    }
                    addmmfList.Add(v2);
                }
            }



            foreach (var v2 in addmmfList)
            {
                AddMetaMemberFunction(v2);
                if (v2.isStatic)
                {
                    m_StaticMetaMemberFunctionList.Add(v2);

                }
                else
                {
                    m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                }
            }

            // 接口类: 收集自身声明的接口函数，供 CheckInterface 使用
            // 只收集接口类自身声明的方法和父接口声明的接口函数，不含从Object继承的函数
            if (m_IsInterfaceClass)
            {
                m_InterfaceDeclareMetaMemberFunctionList.Clear();
                // 父接口声明的接口函数
                if (m_ExtendClass != null && m_ExtendClass.m_IsInterfaceClass)
                {
                    foreach (var v in m_ExtendClass.m_InterfaceDeclareMetaMemberFunctionList)
                    {
                        m_InterfaceDeclareMetaMemberFunctionList.Add(v);
                    }
                }
                // 自身声明的接口函数（排除带默认实现的 isWithInterface 方法）
                foreach (var v in m_FileCollectMetaMemberFunctionList)
                {
                    if (!v.isStatic && v.isWithInterface) continue;
                    if (!m_InterfaceDeclareMetaMemberFunctionList.Contains(v))
                    {
                        m_InterfaceDeclareMetaMemberFunctionList.Add(v);
                    }
                }
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
                List<MetaMemberFunction> forlist = new List<MetaMemberFunction>(interfaceMc.interfaceDeclareMetaMemberFunctionList);
                foreach ( var interfaceMMF in forlist )
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
                                Log.AddMetaCoreLog(LID.MetaCoreFunctionNeedOverrideFlag, interfaceMMF.token, "interface function need override flag", this.allName, interfaceMMF.name );
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
        /// <summary>
        /// 检查名称是否与当前 Module 根下的名称（data/class/enum/namespace）冲突。
        /// Project 成员（.sp 定义与 jsonc 注入）不允许与 Module 下的名称相同：
        /// 引用方使用 ModuleName.name 限定访问时，模块根下的类型与 Project 成员
        /// 共用同一个查找平面，重名会在解析时产生歧义（如 Std.Pi 无法区分是
        /// Std 模块下的类型还是 Project 定义的静态成员）。
        /// </summary>
        public static bool IsNameConflictWithModuleRoot( string name, string memberKind )
        {
            var moduleRoot = ModuleManager.instance.selfModule?.metaNode;
            if (moduleRoot == null || string.IsNullOrEmpty(name))
            {
                return false;
            }
            var mn = moduleRoot.GetChildrenMetaNodeByName(name);
            if (mn == null)
            {
                return false;
            }
            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                "Error Project类" + memberKind + "与Module下名称冲突: Project." + name
                + " 与 Module 下的 " + mn.allName + " 重名!! Project成员不允许与Module下定义的名称相同。");
            return true;
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
                if (isProjectSpecialClass && IsNameConflictWithModuleRoot(v2.name, "成员变量"))
                {
                    continue;
                }
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
                if( mn != null )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                if (isProjectSpecialClass && IsNameConflictWithModuleRoot(v2.name, "成员函数"))
                {
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
        public void SetExtendClass(MetaClass sec)
        {
            this.m_ExtendClass = sec;
        }
        /// <summary>
        /// 设置继承类的 MetaType（含模板参数），并从中派生 m_ExtendClass。
        /// 用于 ref module 加载时恢复带模板的继承关系。
        /// </summary>
        public void SetExtendClassMetaType(MetaType mt)
        {
            this.m_ExtendClassMetaType = mt;
            if (mt != null)
            {
                this.m_ExtendClass = mt.metaClass;
            }
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
        public void AddInterfaceMetaType(MetaType mt)
        {
            if (mt == null) return;
            if (!m_InterfaceMetaType.Contains(mt))
            {
                m_InterfaceMetaType.Add(mt);
            }
            if (mt.metaClass != null && !m_InterfaceClass.Contains(mt.metaClass))
            {
                m_InterfaceClass.Add(mt.metaClass);
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
        /// <summary>
        /// 运行期动态添加的非静态虚函数（如 local{} 块生成的 _Local 类函数）。
        /// HandleExtendMemberFunction 只在文件合并阶段收集函数列表，
        /// 之后动态添加的函数必须同时进入 nonStaticVirtualMetaMemberFunctionList，
        /// 否则 IR 翻译阶段不会为其生成 IRMethod，虚调用将找不到方法。
        /// </summary>
        public void AddDynamicNonStaticMemberFunction( MetaMemberFunction mmf )
        {
            if (mmf == null) return;
            if (mmf.isStatic) return;
            if (m_NonStaticVirtualMetaMemberFunctionList.Contains(mmf)) return;
            m_NonStaticVirtualMetaMemberFunctionList.Add(mmf);
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
        /// <summary>
        /// 按名字和参数个数查找非静态 set 访问器函数（如 bind data 展开生成的 set x(T)）。
        /// 不依赖值表达式即可完成的查找，供 brace 初始化构造期使用。
        /// </summary>
        public virtual MetaMemberFunction GetSetMemberFunctionByNameAndParamCount(string name, int paramCount)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
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

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(paramCount);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isSet && !fun.isStatic && !fun.isTemplateFunction)
                {
                    return fun;
                }
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(string name, int templateParamCount, MetaInputParamCollection inputParam, bool isIncludeExtendClass = true)
        {
            //if (this is MetaGenTemplateClass mgtc && name == "add")
            //{
            //    System.Console.WriteLine($"[DEBUG GetMethod] cls={this.allName} name={name} dictCount={m_MetaMemberFunctionTemplateNodeDict.Count} nonStaticCount={m_NonStaticVirtualMetaMemberFunctionList.Count}");
            //}
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
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
        /// <summary>
        /// 检查当前类是否支持 _getItem_/_setItem_ 下标访问。
        /// </summary>
        public bool HasIndexerMethod()
        {
            return GetMetaDefineGetSetMemberFunctionByName("_getItem_", null, true, false) != null
                || GetMetaDefineGetSetMemberFunctionByName("_setItem_", null, false, true) != null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndDefineTemplateInputParamCount(string name, int templateParamCount, MetaDefineParamCollection defineParam, bool isIncludeExtendClass = true)
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(defineParam != null ? defineParam.maxParamCount : 0);
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
                if (fun.IsEqualMetaDefineParamCollection(defineParam))
                    return fun;
                //}
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateAndMetaType(string name, int templateParamCount, List<MetaType> mtList, bool isIncludeExtendClass = true)
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(mtList != null ? mtList.Count : 0);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    //var gfun = fun.GetGenTemplateFunction(mtList);

                    //if( gfun != null )
                    //{
                    //    return gfun;
                    //}
                    return fun;
                }
                else
                {
                    if (fun.metaMemberParamCollection.IsEqualMetaTypeList(mtList))
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
        public MetaMemberFunction GetOperatorMetaMemberFunctionByName(string name)
        {
            switch( name)
            {
                case "_getItem_":
                    {
                        List<MetaType> mtlist = new List<MetaType>();
                        mtlist.Add(new MetaType(CoreMetaClassManager.int32MetaClass));
                        return GetMetaMemberFunctionByNameAndInputTemplateAndMetaType("_getItem_", 0, mtlist );
                    }
                case "_setItem_":
                    {
                        List<MetaType> mtlist = new List<MetaType>();
                        mtlist.Add(new MetaType(CoreMetaClassManager.int32MetaClass));
                        mtlist.Add(new MetaType(CoreMetaClassManager.objectMetaClass));
                        return GetMetaMemberFunctionByNameAndInputTemplateAndMetaType("_setItem_", 0, mtlist );
                    }
            }
            return null;
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
