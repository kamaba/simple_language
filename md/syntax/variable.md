# S语言的变量， 主要用来存储某个类型的存储的名称表示，对象内部变量，对象静态变量，全局变量，函数内临时变量。

## 对象变量
1. 非静态变量，在定义对象后，在申请类型，获取对象后，该对象自动集成了，定义类的所有非静态变量，通过对象访问。
2. 静态变量，是在类申明时，已定义的变量，可以通过类名称直接访问

### 类/对象变量的语法
```ruby 

Class0
{
    Variable0 = 0;
}

Class1 extends Class0
{
    Variable1 = 0i;
    Variable2 = "abcd";
    static Variable3 = 2.0f;
}

Main()
{
    Class1 c;
    v0 = c.Variable0;   #继承与父类的非静态成员，在子类中可以访问。
    c.Variable1 = 20;  #改变c对象内部Variable1的值，Variable1，即c的内部变量
    string x = c.Variable2;  #c的Variable2引用。

    float xx = Class1.Variable3; #静态变量需要，使用[类名.静态名称]的读取。
}
```
规则: 在使用类的变量时，不允许有任何重名，包括父类与子类中的名称， 如果父类定义了name,则在子类中不允再重新定义该名称，所以在定义的时候，尽量只歧义的名称， 在新建对象时，子类对象会继承父类对象的非静态成员，并且计算大小。 静态成员，只能通过原类名.静态成员名称的方式访问。 类成员变量声明必须初始化：不支持 `int v;` 无初始化声明，必须写 `int v = 1;`（2026-10-08 起违反报 Error 21472 MetaCoreMemberVariableRequireInit，明确提示并附修复指引；此前误报 MetaCoreExpressIsNull 且 extend 拼接"传入参数与要求参数不对应"兜底文案）。local{} 块合成成员等无源初始化场景不受影响。

------------------
## 全局变量 是指通过ProjectConfig配置，然后在代码中通过global关键字，直接访问

# 类/对象变量的语法

```python
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        pi = global.pi;
        h1 = global.minInt;
    }
    static SetMaxInt( int a )
    {
        global.maxInt = a;
    }
}


const data ProjectConfig{
    name = "test project";
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 10000i;
    }
}
```

### 规则，在全局变量使用中，只有在globalVariable定义后，代码中才可以引用，不允许其它方式创建，在设置global变量时，只能在Project函数中，或者是DllExpore中，或者是Compile中进行设置，不允许在其它类中进行设置。

## 函数内变量 指在函数内定义的临时变量，可以通过{}的区域，定义变量的使用范围。
# 函数内变量的语法

```python
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        var1 = 20;
        {
            var2 = "aaa";
        }
        var1 = 30;
    }
}


const data ProjectConfig{
    name = "test project";
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 10000i;
    }
}
```

### 规则，在函数每个{}区间对应的变量的范围，如果超出范围，不能使用该变量， 在函数变量定义中，不允许有重复名称，如果重复名称，则一般会报错。

## const 只读变量（2026-10-02 实装）

`const` 关键字声明的变量为只读：声明初始化之后只能读取，不允许再赋值或修改。三种使用位置：

### 1. const 成员变量

```python
class Class0
{
    const Int32 CF = 10;          # 实例 const 成员
    static const Int32 SCF = 20;  # 静态 const 成员
}

Class0 c = Class0();
c.CF = 100;        # 错误: const 成员不允许赋值（MetaCoreAssignStatementConst）
Class0.SCF = 5;    # 错误: 静态 const 成员同样拦截
```

const 标记随 SLIR 导出（字段 `flags` bit16），**跨模块**导入的类其 const 成员同样被拦截
（2026-10-08 补齐成员字段链路；只读访问不受影响）：

```python
# 引用其它模块导出的 ConstLib 后
Int32 r = ConstLib.SCF;    # 正确: 跨模块只读访问 const 静态成员
ConstLib.SCF = 5;          # 错误: 跨模块 const 成员赋值同样拦截（MetaCoreAssignStatementConst）
```

探针用例：`test/Other/ConstProbe/`（ConstLib 导出 / ConstCross 跨模块引用）。

### 2. const 局部变量（语句）

```python
static Main()
{
    const Int32 la = 10;
    const lb = 30;          # 无类型形态，由初始值推断
    la = 20;                # 错误: const 局部变量不允许再赋值
    lb = 40;                # 错误: 无类型 const 同样拦截
    Int32 r = la + lb;      # 正确: 只读访问
}
```

### 3. const 函数形参

形参带 `const` 标记后，函数体内只读：

```python
static Int32 takeConst( const Int32 a )
{
    a = 100;        # 错误: const 形参在函数体内不允许赋值
    ret a + 1;      # 正确: 只读使用
}
```

### const 传参约束

const 修饰的**实参**只能传给带 `const` 标记的形参（含跨模块引用的方法，`isConst` 随 SLIR 导出）：

```python
const Int32 la = 10;
takeConst( la );    # 正确: const 实参 -> const 形参
takeConst( 5 );     # 正确: 字面量/编译期常量不受限制
takeNormal( la );   # 错误: const 实参传给了非 const 形参（MetaCoreParamConstToNonConst, LID=21468）
```

### 不参与约束的场景（编译期常量与独立特性）

以下 const 属于类型系统常量或机制内部标记，**不触发**赋值拦截与传参约束：

| 场景 | 示例 | 说明 |
|------|------|------|
| enum 枚举值 | `FileStream(path, FileMode.Read, FileAccess.Read)` | 枚举值天然只读，等同字面量 |
| const static 类成员传参 | `Mathd.min(x, BigNumber.CAPACITY)` | 编译期常量可传普通形参（赋值拦截仍生效） |
| `const data` 容器成员 | bind 宿主经 setter 写底层 data 实例成员 | 容器级只读是独立特性，见 [data.md](data.md) |
| jsonc `global.data` 注入变量 | `global.var1 = 99`（isolate 隔离语义） | 注入 const 是初始值折叠标记，非只读承诺 |

### 相关错误码

| LID | 错误 | 说明 |
|-----|------|------|
| `MetaCoreAssignStatementConst` | const 左值再赋值 | 覆盖 `=`、复合赋值（`+=` 等）、`++`/`--` |
| `MetaCoreParamConstToNonConst` (21468) | const 实参传非 const 形参 | 重载决议的精确/宽松两条匹配路径均检查 |

## 变量命名规则（禁止关键字）

变量的名称不允许使用关键字，Front 解析阶段会直接报错（LID `NodeStructParseNameIsKeyword`）。适用于所有变量定义（对象变量、函数内变量、for 循环变量）以及函数/闭包/lambda 参数命名。

```python
var new = 5        # 错误: new 是关键字
string string = "x" # 错误: string 是基本类型关键字
var var = 5        # 错误: var 是关键字
int global = 5     # 错误: global 是关键字
static f( int if ) # 错误: 参数名 if 是关键字
var ok = 1         # 正确
```

注意：
1. 基本类型词（int/string/object/bool/float/double/byte/short/long 等）也是关键字，不允许作为变量名或参数名。
2. `this`、`base`、`local`、`global` 作为限定前缀使用（如 `this._value = b`、`global.x = 5`）是合法的；但单独作为变量名（如 `int global = 5`）会报错。
3. `$` 开头的变量（`$xxx`）不受影响。