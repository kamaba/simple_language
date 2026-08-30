
#树结构库：N 叉树（左孩子右兄弟）+ 二叉树 + 二叉搜索树，最通用树算法的统一实现。
#本文件五个类同时被 Xml/Yaml/Json/Toml/Config(ini) 等层级数据结构复用：
#   - TreeNode<T> 的 _name（标签名/键名/节名）与 _value（文本/标量值）直接对应文档节点；
#   - select("a/b/0/c") 路径选择、childByName、buildPath 与 XPath 风格路径互通。
#核心数据结构：
#   N 叉树（左孩子右兄弟表示）：
#     TreeNode<T>: _name(可选) _value(可选) _parent _firstChild _lastChild
#                  _prevSibling _nextSibling _childCount
#     Tree<T>    : _root _iterNode/_index/_current（先序迭代器）
#   二叉树：
#     BinaryNode<T>: _value _left _right _parent
#     BinaryTree<T>: _root _iterNode/_index/_current（中序迭代器）
#     BinarySearchTree<T> extends BinaryTree<T>
#设计（与 Queue/LinkedList/Set 一致的分层）：
#   - SL 层：泛型实例化（TreeNode<T>/BinaryNode<T> 须由前端实例化）、新节点创建、数组分配；
#   - C 层：指针手术（挂接/摘除/防环）、遍历（先序/中序/后序/层序）、查找、
#     高度/深度/计数、LCA、路径、镜像、BST 插入删除等全部耗时操作
#     通过 SystemTree*/SystemBinary*/SystemBst* 系统调用映射到 CVM 原生实现
#     （tree_system_method.c）。
#遍历序约定：N 叉 0=先序 1=后序 2=层序；二叉 0=先序 1=中序 2=后序 3=层序。
#高度/深度约定：空树 0，单节点高度 1，根深度 1。
#查找未命中返回 null（与本库其他容器越界返回 null 的语义一致）。

# ============================================================================
# N 叉树节点（左孩子右兄弟表示，可直接充当 Xml/Yaml/Json/Toml/Config 的文档节点）
# ============================================================================
public class TreeNode<T> extends Object
{
    # --- 核心字段 ---
    string _name = null                             # 节点名（文档场景：标签名/键名/节名）
    T _value = null                                 # 节点值（文档场景：文本/标量值）
    TreeNode<T> _parent = null                      # 父节点（根为 null）
    TreeNode<T> _firstChild = null                  # 首孩子
    TreeNode<T> _lastChild = null                   # 末孩子
    TreeNode<T> _prevSibling = null                 # 前兄弟
    TreeNode<T> _nextSibling = null                 # 后兄弟
    int _childCount = 0                             # 直接孩子数（由挂接/摘除维护）

    override _init_()
    {
    }
    #指定节点名构造（文档场景：仅命名节点）
    void _init_( string name )
    {
        this._name = name
    }
    #指定节点名与值构造
    void _init_( string name, T value )
    {
        this._name = name
        this._value = value
    }

    # ── 属性 ──

    get string name()
    {
        ret this._name
    }
    set void name( string value )
    {
        this._name = value
    }
    get T value()
    {
        ret this._value
    }
    set void value( T value )
    {
        this._value = value
    }
    get TreeNode<T> parent()
    {
        ret this._parent
    }
    get TreeNode<T> firstChild()
    {
        ret this._firstChild
    }
    get TreeNode<T> lastChild()
    {
        ret this._lastChild
    }
    get TreeNode<T> prevSibling()
    {
        ret this._prevSibling
    }
    get TreeNode<T> nextSibling()
    {
        ret this._nextSibling
    }
    get int childCount()
    {
        ret this._childCount
    }
    get bool isLeaf()
    {
        if this._firstChild == null
        {
            ret true
        }
        ret false
    }
    get bool isRoot()
    {
        if this._parent == null
        {
            ret true
        }
        ret false
    }
    get bool hasChildren()
    {
        if this._firstChild != null
        {
            ret true
        }
        ret false
    }
    #从根到本节点的 '/' 分隔路径（用各节点 _name，文档场景的 XPath 风格定位）
    get string path()
    {
        ret SystemTreeBuildPath( this )
    }

    # ── 孩子操作（指针手术在 VM 层完成）──

