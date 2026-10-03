# Component（组件基类）

`Component` 是组件基类，位于 `Std` 标准库（`source/Front/Lib/Std/Component.sl`），`import Std` 后继承使用。

设计模型为**组件即节点**的组合树：每个 `Component` 持有 `parent`（上级组件）与子组件列表 `components`，全部查询 / 消息 API 都在这棵树上进行。本实现不引入 `GameObject` / `Transform` 等容器类，保持 Std 模块自包含、无循环依赖；若后续需要 `gameObject` 反向引用，由宿主类挂载时设置 `parent` 即可。

类声明：

```sl
import Std;

public class MyComp extends Component
{
    public int power = 0

    override void onMessage( string methodName )
    {
        Console.println("  [onMessage] " + this.name + " <= " + methodName)
    }
}
```

## 字段与属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `parent` | `Component` | 上级组件，由 `addComponent` 挂载时设置 |
| `components` | `List<Component>` | 自身挂载的子组件（初始容量 4） |
| `enabled` | `bool` | 是否启用（默认 `true`） |
| `tag` | `string` | 标签（默认 `"Untagged"`） |
| `name` | `string` | 名称，便于调试（默认 `"Component"`） |
| `isActiveAndEnabled` | `bool`（get） | 自身启用且整条 `parent` 链都启用才算激活 |

---

## 1. 挂载与启用

```sl
Component root = new()
root.name = "root"

CompSpeed speedA = new()
speedA.name = "speedA"

root.addComponent(speedA)        # speedA.parent == root
speedA.setEnabled(false)         # speedA.enabled == false
```

`addComponent` 会同时设置 `comp.parent = this`；传入 `null` 静默忽略。

---

## 2. 组件查询（直接子级）

只遍历自身的 `components` 列表，**不做 enabled 门控**（禁用的直接子组件同样命中）：

```sl
CompSpeed c  = speedA.getComponent<CompSpeed>()      # 泛型版，未找到返回 null
Component c2 = root.getComponent(r1.type)            # 按 Type 查（r1.type 为组件实例的类型）
Component c3 = root.getComponent("CompSpeed")        # 按类型短名（不含命名空间）查

List<Component> all = root.getComponents<CompSpeed>()  # 全部命中，列表顺序 = 挂载顺序
CompSpeed t = speedA.tryGetComponent<CompSpeed>()      # 尝试获取：null 即未找到
```

---

## 3. 树上查找（InChildren / InParent 系列）

带 `bool includeInactive` 参数，门控语义有一个关键细节：

- **门检查在子节点上做**：`includeInactive == false` 时跳过 `enabled == false` 的子节点；
- **递归无条件下钻**：禁用子树仍会被深入，其中的启用组件依然可见（"递归越门"）；
- `InParent` 系列**含自身**，沿 `parent` 链上行；
- `InChildren` 系列**不含自身**，先序遍历整棵子树；
- 泛型 `T` 匹配**上转型感知**：`T` 写基类（如 `Component`）时可命中子类实例。

```sl
# 树：
# root ─ speedA(CompSpeed) ─ life1(CompLife)
#      │                   └ life2(CompLife)
#      ├ lifeB(CompLife) ─ speed3(CompSpeed)
#      └ speedC(CompSpeed)

CompSpeed s = root.getComponentInChildren<CompSpeed>(false)   # speedA（先序第一个）
CompSpeed p = life1.getComponentInParent<CompSpeed>(false)    # speedA
Component self = life1.getComponentInParent<Component>(false) # life1（含自身）

List<Component> all = root.getComponentsInChildren<CompLife>(false)  # [life1, life2, lifeB]
List<Component> ups = life1.getComponentsInParent<Component>(false) # [life1, speedA, root]（由近及远）
```

---

## 4. 标签

```sl
root.tag = "Player"
bool hit = root.compareTag("Player")   # this.tag == otherTag
```

---

## 5. 消息

消息模型为**简化版**：不按方法名字符串反射调用，而是统一派发到 `onMessage` 虚方法，子类 `override` 响应：

| 方法 | 分发范围 | 顺序 |
|------|----------|------|
| `sendMessage(name)` | 自身 + 整棵子树 | 先序：自身先，随后依次每棵 child 子树 |
| `broadcastMessage(name)` | 自身 + 整棵子树 | 与 `sendMessage` 同一实现（整树先序广播） |
| `sendMessageUpwards(name)` | 自身 + 全部祖先 | 沿 `parent` 链由近及远 |

```sl
speedA.sendMessage("ping")
#   [onMessage] speedA <= ping
#   [onMessage] life1 <= ping
#   [onMessage] life2 <= ping

life1.sendMessageUpwards("up")
#   [onMessage] life1 <= up
#   [onMessage] speedA <= up

root.broadcastMessage("bc")    # 整树先序：speedA, life1, life2, lifeB, speed3, speedC 各一行
```

消息分发**无 enabled 门控**；任一 `onMessage` 回调执行失败即中止后续分发；树上遍历深度上限 1024（防御环引用）。

---

## 6. 语义速查表

| API | 含自身 | 查找范围 | enabled 门 |
|-----|--------|----------|-----------|
| `getComponent<T>()` / `tryGetComponent<T>()` | 否 | 直接子级 | 无 |
| `getComponent(Type)` / `getComponent(string)` | 否 | 直接子级 | 无 |
| `getComponents<T>()` | 否 | 直接子级 | 无 |
| `getComponentInChildren<T>(b)` | 否 | 整棵子树（先序） | 子节点门，递归越门 |
| `getComponentsInChildren<T>(b)` | 否 | 整棵子树（先序） | 同上 |
| `getComponentInParent<T>(b)` | **是** | `parent` 链（由近及远） | 每节点门 |
| `getComponentsInParent<T>(b)` | **是** | `parent` 链 | 每节点门 |
| `sendMessage` / `broadcastMessage` | **是** | 整棵子树（先序） | 无 |
| `sendMessageUpwards` | **是** | `parent` 链 | 无 |

`isActiveAndEnabled` 门：`getComponentInParent` 系列在链上每个节点做 enabled 检查，等价于"链上门控"；`InParent(b)` 中 `b == false` 时禁用祖先直接导致越不过该节点。

---

## 7. 其它

- `toString()`：返回 `Component(name=..., type=..., enabled=...)` 形式字符串。
- 构造 `_init_()` 会重置全部字段为默认值。

---

## 8. 实现注记（C VM）

- 查询 / 消息系列已下沉为 C VM 系统方法（`SystemComponentGetComponent` 等 11 个，注册于 `Std.jsonc` 的 `systemCalls[].cvmFunction`），实现见 `csimple_lang/src/vm/system_method_call/component_system_method.c`；SL 层仅保留薄转发。
- SL 层向 C 侧传 `Array<T>(0)` 空数组作为**类型探针**：其元素类型即目标类型 `T`，C 侧一次解析后做廉价类型检查，避免每次 `c as T` 触发 CastClass 的 payload 重新解析。
- `onMessage` 分发走虚表（`vtable`），子类 override 后按实际类型派发。
- 语义验证用例：`test/ExpendTest/ComponentTest.sl`（36 项 check，覆盖查询 / 门控 / 消息 / 万次压力循环）。
