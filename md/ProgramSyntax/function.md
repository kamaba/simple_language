# 函数与方法

函数是可调用的代码单元；方法是绑定在类上的函数（成员方法）。S 语言的函数语法受 C#/Dart 启发，支持：
- 显式返回类型或省略（视为动态/void）
- 参数默认值与可变参数（`params`）
- 模板（泛型）函数
- 方法修饰符：`public` / `private` / `projected` / `extern` / `static` / `final` / `override` / `abstract`
- 属性访问器：`get` / `set`
- 构造函数：使用特殊方法名 `_init_`

下面详细说明语言层面的约定与使用示例，便于开发者快速上手。

---

## 基本语法

顶级或成员函数的一般形式：

```s
// 带返回类型的函数
fun add(Int32 a, Int32 b) {
    ret a + b;
}

// 成员方法（在 class 内）
public Int32 sum(Int32 x, Int32 y) {
    ret x + y;
}
```

- `ret` 用作返回语句关键字（也可使用 `return` 的别名，视实现）。
- 若函数标注返回类型则编译器在类型检查阶段会校验返回值；若未显式返回类型，则采用 `dynamic`/`object` 或根据上下文推断。

---

## 参数（Parameters）

支持以下特性：
- 显式类型声明：`Int32 x`。
- 默认值：参数可以在声明时提供默认表达式，例如 `Int32 x = 10`。
- 可变参数（`params`）：支持把尾参数标注为 `params` 来接收任意数量的值（类似 C# 的 `params`）。

示例：

```s
fun greet(String name = "Guest") {
    Debug.Write("Hello " + name);
}

fun sum(params Int32 nums) {
    Int32 r = 0;
    for v in nums { r += v; }
    ret r;
}
```

规则与注意：
- 有默认值的参数通常必须在参数表的尾部（同多数语言的约定），调用时可省略；
- `params` 应当用于参数列表的最后一项；编译器会把传入的若干实参封装为数组或可迭代对象。

---

## 模板 / 泛型函数（Templates / Generics）

函数可以声明模板类型参数，允许在调用时以具体类型实例化。模板也可以带约束（`where` / `in` 风格的语法），编译器在解析阶段会检查约束。

```s
fun<T> identity(T x) { ret x; }

fun<T extends Number> sumAll(List<T> values) { /* ... */ }
```

实现细节：模板参数可在函数体内作为类型使用；如果函数被声明为模板函数，模板实例化发生在编译/元模型阶段。

---

## 修饰符与特殊关键字

- `static`：声明为类级别函数，不需要实例即可调用。
- `override`：用于子类方法，表示显式重写父类或接口中的方法；若父方法是 `abstract`，子类必须 `override` 并提供实现（否则子类必须声明为 `abstract`）。
- `abstract`：在类内声明抽象方法（无方法体）。编译器不会对抽象方法进行函数体解析。
- `final`：标记方法或字段为不可重写。
- 访问权限：`public` / `private` / `projected` / `extern` 等，用于控制可见性与链接方式。

示例：

```s
public abstract Int32 compareTo(Int32 other);
public override Int32 compareTo(Int32 other) { ret this.value - other; }
```

---

## 构造函数与 `_init_`

类的构造使用特殊方法名 `_init_` 定义，语言通过 `ClassName(args)` 语法来调用构造函数创建实例：

```s
class Point {
    _init_(Int32 x, Int32 y) {
        this.x = x; this.y = y;
    }
}

var p = Point(1, 2);
```

注意：构造函数不能声明返回类型，也不应显式返回值；构造过程先分配内存再执行 `_init_`。

---

## 属性访问器（get / set）

语言支持把 getter/setter 作为特殊成员来定义（解析器将 `get` / `set` 标记为特殊函数）。具体语法可采用显式访问器或独立的 get/set 方法，示例：

```s
// 示例：作为单独成员的访问器（语法示例）
public Int32 Count get() { ret this._count; }
public void Count set(Int32 v) { this._count = v; }
```

或（若语法支持属性块）：

```s
public Int32 Count {
    get { ret this._count; }
    set { this._count = value; }
}
```

编译器将在元模型中把 get/set 标记为对应的访问器函数（`isGet` / `isSet` 标志）。

---

## 接口与重写规则

- 类实现接口（`implements`）时必须提供接口中所有非默认实现的方法；否则类需声明为 `abstract`。
- 当继承父类时：
  - 如果父方法是非抽象且子类未声明 `override`，则子类继承父实现；
  - 如果父方法是 `abstract`，子类必须 `override` 并实现该方法，或声明自己为 `abstract`。

编译器在元模型阶段会检查这些语义并记录问题（日志或编译错误，取决于错误等级配置）。

---

## 示例集合

完整示例展示顶层函数、类方法、模板与构造：

```s
import CSharp.System;

// 顶层函数
fun max(Int32 a, Int32 b) { ret a > b ? a : b; }

// 模板函数
fun<T> swap(ref T a, ref T b) {
    T tmp = a; a = b; b = tmp;
}

class Base {
    abstract Int32 value();
}

class Child extends Base {
    Int32 v = 10;
    override Int32 value() { ret v; }
}

class Example {
    static void Run() {
        var c = Child();
        Debug.Write(c.value());
    }
}
```

---

## 常见错误与诊断

- 重载冲突：相同签名的函数重复定义会被检测并记录为错误。
- 抽象实现缺失：子类未实现父类抽象方法且未声明为 `abstract` 时，会在 `MetaClass` 处理继承成员阶段报告错误。
- 参数重复或非法默认值：函数参数名重复或默认值类型不匹配会导致解析/类型检查失败。

---

以上为 S 语言中函数与方法的详尽说明与示例；如果需要我可以把同等详细程度的改进应用到类、模板、集合或控制流章节。


