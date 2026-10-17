# BigNumber / BigDecimal / Factor（大数与数论）

Math 库的大数与数论部分，位于 `source/Front/Lib/Math/`，命名空间 `Math`。三个类均为**纯 SL 实现**（不依赖 FFI 与 systemCalls 绑定的数学 DLL），仅在内部使用 `SystemArrayFillValue` / `SystemArrayCopy` / `SystemStringCharCodeAt` 等主工程 system method call 完成数组批量操作与字符访问。

| 类 | 别名（@Nickname） | 基类 | 定位 |
|---|---|---|---|
| BigNumber | BigInt | Num | 任意精度有符号整数（约 256 位十进制） |
| BigDecimal | Decimal | Num | 任意精度定点小数（unscaled / 10^scale） |
| Factor | — | — | 纯静态数论工具类（18 个静态方法） |

---

## 1. BigNumber（BigInt）

### 1.1 内部表示与容量

| 常量 | 值 | 含义 |
|---|---|---|
| `BASE` | 10000 | 每段存 4 位十进制（10^4 定点分段） |
| `BASE_DIGITS` | 4 | 每段十进制位数 |
| `CAPACITY` | 64 | 固定段容量 ≈ 256 位十进制，避免动态扩容 |

- `_digits`：`Array<Int32>`，**低位在前**（`_digits[0]` 是最低段）
- `_sign`：0 = 零，1 = 正，-1 = 负
- 溢出行为：超出 CAPACITY 的高位段静默丢弃（乘法/进位处均有钳制）

### 1.2 构造

| 构造 | 说明 |
|---|---|
| `BigNumber()` | 零 |
| `BigNumber( Int32 v )` | 由 Int32 转换（含负数） |
| `BigNumber( string text )` | 等价于 `BigNumber.parse( text )` |

内部缓冲用 `SystemArrayFillValue( digits, 0, CAPACITY, 0 )` 一次批量清零，拷贝用 `SystemArrayCopy`。

### 1.3 算术方法（均返回新对象，不改 this）

| 方法 | 签名 | 说明 |
|---|---|---|
| add | `BigNumber add( BigNumber other )` | 同号绝对值相加；异号大减小，符号取绝对值大者 |
| sub | `BigNumber sub( BigNumber other )` | `this.add( other.negate() )` |
| multiply | `BigNumber multiply( BigNumber other )` | 分段竖式乘，pos >= CAPACITY 处丢弃 |
| divMod | `Array<BigNumber> divMod( BigNumber other )` | 长除法（逐段二分试商），返回 `[商, 余数]`；**余数符号与被除数一致**；除零返回 `[0, 0]` 交由调用方处理 |
| div | `BigNumber div( BigNumber other )` | `divMod` 的商（整除截断） |
| mod | `BigNumber mod( BigNumber other )` | `divMod` 的余数（符号随被除数，非 Python 语义） |
| negate | `BigNumber negate()` | 取反 |
| clone | `BigNumber clone()` | 深拷贝（SystemArrayCopy 整段复制） |
| compare | `int compare( BigNumber other )` | -1 / 0 / 1 |

### 1.4 运算符重载

| 运算符 | 右操作数 | 非法类型行为 |
|---|---|---|
| `+` `-` `*` `/` `%` | `BigNumber` 或 `Int32`（Int32 先装箱转换） | 返回 `this` |
| `<` `<=` `>` `>=` `==` `!=` | `BigNumber` 或 `Int32`（经 compareToBoxed） | 恒为 1（即 `>` 成立） |

```sl
BigNumber a = BigNumber( "12345678901234567890" )
BigNumber b = BigNumber( 98765 )
BigNumber c = a.multiply( b )      # 方法式
BigNumber d = a * b                # 运算符式（等价）
BigNumber e = a + 100              # 右操作数支持 Int32
Array<BigNumber> qr = a.divMod( BigNumber( 7 ) )
# qr[0] = 商, qr[1] = 余数（符号与 a 一致）
```

