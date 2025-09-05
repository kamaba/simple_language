ForTest
{   
    static forfun()
    {
        #!
        i = 20
        for i = 1
        {
            if i > 22
            {
                break
            }
            i = i+2
            CSharp.System.Debug.Write("for i= $i ")
        }
        for i = 123
        {
            if i >= 130
            {
                break
            }
            i++
            CSharp.System.Debug.Write("i= $i ")
        }
        !#
        
        #!
        for i = 0, i < 10
        {
            CSharp.System.Debug.Write("i= $i ")
            i++            
        }
        !#
        #!
        for i = 0, i <= 2, i+=2
        {            
            CSharp.System.Debug.Write("i= $i ");
            n = i * 10;
        }
        !#
        #!
        for i = 0, i < 30, i++
        {
            CSharp.System.Debug.Write("i= $i ");
            n = i * 10;
            #if n == 200{ break }

            if n % 2 == 0 {CSharp.System.Debug.Write("这是一个偶数 = $i ");continue }
        }
        !#
        #! 暂不支持 List,Map,Set,Queue,Link
        int i2 = 0;
        List list = List();        
        for it in list
        {

        }
        !#
        #!  暂不支持Array<int>
        Array<int> arr = [1,2,3];
        for v in arr{
            CSharp.System.Debug.Write(" v= $v ")
        }
        !# 
        #!  暂不支持 Array<object>
        Array b = [{a=1}, {a=2}, {a = 3} ];        
        for v in b{

        }  
        暂不支持 for in range
        for v in 1..10
        {
        }
        暂不支持 for in range
        for v in EItType
        {

        }        
        !#
    }    

    enum EItType
    {
        It1 = 1
        It2 = 2
    }
    static forenum()
    {
        for v in EItType
        {
            !print( v )
        }
    }
    interface IPay
    {
        pay( int a )
        check()
    }
    public class Pay interface IPay
    {
        _paycash = 0
        pay( int a ){
            this._paycash = a
        }
        check()
        {

        }
    }
    static forinterface()
    {
        IPay pay1 = Pay()
        pay1.check()
        pay1.pay(20)
    }
}