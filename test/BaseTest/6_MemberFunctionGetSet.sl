

C
{
    int _a = 10;
    get a(){ ret this._a } 
    set a( _a ){ this._a = _a }   #可以与变量重名

    int b = 20

    #b(){ ret this.b } #如果有set 或者 是get标记，才可以和变量名相同 否则不可以重名
}

MF
{
    static fun()
    {
        C c = C(){}
        c.a = 100
    }
}