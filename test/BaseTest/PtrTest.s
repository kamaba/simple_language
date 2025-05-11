import Application.Core;

#namespace Application.MFC;

public class Ptr<T>
{
    T classT

    Byte pointter8
    UShort pointer16
    UInt32 pointer32
    UInt64 pointer64

    int pointerLength = 4

    UInt32 currentMovePoint;

    public Ptr<T> readPtr<T>( int offset = -1 )
    {
        if offset = -1 
        {
            offset = currentMovePoint;
        }

        int newpoint = pointer32 + pointerLength * offset

        length = T.length

        byte[] bytes = GetPtr( newpoint, length )

        

        ret null
    }

    #使用C里边的内置函数
    byte[] GetPtr( int point, int len )
    {
        ret null
    }
}


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

Node
{
    value = null
    Ptr<Node> prevNode
    Ptr<Node> nextNode
}
PtrTest
{
    static Ptr<Node> createNode()
    {
        Ptr<Node> p = Node()
    }
    static Fun()
    {
        Ptr<Node> front = null
        Ptr<Node> next = null
        for v in 1..100
        {
            Ptr<Node> p = Mem.malloc(Node())
            p.prevNode = front
            if front
            {
                front.nextNode = p
            }
            front = p
        }
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
