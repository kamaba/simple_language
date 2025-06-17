GetSetClass
{
    public int _getIndex_( int ins )
    {
        # 相当于 a = GetSetClass()[10]
    }
    public void _setIndex_( int ins, float a )
    {
        # 相当于 GetSetClass()[10] = 10.0f;
    }
    public float get indexModel()
    {
        ret 10.0f;
    }
    public void set indexModel( float a )
    {
        int b = a;
    }

    #! 使用内置的方式  是否支持该种方法，后边再定
    int bvalue = 100
    {
        get()
        {
            if this.bvalue < 3
            {
                ret 3
            }
            else
            {
                ret this.bvalue
            }
        }
        set( int v )
         {
            if v > 1000
            {
                this.bvalue = v
            }
            else
            {
                this.bvalue = 100
            }
        }
    }
    !#
}
GetClass2{
}
GetSetTest
{
    static Fun()
    {
        gsc = GetSetClass();
        int i = gsc.indexModel;
        gsc.indexModel = 20.0f;
        gsc.bvalue = 888
        int i2 = gsc.bvalue     # 如果有get方法 要优化调用 get[值] 的函数
        #gsc.@20 = 10.0f;    # 相当于 gsc.Set( 20, 10.0f );
    }
}

# set/get 函数，可以与某一个成员变量重名，这时候，如果调用，会优先调用该set/get的函数，而不调用成员变量