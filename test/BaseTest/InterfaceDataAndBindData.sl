
local
{
    a = Vector2()
    {
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
    a.addVector(c);

    float len = a.length()

    global.println("Len=" + len)
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
        ret this.x + other.x + this.y + other.y;
    }

    override float length()
    {
        ret (this.x * this.x) + (this.y * this.y);
    }
}
