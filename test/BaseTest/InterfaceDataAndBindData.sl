
local
{
    a = Vector2(){
        x = 1.0f,
        y = 2.0f
    }
    b = Vector2(){
        x = 3.0f,
        y = 4.0f
    }
    c = VecData2(){
        x = 5.0f,
        y = 6.0f
    }
    a.AddVector(c);

    float len = a.length()

    Console.println("Len=" + len)
}

data VecData2
{
    x = 0.0f
    y = 0.0f
}

interface IVector2 bind VecData2
{
    float addVector(VecData2 other);
    float length();
}

class Vector2 interface IVector2 bind VecData2
{
    override float addVector(VecData2 other)
    {
        return this.x + other.x + this.y + other.y;
    }

    override float length()
    {
        return (this.x * this.x) + (his.y * this.y);
    }
}