    #挂接已有节点为本节点的末子（自动脱离原父，拒绝自挂/挂到子孙防环）
    public bool attach( TreeNode<T> node )
    {
        ret SystemTreeAttach( this, node )
    }
    #创建并挂接命名子节点
    public TreeNode<T> addChild( string name )
    {
        TreeNode<T> node = TreeNode<T>( name )
        if SystemTreeAttach( this, node )
        {
            ret node
        }
        ret null
    }
    #创建并挂接带值子节点
    public TreeNode<T> addChild( string name, T value )
    {
        TreeNode<T> node = TreeNode<T>( name, value )
        if SystemTreeAttach( this, node )
        {
            ret node
        }
        ret null
    }
    #摘除本节点（保留其子树成为新的独立树），根节点返回 false
    public bool detach()
    {
        ret SystemTreeDetach( this )
    }
    #取第 index 个直接孩子（0 起，越界返回 null）
    public TreeNode<T> childAt( int index )
    {
        ret SystemTreeChildAt( this, index ) as TreeNode<T>
    }
    #取第一个名为 name 的直接孩子（文档场景：按标签/键取子节点）
    public TreeNode<T> childByName( string name )
    {
        ret SystemTreeChildByName( this, name ) as TreeNode<T>
    }
    #直接孩子填入 arr（须预分配足够容量），返回实际个数
    public int childrenToArray( Array<TreeNode<T>> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemTreeChildrenToArray( this, arr )
    }
    #直接孩子列表（新数组）
    public Array<TreeNode<T>> children()
    {
        Array<TreeNode<T>> arr = Array<TreeNode<T>>( this._childCount )
        SystemTreeChildrenToArray( this, arr )
        ret arr
    }
    #本节点与其全部子孙是否包含 target（按对象身份）
    public bool contains( TreeNode<T> target )
    {
        ret SystemTreeContains( this, target )
    }

    # ── 子树算法（全部在 VM 层完成）──

    #子树节点总数（含自身）
    public int count()
    {
        ret SystemTreeCount( this )
    }
    #子树高度（单节点 1）
    public int height()
    {
        ret SystemTreeHeight( this )
    }
    #节点深度（根 1）
    public int depth()
    {
        ret SystemTreeDepth( this )
    }
    #先序查找第一个 _value 相等的节点（未命中返回 null）
    public TreeNode<T> find( T value )
    {
        ret SystemTreeFindFirst( this, value ) as TreeNode<T>
    }
    #先序查找第一个 _name 相等的节点（文档场景：按名深查）
    public TreeNode<T> findByName( string name )
    {
        ret SystemTreeFindName( this, name ) as TreeNode<T>
    }
    #先序收集全部 _value 相等节点到 arr（须预分配足够容量），返回实际个数
    public int findAll( T value, Array<TreeNode<T>> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemTreeFindAll( this, value, arr )
    }
    #先序收集全部 _value 相等节点（新数组）
    public Array<TreeNode<T>> findAll( T value )
    {
        int total = SystemTreeCount( this )
        Array<TreeNode<T>> arr = Array<TreeNode<T>>( total )
        int n = SystemTreeFindAll( this, value, arr )
        if n == total
        {
            ret arr
        }
        Array<TreeNode<T>> res = Array<TreeNode<T>>( n )
        for i = 0, i < n, i++
        {
            res._setItem_( i, arr._getItem_( i ) )
        }
        ret res
    }
    #路径选择（"a/b/0/c"：名字定位，纯数字段为孩子下标；空路径返回自身）
    public TreeNode<T> select( string path )
    {
        ret SystemTreeSelect( this, path ) as TreeNode<T>
    }
    #最近公共祖先（不同树返回 null）
    public TreeNode<T> lca( TreeNode<T> other )
    {
        ret SystemTreeLca( this, other ) as TreeNode<T>
    }
    #从本节点到根（含两端）的节点数组（本节点在前）
    public Array<TreeNode<T>> pathToRoot()
    {
        int n = SystemTreeDepth( this )
        Array<TreeNode<T>> arr = Array<TreeNode<T>>( n )
        SystemTreePathToRoot( this, arr )
        ret arr
    }

    # ── 遍历（节点版/取值版，order：0=先序 1=后序 2=层序）──

