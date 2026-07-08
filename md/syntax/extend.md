# 类与继承（extend）

本文整理类定义、继承扩展以及模板（泛型）赋值规则，重点说明 Front 层的模板协变校验。

## 1. 类定义

类用于描述数据与行为，实例对象由成员变量与成员函数构成。

```s
ClassName
{
    Int32 value = 0

    Int32 add(Int32 x)
    {
        ret value + x
    }
}
```

说明：
- 未显式标注访问性时默认按语言默认规则处理。
- 成员访问使用 `.`。
- 返回类型可省略（由编译器推断）时，需保证语义可推断。

## 2. 对象创建方式

常见方式：

1) 构造调用

```s
Class1
{
    Class1(Int32 p1)
    {
        m1 = p1
    }
    Int32 m1 = 10
}

Class1 c1 = Class1(20)
```

2) 显式类型 + `{}` 初始化（按目标类型构造并赋成员）

```s
Class1 c2 = { m1 = 100 }
```

3) 仅声明（由语言默认对象语义决定），或显式 `null`

```s
Class1 c4 = null
```

## 3. extend / 继承关系

- 子类可继承父类成员并扩展自身能力。
- 赋值关系遵循类型系统：同型、子类到父类、接口实现等由类型关系判定。

## 4. 模板（泛型）赋值规则（Front 校验）

### 4.1 基本规则

- **非接口泛型**：要求模板参数完整一致（不支持类模板的协变赋值）。
- **接口泛型**：允许协变校验（目标模板参数可接收来源模板参数）。
  - 同接口间：`I<TTarget> <- I<TExpr>` 按模板参数逐项协变判断。
  - 模板类到接口：`I<TTarget> <- C<TExpr>`（`C` 实现了 `I`）也按接口实参逐项协变判断。
- **数组实体类型**：不支持协变，`Array<T1>` 赋给 `Array<T2>` 必须完整一致。

### 4.2 Front 校验覆盖范围

Front 侧对上述规则在以下场景统一生效：

1. 变量赋值 / 初始化赋值
2. 函数调用实参与形参匹配

即：不仅 VM 运行期有校验，Front 编译期也会先做模板协变可赋值判断。

### 4.3 允许的接口模板协变示例

以下语句为正确语法（来自 `test/BaseTest/ArrayTest.sl` 149/150）：

```s
IIterator<Num> it = concrete.iterator
IIterable<Object> it2 = concrete
```

说明：
- `IIterator<Num> <- IIterator<Int32>`：按只读遍历语义允许 Number 抽象协变。
- `IIterable<Object> <- Int32[]`：按可迭代接口元素可赋值规则允许。

### 4.4 不允许示例

```s
SomeGenericClass<Object> a = SomeGenericClass<Int32>()
```

上述属于**非接口泛型协变**，Front 层会判定为类型不兼容。
