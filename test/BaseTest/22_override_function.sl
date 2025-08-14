import Std
import CSharp.System

VClass
{
    fun()
    {
        System.Console.Write("vclass fun" );
    }

}

OClass
{
    override fun()
    {
        System.Console.Write("oclass fun" )
    }
}

OverrideFunction
{
    static fun()
    {
        VClass oclass = OClass()
        oclass.fun()
    }
}