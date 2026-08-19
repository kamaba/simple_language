!if Windows

WindowApi
{
    HWNL
    {
        int ptr;
    }
    DWORD
    {
        
    }

    @WindowApi.LoadLabirary("Win32Api")
    Ptr<HWNL> LoadLabirary( string name, )
}
class A{
    int a = 20
    b = 30.0f
}
PtrTest
{
    static Ptr<Node> createNode()
    {
        Ptr<Node> p = Node()
    }
    static fun()
    {
        Ptr<Node> front = null
        Ptr<Node> next = null
        for v in 1..100
        {
            Ptr<Node> p = Mem.malloc( Node.type )
            p.prevNode = front      #这里
            if front
            {
                front.nextNode = p      
            }
            front = p
        }
        A a = new()
        Ptr<A> ptra = a.ptr

        A ap1 = ptra.value;

        aa = ap1.a      # 相当于c/c++里边的 ptra->a的值
    }

    Ptr p = WindowApi.LoadLabirary("user.dll")   #相当于 void*

    int a = p.readInt() #读取指针后边几个byte 然后转化成int32
    byte[] b = p.readBytes(128）  #读取后边128位
    Ptr p2 = p.readPtr();  #读取指针 0
    Ptr p3 = p.readPtr(144)  #读取指针+144位，如果是Ptr<int> 则是144x4

    Ptr<Node> pnode = p.readPtr<Node>(20)
    pnode.value = 200

    Ptr<Node> cp1 = Mem.malloc<Node>()

    GC.AddAutoHandle( cp1.ptr )

    Mem.free(cp1.ptr)
}

#内置 Mem 可以调用底动的  posix 内存接口 