    #按序填充节点数组（须预分配足够容量），返回实际个数
    public int nodesToArray( int order, Array<TreeNode<T>> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemTreeFillOrder( this, order, arr )
    }
    #按序填充各节点 _value 数组（须预分配足够容量），返回实际个数
    public int valuesToArray( int order, Array<T> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemTreeFillValues( this, order, arr )
    }
    #按序节点数组（新数组，长度精确）
    public Array<TreeNode<T>> nodes( int order )
    {
        int n = SystemTreeCount( this )
        Array<TreeNode<T>> arr = Array<TreeNode<T>>( n )
        SystemTreeFillOrder( this, order, arr )
        ret arr
    }
    #按序取值数组（新数组，长度精确）
    public Array<T> values( int order )
    {
        int n = SystemTreeCount( this )
        Array<T> arr = Array<T>( n )
        SystemTreeFillValues( this, order, arr )
        ret arr
    }
    #先序节点数组（根->子，文档场景的默认遍历）
    public Array<TreeNode<T>> preorder()
    {
        ret this.nodes( 0 )
    }
    #后序节点数组（孩子->根，适合先处理子节点再自毁的释放序）
    public Array<TreeNode<T>> postorder()
    {
        ret this.nodes( 1 )
    }
    #层序节点数组（逐层，按兄弟链顺序）
    public Array<TreeNode<T>> levelOrder()
    {
        ret this.nodes( 2 )
    }
    #先序取值数组
    public Array<T> preorderValues()
    {
        ret this.values( 0 )
    }
    #后序取值数组
    public Array<T> postorderValues()
    {
        ret this.values( 1 )
    }
    #层序取值数组
    public Array<T> levelOrderValues()
    {
        ret this.values( 2 )
    }

    #输出 name=value（无值只输出 name）形式
    override string toString()
    {
        if this._name == null
        {
            if this._value == null
            {
                ret "?"
            }
            ret this._value.toString()
        }
        if this._value == null
        {
            ret this._name
        }
        ret this._name + "=" + this._value.toString()
    }
}

