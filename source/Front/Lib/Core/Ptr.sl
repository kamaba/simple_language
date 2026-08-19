
Ptr<T>
{    
    T classT = null
    UInt32 pointer32 = 0
    UInt32 currentMovePoint = 0;

    void _init_( T classT )
    {

    }
    void _init_( UInt32 pointer32 )
    {
        this.pointer32 = pointer32
    }

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