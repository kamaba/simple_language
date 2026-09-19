#Tree 结构功能测试：N 叉树/二叉树/二叉搜索树的构建、孩子操作、遍历（先/中/后/层序）、
#查找、路径、LCA、镜像、对称、平衡、BST 增删查；并验证 Xml/Yaml/Json/Toml/Config(ini)
#文档场景复用能力（name/value + select 路径定位）。
TreeTest
{
    # N 叉树节点基础：构造/属性/孩子操作/按下标与按名取子
    static testTreeNodeBasics()
    {
        global.println("===== testTreeNodeBasics =====")
        TreeNode<string> a = TreeNode<string>( "a", "1" )
        global.println("name = " + a.name + ", value = " + a.value)
        global.println("isLeaf = " + a.isLeaf.toString() + ", isRoot = " + a.isRoot.toString())
        TreeNode<string> c1 = a.addChild( "x", "2" )
        TreeNode<string> c2 = a.addChild( "y", "3" )
        TreeNode<string> c3 = a.addChild( "x", "4" )                  # 重名孩子
        global.println("childCount = " + a.childCount.toString())               # 3
        global.println("isLeaf = " + a.isLeaf.toString())                      # false
        global.println("hasChildren = " + a.hasChildren.toString())             # true
        global.println("firstChild = " + a.firstChild.name)                    # x
        global.println("lastChild = " + a.lastChild.name + "=" + a.lastChild.value)   # x=4（末个）
        global.println("childAt(1) = " + a.childAt(1).name)                    # y
        global.println("childAt(9) is null = " + (a.childAt(9) == null).toString())
        global.println("childByName(y) = " + a.childByName("y").value)         # 3
        global.println("childByName(x).value = " + a.childByName("x").value)   # 2（取第一个）
        global.println("childByName(z) is null = " + (a.childByName("z") == null).toString())
        global.println("c1.parent == a = " + (c1.parent == a).toString())
        global.println("c1.nextSibling = " + c1.nextSibling.name)              # y
        global.println("c2.prevSibling = " + c2.prevSibling.name + ", nextSibling = " + c2.nextSibling.name)
        global.println("c3.nextSibling is null = " + (c3.nextSibling == null).toString())
        global.println("c1.isRoot = " + c1.isRoot.toString() + ", c1.isLeaf = " + c1.isLeaf.toString())
        Array<TreeNode<string>> kids = a.children()
        global.println("children.length = " + kids.length.toString())          # 3
        for i = 0, i < kids.length, i++
        {
            global.println("children[" + i.toString() + "] = " + kids._getItem_(i).toString())
        }
        a.name = "aa"
        c1.value = "22"
        global.println("after set: " + a.name + "," + c1.name + "=" + c1.value)
    }

    # N 叉树构建与统计：addRoot/addChild/attach/count/height/depth/length/rootName
    static testTreeBuildAndStats()
    {
        global.println("===== testTreeBuildAndStats =====")
        Tree<int> t = new()
        global.println("isEmpty = " + t.isEmpty.toString() + ", length = " + t.length.toString())
        global.println("empty height = " + t.height.toString())                # 0
        global.println("rootName is null = " + (t.rootName == null).toString())
        TreeNode<int> r = t.addRoot( "r", 1 )
        global.println("isEmpty = " + t.isEmpty.toString())
        TreeNode<int> a = r.addChild( "a", 2 )
        TreeNode<int> x = a.addChild( "x", 4 )
        TreeNode<int> y = a.addChild( "y", 5 )
        TreeNode<int> b = r.addChild( "b", 3 )
        global.println("length = " + t.length.toString())                      # 5
        global.println("height = " + t.height.toString())                      # 3
        global.println("count(a) = " + a.count().toString())                   # 3
        global.println("count(x) = " + x.count().toString())                   # 1
        global.println("depth(r) = " + r.depth().toString())                   # 1
        global.println("depth(a) = " + a.depth().toString())                   # 2
        global.println("depth(x) = " + x.depth().toString())                   # 3
        global.println("rootName = " + t.rootName)
        global.println("root == r = " + (t.root == r).toString())
        # attach 已有节点挂为末子（自动脱离原父）
        TreeNode<int> solo = TreeNode<int>( "solo", 9 )
        global.println("attach solo = " + b.attach( solo ).toString())
        global.println("after attach length = " + t.length.toString())         # 6
        global.println("b.childCount = " + b.childCount.toString())            # 1
        # 以已有节点为根的新树（节点先脱离原父）
        Tree<int> t2 = Tree<int>( y )
        global.println("t2.length = " + t2.length.toString())                  # 1
        global.println("after move, t.length = " + t.length.toString())        # 5
        global.println("t.toString = " + t.toString())
    }

    # N 叉树查找：find(值)/findByName(名)/findAll(全部同值)/contains(节点身份)/containsValue
    static testTreeSearch()
    {
        global.println("===== testTreeSearch =====")
        Tree<string> t = Tree<string>( "r", "1" )
        TreeNode<string> a = t.root.addChild( "a", "2" )
        TreeNode<string> x = a.addChild( "x", "dup" )
        TreeNode<string> y = a.addChild( "y", "dup" )
        TreeNode<string> b = t.root.addChild( "b", "dup" )
        TreeNode<string> c = t.root.addChild( "c", "6" )
        global.println("find(dup).name = " + t.find("dup").name)               # x（先序首个）
        global.println("find(6).name = " + t.find("6").name)                   # c
        global.println("find(99) is null = " + (t.find("99") == null).toString())
        global.println("findByName(y).value = " + t.findByName("y").value)     # dup
        global.println("findByName(nosuch) is null = " + (t.findByName("nosuch") == null).toString())
        Array<TreeNode<string>> all = t.findAll( "dup" )
        global.println("findAll(dup).length = " + all.length.toString())       # 3
        for i = 0, i < all.length, i++
        {
            global.println("findAll(dup)[" + i.toString() + "] = " + all._getItem_(i).name)
        }
        global.println("findAll(1).length = " + t.findAll("1").length.toString())   # 1
        global.println("contains(x) = " + t.contains(x).toString())
        global.println("contains(c) = " + t.contains(c).toString())
        global.println("contains(null) = " + t.contains(null).toString())
        global.println("containsValue(dup) = " + t.containsValue("dup").toString())
        global.println("containsValue(99) = " + t.containsValue("99").toString())
        # 节点级同款 API
        global.println("a.find(dup).name = " + a.find("dup").name)             # x
        global.println("a.findAll(dup).length = " + a.findAll("dup").length.toString())   # 2
        global.println("a.contains(b) = " + a.contains(b).toString())          # false（兄弟不包含）
    }

    # 文档场景复用（Xml/Yaml/Json/Toml/Config-ini 的层级定位基础）：select 路径 + path 回查
    static testDocumentSelect()
    {
        global.println("===== testDocumentSelect =====")
        Tree<string> cfg = Tree<string>( "config" )
        TreeNode<string> server = cfg.root.addChild( "server" )
        TreeNode<string> host = server.addChild( "host", "127.0.0.1" )
        server.addChild( "port", "8080" )
        TreeNode<string> database = cfg.root.addChild( "database" )
        TreeNode<string> item0 = database.addChild( "item" )                   # Json 数组项 / ini 节
        item0.addChild( "name", "mysql" )
        item0.addChild( "port", "3306" )
        TreeNode<string> item1 = database.addChild( "item" )
        item1.addChild( "name", "pgsql" )
        item1.addChild( "port", "5432" )
        global.println("cfg.length = " + cfg.length.toString())                # 11
        global.println("select(server/host).value = " + cfg.select("server/host").value)
        global.println("select(database/item/name).value = " + cfg.select("database/item/name").value)   # mysql（首个 item）
        global.println("select(database/1/name).value = " + cfg.select("database/1/name").value)         # pgsql（下标定位）
        global.println("select(database/1/port).value = " + cfg.select("database/1/port").value)         # 5432
        global.println("select(\"\") == root = " + (cfg.select("") == cfg.root).toString())               # 空路径返回自身
        global.println("select(nosuch) is null = " + (cfg.select("nosuch") == null).toString())
        global.println("select(server/nosuch) is null = " + (cfg.select("server/nosuch") == null).toString())
        global.println("host.path = " + host.path)                             # /config/server/host
        global.println("item1.path = " + item1.path)                           # /config/database/item
        global.println("findByName(port).value = " + cfg.findByName("port").value)   # 8080（先序首个）
        global.println("find(5432).path = " + cfg.find("5432").path)           # /config/database/item/port
        # ini 风格：节名.键 名定位
        TreeNode<string> sec = cfg.root.addChild( "log" )
        sec.addChild( "level", "info" )
        global.println("select(log/level).value = " + cfg.select("log/level").value)
    }

    # N 叉树遍历：先序/后序/层序（节点版与取值版）+ 预分配填充版
    static testTreeTraverse()
    {
        global.println("===== testTreeTraverse =====")
        Tree<int> t = Tree<int>( "r", 1 )
        TreeNode<int> a = t.root.addChild( "a", 2 )
        a.addChild( "x", 4 )
        a.addChild( "y", 5 )
        t.root.addChild( "b", 3 )
        global.println("preorder values = " + joinInt( t.preorderValues() ))    # 1,2,4,5,3
        global.println("postorder values = " + joinInt( t.postorderValues() ))  # 4,5,2,3,1
        global.println("levelOrder values = " + joinInt( t.levelOrderValues() ))# 1,2,3,4,5
        global.println("preorder names = " + joinNodeName( t.preorder() ))      # r,a,x,y,b
        global.println("postorder names = " + joinNodeName( t.postorder() ))    # x,y,a,b,r
        global.println("levelOrder names = " + joinNodeName( t.levelOrder() ))  # r,a,b,x,y
        global.println("nodes(0).length = " + t.nodes(0).length.toString())    # 5
        # 预分配填充版（性能路径：调用方自备数组）
        Array<TreeNode<int>> buf = Array<TreeNode<int>>( 5 )
        int n = t.root.nodesToArray( 1, buf )
        global.println("nodesToArray(post) n = " + n.toString() + ", first = " + buf._getItem_(0).name)
        Array<int> vbuf = Array<int>( 5 )
        int m = t.root.valuesToArray( 2, vbuf )
        global.println("valuesToArray(level) m = " + m.toString() + ", first = " + vbuf._getItem_(0).toString())
        # 节点级遍历（子树）
        global.println("a.preorderValues = " + joinInt( a.preorderValues() ))   # 2,4,5
    }

    # N 叉树 LCA 与路径：lca/pathToRoot/path
    static testTreeLcaAndPath()
    {
        global.println("===== testTreeLcaAndPath =====")
        Tree<int> t = Tree<int>( "r", 1 )
        TreeNode<int> a = t.root.addChild( "a", 2 )
        TreeNode<int> x = a.addChild( "x", 4 )
        TreeNode<int> y = a.addChild( "y", 5 )
        TreeNode<int> b = t.root.addChild( "b", 3 )
        TreeNode<int> z = b.addChild( "z", 6 )
        global.println("lca(x,y) = " + t.lca( x, y ).name)                     # a
        global.println("lca(x,b) = " + t.lca( x, b ).name)                     # r
        global.println("lca(x,z) = " + t.lca( x, z ).name)                     # r
        global.println("lca(x,a) = " + t.lca( x, a ).name)                     # a（祖先后代）
        global.println("lca(r,z) = " + t.lca( t.root, z ).name)                # r
        Array<TreeNode<int>> p = x.pathToRoot()
        global.println("x.pathToRoot length = " + p.length.toString())         # 3
        global.println("x.pathToRoot = " + joinNodeName( p ))                  # x,a,r
        global.println("z.pathToRoot = " + joinNodeName( z.pathToRoot() ))     # z,b,r
        global.println("r.pathToRoot = " + joinNodeName( t.root.pathToRoot() ))# r
        global.println("x.path = " + x.path)                                   # /r/a/x
        global.println("z.path = " + z.path)                                   # /r/b/z
        # 节点级 lca
        global.println("x.lca(y) = " + x.lca( y ).name)                        # a
    }

    # N 叉树 detach/attach 指针手术：脱离原父/自挂拒绝/挂子孙防环/身份包含变化
    static testTreeDetach()
    {
        global.println("===== testTreeDetach =====")
        Tree<int> t = Tree<int>( "r", 1 )
        TreeNode<int> a = t.root.addChild( "a", 2 )
        TreeNode<int> x = a.addChild( "x", 4 )
        TreeNode<int> y = a.addChild( "y", 5 )
        TreeNode<int> b = t.root.addChild( "b", 3 )
        global.println("self attach r->r = " + t.root.attach( t.root ).toString())      # false
        global.println("attach ancestor r under x = " + x.attach( t.root ).toString())  # false 防环（祖先不可挂到后代下）
        global.println("before detach length = " + t.length.toString())        # 5
        global.println("y.detach = " + y.detach().toString())
        global.println("after detach length = " + t.length.toString())         # 4
        global.println("a.childCount = " + a.childCount.toString())            # 1
        global.println("y.isRoot = " + y.isRoot.toString() + ", y.count = " + y.count().toString())
        global.println("contains(y) = " + t.contains(y).toString())            # false
        global.println("b.attach(y) = " + b.attach( y ).toString())
        global.println("after reattach length = " + t.length.toString())       # 5
        global.println("b.childCount = " + b.childCount.toString())            # 1
        global.println("contains(y) = " + t.contains(y).toString())            # true
        global.println("y.parent == b = " + (y.parent == b).toString())
        global.println("root.detach = " + t.root.detach().toString())          # false（根不可 detach）
        # detach 中间子树：a 连同 x 整体脱离
        global.println("a.detach = " + a.detach().toString())
        global.println("after a detach, t.length = " + t.length.toString())    # 4（r,b,y）
        global.println("a.count = " + a.count().toString())                    # 2（a,x）
        global.println("t.contains(x) = " + t.contains(x).toString())          # false
        global.println("r.attach(a) = " + t.root.attach( a ).toString())       # 挂回
        global.println("restored length = " + t.length.toString())             # 5
        global.println("t.toString = " + t.toString())
    }

    # N 叉树迭代器：foreach 先序 + 手动 moveNext/current/index + 耗尽重启 + clear
    static testTreeIterator()
    {
        global.println("===== testTreeIterator =====")
        Tree<int> t = Tree<int>( "r", 1 )
        TreeNode<int> a = t.root.addChild( "a", 2 )
        a.addChild( "x", 4 )
        a.addChild( "y", 5 )
        t.root.addChild( "b", 3 )
        int count = 0
        for item in t
        {
            global.println("foreach item = " + item.toString())
            count++
        }
        global.println("iterated count = " + count.toString())                 # 5
        for item in t
        {
            global.println("foreach again item = " + item.toString())          # 重启迭代
        }
        t.reset()
        while t.moveNext()
        {
            global.println("manual current = " + t.current.toString() + ", index = " + t.index.toString())
        }
        global.println("after exhaust, index = " + t.index.toString())         # -1
        global.println("after exhaust, current is null = " + (t.current == null).toString())
        t.clear()
        global.println("after clear isEmpty = " + t.isEmpty.toString())        # true
        global.println("after clear length = " + t.length.toString())          # 0
        for item in t
        {
            global.println("should not reach here")
        }
        global.println("empty foreach done")
    }

    # 二叉树构建：addRoot/linkLeft/linkRight/addLeft/addRight/left-right setter/count/height/depth
    static testBinaryBuild()
    {
        global.println("===== testBinaryBuild =====")
        BinaryTree<int> bt = new()
        global.println("isEmpty = " + bt.isEmpty.toString() + ", height = " + bt.height.toString())
        BinaryNode<int> r = bt.addRoot( 1 )
        BinaryNode<int> n2 = r.addLeft( 2 )
        BinaryNode<int> n3 = r.addRight( 3 )
        BinaryNode<int> n4 = n2.addLeft( 4 )
        BinaryNode<int> n5 = n2.addRight( 5 )
        BinaryNode<int> n6 = n3.addRight( 6 )
        global.println("length = " + bt.length.toString())                     # 6
        global.println("height = " + bt.height.toString())                     # 3
        global.println("count(n2) = " + n2.count().toString())                 # 3
        global.println("depth(r) = " + r.depth().toString())                   # 1
        global.println("depth(n4) = " + n4.depth().toString())                 # 3
        global.println("n4.isLeaf = " + n4.isLeaf.toString() + ", n2.isLeaf = " + n2.isLeaf.toString())
        global.println("n3.hasLeft = " + n3.hasLeft.toString() + ", n3.hasRight = " + n3.hasRight.toString())
        global.println("r.left.value = " + r.left.value + ", r.right.value = " + r.right.value)
        global.println("n4.parent == n2 = " + (n4.parent == n2).toString())
        # linkLeft/linkRight 接已有节点（自动脱离原父，旧占用者脱离）
        BinaryNode<int> n7 = BinaryNode<int>( 7 )
        global.println("linkLeft(n3,n7) = " + bt.linkLeft( n3, n7 ).toString())
        global.println("n3.left.value = " + n3.left.value)
        global.println("length = " + bt.length.toString())                     # 7
        # setter 版本：置 null 摘除旧孩；置节点挂接
        n3.right = null
        global.println("after n3.right = null, n3.hasRight = " + n3.hasRight.toString())
        global.println("length = " + bt.length.toString())                     # 6
        n6 = BinaryNode<int>( 6 )
        n3.right = n6
        global.println("after set n3.right = 6, length = " + bt.length.toString())   # 7
        # 自挂拒绝
        global.println("r.linkLeft(r) = " + r.linkLeft( r ).toString())        # false
        # 以已有节点为根的新树（脱离原父）
        BinaryTree<int> sub = BinaryTree<int>( n4 )
        global.println("sub.length = " + sub.length.toString())                # 1
        global.println("after move bt.length = " + bt.length.toString())       # 6
    }

    # 二叉树遍历与查找：四序（先/中/后/层）+ find/findAll/contains/lca/pathToNode/pathFromRoot
    static testBinaryTraverseAndSearch()
    {
        global.println("===== testBinaryTraverseAndSearch =====")
        BinaryTree<int> bt = BinaryTree<int>( 1 )
        BinaryNode<int> n2 = bt.root.addLeft( 2 )
        BinaryNode<int> n3 = bt.root.addRight( 3 )
        BinaryNode<int> n4 = n2.addLeft( 4 )
        BinaryNode<int> n5 = n2.addRight( 5 )
        n3.addRight( 6 )
        global.println("preorder = " + joinInt( bt.preorderValues() ))         # 1,2,4,5,3,6
        global.println("inorder = " + joinInt( bt.inorderValues() ))           # 4,2,5,1,3,6
        global.println("postorder = " + joinInt( bt.postorderValues() ))       # 4,5,2,6,3,1
        global.println("levelOrder = " + joinInt( bt.levelOrderValues() ))     # 1,2,3,4,5,6
        global.println("preorder nodes = " + joinBinNodeValue( bt.preorder() ))
        global.println("inorder nodes = " + joinBinNodeValue( bt.inorder() ))
        global.println("nodes(3).length = " + bt.nodes(3).length.toString())   # 6
        global.println("find(5).value = " + bt.find(5).value)
        global.println("find(99) is null = " + (bt.find(99) == null).toString())
        global.println("contains(6) = " + bt.contains(6).toString())
        global.println("contains(99) = " + bt.contains(99).toString())
        # 重复值 findAll
        BinaryTree<int> dup = BinaryTree<int>( 1 )
        dup.root.addLeft( 2 )
        dup.root.addRight( 2 )
        global.println("dup.findAll(2).length = " + dup.findAll(2).length.toString())   # 2
        # LCA
        global.println("lca(n4,n5).value = " + bt.lca( n4, n5 ).value)         # 2
        global.println("lca(n4,n3).value = " + bt.lca( n4, n3 ).value)         # 1
        global.println("lca(n4,root).value = " + bt.lca( n4, bt.root ).value)  # 1
        # 路径
        global.println("pathToNode(n4) = " + joinInt( bt.pathToNode( n4 ) ))   # 1,2,4
        global.println("pathFromRoot(n5) = " + joinInt( n5.pathFromRoot() ))   # 1,2,5
        global.println("pathToNode(root) = " + joinInt( bt.pathToNode( bt.root ) ))   # 1
        # 节点级 API
        global.println("n2.inorderValues = " + joinInt( n2.inorderValues() ))  # 4,2,5
        global.println("n2.lca(n3).value = " + n2.lca( n3 ).value)             # 1
    }

    # 二叉树算法：invert 镜像/isSymmetric 对称/isBalanced 平衡/isBst 判定/containsSubtree 同构子树/unlink
    static testBinaryAlgorithms()
    {
        global.println("===== testBinaryAlgorithms =====")
        BinaryTree<int> bt = BinaryTree<int>( 1 )
        BinaryNode<int> n2 = bt.root.addLeft( 2 )
        BinaryNode<int> n3 = bt.root.addRight( 3 )
        n2.addLeft( 4 )
        n2.addRight( 5 )
        n3.addRight( 6 )
        # invert
        global.println("before invert preorder = " + joinInt( bt.preorderValues() ))   # 1,2,4,5,3,6
        bt.invert()
        global.println("after invert preorder = " + joinInt( bt.preorderValues() ))    # 1,3,6,2,5,4
        global.println("after invert inorder = " + joinInt( bt.inorderValues() ))      # 6,3,1,5,2,4
        bt.invert()
        global.println("restored preorder = " + joinInt( bt.preorderValues() ))        # 1,2,4,5,3,6
        # isSymmetric
        global.println("bt.isSymmetric = " + bt.isSymmetric().toString())      # false
        BinaryTree<int> sym = BinaryTree<int>( 1 )
        BinaryNode<int> sl = sym.root.addLeft( 2 )
        sl.addLeft( 3 )
        sl.addRight( 4 )
        BinaryNode<int> sr = sym.root.addRight( 2 )
        sr.addLeft( 4 )
        sr.addRight( 3 )
        global.println("sym.isSymmetric = " + sym.isSymmetric().toString())    # true
        BinaryTree<int> empty = new()
        global.println("empty isSymmetric = " + empty.isSymmetric().toString())   # true
        # isBalanced
        global.println("bt.isBalanced = " + bt.isBalanced().toString())        # true
        BinaryTree<int> chain = BinaryTree<int>( 1 )
        chain.root.addRight( 2 ).addRight( 3 )
        global.println("chain.isBalanced = " + chain.isBalanced().toString())  # false
        global.println("chain.height = " + chain.height.toString())            # 3
        # isBst
        global.println("bt.isBst = " + bt.isBst().toString())                  # false
        # containsSubtree：独立构造同构子树
        BinaryNode<int> pat = BinaryNode<int>( 2 )
        pat.addLeft( 4 )
        pat.addRight( 5 )
        global.println("containsSubtree(2;4,5) = " + bt.containsSubtree( pat ).toString())   # true
        BinaryNode<int> pat2 = BinaryNode<int>( 2 )
        pat2.addLeft( 5 )
        pat2.addRight( 4 )
        global.println("containsSubtree(2;5,4) = " + bt.containsSubtree( pat2 ).toString())  # false（值反）
        BinaryNode<int> pat3 = BinaryNode<int>( 9 )
        global.println("containsSubtree(9) = " + bt.containsSubtree( pat3 ).toString())      # false
        # unlink：n5 脱离父后 n2 右槽置空
        BinaryNode<int> n5 = n2.right
        global.println("n5.unlink = " + n5.unlink().toString())
        global.println("n2.hasRight = " + n2.hasRight.toString())
        global.println("after unlink length = " + bt.length.toString())        # 5
        global.println("n5.isRoot = " + n5.isRoot.toString())
        global.println("root.unlink = " + bt.root.unlink().toString())         # false（根不可 unlink）
    }

    # 二叉树迭代器（foreach 按中序）+ 手动游标 + clear
    static testBinaryIteratorAndClear()
    {
        global.println("===== testBinaryIteratorAndClear =====")
        BinaryTree<int> bt = BinaryTree<int>( 1 )
        BinaryNode<int> n2 = bt.root.addLeft( 2 )
        BinaryNode<int> n3 = bt.root.addRight( 3 )
        n2.addLeft( 4 )
        n2.addRight( 5 )
        n3.addRight( 6 )
        int count = 0
        for item in bt
        {
            global.println("foreach item = " + item.toString())                # 4,2,5,1,3,6（中序）
            count++
        }
        global.println("iterated count = " + count.toString())
        for item in bt
        {
            global.println("foreach again item = " + item.toString())
        }
        bt.reset()
        while bt.moveNext()
        {
            global.println("manual current = " + bt.current.toString() + ", index = " + bt.index.toString())
        }
        global.println("after exhaust, index = " + bt.index.toString())        # -1
        global.println("after exhaust, current is null = " + (bt.current == null).toString())
        bt.clear()
        global.println("after clear isEmpty = " + bt.isEmpty.toString())
        global.println("after clear length = " + bt.length.toString())
        global.println("after clear height = " + bt.height.toString())
        global.println("after clear toString = " + bt.toString())              # BinaryTree()
        for item in bt
        {
            global.println("should not reach here")
        }
        global.println("empty foreach done")
    }

    # 二叉搜索树：insert/insertRange/findNode/contains/remove 三 case/min/max/isBst/重复值
    static testBstInt()
    {
        global.println("===== testBstInt =====")
        BinarySearchTree<int> bst = new()
        global.println("empty isBst = " + bst.isBst().toString())
        global.println("empty min is null = " + (bst.min() == null).toString())
        bst.insert( 50 )
        bst.insert( 30 )
        bst.insert( 70 )
        bst.insert( 20 )
        bst.insert( 40 )
        bst.insert( 60 )
        bst.insert( 80 )
        bst.insert( 35 )
        bst.insert( 45 )
        global.println("length = " + bst.length.toString())                    # 9
        global.println("height = " + bst.height.toString())                    # 4
        global.println("isBst = " + bst.isBst().toString())                    # true
        global.println("isBalanced = " + bst.isBalanced().toString())          # true
        global.println("inorder = " + joinInt( bst.inorderValues() ))          # 20..80 有序
        global.println("preorder = " + joinInt( bst.preorderValues() ))        # 50,30,20,40,35,45,70,60,80
        global.println("min = " + bst.min().toString() + ", max = " + bst.max().toString())
        global.println("minNode.value = " + bst.minNode().value + ", maxNode.value = " + bst.maxNode().value)
        global.println("findNode(40).value = " + bst.findNode(40).value)
        global.println("findNode(35).depth = " + bst.findNode(35).depth().toString())   # 4
        global.println("findNode(99) is null = " + (bst.findNode(99) == null).toString())
        global.println("contains(80) = " + bst.contains(80).toString())
        global.println("contains(99) = " + bst.contains(99).toString())
        # 重复值：返回已存在节点，不重复插入
        global.println("dup insert(50).value = " + bst.insert(50).value)
        global.println("after dup length = " + bst.length.toString())          # 仍 9
        # remove 双孩：30 有 20/40，中序后继 35
        global.println("remove(30) = " + bst.remove(30).toString())
        global.println("after remove(30) inorder = " + joinInt( bst.inorderValues() ))  # 20,35,40,45,50,60,70,80
        global.println("after remove(30) length = " + bst.length.toString())   # 8
        global.println("still isBst = " + bst.isBst().toString())
        # remove 叶：20
        global.println("remove(20) = " + bst.remove(20).toString())
        # remove 双孩：70 有 60/80
        global.println("remove(70) = " + bst.remove(70).toString())
        global.println("final inorder = " + joinInt( bst.inorderValues() ))    # 35,40,45,50,60,80
        global.println("final length = " + bst.length.toString())              # 6
        global.println("remove(100) = " + bst.remove(100).toString())          # false
        # remove 根
        BinarySearchTree<int> root = new()
        root.insert( 5 )
        global.println("remove root = " + root.remove(5).toString())
        global.println("after remove root isEmpty = " + root.isEmpty.toString())
        # insertRange
        BinarySearchTree<int> rng = new()
        Array<int> vals = Array<int>( 5 )
        vals._setItem_( 0, 3 )
        vals._setItem_( 1, 1 )
        vals._setItem_( 2, 5 )
        vals._setItem_( 3, 2 )
        vals._setItem_( 4, 4 )
        rng.insertRange( vals )
        global.println("after insertRange length = " + rng.length.toString())  # 5
        global.println("after insertRange inorder = " + joinInt( rng.inorderValues() ))   # 1,2,3,4,5
        global.println("rng.toString = " + rng.toString())
    }

    # 字符串 BST（字典序比较）：Xml/Yaml/Json 键名排序场景
    static testBstString()
    {
        global.println("===== testBstString =====")
        BinarySearchTree<string> bst = new()
        bst.insert( "banana" )
        bst.insert( "apple" )
        bst.insert( "cherry" )
        bst.insert( "avocado" )
        global.println("length = " + bst.length.toString())                    # 4
        global.println("inorder = " + joinStr( bst.inorderValues() ))          # apple,avocado,banana,cherry
        global.println("min = " + bst.min() + ", max = " + bst.max())
        global.println("contains(apple) = " + bst.contains("apple").toString())
        global.println("contains(durian) = " + bst.contains("durian").toString())
        global.println("remove(banana) = " + bst.remove("banana").toString())
        global.println("after remove inorder = " + joinStr( bst.inorderValues() ))   # apple,avocado,cherry
        global.println("still isBst = " + bst.isBst().toString())
    }

    # 辅助：int 数组拼接
    static string joinInt( Array<int> arr )
    {
        if arr == null || arr.length == 0
        {
            ret ""
        }
        string s = arr._getItem_( 0 ).toString()
        for i = 1, i < arr.length, i++
        {
            s += "," + arr._getItem_( i ).toString()
        }
        ret s
    }

    # 辅助：string 数组拼接
    static string joinStr( Array<string> arr )
    {
        if arr == null || arr.length == 0
        {
            ret ""
        }
        string s = arr._getItem_( 0 )
        for i = 1, i < arr.length, i++
        {
            s += "," + arr._getItem_( i )
        }
        ret s
    }

    # 辅助：TreeNode 名拼接
    static string joinNodeName( Array<TreeNode<int>> arr )
    {
        if arr == null || arr.length == 0
        {
            ret ""
        }
        string s = arr._getItem_( 0 ).name
        for i = 1, i < arr.length, i++
        {
            s += "," + arr._getItem_( i ).name
        }
        ret s
    }

    # 辅助：BinaryNode 值拼接
    static string joinBinNodeValue( Array<BinaryNode<int>> arr )
    {
        if arr == null || arr.length == 0
        {
            ret ""
        }
        string s = arr._getItem_( 0 ).value.toString()
        for i = 1, i < arr.length, i++
        {
            s += "," + arr._getItem_( i ).value.toString()
        }
        ret s
    }

    static fun()
    {
        global.println("===== TreeTest =====")
        testTreeNodeBasics()
        testTreeBuildAndStats()
        testTreeSearch()
        testDocumentSelect()
        testTreeTraverse()
        testTreeLcaAndPath()
        testTreeDetach()
        testTreeIterator()
        testBinaryBuild()
        testBinaryTraverseAndSearch()
        testBinaryAlgorithms()
        testBinaryIteratorAndClear()
        testBstInt()
        testBstString()
    }
}