# ============================================================================
# N 叉树（持有根节点，foreach 按先序迭代 _value）
# ============================================================================
public class Tree<T> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    # --- 核心字段 ---
    TreeNode<T> _root = null                        # 根节点

    # --- 迭代器字段（先序迭代）---
    TreeNode<T> _iterNode = null
    int _index = -1
    T _current = null

    override _init_()
    {
    }
    #以已有节点为根构造（节点先脱离原父）
    void _init_( TreeNode<T> root )
    {
        if root != null
        {
            SystemTreeSetRoot( this, root )
        }
    }
    #创建命名根节点
    void _init_( string rootName )
    {
        this._root = TreeNode<T>( rootName )
    }
    #创建命名带值根节点
    void _init_( string rootName, T rootValue )
    {
        this._root = TreeNode<T>( rootName, rootValue )
    }

    # ── 属性 ──

    get TreeNode<T> root()
    {
        ret this._root
    }
    set void root( TreeNode<T> node )
    {
        SystemTreeSetRoot( this, node )
    }
    #节点总数（Python len / C# Count）
    get int length()
    {
        if this._root == null
        {
            ret 0
        }
        ret SystemTreeCount( this._root )
    }
    get bool isEmpty()
    {
        if this._root == null
        {
            ret true
        }
        ret false
    }
    get bool isNotEmpty()
    {
        if this._root != null
        {
            ret true
        }
        ret false
    }
    #树高（空树 0）
    get int height()
    {
        if this._root == null
        {
            ret 0
        }
        ret SystemTreeHeight( this._root )
    }
    #根节点名字（文档场景：根标签名）
    get string rootName()
    {
        if this._root == null
        {
            ret null
        }
        ret this._root._name
    }

    # ── 根操作 ──

    #设根（null 表示清根；节点先脱离原父，迭代器重置）
    public void setRoot( TreeNode<T> node )
    {
        SystemTreeSetRoot( this, node )
    }
    #创建并设置命名根节点，返回新根
    public TreeNode<T> addRoot( string name )
    {
        this._root = TreeNode<T>( name )
        SystemTreeSetRoot( this, this._root )
        ret this._root
    }
    #创建并设置命名带值根节点，返回新根
    public TreeNode<T> addRoot( string name, T value )
    {
        this._root = TreeNode<T>( name, value )
        SystemTreeSetRoot( this, this._root )
        ret this._root
    }

    # ── 子树算法（委托根节点，全部在 VM 层完成）──

    #先序查找第一个 _value 相等节点
    public TreeNode<T> find( T value )
    {
        if this._root == null
        {
            ret null
        }
        ret SystemTreeFindFirst( this._root, value ) as TreeNode<T>
    }
    #先序查找第一个 _name 相等节点（文档场景：按名深查）
    public TreeNode<T> findByName( string name )
    {
        if this._root == null
        {
            ret null
        }
        ret SystemTreeFindName( this._root, name ) as TreeNode<T>
    }
    #先序收集全部 _value 相等节点（新数组）
    public Array<TreeNode<T>> findAll( T value )
    {
        if this._root == null
        {
            ret Array<TreeNode<T>>( 0 )
        }
        ret this._root.findAll( value )
    }
    #路径选择（"a/b/0/c"：名字定位，纯数字段为孩子下标）
    public TreeNode<T> select( string path )
    {
        if this._root == null
        {
            ret null
        }
        ret SystemTreeSelect( this._root, path ) as TreeNode<T>
    }
    #最近公共祖先（不同树返回 null）
    public TreeNode<T> lca( TreeNode<T> a, TreeNode<T> b )
    {
        if a == null || b == null
        {
            ret null
        }
        ret SystemTreeLca( a, b ) as TreeNode<T>
    }
    #是否包含某节点（按对象身份）
    public bool contains( TreeNode<T> node )
    {
        if this._root == null || node == null
        {
            ret false
        }
        ret SystemTreeContains( this._root, node )
    }
    #是否包含某值
    public bool containsValue( T value )
    {
        if this.find( value ) != null
        {
            ret true
        }
        ret false
    }

    # ── 遍历（order：0=先序 1=后序 2=层序）──

    #按序节点数组（新数组，长度精确）
    public Array<TreeNode<T>> nodes( int order )
    {
        if this._root == null
        {
            ret Array<TreeNode<T>>( 0 )
        }
        ret this._root.nodes( order )
    }
    #按序取值数组（新数组，长度精确）
    public Array<T> values( int order )
    {
        if this._root == null
        {
            ret Array<T>( 0 )
        }
        ret this._root.values( order )
    }
    public Array<TreeNode<T>> preorder()
    {
        ret this.nodes( 0 )
    }
    public Array<TreeNode<T>> postorder()
    {
        ret this.nodes( 1 )
    }
    public Array<TreeNode<T>> levelOrder()
    {
        ret this.nodes( 2 )
    }
    public Array<T> preorderValues()
    {
        ret this.values( 0 )
    }
    public Array<T> postorderValues()
    {
        ret this.values( 1 )
    }
    public Array<T> levelOrderValues()
    {
        ret this.values( 2 )
    }

    # ── 删除 ──

    #清空（置空根并重置迭代器，节点由 GC 回收）
    public void clear()
    {
        SystemTreeClear( this )
    }

    # ── 迭代器（foreach 热路径，SystemTreeMoveNext 在 VM 层按先序推进）──

    override void reset()
    {
        this._index = -1
        this._current = null
        this._iterNode = null
    }
    override bool moveNext()
    {
        ret SystemTreeMoveNext( this )
    }
    override get T current()
    {
        ret this._current
    }
    override get Core.IIterator<T> iterator()
    {
        this.reset()
        ret this
    }
    get int index()
    {
        ret this._index
    }

    #输出 Tree(root=a,b,c) 格式（先序取值）
    override string toString()
    {
        if this._root == null
        {
            ret "Tree()"
        }
        string showstr = "Tree(" + this._root.toString()
        Array<TreeNode<T>> arr = this.preorder()
        for i = 1, i < arr.length, i++
        {
            showstr += "," + arr._getItem_( i ).toString()
        }
        ret showstr + ")"
    }
}

# ============================================================================
# 二叉树节点（三叉链：_left/_right/_parent）
# ============================================================================
public class BinaryNode<T> extends Object
{
    # --- 核心字段 ---
    T _value = null                                 # 节点值
    BinaryNode<T> _left = null                      # 左孩子
    BinaryNode<T> _right = null                     # 右孩子
    BinaryNode<T> _parent = null                    # 父节点（根为 null）

    override _init_()
    {
    }
    #指定值构造
    void _init_( T value )
    {
        this._value = value
    }

