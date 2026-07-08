

data VecData2
{
    x = 0.0f
    y = 0.0f
}

interface IVector2 bind VecData2
{
    float x { get; set; }
    float y { get; set; }

    float addVector(VecData2 other);
    float length();
}

class Vector2 interface IVector2 bind VecData2
{
    override float addVector(Vec2 other)
    {
        return this.x + other.x + this.y + other.y;
    }

    override float length()
    {
        return Math.sqrt(this.x * this.x + this.y * this.y);
    }
}

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
}