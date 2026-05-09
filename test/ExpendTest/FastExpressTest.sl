
Level1<T>
{
    val=> this._val                     # get object val(){ ret this._val; }

    set val( _val )=> this._val = _val  # set void val( _val ){ this._val = _val }

    T _val = null
}
FastExpressTest
{
    a => 10  # get a(){ ret 10 }
    static fun()
    {
        Level1<int> level1 = new()
        level1.val = 30
        Console.print( "level1: " + level1.val )
    }
}