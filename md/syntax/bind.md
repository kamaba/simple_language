# bind 关键字

`bind` 用于把一个或多个 `data` 绑定到 `class` 或 `interface` 上，在编译阶段自动展开为成员变量、字段访问器（get/set）、数据访问器和 `_init_` 重载。

绑定后的效果可以理解为：给类型增加一组受约束的数据字段与访问入口，便于统一数据组织和处理。

> 限制：`bind` 只能用于 `class` 和 `interface`，不能用于 `data` 或 `enum` 定义上（会报错）。

---

## 1. 基本语法

`bind` 后面必须跟 `data` 类型名，支持多个，以逗号分隔。`data` 可以是普通 `data`，也可以是 `const data`。

```sl
data DataA
{
    a = 20
}
data DataB
{
    b = 100
}
class ClassName bind DataA, DataB
{
    // class body
}

interface InterfaceName bind DataA, DataB
{
    // interface body
}

const data ConfigData
{
    port = 8080
}

class Service bind ConfigData
{
}
```

使用：

```sl
class LogicC
{
    static fun()
    {
        ClassName cn = new()

        cn.DataA.a = 100
        int b = cn.DataB.b
        b2 = cn.b               # 直接访问
        var db = cn.DataB        # 直接访问 DataB 的定义
    }
}
```

---

## 2. 绑定到 class 的展开语义

当 `class` 绑定了 `data` 后，编译器自动在类内注入以下合成代码：

### 2.1 成员变量

为每个被绑定的 data 生成一个内部成员变量：

```sl
data DA { a = 20, b = 30 }
data DB { a2 = 100, b2 = 100 }

class CA bind DA, DB
{
}

#! 等价于自动注入：
class CA
{
    DA _DA = new()
    DB _DB = new()
}
!#
```

### 2.2 字段访问器（get/set）

为每个 data 的字段生成 get/set 代理方法：

```sl
#! 自动注入等价于：
class CA
{
    get int a() { ret this.DA.a }
    set void a( int _a ) { this.DA.a = _a }
    get int b() { ret this.DA.b }
    set void b( int _b ) { this.DA.b = _b }
    get int a2() { ret this.DB.a2 }
    set void a2( int _a2 ) { this.DB.a2 = _a2 }
    get int b2() { ret this.DB.b2 }
    set void b2( int _b2 ) { this.DB.b2 = _b2 }
}
!#
```

因此可以：

```sl
CA c = new()

# 绑定名访问
int v1 = c.DA.a
int v2 = c.DB.a2

# 直访（由 get/set 代理）
int v3 = c.a
int v4 = c.a2

# 直访写入
c.a = 50
c.a2 = 500
```

### 2.3 数据访问器

为每个绑定的 data 生成数据访问器，可通过 `类名Data()` 方法直接访问内部 data 实例：

```sl
#! 自动注入等价于：
class CA
{
    get DA DAData() { ret this.DA }
    set void DAData( DA _data ) { this.DA = _data }
    get DB DBData() { ret this.DB }
    set void DBData( DB _data ) { this.DB = _data }
}
!#
```

### 2.4 _init_ 重载

自动为 1 到 N 个参数（N 为绑定的 data 个数）生成 `_init_` 重载：

```sl
class CA bind DA, DB
{
}

#! 自动注入等价于：
class CA
{
    _init_( DA _t1 ) { this.DA = _t1 }
    _init_( DA _t1, DB _t2 ) { this.DA = _t1; this.DB = _t2 }
}
!#

# 使用
CA c1 = CA(daInstance)
CA c2 = CA(daInstance, dbInstance)
```

若类已有相同参数数量的 `_init_`，则自动跳过（不覆盖）。

---

## 3. 同名字段冲突与重写

如果多个绑定 `data` 中存在同名成员，或与 `class` 自身成员重名，编译器会跳过自动生成该字段的 get/set（输出告警），需要开发者手动实现。

```sl
data DA { a = 20 }
data DB { a = 30 }

class CA bind DA, DB
{
    # 冲突重写策略：统一优先映射到 DA.a
    get a() { ret this.DA.a }
    set a( int v ) { this.DA.a = v }
}

CA c = new()
Console.println(c.a)     # 20（DA.a 的值）
c.a = 99
Console.println(c.DA.a)  # 99
```

