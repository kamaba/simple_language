# 模板（Templates / Generics)

S 语言支持模板（泛型）用于类和函数，允许在定义时参数化类型，从而在编译期间为不同类型生成具体实现。

主要特性：
- 模板类（`class<T>`）在实例化时替换类型参数生成具体类。
- 模板函数（`fun<T>`）在调用时根据类型参数生成具体函数实例。
- 支持模板约束（`extends` / `in` 样式），用于限制模板参数必须满足的基类或接口。

模板类示例：

```s
class Box<T> {
    T value = null;
    _init_(T v) { this.value = v; }
}

// 使用：Box<Int32> 在编译/元模型阶段生成具体 Box<Int32>
var b = Box<Int32>(10);
```

模板类继承与嵌套：

```s
class Pair<T1, T2> {
    T1 first;
    T2 second;
}

class PairList<T> extends List<Pair<T, T>> { }

var pl = PairList<Int32>();
```

模板函数示例：

```s
fun<T> identity(T x) { ret x; }

var a = identity<Int32>(10);
```

模板约束：

```s
fun<T extends Number> sumAll(List<T> vals) { /* ... */ }
```

实现细节（元模型相关）：
- 模板参数可以带约束，编译器在 `MetaTemplate` 层面记录这些约束，并在实例化时校验。
- 模板函数与模板类的生成会影响 parseLevel 与绑定顺序（模板优先、类模板次之、普通函数最后）。