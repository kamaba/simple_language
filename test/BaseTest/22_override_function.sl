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
    _VClassa = 0;
    _init_( int a )
    {

    }
    fun()
    {
        System.Console.WriteLine("vclass fun" );
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
    _init_(int a )
    {
        base._init_(a)
    }
    override fun()
    {
        System.Console.WriteLine("oclass fun" )
    }
    #!
    override get int hashCode()
    {
        ret 1001
    }
    #!
    override string toString()
    {
        ret "OClass.toString()"
    }
    !#
    fun2(){

    }
}

OL2Class extends OClass
{
    _init_(int a )
    {

    }
    _init_(string a)
    {

    }
}

OverrideFunction
{
    static fun()
    {
        VClass oclass = OClass()
        oclass.fun()
        oc2 = OClass()
        System.Console.WriteLine(oc2.toString() + oc2.hashCode )
    }
}