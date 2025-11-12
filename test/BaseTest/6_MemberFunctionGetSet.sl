
import CSharp.System


Level1 
{
    int _Level1_var1 = 123

    get Level1_var1(){ ret this._Level1_var1 }
    set Level1_var1( obj ){ this._Level1_var1 = obj }
}

Level2 extends Level1
{
    int _Level2_var1 = 10;                #如果第一个是_需要先进行私有化
    get Level2_var1(){ ret _Level2_var1._a } 
    set Level2_var1( _a ){ _Level2_var1._a = _a }   #可以与变量重名

    override set Level1_var1( obj )
    {
        this.Level2_var1 = obj
        base._Level1_var1 = obj
    }

    int b = 20
    #b(){ ret this.b } #如果有set 或者 是get标记，才可以和变量名相同 否则不可以重名
}

MFSetGet
{
    static fun()
    {
        var c = Level2(){}

        c.Level1_var1 = 100
        c.Level2_var1 = 200

        System.Console.WriteLine("_this_333331 " + c.Level1_var1 )
        System.Console.WriteLine("_this_333332 " + c.Level2_var1 )
    }
}