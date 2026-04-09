# SLang 的系统函数
系统函数，主要是用来 重载，内置一些方法，可以通过系统函数，改变编译内部的一些逻辑

—————————————————————————————————————————————————————————

## 重载类函数 init
说明: 在类中使用 _init_函数 可以重载函数， 通过不同参数，重载，比如 _init_( int a ){} _init_( int a, int b ){}  

## 重载符号 reloadsign
说明: 在类中，如果要重载符号比如 类的相关 类的相除 类的位移等 
1. 比如 Class1 _reloadsign_( Class1 c, Class1 c2, "+" ){} 可以重载+法

## 重载通过@获取 
说明: 可以重载该方法，快速获取 比如在一个自动写的数组类，可以通过重载快速获取下标的某个值

1. _getIndex_( int index ){} 获取该下载的返回值 ，也可以返回某个定义值 array_variable.@0 或者是使用 array_variable[0] 有同样的方法
2. _getKey_( string key ){} 类似于上边的方法，可以重载后，通过 variable.@"nb" 的方式获取 

## 类型变化 cast
说明： 可以通过该方法，对类进行转换，
1. 比如 var v = intvalue.cast<string>()  对某个int值，转为string, v 是一个string类型
2. Class1{} Class2 extends Class1{}  Class2 class2Value  Class1 c = Class2Value.cast<Class2>()