### 1.5 Num 接口实现

| 成员 | 返回 | 说明 |
|---|---|---|
| `size` (get) | int | `_length * 32`（段数 × 32） |
| `byteLength` (get) | int | `_length * 4`（段数 × 4 字节） |
| `abs()` | Num | 绝对值（需 `as BigNumber` 落地后才能调用本类方法） |
| `floor()` / `ceil()` | Num | 整数即自身，返回 clone |
| `compareTo( Num other )` | Int8 | 其他数值类型经 `toInt32` 转换后比较 |
| `toInt32()` | Int32 | 高位累乘合成，**超出 Int32 范围时环绕** |
| `toFloat32()` / `toFloat64()` | Float32/Float64 | 经 `toInt32` 中转，仅精确到 Int32 范围 |

> 注意：`abs()` 返回基类 `Num`，需要继续调用 BigNumber 方法时先 `as BigNumber` 转型（库内部 `divMod` 即如此使用）。

### 1.6 静态工具

| 成员 | 签名 | 说明 |
|---|---|---|
| `zero` / `one` | static get | 常量 |
| `parse` | `BigNumber parse( string text )` | 十进制解析，支持 `-` 号 |
| `valueOf` | `BigNumber valueOf( Int32 v )` | 同 `BigNumber( v )` |
| `absValue` | `BigNumber absValue( BigNumber v )` | 绝对值 |
| `maxValue` / `minValue` | `(BigNumber a, BigNumber b)` | 最值 |
| `pow` | `BigNumber pow( BigNumber baseValue, Int32 exponent )` | 快速幂，指数需非负 |
| `factorial` | `BigNumber factorial( Int32 n )` | 阶乘，n 从 2 累乘 |
| `gcd` | `BigNumber gcd( BigNumber a, BigNumber b )` | 欧几里得，先取绝对值 |

### 1.7 字符串解析与输出

- `parse`：从右往左逐字符累乘（`SystemStringCharCodeAt` 取码）；**非数字字符直接跳过**（"12a3" 解析为 123）；首字符非数字时结果取负（`-123` 正常，但 `+123` 同样会得到 -123）
- `toString`：十进制输出；最高段原样输出，其余段经 `_pad4` 补足 4 位

```sl
BigNumber.parse( "-12345" )   # -12345
BigNumber.parse( "999999999999999999999999" ).toString()   # 原样输出
BigNumber( 0 ).toString()     # "0"
```

---

## 2. BigDecimal（Decimal）

### 2.1 内部表示

值 = `_unscaled / 10^scale`。例：`_unscaled = 12345, scale = 3` → `12.345`。

- `_unscaled`：`BigNumber`，未缩放整数
- `scale`：`public Int32`，小数位数（可直接读写）

### 2.2 构造

| 构造 | 说明 |
|---|---|
| `BigDecimal()` | 零 |
| `BigDecimal( Int32 v )` | 整数，scale = 0 |
| `BigDecimal( BigNumber unscaled, Int32 scale )` | 直接指定（unscaled 引用共享，不拷贝） |
| `BigDecimal( Int32 value, Int32 scale )` | `value / 10^scale` |
| `BigDecimal( string text )` | 等价于 `BigDecimal.parse( text )` |

### 2.3 算术方法与 scale 规则

| 方法 | 签名 | 结果 scale |
|---|---|---|
| add | `BigDecimal add( BigDecimal other )` | 取两者较大 scale（对齐后相加） |
| sub | `BigDecimal sub( BigDecimal other )` | 同 add |
| multiply | `BigDecimal multiply( BigDecimal other )` | 两者 scale 之和 |
| div | `BigDecimal div( BigDecimal other, Int32 extraScale = 8 )` | `dividend.scale + extraScale`（默认补足 8 位；除零返回 0） |
| negate | `BigDecimal negate()` | 不变 |
| clone | `BigDecimal clone()` | 不变（unscaled 深拷贝） |
| rescale | `BigDecimal rescale( Int32 targetScale )` | 目标值（放大补零 / **缩小截断**） |
| roundTo | `BigDecimal roundTo( Int32 targetScale )` | 目标值（**四舍五入** half-up；targetScale >= scale 时返回 clone） |
| compare | `int compare( BigDecimal other )` | —（对齐较大 scale 后比较） |

