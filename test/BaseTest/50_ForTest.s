import Std
import CSharp.SimpleLanguage
import CSharp.System


namespace Core
{    
    public interface IIterator
    {
        bool moveNext()
        get object current()
        void release()
    }
    public interface IIterable
    {
        IIterator iterator()
    }

    public class IterateVariable interface IIterator
    {
        _start = 0
        _index = 0
        _value = null
        IIterable _iterable = null
        _isDone = false;

        #!
        1. 在使用迭代器时，需要先new一个iterateVariable 对象
        2. 然后把IIterable放到本地的_iterable节点中
        3. 
        !#
        override bool moveNext()
        {
            if this._isDone
            {
                ret false
            }
            this._isDone = _iterable.moveNext()
            ret this._isDone
        }
        override get object current()
        {
            this._value = _iterable.current()
            ret this._value
        }
        override void release()
        {
            
        }
    }

    #!
    public iterface ITIterator<T>
    {
        bool moveNext()
        get T current()
        void release()
    }
    public interface ITIterable<T>
    {
        ITIterator<T> iterator()
    }
    public class TIterateVariable<T> interface ITIterable<T>
    {
        _start = 0
        _index = 0
        T _value = null
        IIterable _iterable = null
        _isDone = false;

        override bool moveNext()
        {
            if this._isDone
            {
                ret false
            }
            this._isDone = _iterable.moveNext()
            ret this._isDone
        }
        override T current()
        {
            this._value = _iterable.current() as T
            ret this._value
        }
        override void release()
        {
            
        }
    }
    !#
}
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
            System.Debug.Write("for i= $i ")
        }
        for i = 123
        {
            if i >= 130
            {
                break
            }
            i++
            System.Debug.Write("i= $i ")
        }
        !#
        
        #!
        for i = 0, i < 10
        {
            System.Debug.Write("i= $i ")
            i++            
        }
        !#
        #!
        for i = 0, i <= 2, i+=2
        {            
            System.Debug.Write("i= $i ");
            n = i * 10;
        }
        !#
        #!
        for i = 0, i < 30, i++
        {
            System.Debug.Write("i= $i ");
            n = i * 10;
            #if n == 200{ break }

            if n % 2 == 0 {System.Debug.Write("这是一个偶数 = $i ");continue }
        }
        !#

        #! 
        arr = [1,2,3];
        for v in arr
        {
            System.Debug.Write(" v= $v ")
        }
        !#

        #!
        for v in [1,2,3,4]
        {
            System.Debug.Write(" v= $v ")
        }
        !#
        
        #!range(1,4,1)
        for v in [1..4]   
        {
            System.Debug.Write(" v= $v ")
        }
        !#

        #!
        a1 = [[1,2,3],[4,5,6],[7,8,9]]
        for v in a1
        {
            #System.Console.WriteLine("这里是索引" + v.index )
            for i = 0, i < v.length, i++
            {
                System.Console.WriteLine("这里是值" + v.$i + "----" + v[i] )
            }
        }
        !#

        #! 暂不支持 List,Map,Set,Queue,Link
        int i2 = 0;
        List list = { 1, 2, 3};        
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

#for关键字的的规则
1. for的使用对于 一个目标的变量的循环遍历  

1.1 for i = 0, i < 10, i++ 是使用for的 条件遍历法， 一般是 第一位是 数字赋值， 第二位是条件遍历  第三位是 变量更新 
1.1.1 不写任何 即for{} 这种写法，可以在里边进行break  
1.1.2 只写for i = 0{} 即，把第一个变量进行赋值，在里这进行变更 
1.1.3 只写for i =0, i < 10 { }  进行条件限制 变更更新在内部
1.1.4 写全的 for i = 0, i < 10, i++ 即 数字赋值，条件遍历  变量更新

1.2 for x in content 是对某个可以iterate的进行遍历 ，如果是iterate进行遍历 
1.2.1 x是遍历的迭代变量 未来要支持 x.index 即当前遍历的索引  x.value 当前值的读取，如果x.value 即使值为空，也可以告是空值 
1.2.2 在遍历的时候， content 会把进行对x.index 进行赋值 
1.2.3 x 如果直接读取，也是可以访问的，即 相当于 x.value 的方法

1.3 for x in Enum 是对enum的遍历 ， 会把enum 里边的 staticMemberVariableArray[] 然后进行遍历 所以enum在构建的时候，会把当前的变量都存在这个数组里边

1.4 for x in Map<T1,T2> 一般会在x中，直接可以读取 x.key, x.value T1 x.key    T2 x.value