
NCTestClass
{
    int val = 100
    
    _init_(int v)
    {
        this.val = v
    }
    
    int GetVal()
    {
        ret this.val
    }
}

NCTest
{
    static fun()
    {
        global.println("=== Null Conditional Test ===")
        
        NCTestClass obj = NCTestClass(42)
        int v1 = obj?.val
        global.println("obj?.val = " + v1)
        
        global.println("Test passed!")
    }
}