```sl
BigDecimal a = BigDecimal.parse( "12.345" )    # unscaled=12345, scale=3
BigDecimal b = BigDecimal( 100 )               # scale=0
a.add( b )        # 112.345, scale=3
a.multiply( b )   # 1234.500, scale=3
a.div( b )        # 0.12345000..., scale = 3 + 8 = 11
a.rescale( 1 )    # 12.3（截断）
a.roundTo( 1 )    # 12.3（四舍五入；12.35 -> 12.4）
```

### 2.4 运算符重载

与 BigNumber 同模式：`+` `-` `*` `/`（`_truediv_` 使用默认 extraScale = 8）与六个比较运算符均支持右操作数 `BigDecimal` 或 `Int32`；非法类型加法返回 `this`、比较恒为 1。

### 2.5 Num 接口实现

| 成员 | 说明 |
|---|---|
| `size` / `byteLength` (get) | 委托 `_unscaled` |
| `abs()` | unscaled 取绝对值 |
| `floor()` | 去掉小数部分；**负数有余数时再减 1**（真向下取整） |
| `ceil()` | 去掉小数部分；**正数有余数时再加 1**（真向上取整） |
| `compareTo( Num other )` | 其他数值类型经 `toInt32` 转换 |
| `toInt32()` | 截断小数取整数部分 |
| `toFloat32()` / `toFloat64()` | 经 `toInt32` 中转，**不含小数部分** |

### 2.6 静态工具

| 成员 | 说明 |
|---|---|
| `zero` / `one` (static get) | 常量 |
| `fromInt32( Int32 v )` | 同 `BigDecimal( v )` |
| `fromUnscaled( BigNumber unscaled, Int32 scale )` | 直接组装 |
| `parse( string text )` | 支持 `"12.345"` / `"-0.5"` / `"100"`（小数位数即 scale） |
| `maxValue` / `minValue` | 最值 |

`toString`：`整数部分.小数部分`，小数部分左侧补零至 scale 位；scale <= 0 时直接输出 unscaled。

---

## 3. Factor（数论工具）

纯静态工具类，无实例状态，全部方法 `public static`，入参均为 `Int32`。

### 3.1 质数与分解

| 方法 | 签名 | 说明 |
|---|---|---|
| isPrime | `bool isPrime( Int32 n )` | 试除法（6k±1 优化）；n < 2 一律 false |
| primeFactors | `Array<Int32> primeFactors( Int32 n )` | 升序质因数（**含重复**），12 → `[2, 2, 3]`；n < 2 返回空数组 |
| distinctPrimeFactors | `Array<Int32> distinctPrimeFactors( Int32 n )` | 去重后的质因数集合 |

### 3.2 约数

| 方法 | 签名 | 说明 |
|---|---|---|
| divisors | `Array<Int32> divisors( Int32 n )` | 所有正约数升序；n < 1 返回空数组 |
| divisorCount | `Int32 divisorCount( Int32 n )` | 约数个数 |
| divisorSum | `Int32 divisorSum( Int32 n )` | 约数之和 |

### 3.3 公约数 / 公倍数

| 方法 | 签名 | 说明 |
|---|---|---|
| gcd | `Int32 gcd( Int32 a, Int32 b )` | 欧几里得（先取绝对值） |
| lcm | `Int32 lcm( Int32 a, Int32 b )` | a、b 任一为 0 返回 0 |
| extendedGcd | `Array<Int32> extendedGcd( Int32 a, Int32 b )` | 返回 `[gcd, x, y]`；系数按 `|a|`、`|b|` 求解 |

### 3.4 幂 / 阶乘 / 组合

