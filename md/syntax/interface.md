# 接口（Interface）

接口用于声明一组方法签名而不提供具体实现。类可以实现一个或多个接口，从而承诺提供接口中声明的方法。

语法示例：

```s
interface IPrintable {
    void print()
}

class Document interface IPrintable {
    override void print() {
        Console.print("Document print");
    }
}
```

说明：
- 接口只能包含方法签名（以及可选的默认实现，若语言扩展支持）。
- 在类头用 `interface` 标记声明实现关系（写在 `extends` 之后，可逗号分隔多个）：`class C extends P interface I1, I2 {}`。语言没有 `implements` 关键字。
- 若类实现接口但未实现接口中所有方法，则该类必须声明为 `abstract`。
- 接口可用于多态和依赖注入：`IPrintable p = Document(); p.print();`。
- 接口之间可用 `extends` 继承其它接口：`interface ILInstList<T> extends IList<object> {}`（见 test/BaseTest/GenFunction.sl）。

示例（接口泛型）：

```s
interface IList<T> {
    void add(T item)
    T get(Int32 index)
}

class ArrayList<T> interface IList<T> {
    override void add(T item) { /* ... */ }
    override T get(Int32 index) { /* ... */ ret T.default; }
}
```
