# 接口（Interface）

接口用于声明一组方法签名而不提供具体实现。类可以实现一个或多个接口，从而承诺提供接口中声明的方法。

语法示例：

```s
interface IPrintable {
    void print();
}

class Document implements IPrintable {
    override void print() {
        Console.print("Document print");
    }
}
```

说明：
- 接口只能包含方法签名（以及可选的默认实现，若语言扩展支持）。
- 使用 `implements` 关键字声明实现关系：`class C implements I1, I2 {}`。
- 若类实现接口但未实现接口中所有方法，则该类必须声明为 `abstract`。
- 接口可用于多态和依赖注入：`IPrintable p = new Document(); p.print();`。

示例（接口泛型）：

```s
interface IList<T> {
    void add(T item);
    T get(Int32 index);
}

class ArrayList<T> implements IList<T> {
    override void add(T item) { /* ... */ }
    override T get(Int32 index) { /* ... */ ret default; }
}
```

