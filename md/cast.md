# S 的类 
1. 因为所有类都继承Object类，所以所有类都可以使用Cast<T>()方法
2. 如果不override Cast<T>类的话，直接使用，则通过继承关系，进行Cast变化 
3. 像float,int,short,long,系统函数，大部分，有重载Cast， 所以 可以直接使用 例  20.3f.Cast<int>() 
4. 在函数的Cast中，可以通过   Cast( T t){} t.GetType() 再继承辨识，返回不同
5. 如果普通语句 int a = 20; float c = a; 则相当于 a.Cast<int>()的转换，则数字型可以直接使用Cast<NumberType>直接转换
6. 如果是 父类转子类，则使用Warning通知，说明，是父类转子类
7. 像普通的语句 NewStatement , AssignStatement ,Return, Tr等语句，内置Cast语句

相当于，如果有变量，和变量表达式时