# S 语言的类 
当你定义一个类时，你定义了一个数据类型的集合。这实际上并没有定义任何的数据，但它定义了类的名称意味着什么，也就是说，类的对象由什么组成及在这个对象上可执行什么操作。对象是类的实例。构成类的方法和变量称为类的成员。

—————————————————————————————————————————————————————————

## 类的定义

类定义用于声明对象的结构（字段）与行为（方法）。S 语言允许在顶层、`namespace` 内或其它类中嵌套定义类。

语法示例：

```s
# 简写形式
MyClass {
    # fields
    Int32 x = 0;
    String name = "";

    # S 语言的类（class）

    当你定义一个类时，你定义了一个数据类型的集合。类本身描述了对象的结构（成员变量/字段）、行为（成员函数/方法）、可见性、继承关系等。对象是类的实例。

    ---

    ## 一、类的定义与基本结构

    S 语言支持多种形式定义类：

    ```s
    # 简写形式
    MyClass {
        Int32 x = 0;           // 成员变量（字段）
        String name = "";

        // 构造函数
        _init_(Int32 a) { this.x = a; }

        // 实例方法
        Int32 getX() { ret this.x; }

        // 静态方法
        static Int32 Zero() { ret 0; }
    }

    // 显式 class 关键字
    class OtherClass { }
    ```

    ### 1.1 成员变量（字段）
    - 支持基本类型、对象、数组、泛型等
    - 可加访问修饰符（public/private/internal/projected）
    - 支持静态字段（static）

    ```s
    public Int32 id = 0;
    private String name = "";
    static Float32_3 zero = new(0.0f, 0.0f, 0.0f);
    ```

    ### 1.2 成员函数（方法）
    - 普通成员方法、静态方法（static）、虚方法（virtual）、接口方法（interface）
    - 支持重载、默认参数

    ```s
    public void scale(Float32_3 s) { ... }
    static Print(String msg, int v1 = 10) { ... }
    virtual int getback() { return 100; }
    interface int C2() { return Y; }
    ```

    ### 1.3 构造函数
    - 统一用 _init_ 命名，可重载

    ```s
    _init_() { ... }
    _init_(Int32 a) { this.x = a; }
    _init_(Float32[] arr) { ... }
    ```

    ### 1.4 静态成员
    - 用 static 修饰，属于类本身

    ```s
    public static Float32_3 one = new(1.0f, 1.0f, 1.0f);
    static Main() { ... }
    ```

    ### 1.5 访问修饰符
    - public：公开（默认）
    - private：仅类内可见
    - internal/projected：特殊作用域

    ### 1.6 继承与 final
    - 继承：`class Child extends Parent {}`
    - final：禁止被继承或重写

    ```s
    final Application.MyClass2 { }
    class MyChild extends MyClass { }
    ```

    ### 1.7 泛型与嵌套类
    - 泛型：`List<T> extends Object { ... }`
    - 支持类中嵌套类、匿名对象、数组等

    ```s
    class List<T> extends Object {
        T[] _items = new();
        add(T t) { ... }
    }
    ```

    ### 1.8 接口与虚方法
    - interface 关键字声明接口方法
    - virtual 支持虚方法重写

    ```s
    interface int C2();
    virtual int getback() { ... }
    ```

    ---

    ## 二、典型语法与用法示例

    ### 2.1 嵌套与命名空间

    ```s
    namespace Application {
        MyClass {
            MyChildClass { }
        }
    }

    var amc = Application.MyClass();
    var amcc = Application.MyClass.MyChildClass();
    ```

    ### 2.2 静态成员与静态方法

    ```s
    public static Float32_3 zero = new(0.0f, 0.0f, 0.0f);
    static Print(String msg) { ... }
    ```

    ### 2.3 构造与初始化

    ```s
    var a = MyClass();
    var b = MyClass(){ x = 10, name = "abc" };
    ```

    ### 2.4 继承、接口、多重继承

    ```s
    class C22 extends Application.CI2 {
        virtual int getback() { return 100; }
    }
    class C23 extends C22 {
        ...
    }
    Applicaction.C3 extends C22 interface Application.CI2, CI3 {
        ...
    }
    ```

    ### 2.5 成员访问与链式操作

    ```s
    obj.field = 10;
    obj.method();
    obj.child.field;
    obj.array[0].field;
    ```

    ### 2.6 数据成员、匿名对象、数组

    ```s
    data Profile {
        grade = 3;
        address = { city = "Shenzhen", zip = 518000 };
        tags = ["math", "final"];
    }
    ```

    ### 2.7 枚举与常量

    ```s
    enum DataKind { Base = 1, Advanced = 2 }
    const data ScoreRule { passLine = 60, excellentLine = 90 }
    ```

    ---

    ## 三、特殊说明

    - 支持 partial class、虚方法、接口、泛型、嵌套、匿名对象、静态/动态成员、数据成员、数组、枚举等
    - 支持多种初始化方式（new、字面量、构造函数）
    - 支持链式成员访问、静态成员直接访问、静态方法调用
    - 支持多重继承（接口）、final 限定、访问修饰符
    - 支持泛型集合、数组、对象数组、匿名对象数组
    - 支持静态/动态成员混用

    ---

    ## 四、综合示例

    ```s
    public class Float32_3 {
        public float[3] _value = new();
        public static Float32_3 zero = new(0.0f, 0.0f, 0.0f);
        public get Float32 x() { ret this._value[0]; }
        _init_() { ... }
        _init_(Float32 _x, Float32 _y, Float32 _z) { ... }
        void scale(Float32_3 _scale) { ... }
        static fun() { ... }
    }

    class List<T> extends Object {
        T[] _items = new();
        add(T t) { ... }
        remove(T t) { ... }
        clear() { ... }
    }

    // 继承、接口、final
    final class MyFinalClass { }
    class Child extends Parent { }
    class MultiImpl extends Base interface IA, IB { }
    ```

    ---

    ## 五、常见用法速查

    - 定义类：`class MyClass { ... }` 或 `MyClass { ... }`
    - 静态成员：`static` 修饰
    - 构造函数：`_init_`，可重载
    - 继承：`class Child extends Parent {}`
    - 接口：`interface` 关键字
    - final：`final class` 或 `final` 成员
    - 访问修饰符：`public`/`private`/`internal`/`projected`
    - 泛型：`class List<T> { ... }`
    - 嵌套类/命名空间：`namespace X { class Y { ... } }`
    - 数据成员/匿名对象/数组/枚举：均支持

    ---

    （如需更详细语法或特殊用法，请参考测试用例和源码实现）
        MyChildClass
        {

        }
    }
}
final Application.MyClass2   #不允许继承
{
    
}
MyClass3
{
    
}


```

file: test.sp
```python
ProjectEnter
{
    static _main_()
    {  
        amc = Application.MyClass();
        amcc = Application.MyClass.MyChildClass();
        amc2 = Application.MyClass2();
    }
    static _test_()
    {
    }
}
```