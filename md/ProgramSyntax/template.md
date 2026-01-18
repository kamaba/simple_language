# S语言的模板
SLang在工程中，如果不使用虚拟机，但不使用AOT时，可以使用模板功能，模板分为模板类与模板函数
—————————————————————————————————————————————————————————

## 模板类的定义
模板类是指，通过定义模板，在构建类时，可以时时的在运行时，构建多种形态的生成类，比如 Level<T>{ T t = new()} 那如果构建时使用 Level<int> levelInt = new() 则levelInt是一个Level<Int32>{ Int32 t = 0} 的类型,模板会进行传入，并且运行时，时时替换成生成的类内容 

```
in the file mycore1.sl
class_name1<T> 
{
    T t = nll
}

class_name<int> cn = new()

```
### 示例1
该示例中，class_name<int> 会创建一个新的生成类，是class_name1+Int32构建的,在传入T时，T会进行时时替换并且生成

```
in the file mycore1.sl
class_name1<T> 
{
    T t = null
}
class_name2<T> extends class_name1<T>
{
    T t2 = new()
}

class_name2<int> cn = new()

```
### 示例2
该示例中，class_name2<int> 会创建一个新的生成类，会使用模板类继承后，再进行每个的时时替换。
int t = null  int t2 = 0;

```
in the file mycore1.sl
class_name1<T>  #这里就是类的定义
{
    class_name4<T> class_name1_variable1 = new()
}
class_name2<T1,T2> extends<class_name3<T2> >
{

}
class_name3<T>
{
    T t = new()

    
    T fun(){
        T a = new()

        ret a
    }
}
class_name4<T>
{
    T t = new()
}

```
### 示例3
该示例中，复杂模板替换,要进行逐级替换，然后再进行类生成，替换时，模板在整个的函数里边也会进行替换。


### 模板函数
模板函数是指，通过在函数上定义模板，构建不同的实例函数的处理，在构建该函数时，可以时时的在运行时，构建多种形态的生成函数，比如 Level{ T getT<T>(){ T t = new(); ret t; } } 那如果构建时使用时，需要进行模板替换,   例:   Level level = new(); int a = level.GetT<int>()  这样就能调用模板函数。