# 对象与构造（Objects & Construction）

对象是在运行时由类实例化得到的实体。S 语言通过 `ClassName(args)` 或字面量 `{ ... }` 等方式创建对象，并使用特殊方法 `_init_` 作为构造函数。

创建对象的方式：

1. 声明但不实例化：`Class1 c2 = null;` 等价于声明变量，未赋值（默认 null）。
2. 直接实例化：`c1 = Class1(20);` 调用 `_init_` 构造函数。
3. 初始化字面量：`Class1 c4 = { m1 = 20 };` 相当于 `Class1 c4 = Class1(); c4.m1 = 20;`，用于匿名或内联成员初始化。
4. 组合方式：`Class2 c1 = Class2(1,2){ m1 = 1, m2 = 2 };` — 先构造再对成员赋值。
5. 通过new函数 Class2 c1 = new(1,2){ m1 = 1, m2 = 2 };

构造函数（`_init_`）注意点：
- `_init_` 不能声明返回类型，也不应显式 `return` 值；构造过程首先分配对象，再运行 `_init_`。
- 在 `_init_` 中应只访问或修改当前实例 (`this`) 的成员，避免直接引用其它对象的内部成员（例如 `this.ct.t = 20` 不被鼓励）。
- 若父类有构造参数，子类可通过 `Base(args)` 语法调用父类构造（视实现细节）。

继承与方法重写：
- 语言去掉了 `virtual` 与 `new` 关键字。若子类想替换父类方法，必须显式使用 `override`。
- 父类声明了 `abstract` 方法，子类必须 `override` 并实现该方法（或自身声明为 `abstract`）。

示例：

```s
class Point {
    Int32 x = 0;
    Int32 y = 0;
    _init_(Int32 a, Int32 b) {
        this.x = a;
        this.y = b;
    }
}

// 使用
var p = Point(10, 20);
var p2 = Point(0,0){ x = 5 }; // 先创建后赋值
```

