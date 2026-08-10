ListTest
{
    static fun()
    {
        List<int> aalist = new(10)
        aalist.add(1)
        aalist.add(2)
        aalist.add(3)

        # brace-assign initialization
        List<int> blist = List<int>(){ 10, 20, 30, 40 }
        Console.println("blist.length = " + blist.length)

        for i = 0, i < blist.length, i++
        {
            Console.println("blist[" + i + "] = " + blist.getValue(i))
        }
    }
}