如果类自身已定义了同名字段或同名函数，自动注入也会静默跳过，由开发者决定映射逻辑。

---

## 4. 绑定到 interface 的语义

`interface` 也可以绑定 `data`，展开为**抽象** get/set（无函数体）：

```sl
data DA { a = 20, b = 30 }

interface INF bind DA
{
    # 自动注入抽象访问器等价于：
    # int get a()
    # void set a( int _a )
    # int get b()
    # void set b( int _b )
}
```

接口中的默认方法可以直接访问绑定数据成员：

```sl
public interface CalcPrice bind BookData
{
    float calc()
    {
        ret this.price * 1
    }
}
```

---

## 5. 接口约束

当接口 `bind` 了某 `data`，实现该接口的类也必须 `bind` 同一 `data`（直接或等价方式），否则编译报错。

```sl
const data BookData
{
    name = "ABC"
    price = 20
}

public interface CalcPrice bind BookData
{
    float calc() { ret this.price * 1 }
}

public class SeltBookData bind BookData interface CalcPrice
{
    public int count = 20

    override float calc()
    {
        ret this.price * this.count
    }
}
```

这样接口方法内可以直接使用 `this.price`，因为接口和实现类都绑定了 `BookData`。

---

## 6. 继承链中的 bind

如果类有继承关系，编译器会递归收集祖先链上的所有 bind（最大深度 32 层），去重后统一展开。子类不会重复生成祖先已绑定的 data 成员。

---

## 7. 综合示例

```sl
const data BookData
{
    name = "ABC"
    pageCount = 20
    price = 20
}

data BP
{
    width = 30
    height = 40
}

# 类 bind 多个 data
BookC bind BookData, BP
{
}

# 接口 bind data
public interface CalcPrice bind BookData
{
    float calc() { ret this.price * 1 }
}

# 类 bind + 接口
public class SeltBookData bind BookData interface CalcPrice
{
    public int count = 20
    override float calc() { ret this.price * this.count }
}

# 冲突重写
data DA2 { a = 1 }
data DB2 { a = 2 }

BindConflictClass bind DA2, DB2
{
    get a() { ret this.DA2.a }
    set a( int v ) { this.DA2.a = v }
}

# 使用
BookC bc = new()
bc.name = "hahah"              # 直访 BookData.name
bc.width = 20                  # 直访 BP.width
bc.height = 40                 # 直访 BP.height

# 绑定名访问
bc.BookData.name = "bind-name-access"
bc.BP.width = 101
bc.BP.height = 202
```

---

## 8. 设计目的

`bind` 的主要价值：

1. **数据结构绑定**：将 `data` 的字段自动注入到类中，避免手写大量样板代码。
2. **统一访问入口**：提供绑定名访问（`c.DataName.field`）和字段直访（`c.field`）两种方式。
3. **接口约束传播**：通过接口绑定，把"数据约束"提升为类型约定，确保实现类具备所需数据字段。
4. **冲突可重写**：同名字段冲突时由开发者手动决定映射策略，保证灵活性。

---

## 9. 对照测试

可参考测试样例：

- [test/BaseTest/BindDataTest.sl](../../test/BaseTest/BindDataTest.sl) — 多 data 绑定、字段直访、冲突重写、接口 bind + 类 bind
- [test/BaseTest/InterfaceDataAndBindData.sl](../../test/BaseTest/InterfaceDataAndBindData.sl) — 接口和实现类都 bind 同一 data 的标准用法

---

## 10. 实现机制

`bind` 的语义展开由 `BindExpandManager`（[source/Front/Core/BindExpandManager.cs](../../source/Front/Core/BindExpandManager.cs)）完成，在编译流程的 MetaCore 阶段、CreateNamespace 之前执行：

1. **Pass 1**：收集全工程所有 `data` 定义到字典。
2. **Pass 2**：遍历每个文件的类，对有 `bind` 的类或接口进行展开：
   - 解析 bind 引用的 data 名称，收集字段和类型信息。
   - 收集祖先链上的 bind（去重）。
   - 检查接口约束：若接口 bind 了某 data，类自身也必须 bind 该 data。
   - 构建字段冲突表，同名字段告警并跳过。
   - 生成合成源码（成员变量、get/set、数据访问器、_init_ 重载）。
   - 将合成源码重新解析后注入到目标类中。
