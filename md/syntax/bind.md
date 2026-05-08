# bind 关键字

`bind` 用于把一个或多个 `data` 绑定到 `class` 或 `interface` 上。

绑定后的效果可以理解为: 给类型增加一组受约束的数据字段与访问入口，便于统一数据组织和处理。

## 1. 基本语法

`bind` 后面必须跟 `data` 类型名，支持多个，以逗号分隔。

`data` 可以是普通 `data`，也可以是 `const data`。

```sl
class ClassName bind DataA, DataB {
	// class body
}

interface InterfaceName bind DataA, DataB {
	// interface body
}

const data ConfigData {
	port = 8080
}

class Service bind ConfigData {
}
```

## 2. 绑定到 class 的语义

当 `class` 绑定了 `data` 后，可理解为 `class` 内部增加了对应的 `data` 成员。

例如:

```sl
data DA { a = 20, b = 30 }
data DB { a2 = 100, b2 = 100 }

class CA bind DA, DB {
}
```

上例可理解为:

1. `CA` 内部具备 `DA`、`DB` 两份数据。
2. 可以通过绑定名访问: `c.DA.a`、`c.DB.a2`。
3. 同时可直接访问已映射字段: `c.a`、`c.b`、`c.a2`、`c.b2`。

并且从测试语义看，绑定对象会以“内部持有字段”的方式存在，可理解为 `_DataName` 形式的内部成员(语义说明，不要求源码可直接访问该命名)。

示例:

```sl
CA c = new()

int x1 = c.DA.a
int x2 = c.a
int y1 = c.DB.a2
int y2 = c.a2
```

## 3. 直访字段的等价展开

为了支持 `c.a` 这类直接访问，绑定时等价于生成字段代理访问器。

以 `DA.a` 为例，语义上可理解为:

```sl
get a() { return DA.a }
set a(int v) { DA.a = v }
```

注意: 上述是语义等价说明，具体实现细节以编译器实际行为为准。

## 4. 同名字段冲突与重写

如果多个绑定 `data` 中存在同名成员，或与 `class` 自身成员重名，则会出现访问歧义。

例如 `DA.a` 与 `DB.a` 同时存在，或 `CA` 本身也定义了 `a`。

这时应在 `class` 内显式重写访问逻辑，决定 `a` 实际映射到哪一个来源。

语义示例:

```sl
class CA bind DA, DB {
	get a() {
		// 自选策略: 返回 DA.a、DB.a 或 CA 内部字段
		return DA.a
	}

	set a(int v) {
		// 自选策略: 写入 DA.a、DB.a 或 CA 内部字段
		DA.a = v
	}
}
```

## 5. 绑定到 interface 的语义

`interface` 也可以绑定 `data`:

```sl
interface INF bind DA {
}
```

接口中的默认方法可以直接访问绑定数据成员，例如 `this.price`。

```sl
public interface CalcPrice bind BookData {
	float calc() {
		ret this.price * 1
	}
}
```

当某个类实现该接口时:

```sl
class CA interface INF {
}
```

在当前测试约束下，若接口已 `bind DA`，实现类通常也需要具备对应 `bind DA` 关系(直接或等价方式)，否则应视为绑定约束不满足。

示例:

```sl
public interface CalcPrice bind BookData {
	float calc(){
		ret this.price * 1
	}
}

public class SellBookData bind BookData interface CalcPrice {
	public int count = 20
	override float calc(){
		ret this.price * count
	}
}
```

## 6. 设计目的

`bind` 的主要价值:

1. 让数据结构与类型能力绑定，形成明确约束。
2. 提供统一访问入口(绑定名访问 + 字段直访)。
3. 通过接口绑定，把“数据约束”提升为类型约定，便于跨类型做一致化处理。

## 7. 综合示例

```sl
data DA { a = 20, b = 30 }
data DB { a2 = 100, b2 = 100 }

interface INF bind DA {
}

class CA bind DA, DB interface INF {
	// 若有重名字段，可在此重写 get/set 决定映射策略
}

CA c = new()

// 绑定名访问
int v1 = c.DA.a
int v2 = c.DB.a2

// 直访访问(由绑定映射而来)
int v3 = c.a
int v4 = c.a2
```

以上示例体现了 `bind` 对数据扩展、字段代理以及接口约束传播的核心行为。

## 8. 对照测试

可参考测试样例:

- [test/ExpendTest/BindDataTest.sl](../../test/ExpendTest/BindDataTest.sl)

该文件覆盖了:

1. 多 `data` 绑定与字段直访。
2. 接口 `bind` + 默认方法内访问绑定字段。
3. 实现类在接口绑定约束下的 `override` 访问。
4. 绑定名访问与同名冲突重写示例。
