public abstract class Data extends object
{
    override String toString()
    { 
        ret SystemBuildDataString( this )
    }

    #将 data 实例序列化为紧凑 JSON 文本：data 成员内置递归展开为对象，
    #class 成员虚调其 toJson() 嫁接子树，enum 成员取底层值，
    #array/list/set/tuple 转数组、map 转对象（规则详见 md/syntax/data.md）。
    public String toJson()
    {
        TreeNode<string> proto = TreeNode<string>()
        ret SystemDataToJson( this, proto, false )
    }

    #toJson 的 2 空格缩进格式化版本。
    public String toJsonPretty()
    {
        TreeNode<string> proto = TreeNode<string>()
        ret SystemDataToJson( this, proto, true )
    }
}