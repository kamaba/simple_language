# S 的运算符
当你定义一个类时，你定义了一个数据类型的蓝图。这实际上并没有定义任何的数据，但它定义了类的名称意味着什么，也就是说，类的对象由什么组成及在这个对象上可执行什么操作。对象是类的实例。构成类的方法和变量称为类的成员。

—————————————————————————————————————————————————————————
符号使用  == 一般表示 对象相等， 如果 int a = 2; int b = 2; 使用 int/float/double/string等常量类型，使用 a == b 时，则会表示 === 相当于 值相等
 如果是普通对象 == 则表示对象相等 Class1 a = Class1(); Class1 b = Class1();  a!= b 因为对象不相同 但如果使用 a === b 则相同，相当于a.ToValue() == b.ToValue(); 
1. 去掉 --i --的做法   主要原因是，为了简精化语言读法 而i++ 这样的做法，不能在传参中使用的限定，只允许在函数中使用  函数中 不允许使用+(正号)  如果使用-(负号) 如果有前置使用符，必须使用(-1)这样做法 即 x + (-1) 而不能使用 x + -1