| 方法 | 签名 | 说明 |
|---|---|---|
| powInt | `Int32 powInt( Int32 baseValue, Int32 exponent )` | 整数快速幂（指数需非负，无溢出保护） |
| factorial | `BigNumber factorial( Int32 n )` | 大数阶乘（避免 Int32 溢出） |
| permutation | `BigNumber permutation( Int32 n, Int32 k )` | 排列数 A(n, k)；k < 0 或 k > n 返回 0 |
| combination | `BigNumber combination( Int32 n, Int32 k )` | 组合数 C(n, k)（k > n/2 时用对称性化简） |

### 3.5 位运算与整根

| 方法 | 签名 | 说明 |
|---|---|---|
| isPowerOfTwo | `bool isPowerOfTwo( Int32 n )` | `n > 0 && ( n & ( n - 1 ) ) == 0` |
| nextPowerOfTwo | `Int32 nextPowerOfTwo( Int32 n )` | >= n 的最小 2 的幂；n <= 1 返回 1 |
| absInt | `Int32 absInt( Int32 v )` | 绝对值 |
| isqrt | `Int32 isqrt( Int32 n )` | 整数平方根（牛顿迭代向下取整）；n <= 0 返回 0 |
| eulerPhi | `Int32 eulerPhi( Int32 n )` | 欧拉函数 φ(n)；n <= 0 返回 0 |

> 内部说明：Factor 的数组裁剪用 `SystemArrayCopy( list, count )`（C 侧对 count 钳 0 到源长，生成新数组）。因 `SystemArrayCopy` 声明返回 `Array<object>`，先落地到局部变量再返回以通过严格类型检查。

---

## 4. system method call 使用情况

| 类 | FFI（@DllImport） | systemCalls（cvmFunction） | 内部使用的系统调用 |
|---|---|---|---|
| BigNumber | 无 | 无（纯 SL 实现） | SystemArrayFillValue / SystemArrayCopy / SystemStringCharCodeAt |
| BigDecimal | 无 | 无（纯 SL 实现，算术全部委托 BigNumber） | 同上（经 BigNumber） |
| Factor | 无 | 无（纯 SL 实现） | SystemArrayCopy |

三个类不依赖 `math_lib.dll`，在任何运行时环境（含无 FFI 场景）行为一致；性能热点（批量清零、整段拷贝）已下沉到主工程 system method call。

---

## 5. 使用示例

```sl
# 大数阶乘：30! ≈ 2.65e32，远超 Int32
BigNumber f = BigNumber.factorial( 30 )
System.debug( f.toString() )      # 265252859812191058636308480000000

# 大数快速幂与最大公约数
BigNumber p = BigNumber.pow( BigNumber( 2 ), 128 )       # 2^128
BigNumber g = BigNumber.gcd( BigNumber( 48 ), BigNumber( 180 ) )   # 12

# 精确小数运算（无浮点误差）
BigDecimal price = BigDecimal.parse( "19.99" )
Decimal count = Decimal( 3 )
Decimal total = price * count          # 59.97，精确
Decimal avg = total / count            # scale = 2 + 8 = 10
System.debug( avg.roundTo( 2 ).toString() )    # 19.99

# 数论
Array<Int32> ps = Factor.primeFactors( 360 )    # [2, 2, 2, 3, 3, 5]
int phi = Factor.eulerPhi( 36 )                 # 12
BigNumber c = Factor.combination( 52, 5 )       # 2598960
Int32 s = Factor.isqrt( 998001 )                # 999
```

---

## 相关文档

- [Math.md](Math.md) — Math 库总览与索引
- [Math-Functions.md](Math-Functions.md) — Mathd / Mathf / Mathh 标量函数
- [Math-Vectors.md](Math-Vectors.md) — 向量类型
- [Math-Matrices.md](Math-Matrices.md) — 定长矩阵值类型（systemCalls 绑定）
- [Matrix.md](Matrix.md) — 通用动态矩阵
- [project-system-method-call-guide.md](../../project/project-system-method-call-guide.md) — system method call 绑定制作指南