    # ── 属性 ──

    get T value()
    {
        ret this._value
    }
    set void value( T value )
    {
        this._value = value
    }
    get BinaryNode<T> left()
    {
        ret this._left
    }
    set void left( BinaryNode<T> node )
    {
        if node != null
        {
            SystemBinaryLink( this, node, true )
            ret
        }
        if this._left != null
        {
            SystemBinaryUnlink( this._left )
        }
    }
    get BinaryNode<T> right()
    {
        ret this._right
    }
    set void right( BinaryNode<T> node )
    {
        if node != null
        {
            SystemBinaryLink( this, node, false )
            ret
        }
        if this._right != null
        {
            SystemBinaryUnlink( this._right )
        }
    }
    get BinaryNode<T> parent()
    {
        ret this._parent
    }
    get bool isLeaf()
    {
        if this._left == null && this._right == null
        {
            ret true
        }
        ret false
    }
    get bool isRoot()
    {
        if this._parent == null
        {
            ret true
        }
        ret false
    }
    get bool hasLeft()
    {
        if this._left != null
        {
            ret true
        }
        ret false
    }
    get bool hasRight()
    {
        if this._right != null
        {
            ret true
        }
        ret false
    }

    # ── 挂接/摘除（指针手术在 VM 层完成）──

    #接到本节点左槽（node 先脱离原父，旧占用者脱离）
    public bool linkLeft( BinaryNode<T> node )
    {
        ret SystemBinaryLink( this, node, true )
    }
    #接到本节点右槽
    public bool linkRight( BinaryNode<T> node )
    {
        ret SystemBinaryLink( this, node, false )
    }
    #创建带值节点接到左槽，返回新节点
    public BinaryNode<T> addLeft( T value )
    {
        BinaryNode<T> node = BinaryNode<T>( value )
        if SystemBinaryLink( this, node, true )
        {
            ret node
        }
        ret null
    }
    #创建带值节点接到右槽，返回新节点
    public BinaryNode<T> addRight( T value )
    {
        BinaryNode<T> node = BinaryNode<T>( value )
        if SystemBinaryLink( this, node, false )
        {
            ret node
        }
        ret null
    }
    #脱离父节点（保留其子树成独立树），根节点返回 false
    public bool unlink()
    {
        ret SystemBinaryUnlink( this )
    }

    # ── 子树算法（全部在 VM 层完成）──

    #子树节点总数（含自身）
    public int count()
    {
        ret SystemBinaryCount( this )
    }
    #子树高度（单节点 1）
    public int height()
    {
        ret SystemBinaryHeight( this )
    }
    #节点深度（根 1）
    public int depth()
    {
        ret SystemBinaryDepth( this )
    }
    #先序查找第一个 _value 相等节点
    public BinaryNode<T> find( T value )
    {
        ret SystemBinaryFind( this, value ) as BinaryNode<T>
    }
    #先序收集全部 _value 相等节点到 arr（须预分配足够容量），返回实际个数
    public int findAll( T value, Array<BinaryNode<T>> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemBinaryFindAll( this, value, arr )
    }
    #先序收集全部 _value 相等节点（新数组）
    public Array<BinaryNode<T>> findAll( T value )
    {
        int total = SystemBinaryCount( this )
        Array<BinaryNode<T>> arr = Array<BinaryNode<T>>( total )
        int n = SystemBinaryFindAll( this, value, arr )
        if n == total
        {
            ret arr
        }
        Array<BinaryNode<T>> res = Array<BinaryNode<T>>( n )
        for i = 0, i < n, i++
        {
            res._setItem_( i, arr._getItem_( i ) )
        }
        ret res
    }
    #是否包含某值
    public bool contains( T value )
    {
        ret SystemBinaryContains( this, value )
    }
    #最近公共祖先（不同树返回 null）
    public BinaryNode<T> lca( BinaryNode<T> other )
    {
        ret SystemBinaryLca( this, other ) as BinaryNode<T>
    }
    #从根到本节点的取值路径（本节点值在前）
    public Array<T> pathFromRoot()
    {
        int n = SystemBinaryDepth( this )
        if n <= 0
        {
            ret Array<T>( 0 )
        }
        BinaryNode<T> r = this
        while r._parent != null
        {
            r = r._parent
        }
        Array<T> arr = Array<T>( n )
        SystemBinaryPathToNode( r, this, arr )
        ret arr
    }
    #镜像翻转本子树（原地）
    public BinaryNode<T> invert()
    {
        ret SystemBinaryInvert( this ) as BinaryNode<T>
    }
    #本子树是否镜像对称
    public bool isSymmetric()
    {
        ret SystemBinaryIsSymmetric( this )
    }
    #本子树是否平衡（任意节点左右高差 <= 1）
    public bool isBalanced()
    {
        ret SystemBinaryIsBalanced( this )
    }
    #本子树是否二叉搜索树（中序严格递增）
    public bool isBst()
    {
        ret SystemBinaryIsBst( this )
    }

