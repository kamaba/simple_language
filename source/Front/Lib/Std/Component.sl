# 组件基类（Unity 风格，仅含共用方法）
#
# 设计取舍：
#   - 不引用 GameObject / Transform 等「Unity 独立类」，保持 Std 模块自包含、无循环依赖。
#   - 采用轻量「组件即节点」组合模型：每个 Component 持有自己的子组件列表（components），
#     所有 GetComponent / 消息 API 都在这棵树上查找。真实引擎里由 GameObject 持有 Component
#     并设置 gameObject 反向引用；这里把共用逻辑收敛到 Component 自身，避免跨模块耦合。
#   - 若后续需要 Component.gameObject / Component.transform 快捷属性，由 GameObject 持有
#     Component 并在挂载时设置 parent / gameObject 即可，不必在本文件引入 Transform。
#
# 对照 Unity 的常用 API：
#   GetComponent<T> / GetComponents<T> / GetComponent(Type) / GetComponent(string)
#   GetComponentInChildren<T> / GetComponentInParent<T>
#   GetComponentsInChildren<T> / GetComponentsInParent<T>
#   TryGetComponent<T> / CompareTag / SendMessage / SendMessageUpwards / BroadcastMessage
public class Component extends Object
{
    # 容器（上级组件）；由挂载时设置
    public Component parent = null
    # 自身挂载的子组件
    public List<Component> components = List<Component>(4)

    # 是否启用（对应 Unity 的 enabled）
    public bool enabled = true
    # 标签（对应 Unity 的 tag）
    public string tag = "Untagged"
    # 名称（便于调试，对应 Unity 的 name）
    public string name = "Component"

    # ── 构造 ───────────────────────────────────────────
    public void _init_()
    {
        this.parent = null
        this.components = List<Component>(4)
        this.enabled = true
        this.tag = "Untagged"
        this.name = "Component"
    }

    # ── 启用状态 ───────────────────────────────────────
    # 自身启用且整条 parent 链都启用时才算「激活且启用」
    public get bool isActiveAndEnabled()
    {
        if !this.enabled
        {
            ret false
        }
        if this.parent != null
        {
            ret this.parent.isActiveAndEnabled
        }
        ret true
    }

    public void setEnabled( bool value )
    {
        this.enabled = value
    }

    public bool getEnabled()
    {
        ret this.enabled
    }

    # ── 组件挂载 ───────────────────────────────────────
    public void AddComponent( Component comp )
    {
        if comp == null
        {
            ret
        }
        comp.parent = this
        this.components.add( comp )
    }

    # ── 组件查询（自身直接子级）────────────────────────
    public T GetComponent<T>()
    {
        for c in this.components
        {
            T t = c as T
            if t != null
            {
                ret t
            }
        }
        ret null
    }

    public Component GetComponent( Type type )
    {
        for c in this.components
        {
            if c.type == type
            {
                ret c
            }
        }
        ret null
    }

    public Component GetComponent( string typeName )
    {
        for c in this.components
        {
            if c.type.toString() == typeName
            {
                ret c
            }
        }
        ret null
    }

    public List<Component> GetComponents<T>()
    {
        List<Component> rtlist = List<Component>(4)
        for c in this.components
        {
            T t = c as T
            if t != null
            {
                rtlist.add(c)
            }
        }
        ret rtlist
    }

    # ── 向下查找（含子组件树）──────────────────────────
    public T GetComponentInChildren<T>( bool includeInactive )
    {
        for c in this.components
        {
            if includeInactive || c.enabled
            {
                T t = c as T
                if t != null
                {
                    ret t
                }
            }
            T rt = c.GetComponentInChildren<T>( includeInactive )
            if rt != null
            {
                ret rt
            }
        }
        ret null
    }

    # ── 向上查找（parent 链）───────────────────────────
    public T GetComponentInParent<T>( bool includeInactive )
    {
        Component p = this
        while p != null
        {
            if includeInactive || p.enabled
            {
                T t = p as T
                if t != null
                {
                    ret t
                }
            }
            p = p.parent
        }
        ret null
    }

    public List<Component> GetComponentsInChildren<T>( bool includeInactive )
    {
        List<Component> result = List<Component>(4)
        this._collectInChildren<T>( result, includeInactive )
        ret result
    }

    void _collectInChildren<T>( List<Component> result, bool includeInactive )
    {
        for c in this.components
        {
            if includeInactive || c.enabled
            {
                T t = c as T
                if t != null
                {
                    result.add(c)
                }
            }
            c._collectInChildren<T>( result, includeInactive )
        }
    }

    public List<Component> GetComponentsInParent<T>( bool includeInactive )
    {
        List<Component> result = List<Component>(4)
        Component p = this
        while p != null
        {
            if includeInactive || p.enabled
            {
                T t = p as T
                if t != null
                {
                    result.add(p)
                }
            }
            p = p.parent
        }
        ret result
    }

    # 与 Unity 的 TryGetComponent<T>(out T) 对齐：直接返回组件，null 表示未找到
    public T TryGetComponent<T>()
    {
        ret this.GetComponent<T>()
    }

    # ── 标签 ───────────────────────────────────────────
    public bool CompareTag( string otherTag )
    {
        ret this.tag == otherTag
    }

    # ── 消息（简化模型：组件可 override onMessage 响应）──
    # 向自身及所有子组件发送
    public void SendMessage( string methodName )
    {
        this.onMessage( methodName )
        for c in this.components
        {
            c.SendMessage( methodName )
        }
    }

    # 向自身及所有祖先发送
    public void SendMessageUpwards( string methodName )
    {
        this.onMessage( methodName )
        if this.parent != null
        {
            this.parent.SendMessageUpwards( methodName )
        }
    }

    # 向自身及整棵子树广播
    public void BroadcastMessage( string methodName )
    {
        this.SendMessage( methodName )
    }

    # 子类可 override，以响应 SendMessage / BroadcastMessage / SendMessageUpwards
    public virtual void onMessage( string methodName )
    {
    }

    override string toString()
    {
        string s = "Component(name=" + this.name
        s = s + ", type=" + this.type.toString()
        s = s + ", enabled=" + this.enabled.toString()
        ret s + ")"
    }
}
