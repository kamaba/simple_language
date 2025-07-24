import Std
import CSharp.System


Level1<T>
{
    T Level1_t = null
    _init_( T t )
    {
        this.Level1_t = t
    }
    void setLevel1( T t )
    {
        if t == null
        {
            this.Level1_t = new()
        }
        else
        {
            this.Level1_t = t
        }
    }
    get T Level1_t()
    {
        ret this.Level1_t
    }
}

GenClass2
{
    static fun()
    {
        Level1<int> GenClass2_fun_l1 = Level1<int>()

        GenClass2_fun_l1.setLevel1(20)

        System.Console.Write("Flaoat" + GenClass2_fun_l1.Level1_t )
    }
}
