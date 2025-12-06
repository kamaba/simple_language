
Core.Ptr<T>
{    
    T classT = null

    Byte pointter8
    UShort pointer16
    UInt32 pointer32
    UInt64 pointer64

    int pointerLength = 4

    UInt32 currentMovePoint = 0;

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