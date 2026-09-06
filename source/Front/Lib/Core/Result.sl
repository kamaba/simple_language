#通用返回结果封装：value 携带业务数据，code 携带状态码（0=成功），message 携带提示信息。
#与 Core.Result<T> 配对（泛型版 value 为强类型 T）。
#配套语言特性：函数返回类型声明为 Result / Result<T> 时，编译器自动注入局部变量 result，
#函数体内直接写 result.code / result.message / result.value，ret expr 会被改写为 result.value = expr。
public class Result extends Object
{
    public Object value = null
    public int code = 0
    public String message = ""

    override _init_()
    {
    }
}

#泛型版返回结果封装（语义同 Result，value 强类型 T）。
public class Result<T> extends Object
{
    public T value = null
    public int code = 0
    public String message = ""

    override _init_()
    {
    }
}