    # ── 遍历（order：0=先序 1=中序 2=后序 3=层序）──

    #按序填充节点数组（须预分配足够容量），返回实际个数
    public int nodesToArray( int order, Array<BinaryNode<T>> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemBinaryFillOrder( this, order, arr )
    }
    #按序填充各节点 _value 数组（须预分配足够容量），返回实际个数
    public int valuesToArray( int order, Array<T> arr )
    {
        if arr == null
        {
            ret 0
        }
        ret SystemBinaryFillValues( this, order, arr )
    }
    #按序节点数组（新数组，长度精确）
    public Array<BinaryNode<T>> nodes( int order )
    {
        int n = SystemBinaryCount( this )
        Array<BinaryNode<T>> arr = Array<BinaryNode<T>>( n )
        SystemBinaryFillOrder( this, order, arr )
        ret arr
    }
    #按序取值数组（新数组，长度精确）
    public Array<T> values( int order )
    {
        int n = SystemBinaryCount( this )
        Array<T> arr = Array<T>( n )
        SystemBinaryFillValues( this, order, arr )
        ret arr
    }
    public Array<BinaryNode<T>> preorder()
    {
        ret this.nodes( 0 )
    }
    public Array<BinaryNode<T>> inorder()
    {
        ret this.nodes( 1 )
    }
    public Array<BinaryNode<T>> postorder()
    {
        ret this.nodes( 2 )
    }
    public Array<BinaryNode<T>> levelOrder()
    {
        ret this.nodes( 3 )
    }
    public Array<T> preorderValues()
    {
        ret this.values( 0 )
    }
    public Array<T> inorderValues()
    {
        ret this.values( 1 )
    }
    public Array<T> postorderValues()
    {
        ret this.values( 2 )
    }
    public Array<T> levelOrderValues()
    {
        ret this.values( 3 )
    }

    #输出值形式
    override string toString()
    {
        if this._value == null
        {
            ret "?"
        }
        ret this._value.toString()
    }
}

