# S 的运算符
当你定义一个类时，你定义了一个数据类型的蓝图。这实际上并没有定义任何的数据，但它定义了类的名称意味着什么，也就是说，类的对象由什么组成及在这个对象上可执行什么操作。对象是类的实例。构成类的方法和变量称为类的成员。

—————————————————————————————————————————————————————————
符号使用  == 一般表示 对象相等， 如果 int a = 2; int b = 2; 使用 int/float/double/string等常量类型，使用 a == b 时，则会表示 === 相当于 值相等
 如果是普通对象 == 则表示对象相等 Class1 a = Class1(); Class1 b = Class1();  a!= b 因为对象不相同 但如果使用 a === b 则相同，相当于a.ToValue() == b.ToValue(); 


 关于string的格式化使用
 1. 形式1 string.format( "Name:{} Core:{} ", name, core )  strint.format( "Name:{1} Core:{0} ", core, name )
 2. 直接使用字符后，连接format 的非静态形式 "Name:{0},Core:{1}".format( core, name )
 3. 普通的字符形式使用"" 表示范围，里边表达" 则需要使用\"进行转义后，在收录进去  这种的对$ {}表达逻辑符号识别  示例:  "name=$name core={core+100}"  输出name=think core=106
 4. 还有一种，使用'' 单引表示范围 只有\'表示转义，其它的都正常收录              这种对$ {} 表达逻辑符号不识别 'name=\'$name\' tp="axx" core=$core ' 输出name='$name' tp="axx" core=$core
 5. 使用f""" 内容 """ 表示复杂的内容，这种的才会对 符号进行识别  如果是shell""" """则 直接识别sh脚本内容  json""" """ 直接识别json内容 xml""" """直接识别 xml内容 
