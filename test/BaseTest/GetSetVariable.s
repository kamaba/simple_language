GetSetClass
{
    public int Get( int ins )
    {
        # 相当于 a = GetSetClass()[10]
    }
    public void Set( int ins, float a )
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
        #gsc.@20 = 10.0f;    # 相当于 gsc.Set( 20, 10.0f );
    }
}