# ============================================================================
# 二叉树（持有根节点，foreach 按中序迭代 _value）
# ============================================================================
public class BinaryTree<T> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    # --- 核心字段 ---
    BinaryNode<T> _root = null                      # 根节点

    # --- 迭代器字段（中序迭代）---
    BinaryNode<T> _iterNode = null
    int _index = -1
    T _current = null

    override _init_()
    {
    }
    #以已有节点为根构造（节点先脱离原父）
    void _init_( BinaryNode<T> root )
    {
        if root != null
        {
            SystemBinarySetRoot( this, root )
        }
    }
    #创建带值根节点
    void _init_( T rootValue )
    {
        this._root = BinaryNode<T>( rootValue )
    }

    # ── 属性 ──

    get BinaryNode<T> root()
    {
        ret this._root
    }
    set void root( BinaryNode<T> node )
    {
        SystemBinarySetRoot( this, node )
    }
    #节点总数
    get int length()
    {
        if this._root == null
        {
            ret 0
        }
        ret SystemBinaryCount( this._root )
    }
    get bool isEmpty()
    {
        if this._root == null
        {
            ret true
        }
        ret false
    }
    get bool isNotEmpty()
    {
        if this._root != null
        {
            ret true
        }
        ret false
    }
    #树高（空树 0）
    get int height()
    {
        if this._root == null
        {
            ret 0
        }
        ret SystemBinaryHeight( this._root )
    }

    # ── 构建（新节点在 SL 层实例化，挂接在 VM 层完成）──

    #设根（null 表示清根；节点先脱离原父，迭代器重置）
    public void setRoot( BinaryNode<T> node )
    {
        SystemBinarySetRoot( this, node )
    }
    #创建并设置带值根节点，返回新根
    public BinaryNode<T> addRoot( T value )
    {
        this._root = BinaryNode<T>( value )
        SystemBinarySetRoot( this, this._root )
        ret this._root
    }
    #node 接到 parent 左槽
    public bool linkLeft( BinaryNode<T> parent, BinaryNode<T> node )
    {
        if parent == null
        {
            ret false
        }
        ret SystemBinaryLink( parent, node, true )
    }
    #node 接到 parent 右槽
    public bool linkRight( BinaryNode<T> parent, BinaryNode<T> node )
    {
        if parent == null
        {
            ret false
        }
        ret SystemBinaryLink( parent, node, false )
    }
    #创建带值节点接到 parent 左槽，返回新节点
    public BinaryNode<T> addLeft( BinaryNode<T> parent, T value )
    {
        if parent == null
        {
            ret null
        }
        ret parent.addLeft( value )
    }
    #创建带值节点接到 parent 右槽，返回新节点
    public BinaryNode<T> addRight( BinaryNode<T> parent, T value )
    {
        if parent == null
        {
            ret null
        }
        ret parent.addRight( value )
    }

    # ── 算法（委托根节点，全部在 VM 层完成）──

    #先序查找第一个 _value 相等节点
    public BinaryNode<T> find( T value )
    {
        if this._root == null
        {
            ret null
        }
        ret SystemBinaryFind( this._root, value ) as BinaryNode<T>
    }
    #先序收集全部 _value 相等节点（新数组）
    public Array<BinaryNode<T>> findAll( T value )
    {
        if this._root == null
        {
            ret Array<BinaryNode<T>>( 0 )
        }
        ret this._root.findAll( value )
    }
    #是否包含某值
    public bool contains( T value )
    {
        if this._root == null
        {
            ret false
        }
        ret SystemBinaryContains( this._root, value )
    }
    #pattern 是否为本树某先序起点的完全同构同值子树
    public bool containsSubtree( BinaryNode<T> pattern )
    {
        if this._root == null || pattern == null
        {
            ret false
        }
        ret SystemBinaryContainsSubtree( this._root, pattern )
    }
    #最近公共祖先（不同树返回 null）
    public BinaryNode<T> lca( BinaryNode<T> a, BinaryNode<T> b )
    {
        if a == null || b == null
        {
            ret null
        }
        ret SystemBinaryLca( a, b ) as BinaryNode<T>
    }
    #从根到 target 的取值路径（找不到返回空数组）
    public Array<T> pathToNode( BinaryNode<T> target )
    {
        if this._root == null || target == null
        {
            ret Array<T>( 0 )
        }
        int n = SystemBinaryCount( this._root )
        Array<T> arr = Array<T>( n )
        int w = SystemBinaryPathToNode( this._root, target, arr )
        if w == n
        {
            ret arr
        }
        Array<T> res = Array<T>( w )
        for i = 0, i < w, i++
        {
            res._setItem_( i, arr._getItem_( i ) )
        }
        ret res
    }
    #镜像翻转整树（原地）
    public void invert()
    {
        if this._root != null
        {
            SystemBinaryInvert( this._root )
        }
    }
    #是否镜像对称
    public bool isSymmetric()
    {
        if this._root == null
        {
            ret true
        }
        ret SystemBinaryIsSymmetric( this._root )
    }
    #是否平衡（任意节点左右高差 <= 1）
    public bool isBalanced()
    {
        if this._root == null
        {
            ret true
        }
        ret SystemBinaryIsBalanced( this._root )
    }
    #是否二叉搜索树（中序严格递增）
    public bool isBst()
    {
        if this._root == null
        {
            ret true
        }
        ret SystemBinaryIsBst( this._root )
    }

    # ── 遍历（order：0=先序 1=中序 2=后序 3=层序）──

    #按序节点数组（新数组，长度精确）
    public Array<BinaryNode<T>> nodes( int order )
    {
        if this._root == null
        {
            ret Array<BinaryNode<T>>( 0 )
        }
        ret this._root.nodes( order )
    }
    #按序取值数组（新数组，长度精确）
    public Array<T> values( int order )
    {
        if this._root == null
        {
            ret Array<T>( 0 )
        }
        ret this._root.values( order )
    }
    public Array<BinaryNode<T>> preorder()
    {
        ret this.nodes( 0 )
    }
    public Array<BinaryNode<T>> inorder()
    {
        ret this.nodes( 1 )
    }
    public Array<BinaryNode<T>> postorder()
    {
        ret this.nodes( 2 )
    }
    public Array<BinaryNode<T>> levelOrder()
    {
        ret this.nodes( 3 )
    }
    public Array<T> preorderValues()
    {
        ret this.values( 0 )
    }
    public Array<T> inorderValues()
    {
        ret this.values( 1 )
    }
    public Array<T> postorderValues()
    {
        ret this.values( 2 )
    }
    public Array<T> levelOrderValues()
    {
        ret this.values( 3 )
    }

    # ── 删除 ──

    #清空（置空根并重置迭代器，节点由 GC 回收）
    public void clear()
    {
        SystemBinaryClear( this )
    }

    # ── 迭代器（foreach 热路径，SystemBinaryMoveNext 在 VM 层按中序推进）──

    override void reset()
    {
        this._index = -1
        this._current = null
        this._iterNode = null
    }
    override bool moveNext()
    {
        ret SystemBinaryMoveNext( this )
    }
    override get T current()
    {
        ret this._current
    }
    override get Core.IIterator<T> iterator()
    {
        this.reset()
        ret this
    }
    get int index()
    {
        ret this._index
    }

    #输出 BinaryTree(a,b,c) 格式（中序取值）
    override string toString()
    {
        if this._root == null
        {
            ret "BinaryTree()"
        }
        Array<T> arr = this.inorderValues()
        string showstr = "BinaryTree(" + arr._getItem_( 0 ).toString()
        for i = 1, i < arr.length, i++
        {
            var v = arr._getItem_( i )
            if v == null
            {
                showstr += ",null"
            }
            else
            {
                showstr += "," + v.toString()
            }
        }
        ret showstr + ")"
    }
}

