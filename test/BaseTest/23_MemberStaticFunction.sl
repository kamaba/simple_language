import Std
import CSharp.System

class Core.Object
{
    string toString()
    {
        ret "Object.toString()"
    }
    get int  hashCode()
    {
        ret 0
    }
}

VClass
{
    fun()
    {
        System.Console.Write("vclass fun" );
    }
    override get int hashCode()
    {
        ret 100
    }
    override string toString()
    {
        ret "VClass.toString()"
    }
    static staticFun()
    {

    }
    final finalFun()
    {

    }
}

OClass extends VClass
{
    override fun()
    {
        System.Console.Write("oclass fun" )
    }
    override get int hashCode()
    {
        ret 1001
    }
    override string toString()
    {
        ret "OClass.toString()"
    }
}

OverrideFunction
{
    static fun()
    {
        VClass oclass = OClass()
        oclass.fun()
        oc2 = OClass()
        oc2.toString()
    }
}