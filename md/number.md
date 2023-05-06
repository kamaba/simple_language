# S语言的数字
在S语言中，已定义了多种数字类型，并且多种类型可以理解为数字，现有类型有: byte,sbyte,char,short,ushort,int,uint,long,ulong.

—————————————————————————————————————————————————————————

#数字使用语法
- a1 = 10i;    
- int a2 = 10;
- a3 = 10ui;
- a4 = 20s;
- a5 = 20us;
- a6 = 100000000L;
- a7 = 10000000000uL;
- b1 = 0b0011_1100;


符号使用  == 一般表示 对象相等， 如果 int a = 2; int b = 2; 使用 int/float/double/string等常量类型，使用 a == b 时，则会表示 === 相当于 值相等
 如果是普通对象 == 则表示对象相等 Class1 a = Class1(); Class1 b = Class1();  a!= b 因为对象不相同 但如果使用 a === b 则相同，相当于a.ToValue() == b.ToValue(); 