# ============================================================================
# 二叉搜索树（继承 BinaryTree，插入/查找/删除全部在 VM 层完成）
# 值须可有序比较（数值按大小、字符串按字典序）；重复值插入返回已存在节点。
# ============================================================================
public class BinarySearchTree<T> extends BinaryTree<T>
{
    override _init_()
    {
    }
    #创建带值根节点
    override void _init_( T rootValue )
    {
        this._root = BinaryNode<T>( rootValue )
    }

    #插入值（返回实际落位节点；重复值返回已存在节点，不重复插入）
    public BinaryNode<T> insert( T value )
    {
        BinaryNode<T> node = BinaryNode<T>( value )
        ret SystemBstInsert( this, node ) as BinaryNode<T>
    }
    #批量插入
    public void insertRange( Array<T> values )
    {
        if values == null
        {
            ret
        }
        for i = 0, i < values.length, i++
        {
            var v = SystemArrayGetValueThis( values, i )
            if v != null
            {
                this.insert( v as T )
            }
        }
    }
    #二分下降查找（未命中返回 null）
    public BinaryNode<T> findNode( T value )
    {
        ret SystemBstFind( this, value ) as BinaryNode<T>
    }
    #是否包含某值（BST 专用二分下降版）
    public override bool contains( T value )
    {
        if SystemBstFind( this, value ) != null
        {
            ret true
        }
        ret false
    }
    #删除某值（叶/单孩/双孩三 case：双孩拷中序后继值再删后继）
    public bool remove( T value )
    {
        ret SystemBstRemove( this, value )
    }
    #最小值节点（最左节点）
    public BinaryNode<T> minNode()
    {
        ret SystemBstMinNode( this ) as BinaryNode<T>
    }
    #最大值节点（最右节点）
    public BinaryNode<T> maxNode()
    {
        ret SystemBstMaxNode( this ) as BinaryNode<T>
    }
    #最小值（空树返回 null）
    public T min()
    {
        BinaryNode<T> node = SystemBstMinNode( this ) as BinaryNode<T>
        if node == null
        {
            ret null
        }
        ret node._value
    }
    #最大值（空树返回 null）
    public T max()
    {
        BinaryNode<T> node = SystemBstMaxNode( this ) as BinaryNode<T>
        if node == null
        {
            ret null
        }
        ret node._value
    }
}
