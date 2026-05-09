
Coroutinest
{
    async Test( int timeout )
    {
        await Time.sleep(timeout )

        Console.print("cor1");

        await asyio.waitSecond( 1 )

        Console.print("cor2")
        


    }
    static fun()
    {
        cor = go Test( 2 )
        await cor
    }
}