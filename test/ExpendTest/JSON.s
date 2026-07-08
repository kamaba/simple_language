JsonTest
{
    fun()
    {
        Json a = Json('{"a":10, b = [1,2,3,4] }' )  #通过字符串转为Json

        Json a = Json( {a = 20, b = [1,2,3,5 ] } )  #匿名data 转为json

        a = Json();
        a["a"] = 20;
        a["b"] = [1,2,3,4];     #手动构建json内容

        a.delete("a")  

        a.$"b".$1 #访问里边某个元素
    }
}